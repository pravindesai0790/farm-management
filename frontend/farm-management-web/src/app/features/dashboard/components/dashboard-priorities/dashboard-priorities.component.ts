import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from "@angular/core";
import { DecimalPipe } from "@angular/common";
import { RouterLink } from "@angular/router";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatButtonModule } from "@angular/material/button";
import { MatTooltipModule } from "@angular/material/tooltip";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { DashboardPrioritiesResponse } from "../../../../core/farm-management/farm-management.models";

@Component({
  selector: "app-dashboard-priorities",
  standalone: true,
  imports: [
    DecimalPipe,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: "./dashboard-priorities.component.html",
  styleUrl: "./dashboard-priorities.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPrioritiesComponent {
  readonly priorities = input<DashboardPrioritiesResponse | null>(null);
  readonly isLoading = input<boolean>(false);
  readonly selectedFarmName = input<string>("All Farms");

  readonly refresh = output<void>();

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
