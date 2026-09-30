import { CommonModule } from "@angular/common";
import { Component, DestroyRef, OnInit, inject, signal } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormBuilder, ReactiveFormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatChipsModule } from "@angular/material/chips";
import { MatDialog, MatDialogModule } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatPaginatorModule, PageEvent } from "@angular/material/paginator";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTableModule } from "@angular/material/table";
import { finalize, merge } from "rxjs";

import { ActivatedRoute, Router } from "@angular/router";
import { PermissionService } from "../../../../core/auth/permission.service";
import { FarmManagementService } from "../../../../core/farm-management/farm-management.service";
import { Farm } from "../../../../core/farm-management/farm-management.models";
import { InventoryItem, StockBalance, StorageLocation } from "../../../../core/inventory/inventory.models";
import { InventoryService } from "../../../../core/inventory/inventory.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import { StockOperationDialogComponent, StockOperationType } from "../stock-operation-dialog/stock-operation-dialog.component";
import { InventorySubNavComponent } from "../inventory-sub-nav/inventory-sub-nav.component";

@Component({
  selector: "app-stock-overview-tab",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    InventorySubNavComponent,
  ],
  templateUrl: "./stock-overview-tab.component.html",
  styleUrl: "./stock-overview-tab.component.scss",
})
export class StockOverviewTabComponent implements OnInit {
  private readonly inventoryService = inject(InventoryService);
  private readonly farmService = inject(FarmManagementService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);

  readonly columns = ["farm", "location", "item", "onHand", "actions"];
  readonly balances = signal<readonly StockBalance[]>([]);
  readonly farms = signal<readonly Farm[]>([]);
  readonly locations = signal<readonly StorageLocation[]>([]);
  readonly items = signal<readonly InventoryItem[]>([]);

  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly isLoading = signal(false);

  readonly filterForm = this.fb.nonNullable.group({
    farmId: ["all"],
    storageLocationId: ["all"],
    inventoryItemId: ["all"],
  });

  ngOnInit(): void {
    this.loadFarms();
    this.loadItems();

    // Deep-linking: Pre-populate filters from route query parameters (e.g. from cross-navigation shortcuts)
    this.route.queryParams
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        const farmId = params['farmId'] || 'all';
        const locationId = params['storageLocationId'] || 'all';
        const itemId = params['inventoryItemId'] || 'all';

        if (farmId !== 'all') {
          this.loadLocations(farmId);
        }

        this.filterForm.patchValue({
          farmId,
          storageLocationId: locationId,
          inventoryItemId: itemId,
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
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.load();
      });

    this.load();
  }

  /**
   * Cross-navigation shortcut routing directly to the Stock Movement Ledger pre-filtered by item and location.
   */
  viewLedger(balance: StockBalance): void {
    this.router.navigate(["/inventory/ledger"], {
      queryParams: {
        farmId: balance.farmId,
        storageLocationId: balance.storageLocationId,
        inventoryItemId: balance.inventoryItemId,
      },
    });
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

  load(): void {
    const farmId = this.filterForm.controls.farmId.value;
    const locationId = this.filterForm.controls.storageLocationId.value;
    const itemId = this.filterForm.controls.inventoryItemId.value;

    this.isLoading.set(true);

    this.inventoryService
      .getOverview(
        this.pageIndex() + 1,
        this.pageSize(),
        farmId === "all" ? null : farmId,
        locationId === "all" ? null : locationId,
        itemId === "all" ? null : itemId,
      )
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (r) => {
          this.balances.set(r.items);
          this.totalCount.set(r.totalCount);
        },
        error: (e) => this.snack.open(getApiErrorMessage(e, "Stock balances could not be loaded."), "Dismiss"),
      });
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  openOperation(operationType: StockOperationType, b?: StockBalance): void {
    const dialogRef = this.dialog.open(StockOperationDialogComponent, {
      width: "480px",
      data: {
        operationType,
        defaultFarmId: b?.farmId,
        defaultLocationId: b?.storageLocationId,
        defaultItemId: b?.inventoryItemId,
      },
    });

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((res) => {
        if (res) this.load();
      });
  }
}
