import { CommonModule } from "@angular/common";
import { Component, DestroyRef, OnInit, computed, inject, signal } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatNativeDateModule } from "@angular/material/core";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSnackBar } from "@angular/material/snack-bar";
import { finalize } from "rxjs";

import { StockBalance, StockMovement } from "../../../../core/inventory/inventory.models";
import { InventoryService } from "../../../../core/inventory/inventory.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import { formatDateOnly, parseDateOnly } from "../../../../core/utils/date.utils";

export interface StockMovementReversalDialogData {
  readonly movement: StockMovement;
}

@Component({
  selector: "app-stock-movement-reversal-dialog",
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
  ],
  templateUrl: "./stock-movement-reversal-dialog.component.html",
  styleUrl: "./stock-movement-reversal-dialog.component.scss",
})
export class StockMovementReversalDialogComponent implements OnInit {
  readonly data = inject<StockMovementReversalDialogData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<StockMovementReversalDialogComponent>);
  private readonly fb = inject(FormBuilder);
  private readonly inventoryService = inject(InventoryService);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  readonly movement = this.data.movement;
  readonly today = new Date();
  readonly minDate = parseDateOnly(this.movement.movementDate) || new Date();

  readonly currentBalance = signal<StockBalance | null>(null);
  readonly isLoadingBalance = signal(false);
  readonly isSubmitting = signal(false);

  readonly form = this.fb.group({
    reason: ["", [Validators.required, Validators.minLength(3), Validators.maxLength(500)]],
    reversalDate: [new Date(), [Validators.required]],
  });

  // Reversing stock addition types (OpeningStock, Receipt, AdjustmentIn, TransferIn) deducts stock from inventory
  readonly isStockDeduction = computed(() => {
    const type = (this.movement.movementTypeName || this.movement.movementType || "").toLowerCase();
    return (
      type === "openingstock" ||
      type === "receipt" ||
      type === "adjustmentin" ||
      type === "transferin"
    );
  });

  readonly availableQuantity = computed(() => this.currentBalance()?.quantityOnHand ?? 0);

  readonly isInsufficientStock = computed(() => {
    if (!this.isStockDeduction()) return false;
    if (this.isLoadingBalance()) return false;
    return this.availableQuantity() < this.movement.quantity;
  });

  ngOnInit(): void {
    if (this.isStockDeduction()) {
      this.isLoadingBalance.set(true);
      this.inventoryService
        .getBalance(this.movement.storageLocationId, this.movement.inventoryItemId)
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isLoadingBalance.set(false)),
        )
        .subscribe({
          next: (b) => this.currentBalance.set(b),
          error: () => this.currentBalance.set(null),
        });
    }
  }

  submit(): void {
    if (this.form.invalid || this.isSubmitting() || this.isInsufficientStock()) return;

    const val = this.form.getRawValue();
    const reversalDateStr = formatDateOnly(val.reversalDate!);

    this.isSubmitting.set(true);

    this.inventoryService
      .reverseMovement(this.movement.id, {
        reason: val.reason!.trim(),
        reversalDate: reversalDateStr,
      })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: () => {
          this.snack.open("Movement reversed successfully.", "Dismiss", { duration: 3000 });
          this.dialogRef.close(true);
        },
        error: (e: unknown) => {
          this.snack.open(getApiErrorMessage(e, "Movement reversal failed."), "Dismiss", { duration: 5000 });
        },
      });
  }
}
