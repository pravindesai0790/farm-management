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
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
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
import { MatSnackBar } from "@angular/material/snack-bar";
import { finalize, forkJoin } from "rxjs";

import { Unit } from "../../../../core/farm-management/farm-management.models";
import { FarmManagementService } from "../../../../core/farm-management/farm-management.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import {
  CompleteIrrigationRequest,
  IrrigationDetailsResponse,
  IrrigationMethodDto,
} from "../../../../core/irrigations/irrigation.models";
import { IrrigationService } from "../../../../core/irrigations/irrigation.service";
import { DateTimePickerComponent } from "../../../../shared/components/date-time-picker/date-time-picker.component";

export interface CompleteIrrigationDialogData {
  readonly irrigation: IrrigationDetailsResponse;
}

export const completeWaterAndTimingValidator: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null => {
  const errors: ValidationErrors = {};

  const qty = control.get("actualWaterQuantity")?.value;
  const unit = control.get("actualWaterUnitId")?.value;
  const hasQty = qty !== null && qty !== undefined && qty !== "" && !isNaN(Number(qty)) && Number(qty) > 0;
  const hasUnit = !!unit;

  if (hasQty && !hasUnit) {
    errors["waterUnitRequired"] = true;
  }
  if (!hasQty && hasUnit) {
    errors["waterQuantityRequired"] = true;
  }

  const startVal = control.get("actualStartedAt")?.value;
  const endVal = control.get("actualEndedAt")?.value;
  if (startVal && endVal) {
    const start = new Date(startVal).getTime();
    const end = new Date(endVal).getTime();
    if (!isNaN(start) && !isNaN(end) && end < start) {
      errors["endBeforeStart"] = true;
    }
  }

  return Object.keys(errors).length > 0 ? errors : null;
};

@Component({
  selector: "app-complete-irrigation-dialog",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule,
    MatProgressSpinnerModule,
    DateTimePickerComponent,
  ],
  templateUrl: "./complete-irrigation-dialog.component.html",
  styleUrl: "./complete-irrigation-dialog.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CompleteIrrigationDialogComponent implements OnInit {
  readonly data: CompleteIrrigationDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<CompleteIrrigationDialogComponent>);
  private readonly irrigationService = inject(IrrigationService);
  private readonly farmService = inject(FarmManagementService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  readonly isLoading = signal(false);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly methods = signal<readonly IrrigationMethodDto[]>([]);
  readonly volumeUnits = signal<readonly Unit[]>([]);

  readonly form = this.fb.group(
    {
      irrigationMethodId: [
        this.data.irrigation.irrigationMethodId || "",
        [Validators.required],
      ],
      actualStartedAt: [
        this.data.irrigation.actualStartedAt || null,
      ],
      actualEndedAt: [
        this.data.irrigation.actualEndedAt || null,
      ],
      actualDurationMinutes: [
        this.data.irrigation.actualDurationMinutes || null,
        [Validators.min(1)],
      ],
      actualWaterQuantity: [
        this.data.irrigation.actualWaterQuantity ?? this.data.irrigation.plannedWaterQuantity,
        [Validators.min(0.01)],
      ],
      actualWaterUnitId: [
        this.data.irrigation.actualWaterUnitId ?? this.data.irrigation.plannedWaterUnitId ?? "",
      ],
      notes: [
        this.data.irrigation.notes || "",
        [Validators.maxLength(1000)],
      ],
    },
    { validators: [completeWaterAndTimingValidator] },
  );

  ngOnInit(): void {
    this.loadLookups();
  }

  private loadLookups(): void {
    this.isLoading.set(true);
    forkJoin({
      methods: this.irrigationService.getMethods(true),
      volumeUnits: this.farmService.listUnits("Volume"),
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (res) => {
          this.methods.set(res.methods);
          this.volumeUnits.set(res.volumeUnits);

          // If no method selected yet and exactly one method available, select it
          if (!this.form.value.irrigationMethodId && res.methods.length === 1) {
            this.form.patchValue({ irrigationMethodId: res.methods[0].id });
          }
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to load irrigation methods and units.");
          this.errorMessage.set(msg);
        },
      });
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onSubmit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const val = this.form.value;
    const startedAt = val.actualStartedAt ? new Date(val.actualStartedAt).toISOString() : null;
    const endedAt = val.actualEndedAt ? new Date(val.actualEndedAt).toISOString() : null;

    const payload: CompleteIrrigationRequest = {
      irrigationMethodId: val.irrigationMethodId!,
      actualStartedAt: startedAt,
      actualEndedAt: endedAt,
      actualDurationMinutes: val.actualDurationMinutes !== null && val.actualDurationMinutes !== undefined ? Number(val.actualDurationMinutes) : null,
      actualWaterQuantity: val.actualWaterQuantity !== null && val.actualWaterQuantity !== undefined ? Number(val.actualWaterQuantity) : null,
      actualWaterUnitId: val.actualWaterUnitId || null,
      notes: val.notes?.trim() || null,
      concurrencyToken: this.data.irrigation.concurrencyToken || null,
    };

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.irrigationService
      .complete(this.data.irrigation.id, payload)
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.snack.open("Irrigation event completed successfully.", "OK", {
            duration: 3500,
          });
          this.dialogRef.close(true);
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to complete irrigation event.");
          this.errorMessage.set(msg);
          this.snack.open(msg, "Dismiss", { duration: 5000 });
        },
      });
  }
}
