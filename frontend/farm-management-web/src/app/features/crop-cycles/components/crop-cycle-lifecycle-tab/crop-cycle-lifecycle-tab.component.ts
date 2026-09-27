import { DatePipe } from "@angular/common";
import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, inject } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatDialog, MatDialogModule } from "@angular/material/dialog";
import { MatIconModule } from "@angular/material/icon";
import { MatMenuModule } from "@angular/material/menu";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTableModule } from "@angular/material/table";
import { MatTooltipModule } from "@angular/material/tooltip";
import { RouterLink } from "@angular/router";
import { PermissionService } from "../../../../core/auth/permission.service";
import { CropCycle, CropCycleLifecycle, CropCycleStage } from "../../../../core/farm-management/farm-management.models";
import { FarmManagementService } from "../../../../core/farm-management/farm-management.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import { CropCycleStageActionDialogComponent, StageActionMode } from "../../dialogs/crop-cycle-stage-action-dialog.component";

@Component({
  selector: "app-crop-cycle-lifecycle-tab",
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatIconModule,
    MatMenuModule,
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
  private readonly service = inject(FarmManagementService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);

  @Input({ required: true }) cycle: CropCycle | null = null;
  @Input() lifecycle: CropCycleLifecycle | null = null;
  @Input() isLoading = false;

  @Output() startCycle = new EventEmitter<void>();
  @Output() stageActionCompleted = new EventEmitter<void>();

  readonly displayedColumns: string[] = [
    "sequence",
    "stageName",
    "status",
    "duration",
    "plannedTimeline",
    "actualTimeline",
    "notes",
    "actions",
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

  canReopen(stage: CropCycleStage): boolean {
    if (stage.status !== "COMPLETED" && stage.status !== "SKIPPED") {
      return false;
    }
    if (!this.lifecycle?.stages) return true;

    return this.lifecycle.stages.every(
      (s) => s.sequenceNumber >= stage.sequenceNumber || s.status === "COMPLETED" || s.status === "SKIPPED"
    );
  }

  openActionDialog(stage: CropCycleStage, actionMode: StageActionMode): void {
    const nextStage = this.lifecycle?.stages
      ?.filter((s) => s.sequenceNumber > stage.sequenceNumber && s.status === "NOT_STARTED")
      .sort((a, b) => a.sequenceNumber - b.sequenceNumber)[0];

    const dialogRef = this.dialog.open(CropCycleStageActionDialogComponent, {
      data: {
        actionMode,
        stageId: stage.id,
        stageName: stage.stageName,
        sequenceNumber: stage.sequenceNumber,
        currentStatus: stage.status,
        actualStartDate: stage.actualStartDate,
        actualEndDate: stage.actualEndDate,
        nextStageName: nextStage?.stageName,
        nextStageSequence: nextStage?.sequenceNumber,
      },
      width: "480px",
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (!result) return;
      this.executeStageAction(stage.id, result);
    });
  }

  private executeStageAction(stageId: string, result: any): void {
    let req;
    switch (result.actionMode) {
      case "complete":
        req = this.service.completeCycleStage(stageId, {
          actualEndDate: result.actualEndDate,
          notes: result.notes,
        });
        break;
      case "skip":
        req = this.service.skipCycleStage(stageId, {
          reason: result.reason,
          skipDate: result.skipDate,
        });
        break;
      case "reopen":
        req = this.service.reopenCycleStage(stageId, {
          reason: result.reason,
        });
        break;
      case "override":
        req = this.service.overrideCycleStage(stageId, {
          targetStatus: result.targetStatus,
          actualStartDate: result.actualStartDate,
          actualEndDate: result.actualEndDate,
          reason: result.reason,
        });
        break;
    }

    if (!req) return;

    req.subscribe({
      next: () => {
        this.snack.open(`Stage action standard executed successfully.`, "Dismiss", { duration: 3000 });
        this.stageActionCompleted.emit();
      },
      error: (e) =>
        this.snack.open(
          getApiErrorMessage(e, "Stage action could not be performed."),
          "Dismiss",
          { duration: 5000 },
        ),
    });
  }
}
