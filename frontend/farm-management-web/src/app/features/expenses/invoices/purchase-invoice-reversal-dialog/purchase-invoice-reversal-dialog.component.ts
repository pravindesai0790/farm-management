import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PurchaseInvoiceResponse } from '../../../../core/expenses/purchase-invoice.models';
import { PurchaseInvoiceService } from '../../../../core/expenses/purchase-invoice.service';
import { getApiErrorMessage } from '../../../../core/models/api-error.model';

export interface PurchaseInvoiceReversalDialogData {
  readonly invoice: PurchaseInvoiceResponse;
}

@Component({
  selector: 'app-purchase-invoice-reversal-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './purchase-invoice-reversal-dialog.component.html',
  styleUrl: './purchase-invoice-reversal-dialog.component.scss',
})
export class PurchaseInvoiceReversalDialogComponent {
  readonly data = inject<PurchaseInvoiceReversalDialogData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<PurchaseInvoiceReversalDialogComponent>);
  private readonly fb = inject(FormBuilder);
  private readonly invoiceService = inject(PurchaseInvoiceService);
  private readonly snack = inject(MatSnackBar);

  readonly isSubmitting = signal(false);

  readonly form = this.fb.group({
    reason: ['', [Validators.required, Validators.maxLength(500)]],
  });

  onSubmit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    const reason = this.form.getRawValue().reason!.trim();

    this.invoiceService.reverse(this.data.invoice.id, { reason }).subscribe({
      next: (reversed) => {
        this.snack.open('Invoice successfully reversed.', 'Close', { duration: 3000 });
        this.dialogRef.close(reversed);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.snack.open(getApiErrorMessage(err, 'Failed to reverse invoice.'), 'Close', { duration: 5000 });
      },
    });
  }
}
