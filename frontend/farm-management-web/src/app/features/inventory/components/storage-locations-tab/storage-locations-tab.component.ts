import { CommonModule } from "@angular/common";
import { Component, DestroyRef, OnInit, inject, signal } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormBuilder, ReactiveFormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatDialog, MatDialogModule } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatPaginatorModule, PageEvent } from "@angular/material/paginator";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTableModule } from "@angular/material/table";
import { Router } from "@angular/router";
import { finalize, merge } from "rxjs";

import { PermissionService } from "../../../../core/auth/permission.service";
import { FarmManagementService } from "../../../../core/farm-management/farm-management.service";
import { Farm } from "../../../../core/farm-management/farm-management.models";
import { StorageLocation } from "../../../../core/inventory/inventory.models";
import { InventoryService } from "../../../../core/inventory/inventory.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import { StorageLocationEditorDialogComponent } from "../storage-location-editor-dialog/storage-location-editor-dialog.component";
import { InventorySubNavComponent } from "../inventory-sub-nav/inventory-sub-nav.component";

@Component({
  selector: "app-storage-locations-tab",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
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
  templateUrl: "./storage-locations-tab.component.html",
  styleUrl: "./storage-locations-tab.component.scss",
})
export class StorageLocationsTabComponent implements OnInit {
  private readonly inventoryService = inject(InventoryService);
  private readonly farmService = inject(FarmManagementService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  readonly permissionService = inject(PermissionService);

  readonly columns = ["name", "farm", "status", "actions"];
  readonly locations = signal<readonly StorageLocation[]>([]);
  readonly farms = signal<readonly Farm[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly isLoading = signal(false);

  readonly filterForm = this.fb.nonNullable.group({
    farmId: ["all"],
    status: ["all"],
  });

  ngOnInit(): void {
    this.loadFarms();

    merge(
      this.filterForm.controls.farmId.valueChanges,
      this.filterForm.controls.status.valueChanges,
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

  load(): void {
    const farmId = this.filterForm.controls.farmId.value;
    const status = this.filterForm.controls.status.value;
    this.isLoading.set(true);

    this.inventoryService
      .listLocations(
        this.pageIndex() + 1,
        this.pageSize(),
        farmId === "all" ? null : farmId,
        status === "all" ? null : status === "active",
      )
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (r) => {
          this.locations.set(r.items);
          this.totalCount.set(r.totalCount);
        },
        error: (e) => this.snack.open(getApiErrorMessage(e, "Storage locations could not be loaded."), "Dismiss"),
      });
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  openLocationEditor(location?: StorageLocation): void {
    const dialogRef = this.dialog.open(StorageLocationEditorDialogComponent, {
      width: "480px",
      data: { location },
    });

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((res) => {
        if (res) this.load();
      });
  }

  /**
   * Navigates to the Stock Balances tab filtered by the selected storage location and farm ID.
   */
  viewStock(loc: StorageLocation): void {
    this.router.navigate(["/inventory/balances"], {
      queryParams: {
        farmId: loc.farmId,
        storageLocationId: loc.id,
      },
    });
  }

  toggleActive(location: StorageLocation, activate: boolean): void {
    const action$ = activate
      ? this.inventoryService.activateLocation(location.id)
      : this.inventoryService.deactivateLocation(location.id);

    action$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.snack.open(`Storage location ${activate ? "activated" : "deactivated"} successfully.`, "Dismiss", { duration: 3000 });
          this.load();
        },
        error: (e) => this.snack.open(getApiErrorMessage(e, "Operation failed."), "Dismiss"),
      });
  }
}
