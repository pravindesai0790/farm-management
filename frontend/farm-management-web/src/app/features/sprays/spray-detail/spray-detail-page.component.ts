import { CommonModule } from "@angular/common";
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatDialog, MatDialogModule } from "@angular/material/dialog";
import { MatDividerModule } from "@angular/material/divider";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTooltipModule } from "@angular/material/tooltip";
import { ActivatedRoute, Router, RouterModule } from "@angular/router";
import { finalize } from "rxjs";

import { BreadcrumbService } from "../../../core/breadcrumb/breadcrumb.service";
import { PermissionService } from "../../../core/auth/permission.service";
import {
  SprayDetailsResponse,
  SprayStatus,
} from "../../../core/sprays/spray.models";
import { SprayService } from "../../../core/sprays/spray.service";
import { ConfirmDialogComponent } from "../../../shared/components/confirm-dialog/confirm-dialog.component";
import { getApiErrorMessage } from "../../../core/models/api-error.model";
import { CancelSprayDialogComponent } from "../dialogs/cancel-spray-dialog/cancel-spray-dialog.component";
import { ScheduleSprayDialogComponent } from "../dialogs/schedule-spray-dialog/schedule-spray-dialog.component";

@Component({
  selector: "app-spray-detail-page",
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    MatDividerModule,
  ],
  templateUrl: "./spray-detail-page.component.html",
  styleUrl: "./spray-detail-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SprayDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly sprayService = inject(SprayService);
  private readonly breadcrumbService = inject(BreadcrumbService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);

  readonly id = this.route.snapshot.paramMap.get("id") ?? "";

  readonly spray = signal<SprayDetailsResponse | null>(null);
  readonly isLoading = signal(true);
  readonly isActionLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    if (!this.id) {
      this.errorMessage.set("No spray application specified.");
      this.isLoading.set(false);
      return;
    }

    this.loadSpray();
  }

  loadSpray(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.sprayService
      .getSpray(this.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (res) => {
          this.spray.set(res);
          if (res.referenceNumber) {
            this.breadcrumbService.setEntityName(this.id, res.referenceNumber);
          }
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to load spray application details.");
          this.errorMessage.set(msg);
          this.snack.open(msg, "Dismiss", { duration: 4000 });
        },
      });
  }

  onEdit(): void {
    this.router.navigate(["/sprays", this.id, "edit"]);
  }

  onSchedule(): void {
    const s = this.spray();
    if (!s) return;

    const ref = this.dialog.open(ScheduleSprayDialogComponent, {
      width: "500px",
      data: {
        sprayId: s.id,
        referenceNumber: s.referenceNumber || "Draft",
        farmName: s.farmName,
        currentScheduledDateTime: s.scheduledDateTime,
        isReschedule: false,
      },
    });

    ref.afterClosed().subscribe((scheduled) => {
      if (scheduled) {
        this.loadSpray();
      }
    });
  }

  onReschedule(): void {
    const s = this.spray();
    if (!s) return;

    const ref = this.dialog.open(ScheduleSprayDialogComponent, {
      width: "500px",
      data: {
        sprayId: s.id,
        referenceNumber: s.referenceNumber || "Draft",
        farmName: s.farmName,
        currentScheduledDateTime: s.scheduledDateTime,
        isReschedule: true,
      },
    });

    ref.afterClosed().subscribe((scheduled) => {
      if (scheduled) {
        this.loadSpray();
      }
    });
  }

  onStart(): void {
    this.router.navigate(["/sprays", this.id, "start"]);
  }

  onContinueExecution(): void {
    this.router.navigate(["/sprays", this.id, "execution"]);
  }

  onCompleteDirect(): void {
    const s = this.spray();
    if (!s) return;

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: "Complete Spray Application",
        message: `Complete application ${s.referenceNumber}? This action will finalize execution and immediately deduct allocated product quantities from inventory.`,
        confirmText: "Complete Spray",
        color: "primary",
        icon: "check_circle",
      },
    });

    dialogRef.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;

      this.isActionLoading.set(true);
      this.sprayService
        .completeSpray(this.id)
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isActionLoading.set(false)),
        )
        .subscribe({
          next: () => {
            this.snack.open("Spray application completed and stock deducted successfully.", "OK", {
              duration: 3500,
            });
            this.loadSpray();
          },
          error: (err) => {
            const msg = getApiErrorMessage(err, "Failed to complete spray application.");
            this.snack.open(msg, "Dismiss", { duration: 5000 });
          },
        });
    });
  }

  onCancelSpray(): void {
    const s = this.spray();
    if (!s) return;

    const ref = this.dialog.open(CancelSprayDialogComponent, {
      width: "500px",
      data: {
        sprayId: s.id,
        referenceNumber: s.referenceNumber || "Draft",
        farmName: s.farmName,
        status: s.status,
      },
    });

    ref.afterClosed().subscribe((cancelled) => {
      if (cancelled) {
        this.loadSpray();
      }
    });
  }

  // Helpers
  getStatusClass(status: SprayStatus | string | undefined): string {
    const s = (status || "").toLowerCase();
    switch (s) {
      case "draft":
        return "status-pill--planned";
      case "scheduled":
        return "status-pill--planned status-pill--scheduled";
      case "inprogress":
      case "in_progress":
        return "status-pill--in-progress";
      case "completed":
        return "status-pill--completed";
      case "cancelled":
        return "status-pill--cancelled";
      default:
        return "status-pill--archived";
    }
  }

  getStatusLabel(status: SprayStatus | string | undefined): string {
    const s = (status || "").toLowerCase();
    switch (s) {
      case "draft":
        return "Draft";
      case "scheduled":
        return "Scheduled";
      case "inprogress":
      case "in_progress":
        return "In Progress";
      case "completed":
        return "Completed";
      case "cancelled":
        return "Cancelled";
      default:
        return status || "Unknown";
    }
  }
}
