import { CommonModule } from "@angular/common";
import { Component, DestroyRef, OnInit, inject, signal } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
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
import { InventoryItem, StorageLocation } from "../../../../core/inventory/inventory.models";
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

  readonly form = this.fb.group({
    movementDate: [new Date(), Validators.required],
    inventoryItemId: [this.data.defaultItemId || "", Validators.required],
    farmId: [this.data.defaultFarmId || "", Validators.required],
    storageLocationId: [this.data.defaultLocationId || "", Validators.required],
    destinationFarmId: [""],
    destinationStorageLocationId: [""],
    adjustmentType: ["AdjustmentIn"],
    quantity: [null as number | null, [Validators.required, Validators.min(0.0001)]],
    referenceNumber: [""],
    notes: [""],
  });

  ngOnInit(): void {
    if (this.isTransfer()) {
      this.form.controls.destinationFarmId.setValidators(Validators.required);
      this.form.controls.destinationStorageLocationId.setValidators(Validators.required);
    }

    if (this.data.operationType === "ADJUSTMENT") {
      this.form.controls.notes.setValidators(Validators.required);
    }

    this.loadFarms();
    this.loadItems();

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

  selectedUnitSymbol(): string {
    const itemId = this.form.controls.inventoryItemId.value;
    if (!itemId) return "";
    const item = this.items().find((i) => i.id === itemId);
    return item ? (item.stockUnitSymbol || item.stockUnitCode) : "";
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
    if (this.form.invalid || this.isSubmitting()) return;

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
