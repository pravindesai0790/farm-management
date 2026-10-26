import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { DatePipe, DecimalPipe, NgClass } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router, RouterLink } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, finalize } from 'rxjs';
import { PermissionService } from '../../../core/auth/permission.service';
import {
  CropCycle,
  CropCycleStage,
  Farm,
  FarmArea,
  Plantation,
} from '../../../core/farm-management/farm-management.models';
import { FarmManagementService } from '../../../core/farm-management/farm-management.service';
import {
  FertilizerApplicationListItem,
  FertilizerApplicationListQuery,
  FertilizerApplicationStatus,
  FertilizerProductLookup,
  getFertilizerStatusBadgeClass,
  getFertilizerStatusDisplayName,
} from '../../../core/fertilizers/fertilizer.models';
import { FertilizerService } from '../../../core/fertilizers/fertilizer.service';
import { getApiErrorMessage } from '../../../core/models/api-error.model';
import { formatDateOnly } from '../../../core/utils/date.utils';
import { downloadFileBlob, extractBlobErrorMessage } from '../../../core/utils/download.utils';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { StartFertilizerDialogComponent } from '../dialogs/start-fertilizer-dialog/start-fertilizer-dialog.component';
import { CompleteFertilizerDialogComponent } from '../dialogs/complete-fertilizer-dialog/complete-fertilizer-dialog.component';
import { CancelFertilizerDialogComponent } from '../dialogs/cancel-fertilizer-dialog/cancel-fertilizer-dialog.component';
import { ScheduleFertilizerDialogComponent } from '../dialogs/schedule-fertilizer-dialog/schedule-fertilizer-dialog.component';

export interface StatusTabOption {
  readonly key: string;
  readonly label: string;
  readonly status: FertilizerApplicationStatus | '';
}

@Component({
  selector: 'app-fertilizer-list-page',
  standalone: true,
  imports: [
    DatePipe,
    DecimalPipe,
    FormsModule,
    NgClass,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
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
  templateUrl: './fertilizer-list-page.component.html',
  styleUrl: './fertilizer-list-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FertilizerListPageComponent implements OnInit {
  private readonly fertilizerService = inject(FertilizerService);
  private readonly farmService = inject(FarmManagementService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly cdr = inject(ChangeDetectorRef);
  readonly permissionService = inject(PermissionService);

  readonly displayedColumns = [
    'reference',
    'context',
    'method',
    'dates',
    'products',
    'area',
    'status',
    'actions',
  ];

  readonly statusTabs: readonly StatusTabOption[] = [
    { key: 'all', label: 'All', status: '' },
    { key: 'draft', label: 'Draft', status: 'Draft' },
    { key: 'planned', label: 'Planned', status: 'Planned' },
    { key: 'scheduled', label: 'Scheduled', status: 'Scheduled' },
    { key: 'in_progress', label: 'In Progress', status: 'InProgress' },
    { key: 'completed', label: 'Completed', status: 'Completed' },
    { key: 'cancelled', label: 'Cancelled', status: 'Cancelled' },
  ];

  readonly dateTypeOptions = [
    { value: 'CreatedDate', label: 'Created Date' },
    { value: 'PlannedDate', label: 'Planned Date' },
    { value: 'ScheduledDate', label: 'Scheduled Date' },
    { value: 'ActualApplicationDate', label: 'Actual Date' },
  ];

  // Data signals
  readonly applications = signal<readonly FertilizerApplicationListItem[]>([]);
  readonly totalCount = signal(0);
  readonly isLoading = signal(false);
  readonly isExporting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  // Tab & Pagination signals
  readonly selectedTab = signal<string>('all');
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);

  // Filter signals
  readonly search = signal('');
  readonly farmId = signal('');
  readonly farmAreaId = signal('');
  readonly plantationId = signal('');
  readonly cropCycleId = signal('');
  readonly cropCycleStageId = signal('');
  readonly inventoryItemId = signal('');
  readonly dateType = signal('CreatedDate');
  readonly fromDate = signal<Date | null>(null);
  readonly toDate = signal<Date | null>(null);

  // Master lookup signals
  readonly farms = signal<readonly Farm[]>([]);
  readonly areas = signal<readonly FarmArea[]>([]);
  readonly plantations = signal<readonly Plantation[]>([]);
  readonly cropCycles = signal<readonly CropCycle[]>([]);
  readonly cropCycleStages = signal<readonly CropCycleStage[]>([]);
  readonly products = signal<readonly FertilizerProductLookup[]>([]);

  private readonly searchSubject = new Subject<string>();

  readonly hasActiveFilters = computed(
    () =>
      !!this.search().trim() ||
      !!this.farmId() ||
      !!this.farmAreaId() ||
      !!this.plantationId() ||
      !!this.cropCycleId() ||
      !!this.cropCycleStageId() ||
      !!this.inventoryItemId() ||
      this.dateType() !== 'CreatedDate' ||
      !!this.fromDate() ||
      !!this.toDate(),
  );

  readonly activeFilterCount = computed(() => {
    let count = 0;
    if (this.search().trim()) count++;
    if (this.farmId()) count++;
    if (this.farmAreaId()) count++;
    if (this.plantationId()) count++;
    if (this.cropCycleId()) count++;
    if (this.cropCycleStageId()) count++;
    if (this.inventoryItemId()) count++;
    if (this.dateType() !== 'CreatedDate') count++;
    if (this.fromDate() || this.toDate()) count++;
    return count;
  });

  // Unit-aware summary: count per status from current page / total
  readonly currentTabStatus = computed(() => {
    const tab = this.statusTabs.find((t) => t.key === this.selectedTab());
    return tab?.status ?? '';
  });

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
    this.loadProducts();
    this.loadApplications();
  }

  loadFarms(): void {
    this.farmService
      .listFarms(1, 100, '', true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => this.farms.set(res.items),
        error: () => {},
      });
  }

  loadProducts(): void {
    this.fertilizerService
      .getProductsLookup()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (prods) => this.products.set(prods),
        error: () => {},
      });
  }

  loadApplications(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const query: FertilizerApplicationListQuery = {
      pageNumber: this.pageIndex() + 1,
      pageSize: this.pageSize(),
      status: this.currentTabStatus() || undefined,
      farmId: this.farmId() || undefined,
      farmAreaId: this.farmAreaId() || undefined,
      plantationId: this.plantationId() || undefined,
      cropCycleId: this.cropCycleId() || undefined,
      cropCycleStageId: this.cropCycleStageId() || undefined,
      inventoryItemId: this.inventoryItemId() || undefined,
      dateType: this.dateType() || undefined,
      fromDate: formatDateOnly(this.fromDate()) || undefined,
      toDate: formatDateOnly(this.toDate()) || undefined,
      search: this.search().trim() || undefined,
      sortBy: 'CreatedAt',
      sortDirection: 'desc',
    };

    this.fertilizerService
      .list(query)
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (res) => {
          this.applications.set(res.items);
          this.totalCount.set(res.totalCount);
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.errorMessage.set(getApiErrorMessage(err, 'Failed to load fertilizer applications.'));
          this.cdr.markForCheck();
        },
      });
  }

  selectTab(tabKey: string): void {
    if (this.selectedTab() === tabKey) return;
    this.selectedTab.set(tabKey);
    this.pageIndex.set(0);
    this.loadApplications();
  }

  onSearchInput(val: string): void {
    this.searchSubject.next(val);
  }

  onFarmChange(farmId: string): void {
    this.farmId.set(farmId);
    this.farmAreaId.set('');
    this.plantationId.set('');
    this.cropCycleId.set('');
    this.cropCycleStageId.set('');
    this.areas.set([]);
    this.plantations.set([]);
    this.cropCycles.set([]);
    this.cropCycleStages.set([]);

    if (farmId) {
      this.farmService
        .listFarmAreas(1, 100, farmId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (res) => this.areas.set(res.items),
        });

      this.farmService
        .listPlantations(1, 100, farmId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (res) => this.plantations.set(res.items),
        });
    }

    this.resetPageAndReload();
  }

  onAreaChange(areaId: string): void {
    this.farmAreaId.set(areaId);
    this.resetPageAndReload();
  }

  onPlantationChange(plantationId: string): void {
    this.plantationId.set(plantationId);
    this.cropCycleId.set('');
    this.cropCycleStageId.set('');
    this.cropCycles.set([]);
    this.cropCycleStages.set([]);

    if (plantationId) {
      this.farmService
        .listCycles(1, 100, undefined, undefined, plantationId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (res) => this.cropCycles.set(res.items),
        });
    }

    this.resetPageAndReload();
  }

  onCropCycleChange(cycleId: string): void {
    this.cropCycleId.set(cycleId);
    this.cropCycleStageId.set('');
    this.cropCycleStages.set([]);

    if (cycleId) {
      this.fertilizerService
        .getCropCycleStages(cycleId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (stages: readonly CropCycleStage[]) => this.cropCycleStages.set(stages),
        });
    }

    this.resetPageAndReload();
  }

  onCropCycleStageChange(stageId: string): void {
    this.cropCycleStageId.set(stageId);
    this.resetPageAndReload();
  }

  onProductChange(itemId: string): void {
    this.inventoryItemId.set(itemId);
    this.resetPageAndReload();
  }

  onDateTypeChange(type: string): void {
    this.dateType.set(type);
    if (this.fromDate() || this.toDate()) {
      this.resetPageAndReload();
    }
  }

  onFromDateChange(date: Date | null): void {
    this.fromDate.set(date);
    this.resetPageAndReload();
  }

  onToDateChange(date: Date | null): void {
    this.toDate.set(date);
    this.resetPageAndReload();
  }

  resetFilters(): void {
    this.search.set('');
    this.farmId.set('');
    this.farmAreaId.set('');
    this.plantationId.set('');
    this.cropCycleId.set('');
    this.cropCycleStageId.set('');
    this.inventoryItemId.set('');
    this.dateType.set('CreatedDate');
    this.fromDate.set(null);
    this.toDate.set(null);
    this.areas.set([]);
    this.plantations.set([]);
    this.cropCycles.set([]);
    this.cropCycleStages.set([]);
    this.resetPageAndReload();
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadApplications();
  }

  private resetPageAndReload(): void {
    this.pageIndex.set(0);
    this.loadApplications();
  }

  exportCsv(): void {
    if (this.isExporting()) return;
    this.isExporting.set(true);

    const query: FertilizerApplicationListQuery = {
      status: this.currentTabStatus() || undefined,
      farmId: this.farmId() || undefined,
      farmAreaId: this.farmAreaId() || undefined,
      plantationId: this.plantationId() || undefined,
      cropCycleId: this.cropCycleId() || undefined,
      cropCycleStageId: this.cropCycleStageId() || undefined,
      inventoryItemId: this.inventoryItemId() || undefined,
      dateType: this.dateType() || undefined,
      fromDate: formatDateOnly(this.fromDate()) || undefined,
      toDate: formatDateOnly(this.toDate()) || undefined,
      search: this.search().trim() || undefined,
      sortBy: 'CreatedAt',
      sortDirection: 'desc',
    };

    this.fertilizerService
      .exportCsv(query)
      .pipe(
        finalize(() => this.isExporting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (blob) => {
          const now = new Date();
          const timestamp = now.toISOString().replace(/[-:T]/g, '').slice(0, 14);
          const filename = `fertilizer_applications_${timestamp}.csv`;

          downloadFileBlob(blob, filename);

          this.snack.open('Fertilizer applications exported successfully.', 'Dismiss', {
            duration: 3500,
          });
        },
        error: async (err) => {
          const msg = await extractBlobErrorMessage(err, 'Failed to export fertilizer applications.');
          this.snack.open(msg, 'Dismiss', { duration: 4000 });
        },
      });
  }

  copyAsNewDraft(app: FertilizerApplicationListItem): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Copy as New Draft',
        message: `Create a new Draft copy of application '${app.referenceNumber}'? Operational context, method, purpose, and planned products will be duplicated into a separate Draft record.`,
        confirmText: 'Create Copy',
        cancelText: 'Cancel',
        color: 'primary',
        icon: 'content_copy',
      },
    });

    dialogRef.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;

      this.fertilizerService
        .copyAsNewDraft(app.id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (newDraft) => {
            this.snack
              .open(
                `New draft '${newDraft.referenceNumber}' created from '${app.referenceNumber}'.`,
                'Open',
                { duration: 5000 },
              )
              .onAction()
              .subscribe(() => {
                this.router.navigate(['/fertilizer-applications', newDraft.id, 'edit']);
              });
            this.loadApplications();
          },
          error: (err) => {
            this.snack.open(
              getApiErrorMessage(err, 'Failed to copy application as draft.'),
              'Dismiss',
              { duration: 4000 },
            );
          },
        });
    });
  }

  deleteDraft(app: FertilizerApplicationListItem): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Delete Fertilizer Draft',
        message: `Are you sure you want to permanently delete draft '${app.referenceNumber}'? This action cannot be undone.`,
        confirmText: 'Delete Draft',
        cancelText: 'Keep Draft',
        color: 'warn',
        icon: 'delete_forever',
      },
    });

    dialogRef.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;

      this.fertilizerService
        .deleteDraft(app.id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.snack.open(`Draft '${app.referenceNumber}' deleted successfully.`, 'Dismiss', {
              duration: 3500,
            });
            this.loadApplications();
          },
          error: (err) => {
            this.snack.open(
              getApiErrorMessage(err, 'Failed to delete draft application.'),
              'Dismiss',
              { duration: 4000 },
            );
          },
        });
    });
  }

  scheduleApplication(app: FertilizerApplicationListItem): void {
    const dialogRef = this.dialog.open(ScheduleFertilizerDialogComponent, {
      width: '500px',
      data: {
        applicationId: app.id,
        referenceNumber: app.referenceNumber,
        farmName: app.farmName,
        status: app.status,
        currentScheduledDate: app.scheduledDate,
        isReschedule: false,
        version: 1,
      },
    });
    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.loadApplications();
      }
    });
  }

  rescheduleApplication(app: FertilizerApplicationListItem): void {
    const dialogRef = this.dialog.open(ScheduleFertilizerDialogComponent, {
      width: '500px',
      data: {
        applicationId: app.id,
        referenceNumber: app.referenceNumber,
        farmName: app.farmName,
        status: app.status,
        currentScheduledDate: app.scheduledDate,
        isReschedule: true,
        version: 1,
      },
    });
    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.loadApplications();
      }
    });
  }

  startApplication(app: FertilizerApplicationListItem): void {
    this.fertilizerService
      .get(app.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (fullApp) => {
          const dialogRef = this.dialog.open(StartFertilizerDialogComponent, {
            width: '640px',
            data: { application: fullApp },
          });
          dialogRef.afterClosed().subscribe((result) => {
            if (result) {
              this.loadApplications();
            }
          });
        },
        error: (err) => {
          this.snack.open(
            getApiErrorMessage(err, 'Failed to load application details.'),
            'Dismiss',
            { duration: 4000 },
          );
        },
      });
  }

  completeApplication(app: FertilizerApplicationListItem): void {
    this.fertilizerService
      .get(app.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (fullApp) => {
          const dialogRef = this.dialog.open(CompleteFertilizerDialogComponent, {
            width: '760px',
            data: { application: fullApp },
          });
          dialogRef.afterClosed().subscribe((result) => {
            if (result) {
              this.loadApplications();
            }
          });
        },
        error: (err) => {
          this.snack.open(
            getApiErrorMessage(err, 'Failed to load application details.'),
            'Dismiss',
            { duration: 4000 },
          );
        },
      });
  }

  cancelApplication(app: FertilizerApplicationListItem): void {
    this.fertilizerService
      .get(app.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (fullApp) => {
          const dialogRef = this.dialog.open(CancelFertilizerDialogComponent, {
            width: '760px',
            data: { application: fullApp },
          });
          dialogRef.afterClosed().subscribe((result) => {
            if (result) {
              this.loadApplications();
            }
          });
        },
        error: (err) => {
          this.snack.open(
            getApiErrorMessage(err, 'Failed to load application details.'),
            'Dismiss',
            { duration: 4000 },
          );
        },
      });
  }

  getStatusBadgeClass(status: FertilizerApplicationStatus): string {
    return getFertilizerStatusBadgeClass(status);
  }

  getStatusDisplayName(status: FertilizerApplicationStatus): string {
    return getFertilizerStatusDisplayName(status);
  }

  getProductsTooltip(app: FertilizerApplicationListItem): string {
    if (!app.productsSummary || app.productsSummary.length === 0) {
      return 'No products configured';
    }
    return app.productsSummary
      .map((p) => {
        const qty = p.actualQuantity != null ? p.actualQuantity : (p.plannedQuantity ?? 0);
        return `${p.itemName}: ${qty} ${p.unitSymbol}`;
      })
      .join('\n');
  }
}
