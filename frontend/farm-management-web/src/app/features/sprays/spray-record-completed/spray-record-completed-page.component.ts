import { CommonModule } from "@angular/common";
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import {
  AbstractControl,
  FormArray,
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
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
import { MatTooltipModule } from "@angular/material/tooltip";
import { ActivatedRoute, Router, RouterModule } from "@angular/router";
import { finalize, forkJoin } from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import {
  CropCycle,
  CropCycleStage,
  Farm,
  FarmArea,
  Plantation,
  Unit,
} from "../../../core/farm-management/farm-management.models";
import { FarmManagementService } from "../../../core/farm-management/farm-management.service";
import { getApiErrorMessage } from "../../../core/models/api-error.model";
import {
  ApplicationMethodResponse,
  RecordCompletedSprayProductItemRequest,
  RecordCompletedSprayRequest,
  SprayProductLookupResponse,
  SprayStorageLocationLookupResponse,
  TargetResponse,
} from "../../../core/sprays/spray.models";
import { SprayService } from "../../../core/sprays/spray.service";
import { ConfirmDialogComponent } from "../../../shared/components/confirm-dialog/confirm-dialog.component";

function formatToDateTimeLocal(date: Date = new Date()): string {
  const pad = (n: number) => n.toString().padStart(2, "0");
  const year = date.getFullYear();
  const month = pad(date.getMonth() + 1);
  const day = pad(date.getDate());
  const hours = pad(date.getHours());
  const minutes = pad(date.getMinutes());
  return `${year}-${month}-${day}T${hours}:${minutes}`;
}

export function cannotBeFutureValidator(control: AbstractControl): ValidationErrors | null {
  const val = control.value;
  if (!val) return null;
  const selectedDate = new Date(val);
  // Add 1 minute tolerance for clock drift
  if (selectedDate.getTime() > Date.now() + 60000) {
    return { futureDate: true };
  }
  return null;
}

export interface ProductCompletedRowForm {
  inventoryItemId: FormControl<string>;
  storageLocationId: FormControl<string>;
  actualQuantity: FormControl<number | null>;
  dosage: FormControl<string>;
}

@Component({
  selector: "app-spray-record-completed-page",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatButtonModule,
    MatCardModule,
    MatDividerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTooltipModule,
  ],
  templateUrl: "./spray-record-completed-page.component.html",
  styleUrl: "./spray-record-completed-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SprayRecordCompletedPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly snack = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  private readonly farmService = inject(FarmManagementService);
  private readonly sprayService = inject(SprayService);
  readonly permissionService = inject(PermissionService);

  readonly isLoading = signal(false);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  // Lookups & Cascading signals
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
  readonly storageLocationsByItem = signal<Map<string, readonly SprayStorageLocationLookupResponse[]>>(new Map());

  readonly maxDateTime = signal(formatToDateTimeLocal());

  readonly form = this.fb.group({
    farmId: ["", [Validators.required]],
    farmAreaId: [""],
    plantationId: [""],
    cropCycleId: [""],
    cropCycleStageId: [""],
    actualApplicationDateTime: [formatToDateTimeLocal(), [Validators.required, cannotBeFutureValidator]],
    actualTreatedArea: [null as number | null, [Validators.min(0.0001)]],
    actualTreatedAreaUnitId: [""],
    waterQuantity: [null as number | null, [Validators.min(0.0001)]],
    waterUnitId: [""],
    targetId: [""],
    applicationMethodId: [""],
    purposeReason: ["", [Validators.maxLength(1000)]],
    products: this.fb.array<FormGroup<ProductCompletedRowForm>>([]),
  });

  get productsArray(): FormArray<FormGroup<ProductCompletedRowForm>> {
    return this.form.get("products") as FormArray<FormGroup<ProductCompletedRowForm>>;
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
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (res) => {
          this.farms.set(res.farms.items);
          this.targets.set(res.targets);
          this.applicationMethods.set(res.appMethods);
          this.areaUnits.set(res.areaUnits);
          this.volumeUnits.set(res.volumeUnits);
          this.availableProducts.set(res.products);

          // Initialize with 1 empty product row
          if (this.productsArray.length === 0) {
            this.addProduct();
          }

          // Optional query param preselection for farmId
          const farmIdParam = this.route.snapshot.queryParamMap.get("farmId");
          if (farmIdParam) {
            this.form.patchValue({ farmId: farmIdParam });
            this.onFarmChange(farmIdParam);
          }
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to load master lookup data");
          this.errorMessage.set(msg);
          this.snack.open(msg, "Dismiss", { duration: 4000 });
        },
      });
  }

  addProduct(): void {
    const row = this.fb.group<ProductCompletedRowForm>({
      inventoryItemId: this.fb.control("", { nonNullable: true, validators: [Validators.required] }),
      storageLocationId: this.fb.control("", { nonNullable: true, validators: [Validators.required] }),
      actualQuantity: this.fb.control<number | null>(null, [Validators.required, Validators.min(0.0001)]),
      dosage: this.fb.control("", { nonNullable: true }),
    });

    this.productsArray.push(row);
  }

  removeProduct(index: number): void {
    if (this.productsArray.length <= 1) {
      return;
    }
    this.productsArray.removeAt(index);
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

    // Clear storage locations cache and reset storage locations in product rows
    this.storageLocationsByItem.set(new Map());
    for (let i = 0; i < this.productsArray.length; i++) {
      const row = this.productsArray.at(i);
      row.patchValue({ storageLocationId: "" });
      const itemId = row.value.inventoryItemId;
      if (selectedFarmId && itemId) {
        this.loadStorageLocationsForProduct(selectedFarmId, itemId);
      }
    }

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
        next: (res) => this.cropCycles.set(res.items),
      });
  }

  private loadStages(cropCycleId: string): void {
    this.sprayService
      .getCropCycleStages(cropCycleId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (stages) => this.stages.set(stages),
      });
  }

  onProductChange(rowIndex: number, inventoryItemId: string): void {
    const row = this.productsArray.at(rowIndex);
    row.patchValue({ storageLocationId: "" });

    const farmId = this.form.value.farmId;
    if (farmId && inventoryItemId) {
      this.loadStorageLocationsForProduct(farmId, inventoryItemId);
    }
  }

  loadStorageLocationsForProduct(farmId: string, inventoryItemId: string): void {
    if (!farmId || !inventoryItemId) return;
    if (this.storageLocationsByItem().has(inventoryItemId)) return;

    this.sprayService
      .getStorageLocationLookup(farmId, inventoryItemId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (locations) => {
          this.storageLocationsByItem.update((map) => {
            const next = new Map(map);
            next.set(inventoryItemId, locations);
            return next;
          });
        },
      });
  }

  getStorageLocations(inventoryItemId?: string | null): readonly SprayStorageLocationLookupResponse[] {
    if (!inventoryItemId) return [];
    return this.storageLocationsByItem().get(inventoryItemId) ?? [];
  }

  getProductInfo(inventoryItemId?: string | null): SprayProductLookupResponse | undefined {
    if (!inventoryItemId) return undefined;
    return this.availableProducts().find((p) => p.inventoryItemId === inventoryItemId);
  }

  isProductOptionDisabled(inventoryItemId: string, currentIndex: number): boolean {
    return this.productsArray.controls.some((ctrl, i) => {
      return i !== currentIndex && ctrl.value.inventoryItemId === inventoryItemId;
    });
  }

  onSubmit(): void {
    this.errorMessage.set(null);

    const formVal = this.form.getRawValue();

    // Check paired treated area / unit
    if (formVal.actualTreatedArea && !formVal.actualTreatedAreaUnitId) {
      this.errorMessage.set("An area unit is required when actual treated area is specified.");
      return;
    }
    if (!formVal.actualTreatedArea && formVal.actualTreatedAreaUnitId) {
      this.errorMessage.set("Actual treated area is required when area unit is selected.");
      return;
    }

    // Check paired water / unit
    if (formVal.waterQuantity && !formVal.waterUnitId) {
      this.errorMessage.set("A water unit is required when water volume is specified.");
      return;
    }
    if (!formVal.waterQuantity && formVal.waterUnitId) {
      this.errorMessage.set("Water quantity is required when water unit is selected.");
      return;
    }

    // Check products
    if (this.productsArray.length === 0) {
      this.errorMessage.set("At least one product is required.");
      return;
    }

    // Check duplicate products
    const selectedItemIds = formVal.products.map((p) => p.inventoryItemId).filter(Boolean);
    const duplicates = selectedItemIds.filter((id, idx) => selectedItemIds.indexOf(id) !== idx);
    if (duplicates.length > 0) {
      this.errorMessage.set("Duplicate products in tank mix are not allowed.");
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMessage.set("Please correct all form errors before submitting.");
      this.snack.open("Please correct all errors before submitting.", "Dismiss", { duration: 4000 });
      return;
    }

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: "Record & Finalize Spray Application",
        message:
          "Recording a completed spray will immediately deduct stock from the selected storage locations and generate immutable stock movement ledger records. Once confirmed, this record cannot be modified or reversed.",
        confirmText: "Save & Complete",
        cancelText: "Review Form",
        color: "primary",
        icon: "inventory_2",
      },
    });

    dialogRef.afterClosed().subscribe((confirmed) => {
      if (confirmed) {
        this.submitCompletedSpray();
      }
    });
  }

  private submitCompletedSpray(): void {
    const formVal = this.form.getRawValue();

    const requestProducts: RecordCompletedSprayProductItemRequest[] = formVal.products.map((p) => ({
      inventoryItemId: p.inventoryItemId!,
      storageLocationId: p.storageLocationId!,
      actualQuantity: Number(p.actualQuantity),
      dosage: p.dosage?.trim() || null,
    }));

    const request: RecordCompletedSprayRequest = {
      farmId: formVal.farmId!,
      actualApplicationDateTime: new Date(formVal.actualApplicationDateTime!).toISOString(),
      products: requestProducts,
      farmAreaId: formVal.farmAreaId || null,
      plantationId: formVal.plantationId || null,
      cropCycleId: formVal.cropCycleId || null,
      cropCycleStageId: formVal.cropCycleStageId || null,
      actualTreatedArea: formVal.actualTreatedArea ? Number(formVal.actualTreatedArea) : null,
      actualTreatedAreaUnitId: formVal.actualTreatedAreaUnitId || null,
      waterQuantity: formVal.waterQuantity ? Number(formVal.waterQuantity) : null,
      waterUnitId: formVal.waterUnitId || null,
      targetId: formVal.targetId || null,
      applicationMethodId: formVal.applicationMethodId || null,
      purposeReason: formVal.purposeReason?.trim() || null,
    };

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.sprayService
      .recordCompleted(request)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: (res) => {
          this.snack.open("Spray application recorded and completed successfully.", "Dismiss", { duration: 4000 });
          this.router.navigate(["/sprays", res.id]);
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to record completed spray application");
          this.errorMessage.set(msg);
          this.snack.open(msg, "Dismiss", { duration: 5000 });
        },
      });
  }

  onCancel(): void {
    this.router.navigate(["/sprays"]);
  }
}
