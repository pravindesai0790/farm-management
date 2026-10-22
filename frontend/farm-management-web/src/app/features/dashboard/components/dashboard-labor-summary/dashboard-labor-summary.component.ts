import { ChangeDetectionStrategy, Component, computed, input } from "@angular/core";
import { DatePipe, DecimalPipe } from "@angular/common";
import { RouterLink } from "@angular/router";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatButtonModule } from "@angular/material/button";
import { MatTooltipModule } from "@angular/material/tooltip";
import { FarmLaborDashboard } from "../../../../core/farm-management/farm-management.models";

@Component({
  selector: "app-dashboard-labor-summary",
  standalone: true,
  imports: [DecimalPipe, DatePipe, RouterLink, MatCardModule, MatIconModule, MatButtonModule, MatTooltipModule],
  templateUrl: "./dashboard-labor-summary.component.html",
  styleUrl: "./dashboard-labor-summary.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardLaborSummaryComponent {
  readonly labor = input<FarmLaborDashboard | null | undefined>();
  readonly selectedFarmId = input<string>("");

  readonly turnoutPercentage = computed(() => {
    const l = this.labor();
    if (!l || l.assignedWorkersCount === 0) return 0;
    return Math.round((l.workedWorkersCount / l.assignedWorkersCount) * 100);
  });

  readonly maxTrendExpense = computed(() => {
    const l = this.labor();
    if (!l || !l.recentDaysTrend || l.recentDaysTrend.length === 0) return 1;
    const max = Math.max(...l.recentDaysTrend.map((t) => t.wageExpense));
    return max > 0 ? max : 1;
  });
}
