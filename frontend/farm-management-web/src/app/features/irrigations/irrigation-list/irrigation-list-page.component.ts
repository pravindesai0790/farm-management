import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from "@angular/core";
import { DatePipe, DecimalPipe, UpperCasePipe } from "@angular/common";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatMenuModule } from "@angular/material/menu";
import { MatPaginatorModule, PageEvent } from "@angular/material/paginator";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTableModule } from "@angular/material/table";
import { MatTooltipModule } from "@angular/material/tooltip";
import { RouterLink } from "@angular/router";
import { Subject, debounceTime, distinctUntilChanged, finalize } from "rxjs";
import { PermissionService } from "../../../core/auth/permission.service";
import {
  CropCycle,
  CropCycleStage,
  Farm,
  FarmArea,
  Plantation,
} from "../../../core/farm-management/farm-management.models";
import { FarmManagementService } from "../../../core/farm-management/farm-management.service";
import {
  IrrigationListItem,
  IrrigationListQuery,
  IrrigationMethodDto,
  IrrigationStatus,
  IrrigationSummaryCounts,
} from "../../../core/irrigations/irrigation.models";
import { IrrigationService } from "../../../core/irrigations/irrigation.service";
import { getApiErrorMessage } from "../../../core/models/api-error.model";

@Component({
  selector: "app-irrigation-list-page",
  standalone: true,
  imports: [
    DatePipe,
    DecimalPipe,
    UpperCasePipe,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule,
    RouterLink,
  ],
  templateUrl: "./irrigation-list-page.component.html",
  styleUrl: "./irrigation-list-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IrrigationListPageComponent implements OnInit {
  private readonly irrigationService = inject(IrrigationService);
  private readonly farmService = inject(FarmManagementService);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly cdr = inject(ChangeDetectorRef);
  readonly permissionService = inject(PermissionService);

  readonly displayedColumns = [
    "reference",
    "location",
    "cropContext",
    "method",
    "dates",
    "waterQuantity",
    "status",
    "actions",
  ];

  // Data signals
  readonly irrigations = signal<readonly IrrigationListItem[]>([]);
  readonly summary = signal<IrrigationSummaryCounts | null>(null);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly isLoading = signal(false);
  readonly isSummaryLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  // Cascading options
  readonly farms = signal<readonly Farm[]>([]);
  readonly areas = signal<readonly FarmArea[]>([]);
  readonly plantations = signal<readonly Plantation[]>([]);
  readonly cropCycles = signal<readonly CropCycle[]>([]);
  readonly stages = signal<readonly CropCycleStage[]>([]);
  readonly methods = signal<readonly IrrigationMethodDto[]>([]);

  // Filter signals
  readonly search = signal("");
  readonly farmId = signal("");
  readonly farmAreaId = signal("");
  readonly plantationId = signal("");
  readonly cropCycleId = signal("");
  readonly cropCycleStageId = signal("");
  readonly irrigationMethodId = signal("");
  readonly status = signal("");
  readonly fromDate = signal("");
  readonly toDate = signal("");
  readonly includeOverdue = signal(false);

  private readonly searchSubject = new Subject<string>();

  readonly hasActiveFilters = computed(
    () =>
      !!this.search().trim() ||
      !!this.farmId() ||
      !!this.farmAreaId() ||
      !!this.plantationId() ||
      !!this.cropCycleId() ||
      !!this.cropCycleStageId() ||
      !!this.irrigationMethodId() ||
      !!this.status() ||
      !!this.fromDate() ||
      !!this.toDate() ||
      this.includeOverdue(),
  );

  ngOnInit(): void {
    this.searchSubject
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((val) => {
        this.search.set(val);
        this.resetPageAndReload();
      });

    this.loadFarms();
    this.loadMethods();
    this.loadSummary();
    this.loadIrrigations();
  }

  loadFarms(): void {
    this.farmService
      .listFarms(1, 100, "", true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => this.farms.set(res.items),
      });
  }

  loadMethods(): void {
    this.irrigationService
      .getMethods(true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (methods) => this.methods.set(methods),
      });
  }

  loadSummary(): void {
    this.isSummaryLoading.set(true);
    const farmFilter = this.farmId() || undefined;

    this.irrigationService
      .getSummaryCounts(farmFilter)
      .pipe(
        finalize(() => this.isSummaryLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (res) => this.summary.set(res),
        error: () => {
          // If summary fails, fallback to null without blocking table
          this.summary.set(null);
        },
      });
  }

  loadIrrigations(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const query: IrrigationListQuery = {
      pageNumber: this.pageIndex() + 1,
      pageSize: this.pageSize(),
      farmId: this.farmId() || undefined,
      farmAreaId: this.farmAreaId() || undefined,
      plantationId: this.plantationId() || undefined,
      cropCycleId: this.cropCycleId() || undefined,
      cropCycleStageId: this.cropCycleStageId() || undefined,
      irrigationMethodId: this.irrigationMethodId() || undefined,
      status: this.status() || undefined,
      fromDate: this.fromDate() || undefined,
      toDate: this.toDate() || undefined,
      search: this.search().trim() || undefined,
      includeOverdue: this.includeOverdue(),
    };

    this.irrigationService
      .listIrrigations(query)
      .pipe(
        finalize(() => {
          this.isLoading.set(false);
          this.cdr.markForCheck();
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (res) => {
          this.irrigations.set(res.items);
          this.totalCount.set(res.totalCount);
          this.cdr.markForCheck();
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to load irrigation events.");
          this.errorMessage.set(msg);
          this.snack.open(msg, "Dismiss", { duration: 4000 });
          this.cdr.markForCheck();
        },
      });
  }

  loadAll(): void {
    this.loadSummary();
    this.loadIrrigations();
  }

  // Cascading Filter Handlers
  onFarmChange(selectedFarmId: string): void {
    this.farmId.set(selectedFarmId);
    this.farmAreaId.set("");
    this.plantationId.set("");
    this.cropCycleId.set("");
    this.cropCycleStageId.set("");
    this.areas.set([]);
    this.plantations.set([]);
    this.cropCycles.set([]);
    this.stages.set([]);

    if (selectedFarmId) {
      this.farmService
        .listFarmAreas(1, 100, selectedFarmId, true)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (res) => this.areas.set(res.items),
        });

      this.farmService
        .listPlantations(1, 100, selectedFarmId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (res) => this.plantations.set(res.items),
        });
    }

    this.loadSummary();
    this.resetPageAndReload();
  }

  onAreaChange(selectedAreaId: string): void {
    this.farmAreaId.set(selectedAreaId);
    this.plantationId.set("");
    this.cropCycleId.set("");
    this.cropCycleStageId.set("");
    this.cropCycles.set([]);
    this.stages.set([]);

    if (this.farmId()) {
      this.farmService
        .listPlantations(1, 100, this.farmId(), selectedAreaId || undefined)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (res) => this.plantations.set(res.items),
        });
    }

    this.resetPageAndReload();
  }

  onPlantationChange(selectedPlantationId: string): void {
    this.plantationId.set(selectedPlantationId);
    this.cropCycleId.set("");
    this.cropCycleStageId.set("");
    this.cropCycles.set([]);
    this.stages.set([]);

    if (selectedPlantationId) {
      this.farmService
        .listCycles(1, 100, undefined, undefined, selectedPlantationId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (res) => this.cropCycles.set(res.items),
        });
    }

    this.resetPageAndReload();
  }

  onCropCycleChange(selectedCropCycleId: string): void {
    this.cropCycleId.set(selectedCropCycleId);
    this.cropCycleStageId.set("");
    this.stages.set([]);

    if (selectedCropCycleId) {
      this.irrigationService
        .getCropCycleStages(selectedCropCycleId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (stages) => this.stages.set(stages),
        });
    }

    this.resetPageAndReload();
  }

  onStageChange(selectedStageId: string): void {
    this.cropCycleStageId.set(selectedStageId);
    this.resetPageAndReload();
  }

  onMethodChange(selectedMethodId: string): void {
    this.irrigationMethodId.set(selectedMethodId);
    this.resetPageAndReload();
  }

  onStatusChange(selectedStatus: string): void {
    this.status.set(selectedStatus);
    this.resetPageAndReload();
  }

  onFromDateChange(value: Date | null): void {
    this.fromDate.set(value ? value.toISOString().split("T")[0] : "");
    this.resetPageAndReload();
  }

  onToDateChange(value: Date | null): void {
    this.toDate.set(value ? value.toISOString().split("T")[0] : "");
    this.resetPageAndReload();
  }

  onSearchChange(value: string): void {
    this.searchSubject.next(value);
  }

  toggleIncludeOverdue(): void {
    this.includeOverdue.update((v) => !v);
    this.resetPageAndReload();
  }

  clearFilters(): void {
    this.search.set("");
    this.farmId.set("");
    this.farmAreaId.set("");
    this.plantationId.set("");
    this.cropCycleId.set("");
    this.cropCycleStageId.set("");
    this.irrigationMethodId.set("");
    this.status.set("");
    this.fromDate.set("");
    this.toDate.set("");
    this.includeOverdue.set(false);
    this.areas.set([]);
    this.plantations.set([]);
    this.cropCycles.set([]);
    this.stages.set([]);

    this.loadSummary();
    this.resetPageAndReload();
  }

  // Quick Card Click Filter Shortcuts
  onKpiClick(filterType: "Scheduled" | "InProgress" | "Completed" | "Overdue"): void {
    if (filterType === "Overdue") {
      this.includeOverdue.set(true);
      this.status.set("");
    } else {
      this.status.set(filterType);
      this.includeOverdue.set(false);
    }
    this.resetPageAndReload();
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadIrrigations();
  }

  private resetPageAndReload(): void {
    this.pageIndex.set(0);
    this.loadIrrigations();
  }

  // UI Helpers
  getStatusClass(status: IrrigationStatus | string): string {
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

  getStatusLabel(status: IrrigationStatus | string): string {
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
        return status;
    }
  }
}
