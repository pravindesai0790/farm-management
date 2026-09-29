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
import { Farm } from "../../../../core/farm-management/farm-management.models";
import { StorageLocation } from "../../../../core/inventory/inventory.models";
import { InventoryService } from "../../../../core/inventory/inventory.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";

export interface StorageLocationEditorDialogData {
  readonly location?: StorageLocation;
}

@Component({
  selector: "app-storage-location-editor-dialog",
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
  templateUrl: "./storage-location-editor-dialog.component.html",
  styleUrl: "./storage-location-editor-dialog.component.scss",
})
export class StorageLocationEditorDialogComponent implements OnInit {
  readonly data = inject<StorageLocationEditorDialogData | null>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<StorageLocationEditorDialogComponent>);
  private readonly fb = inject(FormBuilder);
  private readonly inventoryService = inject(InventoryService);
  private readonly farmService = inject(FarmManagementService);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  readonly farms = signal<readonly Farm[]>([]);
  readonly isSubmitting = signal(false);

  readonly form = this.fb.group({
    farmId: [this.data?.location?.farmId || "", [Validators.required]],
    name: [this.data?.location?.name || "", [Validators.required, Validators.maxLength(200)]],
    description: [this.data?.location?.description || "", [Validators.maxLength(500)]],
  });

  ngOnInit(): void {
    this.farmService.listFarms(1, 100, "", true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((r) => this.farms.set(r.items));
  }

  submit(): void {
    if (this.form.invalid || this.isSubmitting()) return;

    const val = this.form.getRawValue();
    this.isSubmitting.set(true);

    if (this.data?.location) {
      this.inventoryService.updateLocation(this.data.location.id, {
        name: val.name!,
        description: val.description || null,
      }).pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.snack.open("Storage location updated.", "Dismiss", { duration: 3000 });
            this.dialogRef.close(true);
          },
          error: (e) => {
            this.isSubmitting.set(false);
            this.snack.open(getApiErrorMessage(e, "Update failed."), "Dismiss");
          },
        });
    } else {
      this.inventoryService.createLocation({
        farmId: val.farmId!,
        name: val.name!,
        description: val.description || null,
      }).pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.snack.open("Storage location created.", "Dismiss", { duration: 3000 });
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
