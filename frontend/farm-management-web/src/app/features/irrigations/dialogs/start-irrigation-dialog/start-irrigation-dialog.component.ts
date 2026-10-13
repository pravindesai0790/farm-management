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
import { MatSnackBar } from "@angular/material/snack-bar";
import { finalize } from "rxjs";

import {
  CropCycle,
  CropCycleStage,
  FarmArea,
  Plantation,
} from "../../../../core/farm-management/farm-management.models";
import { FarmManagementService } from "../../../../core/farm-management/farm-management.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import { IrrigationService } from "../../../../core/irrigations/irrigation.service";
import { DateTimePickerComponent } from "../../../../shared/components/date-time-picker/date-time-picker.component";

export interface StartIrrigationDialogData {
  readonly irrigationId: string;
  readonly farmId: string;
  readonly farmName: string;
  readonly farmAreaId: string;
  readonly farmAreaName?: string;
  readonly plantationId?: string | null;
  readonly cropCycleId?: string | null;
  readonly cropCycleStageId?: string | null;
  readonly concurrencyToken?: string | null;
}

@Component({
  selector: "app-start-irrigation-dialog",
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
  templateUrl: "./start-irrigation-dialog.component.html",
  styleUrl: "./start-irrigation-dialog.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StartIrrigationDialogComponent implements OnInit {
  readonly data: StartIrrigationDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<StartIrrigationDialogComponent>);
  private readonly irrigationService = inject(IrrigationService);
  private readonly farmService = inject(FarmManagementService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  readonly isSubmitting = signal(false);
  readonly isLoadingLocations = signal(false);

  readonly areas = signal<readonly FarmArea[]>([]);
  readonly plantations = signal<readonly Plantation[]>([]);
  readonly cropCycles = signal<readonly CropCycle[]>([]);
  readonly stages = signal<readonly CropCycleStage[]>([]);

  readonly form = this.fb.group({
    farmAreaId: [this.data.farmAreaId || "", [Validators.required]],
    plantationId: [this.data.plantationId || ""],
    cropCycleId: [this.data.cropCycleId || ""],
    cropCycleStageId: [this.data.cropCycleStageId || ""],
    actualStartedAt: [null as string | null],
  });

  ngOnInit(): void {
    if (this.data.farmId) {
      this.loadAreasAndPlantations(this.data.farmId, this.data.farmAreaId);
    }
  }

  private loadAreasAndPlantations(farmId: string, initialAreaId?: string): void {
    this.isLoadingLocations.set(true);
    this.farmService
      .listFarmAreas(1, 100, farmId, true)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoadingLocations.set(false)),
      )
      .subscribe({
        next: (res) => {
          this.areas.set(res.items);
          this.loadPlantations(farmId, initialAreaId);
        },
        error: () => this.areas.set([]),
      });
  }

  private loadPlantations(farmId: string, areaId?: string): void {
    this.farmService
      .listPlantations(1, 100, farmId, areaId || undefined)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.plantations.set(res.items);
          if (this.data.plantationId) {
            this.loadCropCycles(this.data.plantationId);
          }
        },
        error: () => this.plantations.set([]),
      });
  }

  onAreaChange(areaId: string): void {
    this.form.patchValue({
      plantationId: "",
      cropCycleId: "",
      cropCycleStageId: "",
    });
    this.plantations.set([]);
    this.cropCycles.set([]);
    this.stages.set([]);

    if (this.data.farmId) {
      this.loadPlantations(this.data.farmId, areaId || undefined);
    }
  }

  private loadCropCycles(plantationId: string): void {
    this.farmService
      .listCycles(1, 100, undefined, undefined, plantationId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.cropCycles.set(res.items);
          if (this.data.cropCycleId) {
            this.loadStages(this.data.cropCycleId);
          }
        },
        error: () => this.cropCycles.set([]),
      });
  }

  onPlantationChange(plantationId: string): void {
    this.form.patchValue({
      cropCycleId: "",
      cropCycleStageId: "",
    });
    this.cropCycles.set([]);
    this.stages.set([]);

    if (plantationId) {
      this.loadCropCycles(plantationId);
    }
  }

  private loadStages(cycleId: string): void {
    this.irrigationService
      .getCropCycleStages(cycleId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (stages) => this.stages.set(stages),
        error: () => this.stages.set([]),
      });
  }

  onCropCycleChange(cycleId: string): void {
    this.form.patchValue({
      cropCycleStageId: "",
    });
    this.stages.set([]);

    if (cycleId) {
      this.loadStages(cycleId);
    }
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onSubmit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const rawValue = this.form.value.actualStartedAt?.trim();
    const isoDateTime = rawValue ? new Date(rawValue).toISOString() : null;

    this.isSubmitting.set(true);

    this.irrigationService
      .start(this.data.irrigationId, {
        actualStartedAt: isoDateTime,
        farmAreaId: this.form.value.farmAreaId || undefined,
        plantationId: this.form.value.plantationId || null,
        cropCycleId: this.form.value.cropCycleId || null,
        cropCycleStageId: this.form.value.cropCycleStageId || null,
        concurrencyToken: this.data.concurrencyToken,
      })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.snack.open("Irrigation event started successfully.", "Dismiss", {
            duration: 3500,
          });
          this.dialogRef.close(true);
        },
        error: (err) => {
          this.snack.open(getApiErrorMessage(err, "Failed to start irrigation event."), "Dismiss", {
            duration: 5000,
          });
        },
      });
  }
}
