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
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";

import { FarmManagementService } from "../../../../core/farm-management/farm-management.service";
import { Farm } from "../../../../core/farm-management/farm-management.models";
import { InventoryItem, StockBalance, StorageLocation } from "../../../../core/inventory/inventory.models";
import { InventoryService } from "../../../../core/inventory/inventory.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import { formatDateOnly } from "../../../../core/utils/date.utils";

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
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  readonly farms = signal<readonly Farm[]>([]);
  readonly items = signal<readonly InventoryItem[]>([]);
  readonly locations = signal<readonly StorageLocation[]>([]);
  readonly destLocations = signal<readonly StorageLocation[]>([]);
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
    adjustmentType: ["AdjustmentIn"],
    quantity: [null as number | null, [Validators.required, Validators.min(0.0001)]],
    // Added maxLength(100) validator matching backend referenceNumber column limit
    referenceNumber: ["", [Validators.maxLength(100)]],
    // Added maxLength(1000) validator matching backend notes column limit
    notes: ["", [Validators.maxLength(1000)]],
  });

  // Reactive signal converting form changes so computed() signals update reactively on user edits
  private readonly formValues = toSignal(this.form.valueChanges, {
    initialValue: this.form.getRawValue(),
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

    this.form.controls.farmId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((farmId) => {
        this.form.controls.storageLocationId.setValue("");
        if (farmId) this.loadLocations(farmId, false);
      });

    this.form.controls.destinationFarmId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((farmId) => {
        this.form.controls.destinationStorageLocationId.setValue("");
        if (farmId) this.loadLocations(farmId, true);
      });

    if (this.data.defaultFarmId) {
      this.loadLocations(this.data.defaultFarmId, false);
    }

    // Trigger initial balance check if default item and location are provided
    if (this.data.defaultLocationId && this.data.defaultItemId) {
      checkBalance();
    }
  }

  isTransfer(): boolean {
    return this.data.operationType === "TRANSFER";
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
