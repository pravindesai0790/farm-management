import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ExpenseCategory } from '../../../../core/expenses/expense-category.models';
import { ExpenseCategoryService } from '../../../../core/expenses/expense-category.service';
import { CreateExpenseRequest, Expense, UpdateExpenseRequest } from '../../../../core/expenses/expense.models';
import { ExpenseService } from '../../../../core/expenses/expense.service';
import { Supplier } from '../../../../core/expenses/supplier.models';
import { SupplierService } from '../../../../core/expenses/supplier.service';
import { CropCycle, CropCycleStage, Farm, FarmArea, Plantation } from '../../../../core/farm-management/farm-management.models';
import { FarmManagementService } from '../../../../core/farm-management/farm-management.service';
import { CurrencyItem } from '../../../../core/labor/labor.models';
import { getApiErrorMessage } from '../../../../core/models/api-error.model';
import { formatDateOnly, parseDateOnly } from '../../../../core/utils/date.utils';

export interface DirectExpenseEditorDialogData {
  readonly expense?: Expense;
}

@Component({
  selector: 'app-direct-expense-editor-dialog',
  standalone: true,
  providers: [provideNativeDateAdapter()],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './direct-expense-editor-dialog.component.html',
  styleUrl: './direct-expense-editor-dialog.component.scss',
})
export class DirectExpenseEditorDialogComponent implements OnInit {
  readonly data = inject<DirectExpenseEditorDialogData | null>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<DirectExpenseEditorDialogComponent>);
  private readonly fb = inject(FormBuilder);
  private readonly expenseService = inject(ExpenseService);
  private readonly expenseCategoryService = inject(ExpenseCategoryService);
  private readonly supplierService = inject(SupplierService);
  private readonly farmService = inject(FarmManagementService);
  private readonly snack = inject(MatSnackBar);

  readonly isSubmitting = signal(false);
  readonly isLoadingLookups = signal(false);
  readonly isLoadingAreas = signal(false);
  readonly isLoadingPlantations = signal(false);
  readonly isLoadingCycles = signal(false);
  readonly isLoadingStages = signal(false);

  readonly farms = signal<readonly Farm[]>([]);
  readonly categories = signal<readonly ExpenseCategory[]>([]);
  readonly suppliers = signal<readonly Supplier[]>([]);
  readonly currencies = signal<readonly CurrencyItem[]>([]);
  readonly areas = signal<readonly FarmArea[]>([]);
  readonly plantations = signal<readonly Plantation[]>([]);
  readonly cropCycles = signal<readonly CropCycle[]>([]);
  readonly cropCycleStages = signal<readonly CropCycleStage[]>([]);

  readonly today = new Date();

  readonly form = this.fb.group({
    farmId: [this.data?.expense?.farmId || '', [Validators.required]],
    expenseCategoryId: [this.data?.expense?.expenseCategoryId || '', [Validators.required]],
    expenseDate: [
      parseDateOnly(this.data?.expense?.expenseDate) ?? new Date(),
      [Validators.required],
    ],
    amount: [this.data?.expense?.amount ?? null, [Validators.required, Validators.min(0.01)]],
    currencyId: [this.data?.expense?.currencyId || '', [Validators.required]],
    description: [this.data?.expense?.description || '', [Validators.required, Validators.maxLength(500)]],
    supplierId: [this.data?.expense?.supplierId || ''],
    referenceNumber: [this.data?.expense?.referenceNumber || '', [Validators.maxLength(100)]],
    farmAreaId: [{ value: this.data?.expense?.farmAreaId || '', disabled: !this.data?.expense?.farmId }],
    plantationId: [{ value: this.data?.expense?.plantationId || '', disabled: !this.data?.expense?.farmAreaId }],
    cropCycleId: [{ value: this.data?.expense?.cropCycleId || '', disabled: !this.data?.expense?.plantationId }],
    cropCycleStageId: [{ value: this.data?.expense?.cropCycleStageId || '', disabled: !this.data?.expense?.cropCycleId }],
    attachmentReference: [this.data?.expense?.attachmentReference || '', [Validators.maxLength(500)]],
  });

  ngOnInit(): void {
    this.loadLookups();
    this.setupCascadingListeners();

    if (this.data?.expense) {
      this.populateExistingCascade(this.data.expense);
    }
  }

  loadLookups(): void {
    this.isLoadingLookups.set(true);

    this.farmService.listFarms(1, 100, '', true).subscribe({
      next: (res) => this.farms.set(res.items),
      error: () => {},
    });

    this.expenseCategoryService.list(1, 100, '', true).subscribe({
      next: (res) => this.categories.set(res.items),
      error: () => {},
    });

    this.supplierService.list(1, 100, '', true).subscribe({
      next: (res) => this.suppliers.set(res.items),
      error: () => {},
    });

    this.expenseService.listCurrencies().subscribe({
      next: (res) => {
        const activeCurrencies = res.filter((c) => c.isActive);
        this.currencies.set(activeCurrencies);

        // Auto-select INR for new expenses (fallback to USD or first available)
        if (!this.data?.expense) {
          const inrCurrency = activeCurrencies.find((c) => c.code?.toUpperCase() === 'INR');
          const defaultCurrency = inrCurrency ||
            activeCurrencies.find((c) => c.code?.toUpperCase() === 'USD') ||
            activeCurrencies[0];

          if (defaultCurrency) {
            this.form.patchValue({ currencyId: defaultCurrency.id });
          }
        }
        this.isLoadingLookups.set(false);
      },
      error: () => this.isLoadingLookups.set(false),
    });
  }

  private setupCascadingListeners(): void {
    // 1. Farm changes -> trigger Farm Areas
    this.form.controls.farmId.valueChanges.subscribe((farmId) => {
      this.resetCascadeDownFrom('farmArea');
      if (farmId) {
        this.form.controls.farmAreaId.enable();
        this.loadAreas(farmId);
      } else {
        this.form.controls.farmAreaId.disable();
      }
    });

    // 2. Farm Area changes -> trigger Plantations
    this.form.controls.farmAreaId.valueChanges.subscribe((farmAreaId) => {
      this.resetCascadeDownFrom('plantation');
      const farmId = this.form.controls.farmId.value;
      if (farmId && farmAreaId) {
        this.form.controls.plantationId.enable();
        this.loadPlantations(farmId, farmAreaId);
      } else {
        this.form.controls.plantationId.disable();
      }
    });

    // 3. Plantation changes -> trigger Crop Cycles
    this.form.controls.plantationId.valueChanges.subscribe((plantationId) => {
      this.resetCascadeDownFrom('cropCycle');
      const farmId = this.form.controls.farmId.value;
      const farmAreaId = this.form.controls.farmAreaId.value;
      if (farmId && plantationId) {
        this.form.controls.cropCycleId.enable();
        this.loadCropCycles(farmId, farmAreaId || undefined, plantationId);
      } else {
        this.form.controls.cropCycleId.disable();
      }
    });

    // 4. Crop Cycle changes -> trigger Crop Cycle Stages
    this.form.controls.cropCycleId.valueChanges.subscribe((cycleId) => {
      this.resetCascadeDownFrom('cropCycleStage');
      if (cycleId) {
        this.form.controls.cropCycleStageId.enable();
        this.loadStages(cycleId);
      } else {
        this.form.controls.cropCycleStageId.disable();
      }
    });
  }

  private resetCascadeDownFrom(level: 'farmArea' | 'plantation' | 'cropCycle' | 'cropCycleStage'): void {
    if (level === 'farmArea') {
      this.form.controls.farmAreaId.reset('', { emitEvent: false });
      this.form.controls.farmAreaId.disable();
      this.areas.set([]);
    }
    if (level === 'farmArea' || level === 'plantation') {
      this.form.controls.plantationId.reset('', { emitEvent: false });
      this.form.controls.plantationId.disable();
      this.plantations.set([]);
    }
    if (level === 'farmArea' || level === 'plantation' || level === 'cropCycle') {
      this.form.controls.cropCycleId.reset('', { emitEvent: false });
      this.form.controls.cropCycleId.disable();
      this.cropCycles.set([]);
    }
    this.form.controls.cropCycleStageId.reset('', { emitEvent: false });
    this.form.controls.cropCycleStageId.disable();
    this.cropCycleStages.set([]);
  }

  private loadAreas(farmId: string, preselectId?: string): void {
    this.isLoadingAreas.set(true);
    this.farmService.listAreas(farmId, true).subscribe({
      next: (areas) => {
        this.areas.set(areas);
        this.isLoadingAreas.set(false);
        if (preselectId) {
          this.form.controls.farmAreaId.setValue(preselectId, { emitEvent: false });
        }
      },
      error: () => {
        this.areas.set([]);
        this.isLoadingAreas.set(false);
      },
    });
  }

  private loadPlantations(farmId: string, farmAreaId?: string, preselectId?: string): void {
    this.isLoadingPlantations.set(true);
    this.farmService.listPlantations(1, 100, farmId, farmAreaId).subscribe({
      next: (res) => {
        this.plantations.set(res.items);
        this.isLoadingPlantations.set(false);
        if (preselectId) {
          this.form.controls.plantationId.setValue(preselectId, { emitEvent: false });
        }
      },
      error: () => {
        this.plantations.set([]);
        this.isLoadingPlantations.set(false);
      },
    });
  }

  private loadCropCycles(farmId: string, farmAreaId?: string, plantationId?: string, preselectId?: string): void {
    this.isLoadingCycles.set(true);
    this.farmService.listCycles(1, 100, farmId, farmAreaId, plantationId).subscribe({
      next: (res) => {
        this.cropCycles.set(res.items);
        this.isLoadingCycles.set(false);
        if (preselectId) {
          this.form.controls.cropCycleId.setValue(preselectId, { emitEvent: false });
        }
      },
      error: () => {
        this.cropCycles.set([]);
        this.isLoadingCycles.set(false);
      },
    });
  }

  private loadStages(cycleId: string, preselectId?: string): void {
    this.isLoadingStages.set(true);
    this.farmService.getCycleLifecycle(cycleId).subscribe({
      next: (lifecycle) => {
        this.cropCycleStages.set(lifecycle?.stages || []);
        this.isLoadingStages.set(false);
        if (preselectId) {
          this.form.controls.cropCycleStageId.setValue(preselectId, { emitEvent: false });
        }
      },
      error: () => {
        this.cropCycleStages.set([]);
        this.isLoadingStages.set(false);
      },
    });
  }

  private populateExistingCascade(expense: Expense): void {
    if (expense.farmId) {
      this.form.controls.farmAreaId.enable();
      this.loadAreas(expense.farmId, expense.farmAreaId || undefined);

      if (expense.farmAreaId) {
        this.form.controls.plantationId.enable();
        this.loadPlantations(expense.farmId, expense.farmAreaId, expense.plantationId || undefined);

        if (expense.plantationId) {
          this.form.controls.cropCycleId.enable();
          this.loadCropCycles(expense.farmId, expense.farmAreaId, expense.plantationId, expense.cropCycleId || undefined);

          if (expense.cropCycleId) {
            this.form.controls.cropCycleStageId.enable();
            this.loadStages(expense.cropCycleId, expense.cropCycleStageId || undefined);
          }
        }
      }
    }
  }

  onSubmit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    const val = this.form.getRawValue();
    const formattedDate = formatDateOnly(val.expenseDate) || '';

    if (this.data?.expense) {
      const request: UpdateExpenseRequest = {
        farmId: val.farmId!,
        expenseCategoryId: val.expenseCategoryId!,
        expenseDate: formattedDate,
        amount: Number(val.amount),
        currencyId: val.currencyId!,
        description: val.description!.trim(),
        supplierId: val.supplierId || null,
        referenceNumber: val.referenceNumber?.trim() || null,
        farmAreaId: val.farmAreaId || null,
        plantationId: val.plantationId || null,
        cropCycleId: val.cropCycleId || null,
        cropCycleStageId: val.cropCycleStageId || null,
        attachmentReference: val.attachmentReference?.trim() || null,
      };

      this.expenseService.updateDraft(this.data.expense.id, request).subscribe({
        next: (expense) => {
          this.snack.open('Expense draft updated successfully.', 'Close', { duration: 3000 });
          this.dialogRef.close(expense);
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.snack.open(getApiErrorMessage(err, 'Failed to update expense draft.'), 'Close', { duration: 5000 });
        },
      });
    } else {
      const request: CreateExpenseRequest = {
        farmId: val.farmId!,
        expenseCategoryId: val.expenseCategoryId!,
        expenseDate: formattedDate,
        amount: Number(val.amount),
        currencyId: val.currencyId!,
        description: val.description!.trim(),
        supplierId: val.supplierId || null,
        referenceNumber: val.referenceNumber?.trim() || null,
        farmAreaId: val.farmAreaId || null,
        plantationId: val.plantationId || null,
        cropCycleId: val.cropCycleId || null,
        cropCycleStageId: val.cropCycleStageId || null,
        attachmentReference: val.attachmentReference?.trim() || null,
      };

      this.expenseService.createDraft(request).subscribe({
        next: (expense) => {
          this.snack.open('Expense draft created successfully.', 'Close', { duration: 3000 });
          this.dialogRef.close(expense);
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.snack.open(getApiErrorMessage(err, 'Failed to create expense draft.'), 'Close', { duration: 5000 });
        },
      });
    }
  }
}
