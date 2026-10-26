import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  FormArray,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';

import { PermissionService } from '../../../core/auth/permission.service';
import {
  CropCycle,
  CropCycleStage,
  CycleList,
  Farm,
  FarmArea,
  Plantation,
  Unit,
} from '../../../core/farm-management/farm-management.models';
import { FarmManagementService } from '../../../core/farm-management/farm-management.service';
import { getApiErrorMessage } from '../../../core/models/api-error.model';
import {
  CreateFertilizerDraftRequest,
  FertilizerApplicationDetailsResponse,
  FertilizerApplicationMethodLookup,
  FertilizerApplicationStatus,
  FertilizerDraftProductLineRequest,
  FertilizerIrrigationLookup,
  FertilizerProductLookup,
  FertilizerStorageLocationLookup,
  UpdateFertilizerDraftRequest,
  getFertilizerStatusBadgeClass,
  getFertilizerStatusDisplayName,
} from '../../../core/fertilizers/fertilizer.models';
import { FertilizerService } from '../../../core/fertilizers/fertilizer.service';
import { parseDateOnly } from '../../../core/utils/date.utils';
import { DateTimePickerComponent } from '../../../shared/components/date-time-picker/date-time-picker.component';
import {
  ScheduleFertilizerDialogComponent,
  ScheduleFertilizerDialogData,
} from '../dialogs/schedule-fertilizer-dialog/schedule-fertilizer-dialog.component';
import { StartFertilizerDialogComponent } from '../dialogs/start-fertilizer-dialog/start-fertilizer-dialog.component';
import { CompleteFertilizerDialogComponent } from '../dialogs/complete-fertilizer-dialog/complete-fertilizer-dialog.component';
import { CancelFertilizerDialogComponent } from '../dialogs/cancel-fertilizer-dialog/cancel-fertilizer-dialog.component';

@Component({
  selector: 'app-fertilizer-editor-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatDividerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule,
    DateTimePickerComponent,
    RouterLink,
  ],
  templateUrl: './fertilizer-editor-page.component.html',
  styleUrl: './fertilizer-editor-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FertilizerEditorPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  private readonly farmService = inject(FarmManagementService);
  private readonly fertilizerService = inject(FertilizerService);
  readonly permissionService = inject(PermissionService);

  readonly id = this.route.snapshot.paramMap.get('id');
  readonly copyFromId = this.route.snapshot.queryParamMap.get('copyFrom');

  readonly isEditMode = computed(() => !!this.id);
  readonly isLoading = signal(false);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isStaleConflict = signal(false);
  readonly validationErrors = signal<readonly string[]>([]);

  readonly applicationDetails = signal<FertilizerApplicationDetailsResponse | null>(null);

  // Lookups
  readonly farms = signal<readonly Farm[]>([]);
  readonly areas = signal<readonly FarmArea[]>([]);
  readonly plantations = signal<readonly Plantation[]>([]);
  readonly cropCycles = signal<readonly CropCycle[]>([]);
  readonly stages = signal<readonly CropCycleStage[]>([]);
  readonly applicationMethods = signal<readonly FertilizerApplicationMethodLookup[]>([]);
  readonly irrigations = signal<readonly FertilizerIrrigationLookup[]>([]);
  readonly products = signal<readonly FertilizerProductLookup[]>([]);
  readonly storageLocations = signal<readonly FertilizerStorageLocationLookup[]>([]);
  readonly areaUnits = signal<readonly Unit[]>([]);
  readonly rowProducts = signal<{ [rowIndex: number]: readonly FertilizerProductLookup[] }>({});

  // Computed state
  readonly currentStatus = computed<FertilizerApplicationStatus>(
    () => this.applicationDetails()?.status ?? 'Draft',
  );
  readonly isDraft = computed(() => !this.id || this.currentStatus() === 'Draft');
  readonly isPlanned = computed(() => this.currentStatus() === 'Planned');
  readonly isScheduled = computed(() => this.currentStatus() === 'Scheduled');
  readonly isFrozen = computed(() => {
    const s = this.currentStatus();
    return s === 'InProgress' || s === 'Completed' || s === 'Cancelled';
  });
  readonly isCancelable = computed(() => {
    if (!this.id) return false;
    const s = this.currentStatus();
    return s === 'Draft' || s === 'Planned' || s === 'Scheduled' || s === 'InProgress';
  });

  readonly selectedMethodId = signal<string>('');

  readonly selectedMethod = computed(() => {
    const methodId = this.selectedMethodId();
    return this.applicationMethods().find((m) => m.id === methodId);
  });

  readonly isFertigation = computed(() => {
    const m = this.selectedMethod();
    return m?.code?.toUpperCase() === 'FERTIGATION';
  });

  readonly requiresOtherDescription = computed(() => {
    const m = this.selectedMethod();
    return !!m?.requiresDescription || m?.code?.toUpperCase() === 'OTHER';
  });


  // Form definition
  readonly form = this.fb.group({
    farmId: [''],
    farmAreaId: [''],
    plantationId: [''],
    cropCycleId: [''],
    cropCycleStageId: [''],
    applicationMethodId: [''],
    otherMethodDescription: ['', [Validators.maxLength(200)]],
    irrigationEventId: [''],
    plannedDate: [null as string | Date | null],
    scheduledDate: [null as string | Date | null],
    plannedArea: [null as number | null, [Validators.min(0)]],
    plannedAreaUnitId: [''],
    purposeReason: ['', [Validators.maxLength(500)]],
    products: this.fb.array<FormGroup>([]),
  });

  get productsArray(): FormArray<FormGroup> {
    return this.form.get('products') as FormArray<FormGroup>;
  }

  ngOnInit(): void {
    this.form
      .get('applicationMethodId')
      ?.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((val) => {
        this.selectedMethodId.set(val || '');
      });

    this.loadInitialLookups();

    if (this.id) {
      this.loadApplicationDetails(this.id);
    } else if (this.copyFromId) {
      this.loadTemplateFromCopy(this.copyFromId);
    }
  }

  private loadInitialLookups(): void {
    forkJoin({
      farms: this.farmService.listFarms(1, 100, '', true),
      methods: this.fertilizerService.getApplicationMethodsLookup(),
      products: this.fertilizerService.getProductsLookup(),
      units: this.farmService.listUnits('Area'),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.farms.set(res.farms.items);
          this.applicationMethods.set(res.methods);
          this.products.set(res.products);
          this.areaUnits.set(res.units);

          const currentMethodId = this.form.value.applicationMethodId;
          if (currentMethodId) {
            this.selectedMethodId.set(currentMethodId);
            const m = res.methods.find((x) => x.id === currentMethodId);
            if (m?.code?.toUpperCase() === 'FERTIGATION' && this.form.value.farmId) {
              this.loadIrrigations(this.form.value.farmId, this.form.value.farmAreaId || undefined);
            }
          }
        },
        error: (err) => {
          this.errorMessage.set(getApiErrorMessage(err, 'Failed to load initial master lookups.'));
        },
      });
  }

  private loadApplicationDetails(appId: string): void {
    this.isLoading.set(true);
    this.fertilizerService
      .get(appId)
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (app) => {
          this.applicationDetails.set(app);
          this.populateForm(app);
          if (this.isFrozen()) {
            this.form.disable();
          }
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, 'Failed to load application details.');
          this.errorMessage.set(msg);
          this.snack.open(msg, 'Dismiss', { duration: 4000 });
        },
      });
  }

  private loadTemplateFromCopy(sourceId: string): void {
    this.isLoading.set(true);
    this.fertilizerService
      .get(sourceId)
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (source) => {
          this.populateForm(source, true);
          this.snack.open(
            `Populated draft template from '${source.referenceNumber}'.`,
            'Dismiss',
            { duration: 4000 },
          );
        },
        error: (err) => {
          this.snack.open(
            getApiErrorMessage(err, 'Failed to load template application for copy.'),
            'Dismiss',
            { duration: 4000 },
          );
        },
      });
  }

  private populateForm(app: FertilizerApplicationDetailsResponse, isCopy = false): void {
    this.selectedMethodId.set(app.applicationMethodId || '');
    this.form.patchValue({
      farmId: app.farmId || '',
      farmAreaId: app.farmAreaId || '',
      plantationId: app.plantationId || '',
      cropCycleId: app.cropCycleId || '',
      cropCycleStageId: app.cropCycleStageId || '',
      applicationMethodId: app.applicationMethodId || '',
      otherMethodDescription: app.otherMethodDescription || '',
      irrigationEventId: app.irrigationEventId || '',
      plannedDate: isCopy ? null : app.plannedDate,
      scheduledDate: isCopy ? null : app.scheduledDate,
      plannedArea: app.plannedArea,
      plannedAreaUnitId: app.plannedAreaUnitId || '',
      purposeReason: app.purposeReason || '',
    });

    if (app.farmId) {
      this.loadFarmDependents(app.farmId, app.farmAreaId || undefined);
      if (app.irrigationEventId) {
        this.loadIrrigations(
          app.farmId,
          app.farmAreaId || undefined,
          app.irrigationEventId,
          app.irrigationEventReference || undefined,
        );
      }
    }
    if (app.plantationId) {
      this.loadPlantationCycles(app.plantationId);
    }
    if (app.cropCycleId) {
      this.loadCycleStages(app.cropCycleId);
    }

    this.productsArray.clear();
    if (app.products && app.products.length > 0) {
      for (const p of app.products) {
        this.addProduct({
          inventoryItemId: p.inventoryItemId,
          plannedQuantity: p.plannedQuantity ?? undefined,
          storageLocationId: p.storageLocationId || '',
        });
      }
    }
  }

  onFarmChange(selectedFarmId: string): void {
    this.form.patchValue({
      farmAreaId: '',
      plantationId: '',
      cropCycleId: '',
      cropCycleStageId: '',
      irrigationEventId: '',
    });
    this.areas.set([]);
    this.plantations.set([]);
    this.cropCycles.set([]);
    this.stages.set([]);
    this.storageLocations.set([]);
    this.irrigations.set([]);

    if (selectedFarmId) {
      const selectedFarm = this.farms().find((f) => f.id === selectedFarmId);
      if (selectedFarm && selectedFarm.totalArea) {
        this.form.patchValue({
          plannedArea: selectedFarm.totalArea,
          plannedAreaUnitId: selectedFarm.areaUnitId || '',
        });
      }
      this.loadFarmDependents(selectedFarmId);
      this.reloadAllRowProducts();
    }
  }

  onAreaChange(selectedAreaId: string): void {
    const farmId = this.form.value.farmId;
    this.form.patchValue({
      plantationId: '',
      cropCycleId: '',
      cropCycleStageId: '',
      irrigationEventId: '',
    });
    this.plantations.set([]);
    this.cropCycles.set([]);
    this.stages.set([]);

    if (farmId) {
      if (selectedAreaId) {
        const selectedArea = this.areas().find((a) => a.id === selectedAreaId);
        if (selectedArea && selectedArea.totalArea) {
          this.form.patchValue({
            plannedArea: selectedArea.totalArea,
            plannedAreaUnitId: selectedArea.areaUnitId || '',
          });
        }
      } else {
        const selectedFarm = this.farms().find((f) => f.id === farmId);
        if (selectedFarm && selectedFarm.totalArea) {
          this.form.patchValue({
            plannedArea: selectedFarm.totalArea,
            plannedAreaUnitId: selectedFarm.areaUnitId || '',
          });
        }
      }

      this.farmService
        .listPlantations(1, 100, farmId, selectedAreaId || undefined)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (res) => this.plantations.set(res.items),
        });

      if (this.isFertigation()) {
        this.loadIrrigations(farmId, selectedAreaId || undefined);
      }
    }
  }

  onPlantationChange(selectedPlantationId: string): void {
    this.form.patchValue({
      cropCycleId: '',
      cropCycleStageId: '',
    });
    this.cropCycles.set([]);
    this.stages.set([]);

    if (selectedPlantationId) {
      this.loadPlantationCycles(selectedPlantationId);
    }
  }

  onCropCycleChange(selectedCycleId: string): void {
    this.form.patchValue({
      cropCycleStageId: '',
    });
    this.stages.set([]);

    if (selectedCycleId) {
      this.loadCycleStages(selectedCycleId);
    }
  }

  onMethodChange(selectedMethodId?: string): void {
    const methodId = selectedMethodId || this.form.value.applicationMethodId || '';
    this.selectedMethodId.set(methodId);
    const method = this.selectedMethod();
    if (!method?.requiresDescription && method?.code?.toUpperCase() !== 'OTHER') {
      this.form.patchValue({ otherMethodDescription: '' });
    }

    if (method?.code?.toUpperCase() === 'FERTIGATION') {
      const farmId = this.form.value.farmId;
      const areaId = this.form.value.farmAreaId;
      if (farmId) {
        this.loadIrrigations(farmId, areaId || undefined);
      }
    } else {
      this.form.patchValue({ irrigationEventId: '' });
      this.irrigations.set([]);
    }
  }

  loadIrrigations(
    farmId: string,
    areaId?: string,
    currentEventId?: string,
    currentEventRef?: string,
  ): void {
    this.fertilizerService
      .getIrrigationsLookup(farmId, areaId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (irrs) => {
          const list = [...irrs];
          const targetId = currentEventId || this.form.value.irrigationEventId;
          if (targetId && !list.some((x) => x.id === targetId)) {
            list.unshift({
              id: targetId,
              farmId,
              farmAreaId: areaId || '',
              farmAreaName: this.applicationDetails()?.farmAreaName || 'Selected Area',
              cropCycleId: null,
              cropCycleName: null,
              status: 'Linked',
              plannedAt: null,
              scheduledAt: null,
              actualStartedAt: null,
              actualEndedAt: null,
              irrigationMethodName:
                currentEventRef ||
                this.applicationDetails()?.irrigationEventReference ||
                'Linked Irrigation',
              actualWaterQuantity: null,
              waterUnitSymbol: null,
            });
          }
          this.irrigations.set(list);
        },
        error: () => this.irrigations.set([]),
      });
  }

  private loadFarmDependents(farmId: string, areaId?: string): void {
    forkJoin({
      areas: this.farmService.listFarmAreas(1, 100, farmId),
      plantations: this.farmService.listPlantations(1, 100, farmId, areaId),
      locations: this.fertilizerService.getStorageLocationsLookup(farmId),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.areas.set(res.areas.items);
          this.plantations.set(res.plantations.items);
          this.storageLocations.set(res.locations);
        },
      });

    if (this.isFertigation()) {
      this.fertilizerService
        .getIrrigationsLookup(farmId, areaId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (irrs) => this.irrigations.set(irrs),
        });
    }
  }

  private loadPlantationCycles(plantationId: string): void {
    this.farmService
      .listCycles(1, 100, undefined, undefined, plantationId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res: CycleList) => this.cropCycles.set(res.items),
      });
  }

  private loadCycleStages(cycleId: string): void {
    this.fertilizerService
      .getCropCycleStages(cycleId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (stages) => this.stages.set(stages),
      });
  }

  // --- Dynamic Product Lines & Stock Tracking ---
  loadRowProducts(rowIndex: number, storageLocationId: string): void {
    const farmId = this.form.value.farmId;
    if (!storageLocationId) {
      const current = { ...this.rowProducts() };
      current[rowIndex] = this.products();
      this.rowProducts.set(current);
      return;
    }

    this.fertilizerService
      .getProductsLookup(storageLocationId, farmId || undefined)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (prods) => {
          const current = { ...this.rowProducts() };
          current[rowIndex] = prods;
          this.rowProducts.set(current);
        },
        error: () => {
          const current = { ...this.rowProducts() };
          current[rowIndex] = this.products();
          this.rowProducts.set(current);
        },
      });
  }

  reloadAllRowProducts(): void {
    this.productsArray.controls.forEach((ctrl, idx) => {
      const locId = ctrl.value.storageLocationId;
      if (locId) {
        this.loadRowProducts(idx, locId);
      }
    });
  }

  getRowProducts(rowIndex: number): readonly FertilizerProductLookup[] {
    const rowProds = this.rowProducts()[rowIndex];
    return rowProds && rowProds.length > 0 ? rowProds : this.products();
  }

  getSelectedProduct(rowIndex: number): FertilizerProductLookup | undefined {
    const row = this.productsArray.at(rowIndex);
    if (!row) return undefined;
    const itemId = row.value.inventoryItemId;
    if (!itemId) return undefined;
    return this.getRowProducts(rowIndex).find((p) => p.inventoryItemId === itemId);
  }

  isStockShortage(rowIndex: number): boolean {
    const row = this.productsArray.at(rowIndex);
    if (!row) return false;
    const qty = Number(row.value.plannedQuantity || 0);
    if (qty <= 0) return false;
    const prod = this.getSelectedProduct(rowIndex);
    if (!prod || prod.availableQuantity === undefined) return false;
    return qty > prod.availableQuantity;
  }

  onStorageLocationChange(rowIndex: number, selectedLocId: string): void {
    if (selectedLocId) {
      this.loadRowProducts(rowIndex, selectedLocId);
    } else {
      const current = { ...this.rowProducts() };
      delete current[rowIndex];
      this.rowProducts.set(current);
    }
  }

  addProduct(item?: Partial<FertilizerDraftProductLineRequest>): void {
    const row = this.fb.group({
      storageLocationId: [item?.storageLocationId || ''],
      inventoryItemId: [item?.inventoryItemId || '', [Validators.required]],
      plannedQuantity: [
        item?.plannedQuantity !== undefined ? item.plannedQuantity : null,
        [Validators.min(0)],
      ],
    });

    const index = this.productsArray.length;
    this.productsArray.push(row);

    if (item?.storageLocationId) {
      this.loadRowProducts(index, item.storageLocationId);
    }
  }

  removeProduct(index: number): void {
    this.productsArray.removeAt(index);
    const current = this.rowProducts();
    const updated: { [key: number]: readonly FertilizerProductLookup[] } = {};
    let newIdx = 0;
    for (let i = 0; i < this.productsArray.length + 1; i++) {
      if (i !== index && current[i]) {
        updated[newIdx] = current[i];
        newIdx++;
      }
    }
    this.rowProducts.set(updated);
  }

  getProductInfo(inventoryItemId: string | null | undefined): FertilizerProductLookup | undefined {
    if (!inventoryItemId) return undefined;
    return this.products().find((p) => p.inventoryItemId === inventoryItemId);
  }

  isProductOptionDisabled(inventoryItemId: string, currentRowIndex: number): boolean {
    const selectedIds = this.productsArray.controls
      .map((ctrl, idx) => (idx !== currentRowIndex ? ctrl.value.inventoryItemId : null))
      .filter(Boolean);
    return selectedIds.includes(inventoryItemId);
  }

  hasDuplicateProducts(): boolean {
    const ids = this.productsArray.controls
      .map((c) => c.value.inventoryItemId)
      .filter((id) => !!id);
    const uniqueIds = new Set(ids);
    return uniqueIds.size !== ids.length;
  }

  // --- Actions ---
  onSaveDraft(): void {
    if (this.hasDuplicateProducts()) {
      this.snack.open('Duplicate products are not allowed in the same application.', 'Dismiss', {
        duration: 4000,
      });
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.validationErrors.set([]);

    const payload = this.buildDraftPayload();

    if (this.id) {
      this.fertilizerService
        .updateDraft(this.id, payload as UpdateFertilizerDraftRequest)
        .pipe(
          finalize(() => this.isSubmitting.set(false)),
          takeUntilDestroyed(this.destroyRef),
        )
        .subscribe({
          next: (updated) => {
            this.applicationDetails.set(updated);
            this.snack.open(
              `Draft '${updated.referenceNumber}' updated successfully.`,
              'Dismiss',
              { duration: 3500 },
            );
          },
          error: (err) => this.handleApiError(err, 'Failed to update draft application.'),
        });
    } else {
      this.fertilizerService
        .createDraft(payload as CreateFertilizerDraftRequest)
        .pipe(
          finalize(() => this.isSubmitting.set(false)),
          takeUntilDestroyed(this.destroyRef),
        )
        .subscribe({
          next: (created) => {
            this.snack.open(
              `Draft application '${created.referenceNumber}' created.`,
              'Dismiss',
              { duration: 3500 },
            );
            this.router.navigate(['/fertilizer-applications', created.id, 'edit']);
          },
          error: (err) => this.handleApiError(err, 'Failed to create draft application.'),
        });
    }
  }

  onPlan(): void {
    const errors = this.validateForPlan();
    if (errors.length > 0) {
      this.validationErrors.set(errors);
      this.snack.open('Please fix the validation requirements before planning.', 'Dismiss', {
        duration: 4500,
      });
      return;
    }
    this.validationErrors.set([]);

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const draftPayload = this.buildDraftPayload();
    const saveObs = this.id
      ? this.fertilizerService.updateDraft(this.id, draftPayload as UpdateFertilizerDraftRequest)
      : this.fertilizerService.createDraft(draftPayload as CreateFertilizerDraftRequest);

    saveObs
      .pipe(
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (savedApp) => {
          this.applicationDetails.set(savedApp);
          this.fertilizerService
            .plan(savedApp.id, {
              plannedDate: this.form.value.plannedDate
              ? (typeof this.form.value.plannedDate === 'string'
                ? this.form.value.plannedDate
                : new Date(this.form.value.plannedDate).toISOString())
              : undefined,
              version: savedApp.version,
            })
            .pipe(
              finalize(() => this.isSubmitting.set(false)),
              takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
              next: (plannedApp) => {
                this.applicationDetails.set(plannedApp);
                this.snack.open(
                  `Fertilizer application '${plannedApp.referenceNumber}' planned successfully.`,
                  'Dismiss',
                  { duration: 4000 },
                );
                if (!this.id) {
                  this.router.navigate(['/fertilizer-applications', plannedApp.id, 'edit']);
                }
              },
              error: (err) => this.handleApiError(err, 'Failed to transition application to Planned.'),
            });
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.handleApiError(err, 'Failed to save application changes before planning.');
        },
      });
  }

  onSchedule(): void {
    const app = this.applicationDetails();
    if (!app) {
      this.snack.open('Please save the application first before scheduling.', 'Dismiss', {
        duration: 3500,
      });
      return;
    }

    const dialogData: ScheduleFertilizerDialogData = {
      applicationId: app.id,
      referenceNumber: app.referenceNumber,
      farmName: app.farmName,
      status: app.status,
      currentScheduledDate: app.scheduledDate,
      isReschedule: false,
      version: app.version,
    };

    const dialogRef = this.dialog.open(ScheduleFertilizerDialogComponent, {
      width: '460px',
      data: dialogData,
    });

    dialogRef.afterClosed().subscribe((res: FertilizerApplicationDetailsResponse | null) => {
      if (res) {
        this.applicationDetails.set(res);
        this.form.patchValue({
          scheduledDate: parseDateOnly(res.scheduledDate),
        });
      }
    });
  }

  onReschedule(): void {
    const app = this.applicationDetails();
    if (!app) return;

    const dialogData: ScheduleFertilizerDialogData = {
      applicationId: app.id,
      referenceNumber: app.referenceNumber,
      farmName: app.farmName,
      status: app.status,
      currentScheduledDate: app.scheduledDate,
      isReschedule: true,
      version: app.version,
    };

    const dialogRef = this.dialog.open(ScheduleFertilizerDialogComponent, {
      width: '460px',
      data: dialogData,
    });

    dialogRef.afterClosed().subscribe((res: FertilizerApplicationDetailsResponse | null) => {
      if (res) {
        this.applicationDetails.set(res);
        this.form.patchValue({
          scheduledDate: parseDateOnly(res.scheduledDate),
        });
      }
    });
  }

  onStart(): void {
    const app = this.applicationDetails();
    if (!app) return;

    const dialogRef = this.dialog.open(StartFertilizerDialogComponent, {
      width: '640px',
      data: { application: app },
    });

    dialogRef.afterClosed().subscribe((res) => {
      if (res) {
        this.loadApplicationDetails(app.id);
      }
    });
  }

  onComplete(): void {
    const app = this.applicationDetails();
    if (!app) return;

    const dialogRef = this.dialog.open(CompleteFertilizerDialogComponent, {
      width: '760px',
      data: { application: app },
    });

    dialogRef.afterClosed().subscribe((res) => {
      if (res) {
        this.loadApplicationDetails(app.id);
      }
    });
  }

  onCancelApplication(): void {
    const app = this.applicationDetails();
    if (!app) return;

    const dialogRef = this.dialog.open(CancelFertilizerDialogComponent, {
      width: '760px',
      data: { application: app },
    });

    dialogRef.afterClosed().subscribe((res) => {
      if (res) {
        this.loadApplicationDetails(app.id);
      }
    });
  }

  onCopyAsDraft(): void {
    const app = this.applicationDetails();
    if (!app) return;

    if (!confirm(`Create a new Draft copy from '${app.referenceNumber}'?`)) {
      return;
    }

    this.isSubmitting.set(true);
    this.fertilizerService
      .copyAsNewDraft(app.id)
      .pipe(
        finalize(() => this.isSubmitting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (newDraft) => {
          this.snack.open(
            `New draft '${newDraft.referenceNumber}' created from '${app.referenceNumber}'.`,
            'Open',
            { duration: 4000 },
          );
          this.router.navigate(['/fertilizer-applications', newDraft.id, 'edit']);
        },
        error: (err) => this.handleApiError(err, 'Failed to copy application as new draft.'),
      });
  }

  onDeleteDraft(): void {
    const app = this.applicationDetails();
    if (!app || app.status !== 'Draft') return;

    if (!confirm(`Are you sure you want to permanently delete draft '${app.referenceNumber}'? This action cannot be undone.`)) {
      return;
    }

    this.isSubmitting.set(true);
    this.fertilizerService
      .deleteDraft(app.id)
      .pipe(
        finalize(() => this.isSubmitting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.snack.open(`Draft '${app.referenceNumber}' deleted successfully.`, 'Dismiss', {
            duration: 3500,
          });
          this.router.navigate(['/fertilizer-applications']);
        },
        error: (err) => this.handleApiError(err, 'Failed to delete draft application.'),
      });
  }

  reloadAfterConflict(): void {
    if (this.id) {
      this.isStaleConflict.set(false);
      this.loadApplicationDetails(this.id);
    }
  }

  private validateForPlan(): string[] {
    const errors: string[] = [];
    const val = this.form.value;

    if (!val.farmId) errors.push('Farm is required to plan.');
    if (!val.farmAreaId) errors.push('Farm Area is required to plan.');
    if (!val.applicationMethodId) errors.push('Application method is required to plan.');
    if (this.requiresOtherDescription() && !val.otherMethodDescription?.trim()) {
      errors.push('Other method description is required.');
    }

    if (!val.plannedDate) {
      errors.push('Planned Date is required to plan.');
    }

    if (this.productsArray.length === 0) {
      errors.push('At least one fertilizer product line is required to plan.');
    }

    for (let i = 0; i < this.productsArray.length; i++) {
      const row = this.productsArray.at(i).value;
      const itemInfo = this.getProductInfo(row.inventoryItemId);
      const itemName = itemInfo?.itemName || `Line #${i + 1}`;

      if (!row.inventoryItemId) {
        errors.push(`${itemName}: Product selection is required.`);
      }
      if (row.plannedQuantity == null || Number(row.plannedQuantity) <= 0) {
        errors.push(`${itemName}: Positive planned quantity (> 0) is required.`);
      }
      if (!row.storageLocationId) {
        errors.push(`${itemName}: Storage location is required.`);
      }
    }

    if (this.hasDuplicateProducts()) {
      errors.push('Duplicate products are not allowed in the same application.');
    }

    return errors;
  }

  private buildDraftPayload(): CreateFertilizerDraftRequest | UpdateFertilizerDraftRequest {
    const val = this.form.value;
    const products: FertilizerDraftProductLineRequest[] = this.productsArray.controls
      .map((c) => ({
        inventoryItemId: c.value.inventoryItemId,
        plannedQuantity: c.value.plannedQuantity != null ? Number(c.value.plannedQuantity) : 0,
        storageLocationId: c.value.storageLocationId || null,
      }))
      .filter((p) => !!p.inventoryItemId);

    const toIso = (v: any) => {
      if (!v) return null;
      if (typeof v === 'string') return v;
      if (v instanceof Date && !isNaN(v.getTime())) return v.toISOString();
      return null;
    };

    const basePayload = {
      farmId: val.farmId || null,
      farmAreaId: val.farmAreaId || null,
      plantationId: val.plantationId || null,
      cropCycleId: val.cropCycleId || null,
      cropCycleStageId: val.cropCycleStageId || null,
      applicationMethodId: val.applicationMethodId || null,
      otherMethodDescription: val.otherMethodDescription?.trim() || null,
      irrigationEventId: val.irrigationEventId || null,
      plannedDate: toIso(val.plannedDate),
      scheduledDate: toIso(val.scheduledDate),
      plannedArea: val.plannedArea != null ? Number(val.plannedArea) : null,
      plannedAreaUnitId: val.plannedAreaUnitId || null,
      purposeReason: val.purposeReason?.trim() || null,
      products: products.length > 0 ? products : null,
    };

    if (this.id) {
      return {
        ...basePayload,
        version: this.applicationDetails()?.version,
      };
    }

    return basePayload;
  }

  private handleApiError(err: any, fallbackMessage: string): void {
    if (err?.status === 409) {
      this.isStaleConflict.set(true);
      this.errorMessage.set(
        'This application was updated by another user or session. Please reload to see the latest version before saving.',
      );
    } else {
      const msg = getApiErrorMessage(err, fallbackMessage);
      this.errorMessage.set(msg);
      this.snack.open(msg, 'Dismiss', { duration: 5000 });
    }
  }

  getStatusBadgeClass(status: FertilizerApplicationStatus): string {
    return getFertilizerStatusBadgeClass(status);
  }

  getStatusDisplayName(status: FertilizerApplicationStatus): string {
    return getFertilizerStatusDisplayName(status);
  }
}
