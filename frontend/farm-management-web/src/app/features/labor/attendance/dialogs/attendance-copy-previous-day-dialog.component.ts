import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatDatepickerModule } from "@angular/material/datepicker";
import {
  MAT_DIALOG_DATA,
  MatDialogModule,
  MatDialogRef,
} from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { finalize } from "rxjs";

import {
  AttendanceRecord,
  DailyAttendanceResponse,
} from "../../../../core/labor/attendance.models";
import { AttendanceService } from "../../../../core/labor/attendance.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import {
  formatDateOnly,
  parseDateOnly,
} from "../../../../core/utils/date.utils";
import { ErrorAlertComponent } from "../../../../shared/components/error-alert/error-alert.component";

export interface AttendanceCopyPreviousDayDialogData {
  readonly farmId: string;
  readonly farmName: string;
  readonly targetDate: string;
}

export interface AttendanceCopyPreviousDayResult {
  readonly sourceDate: string;
  readonly sourceRecords: readonly AttendanceRecord[];
}

@Component({
  selector: "app-attendance-copy-previous-day-dialog",
  standalone: true,
  imports: [
    CommonModule,
    ErrorAlertComponent,
    MatButtonModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    ReactiveFormsModule,
  ],
  template: `
    <div class="dialog-container">
      <div class="dialog-header">
        <div class="header-icon">
          <mat-icon>content_copy</mat-icon>
        </div>
        <div class="header-title">
          <h2 mat-dialog-title>Copy Previous Day's Attendance</h2>
          <p class="subtitle">
            {{ data.farmName }} &bull; Target: {{ data.targetDate }}
          </p>
        </div>
      </div>

      <mat-dialog-content class="dialog-content">
        @if (errorMessage()) {
          <app-error-alert [error]="errorMessage()!" />
        }

        <p class="info-text">
          Select a prior date to copy the worker roster and attendance types into today's draft.
          Earnings will be recalculated using today's active wage rates, and any workers
          who are no longer eligible on {{ data.targetDate }} will be automatically excluded.
        </p>

        <form [formGroup]="form" class="form-layout">
          <div class="date-row">
            <mat-form-field appearance="outline" class="date-field">
              <mat-label>Source Date</mat-label>
              <input
                matInput
                [matDatepicker]="picker"
                formControlName="sourceDate"
                placeholder="YYYY-MM-DD"
              />
              <mat-datepicker-toggle matIconSuffix [for]="picker" />
              <mat-datepicker #picker />
            </mat-form-field>

            <button
              mat-stroked-button
              color="primary"
              type="button"
              [disabled]="isChecking() || form.invalid"
              (click)="checkSourceAttendance()"
            >
              @if (isChecking()) {
                <mat-spinner diameter="16" />
              } @else {
                <mat-icon>search</mat-icon>
              }
              Check Roster
            </button>
          </div>
        </form>

        @if (sourceAttendance(); as src) {
          <div class="preview-card" [class.preview-empty]="src.records.length === 0">
            <div class="preview-header">
              <mat-icon>{{ src.records.length > 0 ? "verified" : "info" }}</mat-icon>
              <strong>
                {{ src.records.length }} worker record(s) found on {{ formattedSourceDate() }}
              </strong>
            </div>

            @if (src.records.length > 0) {
              <div class="preview-breakdown">
                <span class="chip">Full Day: {{ src.summary.fullDayCount }}</span>
                <span class="chip">Half Day: {{ src.summary.halfDayCount }}</span>
                <span class="chip">Hourly: {{ src.summary.hourlyCount }}</span>
                @if (src.summary.notWorkedCount > 0) {
                  <span class="chip">Not Worked: {{ src.summary.notWorkedCount }}</span>
                }
              </div>
            } @else {
              <p class="no-records-msg">
                No attendance was recorded for this farm on {{ formattedSourceDate() }}. Please select another date.
              </p>
            }
          </div>
        }
      </mat-dialog-content>

      <mat-dialog-actions class="dialog-actions">
        <button mat-button mat-dialog-close type="button">Cancel</button>
        <button
          mat-flat-button
          color="primary"
          type="button"
          [disabled]="
            isChecking() ||
            !sourceAttendance() ||
            (sourceAttendance()?.records?.length ?? 0) === 0
          "
          (click)="confirmCopy()"
        >
          <mat-icon>copy_all</mat-icon>
          Copy Roster ({{ sourceAttendance()?.records?.length ?? 0 }})
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
        padding: 20px 24px;
      }

      .info-text {
        font-size: 0.88rem;
        line-height: 1.45;
        color: var(--mat-sys-on-surface-variant, #555);
        margin: 0 0 16px;
      }

      .date-row {
        display: flex;
        align-items: center;
        gap: 12px;

        .date-field {
          flex: 1;
        }

        button {
          height: 48px;
          margin-bottom: 22px;
        }
      }

      .preview-card {
        padding: 16px;
        border-radius: 8px;
        background: #e8f5e9;
        border: 1px solid #c8e6c9;
        margin-top: 8px;

        &.preview-empty {
          background: #fff3e0;
          border-color: #ffe0b2;
          color: #e65100;
        }

        .preview-header {
          display: flex;
          align-items: center;
          gap: 8px;
          color: #1b7a36;

          mat-icon {
            font-size: 20px;
            width: 20px;
            height: 20px;
          }
        }

        .preview-breakdown {
          display: flex;
          gap: 8px;
          flex-wrap: wrap;
          margin-top: 10px;

          .chip {
            padding: 2px 8px;
            background: #ffffff;
            border-radius: 12px;
            font-size: 0.75rem;
            font-weight: 500;
            border: 1px solid rgba(0, 0, 0, 0.08);
          }
        }

        .no-records-msg {
          margin: 6px 0 0;
          font-size: 0.85rem;
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
export class AttendanceCopyPreviousDayDialogComponent implements OnInit {
  readonly data = inject<AttendanceCopyPreviousDayDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<AttendanceCopyPreviousDayDialogComponent>);
  private readonly attendanceService = inject(AttendanceService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly isChecking = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly sourceAttendance = signal<DailyAttendanceResponse | null>(null);

  readonly form = this.formBuilder.group({
    sourceDate: [null as Date | null, Validators.required],
  });

  ngOnInit(): void {
    // Default source date to targetDate - 1 day
    const target = parseDateOnly(this.data.targetDate) || new Date();
    const prev = new Date(target);
    prev.setDate(prev.getDate() - 1);
    this.form.controls.sourceDate.setValue(prev);

    this.checkSourceAttendance();
  }

  formattedSourceDate(): string {
    const d = this.form.controls.sourceDate.value;
    return formatDateOnly(d) || "";
  }

  checkSourceAttendance(): void {
    const d = this.form.controls.sourceDate.value;
    const dateStr = formatDateOnly(d);
    if (!dateStr) return;

    this.isChecking.set(true);
    this.errorMessage.set(null);
    this.sourceAttendance.set(null);

    this.attendanceService
      .getDailyAttendance(this.data.farmId, dateStr)
      .pipe(
        finalize(() => this.isChecking.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (res) => {
          this.sourceAttendance.set(res);
        },
        error: (err) => {
          this.errorMessage.set(getApiErrorMessage(err, "Failed to load prior attendance."));
        },
      });
  }

  confirmCopy(): void {
    const src = this.sourceAttendance();
    const dateStr = this.formattedSourceDate();
    if (!src || !dateStr) return;

    const result: AttendanceCopyPreviousDayResult = {
      sourceDate: dateStr,
      sourceRecords: src.records,
    };

    this.dialogRef.close(result);
  }
}
