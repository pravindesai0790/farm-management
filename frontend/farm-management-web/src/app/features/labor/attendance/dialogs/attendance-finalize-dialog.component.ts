import {
  ChangeDetectionStrategy,
  Component,
  inject,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { MatButtonModule } from "@angular/material/button";
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
}

@Component({
  selector: "app-attendance-finalize-dialog",
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
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
        <div class="warning-alert">
          <mat-icon>lock</mat-icon>
          <div class="warning-text">
            <strong>Locking Attendance &amp; Posting Earnings</strong>
            <p>
              Finalizing will permanently lock today's attendance records for {{ data.farmName }}.
              Authoritative entries will be generated in the Worker Earnings Ledger for all worked labor.
              This cannot be undone from the daily screen.
            </p>
          </div>
        </div>

        <div class="summary-breakdown">
          <h3>Daily Attendance Summary</h3>
          <div class="breakdown-grid">
            <div class="breakdown-item">
              <span class="label">Total Workforce:</span>
              <strong>{{ data.summary.totalCount }} workers</strong>
            </div>
            <div class="breakdown-item">
              <span class="label">Worked Workers:</span>
              <strong class="text-success">{{ data.summary.workedCount }}</strong>
            </div>
            <div class="breakdown-item">
              <span class="label">Full Day:</span>
              <span>{{ data.summary.fullDayCount }}</span>
            </div>
            <div class="breakdown-item">
              <span class="label">Half Day:</span>
              <span>{{ data.summary.halfDayCount }}</span>
            </div>
            <div class="breakdown-item">
              <span class="label">Hourly:</span>
              <span>{{ data.summary.hourlyCount }}</span>
            </div>
            @if (data.summary.notWorkedCount > 0) {
              <div class="breakdown-item">
                <span class="label">Not Worked:</span>
                <span>{{ data.summary.notWorkedCount }}</span>
              </div>
            }
          </div>

          <div class="earnings-highlight">
            <span class="label">Total Earnings to Ledger:</span>
            <span class="earnings-value">
              {{ data.currencySymbol }}{{ data.summary.estimatedEarnings | number: "1.2-2" }}
            </span>
          </div>
        </div>
      </mat-dialog-content>

      <mat-dialog-actions class="dialog-actions">
        <button mat-button mat-dialog-close type="button">Cancel</button>
        <button
          mat-flat-button
          color="primary"
          type="button"
          (click)="confirm()"
        >
          <mat-icon>check_circle</mat-icon>
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
        max-width: 520px;
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
        padding: 20px 24px;
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

        mat-icon {
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
          }

          p {
            margin: 0;
            font-size: 0.85rem;
            line-height: 1.4;
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

            .label {
              color: #666;
            }

            .text-success {
              color: #1b7a36;
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

  confirm(): void {
    this.dialogRef.close(true);
  }
}
