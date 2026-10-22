import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  input,
  OnInit,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { DatePipe } from "@angular/common";
import { RouterLink } from "@angular/router";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatButtonModule } from "@angular/material/button";
import { MatChipsModule } from "@angular/material/chips";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatTooltipModule } from "@angular/material/tooltip";
import { FarmManagementService } from "../../../../core/farm-management/farm-management.service";
import {
  DashboardUpcomingWorkResponse,
  UpcomingWorkItem,
} from "../../../../core/farm-management/farm-management.models";

@Component({
  selector: "app-dashboard-upcoming-work",
  standalone: true,
  imports: [
    DatePipe,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: "./dashboard-upcoming-work.component.html",
  styleUrl: "./dashboard-upcoming-work.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardUpcomingWorkComponent implements OnInit {
  readonly farmId = input<string>("");

  readonly upcomingData = signal<DashboardUpcomingWorkResponse | null>(null);
  readonly isLoading = signal<boolean>(true);
  readonly selectedFilter = signal<string>("ALL");
  readonly daysHorizon = signal<number>(7);

  private readonly destroyRef = inject(DestroyRef);
  private readonly farmService = inject(FarmManagementService);

  constructor() {
    effect(() => {
      const fId = this.farmId();
      const horizon = this.daysHorizon();
      this.loadUpcoming(fId, horizon);
    });
  }

  ngOnInit(): void {
    // Initial fetch handled by effect
  }

  loadUpcoming(farmId?: string, horizon?: number): void {
    this.isLoading.set(true);
    const fId = farmId !== undefined ? farmId : this.farmId();
    const days = horizon !== undefined ? horizon : this.daysHorizon();
    this.farmService
      .getDashboardUpcomingWork(fId || undefined, days)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.upcomingData.set(res);
          this.isLoading.set(false);
        },
        error: () => {
          this.isLoading.set(false);
        },
      });
  }

  setFilter(filter: string): void {
    this.selectedFilter.set(filter);
  }

  toggleHorizon(): void {
    this.daysHorizon.update((d) => (d === 7 ? 14 : 7));
    this.loadUpcoming();
  }

  getFilteredItems(): readonly UpcomingWorkItem[] {
    const data = this.upcomingData()?.items || [];
    const filter = this.selectedFilter();
    if (filter === "ALL") return data;
    return data.filter((i) => i.activityType.toUpperCase() === filter);
  }

  getActivityIcon(type: string): string {
    switch (type.toUpperCase()) {
      case "SPRAY": return "science";
      case "IRRIGATION": return "water_drop";
      case "FERTILIZER": return "eco";
      case "LABOR": return "engineering";
      default: return "task_alt";
    }
  }

  getActivityTypeClass(type: string): string {
    switch (type.toUpperCase()) {
      case "SPRAY": return "type-spray";
      case "IRRIGATION": return "type-irrigation";
      case "FERTILIZER": return "type-fertilizer";
      case "LABOR": return "type-labor";
      default: return "";
    }
  }
}
