import { Component, DestroyRef, OnInit, inject, signal } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatSelectModule } from "@angular/material/select";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatSnackBar } from "@angular/material/snack-bar";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";

import { FarmManagementService } from "../../../../core/farm-management/farm-management.service";
import { Unit } from "../../../../core/farm-management/farm-management.models";
import { InventoryItem } from "../../../../core/inventory/inventory.models";
import { InventoryService } from "../../../../core/inventory/inventory.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";

export interface InventoryItemEditorDialogData {
  readonly item?: InventoryItem;
}

@Component({
  selector: "app-inventory-item-editor-dialog",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: "./inventory-item-editor-dialog.component.html",
  styleUrl: "./inventory-item-editor-dialog.component.scss",
})
export class InventoryItemEditorDialogComponent implements OnInit {
  readonly data = inject<InventoryItemEditorDialogData | null>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<InventoryItemEditorDialogComponent>);
  private readonly fb = inject(FormBuilder);
  private readonly inventoryService = inject(InventoryService);
  private readonly farmService = inject(FarmManagementService);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  readonly units = signal<readonly Unit[]>([]);
  readonly isSubmitting = signal(false);

  readonly form = this.fb.group({
    name: [this.data?.item?.name || "", [Validators.required, Validators.maxLength(200)]],
    sku: [this.data?.item?.sku || "", [Validators.maxLength(100)]],
    category: [this.data?.item?.category || "", [Validators.maxLength(100)]],
    stockUnitId: [this.data?.item?.stockUnitId || "", [Validators.required]],
    description: [this.data?.item?.description || "", [Validators.maxLength(500)]],
  });

  ngOnInit(): void {
    this.farmService.listUnits()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((u) => this.units.set(u));
  }

  submit(): void {
    if (this.form.invalid || this.isSubmitting()) return;

    const val = this.form.getRawValue();
    this.isSubmitting.set(true);

    if (this.data?.item) {
      this.inventoryService.updateItem(this.data.item.id, {
        name: val.name!,
        sku: val.sku || null,
        category: val.category || null,
        stockUnitId: val.stockUnitId!,
        description: val.description || null,
      }).pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.snack.open("Inventory item updated.", "Dismiss", { duration: 3000 });
            this.dialogRef.close(true);
          },
          error: (e) => {
            this.isSubmitting.set(false);
            this.snack.open(getApiErrorMessage(e, "Update failed."), "Dismiss");
          },
        });
    } else {
      this.inventoryService.createItem({
        name: val.name!,
        sku: val.sku || null,
        category: val.category || null,
        stockUnitId: val.stockUnitId!,
        description: val.description || null,
      }).pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.snack.open("Inventory item created.", "Dismiss", { duration: 3000 });
            this.dialogRef.close(true);
          },
          error: (e) => {
            this.isSubmitting.set(false);
            this.snack.open(getApiErrorMessage(e, "Create failed."), "Dismiss");
          },
        });
    }
  }
}
