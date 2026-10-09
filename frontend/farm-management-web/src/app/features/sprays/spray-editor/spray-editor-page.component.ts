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
  FormArray,
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatDialog } from "@angular/material/dialog";
import { MatDividerModule } from "@angular/material/divider";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTableModule } from "@angular/material/table";
import { MatTooltipModule } from "@angular/material/tooltip";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { finalize, forkJoin } from "rxjs";

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
import { getApiErrorMessage } from "../../../core/models/api-error.model";
import {
  ApplicationMethodResponse,
  CreateSprayDraftRequest,
  SprayDetailsResponse,
  SprayProductItemRequest,
  SprayProductLookupResponse,
  TargetResponse,
  UpdateSprayDraftRequest,
} from "../../../core/sprays/spray.models";
import { SprayService } from "../../../core/sprays/spray.service";
import { ScheduleSprayDialogComponent } from "../dialogs/schedule-spray-dialog/schedule-spray-dialog.component";

export interface ProductRowForm {
  inventoryItemId: FormControl<string>;
  plannedQuantity: FormControl<number | null>;
  dosage: FormControl<string>;
}

@Component({
  selector: "app-spray-editor-page",
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatCardModule,
    MatDividerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule,
    ReactiveFormsModule,
  ],
  templateUrl: "./spray-editor-page.component.html",
  styleUrl: "./spray-editor-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SprayEditorPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  private readonly farmService = inject(FarmManagementService);
  private readonly sprayService = inject(SprayService);
  readonly permissionService = inject(PermissionService);

  readonly id = this.route.snapshot.paramMap.get("id");
  readonly isEditMode = computed(() => !!this.id);

  readonly isLoading = signal(false);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly sprayDetails = signal<SprayDetailsResponse | null>(null);

  // Cascading and lookup signals
  readonly farms = signal<readonly Farm[]>([]);
  readonly areas = signal<readonly FarmArea[]>([]);
  readonly plantations = signal<readonly Plantation[]>([]);
  readonly cropCycles = signal<readonly CropCycle[]>([]);
  readonly stages = signal<readonly CropCycleStage[]>([]);
  readonly targets = signal<readonly TargetResponse[]>([]);
  readonly applicationMethods = signal<readonly ApplicationMethodResponse[]>([]);
  readonly areaUnits = signal<readonly Unit[]>([]);
  readonly volumeUnits = signal<readonly Unit[]>([]);
  readonly availableProducts = signal<readonly SprayProductLookupResponse[]>([]);

  readonly form = this.fb.group({
    farmId: ["", [Validators.required]],
    farmAreaId: [""],
    plantationId: [""],
    cropCycleId: [""],
    cropCycleStageId: [""],
    plannedDate: [""],
    plannedArea: [null as number | null, [Validators.min(0)]],
    plannedAreaUnitId: [""],
    waterQuantity: [null as number | null, [Validators.min(0)]],
    waterUnitId: [""],
    targetId: [""],
    applicationMethodId: [""],
    purposeReason: ["", [Validators.maxLength(1000)]],
    products: this.fb.array<FormGroup<ProductRowForm>>([]),
  });

  get productsArray(): FormArray<FormGroup<ProductRowForm>> {
    return this.form.get("products") as FormArray<FormGroup<ProductRowForm>>;
  }

  ngOnInit(): void {
    this.loadInitialData();
  }

  private loadInitialData(): void {
    this.isLoading.set(true);

    forkJoin({
      farms: this.farmService.listFarms(1, 100, "", true),
      targets: this.sprayService.getTargets(),
      appMethods: this.sprayService.getApplicationMethods(),
      areaUnits: this.farmService.listUnits("Area"),
      volumeUnits: this.farmService.listUnits("Volume"),
      products: this.sprayService.getProductLookup(),
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
          this.targets.set(res.targets);
          this.applicationMethods.set(res.appMethods);
          this.areaUnits.set(res.areaUnits);
          this.volumeUnits.set(res.volumeUnits);
          this.availableProducts.set(res.products);

          if (this.id) {
            this.loadExistingSpray(this.id);
          }
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to load required master data");
          this.errorMessage.set(msg);
          this.snack.open(msg, "Dismiss", { duration: 4000 });
        },
      });
  }

  private loadExistingSpray(sprayId: string): void {
    this.sprayService
      .getSpray(sprayId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (spray) => {
          this.sprayDetails.set(spray);

          if (spray.status !== "Draft") {
            this.snack.open(
              `Cannot edit spray in status "${spray.status}". Only Draft sprays can be edited.`,
              "OK",
              { duration: 5000 },
            );
            this.router.navigate(["/sprays", sprayId]);
            return;
          }

          this.populateForm(spray);
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to load spray application details");
          this.errorMessage.set(msg);
          this.snack.open(msg, "Dismiss", { duration: 4000 });
        },
      });
  }

  private populateForm(spray: SprayDetailsResponse): void {
    this.form.patchValue({
      farmId: spray.farmId,
      farmAreaId: spray.farmAreaId || "",
      plantationId: spray.plantationId || "",
      cropCycleId: spray.cropCycleId || "",
      cropCycleStageId: spray.cropCycleStageId || "",
      plannedDate: spray.plannedDate || "",
      plannedArea: spray.plannedArea,
      plannedAreaUnitId: spray.plannedAreaUnitId || "",
      waterQuantity: spray.waterQuantity,
      waterUnitId: spray.waterUnitId || "",
      targetId: spray.targetId || "",
      applicationMethodId: spray.applicationMethodId || "",
      purposeReason: spray.purposeReason || "",
    });

    // Populate dependent dropdowns
    if (spray.farmId) {
      this.loadAreasAndPlantations(spray.farmId);
    }
    if (spray.plantationId) {
      this.loadCropCycles(spray.plantationId);
    }
    if (spray.cropCycleId) {
      this.loadStages(spray.cropCycleId);
    }

    // Populate products
    this.productsArray.clear();
    if (spray.products && spray.products.length > 0) {
      for (const p of spray.products) {
        this.addProduct({
          inventoryItemId: p.inventoryItemId,
          plannedQuantity: p.plannedQuantity,
          dosage: p.dosage || "",
        });
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

  private loadAreasAndPlantations(farmId: string): void {
    forkJoin({
      areas: this.farmService.listAreas(farmId),
      plantations: this.farmService.listPlantations(1, 100, farmId),
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
    this.sprayService
      .getCropCycleStages(cycleId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (stages) => this.stages.set(stages),
      });
  }

  // --- Product Grid Methods ---
  addProduct(item?: Partial<SprayProductItemRequest>): void {
    const row = this.fb.group<ProductRowForm>({
      inventoryItemId: this.fb.control(item?.inventoryItemId || "", {
        nonNullable: true,
        validators: [Validators.required],
      }),
      plannedQuantity: this.fb.control(
        item?.plannedQuantity !== undefined ? item.plannedQuantity : null,
        [Validators.min(0)],
      ),
      dosage: this.fb.control(item?.dosage || "", {
        nonNullable: true,
        validators: [Validators.maxLength(100)],
      }),
    });

    this.productsArray.push(row);
  }

  removeProduct(index: number): void {
    this.productsArray.removeAt(index);
  }

  getProductInfo(inventoryItemId: string | null | undefined): SprayProductLookupResponse | undefined {
    if (!inventoryItemId) return undefined;
    return this.availableProducts().find((p) => p.inventoryItemId === inventoryItemId);
  }

  isProductOptionDisabled(inventoryItemId: string, currentRowIndex: number): boolean {
    const selectedIds = this.productsArray.controls
      .map((ctrl, idx) => (idx !== currentRowIndex ? ctrl.value.inventoryItemId : null))
      .filter(Boolean);
    return selectedIds.includes(inventoryItemId);
  }

  hasDuplicateProducts(): boolean {
    const ids = this.productsArray.controls
      .map((c) => c.value.inventoryItemId)
      .filter((id) => !!id);
    const uniqueIds = new Set(ids);
    return uniqueIds.size !== ids.length;
  }

  // --- Submission Actions ---
  onSaveDraft(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.snack.open("Please correct form errors before saving", "Dismiss", { duration: 3000 });
      return;
    }

    if (this.hasDuplicateProducts()) {
      this.snack.open("Duplicate products are not allowed in the same spray", "Dismiss", { duration: 4000 });
      return;
    }

    const payload = this.buildDraftPayload();
    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    if (this.id) {
      this.sprayService
        .updateDraft(this.id, payload as UpdateSprayDraftRequest)
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isSubmitting.set(false)),
        )
        .subscribe({
          next: (res) => {
            this.snack.open("Spray application draft updated successfully", "OK", { duration: 3000 });
            this.router.navigate(["/sprays", res.id]);
          },
          error: (err) => {
            const msg = getApiErrorMessage(err, "Failed to update spray application draft");
            this.errorMessage.set(msg);
            this.snack.open(msg, "Dismiss", { duration: 5000 });
          },
        });
    } else {
      this.sprayService
        .createDraft(payload as CreateSprayDraftRequest)
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isSubmitting.set(false)),
        )
        .subscribe({
          next: (res) => {
            this.snack.open("Spray application draft created successfully", "OK", { duration: 3000 });
            this.router.navigate(["/sprays", res.id]);
          },
          error: (err) => {
            const msg = getApiErrorMessage(err, "Failed to create spray application draft");
            this.errorMessage.set(msg);
            this.snack.open(msg, "Dismiss", { duration: 5000 });
          },
        });
    }
  }

  onSchedule(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.snack.open("Please correct form errors before scheduling", "Dismiss", { duration: 3000 });
      return;
    }

    if (this.hasDuplicateProducts()) {
      this.snack.open("Duplicate products are not allowed in the same spray", "Dismiss", { duration: 4000 });
      return;
    }

    const payload = this.buildDraftPayload();
    this.isSubmitting.set(true);

    const saveObs = this.id
      ? this.sprayService.updateDraft(this.id, payload as UpdateSprayDraftRequest)
      : this.sprayService.createDraft(payload as CreateSprayDraftRequest);

    saveObs
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: (savedSpray) => {
          const dialogRef = this.dialog.open(ScheduleSprayDialogComponent, {
            width: "480px",
            data: {
              sprayId: savedSpray.id,
              referenceNumber: savedSpray.referenceNumber || "Draft",
              farmName: savedSpray.farmName,
              currentScheduledDateTime: savedSpray.scheduledDateTime,
              isReschedule: false,
            },
          });

          dialogRef.afterClosed().subscribe((scheduled) => {
            if (scheduled) {
              this.snack.open("Spray application scheduled successfully", "OK", { duration: 3000 });
              this.router.navigate(["/sprays", savedSpray.id]);
            } else {
              // Nav to saved draft even if schedule modal was dismissed
              this.router.navigate(["/sprays", savedSpray.id]);
            }
          });
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to save spray draft before scheduling");
          this.errorMessage.set(msg);
          this.snack.open(msg, "Dismiss", { duration: 5000 });
        },
      });
  }

  onCancel(): void {
    if (this.id) {
      this.router.navigate(["/sprays", this.id]);
    } else {
      this.router.navigate(["/sprays"]);
    }
  }

  private buildDraftPayload(): CreateSprayDraftRequest | UpdateSprayDraftRequest {
    const raw = this.form.value;
    const products: SprayProductItemRequest[] = this.productsArray.controls
      .map((c) => ({
        inventoryItemId: c.value.inventoryItemId!,
        plannedQuantity: c.value.plannedQuantity !== null && c.value.plannedQuantity !== undefined
          ? Number(c.value.plannedQuantity)
          : null,
        dosage: c.value.dosage?.trim() || null,
      }))
      .filter((p) => !!p.inventoryItemId);

    return {
      farmId: raw.farmId!,
      farmAreaId: raw.farmAreaId || null,
      plantationId: raw.plantationId || null,
      cropCycleId: raw.cropCycleId || null,
      cropCycleStageId: raw.cropCycleStageId || null,
      plannedDate: raw.plannedDate || null,
      plannedArea: raw.plannedArea !== null && raw.plannedArea !== undefined ? Number(raw.plannedArea) : null,
      plannedAreaUnitId: raw.plannedAreaUnitId || null,
      waterQuantity: raw.waterQuantity !== null && raw.waterQuantity !== undefined ? Number(raw.waterQuantity) : null,
      waterUnitId: raw.waterUnitId || null,
      targetId: raw.targetId || null,
      applicationMethodId: raw.applicationMethodId || null,
      purposeReason: raw.purposeReason?.trim() || null,
      products: products.length > 0 ? products : null,
    };
  }
}
