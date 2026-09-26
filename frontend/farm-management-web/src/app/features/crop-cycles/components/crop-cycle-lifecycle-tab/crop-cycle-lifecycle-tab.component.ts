import { DatePipe } from "@angular/common";
import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, inject } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatTableModule } from "@angular/material/table";
import { MatTooltipModule } from "@angular/material/tooltip";
import { RouterLink } from "@angular/router";
import { PermissionService } from "../../../../core/auth/permission.service";
import { CropCycle, CropCycleLifecycle, CropCycleStage } from "../../../../core/farm-management/farm-management.models";

@Component({
  selector: "app-crop-cycle-lifecycle-tab",
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatTooltipModule,
    RouterLink,
  ],
  templateUrl: "./crop-cycle-lifecycle-tab.component.html",
  styleUrl: "./crop-cycle-lifecycle-tab.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CropCycleLifecycleTabComponent {
  readonly permissionService = inject(PermissionService);

  @Input({ required: true }) cycle: CropCycle | null = null;
  @Input() lifecycle: CropCycleLifecycle | null = null;
  @Input() isLoading = false;

  @Output() startCycle = new EventEmitter<void>();

  readonly displayedColumns: string[] = [
    "sequence",
    "stageName",
    "status",
    "duration",
    "plannedTimeline",
    "actualTimeline",
    "notes",
  ];

  getStatusClass(status: string | undefined): string {
    switch (status?.toUpperCase()) {
      case "IN_PROGRESS":
      case "ACTIVE":
        return "status-in-progress";
      case "COMPLETED":
        return "status-completed";
      case "SKIPPED":
        return "status-skipped";
      case "CANCELLED":
      case "CANCELED":
        return "status-cancelled";
      case "PLANNED":
      case "NOT_STARTED":
      default:
        return "status-not-started";
    }
  }

  getStatusLabel(status: string | undefined): string {
    switch (status?.toUpperCase()) {
      case "IN_PROGRESS":
        return "In Progress";
      case "COMPLETED":
        return "Completed";
      case "SKIPPED":
        return "Skipped";
      case "CANCELLED":
      case "CANCELED":
        return "Cancelled";
      case "NOT_STARTED":
        return "Not Started";
      case "PLANNED":
        return "Planned";
      case "ACTIVE":
        return "Active";
      case "HARVESTED":
        return "Harvested";
      default:
        return status || "—";
    }
  }

  onStartCycle(): void {
    this.startCycle.emit();
  }
}
