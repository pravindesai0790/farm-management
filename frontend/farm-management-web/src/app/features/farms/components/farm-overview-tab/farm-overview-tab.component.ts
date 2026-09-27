import { DatePipe, DecimalPipe } from "@angular/common";
import { ChangeDetectionStrategy, Component, computed, inject, input, output } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatTooltipModule } from "@angular/material/tooltip";
import { RouterLink } from "@angular/router";
import { PermissionService } from "../../../../core/auth/permission.service";
import {
  ActiveCycleStageSummary,
  ActiveCycleSummary,
  CropAllocationSummary,
  FarmLaborDashboard,
} from "../../../../core/farm-management/farm-management.models";
import { LaborActivity } from "../../../labor-activities/models/labor-activity.models";

@Component({
  selector: "app-farm-overview-tab",
  standalone: true,
  imports: [
    DatePipe,
    DecimalPipe,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatTooltipModule,
    RouterLink,
  ],
  templateUrl: "./farm-overview-tab.component.html",
  styleUrl: "./farm-overview-tab.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FarmOverviewTabComponent {
  readonly permissionService = inject(PermissionService);

  readonly farmId = input.required<string>();
  readonly activeCycles = input<readonly ActiveCycleSummary[]>([]);
  readonly cropAllocations = input<readonly CropAllocationSummary[]>([]);
  readonly labor = input<FarmLaborDashboard | null | undefined>(null);
  readonly todayActivities = input<readonly LaborActivity[]>([]);

  readonly selectTab = output<'plots' | 'plantations' | 'activities'>();

  readonly cropAllocationPercentages = computed(() => {
    const list = this.cropAllocations();
    if (!list || list.length === 0) return [];

    const colors = ["#7b1fa2", "#f57f17", "#d32f2f", "#2e7d32", "#0288d1"];
    return list.map((c, i) => ({
      ...c,
      color: colors[i % colors.length],
    }));
  });

  getCropIcon(cropName: string | undefined): string {
    const name = cropName?.toLowerCase() || "";
    if (name.includes("grape")) return "🍇";
    if (name.includes("corn") || name.includes("maize")) return "🌽";
    if (name.includes("tomato")) return "🍅";
    if (name.includes("wheat") || name.includes("paddy") || name.includes("rice")) return "🌾";
    if (name.includes("cotton")) return "☁️";
    if (name.includes("apple") || name.includes("fruit")) return "🍎";
    return "🌱";
  }

  getDaysRemainingText(expectedEndDateStr: string | null | undefined): string | null {
    if (!expectedEndDateStr) return null;
    const target = new Date(expectedEndDateStr);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    target.setHours(0, 0, 0, 0);

    const diffTime = target.getTime() - today.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    if (diffDays > 0) {
      return `${diffDays} days left`;
    } else if (diffDays === 0) {
      return "Harvest target today";
    } else {
      return `Overdue by ${Math.abs(diffDays)} days`;
    }
  }

  isStageCompleted(status: string | undefined): boolean {
    return status?.toUpperCase() === "COMPLETED";
  }

  isStageInProgress(status: string | undefined): boolean {
    const s = status?.toUpperCase();
    return s === "IN_PROGRESS" || s === "ACTIVE";
  }

  getActivityStatusClass(status: string): string {
    switch (status?.toUpperCase()) {
      case "COMPLETED":
        return "status-completed";
      case "CANCELLED":
        return "status-cancelled";
      default:
        return "status-draft";
    }
  }

  getActivityStatusLabel(status: string): string {
    switch (status?.toUpperCase()) {
      case "COMPLETED":
        return "Completed";
      case "CANCELLED":
        return "Cancelled";
      default:
        return "Draft";
    }
  }
}
