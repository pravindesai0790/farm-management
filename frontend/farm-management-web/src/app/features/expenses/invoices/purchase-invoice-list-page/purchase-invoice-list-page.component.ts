import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
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
import { debounceTime, distinctUntilChanged, finalize, map, merge, auditTime, catchError, of, switchMap, Subject, tap } from 'rxjs';
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
import { PurchaseInvoiceReceiveDialogComponent } from '../purchase-invoice-receive-dialog/purchase-invoice-receive-dialog.component';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';

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
    'deliveryStatus',
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
  readonly isFiltersExpanded = signal<boolean>(false);

  readonly totalInvoicedAmount = computed(() =>
    this.invoices().filter((inv) => inv.status === 'Posted').reduce((sum, inv) => sum + (inv.totalAmount || 0), 0),
  );
  readonly totalOutstandingBalance = computed(() =>
    this.invoices().filter((inv) => inv.status === 'Posted').reduce((sum, inv) => sum + (inv.outstandingBalance || 0), 0),
  );
  readonly overdueCount = computed(() =>
    this.invoices().filter((inv) => inv.status === 'Posted' && inv.dueStatus === 'Overdue').length,
  );
  readonly awaitingDeliveryCount = computed(() =>
    this.invoices().filter((inv) => inv.status === 'Posted' && inv.receiptStatus && inv.receiptStatus !== 'FullyReceived' && inv.receiptStatus !== 'NotApplicable').length,
  );

  readonly filterForm = this.fb.group({
    search: [''],
    farmId: [''],
    supplierId: [''],
    status: ['all'],
    paymentStatus: ['all'],
    dueStatus: ['all'],
    receiptStatus: ['all'],
    from: [null as Date | string | null],
    to: [null as Date | string | null],
  });

  readonly filterValues = toSignal(
    this.filterForm.valueChanges.pipe(map(() => this.filterForm.getRawValue())),
    { initialValue: this.filterForm.getRawValue() }
  );

  readonly hasActiveFilters = computed(() => {
    const val = this.filterValues();
    if (!val) return false;
    return !!(
      val.search?.trim() ||
      val.farmId ||
      val.supplierId ||
      (val.status && val.status !== 'all') ||
      (val.paymentStatus && val.paymentStatus !== 'all') ||
      (val.dueStatus && val.dueStatus !== 'all') ||
      (val.receiptStatus && val.receiptStatus !== 'all') ||
      val.from ||
      val.to
    );
  });

  readonly activeFilterCount = computed(() => {
    const val = this.filterValues();
    if (!val) return 0;
    let count = 0;
    if (val.search?.trim()) count++;
    if (val.farmId) count++;
    if (val.supplierId) count++;
    if (val.status && val.status !== 'all') count++;
    if (val.paymentStatus && val.paymentStatus !== 'all') count++;
    if (val.dueStatus && val.dueStatus !== 'all') count++;
    if (val.receiptStatus && val.receiptStatus !== 'all') count++;
    if (val.from || val.to) count++;
    return count;
  });

  readonly isAwaitingDeliveryFilterActive = computed(() => {
    const val = this.filterValues();
    return val?.receiptStatus === 'AwaitingDelivery' || val?.receiptStatus === 'NotReceived';
  });

  readonly isOverdueFilterActive = computed(() => {
    const val = this.filterValues();
    return val?.dueStatus === 'Overdue';
  });

  private readonly loadTrigger$ = new Subject<void>();

  ngOnInit(): void {
    this.loadFilterLookups();

    // 1. Reactive invoice loading pipeline using switchMap for in-flight cancellation
    this.loadTrigger$
      .pipe(
        tap(() => this.isLoading.set(true)),
        switchMap(() => {
          const val = this.filterForm.getRawValue();
          const filter: PurchaseInvoiceFilter = {
            search: val.search?.trim() || null,
            farmId: val.farmId || null,
            supplierId: val.supplierId || null,
            status: val.status === 'all' || !val.status ? null : (val.status as PurchaseInvoiceStatus),
            paymentStatus: val.paymentStatus === 'all' || !val.paymentStatus ? null : (val.paymentStatus as any),
            dueStatus: val.dueStatus === 'all' || !val.dueStatus ? null : (val.dueStatus as any),
            receiptStatus: val.receiptStatus === 'all' || !val.receiptStatus ? null : (val.receiptStatus as any),
            from: formatDateOnly(val.from),
            to: formatDateOnly(val.to),
          };

          return this.invoiceService
            .list(filter, this.pageIndex() + 1, this.pageSize())
            .pipe(
              catchError((err) => {
                this.snack.open(getApiErrorMessage(err, 'Failed to load supplier invoices.'), 'Close', { duration: 5000 });
                return of({ items: [], totalCount: 0, page: 1, pageSize: this.pageSize(), totalPages: 0 });
              }),
            );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          this.invoices.set(response.items);
          this.totalCount.set(response.totalCount);
          this.isLoading.set(false);
        },
      });

    // 2. Search control stream debounced 300ms
    const search$ = this.filterForm.controls.search.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
    );

    // 3. Other controls batched via auditTime(0) so multi-control resets or changes emit exactly once
    const otherControls$ = merge(
      this.filterForm.controls.farmId.valueChanges,
      this.filterForm.controls.supplierId.valueChanges,
      this.filterForm.controls.status.valueChanges,
      this.filterForm.controls.paymentStatus.valueChanges,
      this.filterForm.controls.dueStatus.valueChanges,
      this.filterForm.controls.receiptStatus.valueChanges,
      this.filterForm.controls.from.valueChanges,
      this.filterForm.controls.to.valueChanges,
    ).pipe(
      auditTime(0),
    );

    // 4. Any filter change resets page to 0 and triggers the loading pipeline
    merge(search$, otherControls$)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.loadTrigger$.next();
      });

    // Initial load
    this.loadTrigger$.next();
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
    this.loadTrigger$.next();
  }

  toggleFilters(): void {
    this.isFiltersExpanded.update((v) => !v);
  }

  toggleAwaitingDeliveryFilter(): void {
    if (this.isAwaitingDeliveryFilterActive()) {
      this.filterForm.patchValue({ receiptStatus: 'all' });
    } else {
      this.filterForm.patchValue({ receiptStatus: 'AwaitingDelivery' });
    }
  }

  toggleOverdueFilter(): void {
    if (this.isOverdueFilterActive()) {
      this.filterForm.patchValue({ dueStatus: 'all' });
    } else {
      this.filterForm.patchValue({ dueStatus: 'Overdue' });
    }
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadTrigger$.next();
  }

  resetFilters(): void {
    this.filterForm.setValue({
      search: '',
      farmId: '',
      supplierId: '',
      status: 'all',
      paymentStatus: 'all',
      dueStatus: 'all',
      receiptStatus: 'all',
      from: null,
      to: null,
    });
  }

  clearSearch(): void {
    this.filterForm.patchValue({ search: '' });
  }

  removeFilter(field: string): void {
    switch (field) {
      case 'status':
        this.filterForm.patchValue({ status: 'all' });
        break;
      case 'paymentStatus':
        this.filterForm.patchValue({ paymentStatus: 'all' });
        break;
      case 'receiptStatus':
        this.filterForm.patchValue({ receiptStatus: 'all' });
        break;
      case 'dueStatus':
        this.filterForm.patchValue({ dueStatus: 'all' });
        break;
      case 'dateRange':
        this.filterForm.patchValue({ from: null, to: null });
        break;
      case 'farmId':
        this.filterForm.patchValue({ farmId: '' });
        break;
      case 'supplierId':
        this.filterForm.patchValue({ supplierId: '' });
        break;
      case 'search':
        this.filterForm.patchValue({ search: '' });
        break;
    }
  }

  getFarmName(farmId: string | null | undefined): string {
    if (!farmId) return '';
    return this.farms().find((f) => f.id === farmId)?.name || 'Farm';
  }

  getSupplierName(supplierId: string | null | undefined): string {
    if (!supplierId) return '';
    return this.suppliers().find((s) => s.id === supplierId)?.name || 'Supplier';
  }

  postInvoice(invoice: PurchaseInvoiceResponse): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '460px',
      data: {
        title: 'Post Supplier Invoice',
        icon: 'send',
        message: `Are you sure you want to post invoice #${invoice.supplierInvoiceNumber}? Once posted, this invoice cannot be edited or deleted.`,
        confirmText: 'Post Invoice',
        cancelText: 'Cancel',
        color: 'primary',
        details: [
          { label: 'Supplier', value: invoice.supplierName },
          { label: 'Invoice Date', value: invoice.invoiceDate },
          { label: 'Total Amount', value: `${invoice.currencySymbol || '₹'} ${invoice.totalAmount.toLocaleString('en-IN', { minimumFractionDigits: 2 })}` },
        ],
      },
    });

    dialogRef.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;

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

  openReceiveDialog(invoice: PurchaseInvoiceResponse): void {
    const dialogRef = this.dialog.open(PurchaseInvoiceReceiveDialogComponent, {
      width: '920px',
      maxWidth: '95vw',
      maxHeight: '92vh',
      panelClass: 'receive-items-dialog-panel',
      disableClose: true,
      data: { invoice },
    });

    dialogRef.afterClosed().subscribe((res) => {
      if (res) {
        this.loadInvoices();
      }
    });
  }

  isAwaitingDelivery(invoice: PurchaseInvoiceResponse): boolean {
    return (
      invoice.status === 'Posted' &&
      !!invoice.receiptStatus &&
      invoice.receiptStatus !== 'FullyReceived' &&
      invoice.receiptStatus !== 'NotApplicable'
    );
  }

  getDeliveryBadgeClass(invoice: PurchaseInvoiceResponse): string {
    if (invoice.status !== 'Posted') return '';
    switch (invoice.receiptStatus) {
      case 'FullyReceived':
        return 'delivery-received';
      case 'PartiallyReceived':
        return 'delivery-partial';
      case 'NotReceived':
      case 'AwaitingDelivery':
        return 'delivery-awaiting';
      default:
        return '';
    }
  }

  getDeliveryBadgeText(invoice: PurchaseInvoiceResponse): string {
    if (invoice.status !== 'Posted') return '—';
    const pendingCount = invoice.pendingDeliveryLinesCount ?? invoice.totalInventoryLinesCount ?? 0;
    switch (invoice.receiptStatus) {
      case 'FullyReceived':
        return 'Received';
      case 'PartiallyReceived':
        return `Partial (${pendingCount} pending)`;
      case 'NotReceived':
      case 'AwaitingDelivery':
        return pendingCount > 0 ? `Awaiting (${pendingCount} ${pendingCount === 1 ? 'item' : 'items'})` : 'Awaiting';
      case 'NotApplicable':
        return '—';
      default:
        return invoice.receiptStatus || '—';
    }
  }

  getDeliveryTooltip(invoice: PurchaseInvoiceResponse): string {
    if (invoice.status !== 'Posted') {
      return invoice.status === 'Draft' ? 'Invoice must be posted before receiving inventory' : 'Invoice is reversed';
    }
    if (invoice.receiptStatus === 'NotApplicable') {
      return 'Non-inventory expense invoice (no items to receive)';
    }
    if (invoice.receiptStatus === 'FullyReceived') {
      return 'All inventory line items received into stock';
    }
    const pending = invoice.pendingDeliveryLinesCount ?? invoice.totalInventoryLinesCount ?? 0;
    const total = invoice.totalInventoryLinesCount ?? 0;
    return `${pending} of ${total} inventory items awaiting delivery into storage location`;
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
