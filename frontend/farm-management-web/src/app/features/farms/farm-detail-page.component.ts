import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTabsModule } from "@angular/material/tabs";
import { ActivatedRoute } from "@angular/router";
import { catchError, forkJoin, of } from "rxjs";
import { PermissionService } from "../../core/auth/permission.service";
import { BreadcrumbService } from "../../core/breadcrumb/breadcrumb.service";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import {
  DashboardSummaryResponse,
  Farm,
  FarmArea,
  Plantation,
} from "../../core/farm-management/farm-management.models";
import { getApiErrorMessage } from "../../core/models/api-error.model";
import { LaborActivity } from "../labor-activities/models/labor-activity.models";
import { LaborActivityService } from "../labor-activities/services/labor-activity.service";

import { FarmHeaderComponent } from "./components/farm-header/farm-header.component";
import { FarmKpiStripComponent } from "./components/farm-kpi-strip/farm-kpi-strip.component";
import { FarmOverviewTabComponent } from "./components/farm-overview-tab/farm-overview-tab.component";
import { FarmAreasTabComponent } from "./components/farm-areas-tab/farm-areas-tab.component";
import { FarmPlantationsTabComponent } from "./components/farm-plantations-tab/farm-plantations-tab.component";
import { FarmActivitiesTabComponent } from "./components/farm-activities-tab/farm-activities-tab.component";

@Component({
  selector: "app-farm-detail-page",
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTabsModule,
    FarmHeaderComponent,
    FarmKpiStripComponent,
    FarmOverviewTabComponent,
    FarmAreasTabComponent,
    FarmPlantationsTabComponent,
    FarmActivitiesTabComponent,
  ],
  templateUrl: "./farm-detail-page.component.html",
  styleUrl: "./farm-detail-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FarmDetailPageComponent implements OnInit {
  private readonly service = inject(FarmManagementService);
  private readonly laborActivityService = inject(LaborActivityService);
  private readonly route = inject(ActivatedRoute);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly breadcrumbService = inject(BreadcrumbService);

  readonly permissionService = inject(PermissionService);
  readonly farmId = this.route.snapshot.paramMap.get("id")!;

  readonly farm = signal<Farm | null>(null);
  readonly areas = signal<readonly FarmArea[]>([]);
  readonly summary = signal<DashboardSummaryResponse | null>(null);
  readonly plantations = signal<readonly Plantation[]>([]);
  readonly activities = signal<readonly LaborActivity[]>([]);

  readonly selectedTabIndex = signal<number>(0);
  readonly isLoading = signal(true);

  ngOnInit(): void {
    forkJoin({
      farm: this.service.getFarm(this.farmId),
      areas: this.service.listAreas(this.farmId).pipe(catchError(() => of([]))),
      summary: this.service.getDashboardSummary(this.farmId).pipe(catchError(() => of(null))),
      plantations: this.service
        .listPlantations(1, 100, this.farmId)
        .pipe(catchError(() => of({ items: [], totalCount: 0 }))),
      activities: this.laborActivityService
        .list({ farmId: this.farmId, page: 1, pageSize: 20 })
        .pipe(catchError(() => of({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0, hasPreviousPage: false, hasNextPage: false }))),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.farm.set(res.farm);
          this.areas.set(res.areas);
          this.summary.set(res.summary);
          this.plantations.set(res.plantations.items);
          this.activities.set(res.activities.items);

          this.breadcrumbService.setEntityName(res.farm.id, res.farm.name);
          this.breadcrumbService.setTrail([
            { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
            { label: "Farms", route: "/farms" },
            { label: res.farm.name },
          ]);
          this.isLoading.set(false);
        },
        error: (e) => {
          this.snack.open(
            getApiErrorMessage(e, "Farm details could not be loaded."),
            "Dismiss",
            { duration: 5000 },
          );
          this.isLoading.set(false);
        },
      });
  }

  onTabChange(index: number): void {
    this.selectedTabIndex.set(index);
  }

  selectTabByName(name: 'plots' | 'plantations' | 'activities'): void {
    switch (name) {
      case 'plots':
        this.selectedTabIndex.set(1);
        break;
      case 'plantations':
        this.selectedTabIndex.set(2);
        break;
      case 'activities':
        this.selectedTabIndex.set(3);
        break;
    }
  }
}
