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
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatPaginatorModule, PageEvent } from "@angular/material/paginator";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatTableModule } from "@angular/material/table";
import { MatSnackBar } from "@angular/material/snack-bar";
import { RouterLink } from "@angular/router";
import { finalize } from "rxjs";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import {
  CropCycle,
  Farm,
  FarmArea,
  Plantation,
} from "../../core/farm-management/farm-management.models";
import { PermissionService } from "../../core/auth/permission.service";
import { getApiErrorMessage } from "../../core/models/api-error.model";

@Component({
  selector: "app-crop-cycles-page",
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    RouterLink,
  ],
  templateUrl: "./crop-cycles-page.component.html",
  styleUrl: "./crop-cycles-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CropCyclesPageComponent implements OnInit {
  private readonly service = inject(FarmManagementService);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);

  readonly columns = [
    "cycle",
    "farm",
    "area",
    "plantation",
    "dates",
    "status",
    "actions",
  ];

  readonly cycles = signal<readonly CropCycle[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly farms = signal<readonly Farm[]>([]);
  readonly areas = signal<readonly FarmArea[]>([]);
  readonly plantations = signal<readonly Plantation[]>([]);

  readonly farmId = signal("");
  readonly farmAreaId = signal("");
  readonly plantationId = signal("");
  readonly status = signal("");
  readonly seasonYear = signal<number | null>(null);
  readonly isLoading = signal(false);

  readonly hasActiveFilters = computed(
    () =>
      !!this.farmId() ||
      !!this.farmAreaId() ||
      !!this.plantationId() ||
      !!this.status() ||
      this.seasonYear() !== null,
  );

  ngOnInit(): void {
    this.service
      .listFarms(1, 100, "", true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => this.farms.set(res.items),
      });

    this.loadFilterPlantations();
    this.load();
  }

  loadFilterPlantations(): void {
    this.service
      .listPlantations(
        1,
        100,
        this.farmId() || undefined,
        this.farmAreaId() || undefined,
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => this.plantations.set(res.items),
      });
  }

  onFarmChange(newFarmId: string): void {
    this.farmId.set(newFarmId);
    this.farmAreaId.set("");
    this.plantationId.set("");
    this.pageIndex.set(0);
    if (newFarmId) {
      this.service
        .listAreas(newFarmId, true)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (areas) => this.areas.set(areas),
        });
    } else {
      this.areas.set([]);
    }
    this.loadFilterPlantations();
    this.load();
  }

  onAreaChange(newAreaId: string): void {
    this.farmAreaId.set(newAreaId);
    this.plantationId.set("");
    this.pageIndex.set(0);
    this.loadFilterPlantations();
    this.load();
  }

  onPlantationChange(newPlantationId: string): void {
    this.plantationId.set(newPlantationId);
    this.pageIndex.set(0);
    this.load();
  }

  onStatusChange(newStatus: string): void {
    this.status.set(newStatus);
    this.pageIndex.set(0);
    this.load();
  }

  onSeasonYearChange(newYear: number | null): void {
    this.seasonYear.set(newYear);
    this.pageIndex.set(0);
    this.load();
  }

  clearFilters(): void {
    this.farmId.set("");
    this.farmAreaId.set("");
    this.plantationId.set("");
    this.status.set("");
    this.seasonYear.set(null);
    this.areas.set([]);
    this.pageIndex.set(0);
    this.loadFilterPlantations();
    this.load();
  }

  pageChanged(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.service
      .listCycles(
        this.pageIndex() + 1,
        this.pageSize(),
        this.farmId() || undefined,
        this.farmAreaId() || undefined,
        this.plantationId() || undefined,
        this.status() || undefined,
        this.seasonYear() ?? undefined,
      )
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (r) => {
          this.cycles.set(r.items);
          this.totalCount.set(r.totalCount);
        },
        error: (e) => {
          this.snack.open(
            getApiErrorMessage(e, "Crop cycles could not be loaded."),
            "Dismiss",
            { duration: 5000 },
          );
        },
      });
  }
}
