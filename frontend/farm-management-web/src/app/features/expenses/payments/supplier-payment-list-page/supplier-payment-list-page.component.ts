import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Router, RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';

import { PermissionService } from '../../../../core/auth/permission.service';
import { SupplierPaymentFilter, SupplierPaymentResponse, SupplierPaymentStatus } from '../../../../core/expenses/supplier-payment.models';
import { SupplierPaymentService } from '../../../../core/expenses/supplier-payment.service';
import { Supplier } from '../../../../core/expenses/supplier.models';
import { SupplierService } from '../../../../core/expenses/supplier.service';
import { ExpensesSubNavComponent } from '../../components/expenses-sub-nav/expenses-sub-nav.component';
import { SupplierPaymentReversalDialogComponent } from '../supplier-payment-reversal-dialog/supplier-payment-reversal-dialog.component';

@Component({
  selector: 'app-supplier-payment-list-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSnackBarModule,
    ExpensesSubNavComponent,
  ],
  templateUrl: './supplier-payment-list-page.component.html',
  styleUrl: './supplier-payment-list-page.component.scss',
})
export class SupplierPaymentListPageComponent implements OnInit {
  private readonly paymentService = inject(SupplierPaymentService);
  private readonly supplierService = inject(SupplierService);
  readonly permissionService = inject(PermissionService);
  private readonly fb = inject(FormBuilder);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);

  readonly isLoading = signal(true);
  readonly payments = signal<readonly SupplierPaymentResponse[]>([]);
  readonly suppliers = signal<readonly Supplier[]>([]);
  readonly totalItems = signal(0);
  readonly currentPage = signal(1);
  readonly pageSize = signal(20);

  // KPI Metrics
  readonly totalPaidAmount = signal(0);
  readonly activeCount = signal(0);
  readonly reversedCount = signal(0);

  readonly filterForm = this.fb.group({
    search: [''],
    supplierId: [''],
    status: [''],
    from: [null as Date | null],
    to: [null as Date | null],
  });

  ngOnInit(): void {
    this.loadSuppliers();
    this.loadPayments();

    this.filterForm.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged())
      .subscribe(() => {
        this.currentPage.set(1);
        this.loadPayments();
      });
  }

  loadSuppliers(): void {
    this.supplierService.list(1, 200, undefined, true).subscribe({
      next: (res) => this.suppliers.set(res.items),
    });
  }

  loadPayments(): void {
    this.isLoading.set(true);
    const val = this.filterForm.getRawValue();

    const filter: SupplierPaymentFilter = {
      search: val.search || undefined,
      supplierId: val.supplierId || undefined,
      status: (val.status as SupplierPaymentStatus) || undefined,
      from: val.from ? this.formatDate(val.from) : undefined,
      to: val.to ? this.formatDate(val.to) : undefined,
    };

    this.paymentService.list(filter, this.currentPage(), this.pageSize()).subscribe({
      next: (res) => {
        this.payments.set(res.items);
        this.totalItems.set(res.totalCount);

        // Compute KPIs
        const active = res.items.filter((p) => p.status === 'Completed');
        const totalAmount = active.reduce((sum, p) => sum + p.amount, 0);
        this.totalPaidAmount.set(totalAmount);
        this.activeCount.set(active.length);
        this.reversedCount.set(res.items.filter((p) => p.status === 'Reversed').length);

        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snack.open('Failed to load supplier payments.', 'Close', { duration: 4000 });
      },
    });
  }

  onPageEvent(event: PageEvent): void {
    this.currentPage.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    this.loadPayments();
  }

  clearFilters(): void {
    this.filterForm.reset({
      search: '',
      supplierId: '',
      status: '',
      from: null,
      to: null,
    });
  }

  openReversalDialog(payment: SupplierPaymentResponse, event: MouseEvent): void {
    event.stopPropagation();
    const ref = this.dialog.open(SupplierPaymentReversalDialogComponent, {
      width: '540px',
      data: { payment },
    });

    ref.afterClosed().subscribe((result) => {
      if (result) {
        this.loadPayments();
      }
    });
  }

  viewDetails(id: string): void {
    this.router.navigate(['/expenses/payments', id]);
  }

  getMethodBadgeClass(method: string): string {
    switch (method) {
      case 'BankTransfer': return 'badge-info';
      case 'Cash': return 'badge-success';
      case 'Cheque': return 'badge-warning';
      case 'CreditCard': return 'badge-purple';
      case 'DigitalWallet': return 'badge-teal';
      default: return 'badge-secondary';
    }
  }

  private formatDate(d: Date): string {
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
