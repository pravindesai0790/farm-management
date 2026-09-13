import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import {
  FormBuilder,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCheckboxModule } from "@angular/material/checkbox";
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
import { MatTooltipModule } from "@angular/material/tooltip";
import { finalize } from "rxjs";

import {
  CopyPreviousDayAttendanceResponse,
  CopyPreviousDayPreviewResponse,
  CopyPreviousDayPreviewWorkerItem,
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
  readonly currentDraftCount: number;
  readonly hasUnsavedChanges: boolean;
}

@Component({
  selector: "app-attendance-copy-previous-day-dialog",
  standalone: true,
  imports: [
    CommonModule,
    ErrorAlertComponent,
    FormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
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
            {{ data.farmName }} &bull; Target Date: {{ data.targetDate }}
          </p>
        </div>
      </div>

      <mat-dialog-content class="dialog-content">
        @if (errorMessage()) {
          <app-error-alert [error]="errorMessage()!" />
        }

        <p class="info-text">
          Copy the workforce roster from the prior attendance record into today's draft.
          Earnings will be recalculated using today's active wage rates, and workers no longer
          assigned or eligible on {{ data.targetDate }} will be excluded.
        </p>

        <!-- Source Date Selector -->
        <form [formGroup]="form" class="form-layout">
          <div class="date-row">
            <mat-form-field appearance="outline" class="date-field">
              <mat-label>Source Attendance Date</mat-label>
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
              class="check-btn"
              [disabled]="isChecking() || isCopying()"
              (click)="checkPreview()"
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

        <!-- Checking Spinner -->
        @if (isChecking()) {
          <div class="loading-state">
            <mat-spinner diameter="32" />
            <p>Scanning prior attendance records and verifying worker eligibility...</p>
          </div>
        }

        <!-- Preview Results -->
        @if (preview(); as p) {
          <!-- Roster Overview Card -->
          <div class="overview-card" [class.overview-empty]="p.totalSourceCount === 0">
            <div class="overview-header">
              <mat-icon>{{ p.totalSourceCount > 0 ? "verified" : "info" }}</mat-icon>
              <div class="overview-title">
                <strong>
                  {{ p.totalSourceCount }} worker(s) found on {{ p.sourceDate }}
                </strong>
                <span class="eligible-badge">
                  {{ p.eligibleCount }} eligible to copy
                </span>
              </div>
            </div>

            @if (p.sourceSummary) {
              <div class="breakdown-row">
                <span class="chip">Full Day: {{ p.sourceSummary.fullDayCount }}</span>
                <span class="chip">Half Day: {{ p.sourceSummary.halfDayCount }}</span>
                <span class="chip">Hourly: {{ p.sourceSummary.hourlyCount }}</span>
                @if (p.sourceSummary.notWorkedCount > 0) {
                  <span class="chip">Not Worked: {{ p.sourceSummary.notWorkedCount }}</span>
                }
              </div>
            }
          </div>

          <!-- Excluded Workers Warning Section -->
          @if (p.excludedCount > 0) {
            <div class="warning-box excluded-box">
              <div class="warning-header">
                <mat-icon color="warn">warning</mat-icon>
                <strong>{{ p.excludedCount }} worker(s) cannot be copied:</strong>
              </div>
              <ul class="excluded-list">
                @for (item of p.excludedWorkers; track item.workerId) {
                  <li>
                    <span class="worker-name">{{ item.displayName }}</span>
                    <span class="exclusion-reason">{{ item.reason }}</span>
                  </li>
                }
              </ul>
            </div>
          }

          <!-- Existing Draft Overwrite Confirmation Warning -->
          @if (needsOverwriteConfirmation()) {
            <div class="warning-box overwrite-box">
              <div class="warning-header">
                <mat-icon>announcement</mat-icon>
                <strong>Existing Draft Roster Detected</strong>
              </div>
              <p class="overwrite-text">
                Target date already has
                <strong>{{ data.currentDraftCount || p.targetExistingRecordCount }} worker(s)</strong>
                in draft{{ data.hasUnsavedChanges ? " with unsaved changes" : "" }}.
                Copying will replace the existing draft roster.
              </p>
              <mat-checkbox
                color="warn"
                [ngModel]="confirmOverwrite()"
                (ngModelChange)="confirmOverwrite.set($event)"
              >
                <strong>I understand and confirm replacing the current draft roster</strong>
              </mat-checkbox>
            </div>
          }

          <!-- Eligible Worker Selection Table -->
          @if (p.eligibleCount > 0) {
            <div class="selection-section">
              <div class="selection-header">
                <mat-checkbox
                  color="primary"
                  [checked]="isAllSelected()"
                  [indeterminate]="isIndeterminate()"
                  (change)="toggleSelectAll($event.checked)"
                >
                  <strong>Select Workers to Copy ({{ selectedWorkerIds().size }} of {{ p.eligibleCount }})</strong>
                </mat-checkbox>
              </div>

              <div class="workers-table-container">
                <table class="workers-table">
                  <thead>
                    <tr>
                      <th style="width: 40px;"></th>
                      <th>Worker</th>
                      <th>Category</th>
                      <th>Previous Type</th>
                      <th>Target Wage Preview</th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (w of eligibleWorkers(); track w.workerId) {
                      <tr [class.row-selected]="isWorkerSelected(w.workerId)">
                        <td>
                          <mat-checkbox
                            color="primary"
                            [checked]="isWorkerSelected(w.workerId)"
                            (change)="toggleWorkerSelection(w.workerId, $event.checked)"
                          />
                        </td>
                        <td>
                          <div class="worker-col">
                            <span class="worker-name">{{ w.displayName }}</span>
                            @if (w.mobileNumber) {
                              <span class="worker-sub">{{ w.mobileNumber }}</span>
                            }
                          </div>
                        </td>
                        <td>
                          <span class="cat-pill">{{ w.laborCategoryName || 'General' }}</span>
                        </td>
                        <td>
                          <span class="type-pill" [class]="'type-' + w.attendanceType.toLowerCase()">
                            {{ formatType(w.attendanceType) }}
                            @if (w.attendanceType === 'HOURLY' && w.workingHours) {
                              ({{ w.workingHours }}h)
                            }
                          </span>
                        </td>
                        <td>
                          @if (w.provisionalAmount !== null && w.provisionalAmount !== undefined) {
                            <span class="amount-text">
                              ₹{{ w.provisionalAmount | number: '1.2-2' }}
                            </span>
                          } @else {
                            <span class="amount-muted">—</span>
                          }
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            </div>
          }
        }
      </mat-dialog-content>

      <mat-dialog-actions class="dialog-actions">
        <button mat-button mat-dialog-close type="button" [disabled]="isCopying()">
          Cancel
        </button>
        <button
          mat-flat-button
          color="primary"
          type="button"
          [disabled]="!canCopy()"
          (click)="executeCopy()"
        >
          @if (isCopying()) {
            <mat-spinner diameter="16" />
          } @else {
            <mat-icon>copy_all</mat-icon>
          }
          Copy Roster ({{ selectedWorkerIds().size }})
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
        max-width: 680px;
      }

      .dialog-header {
        display: flex;
        align-items: center;
        gap: 16px;
        padding: 20px 24px 14px;
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
        padding: 18px 24px;
        max-height: 72vh;
        overflow-y: auto;
      }

      .info-text {
        font-size: 0.88rem;
        line-height: 1.45;
        color: var(--mat-sys-on-surface-variant, #555);
        margin: 0 0 14px;
      }

      .date-row {
        display: flex;
        align-items: center;
        gap: 12px;

        .date-field {
          flex: 1;
        }

        .check-btn {
          height: 48px;
          margin-bottom: 22px;
        }
      }

      .loading-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 12px;
        padding: 28px 16px;
        color: #666;
        font-size: 0.88rem;
      }

      .overview-card {
        padding: 14px 16px;
        border-radius: 8px;
        background: #f1f8f3;
        border: 1px solid #c8e6c9;
        margin-bottom: 14px;

        &.overview-empty {
          background: #fff8e1;
          border-color: #ffe082;
        }

        .overview-header {
          display: flex;
          align-items: center;
          gap: 10px;
          color: #1b7a36;

          .overview-title {
            display: flex;
            align-items: center;
            gap: 10px;
            flex-wrap: wrap;

            strong {
              font-size: 0.95rem;
            }

            .eligible-badge {
              padding: 2px 8px;
              background: #2e7d32;
              color: #ffffff;
              border-radius: 12px;
              font-size: 0.75rem;
              font-weight: 500;
            }
          }
        }

        .breakdown-row {
          display: flex;
          gap: 8px;
          flex-wrap: wrap;
          margin-top: 8px;

          .chip {
            padding: 2px 8px;
            background: #ffffff;
            border-radius: 12px;
            font-size: 0.75rem;
            font-weight: 500;
            border: 1px solid rgba(0, 0, 0, 0.08);
          }
        }
      }

      .warning-box {
        padding: 12px 16px;
        border-radius: 8px;
        margin-bottom: 14px;

        .warning-header {
          display: flex;
          align-items: center;
          gap: 8px;
          margin-bottom: 6px;
          font-size: 0.9rem;
        }

        &.excluded-box {
          background: #fff3e0;
          border: 1px solid #ffe0b2;
          color: #bf360c;

          .excluded-list {
            margin: 6px 0 0 0;
            padding-left: 20px;
            font-size: 0.84rem;

            li {
              margin-bottom: 4px;

              .worker-name {
                font-weight: 600;
                margin-right: 6px;
              }

              .exclusion-reason {
                color: #d84315;
                font-style: italic;
              }
            }
          }
        }

        &.overwrite-box {
          background: #fbe9e7;
          border: 1px solid #ffccbc;
          color: #c62828;

          .overwrite-text {
            font-size: 0.85rem;
            margin: 0 0 10px;
            line-height: 1.4;
          }
        }
      }

      .selection-section {
        margin-top: 14px;
        border: 1px solid var(--mat-sys-outline-variant, #e0e0e0);
        border-radius: 8px;
        overflow: hidden;

        .selection-header {
          padding: 8px 12px;
          background: #fafafa;
          border-bottom: 1px solid #e0e0e0;
        }

        .workers-table-container {
          max-height: 240px;
          overflow-y: auto;
        }

        .workers-table {
          width: 100%;
          border-collapse: collapse;
          font-size: 0.85rem;

          th,
          td {
            padding: 8px 12px;
            text-align: left;
            border-bottom: 1px solid #f0f0f0;
          }

          th {
            font-size: 0.75rem;
            text-transform: uppercase;
            color: #666;
            background: #fafafa;
            position: sticky;
            top: 0;
            z-index: 1;
          }

          tbody tr:hover {
            background: #f9f9f9;
          }

          tbody tr.row-selected {
            background: rgba(30, 142, 62, 0.04);
          }

          .worker-col {
            display: flex;
            flex-direction: column;

            .worker-name {
              font-weight: 500;
            }

            .worker-sub {
              font-size: 0.75rem;
              color: #777;
            }
          }

          .cat-pill {
            padding: 2px 6px;
            background: #f0f0f0;
            border-radius: 4px;
            font-size: 0.75rem;
          }

          .type-pill {
            padding: 2px 6px;
            border-radius: 4px;
            font-size: 0.75rem;
            font-weight: 500;

            &.type-full_day {
              background: #e8f5e9;
              color: #2e7d32;
            }

            &.type-half_day {
              background: #e3f2fd;
              color: #1565c0;
            }

            &.type-hourly {
              background: #fff8e1;
              color: #f57f17;
            }

            &.type-not_worked {
              background: #f5f5f5;
              color: #757575;
            }
          }

          .amount-text {
            font-weight: 600;
            color: #2e7d32;
          }

          .amount-muted {
            color: #999;
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
export class AttendanceCopyPreviousDayDialogComponent implements OnInit {
  readonly data = inject<AttendanceCopyPreviousDayDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(
    MatDialogRef<AttendanceCopyPreviousDayDialogComponent>,
  );
  private readonly attendanceService = inject(AttendanceService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly isChecking = signal(false);
  readonly isCopying = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly preview = signal<CopyPreviousDayPreviewResponse | null>(null);

  readonly selectedWorkerIds = signal<Set<string>>(new Set<string>());
  readonly confirmOverwrite = signal(false);

  readonly form = this.formBuilder.group({
    sourceDate: [null as Date | null],
  });

  readonly eligibleWorkers = computed<CopyPreviousDayPreviewWorkerItem[]>(() => {
    return this.preview()?.workers?.filter((w) => w.isEligible) ?? [];
  });

  readonly needsOverwriteConfirmation = computed<boolean>(() => {
    const p = this.preview();
    return (
      this.data.currentDraftCount > 0 ||
      this.data.hasUnsavedChanges ||
      (p?.targetHasExistingRecords ?? false)
    );
  });

  readonly isAllSelected = computed<boolean>(() => {
    const list = this.eligibleWorkers();
    return list.length > 0 && this.selectedWorkerIds().size === list.length;
  });

  readonly isIndeterminate = computed<boolean>(() => {
    const count = this.selectedWorkerIds().size;
    const total = this.eligibleWorkers().length;
    return count > 0 && count < total;
  });

  readonly canCopy = computed<boolean>(() => {
    if (this.isChecking() || this.isCopying()) return false;
    if (this.selectedWorkerIds().size === 0) return false;
    if (this.needsOverwriteConfirmation() && !this.confirmOverwrite()) {
      return false;
    }
    return true;
  });

  ngOnInit(): void {
    // Initial check without specifying sourceDate to let backend auto-find previous roster
    this.checkPreview();
  }

  formattedSourceDate(): string | null {
    const d = this.form.controls.sourceDate.value;
    return d ? formatDateOnly(d) : null;
  }

  checkPreview(): void {
    this.isChecking.set(true);
    this.errorMessage.set(null);
    const dateStr = this.formattedSourceDate();

    this.attendanceService
      .previewCopyPreviousDay(this.data.farmId, this.data.targetDate, dateStr)
      .pipe(
        finalize(() => this.isChecking.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (res) => {
          this.preview.set(res);

          // Update sourceDate control if auto-detected
          if (res.sourceDate) {
            const parsed = parseDateOnly(res.sourceDate);
            if (parsed) {
              this.form.controls.sourceDate.setValue(parsed, { emitEvent: false });
            }
          }

          // Select all eligible workers by default
          const eligibleIds = new Set(
            res.workers.filter((w) => w.isEligible).map((w) => w.workerId),
          );
          this.selectedWorkerIds.set(eligibleIds);
        },
        error: (err) => {
          this.errorMessage.set(
            getApiErrorMessage(err, "Failed to load prior attendance roster preview."),
          );
        },
      });
  }

  isWorkerSelected(workerId: string): boolean {
    return this.selectedWorkerIds().has(workerId);
  }

  toggleWorkerSelection(workerId: string, checked: boolean): void {
    const current = new Set(this.selectedWorkerIds());
    if (checked) {
      current.add(workerId);
    } else {
      current.delete(workerId);
    }
    this.selectedWorkerIds.set(current);
  }

  toggleSelectAll(checked: boolean): void {
    if (checked) {
      const allEligible = new Set(this.eligibleWorkers().map((w) => w.workerId));
      this.selectedWorkerIds.set(allEligible);
    } else {
      this.selectedWorkerIds.set(new Set());
    }
  }

  executeCopy(): void {
    const p = this.preview();
    if (!p) return;

    this.isCopying.set(true);
    this.errorMessage.set(null);

    const request = {
      farmId: this.data.farmId,
      targetDate: this.data.targetDate,
      sourceDate: p.sourceDate,
      workerIds: Array.from(this.selectedWorkerIds()),
      overwriteExistingDraft: true,
    };

    this.attendanceService
      .copyPreviousDay(request)
      .pipe(
        finalize(() => this.isCopying.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response: CopyPreviousDayAttendanceResponse) => {
          this.dialogRef.close(response);
        },
        error: (err) => {
          this.errorMessage.set(
            getApiErrorMessage(err, "Failed to copy previous day's attendance roster."),
          );
        },
      });
  }

  formatType(type: string): string {
    switch (type?.toUpperCase()) {
      case "FULL_DAY":
        return "Full Day";
      case "HALF_DAY":
        return "Half Day";
      case "HOURLY":
        return "Hourly";
      case "NOT_WORKED":
        return "Not Worked";
      default:
        return type || "—";
    }
  }
}
