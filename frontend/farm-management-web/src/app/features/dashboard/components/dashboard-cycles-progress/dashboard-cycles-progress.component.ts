import { ChangeDetectionStrategy, Component, input } from "@angular/core";
import { DatePipe } from "@angular/common";
import { RouterLink } from "@angular/router";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatButtonModule } from "@angular/material/button";
import { ActiveCycleSummary } from "../../../../core/farm-management/farm-management.models";

@Component({
  selector: "app-dashboard-cycles-progress",
  standalone: true,
  imports: [DatePipe, RouterLink, MatCardModule, MatIconModule, MatButtonModule],
  templateUrl: "./dashboard-cycles-progress.component.html",
  styleUrl: "./dashboard-cycles-progress.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardCyclesProgressComponent {
  readonly cycles = input.required<readonly ActiveCycleSummary[]>();
}
