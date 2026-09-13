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
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar, MatSnackBarModule } from "@angular/material/snack-bar";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { debounceTime, distinctUntilChanged, finalize } from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import { BreadcrumbService } from "../../../core/breadcrumb/breadcrumb.service";
import {
  ATTENDANCE_TYPE_OPTIONS,
  AttendanceDetail,
  AttendanceType,
  AttendanceWagePreviewResponse,
  formatAttendanceStatus,
  formatAttendanceType,
  getAttendanceTypeBadgeClass,
} from "../../../core/labor/attendance.models";
import { AttendanceService } from "../../../core/labor/attendance.service";
import { getApiErrorMessage } from "../../../core/models/api-error.model";
import { ErrorAlertComponent } from "../../../shared/components/error-alert/error-alert.component";

@Component({
  selector: "app-attendance-editor-page",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSnackBarModule,
    ErrorAlertComponent,
  ],
  templateUrl: "./attendance-editor-page.component.html",
  styleUrl: "./attendance-editor-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AttendanceEditorPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly attendanceService = inject(AttendanceService);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly breadcrumbService = inject(BreadcrumbService);
  readonly permissionService = inject(PermissionService);

  readonly typeOptions = ATTENDANCE_TYPE_OPTIONS;

  readonly attendanceId = signal<string | null>(null);
  readonly attendance = signal<AttendanceDetail | null>(null);
  readonly isLoading = signal<boolean>(true);
  readonly submitting = signal<boolean>(false);
  readonly previewLoading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly previewError = signal<string | null>(null);
  readonly preview = signal<AttendanceWagePreviewResponse | null>(null);

  readonly selectedAttendanceType = signal<AttendanceType>("FULL_DAY");

  readonly form = this.fb.nonNullable.group({
    attendanceType: this.fb.nonNullable.control<AttendanceType>(
      "FULL_DAY",
      [Validators.required],
    ),
    workingHours: this.fb.control<number | null>(null),
    notes: this.fb.control<string>("", [Validators.maxLength(500)]),
  });

  readonly isDraft = computed(() => {
    return this.attendance()?.status?.toUpperCase() === "DRAFT";
  });

  readonly isFinalized = computed(() => {
    return this.attendance()?.status?.toUpperCase() === "FINALIZED";
  });

  readonly isHourly = computed(() => {
    return this.selectedAttendanceType() === "HOURLY";
  });

  readonly canEdit = computed(() => {
    return this.isDraft() && this.permissionService.has("Attendance.Update");
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get("id");
    if (!id) {
      this.router.navigate(["/labor/attendance"]);
      return;
    }

    this.attendanceId.set(id);

    this.breadcrumbService.setTrail([
      { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
      { label: "Labor", route: "/labor" },
      { label: "Daily Attendance", route: "/labor/attendance" },
      { label: "Edit Attendance Draft" },
    ]);

    this.loadRecord(id);

    this.form.controls.attendanceType.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((type) => {
        this.selectedAttendanceType.set(type);
        this.updateHoursValidation(type);
        this.triggerWagePreview();
      });

    this.form.controls.workingHours.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        this.triggerWagePreview();
      });
  }

  loadRecord(id: string): void {
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

          const title = `Edit ${detail.workerDisplayName} · ${detail.attendanceDate}`;
          this.breadcrumbService.setEntityName(id, title);
          this.breadcrumbService.setTrail([
            { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
            { label: "Labor", route: "/labor" },
            { label: "Daily Attendance", route: "/labor/attendance" },
            { label: detail.workerDisplayName, route: `/labor/attendance/${id}` },
            { label: "Edit Draft" },
          ]);

          if (detail.status?.toUpperCase() === "DRAFT") {
            const initialType = (detail.attendanceType as AttendanceType) || "FULL_DAY";
            this.selectedAttendanceType.set(initialType);
            this.form.patchValue(
              {
                attendanceType: initialType,
                workingHours: detail.workingHours ?? null,
                notes: detail.notes ?? "",
              },
              { emitEvent: false },
            );
            this.updateHoursValidation(initialType);
          }
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to load attendance record for editing.");
          this.errorMessage.set(msg);
        },
      });
  }

  private updateHoursValidation(type: AttendanceType): void {
    const hoursCtrl = this.form.controls.workingHours;
    if (type === "HOURLY") {
      hoursCtrl.setValidators([
        Validators.required,
        Validators.min(0.5),
        Validators.max(24),
      ]);
      if (!hoursCtrl.value) {
        hoursCtrl.setValue(8);
      }
    } else {
      hoursCtrl.clearValidators();
      hoursCtrl.setValue(null);
    }
    hoursCtrl.updateValueAndValidity();
  }

  private triggerWagePreview(): void {
    const current = this.attendance();
    if (!current || !this.isDraft()) {
      return;
    }

    const type = this.form.controls.attendanceType.value;
    const hours = this.form.controls.workingHours.value;

    if (type === "NOT_WORKED") {
      this.previewError.set(null);
      this.preview.set(null);
      return;
    }

    if (type === "HOURLY" && (!hours || hours < 0.5 || hours > 24)) {
      return;
    }

    this.previewLoading.set(true);
    this.previewError.set(null);

    this.attendanceService
      .previewWage({
        workerId: current.workerId,
        attendanceDate: current.attendanceDate,
        attendanceType: type,
        workingHours: type === "HOURLY" ? hours : null,
        farmId: current.farmId,
      })
      .pipe(
        finalize(() => this.previewLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (res) => {
          if (res.isEarningEligible === false || res.rate <= 0) {
            this.preview.set(null);
            this.previewError.set(
              res.ineligibilityReason ||
                `No active wage rate entry found for gender '${res.gender}' and wage type '${formatAttendanceType(type)}' on date ${current.attendanceDate}.`,
            );
          } else {
            this.preview.set(res);
            this.previewError.set(null);
          }
        },
        error: (err) => {
          this.preview.set(null);
          const msg = getApiErrorMessage(
            err,
            `No active wage rate entry configured for attendance type '${formatAttendanceType(type)}' on date ${current.attendanceDate}.`,
          );
          this.previewError.set(msg);
        },
      });
  }

  onSave(): void {
    const current = this.attendance();
    if (!current || !this.canEdit() || this.form.invalid || this.submitting()) {
      return;
    }

    const type = this.form.controls.attendanceType.value;
    if (type !== "NOT_WORKED" && this.previewError()) {
      this.errorMessage.set(this.previewError());
      return;
    }

    const hours = this.form.controls.workingHours.value;
    const notes = this.form.controls.notes.value?.trim() || null;

    this.submitting.set(true);
    this.errorMessage.set(null);

    this.attendanceService
      .updateDraft(current.id, {
        attendanceType: type,
        workingHours: type === "HOURLY" ? hours : null,
        notes,
      })
      .pipe(
        finalize(() => this.submitting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.snack.open("Attendance draft updated successfully.", "OK", {
            duration: 3500,
          });
          this.router.navigate(["/labor/attendance", current.id]);
        },
        error: (err) => {
          this.errorMessage.set(
            getApiErrorMessage(err, "Failed to update draft attendance."),
          );
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
