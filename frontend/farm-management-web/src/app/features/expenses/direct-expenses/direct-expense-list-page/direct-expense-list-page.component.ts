import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { debounceTime, distinctUntilChanged, finalize, merge } from 'rxjs';
import { PermissionService } from '../../../../core/auth/permission.service';
import { ExpenseCategory } from '../../../../core/expenses/expense-category.models';
import { ExpenseCategoryService } from '../../../../core/expenses/expense-category.service';
import { Expense, ExpenseFilter, ExpenseStatus } from '../../../../core/expenses/expense.models';
import { ExpenseService } from '../../../../core/expenses/expense.service';
import { Supplier } from '../../../../core/expenses/supplier.models';
import { SupplierService } from '../../../../core/expenses/supplier.service';
import { Farm } from '../../../../core/farm-management/farm-management.models';
import { FarmManagementService } from '../../../../core/farm-management/farm-management.service';
import { getApiErrorMessage } from '../../../../core/models/api-error.model';
import { formatDateOnly } from '../../../../core/utils/date.utils';
import { ExpensesSubNavComponent } from '../../components/expenses-sub-nav/expenses-sub-nav.component';
import { DirectExpenseDetailDialogComponent } from '../direct-expense-detail-dialog/direct-expense-detail-dialog.component';
import { DirectExpenseEditorDialogComponent } from '../direct-expense-editor-dialog/direct-expense-editor-dialog.component';
import { DirectExpenseReversalDialogComponent } from '../direct-expense-reversal-dialog/direct-expense-reversal-dialog.component';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-direct-expense-list-page',
  standalone: true,
  providers: [provideNativeDateAdapter()],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule,
    ExpensesSubNavComponent,
  ],
  templateUrl: './direct-expense-list-page.component.html',
  styleUrl: './direct-expense-list-page.component.scss',
})
export class DirectExpenseListPageComponent implements OnInit {
  private readonly expenseService = inject(ExpenseService);
  private readonly farmService = inject(FarmManagementService);
  private readonly expenseCategoryService = inject(ExpenseCategoryService);
  private readonly supplierService = inject(SupplierService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);

  readonly columns = [
    'expenseDate',
    'farmName',
    'category',
    'description',
    'referenceNumber',
    'amount',
    'status',
    'actions',
  ];

  readonly expenses = signal<readonly Expense[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly isLoading = signal(false);

  readonly farms = signal<readonly Farm[]>([]);
  readonly categories = signal<readonly ExpenseCategory[]>([]);
  readonly suppliers = signal<readonly Supplier[]>([]);

  readonly totalPostedAmount = computed(() =>
    this.expenses().filter((e) => e.status === 'Posted').reduce((acc, e) => acc + (e.amount || 0), 0),
  );
  readonly draftCount = computed(() => this.expenses().filter((e) => e.status === 'Draft').length);
  readonly postedCount = computed(() => this.expenses().filter((e) => e.status === 'Posted').length);
  readonly linkedCount = computed(() =>
    this.expenses().filter((e) => !!(e.farmAreaId || e.cropCycleId || e.plantationId)).length,
  );

  readonly filterForm = this.fb.group({
    search: [''],
    farmId: [''],
    categoryId: [''],
    supplierId: [''],
    status: ['all'],
    from: [null as Date | string | null],
    to: [null as Date | string | null],
  });

  ngOnInit(): void {
    this.loadFilterLookups();

    merge(
      this.filterForm.controls.search.valueChanges.pipe(
        debounceTime(300),
        distinctUntilChanged(),
      ),
      this.filterForm.controls.farmId.valueChanges,
      this.filterForm.controls.categoryId.valueChanges,
      this.filterForm.controls.supplierId.valueChanges,
      this.filterForm.controls.status.valueChanges,
      this.filterForm.controls.from.valueChanges,
      this.filterForm.controls.to.valueChanges,
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.loadExpenses();
      });

    this.loadExpenses();
  }

  loadFilterLookups(): void {
    this.farmService.listFarms(1, 100, '', true).subscribe({
      next: (res) => this.farms.set(res.items),
      error: () => {},
    });

    this.expenseCategoryService.list(1, 100, '', true).subscribe({
      next: (res) => this.categories.set(res.items),
      error: () => {},
    });

    this.supplierService.list(1, 100, '', true).subscribe({
      next: (res) => this.suppliers.set(res.items),
      error: () => {},
    });
  }

  loadExpenses(): void {
    this.isLoading.set(true);

    const val = this.filterForm.getRawValue();
    const filter: ExpenseFilter = {
      search: val.search?.trim() || null,
      farmId: val.farmId || null,
      categoryId: val.categoryId || null,
      supplierId: val.supplierId || null,
      status: val.status === 'all' || !val.status ? null : (val.status as ExpenseStatus),
      from: formatDateOnly(val.from),
      to: formatDateOnly(val.to),
    };

    this.expenseService
      .list(filter, this.pageIndex() + 1, this.pageSize())
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          this.expenses.set(response.items);
          this.totalCount.set(response.totalCount);
        },
        error: (err) => {
          this.snack.open(getApiErrorMessage(err, 'Failed to load expenses.'), 'Close', { duration: 5000 });
        },
      });
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadExpenses();
  }

  resetFilters(): void {
    this.filterForm.reset({
      search: '',
      farmId: '',
      categoryId: '',
      supplierId: '',
      status: 'all',
      from: null,
      to: null,
    });
  }

  openCreateDialog(): void {
    const dialogRef = this.dialog.open(DirectExpenseEditorDialogComponent, {
      width: '680px',
      disableClose: true,
    });

    dialogRef.afterClosed().subscribe((created) => {
      if (created) {
        this.loadExpenses();
      }
    });
  }

  openEditDialog(expense: Expense): void {
    const dialogRef = this.dialog.open(DirectExpenseEditorDialogComponent, {
      width: '680px',
      disableClose: true,
      data: { expense },
    });

    dialogRef.afterClosed().subscribe((updated) => {
      if (updated) {
        this.loadExpenses();
      }
    });
  }

  openDetailDialog(expense: Expense): void {
    this.dialog.open(DirectExpenseDetailDialogComponent, {
      width: '640px',
      data: { expense },
    });
  }

  postExpense(expense: Expense): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '460px',
      data: {
        title: 'Post Direct Expense',
        icon: 'send',
        message: `Are you sure you want to post this expense? Once posted, this expense cannot be edited or deleted.`,
        confirmText: 'Post Expense',
        cancelText: 'Cancel',
        color: 'primary',
        details: [
          { label: 'Category', value: expense.expenseCategoryName },
          { label: 'Farm', value: expense.farmName },
          { label: 'Expense Date', value: expense.expenseDate },
          { label: 'Amount', value: `${expense.currencySymbol || '₹'} ${expense.amount.toLocaleString('en-IN', { minimumFractionDigits: 2 })}` },
        ],
      },
    });

    dialogRef.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;

      this.isLoading.set(true);
      this.expenseService
        .post(expense.id)
        .pipe(
          finalize(() => this.isLoading.set(false)),
          takeUntilDestroyed(this.destroyRef),
        )
        .subscribe({
          next: () => {
            this.snack.open('Expense posted successfully.', 'Close', { duration: 3000 });
            this.loadExpenses();
          },
          error: (err) => {
            this.snack.open(getApiErrorMessage(err, 'Failed to post expense.'), 'Close', { duration: 5000 });
          },
        });
    });
  }

  openReverseDialog(expense: Expense): void {
    const dialogRef = this.dialog.open(DirectExpenseReversalDialogComponent, {
      width: '520px',
      disableClose: true,
      data: { expense },
    });

    dialogRef.afterClosed().subscribe((reversed) => {
      if (reversed) {
        this.loadExpenses();
      }
    });
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Draft':
        return 'status-draft';
      case 'Posted':
        return 'status-posted';
      case 'Reversed':
        return 'status-reversed';
      default:
        return '';
    }
  }
}
