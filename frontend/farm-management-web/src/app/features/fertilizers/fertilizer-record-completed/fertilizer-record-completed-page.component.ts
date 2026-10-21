import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormArray,
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router, RouterModule } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
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
  FertilizerApplicationMethodLookup,
  FertilizerIrrigationLookup,
  FertilizerProductLookup,
  FertilizerStorageLocationLookup,
  RecordCompletedFertilizerApplicationRequest,
  RecordCompletedFertilizerProductLineRequest,
} from '../../../core/fertilizers/fertilizer.models';
import { FertilizerService } from '../../../core/fertilizers/fertilizer.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { DateTimePickerComponent } from '../../../shared/components/date-time-picker/date-time-picker.component';

export function notFutureDateValidator(control: AbstractControl): ValidationErrors | null {
  const val = control.value;
  if (!val) return null;
  const selectedDate = new Date(val);
  const now = new Date();
  // Allow 1 minute clock tolerance
  if (selectedDate.getTime() > now.getTime() + 60000) {
    return { futureDate: true };
  }
  return null;
}

export interface DirectProductRowForm {
  inventoryItemId: FormControl<string>;
  storageLocationId: FormControl<string>;
  actualQuantity: FormControl<number | null>;
}

@Component({
  selector: 'app-fertilizer-record-completed-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatButtonModule,
    MatCardModule,
    MatDividerModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    DateTimePickerComponent,
  ],
  templateUrl: './fertilizer-record-completed-page.component.html',
  styleUrl: './fertilizer-record-completed-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FertilizerRecordCompletedPageComponent implements OnInit {
  private readonly fertilizerService = inject(FertilizerService);
  private readonly farmService = inject(FarmManagementService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  readonly isLoading = signal(false);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly shortages = signal<readonly string[]>([]);

  // Master Lookups
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

  readonly selectedMethodId = signal<string>('');
  readonly selectedMethod = computed(() => {
    const id = this.selectedMethodId();
    return this.applicationMethods().find((m) => m.id === id);
  });
  readonly isFertigation = computed(() => {
    return this.selectedMethod()?.code?.toUpperCase() === 'FERTIGATION';
  });
  readonly requiresOtherDescription = computed(() => {
    const m = this.selectedMethod();
    return !!m?.requiresDescription || m?.code?.toUpperCase() === 'OTHER';
  });

  readonly maxDate: Date = new Date();
  readonly isRetrospective = signal(false);

  readonly form = this.fb.group({
    farmId: ['', [Validators.required]],
    farmAreaId: ['', [Validators.required]],
    plantationId: [''],
    cropCycleId: [''],
    cropCycleStageId: [''],
    applicationMethodId: ['', [Validators.required]],
    otherMethodDescription: ['', [Validators.maxLength(200)]],
    irrigationEventId: [''],
    actualApplicationDate: [
      new Date().toISOString() as string | Date | null,
      [Validators.required, notFutureDateValidator],
    ],
    actualArea: [null as number | null, [Validators.min(0.0001)]],
    actualAreaUnitId: [''],
    purposeReason: ['', [Validators.maxLength(500)]],
    acknowledgeRetrospectiveStockDeduction: [false],
    products: this.fb.array<FormGroup<DirectProductRowForm>>([]),
  });

  get productsArray(): FormArray<FormGroup<DirectProductRowForm>> {
    return this.form.get('products') as FormArray<FormGroup<DirectProductRowForm>>;
  }

  ngOnInit(): void {
    this.form
      .get('applicationMethodId')
      ?.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((val) => {
        this.selectedMethodId.set(val || '');
      });

    this.form
      .get('actualApplicationDate')
      ?.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((val) => {
        this.checkIfRetrospective(val);
      });

    this.checkIfRetrospective(this.form.value.actualApplicationDate);

    this.loadInitialLookups();
    this.addProduct(); // Start with 1 empty line
  }

  private checkIfRetrospective(dateVal: string | Date | null | undefined): void {
    if (!dateVal) {
      this.isRetrospective.set(false);
      return;
    }
    const selected = new Date(dateVal);
    const startOfToday = new Date();
    startOfToday.setHours(0, 0, 0, 0);
    this.isRetrospective.set(selected.getTime() < startOfToday.getTime());
  }

  private loadInitialLookups(): void {
    this.isLoading.set(true);
    forkJoin({
      farms: this.farmService.listFarms(1, 100, '', true),
      methods: this.fertilizerService.getApplicationMethodsLookup(),
      products: this.fertilizerService.getProductsLookup(),
      units: this.farmService.listUnits('Area'),
    })
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
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
          this.errorMessage.set(getApiErrorMessage(err, 'Failed to load initial lookups.'));
        },
      });
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
          actualArea: selectedFarm.totalArea,
          actualAreaUnitId: selectedFarm.areaUnitId || '',
        });
      }

      forkJoin({
        areas: this.farmService.listFarmAreas(1, 100, selectedFarmId),
        plantations: this.farmService.listPlantations(1, 100, selectedFarmId),
        locations: this.fertilizerService.getStorageLocationsLookup(selectedFarmId),
      })
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (res) => {
            this.areas.set(res.areas.items);
            this.plantations.set(res.plantations.items);
            this.storageLocations.set(res.locations);
            this.reloadAllRowProducts();
          },
        });

      if (this.isFertigation()) {
        this.loadIrrigations(selectedFarmId);
      }
    }
  }

  onAreaChange(selectedAreaId: string): void {
    const farmId = this.form.value.farmId;
    this.form.patchValue({
      plantationId: '',
      cropCycleId: '',
      cropCycleStageId: '',
    });
    this.plantations.set([]);
    this.cropCycles.set([]);
    this.stages.set([]);

    if (farmId) {
      if (selectedAreaId) {
        const selectedArea = this.areas().find((a) => a.id === selectedAreaId);
        if (selectedArea && selectedArea.totalArea) {
          this.form.patchValue({
            actualArea: selectedArea.totalArea,
            actualAreaUnitId: selectedArea.areaUnitId || '',
          });
        }
      } else {
        const selectedFarm = this.farms().find((f) => f.id === farmId);
        if (selectedFarm && selectedFarm.totalArea) {
          this.form.patchValue({
            actualArea: selectedFarm.totalArea,
            actualAreaUnitId: selectedFarm.areaUnitId || '',
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
      this.farmService
        .listCycles(1, 100, undefined, undefined, selectedPlantationId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (res: CycleList) => this.cropCycles.set(res.items),
        });
    }
  }

  onCropCycleChange(selectedCycleId: string): void {
    this.form.patchValue({ cropCycleStageId: '' });
    this.stages.set([]);

    if (selectedCycleId) {
      this.fertilizerService
        .getCropCycleStages(selectedCycleId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (stages) => this.stages.set(stages),
        });
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

  private loadIrrigations(farmId: string, areaId?: string): void {
    this.fertilizerService
      .getIrrigationsLookup(farmId, areaId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (irrs) => this.irrigations.set(irrs),
        error: () => this.irrigations.set([]),
      });
  }

  // Stock Tracking & Dynamic Product Lines
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
    const qty = Number(row.value.actualQuantity || 0);
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

  addProduct(): void {
    const row = this.fb.group<DirectProductRowForm>({
      storageLocationId: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required],
      }),
      inventoryItemId: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required],
      }),
      actualQuantity: new FormControl(null, [
        Validators.required,
        Validators.min(0.0001),
      ]),
    });
    this.productsArray.push(row);
  }

  removeProduct(index: number): void {
    if (this.productsArray.length > 1) {
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

  onSaveDirectCompleted(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (this.hasDuplicateProducts()) {
      this.errorMessage.set(
        'Duplicate products detected. Please consolidate identical products into a single line.',
      );
      return;
    }

    if (this.requiresOtherDescription() && !this.form.value.otherMethodDescription?.trim()) {
      this.errorMessage.set('Other Method Description is required for the selected method.');
      return;
    }

    if (this.isRetrospective() && !this.form.value.acknowledgeRetrospectiveStockDeduction) {
      this.errorMessage.set(
        'Please acknowledge that retrospective applications deduct inventory from current stock balances.',
      );
      return;
    }

    // Exactly one confirmation dialog
    const farmName = this.farms().find((f) => f.id === this.form.value.farmId)?.name || 'Farm';
    const areaName = this.areas().find((a) => a.id === this.form.value.farmAreaId)?.name || 'Area';
    const rawDate = this.form.value.actualApplicationDate;
    const dateFormatted = rawDate ? new Date(rawDate).toLocaleString() : '';
    const productCount = this.productsArray.length;

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '480px',
      data: {
        title: 'Confirm Completed Fertilizer Application',
        message:
          'You are recording an irrevocable completed application. Warehouse inventory will be atomically deducted immediately upon creation.',
        confirmText: 'Confirm & Record',
        cancelText: 'Cancel',
        color: 'primary',
        icon: 'check_circle',
        details: [
          { label: 'Farm', value: farmName },
          { label: 'Area', value: areaName },
          { label: 'Actual Date & Time', value: dateFormatted },
          { label: 'Products', value: `${productCount} item(s)` },
          ...(this.isRetrospective()
            ? [{ label: 'Stock Deductions', value: 'Deducted from CURRENT warehouse balances' }]
            : []),
        ],
      },
    });

    dialogRef.afterClosed().subscribe((confirmed) => {
      if (confirmed) {
        this.executeRecordCompleted();
      }
    });
  }

  private executeRecordCompleted(): void {
    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.shortages.set([]);

    const formVal = this.form.value;
    const productRequests: RecordCompletedFertilizerProductLineRequest[] =
      (formVal.products || []).map((p) => ({
        inventoryItemId: p.inventoryItemId!,
        storageLocationId: p.storageLocationId!,
        actualQuantity: Number(p.actualQuantity),
      }));

    const rawDate = formVal.actualApplicationDate;
    const actualIsoDate = rawDate
      ? typeof rawDate === 'string'
        ? rawDate
        : (rawDate as Date).toISOString()
      : new Date().toISOString();

    const req: RecordCompletedFertilizerApplicationRequest = {
      farmId: formVal.farmId!,
      farmAreaId: formVal.farmAreaId!,
      plantationId: formVal.plantationId || undefined,
      cropCycleId: formVal.cropCycleId || undefined,
      cropCycleStageId: formVal.cropCycleStageId || undefined,
      applicationMethodId: formVal.applicationMethodId!,
      otherMethodDescription: formVal.otherMethodDescription || undefined,
      irrigationEventId: formVal.irrigationEventId || undefined,
      actualApplicationDate: actualIsoDate,
      actualArea: formVal.actualArea ? Number(formVal.actualArea) : undefined,
      actualAreaUnitId: formVal.actualAreaUnitId || undefined,
      purposeReason: formVal.purposeReason || undefined,
      acknowledgeRetrospectiveStockDeduction: !!formVal.acknowledgeRetrospectiveStockDeduction,
      idempotencyKey: crypto.randomUUID(),
      products: productRequests,
    };

    this.fertilizerService
      .recordCompleted(req)
      .pipe(
        finalize(() => this.isSubmitting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (created) => {
          this.snack.open(
            `Fertilizer application '${created.referenceNumber}' recorded successfully as Completed.`,
            'Dismiss',
            { duration: 4000 },
          );
          this.router.navigate(['/fertilizer-applications']);
        },
        error: (err) => {
          this.parseErrors(err);
        },
      });
  }

  private parseErrors(err: any): void {
    const extractedShortages: string[] = [];
    if (err?.error?.errors && typeof err.error.errors === 'object') {
      for (const key of Object.keys(err.error.errors)) {
        const val = err.error.errors[key];
        if (Array.isArray(val)) {
          extractedShortages.push(...val);
        } else if (typeof val === 'string') {
          extractedShortages.push(val);
        }
      }
    }

    if (extractedShortages.length > 0) {
      this.shortages.set(extractedShortages);
    }

    this.errorMessage.set(
      getApiErrorMessage(err, 'Failed to record completed fertilizer application.'),
    );
  }
}
