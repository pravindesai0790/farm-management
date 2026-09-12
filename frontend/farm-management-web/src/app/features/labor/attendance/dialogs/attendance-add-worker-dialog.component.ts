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
import { finalize, forkJoin } from "rxjs";

import {
  ATTENDANCE_TYPE_OPTIONS,
  AttendanceEligibleWorker,
  AttendanceType,
} from "../../../../core/labor/attendance.models";
import { AttendanceService } from "../../../../core/labor/attendance.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import { ErrorAlertComponent } from "../../../../shared/components/error-alert/error-alert.component";

export interface AttendanceAddWorkerDialogData {
  readonly farmId: string;
  readonly farmName: string;
  readonly attendanceDate: string;
  readonly alreadyAddedWorkerIds: readonly string[];
}

export interface AttendanceAddWorkerResult {
  readonly worker: AttendanceEligibleWorker;
  readonly attendanceType: AttendanceType;
  readonly workingHours?: number | null;
  readonly notes?: string | null;
}

@Component({
  selector: "app-attendance-add-worker-dialog",
  standalone: true,
  imports: [
    CommonModule,
    ErrorAlertComponent,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  template: `
    <div class="dialog-container">
      <div class="dialog-header">
        <div class="header-icon">
          <mat-icon>person_add</mat-icon>
        </div>
        <div class="header-title">
          <h2 mat-dialog-title>Add Worker to Attendance</h2>
          <p class="subtitle">
            {{ data.farmName }} &bull; {{ data.attendanceDate }}
          </p>
        </div>
      </div>

      <mat-dialog-content class="dialog-content">
        @if (errorMessage()) {
          <app-error-alert [error]="errorMessage()!" />
        }

        @if (isLoading()) {
          <div class="loading-state">
            <mat-spinner diameter="32" />
            <span>Loading assigned workers…</span>
          </div>
        } @else {
          <form [formGroup]="form" class="form-layout">
            <!-- Worker Selector -->
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Select Worker</mat-label>
              <mat-select formControlName="workerId" placeholder="Choose worker">
                @for (worker of availableWorkers(); track worker.workerId) {
                  <mat-option [value]="worker.workerId">
                    {{ worker.displayName }}
                    @if (worker.laborCategoryName) {
                      ({{ worker.laborCategoryName }})
                    }
                    @if (worker.mobileNumber) {
                      &bull; {{ worker.mobileNumber }}
                    }
                  </mat-option>
                }
              </mat-select>
              @if (availableWorkers().length === 0) {
                <mat-hint>All assigned eligible workers are already added.</mat-hint>
              }
              @if (form.controls.workerId.touched && form.controls.workerId.invalid) {
                <mat-error>Please select a worker.</mat-error>
              }
            </mat-form-field>

            <!-- Selected Worker Card -->
            @if (selectedWorker(); as w) {
              <div class="worker-summary-card">
                <div class="summary-row">
                  <span class="label">Name:</span>
                  <strong>{{ w.displayName }}</strong>
                </div>
                <div class="summary-row">
                  <span class="label">Gender:</span>
                  <span>{{ w.gender }}</span>
                  <span class="dot">&bull;</span>
                  <span class="label">Category:</span>
                  <span>{{ w.laborCategoryName || "General" }}</span>
                </div>
                @if (w.contractorName) {
                  <div class="summary-row">
                    <span class="label">Contractor:</span>
                    <span>{{ w.contractorName }}</span>
                  </div>
                }
              </div>
            }

            <!-- Attendance Type -->
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Attendance Type</mat-label>
              <mat-select formControlName="attendanceType">
                @for (opt of attendanceTypeOptions; track opt.value) {
                  <mat-option [value]="opt.value">
                    {{ opt.label }} — {{ opt.description }}
                  </mat-option>
                }
              </mat-select>
            </mat-form-field>

            <!-- Working Hours (Conditional on HOURLY) -->
            @if (form.controls.attendanceType.value === 'HOURLY') {
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Working Hours</mat-label>
                <input
                  matInput
                  type="number"
                  step="0.25"
                  min="0.25"
                  max="24"
                  formControlName="workingHours"
                  placeholder="e.g. 6.0"
                />
                <span matSuffix>&nbsp;hrs&nbsp;</span>
                @if (
                  form.controls.workingHours.touched &&
                  form.controls.workingHours.invalid
                ) {
                  <mat-error>Hours must be between 0.25 and 24.</mat-error>
                }
              </mat-form-field>
            }

            <!-- Notes -->
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Notes (optional)</mat-label>
              <input
                matInput
                formControlName="notes"
                placeholder="Optional shift or task details"
              />
            </mat-form-field>
          </form>
        }
      </mat-dialog-content>

      <mat-dialog-actions class="dialog-actions">
        <button mat-button mat-dialog-close type="button">Cancel</button>
        <button
          mat-flat-button
          color="primary"
          type="button"
          [disabled]="form.invalid || !selectedWorker()"
          (click)="submit()"
        >
          <mat-icon>check</mat-icon>
          Add to Attendance
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

      .form-layout {
        display: flex;
        flex-direction: column;
        gap: 12px;
      }

      .full-width {
        width: 100%;
      }

      .worker-summary-card {
        padding: 12px 16px;
        background: #f8f9fa;
        border-radius: 8px;
        border: 1px solid #e9ecef;
        font-size: 0.85rem;
        display: flex;
        flex-direction: column;
        gap: 6px;

        .summary-row {
          display: flex;
          align-items: center;
          gap: 6px;

          .label {
            color: #6c757d;
          }

          .dot {
            color: #adb5bd;
          }
        }
      }

      .loading-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        padding: 36px 16px;
        gap: 12px;
        color: #666;
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
export class AttendanceAddWorkerDialogComponent implements OnInit {
  readonly data = inject<AttendanceAddWorkerDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<AttendanceAddWorkerDialogComponent>);
  private readonly attendanceService = inject(AttendanceService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly attendanceTypeOptions = ATTENDANCE_TYPE_OPTIONS;
  readonly allEligibleWorkers = signal<readonly AttendanceEligibleWorker[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly alreadyAddedSet = computed(
    () => new Set(this.data.alreadyAddedWorkerIds || []),
  );

  readonly availableWorkers = computed(() => {
    return this.allEligibleWorkers().filter(
      (w) => !this.alreadyAddedSet().has(w.workerId),
    );
  });

  readonly form = this.formBuilder.nonNullable.group({
    workerId: ["", Validators.required],
    attendanceType: ["FULL_DAY" as AttendanceType, Validators.required],
    workingHours: [null as number | null],
    notes: [""],
  });

  readonly selectedWorker = computed(() => {
    const id = this.form.controls.workerId.value;
    if (!id) return null;
    return this.allEligibleWorkers().find((w) => w.workerId === id) || null;
  });

  ngOnInit(): void {
    this.loadWorkers();

    // Update validators on attendanceType change
    this.form.controls.attendanceType.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((type) => {
        if (type === "HOURLY") {
          this.form.controls.workingHours.setValidators([
            Validators.required,
            Validators.min(0.25),
            Validators.max(24),
          ]);
          if (!this.form.controls.workingHours.value) {
            this.form.controls.workingHours.setValue(8);
          }
        } else {
          this.form.controls.workingHours.clearValidators();
          this.form.controls.workingHours.setValue(null);
        }
        this.form.controls.workingHours.updateValueAndValidity();
      });
  }

  loadWorkers(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.attendanceService
      .getEligibleWorkers(this.data.farmId, this.data.attendanceDate, null, 1, 100)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          const allItems = [...(res.items || [])];
          if (res.totalCount > 100) {
            const totalPages = Math.ceil(res.totalCount / 100);
            const remainingRequests = [];
            for (let p = 2; p <= totalPages; p++) {
              remainingRequests.push(
                this.attendanceService.getEligibleWorkers(
                  this.data.farmId,
                  this.data.attendanceDate,
                  null,
                  p,
                  100,
                ),
              );
            }
            forkJoin(remainingRequests)
              .pipe(
                finalize(() => this.isLoading.set(false)),
                takeUntilDestroyed(this.destroyRef),
              )
              .subscribe({
                next: (pages) => {
                  for (const pageRes of pages) {
                    allItems.push(...(pageRes.items || []));
                  }
                  this.allEligibleWorkers.set(allItems);
                },
                error: (err) => {
                  this.allEligibleWorkers.set(allItems);
                  this.errorMessage.set(
                    getApiErrorMessage(err, "Failed to load all eligible workers."),
                  );
                },
              });
          } else {
            this.isLoading.set(false);
            this.allEligibleWorkers.set(allItems);
          }
        },
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(getApiErrorMessage(err, "Failed to load eligible workers."));
        },
      });
  }

  submit(): void {
    if (this.form.invalid) return;

    const worker = this.selectedWorker();
    if (!worker) return;

    const result: AttendanceAddWorkerResult = {
      worker,
      attendanceType: this.form.controls.attendanceType.value,
      workingHours: this.form.controls.workingHours.value,
      notes: this.form.controls.notes.value.trim() || null,
    };

    this.dialogRef.close(result);
  }
}
