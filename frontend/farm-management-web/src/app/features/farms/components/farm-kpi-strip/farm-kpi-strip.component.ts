import { DecimalPipe } from "@angular/common";
import { ChangeDetectionStrategy, Component, computed, input } from "@angular/core";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import {
  CropAllocationSummary,
  FarmLaborDashboard,
  KpiSummary,
} from "../../../../core/farm-management/farm-management.models";

@Component({
  selector: "app-farm-kpi-strip",
  standalone: true,
  imports: [DecimalPipe, MatCardModule, MatIconModule],
  templateUrl: "./farm-kpi-strip.component.html",
  styleUrl: "./farm-kpi-strip.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FarmKpiStripComponent {
  readonly kpi = input.required<KpiSummary>();
  readonly season = input<string | null>(null);
  readonly cropAllocations = input<readonly CropAllocationSummary[]>([]);
  readonly labor = input<FarmLaborDashboard | null | undefined>(null);
  readonly activitiesCount = input<number>(0);

  readonly topCropNames = computed(() => {
    const list = this.cropAllocations();
    if (!list || list.length === 0) return "No active crops";
    return list.slice(0, 3).map((c) => c.cropName).join(" · ");
  });

  readonly laborTurnoutPercentage = computed(() => {
    const l = this.labor();
    if (!l || !l.assignedWorkersCount || l.assignedWorkersCount === 0) return 0;
    return Math.round((l.workedWorkersCount / l.assignedWorkersCount) * 100);
  });
}
