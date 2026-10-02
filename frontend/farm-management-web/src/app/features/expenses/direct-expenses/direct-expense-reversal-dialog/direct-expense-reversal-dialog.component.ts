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
import { Expense } from '../../../../core/expenses/expense.models';
import { ExpenseService } from '../../../../core/expenses/expense.service';
import { getApiErrorMessage } from '../../../../core/models/api-error.model';

export interface DirectExpenseReversalDialogData {
  readonly expense: Expense;
}

@Component({
  selector: 'app-direct-expense-reversal-dialog',
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
  templateUrl: './direct-expense-reversal-dialog.component.html',
  styleUrl: './direct-expense-reversal-dialog.component.scss',
})
export class DirectExpenseReversalDialogComponent {
  readonly data = inject<DirectExpenseReversalDialogData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<DirectExpenseReversalDialogComponent>);
  private readonly fb = inject(FormBuilder);
  private readonly expenseService = inject(ExpenseService);
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

    this.expenseService.reverse(this.data.expense.id, { reason }).subscribe({
      next: (reversed) => {
        this.snack.open('Expense successfully reversed.', 'Close', { duration: 3000 });
        this.dialogRef.close(reversed);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.snack.open(getApiErrorMessage(err, 'Failed to reverse expense.'), 'Close', { duration: 5000 });
      },
    });
  }
}
