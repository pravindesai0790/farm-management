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
import { RouterLink } from "@angular/router";
import { finalize } from "rxjs";
import { PermissionService } from "../../core/auth/permission.service";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import { Farm, FarmArea } from "../../core/farm-management/farm-management.models";

@Component({
  selector: "app-farm-areas-page",
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
  templateUrl: "./farm-areas-page.component.html",
  styleUrl: "./farm-areas-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FarmAreasPageComponent implements OnInit {
  private readonly service = inject(FarmManagementService);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);

  readonly columns = ["farm", "area", "size", "status", "actions"];
  readonly farms = signal<readonly Farm[]>([]);
  readonly selectedFarmId = signal("");
  readonly farmNames = signal<Record<string, string>>({});
  readonly areas = signal<readonly FarmArea[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly isLoading = signal(false);

  readonly hasActiveFilters = computed(() => !!this.selectedFarmId());

  ngOnInit(): void {
    this.service
      .listFarms(1, 100, "", null)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (farms) => {
          this.farms.set(farms.items);
          this.farmNames.set(
            Object.fromEntries(farms.items.map((farm) => [farm.id, farm.name])),
          );
        },
      });

    this.loadAreas();
  }

  loadAreas(): void {
    this.isLoading.set(true);
    this.service
      .listFarmAreas(
        this.pageIndex() + 1,
        this.pageSize(),
        this.selectedFarmId() || undefined,
      )
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (response) => {
          this.areas.set(response.items);
          this.totalCount.set(response.totalCount);
        },
        error: () => {
          this.areas.set([]);
          this.totalCount.set(0);
        },
      });
  }

  onFarmChange(farmId: string): void {
    this.selectedFarmId.set(farmId);
    this.pageIndex.set(0);
    this.loadAreas();
  }

  clearFilter(): void {
    this.selectedFarmId.set("");
    this.pageIndex.set(0);
    this.loadAreas();
  }

  pageChanged(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadAreas();
  }
}
