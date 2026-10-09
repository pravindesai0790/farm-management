import { CommonModule } from "@angular/common";
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatChipsModule } from "@angular/material/chips";
import { MatDialog } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatPaginatorModule, PageEvent } from "@angular/material/paginator";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTableModule } from "@angular/material/table";
import { MatTooltipModule } from "@angular/material/tooltip";
import { finalize } from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import { getApiErrorMessage } from "../../../core/models/api-error.model";
import {
  PlantProtectionProductQuery,
  PlantProtectionProductResponse,
} from "../../../core/plant-protection/plant-protection.models";
import { PlantProtectionService } from "../../../core/plant-protection/plant-protection.service";
import { ProductTypeResponse } from "../../../core/sprays/spray.models";
import { SprayService } from "../../../core/sprays/spray.service";
import { PlantProtectionProductEditorDialogComponent } from "./dialogs/plant-protection-product-editor-dialog.component";

@Component({
  selector: "app-plant-protection-product-list-page",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule,
  ],
  templateUrl: "./plant-protection-product-list-page.component.html",
  styleUrl: "./plant-protection-product-list-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlantProtectionProductListPageComponent implements OnInit {
  private readonly plantProtectionService = inject(PlantProtectionService);
  private readonly sprayService = inject(SprayService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);

  readonly displayedColumns = [
    "name",
    "type",
    "activeIngredient",
    "manufacturer",
    "stockUnit",
    "usage",
    "status",
    "actions",
  ];

  readonly items = signal<readonly PlantProtectionProductResponse[]>([]);
  readonly productTypes = signal<readonly ProductTypeResponse[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  // Filters
  readonly search = signal("");
  readonly productTypeId = signal("");
  readonly status = signal("all");

  private searchDebounceTimer?: any;

  ngOnInit(): void {
    this.loadProductTypes();
    this.load();
  }

  private loadProductTypes(): void {
    this.sprayService
      .getProductTypes()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (types) => this.productTypes.set(types),
      });
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    let activeFilter: boolean | null = null;
    if (this.status() === "active") activeFilter = true;
    else if (this.status() === "inactive") activeFilter = false;

    const query: PlantProtectionProductQuery = {
      page: this.pageIndex() + 1,
      pageSize: this.pageSize(),
      search: this.search().trim() || null,
      productTypeId: this.productTypeId() || null,
      isActive: activeFilter,
    };

    this.plantProtectionService
      .listProducts(query)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (res) => {
          this.items.set(res.items);
          this.totalCount.set(res.totalCount);
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to load plant protection products");
          this.errorMessage.set(msg);
          this.snack.open(msg, "Dismiss", { duration: 4000 });
        },
      });
  }

  onSearchChange(term: string): void {
    this.search.set(term);
    clearTimeout(this.searchDebounceTimer);
    this.searchDebounceTimer = setTimeout(() => {
      this.pageIndex.set(0);
      this.load();
    }, 300);
  }

  onTypeChange(typeId: string): void {
    this.productTypeId.set(typeId);
    this.pageIndex.set(0);
    this.load();
  }

  onStatusChange(status: string): void {
    this.status.set(status);
    this.pageIndex.set(0);
    this.load();
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  openCreateDialog(): void {
    const dialogRef = this.dialog.open(PlantProtectionProductEditorDialogComponent, {
      width: "560px",
      data: {},
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.load();
      }
    });
  }

  openEditDialog(product: PlantProtectionProductResponse): void {
    const dialogRef = this.dialog.open(PlantProtectionProductEditorDialogComponent, {
      width: "560px",
      data: { product },
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.load();
      }
    });
  }

  toggleActive(product: PlantProtectionProductResponse): void {
    const isDeactivating = product.isActive;
    const action$ = isDeactivating
      ? this.plantProtectionService.deactivateProduct(product.id)
      : this.plantProtectionService.activateProduct(product.id);

    action$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.snack.open(
          `Product profile "${product.inventoryItemName}" ${isDeactivating ? "deactivated" : "activated"} successfully.`,
          "Dismiss",
          { duration: 4000 },
        );
        this.load();
      },
      error: (err) => {
        const msg = getApiErrorMessage(
          err,
          `Failed to ${isDeactivating ? "deactivate" : "activate"} product profile`,
        );
        this.snack.open(msg, "Dismiss", { duration: 5000 });
      },
    });
  }
}
