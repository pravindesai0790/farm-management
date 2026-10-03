import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { ExpenseService } from '../../../../core/expenses/expense.service';
import { PaymentMethod, RecordSupplierPaymentRequest, SupplierPaymentAllocationRequest, UnpaidPurchaseInvoiceSummaryResponse } from '../../../../core/expenses/supplier-payment.models';
import { SupplierPaymentService } from '../../../../core/expenses/supplier-payment.service';
import { Supplier } from '../../../../core/expenses/supplier.models';
import { SupplierService } from '../../../../core/expenses/supplier.service';
import { CurrencyItem } from '../../../../core/labor/labor.models';
import { getApiErrorMessage } from '../../../../core/models/api-error.model';

@Component({
  selector: 'app-supplier-payment-editor-page',
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
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSnackBarModule,
  ],
  templateUrl: './supplier-payment-editor-page.component.html',
  styleUrl: './supplier-payment-editor-page.component.scss',
})
export class SupplierPaymentEditorPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly supplierService = inject(SupplierService);
  private readonly expenseService = inject(ExpenseService);
  private readonly paymentService = inject(SupplierPaymentService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly snack = inject(MatSnackBar);

  readonly isLoading = signal(false);
  readonly isSubmitting = signal(false);
  readonly suppliers = signal<readonly Supplier[]>([]);
  readonly currencies = signal<readonly CurrencyItem[]>([]);
  readonly unpaidInvoices = signal<UnpaidPurchaseInvoiceSummaryResponse[]>([]);
  readonly maxDate = new Date();

  readonly paymentMethods: { value: PaymentMethod; label: string }[] = [
    { value: 'BankTransfer', label: 'Bank Transfer' },
    { value: 'Cash', label: 'Cash' },
    { value: 'Upi', label: 'UPI' },
    { value: 'Cheque', label: 'Cheque' },
    { value: 'Other', label: 'Other' },
  ];

  readonly headerForm = this.fb.group({
    supplierId: ['', Validators.required],
    paymentDate: [new Date(), Validators.required],
    currencyId: ['', Validators.required],
    paymentMethod: ['BankTransfer' as PaymentMethod, Validators.required],
    amount: [null as number | null, [Validators.required, Validators.min(0.01)]],
    referenceNumber: [''],
    notes: [''],
  });

  readonly allocationsArray = this.fb.array<FormGroup>([]);

  // Computed totals for live reconciliation
  readonly totalAllocated = signal(0);

  readonly unallocatedAmount = computed(() => {
    const paymentAmt = this.headerForm.get('amount')?.value || 0;
    return Math.round((paymentAmt - this.totalAllocated()) * 100) / 100;
  });

  readonly isAllocationValid = computed(() => {
    const paymentAmt = this.headerForm.get('amount')?.value || 0;
    const allocated = this.totalAllocated();
    return paymentAmt > 0 && Math.abs(paymentAmt - allocated) < 0.01 && allocated > 0;
  });

  ngOnInit(): void {
    this.loadMasterData();

    // Check pre-selected supplier from query param if arriving from Invoice Details page
    this.route.queryParams.subscribe((params) => {
      if (params['supplierId']) {
        this.headerForm.patchValue({ supplierId: params['supplierId'] });
        this.onSupplierChange(params['supplierId']);
      }
    });

    // Listen to allocation input changes
    this.allocationsArray.valueChanges.subscribe(() => {
      this.recalculateTotalAllocated();
    });

    // Listen to payment amount changes
    this.headerForm.get('amount')?.valueChanges.subscribe(() => {
      this.recalculateTotalAllocated();
    });
  }

  loadMasterData(): void {
    this.isLoading.set(true);
    this.supplierService.list(1, 200, undefined, true).subscribe({
      next: (suppliersRes) => {
        this.suppliers.set(suppliersRes.items);
        this.expenseService.listCurrencies().subscribe({
          next: (currenciesRes: readonly CurrencyItem[]) => {
            this.currencies.set(currenciesRes);
            if (currenciesRes.length > 0) {
              const inr = currenciesRes.find((c) => c.code === 'INR') || currenciesRes[0];
              if (!this.headerForm.get('currencyId')?.value) {
                this.headerForm.patchValue({ currencyId: inr.id });
              }
            }
            this.isLoading.set(false);
          },
          error: () => this.isLoading.set(false),
        });
      },
      error: () => this.isLoading.set(false),
    });
  }

  onSupplierChange(supplierId: string): void {
    if (!supplierId) {
      this.unpaidInvoices.set([]);
      this.allocationsArray.clear();
      return;
    }

    const currencyId = this.headerForm.get('currencyId')?.value || undefined;
    this.isLoading.set(true);

    this.paymentService.getUnpaidInvoices(supplierId, currencyId).subscribe({
      next: (invoices) => {
        this.unpaidInvoices.set(invoices);
        this.buildAllocationsFormArray(invoices);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.snack.open(getApiErrorMessage(err, 'Failed to fetch unpaid invoices.'), 'Close', { duration: 4000 });
      },
    });
  }

  buildAllocationsFormArray(invoices: UnpaidPurchaseInvoiceSummaryResponse[]): void {
    this.allocationsArray.clear();
    for (const inv of invoices) {
      const group = this.fb.group({
        purchaseInvoiceId: [inv.id],
        supplierInvoiceNumber: [inv.supplierInvoiceNumber],
        invoiceDate: [inv.invoiceDate],
        dueDate: [inv.dueDate],
        totalAmount: [inv.totalAmount],
        outstandingBalance: [inv.outstandingBalance],
        allocatedAmount: [0, [Validators.min(0), Validators.max(inv.outstandingBalance)]],
      });
      this.allocationsArray.push(group);
    }
    this.recalculateTotalAllocated();
  }

  autoAllocateFifo(): void {
    const paymentAmount = this.headerForm.get('amount')?.value || 0;
    if (paymentAmount <= 0) {
      this.snack.open('Please enter a valid payment amount before auto-allocating.', 'Close', { duration: 3000 });
      return;
    }

    let remainingToAllocate = paymentAmount;
    for (let i = 0; i < this.allocationsArray.length; i++) {
      const group = this.allocationsArray.at(i) as FormGroup;
      const outstanding = group.get('outstandingBalance')?.value || 0;

      if (remainingToAllocate <= 0) {
        group.patchValue({ allocatedAmount: 0 }, { emitEvent: false });
      } else {
        const alloc = Math.min(remainingToAllocate, outstanding);
        const roundedAlloc = Math.round(alloc * 100) / 100;
        group.patchValue({ allocatedAmount: roundedAlloc }, { emitEvent: false });
        remainingToAllocate -= roundedAlloc;
      }
    }

    this.recalculateTotalAllocated();
    this.snack.open('Auto-allocation complete (FIFO applied).', 'Close', { duration: 2500 });
  }

  clearAllocations(): void {
    for (let i = 0; i < this.allocationsArray.length; i++) {
      const group = this.allocationsArray.at(i) as FormGroup;
      group.patchValue({ allocatedAmount: 0 }, { emitEvent: false });
    }
    this.recalculateTotalAllocated();
  }

  recalculateTotalAllocated(): void {
    let sum = 0;
    for (let i = 0; i < this.allocationsArray.length; i++) {
      const group = this.allocationsArray.at(i) as FormGroup;
      const val = group.get('allocatedAmount')?.value || 0;
      sum += val;
    }
    this.totalAllocated.set(Math.round(sum * 100) / 100);
  }

  onSubmit(): void {
    if (this.headerForm.invalid || !this.isAllocationValid() || this.isSubmitting()) {
      this.headerForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    const headerVal = this.headerForm.getRawValue();

    const activeAllocations: SupplierPaymentAllocationRequest[] = [];
    for (let i = 0; i < this.allocationsArray.length; i++) {
      const group = this.allocationsArray.at(i) as FormGroup;
      const allocatedAmount = group.get('allocatedAmount')?.value || 0;
      if (allocatedAmount > 0) {
        activeAllocations.push({
          purchaseInvoiceId: group.get('purchaseInvoiceId')?.value,
          allocatedAmount,
        });
      }
    }

    const payload: RecordSupplierPaymentRequest = {
      supplierId: headerVal.supplierId!,
      paymentDate: this.formatDate(headerVal.paymentDate!),
      amount: headerVal.amount!,
      currencyId: headerVal.currencyId!,
      paymentMethod: headerVal.paymentMethod!,
      allocations: activeAllocations,
      referenceNumber: headerVal.referenceNumber?.trim() || undefined,
      notes: headerVal.notes?.trim() || undefined,
      idempotencyKey: `PAY-${Date.now()}-${Math.random().toString(36).substring(2, 7)}`,
    };

    this.paymentService.recordPayment(payload).subscribe({
      next: (res) => {
        this.snack.open('Supplier payment recorded successfully.', 'Close', { duration: 3000 });
        this.router.navigate(['/expenses/payments', res.id]);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.snack.open(getApiErrorMessage(err, 'Failed to record supplier payment.'), 'Close', { duration: 5000 });
      },
    });
  }

  private formatDate(d: Date): string {
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
