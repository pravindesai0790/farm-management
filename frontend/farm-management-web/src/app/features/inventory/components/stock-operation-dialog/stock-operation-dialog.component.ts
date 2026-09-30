import { CommonModule } from "@angular/common";
import { Component, DestroyRef, OnInit, computed, inject, signal } from "@angular/core";
import { takeUntilDestroyed, toSignal } from "@angular/core/rxjs-interop";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatNativeDateModule } from "@angular/material/core";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { finalize, map, merge } from "rxjs";

import { FarmManagementService } from "../../../../core/farm-management/farm-management.service";
import { CropCycle, CropCycleStage, Farm, FarmArea, Plantation } from "../../../../core/farm-management/farm-management.models";
import { InventoryItem, StockBalance, StorageLocation } from "../../../../core/inventory/inventory.models";
import { InventoryService } from "../../../../core/inventory/inventory.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import { formatDateOnly } from "../../../../core/utils/date.utils";
import { LaborActivity } from "../../../labor-activities/models/labor-activity.models";
import { LaborActivityService } from "../../../labor-activities/services/labor-activity.service";

export type StockOperationType =
  | "OPENING_STOCK"
  | "RECEIPT"
  | "ISSUE"
  | "ADJUSTMENT"
  | "TRANSFER";

export interface StockOperationDialogData {
  readonly operationType: StockOperationType;
  readonly defaultFarmId?: string;
  readonly defaultLocationId?: string;
  readonly defaultItemId?: string;
}

@Component({
  selector: "app-stock-operation-dialog",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatNativeDateModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  templateUrl: "./stock-operation-dialog.component.html",
  styleUrl: "./stock-operation-dialog.component.scss",
})
export class StockOperationDialogComponent implements OnInit {
  readonly data = inject<StockOperationDialogData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<StockOperationDialogComponent>);
  private readonly fb = inject(FormBuilder);
  private readonly inventoryService = inject(InventoryService);
  private readonly farmService = inject(FarmManagementService);
  private readonly laborActivityService = inject(LaborActivityService);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  readonly farms = signal<readonly Farm[]>([]);
  readonly items = signal<readonly InventoryItem[]>([]);
  readonly locations = signal<readonly StorageLocation[]>([]);
  readonly destLocations = signal<readonly StorageLocation[]>([]);
  readonly farmAreas = signal<readonly FarmArea[]>([]);
  readonly plantations = signal<readonly Plantation[]>([]);
  readonly cropCycles = signal<readonly CropCycle[]>([]);
  readonly cycleStages = signal<readonly CropCycleStage[]>([]);
  readonly laborActivities = signal<readonly LaborActivity[]>([]);

  // Loading signals for operational cascading dropdowns
  readonly isLoadingAreas = signal(false);
  readonly isLoadingPlantations = signal(false);
  readonly isLoadingCycles = signal(false);
  readonly isLoadingStages = signal(false);
  readonly isLoadingActivities = signal(false);
  readonly isSubmitting = signal(false);

  // Maximum date restriction for datepicker (prevents future movement dates)
  readonly today = new Date();

  // Signals for real-time stock balance lookup
  readonly currentBalance = signal<StockBalance | null>(null);
  readonly isLoadingBalance = signal(false);

  readonly form = this.fb.group({
    movementDate: [new Date(), Validators.required],
    inventoryItemId: [this.data.defaultItemId || "", Validators.required],
    farmId: [this.data.defaultFarmId || "", Validators.required],
    storageLocationId: [this.data.defaultLocationId || "", Validators.required],
    destinationFarmId: [""],
    destinationStorageLocationId: [""],
    farmAreaId: [""],
    plantationId: [{ value: "", disabled: true }],
    cropCycleId: [{ value: "", disabled: true }],
    cropCycleStageId: [{ value: "", disabled: true }],
    laborActivityId: [{ value: "", disabled: true }],
    adjustmentType: ["AdjustmentIn"],
    quantity: [null as number | null, [Validators.required, Validators.min(0.0001)]],
    // Added maxLength(100) validator matching backend referenceNumber column limit
    referenceNumber: ["", [Validators.maxLength(100)]],
    // Added maxLength(1000) validator matching backend notes column limit
    notes: ["", [Validators.maxLength(1000)]],
  });

  // Reactive signal converting form changes so computed() signals update reactively on user edits
  private readonly formValues = toSignal(
    merge(this.form.valueChanges, this.form.statusChanges).pipe(map(() => this.form.getRawValue())),
    {
      initialValue: this.form.getRawValue(),
    }
  );

  // Filtered labor activities based on selected cropCycleStageId (if stage is selected)
  readonly filteredLaborActivities = computed(() => {
    const stageId = this.formValues().cropCycleStageId;
    const all = this.laborActivities();
    if (!stageId) return all;
    return all.filter((a) => !a.cropCycleStage?.id || a.cropCycleStage.id === stageId);
  });

  // Computed helper determining if both storage location and item have been selected
  readonly hasSelectedLocationAndItem = computed(() => {
    const val = this.formValues();
    return !!val.storageLocationId && !!val.inventoryItemId;
  });

  // Computed available quantity on hand
  readonly availableQuantity = computed(() => this.currentBalance()?.quantityOnHand ?? 0);

  // Computed unit symbol for the currently selected item
  readonly selectedUnitSymbol = computed(() => {
    const itemId = this.formValues().inventoryItemId;
    if (!itemId) return "";
    const item = this.items().find((i) => i.id === itemId);
    return item ? (item.stockUnitSymbol || item.stockUnitCode) : "";
  });

  // Computed validation flag determining if entered quantity exceeds available stock in real-time
  readonly isInsufficientStock = computed(() => {
    const op = this.data.operationType;
    const values = this.formValues();
    const isIssueOrTransfer = op === "ISSUE" || op === "TRANSFER";
    const isAdjOut = op === "ADJUSTMENT" && values.adjustmentType === "AdjustmentOut";
    if (!isIssueOrTransfer && !isAdjOut) return false;

    const qty = values.quantity;
    if (qty === null || qty === undefined || qty <= 0) return false;

    return qty > this.availableQuantity();
  });

  ngOnInit(): void {
    if (this.isTransfer()) {
      this.form.controls.destinationFarmId.setValidators(Validators.required);
      this.form.controls.destinationStorageLocationId.setValidators(Validators.required);
    }

    if (this.data.operationType === "ADJUSTMENT") {
      this.form.controls.notes.setValidators([Validators.required, Validators.maxLength(1000)]);
    }

    this.loadFarms();
    this.loadItems();

    // Reactive stock balance checker: fetches current stock balance when item and location are selected
    const checkBalance = () => {
      const locId = this.form.controls.storageLocationId.value;
      const itemId = this.form.controls.inventoryItemId.value;
      if (locId && itemId) {
        this.isLoadingBalance.set(true);
        this.inventoryService.getBalance(locId, itemId)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: (b) => {
              this.currentBalance.set(b);
              this.isLoadingBalance.set(false);
            },
            error: () => {
              this.currentBalance.set(null);
              this.isLoadingBalance.set(false);
            },
          });
      } else {
        this.currentBalance.set(null);
      }
    };

    this.form.controls.storageLocationId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => checkBalance());

    this.form.controls.inventoryItemId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => checkBalance());

    // 0. Farm selection: resets all downstream operational dropdowns and loads areas
    this.form.controls.farmId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((farmId) => {
        this.form.controls.storageLocationId.setValue("");
        this.form.controls.farmAreaId.setValue("");
        this.form.controls.plantationId.setValue("");
        this.form.controls.cropCycleId.setValue("");
        this.form.controls.cropCycleStageId.setValue("");
        this.form.controls.laborActivityId.setValue("");

        this.form.controls.plantationId.disable();
        this.form.controls.cropCycleId.disable();
        this.form.controls.cropCycleStageId.disable();
        this.form.controls.laborActivityId.disable();

        this.farmAreas.set([]);
        this.plantations.set([]);
        this.cropCycles.set([]);
        this.cycleStages.set([]);
        this.laborActivities.set([]);

        if (farmId) {
          this.loadLocations(farmId, false);
          this.loadFarmAreas(farmId);
        }
      });

    // 1. Area selection: loads plantations for selected area and enables plantation dropdown
    this.form.controls.farmAreaId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((areaId) => {
        this.form.controls.plantationId.setValue("");
        this.form.controls.cropCycleId.setValue("");
        this.form.controls.cropCycleStageId.setValue("");
        this.form.controls.laborActivityId.setValue("");

        this.form.controls.cropCycleId.disable();
        this.form.controls.cropCycleStageId.disable();
        this.form.controls.laborActivityId.disable();

        this.plantations.set([]);
        this.cropCycles.set([]);
        this.cycleStages.set([]);
        this.laborActivities.set([]);

        const farmId = this.form.controls.farmId.value;
        if (areaId && farmId) {
          this.form.controls.plantationId.enable();
          this.isLoadingPlantations.set(true);
          this.farmService.listPlantations(1, 100, farmId, areaId)
            .pipe(
              takeUntilDestroyed(this.destroyRef),
              finalize(() => this.isLoadingPlantations.set(false))
            )
            .subscribe({
              next: (r) => this.plantations.set(r.items),
              error: () => this.plantations.set([]),
            });
        } else {
          this.form.controls.plantationId.disable();
        }
      });

    // 2. Plantation selection: loads cycles for selected plantation and enables cycle dropdown
    this.form.controls.plantationId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((plantationId) => {
        this.form.controls.cropCycleId.setValue("");
        this.form.controls.cropCycleStageId.setValue("");
        this.form.controls.laborActivityId.setValue("");

        this.form.controls.cropCycleStageId.disable();
        this.form.controls.laborActivityId.disable();

        this.cropCycles.set([]);
        this.cycleStages.set([]);
        this.laborActivities.set([]);

        const farmId = this.form.controls.farmId.value;
        const areaId = this.form.controls.farmAreaId.value;
        if (plantationId && farmId) {
          this.form.controls.cropCycleId.enable();
          this.isLoadingCycles.set(true);
          this.farmService.listCycles(1, 100, farmId, areaId || undefined, plantationId)
            .pipe(
              takeUntilDestroyed(this.destroyRef),
              finalize(() => this.isLoadingCycles.set(false))
            )
            .subscribe({
              next: (r) => this.cropCycles.set(r.items),
              error: () => this.cropCycles.set([]),
            });
        } else {
          this.form.controls.cropCycleId.disable();
        }
      });

    // 3. Crop Cycle selection: loads stages and activities for selected cycle, auto-preselects active stage
    this.form.controls.cropCycleId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((cycleId) => {
        this.form.controls.cropCycleStageId.setValue("");
        this.form.controls.laborActivityId.setValue("");

        this.cycleStages.set([]);
        this.laborActivities.set([]);

        const farmId = this.form.controls.farmId.value;
        const areaId = this.form.controls.farmAreaId.value;
        const plantationId = this.form.controls.plantationId.value;

        if (cycleId && farmId) {
          this.form.controls.cropCycleStageId.enable();
          this.form.controls.laborActivityId.enable();

          this.isLoadingStages.set(true);
          this.farmService.getCycleLifecycle(cycleId)
            .pipe(
              takeUntilDestroyed(this.destroyRef),
              finalize(() => this.isLoadingStages.set(false))
            )
            .subscribe({
              next: (lifecycle) => {
                this.cycleStages.set(lifecycle.stages || []);
                const activeStage = lifecycle.stages?.find(
                  (s) => s.status === "IN_PROGRESS"
                );
                if (activeStage) {
                  this.form.controls.cropCycleStageId.setValue(activeStage.id);
                }
              },
              error: () => this.cycleStages.set([]),
            });

          this.isLoadingActivities.set(true);
          this.laborActivityService.list({
            page: 1,
            pageSize: 100,
            farmId,
            farmAreaId: areaId || undefined,
            plantationId: plantationId || undefined,
            cropCycleId: cycleId,
          })
            .pipe(
              takeUntilDestroyed(this.destroyRef),
              finalize(() => this.isLoadingActivities.set(false))
            )
            .subscribe({
              next: (r) => this.laborActivities.set(r.items),
              error: () => this.laborActivities.set([]),
            });
        } else {
          this.form.controls.cropCycleStageId.disable();
          this.form.controls.laborActivityId.disable();
        }
      });

    // 4. Stage selection: if current selected activity doesn't match new stage, reset activity
    this.form.controls.cropCycleStageId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((stageId) => {
        const currentActId = this.form.controls.laborActivityId.value;
        if (currentActId) {
          const act = this.laborActivities().find((a) => a.id === currentActId);
          if (act && act.cropCycleStage?.id && stageId && act.cropCycleStage.id !== stageId) {
            this.form.controls.laborActivityId.setValue("");
          }
        }
      });

    this.form.controls.destinationFarmId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((farmId) => {
        this.form.controls.destinationStorageLocationId.setValue("");
        if (farmId) this.loadLocations(farmId, true);
      });

    if (this.data.defaultFarmId) {
      this.loadLocations(this.data.defaultFarmId, false);
      this.loadFarmAreas(this.data.defaultFarmId);
    }

    // Trigger initial balance check if default item and location are provided
    if (this.data.defaultLocationId && this.data.defaultItemId) {
      checkBalance();
    }
  }

  isTransfer(): boolean {
    return this.data.operationType === "TRANSFER";
  }

  isIssue(): boolean {
    return this.data.operationType === "ISSUE";
  }

  getTitle(): string {
    switch (this.data.operationType) {
      case "OPENING_STOCK": return "Record Opening Stock";
      case "RECEIPT": return "Receive Stock";
      case "ISSUE": return "Issue Stock";
      case "ADJUSTMENT": return "Adjust Stock Balance";
      case "TRANSFER": return "Transfer Stock";
    }
  }

  getIcon(): string {
    switch (this.data.operationType) {
      case "OPENING_STOCK": return "flag";
      case "RECEIPT": return "archive";
      case "ISSUE": return "unarchive";
      case "ADJUSTMENT": return "tune";
      case "TRANSFER": return "swap_horiz";
    }
  }

  private loadFarms(): void {
    this.farmService.listFarms(1, 100, "", true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((r) => this.farms.set(r.items));
  }

  private loadItems(): void {
    this.inventoryService.listItems(1, 200, null, null, true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((r) => this.items.set(r.items));
  }

  private loadFarmAreas(farmId: string): void {
    if (this.data.operationType !== "ISSUE") return;
    this.isLoadingAreas.set(true);
    this.farmService.listFarmAreas(1, 100, farmId, true)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoadingAreas.set(false))
      )
      .subscribe({
        next: (r) => this.farmAreas.set(r.items),
        error: () => this.farmAreas.set([]),
      });
  }

  private loadLocations(farmId: string, isDestination: boolean): void {
    this.inventoryService.listLocations(1, 100, farmId, true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((r) => {
        if (isDestination) {
          this.destLocations.set(r.items);
        } else {
          this.locations.set(r.items);
        }
      });
  }

  submit(): void {
    if (this.form.invalid || this.isSubmitting() || this.isInsufficientStock()) return;

    const val = this.form.getRawValue();
    const movementDateStr = formatDateOnly(val.movementDate!)!;

    this.isSubmitting.set(true);

    switch (this.data.operationType) {
      case "OPENING_STOCK":
        this.inventoryService.recordOpeningStock({
          farmId: val.farmId!,
          storageLocationId: val.storageLocationId!,
          inventoryItemId: val.inventoryItemId!,
          quantity: val.quantity!,
          movementDate: movementDateStr,
          notes: val.notes || null,
        }).subscribe(this.obsHandler());
        break;

      case "RECEIPT":
        this.inventoryService.recordReceipt({
          farmId: val.farmId!,
          storageLocationId: val.storageLocationId!,
          inventoryItemId: val.inventoryItemId!,
          quantity: val.quantity!,
          movementDate: movementDateStr,
          referenceNumber: val.referenceNumber || null,
          notes: val.notes || null,
        }).subscribe(this.obsHandler());
        break;

      case "ISSUE":
        this.inventoryService.recordIssue({
          farmId: val.farmId!,
          storageLocationId: val.storageLocationId!,
          inventoryItemId: val.inventoryItemId!,
          quantity: val.quantity!,
          movementDate: movementDateStr,
          referenceNumber: val.referenceNumber || null,
          purposeNotes: val.notes || null,
          cropCycleId: val.cropCycleId || null,
          cropCycleStageId: val.cropCycleStageId || null,
          plantationId: val.plantationId || null,
          farmAreaId: val.farmAreaId || null,
          laborActivityId: val.laborActivityId || null,
        }).subscribe(this.obsHandler());
        break;

      case "ADJUSTMENT":
        this.inventoryService.recordAdjustment({
          farmId: val.farmId!,
          storageLocationId: val.storageLocationId!,
          inventoryItemId: val.inventoryItemId!,
          adjustmentType: val.adjustmentType as "AdjustmentIn" | "AdjustmentOut",
          quantity: val.quantity!,
          movementDate: movementDateStr,
          reasonNotes: val.notes!,
        }).subscribe(this.obsHandler());
        break;

      case "TRANSFER":
        this.inventoryService.recordTransfer({
          sourceFarmId: val.farmId!,
          sourceStorageLocationId: val.storageLocationId!,
          destinationFarmId: val.destinationFarmId!,
          destinationStorageLocationId: val.destinationStorageLocationId!,
          inventoryItemId: val.inventoryItemId!,
          quantity: val.quantity!,
          movementDate: movementDateStr,
          referenceNumber: val.referenceNumber || null,
          notes: val.notes || null,
        }).subscribe(this.obsHandler());
        break;
    }
  }

  private obsHandler() {
    return {
      next: () => {
        this.snack.open("Transaction posted successfully.", "Dismiss", { duration: 3000 });
        this.dialogRef.close(true);
      },
      error: (e: unknown) => {
        this.isSubmitting.set(false);
        this.snack.open(getApiErrorMessage(e, "Transaction could not be posted."), "Dismiss", { duration: 5000 });
      },
    };
  }
}
