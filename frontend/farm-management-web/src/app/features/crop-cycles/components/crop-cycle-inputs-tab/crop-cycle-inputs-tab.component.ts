import { CommonModule, DatePipe, DecimalPipe } from "@angular/common";
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  Input,
  OnChanges,
  SimpleChanges,
  computed,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormsModule } from "@angular/forms";
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
import { MatTooltipModule } from "@angular/material/tooltip";
import { Router } from "@angular/router";
import { finalize } from "rxjs";

import { PermissionService } from "../../../../core/auth/permission.service";
import { CropCycle } from "../../../../core/farm-management/farm-management.models";
import { StockMovement } from "../../../../core/inventory/inventory.models";
import { InventoryService } from "../../../../core/inventory/inventory.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import { StockMovementReversalDialogComponent } from "../../../inventory/components/stock-movement-reversal-dialog/stock-movement-reversal-dialog.component";
import { StockOperationDialogComponent } from "../../../inventory/components/stock-operation-dialog/stock-operation-dialog.component";

export interface AppliedInputSummary {
  readonly itemId: string;
  readonly itemName: string;
  readonly itemSku?: string | null;
  readonly category: string;
  readonly unitSymbol: string;
  readonly netQuantity: number;
  readonly issueCount: number;
  readonly lastAppliedDate: string;
  readonly stages: readonly string[];
}

@Component({
  selector: "app-crop-cycle-inputs-tab",
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    DecimalPipe,
    FormsModule,
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
    MatTooltipModule,
  ],
  templateUrl: "./crop-cycle-inputs-tab.component.html",
  styleUrl: "./crop-cycle-inputs-tab.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CropCycleInputsTabComponent implements OnChanges {
  private readonly inventoryService = inject(InventoryService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly permissionService = inject(PermissionService);

  @Input({ required: true }) cycle: CropCycle | null = null;

  readonly movements = signal<readonly StockMovement[]>([]);
  readonly isLoading = signal(false);
  readonly selectedCategory = signal<string>("all");
  readonly searchQuery = signal<string>("");

  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);

  readonly displayedColumns: string[] = [
    "date",
    "item",
    "stage",
    "quantity",
    "location",
    "activity",
    "notes",
    "status",
    "actions",
  ];

  readonly categories = computed(() => {
    const list = this.movements();
    const set = new Set<string>();
    for (const m of list) {
      if (m.inventoryItemCategory) {
        set.add(m.inventoryItemCategory);
      }
    }
    return Array.from(set).sort();
  });

  readonly summaryItems = computed<readonly AppliedInputSummary[]>(() => {
    const list = this.movements();
    const map = new Map<string, {
      itemId: string;
      itemName: string;
      itemSku?: string | null;
      category: string;
      unitSymbol: string;
      netQuantity: number;
      issueCount: number;
      lastAppliedDate: string;
      stagesSet: Set<string>;
    }>();

    for (const m of list) {
      const key = m.inventoryItemId;
      let existing = map.get(key);
      if (!existing) {
        existing = {
          itemId: m.inventoryItemId,
          itemName: m.inventoryItemName,
          itemSku: m.inventoryItemSku,
          category: m.inventoryItemCategory || "General Input",
          unitSymbol: m.stockUnitSymbol || m.stockUnitCode || "",
          netQuantity: 0,
          issueCount: 0,
          lastAppliedDate: m.movementDate,
          stagesSet: new Set<string>(),
        };
        map.set(key, existing);
      }

      if (m.cropCycleStageName) {
        existing.stagesSet.add(m.cropCycleStageName);
      }

      if (m.movementDate > existing.lastAppliedDate) {
        existing.lastAppliedDate = m.movementDate;
      }

      const isRev = (m.movementTypeName || m.movementType || "").toLowerCase().includes("reversal") || m.reversedMovementId;

      if (!isRev && !m.isReversed) {
        existing.netQuantity += m.quantity;
        existing.issueCount += 1;
      } else if (isRev) {
        existing.netQuantity -= m.quantity;
      }
    }

    return Array.from(map.values()).map((val) => ({
      itemId: val.itemId,
      itemName: val.itemName,
      itemSku: val.itemSku,
      category: val.category,
      unitSymbol: val.unitSymbol,
      netQuantity: Math.max(0, val.netQuantity),
      issueCount: val.issueCount,
      lastAppliedDate: val.lastAppliedDate,
      stages: Array.from(val.stagesSet),
    }));
  });

  readonly filteredMovements = computed(() => {
    let list = this.movements();
    const cat = this.selectedCategory();
    const query = this.searchQuery().trim().toLowerCase();

    if (cat !== "all") {
      list = list.filter((m) => m.inventoryItemCategory === cat);
    }

    if (query) {
      list = list.filter(
        (m) =>
          m.inventoryItemName.toLowerCase().includes(query) ||
          (m.referenceNumber && m.referenceNumber.toLowerCase().includes(query)) ||
          (m.notes && m.notes.toLowerCase().includes(query)) ||
          (m.cropCycleStageName && m.cropCycleStageName.toLowerCase().includes(query))
      );
    }

    return list;
  });

  readonly kpis = computed(() => {
    const list = this.movements();
    const summaries = this.summaryItems();
    const distinctCount = summaries.length;
    const totalApplications = list.filter(
      (m) => !m.isReversed && !((m.movementTypeName || m.movementType || "").toLowerCase().includes("reversal"))
    ).length;

    let latestDate = "—";
    if (list.length > 0) {
      const dates = list.map((m) => m.movementDate).sort();
      latestDate = dates[dates.length - 1];
    }

    return {
      distinctCount,
      totalApplications,
      latestDate,
    };
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes["cycle"] && this.cycle) {
      this.load();
    }
  }

  load(): void {
    if (!this.cycle?.id) return;

    this.isLoading.set(true);

    this.inventoryService
      .getLedger(
        this.pageIndex() + 1,
        this.pageSize(),
        null,
        null,
        null,
        null,
        null,
        null,
        this.cycle.id
      )
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false))
      )
      .subscribe({
        next: (res) => {
          this.movements.set(res.items);
          this.totalCount.set(res.totalCount);
        },
        error: (err) => {
          this.snack.open(
            getApiErrorMessage(err, "Failed to load cycle inputs."),
            "Dismiss",
            { duration: 5000 }
          );
        },
      });
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  canIssueInputs(): boolean {
    if (!this.cycle) return false;
    const canCreate = this.permissionService.has("InventoryTransaction.Create");
    const validStatus =
      this.cycle.status === "PLANNED" ||
      this.cycle.status === "ACTIVE" ||
      this.cycle.status === "HARVESTED";
    return canCreate && validStatus;
  }

  openIssueDialog(): void {
    if (!this.cycle) return;

    const dialogRef = this.dialog.open(StockOperationDialogComponent, {
      width: "640px",
      data: {
        operationType: "ISSUE",
        defaultCropCycleId: this.cycle.id,
        defaultPlantationId: this.cycle.plantationId,
      },
    });

    dialogRef
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((res) => {
        if (res) {
          this.snack.open("Input issued to crop cycle successfully.", "Dismiss", {
            duration: 3000,
          });
          this.load();
        }
      });
  }

  canReverse(m: StockMovement): boolean {
    if (!m) return false;
    if (m.isReversed) return false;
    const type = (m.movementTypeName || m.movementType || "").toLowerCase();
    if (type.includes("reversal") || m.reversedMovementId) return false;
    return this.permissionService.has("InventoryTransaction.Create");
  }

  openReversalDialog(movement: StockMovement): void {
    const dialogRef = this.dialog.open(StockMovementReversalDialogComponent, {
      width: "520px",
      data: { movement },
    });

    dialogRef
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((res) => {
        if (res) {
          this.snack.open("Transaction reversed successfully.", "Dismiss", {
            duration: 3000,
          });
          this.load();
        }
      });
  }

  navigateToLedger(): void {
    if (!this.cycle?.id) return;
    this.router.navigate(["/inventory/stock"], {
      queryParams: { cropCycleId: this.cycle.id, tab: 1 },
    });
  }

  getCategoryBadgeClass(category?: string | null): string {
    if (!category) return "cat-general";
    const cat = category.toLowerCase();
    if (cat.includes("seed") || cat.includes("plant")) return "cat-seed";
    if (cat.includes("fertilizer") || cat.includes("soil") || cat.includes("npk") || cat.includes("nutrient")) return "cat-fertilizer";
    if (cat.includes("chemical") || cat.includes("pesticide") || cat.includes("herbicide") || cat.includes("spray")) return "cat-chemical";
    if (cat.includes("irrigation") || cat.includes("plumb")) return "cat-irrigation";
    if (cat.includes("harvest") || cat.includes("storage")) return "cat-harvest";
    if (cat.includes("safety") || cat.includes("protect")) return "cat-safety";
    if (cat.includes("tool") || cat.includes("equipment")) return "cat-tools";
    if (cat.includes("fuel") || cat.includes("oil")) return "cat-fuel";
    return "cat-general";
  }
}
