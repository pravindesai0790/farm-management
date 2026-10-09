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
  FormArray,
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatDialog, MatDialogModule } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTooltipModule } from "@angular/material/tooltip";
import { ActivatedRoute, Router, RouterModule } from "@angular/router";
import { finalize, forkJoin } from "rxjs";

import { BreadcrumbService } from "../../../core/breadcrumb/breadcrumb.service";
import { FarmManagementService } from "../../../core/farm-management/farm-management.service";
import { Unit } from "../../../core/farm-management/farm-management.models";
import { PermissionService } from "../../../core/auth/permission.service";
import {
  ApplicationMethodResponse,
  CompleteSprayProductItemRequest,
  CompleteSprayRequest,
  SprayDetailsResponse,
  SprayProductDto,
  SprayStorageLocationLookupResponse,
  StartSprayProductItemRequest,
  StartSprayRequest,
  TargetResponse,
  UpdateSprayExecutionProductItemRequest,
  UpdateSprayExecutionRequest,
} from "../../../core/sprays/spray.models";
import { SprayService } from "../../../core/sprays/spray.service";
import { ConfirmDialogComponent } from "../../../shared/components/confirm-dialog/confirm-dialog.component";
import { getApiErrorMessage } from "../../../core/models/api-error.model";

function formatToDateTimeLocal(date: Date = new Date()): string {
  const pad = (n: number) => n.toString().padStart(2, "0");
  const year = date.getFullYear();
  const month = pad(date.getMonth() + 1);
  const day = pad(date.getDate());
  const hours = pad(date.getHours());
  const minutes = pad(date.getMinutes());
  return `${year}-${month}-${day}T${hours}:${minutes}`;
}

interface ProductExecutionRowForm {
  inventoryItemId: FormControl<string>;
  inventoryItemName: FormControl<string>;
  inventoryItemSku: FormControl<string>;
  stockUnitSymbol: FormControl<string>;
  storageLocationId: FormControl<string>;
  storageLocationName: FormControl<string>;
  plannedQuantity: FormControl<number | null>;
  actualQuantity: FormControl<number | null>;
  dosage: FormControl<string>;
}

@Component({
  selector: "app-spray-execution-page",
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
    MatTooltipModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: "./spray-execution-page.component.html",
  styleUrl: "./spray-execution-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SprayExecutionPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly sprayService = inject(SprayService);
  private readonly farmService = inject(FarmManagementService);
  private readonly breadcrumbService = inject(BreadcrumbService);
  private readonly snack = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);

  readonly id = this.route.snapshot.paramMap.get("id") ?? "";

  // Mode detection: 'start' vs 'execution'
  readonly isStartMode = computed(() => {
    return this.route.snapshot.url.some((segment) => segment.path === "start");
  });

  // State signals
  readonly spray = signal<SprayDetailsResponse | null>(null);
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  // Lookups
  readonly targets = signal<readonly TargetResponse[]>([]);
  readonly applicationMethods = signal<readonly ApplicationMethodResponse[]>([]);
  readonly areaUnits = signal<readonly Unit[]>([]);
  readonly volumeUnits = signal<readonly Unit[]>([]);
  readonly storageLocationsByItem = signal<Map<string, readonly SprayStorageLocationLookupResponse[]>>(new Map());

  // Reactive Form
  readonly form = this.fb.group({
    actualApplicationDateTime: [formatToDateTimeLocal(), [Validators.required]],
    actualTreatedArea: [null as number | null, [Validators.min(0)]],
    actualTreatedAreaUnitId: [""],
    waterQuantity: [null as number | null, [Validators.min(0)]],
    waterUnitId: [""],
    targetId: [""],
    applicationMethodId: [""],
    purposeReason: ["", [Validators.maxLength(1000)]],
    products: this.fb.array<FormGroup<ProductExecutionRowForm>>([]),
  });

  get productsArray(): FormArray<FormGroup<ProductExecutionRowForm>> {
    return this.form.get("products") as FormArray<FormGroup<ProductExecutionRowForm>>;
  }

  ngOnInit(): void {
    if (!this.id) {
      this.errorMessage.set("No spray application ID specified.");
      this.isLoading.set(false);
      return;
    }

    this.loadInitialData();
  }

  private loadInitialData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      targets: this.sprayService.getTargets(),
      appMethods: this.sprayService.getApplicationMethods(),
      areaUnits: this.farmService.listUnits("Area"),
      volumeUnits: this.farmService.listUnits("Volume"),
      spray: this.sprayService.getSpray(this.id),
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (res) => {
          this.targets.set(res.targets);
          this.applicationMethods.set(res.appMethods);
          this.areaUnits.set(res.areaUnits);
          this.volumeUnits.set(res.volumeUnits);
          this.spray.set(res.spray);

          if (res.spray.referenceNumber) {
            this.breadcrumbService.setEntityName(
              this.id,
              this.isStartMode() ? `Start ${res.spray.referenceNumber}` : `Execute ${res.spray.referenceNumber}`,
            );
          }

          this.populateForm(res.spray);

          // If in start mode, query storage locations for each product
          if (this.isStartMode() && res.spray.products.length > 0) {
            this.loadStorageLocationsForProducts(res.spray.farmId, res.spray.products);
          }
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to load spray application details");
          this.errorMessage.set(msg);
          this.snack.open(msg, "Dismiss", { duration: 4000 });
        },
      });
  }

  private populateForm(spray: SprayDetailsResponse): void {
    const isStart = this.isStartMode();

    // Default datetime
    let appDateTime = formatToDateTimeLocal();
    if (!isStart && spray.actualApplicationDateTime) {
      const dt = new Date(spray.actualApplicationDateTime);
      if (!isNaN(dt.getTime())) {
        appDateTime = formatToDateTimeLocal(dt);
      }
    }

    this.form.patchValue({
      actualApplicationDateTime: appDateTime,
      actualTreatedArea: spray.actualTreatedArea ?? spray.plannedArea ?? null,
      actualTreatedAreaUnitId: spray.actualTreatedAreaUnitId ?? spray.plannedAreaUnitId ?? "",
      waterQuantity: spray.waterQuantity ?? null,
      waterUnitId: spray.waterUnitId ?? "",
      targetId: spray.targetId ?? "",
      applicationMethodId: spray.applicationMethodId ?? "",
      purposeReason: spray.purposeReason ?? "",
    });

    this.productsArray.clear();

    spray.products.forEach((p) => {
      // In both Start and Execution modes, actual quantity defaults to actual or planned quantity
      const actualQty = p.actualQuantity ?? p.plannedQuantity ?? null;

      const rowGroup = this.fb.group<ProductExecutionRowForm>({
        inventoryItemId: new FormControl(p.inventoryItemId, {
          nonNullable: true,
          validators: [Validators.required],
        }),
        inventoryItemName: new FormControl(p.inventoryItemName, { nonNullable: true }),
        inventoryItemSku: new FormControl(p.inventoryItemSku ?? "", { nonNullable: true }),
        stockUnitSymbol: new FormControl(p.stockUnitSymbol ?? p.stockUnitName ?? "", { nonNullable: true }),
        storageLocationId: new FormControl(p.storageLocationId ?? "", {
          nonNullable: true,
          validators: isStart ? [Validators.required] : [],
        }),
        storageLocationName: new FormControl(p.storageLocationName ?? "", { nonNullable: true }),
        plannedQuantity: new FormControl(p.plannedQuantity ?? null),
        actualQuantity: new FormControl(actualQty, {
          validators: [Validators.required, Validators.min(0.0001)],
        }),
        dosage: new FormControl(p.dosage ?? "", { nonNullable: true }),
      });

      this.productsArray.push(rowGroup);
    });
  }

  private loadStorageLocationsForProducts(farmId: string, products: readonly SprayProductDto[]): void {
    const requests = products.map((p) =>
      this.sprayService.getStorageLocationLookup(farmId, p.inventoryItemId),
    );

    forkJoin(requests)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (results) => {
          const map = new Map<string, readonly SprayStorageLocationLookupResponse[]>();
          results.forEach((locations, idx) => {
            const item = products[idx];
            map.set(item.inventoryItemId, locations);

            // Pre-select storage location if already assigned or only 1 available
            const row = this.productsArray.at(idx);
            if (row && !row.value.storageLocationId) {
              if (item.storageLocationId) {
                row.patchValue({ storageLocationId: item.storageLocationId });
              } else if (locations.length === 1) {
                row.patchValue({ storageLocationId: locations[0].storageLocationId });
              } else {
                const available = locations.find((l) => l.hasStock && l.currentStock > 0);
                if (available) {
                  row.patchValue({ storageLocationId: available.storageLocationId });
                }
              }
            }
          });
          this.storageLocationsByItem.set(map);
        },
      });
  }

  getStorageLocations(itemId: string): readonly SprayStorageLocationLookupResponse[] {
    return this.storageLocationsByItem().get(itemId) ?? [];
  }

  getAvailableStock(itemId: string, locationId: string): number | null {
    const locations = this.getStorageLocations(itemId);
    const loc = locations.find((l) => l.storageLocationId === locationId);
    return loc ? loc.currentStock : null;
  }

  onStart(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.snack.open("Please provide storage locations and actual quantities for all products.", "Dismiss", { duration: 4000 });
      return;
    }

    const val = this.form.value;
    const products: StartSprayProductItemRequest[] = this.productsArray.controls.map((control) => {
      const v = control.value;
      return {
        inventoryItemId: v.inventoryItemId!,
        storageLocationId: v.storageLocationId!,
        actualQuantity: Number(v.actualQuantity),
        dosage: v.dosage?.trim() || null,
      };
    });

    const request: StartSprayRequest = {
      actualApplicationDateTime: new Date(val.actualApplicationDateTime!).toISOString(),
      products,
    };

    this.isSubmitting.set(true);
    this.sprayService
      .startSpray(this.id, request)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: (updated) => {
          this.snack.open("Spray application started successfully.", "OK", { duration: 3000 });
          this.router.navigate(["/sprays", this.id, "execution"]);
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to start spray application");
          this.snack.open(msg, "Dismiss", { duration: 4500 });
        },
      });
  }

  onSaveProgress(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.snack.open("Please correct form errors before saving progress.", "Dismiss", { duration: 4000 });
      return;
    }

    const val = this.form.value;
    const products: UpdateSprayExecutionProductItemRequest[] = this.productsArray.controls.map((control) => {
      const v = control.value;
      return {
        inventoryItemId: v.inventoryItemId!,
        actualQuantity: Number(v.actualQuantity),
        dosage: v.dosage?.trim() || null,
      };
    });

    const request: UpdateSprayExecutionRequest = {
      actualApplicationDateTime: new Date(val.actualApplicationDateTime!).toISOString(),
      actualTreatedArea: val.actualTreatedArea ? Number(val.actualTreatedArea) : null,
      actualTreatedAreaUnitId: val.actualTreatedAreaUnitId || null,
      waterQuantity: val.waterQuantity ? Number(val.waterQuantity) : null,
      waterUnitId: val.waterUnitId || null,
      targetId: val.targetId || null,
      applicationMethodId: val.applicationMethodId || null,
      purposeReason: val.purposeReason?.trim() || null,
      products,
    };

    this.isSubmitting.set(true);
    this.sprayService
      .saveExecution(this.id, request)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: () => {
          this.snack.open("Execution progress saved (inventory not deducted).", "OK", { duration: 3000 });
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to save execution progress");
          this.snack.open(msg, "Dismiss", { duration: 4500 });
        },
      });
  }

  onComplete(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.snack.open("Please ensure all required actual metrics are filled in before completion.", "Dismiss", { duration: 4000 });
      return;
    }

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: "Complete Spray Application",
        message:
          "Are you sure you want to complete this spray application? This action will finalize the application and deduct product quantities from inventory immediately.",
        confirmText: "Complete Spray",
        color: "primary",
        icon: "check_circle",
      },
    });

    dialogRef.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;

      const val = this.form.value;
      const products: CompleteSprayProductItemRequest[] = this.productsArray.controls.map((control) => {
        const v = control.value;
        return {
          inventoryItemId: v.inventoryItemId!,
          actualQuantity: Number(v.actualQuantity),
          dosage: v.dosage?.trim() || null,
        };
      });

      const request: CompleteSprayRequest = {
        actualApplicationDateTime: new Date(val.actualApplicationDateTime!).toISOString(),
        actualTreatedArea: val.actualTreatedArea ? Number(val.actualTreatedArea) : null,
        actualTreatedAreaUnitId: val.actualTreatedAreaUnitId || null,
        waterQuantity: val.waterQuantity ? Number(val.waterQuantity) : null,
        waterUnitId: val.waterUnitId || null,
        targetId: val.targetId || null,
        applicationMethodId: val.applicationMethodId || null,
        purposeReason: val.purposeReason?.trim() || null,
        products,
      };

      this.isSubmitting.set(true);
      this.sprayService
        .completeSpray(this.id, request)
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isSubmitting.set(false)),
        )
        .subscribe({
          next: () => {
            this.snack.open("Spray application completed and stock deducted successfully.", "OK", { duration: 3500 });
            this.router.navigate(["/sprays", this.id]);
          },
          error: (err) => {
            const msg = getApiErrorMessage(err, "Failed to complete spray application");
            this.snack.open(msg, "Dismiss", { duration: 5000 });
          },
        });
    });
  }

  onCancel(): void {
    this.router.navigate(["/sprays", this.id]);
  }
}
