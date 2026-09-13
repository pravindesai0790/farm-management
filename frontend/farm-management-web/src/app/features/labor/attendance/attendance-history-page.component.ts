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
import { FormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatNativeDateModule } from "@angular/material/core";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatPaginatorModule, PageEvent } from "@angular/material/paginator";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatSortModule, Sort } from "@angular/material/sort";
import { MatTableModule } from "@angular/material/table";
import { MatTooltipModule } from "@angular/material/tooltip";
import { Router, RouterLink } from "@angular/router";
import { Subject, debounceTime, distinctUntilChanged, finalize } from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import { BreadcrumbService } from "../../../core/breadcrumb/breadcrumb.service";
import { Farm } from "../../../core/farm-management/farm-management.models";
import { FarmManagementService } from "../../../core/farm-management/farm-management.service";
import {
  ATTENDANCE_TYPE_OPTIONS,
  AttendanceRecord,
  formatAttendanceStatus,
  formatAttendanceType,
  getAttendanceTypeBadgeClass,
} from "../../../core/labor/attendance.models";
import { AttendanceService } from "../../../core/labor/attendance.service";
import { WorkerItem } from "../../../core/labor/labor.models";
import { LaborService } from "../../../core/labor/labor.service";
import { getApiErrorMessage } from "../../../core/models/api-error.model";
import { formatDateOnly } from "../../../core/utils/date.utils";
import { ErrorAlertComponent } from "../../../shared/components/error-alert/error-alert.component";

@Component({
  selector: "app-attendance-history-page",
  standalone: true,
  imports: [
    CommonModule,
    ErrorAlertComponent,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatNativeDateModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSortModule,
    MatTableModule,
    MatTooltipModule,
    RouterLink,
  ],
  templateUrl: "./attendance-history-page.component.html",
  styleUrl: "./attendance-history-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AttendanceHistoryPageComponent implements OnInit {
  private readonly attendanceService = inject(AttendanceService);
  private readonly farmService = inject(FarmManagementService);
  private readonly laborService = inject(LaborService);
  private readonly breadcrumbService = inject(BreadcrumbService);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);

  readonly attendanceTypeOptions = ATTENDANCE_TYPE_OPTIONS;

  readonly displayedColumns: readonly string[] = [
    "date",
    "farm",
    "worker",
    "gender",
    "attendanceType",
    "hours",
    "earnings",
    "status",
    "actions",
  ];

  // Data & Pagination Signals
  readonly records = signal<readonly AttendanceRecord[]>([]);
  readonly totalCount = signal<number>(0);
  readonly pageIndex = signal<number>(0);
  readonly pageSize = signal<number>(20);
  readonly sortBy = signal<string>("date");
  readonly sortDescending = signal<boolean>(true);

  // Status Signals
  readonly isLoading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  // Filter Signals
  readonly farmId = signal<string>("");
  readonly workerId = signal<string>("");
  readonly attendanceType = signal<string>("");
  readonly status = signal<string>("");
  readonly search = signal<string>("");
  readonly fromDate = signal<Date | null>(null);
  readonly toDate = signal<Date | null>(null);

  // Master Data Signals
  readonly farms = signal<readonly Farm[]>([]);
  readonly workers = signal<readonly WorkerItem[]>([]);

  private readonly searchSubject$ = new Subject<string>();

  readonly hasActiveFilters = computed(
    () =>
      !!this.farmId() ||
      !!this.workerId() ||
      !!this.attendanceType() ||
      !!this.status() ||
      !!this.search() ||
      this.fromDate() !== null ||
      this.toDate() !== null,
  );

  ngOnInit(): void {
    this.breadcrumbService.setTrail([
      { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
      { label: "Labor", route: "/labor" },
      { label: "Daily Attendance", route: "/labor/attendance" },
      { label: "Attendance History" },
    ]);

    this.searchSubject$
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((term) => {
        this.search.set(term);
        this.pageIndex.set(0);
        this.loadHistory();
      });

    this.loadFarms();
    this.loadWorkers();
    this.loadHistory();
  }

  loadFarms(): void {
    this.farmService
      .listFarms(1, 100, "", true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => this.farms.set(res.items),
        error: (err) => {
          this.snack.open(
            getApiErrorMessage(err, "Failed to load farms."),
            "Dismiss",
            { duration: 4000 },
          );
        },
      });
  }

  loadWorkers(): void {
    this.laborService
      .listWorkers(1, 200, "", true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => this.workers.set(res.items),
        error: (err) => {
          this.snack.open(
            getApiErrorMessage(err, "Failed to load workers."),
            "Dismiss",
            { duration: 4000 },
          );
        },
      });
  }

  loadHistory(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const fromDateStr = formatDateOnly(this.fromDate());
    const toDateStr = formatDateOnly(this.toDate());

    this.attendanceService
      .getAttendanceHistory({
        farmId: this.farmId() || undefined,
        workerId: this.workerId() || undefined,
        fromDate: fromDateStr || undefined,
        toDate: toDateStr || undefined,
        attendanceType: this.attendanceType() || undefined,
        status: this.status() || undefined,
        search: this.search() || undefined,
        sortBy: this.sortBy(),
        sortDescending: this.sortDescending(),
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
      })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (response) => {
          this.records.set(response.items);
          this.totalCount.set(response.totalCount);
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to load attendance history.");
          this.errorMessage.set(msg);
        },
      });
  }

  onFarmChange(newFarmId: string): void {
    this.farmId.set(newFarmId);
    this.pageIndex.set(0);
    this.loadHistory();
  }

  onWorkerChange(newWorkerId: string): void {
    this.workerId.set(newWorkerId);
    this.pageIndex.set(0);
    this.loadHistory();
  }

  onAttendanceTypeChange(newType: string): void {
    this.attendanceType.set(newType);
    this.pageIndex.set(0);
    this.loadHistory();
  }

  onStatusChange(newStatus: string): void {
    this.status.set(newStatus);
    this.pageIndex.set(0);
    this.loadHistory();
  }

  onFromDateChange(date: Date | null): void {
    this.fromDate.set(date);
    this.pageIndex.set(0);
    this.loadHistory();
  }

  onToDateChange(date: Date | null): void {
    this.toDate.set(date);
    this.pageIndex.set(0);
    this.loadHistory();
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchSubject$.next(value);
  }

  onSortChange(sort: Sort): void {
    this.sortBy.set(sort.active);
    this.sortDescending.set(sort.direction === "desc");
    this.pageIndex.set(0);
    this.loadHistory();
  }

  pageChanged(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadHistory();
  }

  clearFilters(): void {
    this.farmId.set("");
    this.workerId.set("");
    this.attendanceType.set("");
    this.status.set("");
    this.search.set("");
    this.fromDate.set(null);
    this.toDate.set(null);
    this.pageIndex.set(0);
    this.loadHistory();
  }

  navigateToDetail(id: string): void {
    this.router.navigate(["/labor/attendance", id]);
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
