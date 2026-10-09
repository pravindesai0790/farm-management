import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from "@angular/core";
import { DatePipe } from "@angular/common";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatDialog } from "@angular/material/dialog";
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
import { Router, RouterLink } from "@angular/router";
import { finalize } from "rxjs";
import { PermissionService } from "../../../core/auth/permission.service";
import {
  CropCycle,
  CropCycleStage,
  CycleList,
  Farm,
  FarmArea,
  Plantation,
} from "../../../core/farm-management/farm-management.models";
import { FarmManagementService } from "../../../core/farm-management/farm-management.service";
import { getApiErrorMessage } from "../../../core/models/api-error.model";
import {
  SprayListItem,
  SprayListQuery,
  SprayStatus,
  TargetResponse,
} from "../../../core/sprays/spray.models";
import { SprayService } from "../../../core/sprays/spray.service";
import { CancelSprayDialogComponent } from "../dialogs/cancel-spray-dialog/cancel-spray-dialog.component";
import { ScheduleSprayDialogComponent } from "../dialogs/schedule-spray-dialog/schedule-spray-dialog.component";

@Component({
  selector: "app-spray-list-page",
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    MatButtonModule,
    MatCardModule,
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
  templateUrl: "./spray-list-page.component.html",
  styleUrl: "./spray-list-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SprayListPageComponent implements OnInit {
  private readonly sprayService = inject(SprayService);
  private readonly farmService = inject(FarmManagementService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);

  readonly displayedColumns = [
    "reference",
    "farm",
    "cropContext",
    "target",
    "dates",
    "products",
    "status",
    "actions",
  ];

  // Data signals
  readonly sprays = signal<readonly SprayListItem[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly isLoading = signal(false);

  // Cascading options
  readonly farms = signal<readonly Farm[]>([]);
  readonly areas = signal<readonly FarmArea[]>([]);
  readonly plantations = signal<readonly Plantation[]>([]);
  readonly cropCycles = signal<readonly CropCycle[]>([]);
  readonly stages = signal<readonly CropCycleStage[]>([]);
  readonly targets = signal<readonly TargetResponse[]>([]);

  // Filter signals
  readonly search = signal("");
  readonly farmId = signal("");
  readonly farmAreaId = signal("");
  readonly plantationId = signal("");
  readonly cropCycleId = signal("");
  readonly cropCycleStageId = signal("");
  readonly status = signal("");
  readonly targetId = signal("");
  readonly fromDate = signal("");
  readonly toDate = signal("");
  readonly includeOverdue = signal(false);

  readonly hasActiveFilters = computed(
    () =>
      !!this.search().trim() ||
      !!this.farmId() ||
      !!this.farmAreaId() ||
      !!this.plantationId() ||
      !!this.cropCycleId() ||
      !!this.cropCycleStageId() ||
      !!this.status() ||
      !!this.targetId() ||
      !!this.fromDate() ||
      !!this.toDate() ||
      this.includeOverdue(),
  );

  ngOnInit(): void {
    this.loadFarms();
    this.loadTargets();
    this.loadSprays();
  }

  loadFarms(): void {
    this.farmService
      .listFarms(1, 100, "", true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => this.farms.set(res.items),
      });
  }

  loadTargets(): void {
    this.sprayService
      .getTargets()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (targets) => this.targets.set(targets),
      });
  }

  loadSprays(): void {
    this.isLoading.set(true);

    const query: SprayListQuery = {
      pageNumber: this.pageIndex() + 1,
      pageSize: this.pageSize(),
      farmId: this.farmId() || undefined,
      farmAreaId: this.farmAreaId() || undefined,
      plantationId: this.plantationId() || undefined,
      cropCycleId: this.cropCycleId() || undefined,
      cropCycleStageId: this.cropCycleStageId() || undefined,
      status: this.status() || undefined,
      targetId: this.targetId() || undefined,
      fromDate: this.fromDate() || undefined,
      toDate: this.toDate() || undefined,
      search: this.search().trim() || undefined,
      includeOverdue: this.includeOverdue(),
    };

    this.sprayService
      .listSprays(query)
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (res) => {
          this.sprays.set(res.items);
          this.totalCount.set(res.totalCount);
        },
        error: (err) => {
          this.snack.open(getApiErrorMessage(err, "Failed to load spray applications."), "Dismiss", {
            duration: 4000,
          });
        },
      });
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
          next: (res: CycleList) => this.cropCycles.set(res.items),
        });
    }

    this.resetPageAndReload();
  }

  onCropCycleChange(selectedCycleId: string): void {
    this.cropCycleId.set(selectedCycleId);
    this.cropCycleStageId.set("");
    this.stages.set([]);

    if (selectedCycleId) {
      this.sprayService
        .getCropCycleStages(selectedCycleId)
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

  onStatusChange(val: string): void {
    this.status.set(val);
    this.resetPageAndReload();
  }

  onTargetChange(val: string): void {
    this.targetId.set(val);
    this.resetPageAndReload();
  }

  onSearchChange(text: string): void {
    this.search.set(text);
    this.resetPageAndReload();
  }

  onFromDateChange(date: string): void {
    this.fromDate.set(date);
    this.resetPageAndReload();
  }

  onToDateChange(date: string): void {
    this.toDate.set(date);
    this.resetPageAndReload();
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
    this.status.set("");
    this.targetId.set("");
    this.fromDate.set("");
    this.toDate.set("");
    this.includeOverdue.set(false);
    this.areas.set([]);
    this.plantations.set([]);
    this.cropCycles.set([]);
    this.stages.set([]);
    this.resetPageAndReload();
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadSprays();
  }

  private resetPageAndReload(): void {
    this.pageIndex.set(0);
    this.loadSprays();
  }

  // Interactive Actions
  openCancelDialog(spray: SprayListItem): void {
    const ref = this.dialog.open(CancelSprayDialogComponent, {
      width: "500px",
      data: {
        sprayId: spray.id,
        referenceNumber: spray.referenceNumber,
        farmName: spray.farmName,
        status: spray.statusName,
      },
    });

    ref.afterClosed().subscribe((result) => {
      if (result) {
        this.loadSprays();
      }
    });
  }

  openScheduleDialog(spray: SprayListItem, isReschedule: boolean = false): void {
    const ref = this.dialog.open(ScheduleSprayDialogComponent, {
      width: "480px",
      data: {
        sprayId: spray.id,
        referenceNumber: spray.referenceNumber,
        farmName: spray.farmName,
        currentScheduledDateTime: spray.scheduledDateTime,
        isReschedule,
      },
    });

    ref.afterClosed().subscribe((result) => {
      if (result) {
        this.loadSprays();
      }
    });
  }

  // Helpers for Status Pill
  getStatusClass(status: SprayStatus | string): string {
    const s = (status || "").toString().toLowerCase();
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

  getStatusLabel(status: SprayStatus | string): string {
    const s = (status || "").toString().toLowerCase();
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
        return status;
    }
  }
}
