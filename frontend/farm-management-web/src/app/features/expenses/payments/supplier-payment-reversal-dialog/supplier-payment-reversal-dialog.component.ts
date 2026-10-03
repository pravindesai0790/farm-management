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
import { SupplierPaymentResponse } from '../../../../core/expenses/supplier-payment.models';
import { SupplierPaymentService } from '../../../../core/expenses/supplier-payment.service';
import { getApiErrorMessage } from '../../../../core/models/api-error.model';

export interface SupplierPaymentReversalDialogData {
  readonly payment: SupplierPaymentResponse;
}

@Component({
  selector: 'app-supplier-payment-reversal-dialog',
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
  templateUrl: './supplier-payment-reversal-dialog.component.html',
  styleUrl: './supplier-payment-reversal-dialog.component.scss',
})
export class SupplierPaymentReversalDialogComponent {
  readonly data = inject<SupplierPaymentReversalDialogData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<SupplierPaymentReversalDialogComponent>);
  private readonly fb = inject(FormBuilder);
  private readonly paymentService = inject(SupplierPaymentService);
  private readonly snack = inject(MatSnackBar);

  readonly isSubmitting = signal(false);

  readonly form = this.fb.group({
    reason: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(500)]],
  });

  onSubmit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    const reason = this.form.getRawValue().reason!.trim();

    this.paymentService.reversePayment(this.data.payment.id, { reason }).subscribe({
      next: (reversed) => {
        this.snack.open('Supplier payment successfully reversed.', 'Close', { duration: 3000 });
        this.dialogRef.close(reversed);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.snack.open(getApiErrorMessage(err, 'Failed to reverse supplier payment.'), 'Close', { duration: 5000 });
      },
    });
  }
}
