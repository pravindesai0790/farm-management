import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
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
import { Router, RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged, finalize, merge } from 'rxjs';
import { PermissionService } from '../../../../core/auth/permission.service';
import {
  PurchaseInvoiceFilter,
  PurchaseInvoiceResponse,
  PurchaseInvoiceStatus,
} from '../../../../core/expenses/purchase-invoice.models';
import { PurchaseInvoiceService } from '../../../../core/expenses/purchase-invoice.service';
import { Supplier } from '../../../../core/expenses/supplier.models';
import { SupplierService } from '../../../../core/expenses/supplier.service';
import { Farm } from '../../../../core/farm-management/farm-management.models';
import { FarmManagementService } from '../../../../core/farm-management/farm-management.service';
import { getApiErrorMessage } from '../../../../core/models/api-error.model';
import { formatDateOnly } from '../../../../core/utils/date.utils';
import { ExpensesSubNavComponent } from '../../components/expenses-sub-nav/expenses-sub-nav.component';
import { PurchaseInvoiceReversalDialogComponent } from '../purchase-invoice-reversal-dialog/purchase-invoice-reversal-dialog.component';

@Component({
  selector: 'app-purchase-invoice-list-page',
  standalone: true,
  providers: [provideNativeDateAdapter()],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
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
  templateUrl: './purchase-invoice-list-page.component.html',
  styleUrl: './purchase-invoice-list-page.component.scss',
})
export class PurchaseInvoiceListPageComponent implements OnInit {
  private readonly invoiceService = inject(PurchaseInvoiceService);
  private readonly farmService = inject(FarmManagementService);
  private readonly supplierService = inject(SupplierService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);

  readonly columns = [
    'invoiceNumber',
    'invoiceDate',
    'dueDate',
    'supplierName',
    'farmName',
    'lines',
    'totalAmount',
    'paymentStatus',
    'status',
    'actions',
  ];

  readonly invoices = signal<readonly PurchaseInvoiceResponse[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly isLoading = signal(false);

  readonly farms = signal<readonly Farm[]>([]);
  readonly suppliers = signal<readonly Supplier[]>([]);

  readonly filterForm = this.fb.group({
    search: [''],
    farmId: [''],
    supplierId: [''],
    status: ['all'],
    paymentStatus: ['all'],
    dueStatus: ['all'],
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
      this.filterForm.controls.supplierId.valueChanges,
      this.filterForm.controls.status.valueChanges,
      this.filterForm.controls.paymentStatus.valueChanges,
      this.filterForm.controls.dueStatus.valueChanges,
      this.filterForm.controls.from.valueChanges,
      this.filterForm.controls.to.valueChanges,
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.loadInvoices();
      });

    this.loadInvoices();
  }

  loadFilterLookups(): void {
    this.farmService.listFarms(1, 100, '', true).subscribe({
      next: (res) => this.farms.set(res.items),
      error: () => {},
    });

    this.supplierService.list(1, 100, '', true).subscribe({
      next: (res) => this.suppliers.set(res.items),
      error: () => {},
    });
  }

  loadInvoices(): void {
    this.isLoading.set(true);

    const val = this.filterForm.getRawValue();
    const filter: PurchaseInvoiceFilter = {
      search: val.search?.trim() || null,
      farmId: val.farmId || null,
      supplierId: val.supplierId || null,
      status: val.status === 'all' || !val.status ? null : (val.status as PurchaseInvoiceStatus),
      paymentStatus: val.paymentStatus === 'all' || !val.paymentStatus ? null : (val.paymentStatus as any),
      dueStatus: val.dueStatus === 'all' || !val.dueStatus ? null : (val.dueStatus as any),
      from: formatDateOnly(val.from),
      to: formatDateOnly(val.to),
    };

    this.invoiceService
      .list(filter, this.pageIndex() + 1, this.pageSize())
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          this.invoices.set(response.items);
          this.totalCount.set(response.totalCount);
        },
        error: (err) => {
          this.snack.open(getApiErrorMessage(err, 'Failed to load supplier invoices.'), 'Close', { duration: 5000 });
        },
      });
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadInvoices();
  }

  resetFilters(): void {
    this.filterForm.reset({
      search: '',
      farmId: '',
      supplierId: '',
      status: 'all',
      paymentStatus: 'all',
      dueStatus: 'all',
      from: null,
      to: null,
    });
  }

  postInvoice(invoice: PurchaseInvoiceResponse): void {
    if (!confirm(`Are you sure you want to post invoice ${invoice.supplierInvoiceNumber}? Posted invoices cannot be edited.`)) {
      return;
    }

    this.isLoading.set(true);
    this.invoiceService
      .post(invoice.id)
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.snack.open('Invoice posted successfully.', 'Close', { duration: 3000 });
          this.loadInvoices();
        },
        error: (err) => {
          this.snack.open(getApiErrorMessage(err, 'Failed to post invoice.'), 'Close', { duration: 5000 });
        },
      });
  }

  openReverseDialog(invoice: PurchaseInvoiceResponse): void {
    const dialogRef = this.dialog.open(PurchaseInvoiceReversalDialogComponent, {
      width: '520px',
      disableClose: true,
      data: { invoice },
    });

    dialogRef.afterClosed().subscribe((reversed) => {
      if (reversed) {
        this.loadInvoices();
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

  getPaymentStatusClass(status: string): string {
    switch (status) {
      case 'Unpaid':
        return 'payment-unpaid';
      case 'PartiallyPaid':
        return 'payment-partial';
      case 'Paid':
        return 'payment-paid';
      default:
        return '';
    }
  }

  getDueStatusClass(status: string): string {
    switch (status) {
      case 'Overdue':
        return 'due-overdue';
      case 'DueSoon':
        return 'due-soon';
      case 'Current':
        return 'due-current';
      default:
        return '';
    }
  }
}
