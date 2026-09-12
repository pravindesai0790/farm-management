import { DatePipe, DecimalPipe } from "@angular/common";
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import {
  MAT_DIALOG_DATA,
  MatDialogModule,
  MatDialogRef,
} from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSnackBar } from "@angular/material/snack-bar";
import { finalize } from "rxjs";

import {
  WorkerPayment,
  formatPaymentMethod,
  formatPaymentType,
} from "../../../../core/labor/labor.models";
import { LaborService } from "../../../../core/labor/labor.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import { ErrorAlertComponent } from "../../../../shared/components/error-alert/error-alert.component";

export interface WorkerPaymentCancelDialogData {
  readonly workerId: string;
  readonly payment: WorkerPayment;
}

@Component({
  selector: "app-worker-payment-cancel-dialog",
  standalone: true,
  imports: [
    DatePipe,
    DecimalPipe,
    ErrorAlertComponent,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    ReactiveFormsModule,
  ],
  template: `
    <div class="dialog-container">
      <h2 mat-dialog-title class="dialog-header">
        <mat-icon color="warn">cancel</mat-icon>
        Cancel Worker Payment
      </h2>

      <mat-dialog-content class="dialog-body">
        <div class="warning-banner">
          <mat-icon>warning</mat-icon>
          <div>
            <strong>Are you sure you want to cancel this payment?</strong>
            <p>
              Cancelling will reverse any allocations made against this payment and
              recalculate the worker's available payable balance. This action cannot be undone.
            </p>
          </div>
        </div>

        <div class="payment-details-card">
          <div class="detail-row">
            <span class="label">Worker</span>
            <span class="value">{{ data.payment.workerDisplayName || 'Worker' }}</span>
          </div>
          <div class="detail-row">
            <span class="label">Date</span>
            <span class="value">{{ data.payment.paymentDate | date:'mediumDate' }}</span>
          </div>
          <div class="detail-row">
            <span class="label">Payment Type</span>
            <span class="value font-bold">{{ formatType(data.payment.paymentType) }}</span>
          </div>
          <div class="detail-row">
            <span class="label">Amount</span>
            <span class="value font-bold amount-text">
              {{ data.payment.currencySymbol || '₹' }}{{ data.payment.amount | number:'1.2-2' }}
            </span>
          </div>
          <div class="detail-row">
            <span class="label">Method</span>
            <span class="value">{{ formatMethod(data.payment.paymentMethod) }}</span>
          </div>
          @if (data.payment.referenceNumber) {
            <div class="detail-row">
              <span class="label">Reference</span>
              <span class="value code-val">{{ data.payment.referenceNumber }}</span>
            </div>
          }
        </div>

        @if (errorMessage()) {
          <app-error-alert [error]="errorMessage()!" />
        }

        <form [formGroup]="form" (ngSubmit)="submit()" class="cancel-form">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Cancellation Reason</mat-label>
            <textarea
              matInput
              rows="3"
              formControlName="reason"
              placeholder="Provide a clear reason for audit and compliance purposes"
              required
            ></textarea>
            @if (form.controls.reason.hasError('required')) {
              <mat-error>Cancellation reason is required.</mat-error>
            }
          </mat-form-field>
        </form>
      </mat-dialog-content>

      <mat-dialog-actions align="end" class="dialog-actions">
        <button mat-button type="button" (click)="cancel()" [disabled]="isSubmitting()">
          Back
        </button>
        <button
          mat-flat-button
          color="warn"
          type="button"
          (click)="submit()"
          [disabled]="form.invalid || isSubmitting()"
        >
          @if (isSubmitting()) {
            <mat-spinner diameter="18" class="btn-spinner"></mat-spinner>
            Cancelling…
          } @else {
            Confirm Cancellation
          }
        </button>
      </mat-dialog-actions>
    </div>
  `,
  styles: [`
    .dialog-container {
      display: flex;
      flex-direction: column;
      max-width: 520px;
    }

    .dialog-header {
      display: flex;
      align-items: center;
      gap: 10px;
      margin: 0;
      padding: 16px 24px;
      border-bottom: 1px solid var(--mat-sys-outline-variant, #e0e0e0);
      font-size: 1.2rem;
      font-weight: 600;
    }

    .dialog-body {
      padding: 16px 24px 8px;
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .warning-banner {
      display: flex;
      gap: 12px;
      background-color: #fff3e0;
      border: 1px solid #ffe0b2;
      border-radius: 8px;
      padding: 12px 14px;
      color: #e65100;
      font-size: 0.85rem;
      line-height: 1.4;

      mat-icon {
        flex-shrink: 0;
        font-size: 22px;
        width: 22px;
        height: 22px;
      }

      p {
        margin: 4px 0 0;
      }
    }

    .payment-details-card {
      background-color: var(--mat-sys-surface-container-low, #f9f9f9);
      border: 1px solid var(--mat-sys-outline-variant, #e0e0e0);
      border-radius: 8px;
      padding: 12px 16px;
      display: flex;
      flex-direction: column;
      gap: 6px;

      .detail-row {
        display: flex;
        justify-content: space-between;
        font-size: 0.88rem;

        .label {
          color: var(--mat-sys-on-surface-variant, #666);
        }

        .value {
          color: var(--mat-sys-on-surface, #212121);
        }

        .font-bold {
          font-weight: 600;
        }

        .amount-text {
          color: #c62828;
        }

        .code-val {
          font-family: monospace;
          background: #eee;
          padding: 1px 4px;
          border-radius: 4px;
        }
      }
    }

    .full-width {
      width: 100%;
    }

    .dialog-actions {
      padding: 12px 24px 16px;
      border-top: 1px solid var(--mat-sys-outline-variant, #e0e0e0);
      gap: 8px;

      .btn-spinner {
        display: inline-block;
        margin-right: 6px;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkerPaymentCancelDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly laborService = inject(LaborService);
  private readonly dialogRef = inject(MatDialogRef<WorkerPaymentCancelDialogComponent>);
  readonly data = inject<WorkerPaymentCancelDialogData>(MAT_DIALOG_DATA);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.group({
    reason: ["", [Validators.required, Validators.maxLength(500)]],
  });

  formatType(type: string): string {
    return formatPaymentType(type);
  }

  formatMethod(method: string): string {
    return formatPaymentMethod(method);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const reason = this.form.controls.reason.value?.trim() || null;

    this.laborService
      .cancelWorkerPayment(this.data.workerId, this.data.payment.id, { reason })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: (cancelled) => {
          this.snack.open("Worker payment cancelled successfully.", "Dismiss", {
            duration: 3500,
          });
          this.dialogRef.close(cancelled);
        },
        error: (err) => {
          this.errorMessage.set(
            getApiErrorMessage(err, "Failed to cancel worker payment."),
          );
        },
      });
  }

  cancel(): void {
    this.dialogRef.close(null);
  }
}
