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
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { ActivatedRoute, Router } from "@angular/router";
import { finalize, forkJoin } from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import { BreadcrumbService } from "../../../core/breadcrumb/breadcrumb.service";
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
import { getApiErrorMessage } from "../../../core/models/api-error.model";
import {
  CreateIrrigationDraftRequest,
  IrrigationDetailsResponse,
  IrrigationMethodDto,
  UpdateIrrigationDraftRequest,
} from "../../../core/irrigations/irrigation.models";
import { IrrigationService } from "../../../core/irrigations/irrigation.service";
import { DateTimePickerComponent } from "../../../shared/components/date-time-picker/date-time-picker.component";

/**
 * Validates water quantity and unit coupling:
 * - If quantity > 0, unit is required.
 * - If unit is selected, quantity is required and must be > 0.
 */
export const waterQuantityAndUnitValidator: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null => {
  const qty = control.get("plannedWaterQuantity")?.value;
  const unit = control.get("plannedWaterUnitId")?.value;

  const hasQty = qty !== null && qty !== undefined && qty !== "" && !isNaN(Number(qty)) && Number(qty) > 0;
  const hasUnit = !!unit;

  if (hasQty && !hasUnit) {
    return { waterUnitRequired: true };
  }
  if (!hasQty && hasUnit) {
    return { waterQuantityRequired: true };
  }
  return null;
};

@Component({
  selector: "app-irrigation-editor-page",
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    ReactiveFormsModule,
    DateTimePickerComponent,
  ],
  templateUrl: "./irrigation-editor-page.component.html",
  styleUrl: "./irrigation-editor-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IrrigationEditorPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly farmService = inject(FarmManagementService);
  private readonly irrigationService = inject(IrrigationService);
  private readonly breadcrumbService = inject(BreadcrumbService);
  readonly permissionService = inject(PermissionService);

  readonly id = this.route.snapshot.paramMap.get("id");
  readonly isEditMode = computed(() => !!this.id);

  readonly isLoading = signal(false);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly irrigationDetails = signal<IrrigationDetailsResponse | null>(null);

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
      irrigationMethodId: [""],
      plannedAt: [null as string | null],
      plannedWaterQuantity: [null as number | null, [Validators.min(0.01)]],
      plannedWaterUnitId: [""],
      notes: ["", [Validators.maxLength(1000)]],
    },
    { validators: [waterQuantityAndUnitValidator] },
  );

  ngOnInit(): void {
    this.loadInitialData();
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
        finalize(() => {
          if (!this.id) {
            this.isLoading.set(false);
          }
        }),
      )
      .subscribe({
        next: (res) => {
          this.farms.set(res.farms.items);
          this.methods.set(res.methods);
          this.volumeUnits.set(res.volumeUnits);

          if (this.id) {
            this.loadExistingIrrigation(this.id);
          }
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to load required master data");
          this.errorMessage.set(msg);
          this.snack.open(msg, "Dismiss", { duration: 4000 });
        },
      });
  }

  private loadExistingIrrigation(irrigationId: string): void {
    this.irrigationService
      .getIrrigation(irrigationId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (event) => {
          this.irrigationDetails.set(event);

          const label = event.farmAreaName
            ? `${event.farmName} — ${event.farmAreaName}`
            : event.farmName;
          this.breadcrumbService.setEntityName(event.id, label);

          if (event.status !== "Draft" && event.status !== "Scheduled") {
            this.snack.open(
              `Cannot edit irrigation event in status "${event.status}". Only Draft and Scheduled events can be edited.`,
              "OK",
              { duration: 5000 },
            );
            this.router.navigate(["/irrigations", irrigationId]);
            return;
          }

          this.populateForm(event);
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to load irrigation event details");
          this.errorMessage.set(msg);
          this.snack.open(msg, "Dismiss", { duration: 4000 });
        },
      });
  }

  private populateForm(event: IrrigationDetailsResponse): void {
    this.form.patchValue({
      farmId: event.farmId,
      farmAreaId: event.farmAreaId || "",
      plantationId: event.plantationId || "",
      cropCycleId: event.cropCycleId || "",
      cropCycleStageId: event.cropCycleStageId || "",
      irrigationMethodId: event.irrigationMethodId || "",
      plannedAt: event.plannedAt || null,
      plannedWaterQuantity: event.plannedWaterQuantity,
      plannedWaterUnitId: event.plannedWaterUnitId || "",
      notes: event.notes || "",
    });

    // Populate dependent dropdowns
    if (event.farmId) {
      this.loadAreasAndPlantations(event.farmId, event.farmAreaId || undefined);
    }
    if (event.plantationId) {
      this.loadCropCycles(event.plantationId);
    }
    if (event.cropCycleId) {
      this.loadStages(event.cropCycleId);
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

  private loadStages(cycleId: string): void {
    this.irrigationService
      .getCropCycleStages(cycleId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (stages) => this.stages.set(stages),
      });
  }

  onSave(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.snack.open("Please correct form errors before saving", "Dismiss", { duration: 3000 });
      return;
    }

    const payload = this.buildPayload();
    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    if (this.id) {
      this.irrigationService
        .updateDraft(this.id, payload as UpdateIrrigationDraftRequest)
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isSubmitting.set(false)),
        )
        .subscribe({
          next: (res) => {
            this.snack.open("Irrigation event draft updated successfully", "OK", { duration: 3000 });
            this.router.navigate(["/irrigations", res.id]);
          },
          error: (err) => {
            const msg = getApiErrorMessage(err, "Failed to update irrigation event draft");
            this.errorMessage.set(msg);
            this.snack.open(msg, "Dismiss", { duration: 5000 });
          },
        });
    } else {
      this.irrigationService
        .createDraft(payload as CreateIrrigationDraftRequest)
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isSubmitting.set(false)),
        )
        .subscribe({
          next: (res) => {
            this.snack.open("Irrigation event draft created successfully", "OK", { duration: 3000 });
            this.router.navigate(["/irrigations", res.id]);
          },
          error: (err) => {
            const msg = getApiErrorMessage(err, "Failed to create irrigation event draft");
            this.errorMessage.set(msg);
            this.snack.open(msg, "Dismiss", { duration: 5000 });
          },
        });
    }
  }

  onCancel(): void {
    if (this.id) {
      this.router.navigate(["/irrigations", this.id]);
    } else {
      this.router.navigate(["/irrigations"]);
    }
  }

  private buildPayload(): CreateIrrigationDraftRequest | UpdateIrrigationDraftRequest {
    const val = this.form.value;

    const basePayload = {
      farmId: val.farmId!,
      farmAreaId: val.farmAreaId!,
      plantationId: val.plantationId || null,
      cropCycleId: val.cropCycleId || null,
      cropCycleStageId: val.cropCycleStageId || null,
      irrigationMethodId: val.irrigationMethodId || null,
      plannedAt: val.plannedAt ? new Date(val.plannedAt).toISOString() : null,
      plannedWaterQuantity: val.plannedWaterQuantity !== null && val.plannedWaterQuantity !== undefined ? Number(val.plannedWaterQuantity) : null,
      plannedWaterUnitId: val.plannedWaterUnitId || null,
      notes: val.notes?.trim() || null,
    };

    if (this.id) {
      return {
        ...basePayload,
        concurrencyToken: this.irrigationDetails()?.concurrencyToken || null,
      };
    }

    return basePayload;
  }
}
