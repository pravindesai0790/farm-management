import { DatePipe } from "@angular/common";
import { ChangeDetectionStrategy, Component, inject, input } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatTableModule } from "@angular/material/table";
import { RouterLink } from "@angular/router";
import { PermissionService } from "../../../../core/auth/permission.service";
import { LaborActivity } from "../../../labor-activities/models/labor-activity.models";

@Component({
  selector: "app-farm-activities-tab",
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatTableModule,
    RouterLink,
  ],
  templateUrl: "./farm-activities-tab.component.html",
  styleUrl: "./farm-activities-tab.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FarmActivitiesTabComponent {
  readonly permissionService = inject(PermissionService);

  readonly farmId = input.required<string>();
  readonly activities = input<readonly LaborActivity[]>([]);

  readonly displayedColumns: string[] = [
    "activity",
    "area",
    "date",
    "status",
    "actions",
  ];

  getStatusClass(status: string): string {
    switch (status?.toUpperCase()) {
      case "COMPLETED":
        return "status-completed";
      case "CANCELLED":
        return "status-cancelled";
      default:
        return "status-draft";
    }
  }

  getStatusLabel(status: string): string {
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
