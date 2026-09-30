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
import { Router } from "@angular/router";
import { debounceTime, distinctUntilChanged, finalize, merge } from "rxjs";

import { PermissionService } from "../../../../core/auth/permission.service";
import { FarmManagementService } from "../../../../core/farm-management/farm-management.service";
import { Unit } from "../../../../core/farm-management/farm-management.models";
import { InventoryItem } from "../../../../core/inventory/inventory.models";
import { InventoryService } from "../../../../core/inventory/inventory.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import { InventoryItemEditorDialogComponent } from "../inventory-item-editor-dialog/inventory-item-editor-dialog.component";
import { InventorySubNavComponent } from "../inventory-sub-nav/inventory-sub-nav.component";

@Component({
  selector: "app-inventory-items-tab",
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
  templateUrl: "./inventory-items-tab.component.html",
  styleUrl: "./inventory-items-tab.component.scss",
})
export class InventoryItemsTabComponent implements OnInit {
  private readonly inventoryService = inject(InventoryService);
  private readonly farmService = inject(FarmManagementService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  readonly permissionService = inject(PermissionService);

  readonly columns = ["name", "sku", "category", "unit", "status", "actions"];
  readonly items = signal<readonly InventoryItem[]>([]);
  readonly units = signal<readonly Unit[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly isLoading = signal(false);

  readonly filterForm = this.fb.nonNullable.group({
    search: [""],
    status: ["all"],
  });

  ngOnInit(): void {
    this.loadUnits();

    merge(
      this.filterForm.controls.search.valueChanges.pipe(debounceTime(300), distinctUntilChanged()),
      this.filterForm.controls.status.valueChanges,
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.load();
      });

    this.load();
  }

  private loadUnits(): void {
    this.farmService.listUnits()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((u) => this.units.set(u));
  }

  load(): void {
    const status = this.filterForm.controls.status.value;
    this.isLoading.set(true);

    this.inventoryService
      .listItems(
        this.pageIndex() + 1,
        this.pageSize(),
        this.filterForm.controls.search.value,
        null,
        status === "all" ? null : status === "active",
      )
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (r) => {
          this.items.set(r.items);
          this.totalCount.set(r.totalCount);
        },
        error: (e) => this.snack.open(getApiErrorMessage(e, "Items could not be loaded."), "Dismiss"),
      });
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  openItemEditor(item?: InventoryItem): void {
    const dialogRef = this.dialog.open(InventoryItemEditorDialogComponent, {
      width: "480px",
      data: { item },
    });

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((res) => {
        if (res) this.load();
      });
  }

  /**
   * Navigates to the Stock Balances tab filtered by the selected inventory item ID.
   */
  viewBalances(item: InventoryItem): void {
    this.router.navigate(["/inventory/balances"], {
      queryParams: { inventoryItemId: item.id },
    });
  }

  toggleActive(item: InventoryItem, activate: boolean): void {
    const action$ = activate
      ? this.inventoryService.activateItem(item.id)
      : this.inventoryService.deactivateItem(item.id);

    action$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.snack.open(`Item ${activate ? "activated" : "deactivated"} successfully.`, "Dismiss", { duration: 3000 });
          this.load();
        },
        error: (e) => this.snack.open(getApiErrorMessage(e, "Operation failed."), "Dismiss"),
      });
  }
}
