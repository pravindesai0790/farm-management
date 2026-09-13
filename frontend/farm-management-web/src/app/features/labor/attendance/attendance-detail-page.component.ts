import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatDialog, MatDialogModule } from "@angular/material/dialog";
import { MatDividerModule } from "@angular/material/divider";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSnackBar, MatSnackBarModule } from "@angular/material/snack-bar";
import { MatTooltipModule } from "@angular/material/tooltip";
import { finalize } from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import { BreadcrumbService } from "../../../core/breadcrumb/breadcrumb.service";
import {
  AttendanceDetail,
  formatAttendanceStatus,
  formatAttendanceType,
  getAttendanceTypeBadgeClass,
} from "../../../core/labor/attendance.models";
import { AttendanceService } from "../../../core/labor/attendance.service";
import { getApiErrorMessage } from "../../../core/models/api-error.model";
import { ErrorAlertComponent } from "../../../shared/components/error-alert/error-alert.component";
import {
  AttendanceEditDialogComponent,
  AttendanceEditDialogData,
} from "./dialogs/attendance-edit-dialog.component";

@Component({
  selector: "app-attendance-detail-page",
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatDividerModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTooltipModule,
    ErrorAlertComponent,
  ],
  templateUrl: "./attendance-detail-page.component.html",
  styleUrl: "./attendance-detail-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AttendanceDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly attendanceService = inject(AttendanceService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly breadcrumbService = inject(BreadcrumbService);
  readonly permissionService = inject(PermissionService);

  readonly attendance = signal<AttendanceDetail | null>(null);
  readonly isLoading = signal<boolean>(true);
  readonly actionInProgress = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  readonly isDraft = computed(() => {
    return this.attendance()?.status?.toUpperCase() === "DRAFT";
  });

  readonly isFinalized = computed(() => {
    return this.attendance()?.status?.toUpperCase() === "FINALIZED";
  });

  readonly canEdit = computed(() => {
    return this.isDraft() && this.permissionService.has("Attendance.Update");
  });

  readonly canFinalize = computed(() => {
    return this.isDraft() && this.permissionService.has("Attendance.Finalize");
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get("id");
    if (!id) {
      this.router.navigate(["/labor/attendance"]);
      return;
    }

    const cached = this.breadcrumbService.getEntityName(id);
    this.breadcrumbService.setTrail([
      { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
      { label: "Labor", route: "/labor" },
      { label: "Daily Attendance", route: "/labor/attendance" },
      { label: cached ?? "Attendance Details" },
    ]);

    this.load(id);
  }

  load(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.attendanceService
      .getAttendanceById(id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (detail) => {
          this.attendance.set(detail);
          const title = `${detail.workerDisplayName} · ${detail.attendanceDate}`;
          this.breadcrumbService.setEntityName(detail.id, title);
          this.breadcrumbService.setTrail([
            { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
            { label: "Labor", route: "/labor" },
            { label: "Daily Attendance", route: "/labor/attendance" },
            { label: title },
          ]);
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to load attendance details.");
          this.errorMessage.set(msg);
          this.snack.open(msg, "Dismiss", { duration: 5000 });
        },
      });
  }

  openEditDialog(): void {
    const current = this.attendance();
    if (!current || !this.canEdit()) {
      return;
    }

    const dialogRef = this.dialog.open(AttendanceEditDialogComponent, {
      width: "480px",
      data: { record: current } as AttendanceEditDialogData,
      disableClose: true,
    });

    dialogRef.afterClosed().subscribe((saved) => {
      if (saved) {
        this.snack.open("Attendance draft updated successfully.", "OK", {
          duration: 3500,
        });
        this.load(current.id);
      }
    });
  }

  finalizeRecord(): void {
    const current = this.attendance();
    if (!current || !this.canFinalize()) {
      return;
    }

    this.actionInProgress.set(true);
    this.attendanceService
      .finalizeSingleAttendance(current.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.actionInProgress.set(false)),
      )
      .subscribe({
        next: () => {
          this.snack.open(
            "Attendance finalized successfully and posted to earnings ledger.",
            "OK",
            { duration: 4000 },
          );
          this.load(current.id);
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to finalize attendance record.");
          this.snack.open(msg, "Dismiss", { duration: 5000 });
        },
      });
  }

  formatType(type?: string | null): string {
    return formatAttendanceType(type);
  }

  formatStatus(status?: string | null): string {
    return formatAttendanceStatus(status);
  }

  getTypeBadgeClass(type?: string | null): string {
    return getAttendanceTypeBadgeClass(type);
  }
}
