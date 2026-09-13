import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCheckboxModule } from "@angular/material/checkbox";
import {
  MAT_DIALOG_DATA,
  MatDialogModule,
  MatDialogRef,
} from "@angular/material/dialog";
import { MatIconModule } from "@angular/material/icon";

import {
  DailyAttendanceSummary,
} from "../../../../core/labor/attendance.models";

export interface AttendanceFinalizeDialogData {
  readonly farmName: string;
  readonly attendanceDate: string;
  readonly summary: DailyAttendanceSummary;
  readonly currencySymbol: string;
  readonly totalHours?: number;
}

@Component({
  selector: "app-attendance-finalize-dialog",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatDialogModule,
    MatIconModule,
  ],
  template: `
    <div class="dialog-container">
      <div class="dialog-header">
        <div class="header-icon">
          <mat-icon>verified</mat-icon>
        </div>
        <div class="header-title">
          <h2 mat-dialog-title>Finalize Daily Attendance</h2>
          <p class="subtitle">
            {{ data.farmName }} &bull; {{ data.attendanceDate }}
          </p>
        </div>
      </div>

      <mat-dialog-content class="dialog-content">
        <div class="warning-alert" role="alert">
          <mat-icon class="warning-icon">warning</mat-icon>
          <div class="warning-text">
            <strong>Finalization Warning</strong>
            <p>
              After finalization, attendance becomes read-only and earnings are created in the Worker Earnings Ledger.
            </p>
          </div>
        </div>

        <div class="summary-breakdown">
          <h3>Daily Attendance Summary</h3>
          <div class="breakdown-grid">
            <div class="breakdown-item">
              <span class="label">Farm:</span>
              <strong>{{ data.farmName }}</strong>
            </div>
            <div class="breakdown-item">
              <span class="label">Date:</span>
              <strong>{{ data.attendanceDate }}</strong>
            </div>
            <div class="breakdown-item">
              <span class="label">Number of Workers:</span>
              <strong>{{ data.summary.totalCount }} workers ({{ data.summary.workedCount }} worked)</strong>
            </div>
            <div class="breakdown-item">
              <span class="label">Full Day Count:</span>
              <span>{{ data.summary.fullDayCount }}</span>
            </div>
            <div class="breakdown-item">
              <span class="label">Half Day Count:</span>
              <span>{{ data.summary.halfDayCount }}</span>
            </div>
            <div class="breakdown-item">
              <span class="label">Hourly Count:</span>
              <span>{{ data.summary.hourlyCount }}</span>
            </div>
            @if (data.summary.hourlyCount > 0 && data.totalHours) {
              <div class="breakdown-item">
                <span class="label">Total Hours:</span>
                <strong>{{ data.totalHours | number: "1.1-2" }} hrs</strong>
              </div>
            }
            @if (data.summary.notWorkedCount > 0) {
              <div class="breakdown-item">
                <span class="label">Not Worked Count:</span>
                <span>{{ data.summary.notWorkedCount }}</span>
              </div>
            }
          </div>

          <div class="earnings-highlight">
            <span class="label">Total Estimated Earnings:</span>
            <span class="earnings-value">
              {{ data.currencySymbol }}{{ data.summary.estimatedEarnings | number: "1.2-2" }}
            </span>
          </div>
        </div>

        <div class="confirmation-checkbox-wrap">
          <mat-checkbox
            [ngModel]="confirmedByUser()"
            (ngModelChange)="confirmedByUser.set($event)"
            color="primary"
          >
            I understand and confirm that after finalization, attendance becomes read-only and earnings are created in the Worker Earnings Ledger.
          </mat-checkbox>
        </div>
      </mat-dialog-content>

      <mat-dialog-actions class="dialog-actions">
        <button mat-button mat-dialog-close type="button">Cancel</button>
        <button
          mat-flat-button
          color="primary"
          type="button"
          [disabled]="!confirmedByUser()"
          (click)="confirm()"
        >
          <mat-icon>verified</mat-icon>
          Confirm &amp; Finalize
        </button>
      </mat-dialog-actions>
    </div>
  `,
  styles: [
    `
      .dialog-container {
        display: flex;
        flex-direction: column;
        width: 100%;
        max-width: 540px;
      }

      .dialog-header {
        display: flex;
        align-items: center;
        gap: 16px;
        padding: 20px 24px 12px;
        border-bottom: 1px solid var(--mat-sys-outline-variant, #e0e0e0);

        .header-icon {
          width: 44px;
          height: 44px;
          border-radius: 10px;
          background: rgba(30, 142, 62, 0.12);
          color: #1b7a36;
          display: flex;
          align-items: center;
          justify-content: center;

          mat-icon {
            font-size: 24px;
            width: 24px;
            height: 24px;
          }
        }

        .header-title {
          h2 {
            margin: 0;
            font-size: 1.25rem;
            font-weight: 600;
          }

          .subtitle {
            margin: 2px 0 0;
            font-size: 0.85rem;
            color: var(--mat-sys-on-surface-variant, #666);
          }
        }
      }

      .dialog-content {
        padding: 20px 24px 12px;
      }

      .warning-alert {
        display: flex;
        gap: 12px;
        padding: 14px 16px;
        border-radius: 8px;
        background: #fff8e1;
        border: 1px solid #ffe082;
        color: #795548;
        margin-bottom: 20px;

        .warning-icon {
          color: #f57f17;
          font-size: 24px;
          width: 24px;
          height: 24px;
          flex-shrink: 0;
        }

        .warning-text {
          strong {
            display: block;
            margin-bottom: 4px;
            color: #b78103;
            font-size: 0.95rem;
          }

          p {
            margin: 0;
            font-size: 0.88rem;
            line-height: 1.45;
            color: #5d4037;
          }
        }
      }

      .summary-breakdown {
        background: #f8f9fa;
        border: 1px solid #e9ecef;
        border-radius: 8px;
        padding: 16px;

        h3 {
          margin: 0 0 12px;
          font-size: 0.95rem;
          font-weight: 600;
          color: #333;
        }

        .breakdown-grid {
          display: grid;
          grid-template-columns: repeat(2, 1fr);
          gap: 8px 16px;
          font-size: 0.88rem;

          .breakdown-item {
            display: flex;
            justify-content: space-between;
            gap: 8px;

            .label {
              color: #666;
            }
          }
        }

        .earnings-highlight {
          margin-top: 14px;
          padding-top: 12px;
          border-top: 1px dashed #ced4da;
          display: flex;
          justify-content: space-between;
          align-items: center;

          .label {
            font-weight: 600;
            color: #333;
          }

          .earnings-value {
            font-size: 1.2rem;
            font-weight: 700;
            color: #1b7a36;
          }
        }
      }

      .confirmation-checkbox-wrap {
        margin-top: 16px;
        padding: 12px 14px;
        background: #f5f5f5;
        border-radius: 8px;
        border: 1px solid #e0e0e0;
        font-size: 0.85rem;

        ::ng-deep .mdc-label {
          font-size: 0.86rem;
          line-height: 1.4;
          color: #333;
          font-weight: 500;
        }
      }

      .dialog-actions {
        display: flex;
        justify-content: flex-end;
        gap: 8px;
        padding: 12px 24px 16px;
        border-top: 1px solid var(--mat-sys-outline-variant, #e0e0e0);
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AttendanceFinalizeDialogComponent {
  readonly data = inject<AttendanceFinalizeDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<AttendanceFinalizeDialogComponent>);

  readonly confirmedByUser = signal<boolean>(false);

  confirm(): void {
    if (!this.confirmedByUser()) return;
    this.dialogRef.close(true);
  }
}

