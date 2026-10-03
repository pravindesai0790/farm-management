import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { PermissionService } from '../../../../core/auth/permission.service';
import { PurchaseInvoiceReceiptGroupResponse, PurchaseInvoiceResponse } from '../../../../core/expenses/purchase-invoice.models';
import { PurchaseInvoiceService } from '../../../../core/expenses/purchase-invoice.service';
import { getApiErrorMessage } from '../../../../core/models/api-error.model';
import { ExpensesSubNavComponent } from '../../components/expenses-sub-nav/expenses-sub-nav.component';
import { PurchaseInvoiceReceiveDialogComponent } from '../purchase-invoice-receive-dialog/purchase-invoice-receive-dialog.component';
import { PurchaseInvoiceReversalDialogComponent } from '../purchase-invoice-reversal-dialog/purchase-invoice-reversal-dialog.component';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-purchase-invoice-detail-page',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
    ExpensesSubNavComponent,
  ],
  templateUrl: './purchase-invoice-detail-page.component.html',
  styleUrl: './purchase-invoice-detail-page.component.scss',
})
export class PurchaseInvoiceDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly invoiceService = inject(PurchaseInvoiceService);
  private readonly snack = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);

  readonly invoice = signal<PurchaseInvoiceResponse | null>(null);
  readonly receiptHistory = signal<PurchaseInvoiceReceiptGroupResponse[]>([]);
  readonly isLoading = signal(true);
  readonly isPosting = signal(false);

  readonly lineColumns = [
    'lineType',
    'itemOrCategory',
    'description',
    'quantity',
    'unitPrice',
    'lineAmount',
    'linkages',
  ];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadInvoice(id);
    } else {
      this.router.navigate(['/expenses/invoices']);
    }
  }

  loadInvoice(id: string): void {
    this.isLoading.set(true);
    this.invoiceService
      .get(id)
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (res) => {
          this.invoice.set(res);
          this.loadReceiptHistory(res.id);
        },
        error: (err) => {
          this.snack.open(getApiErrorMessage(err, 'Failed to load invoice details.'), 'Close', { duration: 5000 });
          this.router.navigate(['/expenses/invoices']);
        },
      });
  }

  loadReceiptHistory(id: string): void {
    this.invoiceService
      .getReceiptHistory(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (history) => this.receiptHistory.set(history),
        error: (err) => console.error('Failed to load receipt history', err),
      });
  }

  openReceiveDialog(): void {
    const inv = this.invoice();
    if (!inv) return;

    const dialogRef = this.dialog.open(PurchaseInvoiceReceiveDialogComponent, {
      width: '920px',
      maxWidth: '95vw',
      maxHeight: '92vh',
      panelClass: 'receive-items-dialog-panel',
      disableClose: true,
      data: { invoice: inv },
    });

    dialogRef.afterClosed().subscribe((res) => {
      if (res) {
        this.loadInvoice(inv.id);
      }
    });
  }

  postInvoice(): void {
    const inv = this.invoice();
    if (!inv) return;

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '460px',
      data: {
        title: 'Post Supplier Invoice',
        icon: 'send',
        message: `Are you sure you want to post invoice #${inv.supplierInvoiceNumber}? Once posted, this invoice cannot be edited or deleted.`,
        confirmText: 'Post Invoice',
        cancelText: 'Cancel',
        color: 'primary',
        details: [
          { label: 'Supplier', value: inv.supplierName },
          { label: 'Invoice Date', value: inv.invoiceDate },
          { label: 'Total Amount', value: `${inv.currencySymbol || '₹'} ${inv.totalAmount.toLocaleString('en-IN', { minimumFractionDigits: 2 })}` },
        ],
      },
    });

    dialogRef.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;

      this.isPosting.set(true);
      this.invoiceService
        .post(inv.id)
        .pipe(
          finalize(() => this.isPosting.set(false)),
          takeUntilDestroyed(this.destroyRef),
        )
        .subscribe({
          next: (updated) => {
            this.snack.open('Invoice posted successfully.', 'Close', { duration: 3000 });
            this.invoice.set(updated);
          },
          error: (err) => {
            this.snack.open(getApiErrorMessage(err, 'Failed to post invoice.'), 'Close', { duration: 5000 });
          },
        });
    });
  }

  openReverseDialog(): void {
    const inv = this.invoice();
    if (!inv) return;

    const dialogRef = this.dialog.open(PurchaseInvoiceReversalDialogComponent, {
      width: '520px',
      disableClose: true,
      data: { invoice: inv },
    });

    dialogRef.afterClosed().subscribe((reversed) => {
      if (reversed) {
        this.invoice.set(reversed);
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
}
