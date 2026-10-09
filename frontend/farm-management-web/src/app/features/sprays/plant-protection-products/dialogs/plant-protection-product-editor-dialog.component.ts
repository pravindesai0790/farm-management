import { CommonModule } from "@angular/common";
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import {
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTooltipModule } from "@angular/material/tooltip";
import { finalize, forkJoin } from "rxjs";

import { InventoryItem } from "../../../../core/inventory/inventory.models";
import { InventoryService } from "../../../../core/inventory/inventory.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import {
  CreatePlantProtectionProductRequest,
  PlantProtectionProductResponse,
  UpdatePlantProtectionProductRequest,
} from "../../../../core/plant-protection/plant-protection.models";
import { PlantProtectionService } from "../../../../core/plant-protection/plant-protection.service";
import { ProductTypeResponse } from "../../../../core/sprays/spray.models";
import { SprayService } from "../../../../core/sprays/spray.service";

export interface PlantProtectionProductEditorDialogData {
  readonly product?: PlantProtectionProductResponse;
  readonly preselectedInventoryItemId?: string;
}

@Component({
  selector: "app-plant-protection-product-editor-dialog",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: "./plant-protection-product-editor-dialog.component.html",
  styleUrl: "./plant-protection-product-editor-dialog.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlantProtectionProductEditorDialogComponent implements OnInit {
  readonly data = inject<PlantProtectionProductEditorDialogData | null>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<PlantProtectionProductEditorDialogComponent>);
  private readonly fb = inject(FormBuilder);
  private readonly plantProtectionService = inject(PlantProtectionService);
  private readonly sprayService = inject(SprayService);
  private readonly inventoryService = inject(InventoryService);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  readonly isEditMode = computed(() => !!this.data?.product);
  readonly hasCompletedSprayUsage = computed(() => !!this.data?.product?.hasCompletedSprayUsage);

  readonly isSubmitting = signal(false);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly productTypes = signal<readonly ProductTypeResponse[]>([]);
  readonly availableInventoryItems = signal<readonly InventoryItem[]>([]);

  readonly form = this.fb.group({
    inventoryItemId: [this.data?.product?.inventoryItemId || this.data?.preselectedInventoryItemId || "", [Validators.required]],
    productTypeId: [this.data?.product?.productTypeId || "", [Validators.required]],
    activeIngredient: [this.data?.product?.activeIngredient || "", [Validators.maxLength(200)]],
    manufacturer: [this.data?.product?.manufacturer || "", [Validators.maxLength(200)]],
    description: [this.data?.product?.description || "", [Validators.maxLength(1000)]],
  });

  ngOnInit(): void {
    this.loadInitialData();

    // If edit mode and item has completed spray usage, disable productTypeId
    if (this.isEditMode() && this.hasCompletedSprayUsage()) {
      this.form.controls.productTypeId.disable();
    }
  }

  private loadInitialData(): void {
    this.isLoading.set(true);

    const productTypes$ = this.sprayService.getProductTypes();

    if (!this.isEditMode()) {
      forkJoin({
        types: productTypes$,
        items: this.inventoryService.listItems(1, 100, undefined, undefined, true),
      })
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isLoading.set(false)),
        )
        .subscribe({
          next: (res) => {
            this.productTypes.set(res.types);
            this.availableInventoryItems.set(res.items.items);
          },
          error: (err) => {
            const msg = getApiErrorMessage(err, "Failed to load master lookup data");
            this.errorMessage.set(msg);
          },
        });
    } else {
      productTypes$
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isLoading.set(false)),
        )
        .subscribe({
          next: (types) => {
            this.productTypes.set(types);
          },
          error: (err) => {
            const msg = getApiErrorMessage(err, "Failed to load product types");
            this.errorMessage.set(msg);
          },
        });
    }
  }

  getSelectedInventoryItem(): InventoryItem | undefined {
    const id = this.form.controls.inventoryItemId.value;
    if (!id) return undefined;
    return this.availableInventoryItems().find((i) => i.id === id);
  }

  onSubmit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const val = this.form.getRawValue();

    if (this.isEditMode()) {
      const productId = this.data!.product!.id;
      const updatePayload: UpdatePlantProtectionProductRequest = {
        productTypeId: val.productTypeId!,
        activeIngredient: val.activeIngredient?.trim() || null,
        manufacturer: val.manufacturer?.trim() || null,
        description: val.description?.trim() || null,
      };

      this.plantProtectionService
        .updateProduct(productId, updatePayload)
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isSubmitting.set(false)),
        )
        .subscribe({
          next: (res) => {
            this.snack.open("Plant protection product updated successfully.", "Dismiss", { duration: 4000 });
            this.dialogRef.close(res);
          },
          error: (err) => {
            const msg = getApiErrorMessage(err, "Failed to update plant protection product profile");
            this.errorMessage.set(msg);
          },
        });
    } else {
      const createPayload: CreatePlantProtectionProductRequest = {
        inventoryItemId: val.inventoryItemId!,
        productTypeId: val.productTypeId!,
        activeIngredient: val.activeIngredient?.trim() || null,
        manufacturer: val.manufacturer?.trim() || null,
        description: val.description?.trim() || null,
      };

      this.plantProtectionService
        .createProduct(createPayload)
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isSubmitting.set(false)),
        )
        .subscribe({
          next: (res) => {
            this.snack.open("Plant protection product created successfully.", "Dismiss", { duration: 4000 });
            this.dialogRef.close(res);
          },
          error: (err) => {
            const msg = getApiErrorMessage(err, "Failed to create plant protection product profile");
            this.errorMessage.set(msg);
          },
        });
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
