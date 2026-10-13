import { CommonModule } from "@angular/common";
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatDialog } from "@angular/material/dialog";
import { MatDividerModule } from "@angular/material/divider";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTooltipModule } from "@angular/material/tooltip";
import { ActivatedRoute, Router, RouterModule } from "@angular/router";
import { finalize } from "rxjs";

import { BreadcrumbService } from "../../../core/breadcrumb/breadcrumb.service";
import { PermissionService } from "../../../core/auth/permission.service";
import { getApiErrorMessage } from "../../../core/models/api-error.model";
import {
  IrrigationDetailsResponse,
  IrrigationStatus,
} from "../../../core/irrigations/irrigation.models";
import { IrrigationService } from "../../../core/irrigations/irrigation.service";
import { CancelIrrigationDialogComponent } from "../dialogs/cancel-irrigation-dialog/cancel-irrigation-dialog.component";
import { CompleteIrrigationDialogComponent } from "../dialogs/complete-irrigation-dialog/complete-irrigation-dialog.component";
import { ScheduleIrrigationDialogComponent } from "../dialogs/schedule-irrigation-dialog/schedule-irrigation-dialog.component";
import { StartIrrigationDialogComponent } from "../dialogs/start-irrigation-dialog/start-irrigation-dialog.component";

@Component({
  selector: "app-irrigation-detail-page",
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatButtonModule,
    MatCardModule,
    MatDividerModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: "./irrigation-detail-page.component.html",
  styleUrl: "./irrigation-detail-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IrrigationDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly irrigationService = inject(IrrigationService);
  private readonly breadcrumbService = inject(BreadcrumbService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);

  readonly id = this.route.snapshot.paramMap.get("id") ?? "";

  readonly irrigation = signal<IrrigationDetailsResponse | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  readonly isTerminal = computed(() => {
    const s = this.irrigation()?.status;
    return s === "Completed" || s === "Cancelled";
  });

  ngOnInit(): void {
    if (!this.id) {
      this.errorMessage.set("No irrigation event ID specified.");
      this.isLoading.set(false);
      return;
    }

    this.loadIrrigation();
  }

  loadIrrigation(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.irrigationService
      .getIrrigation(this.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (res) => {
          this.irrigation.set(res);
          this.breadcrumbService.setEntityName(
            this.id,
            `${res.farmName} - ${res.farmAreaName}`,
          );
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to load irrigation event details.");
          this.errorMessage.set(msg);
          this.snack.open(msg, "Dismiss", { duration: 4000 });
        },
      });
  }

  onEdit(): void {
    this.router.navigate(["/irrigations", this.id, "edit"]);
  }

  onSchedule(): void {
    const item = this.irrigation();
    if (!item) return;

    const ref = this.dialog.open(ScheduleIrrigationDialogComponent, {
      width: "500px",
      data: {
        irrigationId: item.id,
        farmName: item.farmName,
        farmAreaName: item.farmAreaName,
        currentScheduledAt: item.scheduledAt,
        isReschedule: false,
        concurrencyToken: item.concurrencyToken,
      },
    });

    ref.afterClosed().subscribe((scheduled) => {
      if (scheduled) {
        this.loadIrrigation();
      }
    });
  }

  onReschedule(): void {
    const item = this.irrigation();
    if (!item) return;

    const ref = this.dialog.open(ScheduleIrrigationDialogComponent, {
      width: "500px",
      data: {
        irrigationId: item.id,
        farmName: item.farmName,
        farmAreaName: item.farmAreaName,
        currentScheduledAt: item.scheduledAt,
        isReschedule: true,
        concurrencyToken: item.concurrencyToken,
      },
    });

    ref.afterClosed().subscribe((rescheduled) => {
      if (rescheduled) {
        this.loadIrrigation();
      }
    });
  }

  onStart(): void {
    const item = this.irrigation();
    if (!item) return;

    const ref = this.dialog.open(StartIrrigationDialogComponent, {
      width: "560px",
      data: {
        irrigationId: item.id,
        farmId: item.farmId,
        farmName: item.farmName,
        farmAreaId: item.farmAreaId,
        farmAreaName: item.farmAreaName,
        plantationId: item.plantationId,
        cropCycleId: item.cropCycleId,
        cropCycleStageId: item.cropCycleStageId,
        concurrencyToken: item.concurrencyToken,
      },
    });

    ref.afterClosed().subscribe((started) => {
      if (started) {
        this.loadIrrigation();
      }
    });
  }

  onComplete(): void {
    const item = this.irrigation();
    if (!item) return;

    const ref = this.dialog.open(CompleteIrrigationDialogComponent, {
      width: "560px",
      data: {
        irrigation: item,
      },
    });

    ref.afterClosed().subscribe((completed) => {
      if (completed) {
        this.loadIrrigation();
      }
    });
  }

  onCancel(): void {
    const item = this.irrigation();
    if (!item) return;

    const ref = this.dialog.open(CancelIrrigationDialogComponent, {
      width: "500px",
      data: {
        irrigationId: item.id,
        farmName: item.farmName,
        farmAreaName: item.farmAreaName,
        currentStatus: item.status,
        concurrencyToken: item.concurrencyToken,
      },
    });

    ref.afterClosed().subscribe((cancelled) => {
      if (cancelled) {
        this.loadIrrigation();
      }
    });
  }

  // Visual Helpers
  getStatusClass(status?: IrrigationStatus | string): string {
    switch (status) {
      case "Draft":
        return "status-pill--planned";
      case "Scheduled":
        return "status-pill--planned status-pill--scheduled";
      case "InProgress":
        return "status-pill--in-progress";
      case "Completed":
        return "status-pill--completed";
      case "Cancelled":
        return "status-pill--cancelled";
      default:
        return "status-pill--archived";
    }
  }

  getStatusLabel(status?: IrrigationStatus | string): string {
    switch (status) {
      case "Draft":
        return "Draft";
      case "Scheduled":
        return "Scheduled";
      case "InProgress":
        return "In Progress";
      case "Completed":
        return "Completed";
      case "Cancelled":
        return "Cancelled";
      default:
        return status || "Unknown";
    }
  }
}
