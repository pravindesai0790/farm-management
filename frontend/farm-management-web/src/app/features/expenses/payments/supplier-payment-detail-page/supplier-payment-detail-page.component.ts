import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { PermissionService } from '../../../../core/auth/permission.service';
import { SupplierPaymentResponse } from '../../../../core/expenses/supplier-payment.models';
import { SupplierPaymentService } from '../../../../core/expenses/supplier-payment.service';
import { getApiErrorMessage } from '../../../../core/models/api-error.model';
import { SupplierPaymentReversalDialogComponent } from '../supplier-payment-reversal-dialog/supplier-payment-reversal-dialog.component';

@Component({
  selector: 'app-supplier-payment-detail-page',
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
    MatSnackBarModule,
  ],
  templateUrl: './supplier-payment-detail-page.component.html',
  styleUrl: './supplier-payment-detail-page.component.scss',
})
export class SupplierPaymentDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly paymentService = inject(SupplierPaymentService);
  readonly permissionService = inject(PermissionService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);

  readonly isLoading = signal(true);
  readonly payment = signal<SupplierPaymentResponse | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadPayment(id);
    } else {
      this.router.navigate(['/expenses/payments']);
    }
  }

  loadPayment(id: string): void {
    this.isLoading.set(true);
    this.paymentService.getById(id).subscribe({
      next: (res) => {
        this.payment.set(res);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.snack.open(getApiErrorMessage(err, 'Failed to load supplier payment details.'), 'Close', { duration: 5000 });
        this.router.navigate(['/expenses/payments']);
      },
    });
  }

  openReversalDialog(): void {
    const p = this.payment();
    if (!p) return;

    const ref = this.dialog.open(SupplierPaymentReversalDialogComponent, {
      width: '540px',
      data: { payment: p },
    });

    ref.afterClosed().subscribe((result) => {
      if (result) {
        this.payment.set(result);
      }
    });
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
}
