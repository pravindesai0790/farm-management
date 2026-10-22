import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { DecimalPipe } from "@angular/common";
import { RouterLink } from "@angular/router";
import { FormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatSelectModule } from "@angular/material/select";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatTooltipModule } from "@angular/material/tooltip";

import { AuthService } from "../../core/auth/auth.service";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import {
  DashboardPrioritiesResponse,
  DashboardSummaryResponse,
  Farm,
} from "../../core/farm-management/farm-management.models";

import { DashboardKpiStripComponent } from "./components/dashboard-kpi-strip/dashboard-kpi-strip.component";
import { DashboardPrioritiesComponent } from "./components/dashboard-priorities/dashboard-priorities.component";
import { DashboardCyclesProgressComponent } from "./components/dashboard-cycles-progress/dashboard-cycles-progress.component";
import { DashboardCropDistributionComponent } from "./components/dashboard-crop-distribution/dashboard-crop-distribution.component";
import { DashboardUpcomingWorkComponent } from "./components/dashboard-upcoming-work/dashboard-upcoming-work.component";
import { DashboardLaborSummaryComponent } from "./components/dashboard-labor-summary/dashboard-labor-summary.component";
import { DashboardInventorySummaryComponent } from "./components/dashboard-inventory-summary/dashboard-inventory-summary.component";
import { DashboardFinancialSummaryComponent } from "./components/dashboard-financial-summary/dashboard-financial-summary.component";
import { DashboardActivityFeedComponent } from "./components/dashboard-activity-feed/dashboard-activity-feed.component";

const SELECTED_FARM_STORAGE_KEY = "fm_dashboard_selected_farm_id";

@Component({
  selector: "app-dashboard-page",
  standalone: true,
  imports: [
    DecimalPipe,
    RouterLink,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatFormFieldModule,
    MatTooltipModule,
    DashboardKpiStripComponent,
    DashboardPrioritiesComponent,
    DashboardCyclesProgressComponent,
    DashboardCropDistributionComponent,
    DashboardUpcomingWorkComponent,
    DashboardLaborSummaryComponent,
    DashboardInventorySummaryComponent,
    DashboardFinancialSummaryComponent,
    DashboardActivityFeedComponent,
  ],
  templateUrl: "./dashboard-page.component.html",
  styleUrl: "./dashboard-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPageComponent implements OnInit {
  readonly summary = signal<DashboardSummaryResponse | null>(null);
  readonly priorities = signal<DashboardPrioritiesResponse | null>(null);
  readonly farms = signal<readonly Farm[]>([]);
  readonly selectedFarmId = signal<string>("");
  readonly isLoading = signal<boolean>(true);
  readonly isLoadingPriorities = signal<boolean>(true);
  readonly error = signal<string | null>(null);

  private readonly destroyRef = inject(DestroyRef);
  private readonly snackBar = inject(MatSnackBar);
  private readonly farmService = inject(FarmManagementService);
  private readonly authService = inject(AuthService);

  readonly currentUser = this.authService.user;

  ngOnInit(): void {
    const savedFarmId = localStorage.getItem(SELECTED_FARM_STORAGE_KEY) || "";
    if (savedFarmId) {
      this.selectedFarmId.set(savedFarmId);
    }
    this.loadFarms();
    this.loadDashboardData(savedFarmId || undefined);
  }

  loadFarms(): void {
    this.farmService
      .listFarms(1, 100, "", true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.farms.set(response.items);
          const currentSelected = this.selectedFarmId();
          if (
            currentSelected &&
            !response.items.some((f) => f.id === currentSelected)
          ) {
            // Saved farm is no longer valid or accessible in this organization
            this.selectedFarmId.set("");
            localStorage.removeItem(SELECTED_FARM_STORAGE_KEY);
            this.loadDashboardData();
          }
        },
        error: () => {
          // Non-blocking: summary endpoint still functions for organization scope
        },
      });
  }

  loadDashboardData(farmId?: string): void {
    this.isLoading.set(true);
    this.isLoadingPriorities.set(true);
    this.error.set(null);

    const fId = farmId || undefined;

    // Load main summary
    this.farmService
      .getDashboardSummary(fId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.summary.set(response);
          this.isLoading.set(false);
        },
        error: () => {
          this.summary.set(null);
          this.isLoading.set(false);
          this.error.set("Unable to load farm dashboard metrics. Please check connection.");
          this.snackBar.open("Failed to load dashboard data.", "Dismiss", {
            duration: 5000,
          });
        },
      });

    // Load today's operational priorities
    this.farmService
      .getDashboardPriorities(fId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.priorities.set(response);
          this.isLoadingPriorities.set(false);
        },
        error: () => {
          this.priorities.set(null);
          this.isLoadingPriorities.set(false);
        },
      });
  }

  onFarmChange(farmId: string): void {
    this.selectedFarmId.set(farmId);
    if (farmId) {
      localStorage.setItem(SELECTED_FARM_STORAGE_KEY, farmId);
    } else {
      localStorage.removeItem(SELECTED_FARM_STORAGE_KEY);
    }
    this.loadDashboardData(farmId);
  }

  refresh(): void {
    this.loadDashboardData(this.selectedFarmId());
  }

  getSelectedFarmName(): string {
    const id = this.selectedFarmId();
    if (!id) return "All Farms";
    const farm = this.farms().find((f) => f.id === id);
    return farm ? farm.name : "Selected Farm";
  }
}
