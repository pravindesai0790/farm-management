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
import { MatChipsModule } from "@angular/material/chips";
import { MatNativeDateModule } from "@angular/material/core";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatDialog, MatDialogModule } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatPaginatorModule, PageEvent } from "@angular/material/paginator";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTableModule } from "@angular/material/table";
import { MatTooltipModule } from "@angular/material/tooltip";
import { RouterLink } from "@angular/router";
import { finalize } from "rxjs";

import { PermissionService } from "../../../../core/auth/permission.service";
import {
  Farm,
  FarmArea,
  Plantation,
} from "../../../../core/farm-management/farm-management.models";
import { FarmManagementService } from "../../../../core/farm-management/farm-management.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import { formatDateOnly, parseDateOnly } from "../../../../core/utils/date.utils";
import {
  LaborActivityCancelDialogComponent,
} from "../../components/labor-activity-cancel-dialog/labor-activity-cancel-dialog.component";
import {
  LaborActivity,
  NamedReference,
} from "../../models/labor-activity.models";
import { LaborActivityService } from "../../services/labor-activity.service";

@Component({
  selector: "app-labor-activity-list-page",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatNativeDateModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule,
    RouterLink,
  ],
  templateUrl: "./labor-activity-list-page.component.html",
  styleUrl: "./labor-activity-list-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LaborActivityListPageComponent implements OnInit {
  private readonly activityService = inject(LaborActivityService);
  private readonly farmService = inject(FarmManagementService);
  private readonly snack = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);

  readonly displayedColumns: readonly string[] = [
    "date",
    "farm",
    "area",
    "plantation",
    "activityType",
    "status",
    "actions",
  ];

  // Data signals
  readonly activities = signal<readonly LaborActivity[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly isLoading = signal(false);

  // Filter signals
  readonly farmId = signal("");
  readonly farmAreaId = signal("");
  readonly plantationId = signal("");
  readonly activityTypeId = signal("");
  readonly status = signal("");
  readonly fromDate = signal<Date | null>(null);
  readonly toDate = signal<Date | null>(null);

  // Master data lookup signals
  readonly farms = signal<readonly Farm[]>([]);
  readonly areas = signal<readonly FarmArea[]>([]);
  readonly plantations = signal<readonly Plantation[]>([]);
  readonly activityTypes = signal<readonly NamedReference[]>([]);

  readonly hasActiveFilters = computed(
    () =>
      !!this.farmId() ||
      !!this.farmAreaId() ||
      !!this.plantationId() ||
      !!this.activityTypeId() ||
      !!this.status() ||
      this.fromDate() !== null ||
      this.toDate() !== null,
  );

  ngOnInit(): void {
    this.loadFarms();
    this.loadActivityTypes();
    this.loadPlantations();
    this.loadActivities();
  }

  loadFarms(): void {
    this.farmService
      .listFarms(1, 100, "", true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => this.farms.set(res.items),
        error: (err) => {
          this.snack.open(getApiErrorMessage(err, "Failed to load farms."), "Dismiss", { duration: 4000 });
        },
      });
  }

  loadActivityTypes(): void {
    this.activityService
      .getTypes()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (types) => this.activityTypes.set(types),
        error: (err) => {
          this.snack.open(getApiErrorMessage(err, "Failed to load activity types."), "Dismiss", { duration: 4000 });
        },
      });
  }

  loadPlantations(): void {
    this.farmService
      .listPlantations(
        1,
        100,
        this.farmId() || undefined,
        this.farmAreaId() || undefined,
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => this.plantations.set(res.items),
      });
  }

  onFarmChange(newFarmId: string): void {
    this.farmId.set(newFarmId);
    this.farmAreaId.set("");
    this.plantationId.set("");
    this.pageIndex.set(0);

    if (newFarmId) {
      this.farmService
        .listAreas(newFarmId, true)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (areas) => this.areas.set(areas),
        });
    } else {
      this.areas.set([]);
    }

    this.loadPlantations();
    this.loadActivities();
  }

  onAreaChange(newAreaId: string): void {
    this.farmAreaId.set(newAreaId);
    this.plantationId.set("");
    this.pageIndex.set(0);
    this.loadPlantations();
    this.loadActivities();
  }

  onPlantationChange(newPlantationId: string): void {
    this.plantationId.set(newPlantationId);
    this.pageIndex.set(0);
    this.loadActivities();
  }

  onActivityTypeChange(newTypeId: string): void {
    this.activityTypeId.set(newTypeId);
    this.pageIndex.set(0);
    this.loadActivities();
  }

  onStatusChange(newStatus: string): void {
    this.status.set(newStatus);
    this.pageIndex.set(0);
    this.loadActivities();
  }

  onFromDateChange(date: Date | null): void {
    this.fromDate.set(date);
    this.pageIndex.set(0);
    this.loadActivities();
  }

  onToDateChange(date: Date | null): void {
    this.toDate.set(date);
    this.pageIndex.set(0);
    this.loadActivities();
  }

  clearFilters(): void {
    this.farmId.set("");
    this.farmAreaId.set("");
    this.plantationId.set("");
    this.activityTypeId.set("");
    this.status.set("");
    this.fromDate.set(null);
    this.toDate.set(null);
    this.areas.set([]);
    this.pageIndex.set(0);
    this.loadPlantations();
    this.loadActivities();
  }

  pageChanged(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadActivities();
  }

  loadActivities(): void {
    this.isLoading.set(true);
    this.activityService
      .list({
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
        farmId: this.farmId() || undefined,
        farmAreaId: this.farmAreaId() || undefined,
        plantationId: this.plantationId() || undefined,
        activityTypeId: this.activityTypeId() || undefined,
        fromDate: formatDateOnly(this.fromDate()) || undefined,
        toDate: formatDateOnly(this.toDate()) || undefined,
        status: this.status() || undefined,
      })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (response) => {
          this.activities.set(response.items);
          this.totalCount.set(response.totalCount);
        },
        error: (err) => {
          this.snack.open(getApiErrorMessage(err, "Failed to load labor activities."), "Dismiss", { duration: 5000 });
        },
      });
  }

  openCancelDialog(activity: LaborActivity): void {
    const dialogRef = this.dialog.open(LaborActivityCancelDialogComponent, {
      width: "480px",
      data: {
        activityId: activity.id,
        activityDate: activity.activityDate,
        activityTypeName: activity.activityType.name,
        farmName: activity.farm.name,
      },
    });

    dialogRef
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((reason: string | undefined) => {
        if (reason) {
          this.cancelActivity(activity.id, reason);
        }
      });
  }

  private cancelActivity(id: string, reason: string): void {
    this.isLoading.set(true);
    this.activityService
      .cancel(id, reason)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: () => {
          this.snack.open("Labor activity cancelled successfully.", "Dismiss", {
            duration: 4000,
          });
          this.loadActivities();
        },
        error: (err) => {
          this.snack.open(getApiErrorMessage(err, "Failed to cancel labor activity."), "Dismiss", { duration: 5000 });
        },
      });
  }
}
