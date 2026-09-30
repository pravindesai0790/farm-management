import { CommonModule } from "@angular/common";
import { Component, DestroyRef, OnInit, inject, signal } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormBuilder, ReactiveFormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatChipsModule } from "@angular/material/chips";
import { MatNativeDateModule } from "@angular/material/core";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatPaginatorModule, PageEvent } from "@angular/material/paginator";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTableModule } from "@angular/material/table";
import { finalize, merge } from "rxjs";

import { ActivatedRoute } from "@angular/router";
import { FarmManagementService } from "../../../../core/farm-management/farm-management.service";
import { Farm } from "../../../../core/farm-management/farm-management.models";
import { InventoryItem, StockMovement, StorageLocation } from "../../../../core/inventory/inventory.models";
import { InventoryService } from "../../../../core/inventory/inventory.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import { formatDateOnly } from "../../../../core/utils/date.utils";
import { InventorySubNavComponent } from "../inventory-sub-nav/inventory-sub-nav.component";

@Component({
  selector: "app-stock-ledger-tab",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatNativeDateModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    InventorySubNavComponent,
  ],
  templateUrl: "./stock-ledger-tab.component.html",
  styleUrl: "./stock-ledger-tab.component.scss",
})
export class StockLedgerTabComponent implements OnInit {
  private readonly inventoryService = inject(InventoryService);
  private readonly farmService = inject(FarmManagementService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  readonly columns = ["date", "type", "item", "location", "quantity", "notes"];
  readonly movements = signal<readonly StockMovement[]>([]);
  readonly farms = signal<readonly Farm[]>([]);
  readonly locations = signal<readonly StorageLocation[]>([]);
  readonly items = signal<readonly InventoryItem[]>([]);

  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly isLoading = signal(false);

  readonly filterForm = this.fb.group({
    farmId: ["all"],
    storageLocationId: ["all"],
    inventoryItemId: ["all"],
    movementType: ["all"],
    fromDate: [null as Date | null],
    toDate: [null as Date | null],
  });

  ngOnInit(): void {
    this.loadFarms();
    this.loadItems();

    // Deep-linking: Pre-populate movement ledger filters from route query parameters (e.g. from View Ledger shortcut)
    this.route.queryParams
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        const farmId = params['farmId'] || 'all';
        const locationId = params['storageLocationId'] || 'all';
        const itemId = params['inventoryItemId'] || 'all';
        const type = params['movementType'] || 'all';

        if (farmId !== 'all') {
          this.loadLocations(farmId);
        }

        this.filterForm.patchValue({
          farmId,
          storageLocationId: locationId,
          inventoryItemId: itemId,
          movementType: type,
        }, { emitEvent: false });

        this.load();
      });

    this.filterForm.controls.farmId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((farmId) => {
        this.filterForm.controls.storageLocationId.setValue("all");
        if (farmId && farmId !== "all") {
          this.loadLocations(farmId);
        } else {
          this.locations.set([]);
        }
      });

    merge(
      this.filterForm.controls.farmId.valueChanges,
      this.filterForm.controls.storageLocationId.valueChanges,
      this.filterForm.controls.inventoryItemId.valueChanges,
      this.filterForm.controls.movementType.valueChanges,
      this.filterForm.controls.fromDate.valueChanges,
      this.filterForm.controls.toDate.valueChanges,
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.load();
      });

    this.load();
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

  private loadLocations(farmId: string): void {
    this.inventoryService.listLocations(1, 100, farmId, true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((r) => this.locations.set(r.items));
  }

  isIncoming(movement: StockMovement | string | number): boolean {
    const val = typeof movement === "object" && movement !== null
      ? (movement.movementTypeName || movement.movementType)
      : movement;

    const str = String(val).toLowerCase();
    return (
      str === "openingstock" ||
      str === "receipt" ||
      str === "adjustmentin" ||
      str === "transferin" ||
      str === "1" ||
      str === "2" ||
      str === "4" ||
      str === "6"
    );
  }

  getMovementBadgeClass(movement: StockMovement | string | number): string {
    if (this.isIncoming(movement)) return "badge-in";
    return "badge-out";
  }

  load(): void {
    const val = this.filterForm.getRawValue();

    this.isLoading.set(true);

    this.inventoryService
      .getLedger(
        this.pageIndex() + 1,
        this.pageSize(),
        val.farmId === "all" ? null : val.farmId,
        val.storageLocationId === "all" ? null : val.storageLocationId,
        val.inventoryItemId === "all" ? null : val.inventoryItemId,
        val.movementType === "all" ? null : val.movementType,
        val.fromDate ? formatDateOnly(val.fromDate) : null,
        val.toDate ? formatDateOnly(val.toDate) : null,
      )
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (r) => {
          this.movements.set(r.items);
          this.totalCount.set(r.totalCount);
        },
        error: (e) => this.snack.open(getApiErrorMessage(e, "Stock ledger could not be loaded."), "Dismiss"),
      });
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }
}
