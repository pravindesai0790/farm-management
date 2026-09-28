import { DatePipe } from "@angular/common";
import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, inject, signal } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatDialog, MatDialogModule } from "@angular/material/dialog";
import { MatIconModule } from "@angular/material/icon";
import { MatMenuModule } from "@angular/material/menu";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatTableModule } from "@angular/material/table";
import { MatTooltipModule } from "@angular/material/tooltip";
import { RouterLink } from "@angular/router";
import { PermissionService } from "../../../../core/auth/permission.service";
import { CropCycle, CropCycleLifecycle, CropCycleStage } from "../../../../core/farm-management/farm-management.models";
import { CropCycleStageActionDialogComponent, StageActionMode } from "../../dialogs/crop-cycle-stage-action-dialog.component";
import { CropCycleStagePlannedDatesDialogComponent } from "../../dialogs/crop-cycle-stage-planned-dates-dialog.component";

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
  private readonly dialog = inject(MatDialog);

  @Input({ required: true }) cycle: CropCycle | null = null;
  @Input() lifecycle: CropCycleLifecycle | null = null;
  @Input() isLoading = false;

  @Output() startCycle = new EventEmitter<void>();
  @Output() completeCycle = new EventEmitter<void>();
  @Output() stageActionCompleted = new EventEmitter<void>();

  readonly isActionProcessing = signal(false);

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

  onCompleteCycle(): void {
    this.completeCycle.emit();
  }

  canComplete(stage: CropCycleStage): boolean {
    return (
      this.cycle?.status === "ACTIVE" &&
      stage.status === "IN_PROGRESS" &&
      this.permissionService.has("CropCycleLifecycle.UpdateStage")
    );
  }

  canSkip(stage: CropCycleStage): boolean {
    return (
      this.cycle?.status === "ACTIVE" &&
      stage.status === "IN_PROGRESS" &&
      this.permissionService.has("CropCycleLifecycle.SkipStage")
    );
  }

  canReopen(stage: CropCycleStage): boolean {
    if (this.cycle?.status !== "ACTIVE" || !this.permissionService.has("CropCycleLifecycle.ReopenStage")) {
      return false;
    }
    if (stage.status !== "COMPLETED" && stage.status !== "SKIPPED") {
      return false;
    }
    if (!this.lifecycle?.stages) return true;

    return this.lifecycle.stages.every(
      (s) => s.sequenceNumber >= stage.sequenceNumber || s.status === "COMPLETED" || s.status === "SKIPPED",
    );
  }

  canOverride(stage: CropCycleStage): boolean {
    return (
      this.cycle?.status === "ACTIVE" &&
      this.permissionService.has("CropCycleLifecycle.OverrideStage")
    );
  }

  canUpdatePlannedDates(stage: CropCycleStage): boolean {
    return (
      this.cycle?.status === "ACTIVE" &&
      this.permissionService.has("CropCycleLifecycle.UpdateStage")
    );
  }

  hasMenuActions(stage: CropCycleStage): boolean {
    return this.canUpdatePlannedDates(stage) || this.canSkip(stage) || this.canOverride(stage);
  }

  hasAnyAction(stage: CropCycleStage): boolean {
    return this.canComplete(stage) || this.canReopen(stage) || this.hasMenuActions(stage);
  }

  openActionDialog(stage: CropCycleStage, actionMode: StageActionMode): void {
    if (this.isLoading || this.isActionProcessing()) return;

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
      width: "500px",
      disableClose: true,
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result?.success) {
        this.stageActionCompleted.emit();
      }
    });
  }

  openEditPlannedDatesDialog(stage: CropCycleStage): void {
    if (this.isLoading || this.isActionProcessing()) return;

    const stages = this.lifecycle?.stages || [];
    const prevStage = stages
      .filter((s) => s.sequenceNumber < stage.sequenceNumber)
      .sort((a, b) => b.sequenceNumber - a.sequenceNumber)[0];

    const nextStage = stages
      .filter((s) => s.sequenceNumber > stage.sequenceNumber)
      .sort((a, b) => a.sequenceNumber - b.sequenceNumber)[0];

    const dialogRef = this.dialog.open(CropCycleStagePlannedDatesDialogComponent, {
      data: {
        stageId: stage.id,
        stageName: stage.stageName,
        sequenceNumber: stage.sequenceNumber,
        currentStatus: stage.status,
        plannedStartDate: stage.plannedStartDate,
        plannedEndDate: stage.plannedEndDate,
        actualStartDate: stage.actualStartDate,
        actualEndDate: stage.actualEndDate,
        expectedDurationDays: stage.expectedDurationDays,
        prevStageName: prevStage?.stageName,
        prevStagePlannedStartDate: prevStage?.plannedStartDate,
        nextStageName: nextStage?.stageName,
        nextStagePlannedEndDate: nextStage?.plannedEndDate,
      },
      width: "520px",
      disableClose: true,
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result?.success) {
        this.stageActionCompleted.emit();
      }
    });
  }
}
