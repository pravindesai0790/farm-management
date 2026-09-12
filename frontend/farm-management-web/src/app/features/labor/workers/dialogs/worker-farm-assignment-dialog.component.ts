import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
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
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { finalize } from "rxjs";

import { FarmManagementService } from "../../../../core/farm-management/farm-management.service";
import { Farm } from "../../../../core/farm-management/farm-management.models";
import {
  CreateWorkerFarmAssignmentRequest,
  UpdateWorkerFarmAssignmentRequest,
  WorkerFarmAssignment,
} from "../../../../core/labor/labor.models";
import { LaborService } from "../../../../core/labor/labor.service";
import {
  getApiErrorMessage,
  getApiValidationErrors,
} from "../../../../core/models/api-error.model";
import { formatDateOnly, parseDateOnly } from "../../../../core/utils/date.utils";
import { ErrorAlertComponent } from "../../../../shared/components/error-alert/error-alert.component";

export interface WorkerFarmAssignmentDialogData {
  readonly workerId: string;
  readonly assignment?: WorkerFarmAssignment;
}

function assignedToAfterAssignedFrom(
  control: AbstractControl,
): ValidationErrors | null {
  const from = control.get("assignedFrom")?.value as Date | string | null;
  const to = control.get("assignedTo")?.value as Date | string | null;

  if (from && to) {
    const fromDate = parseDateOnly(from);
    const toDate = parseDateOnly(to);
    if (fromDate && toDate && toDate < fromDate) {
      return { assignedToBeforeAssignedFrom: true };
    }
  }
  return null;
}

@Component({
  selector: "app-worker-farm-assignment-dialog",
  standalone: true,
  imports: [
    ErrorAlertComponent,
    MatButtonModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  template: `
    <h2 mat-dialog-title>
      <mat-icon class="title-icon">agriculture</mat-icon>
      {{ isEditing ? "Edit Farm Assignment" : "Assign Farm to Worker" }}
    </h2>

    <mat-dialog-content>
      @if (isLoadingFarms()) {
        <div class="dialog-loading">
          <mat-spinner diameter="32" />
          <span>Loading available farms…</span>
        </div>
      } @else {
        <form [formGroup]="form" (ngSubmit)="submit()" class="assignment-form">
          <!-- Farm selection -->
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Farm</mat-label>
            @if (isEditing) {
              <input matInput [value]="data.assignment?.farmName + ' (' + data.assignment?.farmCode + ')'" disabled />
            } @else {
              <mat-select formControlName="farmId">
                @for (farm of farms(); track farm.id) {
                  <mat-option [value]="farm.id">
                    {{ farm.name }} ({{ farm.code }})
                  </mat-option>
                }
              </mat-select>
              @if (apiError("farmId")) {
                <mat-error>{{ apiError("farmId") }}</mat-error>
              } @else if (form.controls.farmId.hasError("required")) {
                <mat-error>A farm is required.</mat-error>
              }
            }
          </mat-form-field>

          <div class="dates-row">
            <!-- Assigned From -->
            <mat-form-field appearance="outline" class="half-width">
              <mat-label>Assigned from</mat-label>
              <input
                matInput
                [matDatepicker]="fromPicker"
                formControlName="assignedFrom"
                placeholder="Start date"
              />
              <mat-datepicker-toggle matIconSuffix [for]="fromPicker" />
              <mat-datepicker #fromPicker />
              @if (apiError("assignedFrom")) {
                <mat-error>{{ apiError("assignedFrom") }}</mat-error>
              } @else if (form.controls.assignedFrom.hasError("required")) {
                <mat-error>Start date is required.</mat-error>
              }
            </mat-form-field>

            <!-- Assigned To -->
            <mat-form-field appearance="outline" class="half-width">
              <mat-label>Assigned to (optional)</mat-label>
              <input
                matInput
                [matDatepicker]="toPicker"
                [min]="form.controls.assignedFrom.value"
                formControlName="assignedTo"
                placeholder="End date or blank for ongoing"
              />
              <mat-datepicker-toggle matIconSuffix [for]="toPicker" />
              <mat-datepicker #toPicker />
              @if (apiError("assignedTo")) {
                <mat-error>{{ apiError("assignedTo") }}</mat-error>
              } @else if (form.hasError("assignedToBeforeAssignedFrom")) {
                <mat-error>End date cannot precede start date.</mat-error>
              }
            </mat-form-field>
          </div>

          <!-- Notes -->
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Notes</mat-label>
            <textarea
              matInput
              rows="3"
              formControlName="notes"
              placeholder="e.g. Seasonal harvesting shift, specific sector responsibility"
            ></textarea>
            @if (apiError("notes")) {
              <mat-error>{{ apiError("notes") }}</mat-error>
            }
          </mat-form-field>

          @if (errorMessage()) {
            <app-error-alert [error]="errorMessage()" fallback="Assignment could not be saved." />
          }
        </form>
      }
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close [disabled]="isSubmitting()">Cancel</button>
      <button
        mat-flat-button
        color="primary"
        (click)="submit()"
        [disabled]="isSubmitting() || isLoadingFarms()"
      >
        @if (isSubmitting()) {
          <mat-spinner diameter="18" class="btn-spinner" />
        }
        {{ isEditing ? "Save changes" : "Assign farm" }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .title-icon {
      vertical-align: middle;
      margin-right: 8px;
      color: var(--mat-sys-primary, #2e7d32);
    }
    .dialog-loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 12px;
      padding: 32px 16px;
      color: #616161;
    }
    .assignment-form {
      display: flex;
      flex-direction: column;
      gap: 8px;
      padding-top: 8px;
      min-width: 440px;
    }
    .full-width {
      width: 100%;
    }
    .dates-row {
      display: flex;
      gap: 16px;
    }
    .half-width {
      flex: 1;
    }
    .btn-spinner {
      display: inline-block;
      margin-right: 6px;
    }
    @media (max-width: 520px) {
      .assignment-form {
        min-width: 100%;
      }
      .dates-row {
        flex-direction: column;
        gap: 0;
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkerFarmAssignmentDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly laborService = inject(LaborService);
  private readonly farmService = inject(FarmManagementService);
  private readonly dialogRef = inject(MatDialogRef<WorkerFarmAssignmentDialogComponent>);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly data = inject<WorkerFarmAssignmentDialogData>(MAT_DIALOG_DATA);

  readonly isEditing = !!this.data.assignment;
  readonly isLoadingFarms = signal(true);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<unknown>(null);
  readonly apiErrors = signal<Readonly<Record<string, readonly string[]>>>({});
  readonly farms = signal<readonly Farm[]>([]);

  readonly form = this.fb.group(
    {
      farmId: [
        this.data.assignment?.farmId ?? "",
        this.isEditing ? [] : [Validators.required],
      ],
      assignedFrom: [
        this.data.assignment
          ? parseDateOnly(this.data.assignment.assignedFrom)
          : new Date(),
        [Validators.required],
      ],
      assignedTo: [
        this.data.assignment?.assignedTo
          ? parseDateOnly(this.data.assignment.assignedTo)
          : null,
      ],
      notes: [this.data.assignment?.notes ?? ""],
    },
    { validators: assignedToAfterAssignedFrom },
  );

  ngOnInit(): void {
    if (!this.isEditing) {
      this.farmService
        .listFarms(1, 100, "", true)
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isLoadingFarms.set(false)),
        )
        .subscribe({
          next: (res) => this.farms.set(res.items),
          error: (e) => this.errorMessage.set(e),
        });
    } else {
      this.isLoadingFarms.set(false);
    }
  }

  apiError(field: string): string | null {
    return this.apiErrors()[field]?.[0] ?? null;
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.apiErrors.set({});

    const raw = this.form.getRawValue();

    if (this.isEditing && this.data.assignment) {
      const updateReq: UpdateWorkerFarmAssignmentRequest = {
        assignedFrom: formatDateOnly(raw.assignedFrom)!,
        assignedTo: formatDateOnly(raw.assignedTo),
        notes: raw.notes?.trim() || null,
      };

      this.laborService
        .updateWorkerFarmAssignment(
          this.data.workerId,
          this.data.assignment.id,
          updateReq,
        )
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isSubmitting.set(false)),
        )
        .subscribe({
          next: (res) => {
            this.snack.open("Farm assignment updated.", "Dismiss", {
              duration: 3000,
            });
            this.dialogRef.close(res);
          },
          error: (err) => {
            this.errorMessage.set(err);
            this.apiErrors.set(getApiValidationErrors(err));
          },
        });
    } else {
      const createReq: CreateWorkerFarmAssignmentRequest = {
        farmId: raw.farmId!,
        assignedFrom: formatDateOnly(raw.assignedFrom)!,
        assignedTo: formatDateOnly(raw.assignedTo),
        notes: raw.notes?.trim() || null,
      };

      this.laborService
        .createWorkerFarmAssignment(this.data.workerId, createReq)
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isSubmitting.set(false)),
        )
        .subscribe({
          next: (res) => {
            this.snack.open("Farm assigned to worker.", "Dismiss", {
              duration: 3000,
            });
            this.dialogRef.close(res);
          },
          error: (err) => {
            this.errorMessage.set(err);
            this.apiErrors.set(getApiValidationErrors(err));
          },
        });
    }
  }
}
