import { ChangeDetectionStrategy, Component, computed, input } from "@angular/core";
import { DecimalPipe } from "@angular/common";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import {
  FarmLaborDashboard,
  KpiSummary,
} from "../../../../core/farm-management/farm-management.models";

@Component({
  selector: "app-dashboard-kpi-strip",
  standalone: true,
  imports: [DecimalPipe, MatCardModule, MatIconModule],
  templateUrl: "./dashboard-kpi-strip.component.html",
  styleUrl: "./dashboard-kpi-strip.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardKpiStripComponent {
  readonly kpi = input.required<KpiSummary>();
  readonly labor = input<FarmLaborDashboard | null | undefined>(null);
  readonly currentSeason = input<string | null>(null);

  readonly turnoutPercentage = computed(() => {
    const l = this.labor();
    if (!l || l.assignedWorkersCount === 0) return 0;
    return Math.round((l.workedWorkersCount / l.assignedWorkersCount) * 100);
  });
}
