import { CommonModule } from "@angular/common";
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
import { MatCardModule } from "@angular/material/card";
import { MatDialog } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTooltipModule } from "@angular/material/tooltip";
import { Router, RouterModule } from "@angular/router";
import { finalize, forkJoin, merge } from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import {
  CropCycle,
  CropCycleStage,
  CycleList,
  Farm,
  FarmArea,
  Plantation,
  Unit,
} from "../../../core/farm-management/farm-management.models";
import { FarmManagementService } from "../../../core/farm-management/farm-management.service";
import {
  IrrigationMethodDto,
  RecordCompletedIrrigationRequest,
} from "../../../core/irrigations/irrigation.models";
import { IrrigationService } from "../../../core/irrigations/irrigation.service";
import { getApiErrorMessage } from "../../../core/models/api-error.model";
import { ConfirmDialogComponent } from "../../../shared/components/confirm-dialog/confirm-dialog.component";
import { DateTimePickerComponent } from "../../../shared/components/date-time-picker/date-time-picker.component";

export const waterQuantityAndUnitValidator = (
  control: AbstractControl,
): ValidationErrors | null => {
  const quantity = control.get("actualWaterQuantity")?.value;
  const unit = control.get("actualWaterUnitId")?.value;

  const hasQuantity =
    quantity !== null &&
    quantity !== undefined &&
    quantity !== "" &&
    !isNaN(Number(quantity)) &&
    Number(quantity) > 0;
  const hasUnit = !!unit && String(unit).trim().length > 0;

  if (hasQuantity && !hasUnit) {
    return { unitRequired: true };
  }
  if (!hasQuantity && hasUnit) {
    return { quantityRequired: true };
  }
  return null;
};

export const startEndDateTimeValidator = (
  control: AbstractControl,
): ValidationErrors | null => {
  const startVal = control.get("actualStartedAt")?.value;
  const endVal = control.get("actualEndedAt")?.value;

  if (startVal && endVal) {
    const startMs = new Date(startVal).getTime();
    const endMs = new Date(endVal).getTime();
    if (!isNaN(startMs) && !isNaN(endMs) && endMs < startMs) {
      return { endBeforeStart: true };
    }
  }
  return null;
};

@Component({
  selector: "app-irrigation-record-completed-page",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    DateTimePickerComponent,
  ],
  templateUrl: "./irrigation-record-completed-page.component.html",
  styleUrl: "./irrigation-record-completed-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IrrigationRecordCompletedPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly farmService = inject(FarmManagementService);
  private readonly irrigationService = inject(IrrigationService);
  readonly permissionService = inject(PermissionService);

  readonly isLoading = signal(false);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  // Cascading and lookup signals
  readonly farms = signal<readonly Farm[]>([]);
  readonly areas = signal<readonly FarmArea[]>([]);
  readonly plantations = signal<readonly Plantation[]>([]);
  readonly cropCycles = signal<readonly CropCycle[]>([]);
  readonly stages = signal<readonly CropCycleStage[]>([]);
  readonly methods = signal<readonly IrrigationMethodDto[]>([]);
  readonly volumeUnits = signal<readonly Unit[]>([]);

  readonly form = this.fb.group(
    {
      farmId: ["", [Validators.required]],
      farmAreaId: ["", [Validators.required]],
      plantationId: [""],
      cropCycleId: [""],
      cropCycleStageId: [""],
      irrigationMethodId: ["", [Validators.required]],
      actualStartedAt: [null as string | null],
      actualEndedAt: [null as string | null],
      actualDurationMinutes: [null as number | null, [Validators.min(1)]],
      actualWaterQuantity: [null as number | null, [Validators.min(0.01)]],
      actualWaterUnitId: [""],
      notes: ["", [Validators.maxLength(2000)]],
    },
    { validators: [waterQuantityAndUnitValidator, startEndDateTimeValidator] },
  );

  ngOnInit(): void {
    this.loadInitialData();
    this.setupDurationAutoCalculation();
  }

  private loadInitialData(): void {
    this.isLoading.set(true);

    forkJoin({
      farms: this.farmService.listFarms(1, 100, "", true),
      methods: this.irrigationService.getMethods(true),
      volumeUnits: this.farmService.listUnits("Volume"),
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (res) => {
          this.farms.set(res.farms.items);
          this.methods.set(res.methods);
          this.volumeUnits.set(res.volumeUnits);

          // If exactly one method exists, auto-select it
          if (res.methods.length === 1) {
            this.form.patchValue({ irrigationMethodId: res.methods[0].id });
          }
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to load master data for irrigation.");
          this.errorMessage.set(msg);
          this.snack.open(msg, "Dismiss", { duration: 4000 });
        },
      });
  }

  private setupDurationAutoCalculation(): void {
    merge(
      this.form.get("actualStartedAt")!.valueChanges,
      this.form.get("actualEndedAt")!.valueChanges,
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.recalculateDuration();
      });
  }

  private recalculateDuration(): void {
    const startVal = this.form.get("actualStartedAt")?.value;
    const endVal = this.form.get("actualEndedAt")?.value;

    if (startVal && endVal) {
      const startDate = new Date(startVal);
      const endDate = new Date(endVal);

      if (!isNaN(startDate.getTime()) && !isNaN(endDate.getTime())) {
        const diffMs = endDate.getTime() - startDate.getTime();
        if (diffMs > 0) {
          const minutes = Math.round(diffMs / 60000);
          this.form.patchValue(
            { actualDurationMinutes: minutes },
            { emitEvent: false },
          );
        }
      }
    }
  }

  onFarmChange(selectedFarmId: string): void {
    this.form.patchValue({
      farmAreaId: "",
      plantationId: "",
      cropCycleId: "",
      cropCycleStageId: "",
    });
    this.areas.set([]);
    this.plantations.set([]);
    this.cropCycles.set([]);
    this.stages.set([]);

    if (selectedFarmId) {
      this.loadAreasAndPlantations(selectedFarmId);
    }
  }

  onAreaChange(selectedAreaId: string): void {
    const farmId = this.form.value.farmId;
    if (!farmId) return;

    this.form.patchValue({
      plantationId: "",
      cropCycleId: "",
      cropCycleStageId: "",
    });
    this.cropCycles.set([]);
    this.stages.set([]);

    this.farmService
      .listPlantations(1, 100, farmId, selectedAreaId || undefined)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => this.plantations.set(res.items),
      });
  }

  onPlantationChange(selectedPlantationId: string): void {
    this.form.patchValue({
      cropCycleId: "",
      cropCycleStageId: "",
    });
    this.cropCycles.set([]);
    this.stages.set([]);

    if (selectedPlantationId) {
      this.loadCropCycles(selectedPlantationId);
    }
  }

  onCropCycleChange(selectedCycleId: string): void {
    this.form.patchValue({
      cropCycleStageId: "",
    });
    this.stages.set([]);

    if (selectedCycleId) {
      this.loadStages(selectedCycleId);
    }
  }

  private loadAreasAndPlantations(farmId: string, areaId?: string): void {
    forkJoin({
      areas: this.farmService.listAreas(farmId),
      plantations: this.farmService.listPlantations(1, 100, farmId, areaId),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.areas.set(res.areas);
          this.plantations.set(res.plantations.items);
        },
      });
  }

  private loadCropCycles(plantationId: string): void {
    this.farmService
      .listCycles(1, 100, undefined, undefined, plantationId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res: CycleList) => this.cropCycles.set(res.items),
      });
  }

  private loadStages(cropCycleId: string): void {
    this.irrigationService
      .getCropCycleStages(cropCycleId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (stages) => this.stages.set(stages),
      });
  }

  onCancel(): void {
    if (this.form.dirty) {
      const dialogRef = this.dialog.open(ConfirmDialogComponent, {
        data: {
          title: "Discard Changes?",
          message: "You have unsaved changes. Are you sure you want to discard them?",
          confirmText: "Discard",
          cancelText: "Keep Editing",
        },
      });

      dialogRef.afterClosed().subscribe((confirmed) => {
        if (confirmed) {
          this.router.navigate(["/irrigations"]);
        }
      });
    } else {
      this.router.navigate(["/irrigations"]);
    }
  }

  onSubmit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.errorMessage.set("Please review the highlighted validation errors before submitting.");
      return;
    }

    const val = this.form.value;
    const startedAt = val.actualStartedAt ? new Date(val.actualStartedAt).toISOString() : null;
    const endedAt = val.actualEndedAt ? new Date(val.actualEndedAt).toISOString() : null;

    const payload: RecordCompletedIrrigationRequest = {
      farmId: val.farmId!,
      farmAreaId: val.farmAreaId!,
      irrigationMethodId: val.irrigationMethodId!,
      plantationId: val.plantationId || null,
      cropCycleId: val.cropCycleId || null,
      cropCycleStageId: val.cropCycleStageId || null,
      actualStartedAt: startedAt,
      actualEndedAt: endedAt,
      actualDurationMinutes:
        val.actualDurationMinutes != null
          ? Number(val.actualDurationMinutes)
          : null,
      actualWaterQuantity:
        val.actualWaterQuantity != null
          ? Number(val.actualWaterQuantity)
          : null,
      actualWaterUnitId: val.actualWaterUnitId || null,
      notes: val.notes?.trim() || null,
    };

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.irrigationService
      .recordCompleted(payload)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: (res) => {
          this.snack.open("Completed irrigation event recorded successfully.", "Dismiss", {
            duration: 3500,
          });
          this.router.navigate(["/irrigations", res.id]);
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to record completed irrigation event.");
          this.errorMessage.set(msg);
          this.snack.open(msg, "Dismiss", { duration: 5000 });
        },
      });
  }
}
