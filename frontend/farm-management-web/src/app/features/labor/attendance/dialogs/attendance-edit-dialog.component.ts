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
import { MatSelectModule } from "@angular/material/select";
import { debounceTime, distinctUntilChanged, finalize } from "rxjs";

import {
  ATTENDANCE_TYPE_OPTIONS,
  AttendanceDetail,
  AttendanceRecord,
  AttendanceType,
  AttendanceWagePreviewResponse,
  formatAttendanceType,
} from "../../../../core/labor/attendance.models";
import { AttendanceService } from "../../../../core/labor/attendance.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import { ErrorAlertComponent } from "../../../../shared/components/error-alert/error-alert.component";

export interface AttendanceEditDialogData {
  readonly record: AttendanceDetail;
}

@Component({
  selector: "app-attendance-edit-dialog",
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
    MatSelectModule,
    ErrorAlertComponent,
  ],
  template: `
    <div class="dialog-container">
      <div class="dialog-header">
        <div class="header-icon">
          <mat-icon>edit_calendar</mat-icon>
        </div>
        <div class="header-title">
          <h2 mat-dialog-title>Edit Draft Attendance</h2>
          <p class="subtitle">
            {{ data.record.workerDisplayName }} &bull; {{ data.record.attendanceDate }}
          </p>
        </div>
      </div>

      <mat-dialog-content class="dialog-content">
        @if (errorMessage()) {
          <app-error-alert
            [error]="errorMessage()!"
            fallback="Failed to update draft attendance."
          />
        }

        <div class="worker-summary-strip">
          <div class="summary-col">
            <span class="col-label">Farm</span>
            <span class="col-val">{{ data.record.farmName }}</span>
          </div>
          <div class="summary-col">
            <span class="col-label">Labor Category</span>
            <span class="col-val">{{ data.record.laborCategoryName || "General" }}</span>
          </div>
          <div class="summary-col">
            <span class="col-label">Employment</span>
            <span class="col-val">{{ data.record.employmentType }}</span>
          </div>
        </div>

        <form [formGroup]="form" class="edit-form">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Attendance Type</mat-label>
            <mat-select formControlName="attendanceType">
              <mat-select-trigger>
                {{ formatType(selectedAttendanceType()) }}
              </mat-select-trigger>
              @for (option of typeOptions; track option.value) {
                <mat-option [value]="option.value">
                  <div class="option-row">
                    <span class="option-label">{{ option.label }}</span>
                    <span class="option-desc">{{ option.description }}</span>
                  </div>
                </mat-option>
              }
            </mat-select>
            @if (form.controls.attendanceType.hasError("required")) {
              <mat-error>Attendance type is required.</mat-error>
            }
          </mat-form-field>

          @if (isHourly()) {
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Working Hours</mat-label>
              <input
                matInput
                type="number"
                step="0.5"
                min="0.5"
                max="24"
                formControlName="workingHours"
                placeholder="e.g. 8"
              />
              <mat-icon matSuffix>schedule</mat-icon>
              @if (form.controls.workingHours.hasError("required")) {
                <mat-error>Working hours are required for hourly attendance.</mat-error>
              } @else if (form.controls.workingHours.hasError("min")) {
                <mat-error>Working hours must be at least 0.5 hours.</mat-error>
              } @else if (form.controls.workingHours.hasError("max")) {
                <mat-error>Working hours cannot exceed 24 hours.</mat-error>
              }
            </mat-form-field>
          }

          <!-- Wage calculation preview -->
          <div class="wage-preview-panel" [class.panel-error]="!!previewError()">
            @if (previewLoading()) {
              <div class="preview-loading">
                <mat-spinner diameter="20"></mat-spinner>
                <span>Calculating wage preview...</span>
              </div>
            } @else if (previewError(); as wageErr) {
              <div class="preview-error">
                <mat-icon class="error-icon">warning_amber</mat-icon>
                <div class="error-text">
                  <strong>Wage Rate Unavailable</strong>
                  <span>{{ wageErr }}</span>
                </div>
              </div>
            } @else if (preview()) {
              <div class="preview-result">
                <div class="preview-item">
                  <span class="prev-label">Resolved Wage Rate:</span>
                  <span class="prev-val">
                    {{ preview()?.currencySymbol || "₹" }}{{ (preview()?.rate ?? 0) | number: "1.2-2" }}
                  </span>
                </div>
                <div class="preview-item highlight">
                  <span class="prev-label">Estimated Gross Earnings:</span>
                  <span class="prev-val amount">
                    {{ preview()?.currencySymbol || "₹" }}{{ (preview()?.calculatedAmount ?? 0) | number: "1.2-2" }}
                  </span>
                </div>
              </div>
            } @else {
              <div class="preview-initial">
                <span class="prev-label">Estimated Gross:</span>
                <span class="prev-val amount">
                  {{ data.record.currencySymbol || "₹" }}{{ (data.record.calculatedAmount ?? 0) | number: "1.2-2" }}
                </span>
              </div>
            }
          </div>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Notes (Optional)</mat-label>
            <textarea
              matInput
              rows="3"
              formControlName="notes"
              maxlength="500"
              placeholder="Add any specific work context or overtime notes..."
            ></textarea>
            <mat-hint align="end">{{ (form.controls.notes.value?.length || 0) }}/500</mat-hint>
          </mat-form-field>
        </form>
      </mat-dialog-content>

      <mat-dialog-actions align="end" class="dialog-actions">
        <button
          mat-button
          type="button"
          [disabled]="submitting()"
          (click)="dialogRef.close()"
        >
          Cancel
        </button>
        <button
          mat-flat-button
          color="primary"
          type="button"
          [disabled]="form.invalid || submitting()"
          (click)="onSave()"
        >
          @if (submitting()) {
            <mat-spinner diameter="18" class="btn-spinner"></mat-spinner>
            <span>Saving...</span>
          } @else {
            <span>Save Changes</span>
          }
        </button>
      </mat-dialog-actions>
    </div>
  `,
  styles: [
    `
      .dialog-container {
        display: flex;
        flex-direction: column;
        max-width: 520px;
        min-width: 420px;
      }

      .dialog-header {
        display: flex;
        align-items: center;
        gap: 1rem;
        padding: 1.25rem 1.5rem 0.75rem;

        .header-icon {
          display: flex;
          align-items: center;
          justify-content: center;
          width: 44px;
          height: 44px;
          border-radius: 12px;
          background: #eff6ff;
          color: #2563eb;

          mat-icon {
            font-size: 24px;
            width: 24px;
            height: 24px;
          }
        }

        .header-title {
          h2 {
            margin: 0;
            font-size: 1.15rem;
            font-weight: 600;
            color: #1e293b;
          }

          .subtitle {
            margin: 0.15rem 0 0;
            font-size: 0.85rem;
            color: #64748b;
          }
        }
      }

      .dialog-content {
        padding: 0.75rem 1.5rem 1rem;
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }

      .worker-summary-strip {
        display: flex;
        align-items: center;
        justify-content: space-between;
        background: #f8fafc;
        border: 1px solid #e2e8f0;
        border-radius: 8px;
        padding: 0.65rem 1rem;

        .summary-col {
          display: flex;
          flex-direction: column;
          gap: 0.15rem;

          .col-label {
            font-size: 0.7rem;
            text-transform: uppercase;
            letter-spacing: 0.05em;
            color: #64748b;
            font-weight: 500;
          }

          .col-val {
            font-size: 0.85rem;
            font-weight: 600;
            color: #1e293b;
          }
        }
      }

      .edit-form {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
      }

      .full-width {
        width: 100%;
      }

      .option-row {
        display: flex;
        justify-content: space-between;
        align-items: center;
        width: 100%;
        gap: 1rem;

        .option-label {
          font-weight: 500;
        }

        .option-desc {
          font-size: 0.75rem;
          color: #94a3b8;
        }
      }

      .wage-preview-panel {
        background: #f0fdf4;
        border: 1px solid #bbf7d0;
        border-radius: 8px;
        padding: 0.75rem 1rem;
        margin-bottom: 0.5rem;

        .preview-loading {
          display: flex;
          align-items: center;
          gap: 0.6rem;
          color: #166534;
          font-size: 0.85rem;
        }

        .preview-result,
        .preview-initial {
          display: flex;
          justify-content: space-between;
          align-items: center;
          flex-wrap: wrap;
          gap: 0.75rem;

          .preview-item {
            display: flex;
            align-items: center;
            gap: 0.4rem;

            .prev-label {
              font-size: 0.8rem;
              color: #166534;
            }

            .prev-val {
              font-size: 0.9rem;
              font-weight: 600;
              color: #14532d;

              &.amount {
                font-size: 1.05rem;
                color: #15803d;
              }
            }
          }
        }
      }

      .dialog-actions {
        padding: 0.75rem 1.5rem 1.25rem;
        gap: 0.5rem;

        .btn-spinner {
          display: inline-block;
          margin-right: 0.4rem;
        }
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AttendanceEditDialogComponent implements OnInit {
  readonly data = inject<AttendanceEditDialogData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<AttendanceEditDialogComponent>);
  private readonly fb = inject(FormBuilder);
  private readonly attendanceService = inject(AttendanceService);
  private readonly destroyRef = inject(DestroyRef);

  readonly typeOptions = ATTENDANCE_TYPE_OPTIONS;

  readonly errorMessage = signal<string | null>(null);
  readonly previewError = signal<string | null>(null);
  readonly submitting = signal<boolean>(false);
  readonly previewLoading = signal<boolean>(false);
  readonly preview = signal<AttendanceWagePreviewResponse | null>(null);

  readonly selectedAttendanceType = signal<AttendanceType>(
    (this.data.record.attendanceType as AttendanceType) || "FULL_DAY",
  );

  readonly form = this.fb.nonNullable.group({
    attendanceType: this.fb.nonNullable.control<AttendanceType>(
      (this.data.record.attendanceType as AttendanceType) || "FULL_DAY",
      [Validators.required],
    ),
    workingHours: this.fb.control<number | null>(
      this.data.record.workingHours ?? null,
    ),
    notes: this.fb.control<string>(this.data.record.notes ?? "", [
      Validators.maxLength(500),
    ]),
  });

  readonly isHourly = computed(() => {
    return this.selectedAttendanceType() === "HOURLY";
  });

  ngOnInit(): void {
    const initialType = (this.data.record.attendanceType as AttendanceType) || "FULL_DAY";
    this.selectedAttendanceType.set(initialType);
    this.updateHoursValidation(initialType);

    this.form.controls.attendanceType.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((type) => {
        this.selectedAttendanceType.set(type);
        this.updateHoursValidation(type);
        this.triggerWagePreview();
      });

    this.form.controls.workingHours.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        this.triggerWagePreview();
      });
  }

  private updateHoursValidation(type: AttendanceType): void {
    const hoursCtrl = this.form.controls.workingHours;
    if (type === "HOURLY") {
      hoursCtrl.setValidators([
        Validators.required,
        Validators.min(0.5),
        Validators.max(24),
      ]);
      if (!hoursCtrl.value) {
        hoursCtrl.setValue(8);
      }
    } else {
      hoursCtrl.clearValidators();
      hoursCtrl.setValue(null);
    }
    hoursCtrl.updateValueAndValidity();
  }

  private triggerWagePreview(): void {
    const type = this.form.controls.attendanceType.value;
    const hours = this.form.controls.workingHours.value;

    if (type === "NOT_WORKED") {
      this.previewError.set(null);
      this.preview.set(null);
      return;
    }

    if (type === "HOURLY" && (!hours || hours < 0.5 || hours > 24)) {
      return;
    }

    this.previewLoading.set(true);
    this.previewError.set(null);

    this.attendanceService
      .previewWage({
        workerId: this.data.record.workerId,
        attendanceDate: this.data.record.attendanceDate,
        attendanceType: type,
        workingHours: type === "HOURLY" ? hours : null,
        farmId: this.data.record.farmId,
      })
      .pipe(
        finalize(() => this.previewLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (res) => {
          if (res.isEarningEligible === false || res.rate <= 0) {
            this.preview.set(null);
            this.previewError.set(
              res.ineligibilityReason ||
                `No active wage rate entry found for gender '${res.gender}' and wage type '${formatAttendanceType(type)}' on date ${this.data.record.attendanceDate}.`,
            );
          } else {
            this.preview.set(res);
            this.previewError.set(null);
          }
        },
        error: (err) => {
          this.preview.set(null);
          const msg = getApiErrorMessage(
            err,
            `No active wage rate entry configured for attendance type '${formatAttendanceType(type)}' on date ${this.data.record.attendanceDate}.`,
          );
          this.previewError.set(msg);
        },
      });
  }

  onSave(): void {
    if (this.form.invalid || this.submitting()) {
      return;
    }

    const type = this.form.controls.attendanceType.value;
    if (type !== "NOT_WORKED" && this.previewError()) {
      this.errorMessage.set(this.previewError());
      return;
    }

    const hours = this.form.controls.workingHours.value;
    const notes = this.form.controls.notes.value?.trim() || null;

    this.submitting.set(true);
    this.errorMessage.set(null);

    this.attendanceService
      .updateDraft(this.data.record.id, {
        attendanceType: type,
        workingHours: type === "HOURLY" ? hours : null,
        notes,
      })
      .pipe(
        finalize(() => this.submitting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (updatedRecord: AttendanceRecord) => {
          this.dialogRef.close(updatedRecord);
        },
        error: (err) => {
          this.errorMessage.set(getApiErrorMessage(err, "Failed to update draft attendance."));
        },
      });
  }

  formatType(type?: string | null): string {
    return formatAttendanceType(type);
  }
}
