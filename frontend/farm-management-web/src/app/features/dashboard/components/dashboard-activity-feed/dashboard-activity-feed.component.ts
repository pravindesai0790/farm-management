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
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { FarmManagementService } from "../../../../core/farm-management/farm-management.service";
import {
  DashboardRecentActivityResponse,
} from "../../../../core/farm-management/farm-management.models";

@Component({
  selector: "app-dashboard-activity-feed",
  standalone: true,
  imports: [
    DatePipe,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: "./dashboard-activity-feed.component.html",
  styleUrl: "./dashboard-activity-feed.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardActivityFeedComponent implements OnInit {
  readonly farmId = input<string>("");

  readonly activityData = signal<DashboardRecentActivityResponse | null>(null);
  readonly isLoading = signal<boolean>(true);

  private readonly farmService = inject(FarmManagementService);
  private readonly destroyRef = inject(DestroyRef);

  constructor() {
    effect(() => {
      const currentFarmId = this.farmId();
      this.loadActivity(currentFarmId);
    });
  }

  ngOnInit(): void {
    // Initial fetch handled by effect
  }

  loadActivity(farmId?: string): void {
    this.isLoading.set(true);
    this.farmService
      .getDashboardRecentActivity(farmId || undefined, 10)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.activityData.set(res);
          this.isLoading.set(false);
        },
        error: () => {
          this.isLoading.set(false);
        },
      });
  }

  getActivityIcon(eventType: string): string {
    const type = eventType.toUpperCase();
    if (type.includes("SPRAY")) return "science";
    if (type.includes("IRRIGATION")) return "water_drop";
    if (type.includes("FERTILIZER")) return "eco";
    if (type.includes("STOCK") || type.includes("INVENTORY")) return "inventory_2";
    if (type.includes("ATTENDANCE") || type.includes("LABOR")) return "engineering";
    if (type.includes("EXPENSE") || type.includes("INVOICE") || type.includes("PAYMENT")) return "receipt";
    return "history";
  }

  getActivityTypeClass(eventType: string): string {
    const type = eventType.toUpperCase();
    if (type.includes("SPRAY")) return "icon-spray";
    if (type.includes("IRRIGATION")) return "icon-irrigation";
    if (type.includes("FERTILIZER")) return "icon-fertilizer";
    if (type.includes("STOCK") || type.includes("INVENTORY")) return "icon-stock";
    if (type.includes("ATTENDANCE") || type.includes("LABOR")) return "icon-labor";
    if (type.includes("EXPENSE") || type.includes("INVOICE")) return "icon-financial";
    return "icon-default";
  }
}
