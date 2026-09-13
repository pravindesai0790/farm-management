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
import { FormBuilder, ReactiveFormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatDialog } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTableModule } from "@angular/material/table";
import { MatTooltipModule } from "@angular/material/tooltip";
import { RouterLink } from "@angular/router";
import { Subject, debounceTime, distinctUntilChanged, finalize } from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import { BreadcrumbService } from "../../../core/breadcrumb/breadcrumb.service";
import { Farm } from "../../../core/farm-management/farm-management.models";
import { FarmManagementService } from "../../../core/farm-management/farm-management.service";
import {
  ATTENDANCE_TYPE_OPTIONS,
  AttendanceEligibleWorker,
  AttendanceGridRow,
  AttendanceRecord,
  AttendanceType,
  CopyPreviousDayAttendanceResponse,
  DailyAttendanceSummary,
  ExcludedWorkerInfo,
  SaveDailyDraftAttendanceBatchRequest,
  formatAttendanceStatus,
  formatAttendanceType,
  getAttendanceTypeBadgeClass,
} from "../../../core/labor/attendance.models";
import { AttendanceService } from "../../../core/labor/attendance.service";
import { getApiErrorMessage } from "../../../core/models/api-error.model";
import { formatDateOnly, parseDateOnly } from "../../../core/utils/date.utils";
import { ErrorAlertComponent } from "../../../shared/components/error-alert/error-alert.component";
import {
  AttendanceLoadWorkersDialogComponent,
  AttendanceLoadWorkersDialogData,
} from "./dialogs/attendance-load-workers-dialog.component";
import {
  AttendanceAddWorkerDialogComponent,
  AttendanceAddWorkerDialogData,
  AttendanceAddWorkerResult,
} from "./dialogs/attendance-add-worker-dialog.component";
import {
  AttendanceCopyPreviousDayDialogComponent,
  AttendanceCopyPreviousDayDialogData,
} from "./dialogs/attendance-copy-previous-day-dialog.component";
import {
  AttendanceFinalizeDialogComponent,
  AttendanceFinalizeDialogData,
} from "./dialogs/attendance-finalize-dialog.component";

@Component({
  selector: "app-attendance-daily-page",
  standalone: true,
  imports: [
    CommonModule,
    ErrorAlertComponent,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: "./attendance-daily-page.component.html",
  styleUrl: "./attendance-daily-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AttendanceDailyPageComponent implements OnInit {
  private readonly attendanceService = inject(AttendanceService);
  private readonly farmService = inject(FarmManagementService);
  readonly permissionService = inject(PermissionService);
  private readonly breadcrumbService = inject(BreadcrumbService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly formBuilder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly attendanceTypeOptions = ATTENDANCE_TYPE_OPTIONS;

  readonly displayedColumns: readonly string[] = [
    "worker",
    "gender",
    "category",
    "attendanceType",
    "workingHours",
    "wageRate",
    "earnings",
    "status",
    "actions",
  ];

  // Primary State Signals
  readonly farms = signal<readonly Farm[]>([]);
  readonly selectedFarmId = signal<string>("");
  readonly selectedDate = signal<Date>(new Date());
  readonly rows = signal<AttendanceGridRow[]>([]);
  readonly summary = signal<DailyAttendanceSummary>({
    totalCount: 0,
    workedCount: 0,
    fullDayCount: 0,
    halfDayCount: 0,
    hourlyCount: 0,
    notWorkedCount: 0,
    estimatedEarnings: 0,
    status: "DRAFT",
  });
  readonly currencySymbol = signal<string>("₹");
  readonly finalizedMetadata = signal<{
    finalizedAt?: string | null;
    finalizedBy?: string | null;
  } | null>(null);
  readonly lastCopiedExcludedWorkers = signal<ExcludedWorkerInfo[]>([]);

  // Status & Progress Signals
  readonly isLoading = signal<boolean>(false);
  readonly isSaving = signal<boolean>(false);
  readonly isFinalizing = signal<boolean>(false);
  readonly isPreviewing = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly apiError = signal<unknown>(null);
  readonly hasUnsavedChanges = signal<boolean>(false);

  // Filter Form for Top Bar
  readonly topControlsForm = this.formBuilder.nonNullable.group({
    attendanceDate: [new Date()],
    farmId: [""],
    searchWorker: [""],
  });

  private readonly previewTrigger$ = new Subject<void>();

  // Computed state
  readonly selectedFarm = computed(() => {
    const id = this.selectedFarmId();
    return this.farms().find((f) => f.id === id) || null;
  });

  readonly formattedDate = computed(() => {
    return formatDateOnly(this.selectedDate()) || "";
  });

  readonly isFinalized = computed(() => {
    return (
      this.summary().status?.toUpperCase() === "FINALIZED" ||
      (this.rows().length > 0 &&
        this.rows().every((r) => r.status?.toUpperCase() === "FINALIZED"))
    );
  });

  readonly searchWorkerQuery = signal<string>("");

  readonly filteredRows = computed(() => {
    const search = this.searchWorkerQuery().toLowerCase();
    const all = this.rows();
    if (!search) return all;

    return all.filter((r) => {
      const nameMatch = (r.workerDisplayName || "").toLowerCase().includes(search);
      const firstNameMatch = (r.workerFirstName || "").toLowerCase().includes(search);
      const lastNameMatch = (r.workerLastName || "").toLowerCase().includes(search);
      const mobileMatch = r.mobileNumber ? r.mobileNumber.toLowerCase().includes(search) : false;
      const catMatch = r.laborCategoryName
        ? r.laborCategoryName.toLowerCase().includes(search)
        : false;
      const contractorMatch = r.contractorName
        ? r.contractorName.toLowerCase().includes(search)
        : false;
      return (
        nameMatch ||
        firstNameMatch ||
        lastNameMatch ||
        mobileMatch ||
        catMatch ||
        contractorMatch
      );
    });
  });

  readonly canEdit = computed(() => {
    if (this.isFinalized()) return false;
    return (
      this.permissionService.has("Attendance.Create") ||
      this.permissionService.has("Attendance.Update")
    );
  });

  readonly canCreate = computed(() => {
    return this.permissionService.has("Attendance.Create");
  });

  readonly hasInvalidHourlyRows = computed(() => {
    return this.rows().some(
      (r) =>
        r.attendanceType === "HOURLY" &&
        (!r.workingHours || r.workingHours <= 0 || r.workingHours > 24 || !!r.hoursError),
    );
  });

  readonly canSaveDraft = computed(() => {
    if (this.isFinalized()) return false;
    if (this.rows().length === 0) return false;
    if (
      !this.permissionService.has("Attendance.Create") &&
      !this.permissionService.has("Attendance.Update")
    ) {
      return false;
    }
    if (this.isSaving() || this.isFinalizing()) return false;
    if (this.hasInvalidHourlyRows()) return false;
    return true;
  });

  readonly canFinalize = computed(() => {
    if (this.isFinalized()) return false;
    if (this.rows().length === 0) return false;
    if (!this.permissionService.has("Attendance.Finalize")) return false;
    if (this.isSaving() || this.isFinalizing()) return false;
    if (this.hasInvalidHourlyRows()) return false;
    return true;
  });

  readonly alreadyAddedWorkerIds = computed(() => {
    return this.rows().map((r) => r.workerId);
  });

  ngOnInit(): void {
    this.breadcrumbService.setTrail([
      { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
      { label: "Labor", route: "/labor", icon: "engineering" },
      { label: "Daily Attendance", route: "/labor/attendance" },
    ]);

    this.loadFarms();

    // Debounced wage preview stream
    this.previewTrigger$
      .pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.runWagePreview();
      });

    // Date change handler
    this.topControlsForm.controls.attendanceDate.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((newDate) => {
        if (newDate) {
          this.selectedDate.set(newDate);
          if (this.selectedFarmId()) {
            this.loadDailyAttendance();
          }
        }
      });

    // Farm change handler
    this.topControlsForm.controls.farmId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((newFarmId) => {
        if (newFarmId && newFarmId !== this.selectedFarmId()) {
          this.selectedFarmId.set(newFarmId);
          this.loadDailyAttendance();
        }
      });

    // In-grid search worker change handler
    this.topControlsForm.controls.searchWorker.valueChanges
      .pipe(
        debounceTime(150),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((query) => {
        this.searchWorkerQuery.set(query?.trim() || "");
      });
  }

  loadFarms(): void {
    this.farmService
      .listFarms(1, 100, "", true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          const activeFarms = res.items || [];
          this.farms.set(activeFarms);
          if (activeFarms.length > 0 && !this.selectedFarmId()) {
            const firstFarmId = activeFarms[0].id;
            this.topControlsForm.controls.farmId.setValue(firstFarmId, {
              emitEvent: true,
            });
          }
        },
        error: (err) => {
          this.errorMessage.set(getApiErrorMessage(err, "Failed to load farms."));
        },
      });
  }

  loadDailyAttendance(): void {
    const farmId = this.selectedFarmId();
    const date = this.formattedDate();

    if (!farmId || !date) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.apiError.set(null);
    this.hasUnsavedChanges.set(false);
    this.lastCopiedExcludedWorkers.set([]);

    this.attendanceService
      .getDailyAttendance(farmId, date)
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (res) => {
          this.summary.set(res.summary);
          const mappedRows: AttendanceGridRow[] = (res.records || []).map((r) => ({
            id: r.id,
            workerId: r.workerId,
            workerDisplayName: r.workerDisplayName,
            workerFirstName: r.workerFirstName,
            workerLastName: r.workerLastName,
            gender: r.gender,
            mobileNumber: null, // worker mobile if available or from record
            laborCategoryName: r.laborCategoryName,
            contractorName: null,
            employmentType: r.employmentType,
            attendanceType: (r.attendanceType as AttendanceType) || "FULL_DAY",
            workingHours: r.workingHours,
            calculatedRate: r.calculatedRate,
            calculatedAmount: r.calculatedAmount,
            currencySymbol: r.currencySymbol || "₹",
            status: r.status as any,
            notes: r.notes,
            isModified: false,
          }));

          this.rows.set(mappedRows);

          if (res.records.length > 0) {
            const firstWithSymbol = res.records.find((r) => r.currencySymbol);
            if (firstWithSymbol?.currencySymbol) {
              this.currencySymbol.set(firstWithSymbol.currencySymbol);
            }
          }

          const finalizedRecord = res.records.find((r) => r.finalizedAt);
          if (finalizedRecord) {
            this.finalizedMetadata.set({
              finalizedAt: finalizedRecord.finalizedAt,
              finalizedBy: finalizedRecord.finalizedBy,
            });
          } else {
            this.finalizedMetadata.set(null);
          }
        },
        error: (err) => {
          this.errorMessage.set(
            getApiErrorMessage(err, "Failed to load daily attendance."),
          );
        },
      });
  }

  // Live Wage Preview & Grid Editing logic
  // Live Wage Preview & Grid Editing logic
  onAttendanceTypeChange(row: AttendanceGridRow, newType: AttendanceType): void {
    if (this.isFinalized()) return;
    row.attendanceType = newType;
    row.isModified = true;
    this.hasUnsavedChanges.set(true);

    if (newType === "NOT_WORKED") {
      row.workingHours = null;
      row.hoursError = null;
      row.calculatedRate = null;
      row.calculatedAmount = 0;
    } else if (newType === "HOURLY") {
      if (!row.workingHours || row.workingHours <= 0) {
        row.workingHours = 8;
      }
      row.hoursError = null;
    } else {
      // FULL_DAY or HALF_DAY
      row.workingHours = null;
      row.hoursError = null;
    }
    this.previewTrigger$.next();
  }

  onHoursInput(row: AttendanceGridRow, event: Event): void {
    if (this.isFinalized()) return;
    const input = event.target as HTMLInputElement;
    const rawVal = input.value?.trim();

    if (!rawVal) {
      row.workingHours = null;
      row.hoursError = "Hours required";
      row.calculatedAmount = null;
      row.isModified = true;
      this.hasUnsavedChanges.set(true);
      this.previewTrigger$.next();
      return;
    }

    const val = parseFloat(rawVal);
    if (isNaN(val) || val <= 0) {
      row.workingHours = isNaN(val) ? null : val;
      row.hoursError = "Hours must be > 0";
      row.calculatedAmount = null;
      row.isModified = true;
      this.hasUnsavedChanges.set(true);
      this.previewTrigger$.next();
      return;
    }

    if (val > 24) {
      row.workingHours = val;
      row.hoursError = "Max 24 hrs";
      row.calculatedAmount = null;
      row.isModified = true;
      this.hasUnsavedChanges.set(true);
      this.previewTrigger$.next();
      return;
    }

    // Valid working hours: 0.25 <= val <= 24
    row.workingHours = val;
    row.hoursError = null;
    row.isModified = true;
    this.hasUnsavedChanges.set(true);
    this.previewTrigger$.next();
  }

  onHoursChange(row: AttendanceGridRow, event: Event): void {
    this.onHoursInput(row, event);
  }

  onNotesChange(row: AttendanceGridRow, event: Event): void {
    if (this.isFinalized()) return;
    const input = event.target as HTMLInputElement;
    row.notes = input.value;
    row.isModified = true;
    this.hasUnsavedChanges.set(true);
  }

  removeRow(row: AttendanceGridRow): void {
    if (this.isFinalized()) return;
    const updated = this.rows().filter((r) => r.workerId !== row.workerId);
    this.rows.set(updated);
    this.hasUnsavedChanges.set(true);

    if (updated.length > 0) {
      this.previewTrigger$.next();
    } else {
      this.summary.set({
        totalCount: 0,
        workedCount: 0,
        fullDayCount: 0,
        halfDayCount: 0,
        hourlyCount: 0,
        notWorkedCount: 0,
        estimatedEarnings: 0,
        status: this.summary().status,
      });
    }
  }

  private runWagePreview(): void {
    const list = this.rows();
    const date = this.formattedDate();
    const farmId = this.selectedFarmId();

    if (list.length === 0 || !date) return;

    this.isPreviewing.set(true);

    const items = list.map((r) => ({
      workerId: r.workerId,
      attendanceType: r.attendanceType,
      workingHours:
        r.attendanceType === "HOURLY"
          ? r.workingHours && r.workingHours > 0 && r.workingHours <= 24 && !r.hoursError
            ? r.workingHours
            : null
          : null,
    }));

    this.attendanceService
      .previewWageBatch({
        attendanceDate: date,
        farmId: farmId || null,
        items,
      })
      .pipe(
        finalize(() => this.isPreviewing.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (previewRes) => {
          // Map preview results back onto rows
          const previewMap = new Map(
            previewRes.items.map((p) => [p.workerId, p]),
          );

          const updatedRows = this.rows().map((r) => {
            if (r.attendanceType === "NOT_WORKED") {
              return {
                ...r,
                calculatedRate: null,
                calculatedAmount: 0,
              };
            }
            const preview = previewMap.get(r.workerId);
            if (preview) {
              return {
                ...r,
                calculatedRate: preview.rate,
                calculatedAmount: preview.calculatedAmount,
                currencySymbol: preview.currencySymbol || this.currencySymbol(),
              };
            }
            return r;
          });

          this.rows.set(updatedRows);

          // Update summary directly from authoritative backend preview batch aggregates
          this.summary.set({
            totalCount: previewRes.totalCount,
            workedCount: previewRes.workedCount,
            fullDayCount: previewRes.fullDayCount,
            halfDayCount: previewRes.halfDayCount,
            hourlyCount: previewRes.hourlyCount,
            notWorkedCount: previewRes.notWorkedCount,
            estimatedEarnings: previewRes.totalEstimatedEarnings,
            status: this.summary().status,
          });

          if (previewRes.items.length > 0 && previewRes.items[0].currencySymbol) {
            this.currencySymbol.set(previewRes.items[0].currencySymbol);
          }
        },
        error: (err) => {
          console.warn("Could not preview wages:", err);
        },
      });
  }

  // Top Action: Load Eligible Workers
  openLoadWorkersDialog(): void {
    const farm = this.selectedFarm();
    if (!farm) return;

    const dialogRef = this.dialog.open(AttendanceLoadWorkersDialogComponent, {
      width: "880px",
      maxWidth: "95vw",
      data: {
        farmId: farm.id,
        farmName: farm.name,
        attendanceDate: this.formattedDate(),
        alreadyAddedWorkerIds: this.alreadyAddedWorkerIds(),
      } as AttendanceLoadWorkersDialogData,
    });

    dialogRef
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((selectedWorkers: AttendanceEligibleWorker[] | undefined) => {
        if (!selectedWorkers || selectedWorkers.length === 0) return;

        const currentRows = [...this.rows()];
        const existingIds = new Set(currentRows.map((r) => r.workerId));

        for (const w of selectedWorkers) {
          if (!existingIds.has(w.workerId)) {
            currentRows.push({
              workerId: w.workerId,
              workerDisplayName: w.displayName,
              workerFirstName: w.firstName,
              workerLastName: w.lastName,
              gender: w.gender,
              mobileNumber: w.mobileNumber,
              laborCategoryName: w.laborCategoryName,
              contractorName: w.contractorName,
              employmentType: w.employmentType,
              attendanceType: "FULL_DAY",
              workingHours: null,
              calculatedRate: null,
              calculatedAmount: null,
              currencySymbol: this.currencySymbol(),
              status: "DRAFT",
              notes: null,
              isModified: true,
            });
          }
        }

        this.rows.set(currentRows);
        this.hasUnsavedChanges.set(true);
        this.previewTrigger$.next();

        this.snack.open(
          `Added ${selectedWorkers.length} worker(s) to attendance draft.`,
          "Close",
          { duration: 3000 },
        );
      });
  }

  // Top Action: Add Single Worker
  openAddWorkerDialog(initialSearchQuery?: string | null): void {
    const farm = this.selectedFarm();
    if (!farm) return;

    const initialSearch =
      initialSearchQuery?.trim() ||
      this.topControlsForm.controls.searchWorker.value?.trim() ||
      null;

    const dialogRef = this.dialog.open(AttendanceAddWorkerDialogComponent, {
      width: "880px",
      maxWidth: "95vw",
      data: {
        farmId: farm.id,
        farmName: farm.name,
        attendanceDate: this.formattedDate(),
        alreadyAddedWorkerIds: this.alreadyAddedWorkerIds(),
        initialSearch,
      } as AttendanceAddWorkerDialogData,
    });

    dialogRef
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result: AttendanceAddWorkerResult | undefined) => {
        if (!result) return;

        const w = result.worker;
        const currentRows = [...this.rows()];

        // Prevent adding worker twice
        if (currentRows.some((r) => r.workerId === w.workerId)) {
          this.snack.open(
            `${w.displayName} is already in today's attendance roster.`,
            "Close",
            { duration: 3000 },
          );
          return;
        }

        currentRows.push({
          workerId: w.workerId,
          workerDisplayName: w.displayName,
          workerFirstName: w.firstName,
          workerLastName: w.lastName,
          gender: w.gender,
          mobileNumber: w.mobileNumber,
          laborCategoryName: w.laborCategoryName,
          contractorName: w.contractorName,
          employmentType: w.employmentType,
          attendanceType: result.attendanceType,
          workingHours: result.workingHours,
          calculatedRate: null,
          calculatedAmount: null,
          currencySymbol: this.currencySymbol(),
          status: "DRAFT",
          notes: result.notes,
          isModified: true,
        });

        this.rows.set(currentRows);
        this.hasUnsavedChanges.set(true);
        this.previewTrigger$.next();

        this.snack.open(
          `Added ${w.displayName} to attendance draft.`,
          "Close",
          { duration: 3000 },
        );
      });
  }

  // Top Action: Copy Previous Day
  openCopyPreviousDayDialog(): void {
    const farm = this.selectedFarm();
    if (!farm || this.isFinalized()) return;

    const dialogRef = this.dialog.open(
      AttendanceCopyPreviousDayDialogComponent,
      {
        width: "680px",
        maxWidth: "95vw",
        data: {
          farmId: farm.id,
          farmName: farm.name,
          targetDate: this.formattedDate(),
          currentDraftCount: this.rows().length,
          hasUnsavedChanges: this.hasUnsavedChanges(),
        } as AttendanceCopyPreviousDayDialogData,
      },
    );

    dialogRef
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result: CopyPreviousDayAttendanceResponse | undefined) => {
        if (!result) return;

        const updatedRows: AttendanceGridRow[] = (
          result.dailyAttendance.records || []
        ).map((r) => ({
          id: r.id,
          workerId: r.workerId,
          workerDisplayName: r.workerDisplayName,
          workerFirstName: r.workerFirstName,
          workerLastName: r.workerLastName,
          gender: r.gender,
          mobileNumber: null,
          laborCategoryName: r.laborCategoryName,
          contractorName: null,
          employmentType: r.employmentType,
          attendanceType: (r.attendanceType as AttendanceType) || "FULL_DAY",
          workingHours: r.workingHours,
          calculatedRate: r.calculatedRate,
          calculatedAmount: r.calculatedAmount,
          currencySymbol: r.currencySymbol || this.currencySymbol(),
          status: "DRAFT",
          notes: r.notes,
          isModified: false,
        }));

        this.rows.set(updatedRows);
        this.summary.set(result.dailyAttendance.summary);
        this.hasUnsavedChanges.set(false);
        this.lastCopiedExcludedWorkers.set(
          result.excludedWorkers ? [...result.excludedWorkers] : [],
        );

        let message = `Copied ${result.copiedCount} worker(s) from ${result.sourceDate} into today's draft.`;
        if (result.excludedCount > 0) {
          message += ` (${result.excludedCount} worker(s) excluded due to ineligibility)`;
        }

        this.snack.open(message, "Close", { duration: 6000 });
      });
  }

  // Primary Action: Save Draft
  saveDraft(): void {
    const farmId = this.selectedFarmId();
    const date = this.formattedDate();

    if (!farmId || !date) return;
    if (this.isFinalized()) return;

    if (this.hasInvalidHourlyRows()) {
      this.snack.open(
        "Please enter valid working hours (0.25 - 24) for all hourly workers before saving.",
        "Close",
        { duration: 4000 },
      );
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const payload: SaveDailyDraftAttendanceBatchRequest = {
      farmId,
      attendanceDate: date,
      removeOmittedDrafts: true,
      items: this.rows().map((r) => ({
        id: r.id || null,
        workerId: r.workerId,
        attendanceType: r.attendanceType,
        workingHours: r.attendanceType === "HOURLY" ? r.workingHours : null,
        notes: r.notes,
      })),
    };

    this.attendanceService
      .saveDailyDraftBatch(payload)
      .pipe(
        finalize(() => this.isSaving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (res) => {
          this.hasUnsavedChanges.set(false);
          this.summary.set(res.summary);

          // Update rows with returned IDs & backend calculations
          const recordMap = new Map(res.records.map((r) => [r.workerId, r]));
          const updated = this.rows().map((r) => {
            const saved = recordMap.get(r.workerId);
            if (saved) {
              return {
                ...r,
                id: saved.id,
                calculatedRate: saved.calculatedRate,
                calculatedAmount: saved.calculatedAmount,
                status: saved.status as any,
                isModified: false,
                hoursError: null,
              };
            }
            return r;
          });

          this.rows.set(updated);

          this.snack.open("Draft attendance saved successfully.", "Close", {
            duration: 3000,
          });
        },
        error: (err) => {
          this.errorMessage.set(
            getApiErrorMessage(err, "Failed to save draft attendance."),
          );
        },
      });
  }

  // Primary Action: Finalize Attendance
  openFinalizeDialog(): void {
    const farm = this.selectedFarm();
    if (!farm || this.rows().length === 0 || this.isFinalized()) return;

    if (this.hasInvalidHourlyRows()) {
      this.snack.open(
        "Please enter valid working hours (0.25 - 24) for all hourly workers before finalizing.",
        "Close",
        { duration: 4000 },
      );
      return;
    }

    const totalHours = this.rows()
      .filter((r) => r.attendanceType === "HOURLY" && r.workingHours && r.workingHours > 0)
      .reduce((sum, r) => sum + (r.workingHours || 0), 0);

    const dialogRef = this.dialog.open(AttendanceFinalizeDialogComponent, {
      width: "540px",
      data: {
        farmName: farm.name,
        attendanceDate: this.formattedDate(),
        summary: this.summary(),
        currencySymbol: this.currencySymbol(),
        totalHours,
      } as AttendanceFinalizeDialogData,
    });

    dialogRef
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((confirmed: boolean | undefined) => {
        if (!confirmed) return;
        this.executeFinalization();
      });
  }

  private executeFinalization(): void {
    const farmId = this.selectedFarmId();
    const date = this.formattedDate();
    if (!farmId || !date) return;

    if (this.hasInvalidHourlyRows()) {
      this.snack.open(
        "Please enter valid working hours (0.25 - 24) for all hourly workers before finalizing.",
        "Close",
        { duration: 4000 },
      );
      return;
    }

    this.isFinalizing.set(true);
    this.errorMessage.set(null);
    this.apiError.set(null);

    // If there are unsaved changes, save batch first then finalize
    const saveFirst$ = this.hasUnsavedChanges()
      ? this.attendanceService.saveDailyDraftBatch({
          farmId,
          attendanceDate: date,
          removeOmittedDrafts: true,
          items: this.rows().map((r) => ({
            id: r.id || null,
            workerId: r.workerId,
            attendanceType: r.attendanceType,
            workingHours: r.attendanceType === "HOURLY" ? r.workingHours : null,
            notes: r.notes,
          })),
        })
      : null;

    if (saveFirst$) {
      saveFirst$
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.hasUnsavedChanges.set(false);
            this.callFinalizeEndpoint(farmId, date);
          },
          error: (err) => {
            this.isFinalizing.set(false);
            this.apiError.set(err);
            this.errorMessage.set(
              getApiErrorMessage(err, "Failed to save draft before finalization."),
            );
          },
        });
    } else {
      this.callFinalizeEndpoint(farmId, date);
    }
  }

  private callFinalizeEndpoint(farmId: string, date: string): void {
    this.attendanceService
      .finalizeAttendance({
        farmId,
        attendanceDate: date,
      })
      .pipe(
        finalize(() => this.isFinalizing.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (res) => {
          this.snack.open(
            `Attendance finalized! ${res.finalizedCount} records locked and posted to Worker Earnings Ledger.`,
            "Close",
            { duration: 5000 },
          );
          this.loadDailyAttendance();
        },
        error: (err) => {
          this.apiError.set(err);
          this.errorMessage.set(
            getApiErrorMessage(err, "Failed to finalize attendance."),
          );
        },
      });
  }

  // Display formatting helpers
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
