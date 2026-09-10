import { CommonModule, DatePipe } from "@angular/common";
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
import { MatIconModule } from "@angular/material/icon";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSnackBar } from "@angular/material/snack-bar";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { finalize } from "rxjs";

import { PermissionService } from "../../../../core/auth/permission.service";
import { BreadcrumbService } from "../../../../core/breadcrumb/breadcrumb.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import {
  LaborActivityCancelDialogComponent,
} from "../../components/labor-activity-cancel-dialog/labor-activity-cancel-dialog.component";
import { LaborActivity } from "../../models/labor-activity.models";
import { LaborActivityService } from "../../services/labor-activity.service";

@Component({
  selector: "app-labor-activity-detail-page",
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule,
    RouterLink,
  ],
  templateUrl: "./labor-activity-detail-page.component.html",
  styleUrl: "./labor-activity-detail-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LaborActivityDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly activityService = inject(LaborActivityService);
  private readonly permissionService = inject(PermissionService);
  private readonly breadcrumbService = inject(BreadcrumbService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  readonly id = this.route.snapshot.paramMap.get("id")!;
  readonly activity = signal<LaborActivity | null>(null);
  readonly isLoading = signal(true);
  readonly isCancelling = signal(false);

  hasPermission(permission: string): boolean {
    return this.permissionService.has(permission);
  }

  ngOnInit(): void {
    this.loadActivity();
  }

  loadActivity(): void {
    this.isLoading.set(true);
    this.activityService
      .get(this.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (activity) => {
          this.activity.set(activity);
          this.breadcrumbService.setEntityName(
            activity.id,
            `${activity.activityType.name} (${activity.activityDate})`,
          );
        },
        error: (err) => {
          this.snack.open(
            getApiErrorMessage(err, "Failed to load labor activity."),
            "Dismiss",
            { duration: 5000 },
          );
        },
      });
  }

  openCancelDialog(): void {
    const act = this.activity();
    if (!act || act.status === "CANCELLED") return;

    const dialogRef = this.dialog.open(LaborActivityCancelDialogComponent, {
      data: {
        activityId: act.id,
        activityDate: act.activityDate,
        activityTypeName: act.activityType.name,
        farmName: act.farm.name,
      },
      width: "500px",
    });

    dialogRef
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((reason: string | undefined) => {
        if (!reason) return;

        this.isCancelling.set(true);
        this.activityService
          .cancel(act.id, reason)
          .pipe(
            takeUntilDestroyed(this.destroyRef),
            finalize(() => this.isCancelling.set(false)),
          )
          .subscribe({
            next: () => {
              this.snack.open("Labor activity cancelled successfully.", "Dismiss", {
                duration: 4000,
              });
              this.loadActivity();
            },
            error: (err) => {
              this.snack.open(
                getApiErrorMessage(err, "Failed to cancel labor activity."),
                "Dismiss",
                { duration: 5000 },
              );
            },
          });
      });
  }
}
