import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Expense } from '../../../../core/expenses/expense.models';

export interface DirectExpenseDetailDialogData {
  readonly expense: Expense;
}

@Component({
  selector: 'app-direct-expense-detail-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatChipsModule,
    MatDialogModule,
    MatIconModule,
  ],
  templateUrl: './direct-expense-detail-dialog.component.html',
  styleUrl: './direct-expense-detail-dialog.component.scss',
})
export class DirectExpenseDetailDialogComponent {
  readonly data = inject<DirectExpenseDetailDialogData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<DirectExpenseDetailDialogComponent>);

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
