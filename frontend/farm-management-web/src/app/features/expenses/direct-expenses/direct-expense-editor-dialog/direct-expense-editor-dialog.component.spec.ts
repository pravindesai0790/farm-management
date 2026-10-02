import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { ExpenseCategoryService } from '../../../../core/expenses/expense-category.service';
import { Expense } from '../../../../core/expenses/expense.models';
import { ExpenseService } from '../../../../core/expenses/expense.service';
import { SupplierService } from '../../../../core/expenses/supplier.service';
import { FarmManagementService } from '../../../../core/farm-management/farm-management.service';
import { DirectExpenseEditorDialogComponent } from './direct-expense-editor-dialog.component';

describe('DirectExpenseEditorDialogComponent', () => {
  let component: DirectExpenseEditorDialogComponent;
  let fixture: ComponentFixture<DirectExpenseEditorDialogComponent>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<DirectExpenseEditorDialogComponent>>;
  let mockExpenseService: jasmine.SpyObj<ExpenseService>;
  let mockCategoryService: jasmine.SpyObj<ExpenseCategoryService>;
  let mockSupplierService: jasmine.SpyObj<SupplierService>;
  let mockFarmService: jasmine.SpyObj<FarmManagementService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const sampleExpense: Expense = {
    id: 'exp-1',
    organizationId: 'org-1',
    farmId: 'farm-1',
    farmName: 'Farm One',
    expenseCategoryId: 'cat-1',
    expenseCategoryName: 'Fertilizers',
    expenseDate: '2026-10-01',
    description: 'Direct fertilizer purchase',
    amount: 150.0,
    currencyId: 'curr-1',
    currencyCode: 'USD',
    currencySymbol: '$',
    status: 'Draft',
    createdAt: '2026-10-01T00:00:00Z',
    createdBy: 'u-1',
  };

  beforeEach(async () => {
    mockDialogRef = jasmine.createSpyObj('MatDialogRef', ['close']);
    mockExpenseService = jasmine.createSpyObj('ExpenseService', [
      'createDraft',
      'updateDraft',
      'listCurrencies',
    ]);
    mockCategoryService = jasmine.createSpyObj('ExpenseCategoryService', ['list']);
    mockSupplierService = jasmine.createSpyObj('SupplierService', ['list']);
    mockFarmService = jasmine.createSpyObj('FarmManagementService', [
      'listFarms',
      'listAreas',
      'listPlantations',
      'listCycles',
      'getCycleLifecycle',
    ]);
    mockSnackBar = jasmine.createSpyObj('MatSnackBar', ['open']);

    mockFarmService.listFarms.and.returnValue(
      of({ items: [{ id: 'farm-1', name: 'Farm One' } as any], totalCount: 1, page: 1, pageSize: 20 }),
    );
    mockCategoryService.list.and.returnValue(
      of({ items: [{ id: 'cat-1', name: 'Fertilizers' } as any], totalCount: 1, page: 1, pageSize: 20 }),
    );
    mockSupplierService.list.and.returnValue(
      of({ items: [{ id: 'sup-1', name: 'Supplier A' } as any], totalCount: 1, page: 1, pageSize: 20 }),
    );
    mockExpenseService.listCurrencies.and.returnValue(
      of([
        { id: 'curr-inr', code: 'INR', name: 'Indian Rupee', symbol: '₹', isSystem: true, isActive: true, displayOrder: 1 },
        { id: 'curr-usd', code: 'USD', name: 'US Dollar', symbol: '$', isSystem: true, isActive: true, displayOrder: 2 },
      ]),
    );
    mockFarmService.listAreas.and.returnValue(of([{ id: 'area-1', name: 'North Area' } as any]));
    mockFarmService.listPlantations.and.returnValue(
      of({ items: [{ id: 'plant-1', plantationName: 'Corn Plantation' } as any], totalCount: 1, page: 1, pageSize: 20 }),
    );
    mockFarmService.listCycles.and.returnValue(
      of({ items: [{ id: 'cycle-1', cycleName: 'Cycle 2026' } as any], totalCount: 1, page: 1, pageSize: 20 }),
    );
    mockFarmService.getCycleLifecycle.and.returnValue(
      of({
        cycleId: 'cycle-1',
        stages: [{ id: 'stage-1', stageName: 'Planting', sequenceNumber: 1 } as any],
      } as any),
    );

    await TestBed.configureTestingModule({
      imports: [DirectExpenseEditorDialogComponent, NoopAnimationsModule],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNativeDateAdapter(),
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: null },
        { provide: ExpenseService, useValue: mockExpenseService },
        { provide: ExpenseCategoryService, useValue: mockCategoryService },
        { provide: SupplierService, useValue: mockSupplierService },
        { provide: FarmManagementService, useValue: mockFarmService },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(DirectExpenseEditorDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('creates the component and initializes default values with INR auto-selected', () => {
    expect(component).toBeTruthy();
    expect(component.form.valid).toBeFalse();
    expect(component.currencies().length).toBe(2);
    expect(component.form.controls.currencyId.value).toBe('curr-inr');
    expect(component.farms().length).toBe(1);
    expect(component.categories().length).toBe(1);
    expect(component.form.controls.farmAreaId.disabled).toBeTrue();
    expect(component.form.controls.plantationId.disabled).toBeTrue();
    expect(component.form.controls.cropCycleId.disabled).toBeTrue();
    expect(component.form.controls.cropCycleStageId.disabled).toBeTrue();
  });

  it('validates required fields: farm, category, amount, description', () => {
    component.form.controls.farmId.setValue('');
    component.form.controls.expenseCategoryId.setValue('');
    component.form.controls.amount.setValue(null);
    component.form.controls.description.setValue('');

    expect(component.form.valid).toBeFalse();
    expect(component.form.controls.farmId.hasError('required')).toBeTrue();
    expect(component.form.controls.expenseCategoryId.hasError('required')).toBeTrue();
    expect(component.form.controls.amount.hasError('required')).toBeTrue();
    expect(component.form.controls.description.hasError('required')).toBeTrue();
  });

  it('validates amount must be greater than zero', () => {
    component.form.controls.amount.setValue(0);
    expect(component.form.controls.amount.hasError('min')).toBeTrue();

    component.form.controls.amount.setValue(-5);
    expect(component.form.controls.amount.hasError('min')).toBeTrue();

    component.form.controls.amount.setValue(10);
    expect(component.form.controls.amount.hasError('min')).toBeFalse();
  });

  it('handles cascading operational linkages from farm down to crop cycle stage', () => {
    // 1. Select Farm -> farmAreaId enabled and areas loaded
    component.form.controls.farmId.setValue('farm-1');
    expect(mockFarmService.listAreas).toHaveBeenCalledWith('farm-1', true);
    expect(component.form.controls.farmAreaId.enabled).toBeTrue();
    expect(component.form.controls.plantationId.disabled).toBeTrue();
    expect(component.form.controls.cropCycleId.disabled).toBeTrue();
    expect(component.form.controls.cropCycleStageId.disabled).toBeTrue();

    // 2. Select Farm Area -> plantationId enabled and plantations loaded
    component.form.controls.farmAreaId.setValue('area-1');
    expect(mockFarmService.listPlantations).toHaveBeenCalledWith(1, 100, 'farm-1', 'area-1');
    expect(component.form.controls.plantationId.enabled).toBeTrue();
    expect(component.form.controls.cropCycleId.disabled).toBeTrue();
    expect(component.form.controls.cropCycleStageId.disabled).toBeTrue();

    // 3. Select Plantation -> cropCycleId enabled and cycles loaded
    component.form.controls.plantationId.setValue('plant-1');
    expect(mockFarmService.listCycles).toHaveBeenCalledWith(1, 100, 'farm-1', 'area-1', 'plant-1');
    expect(component.form.controls.cropCycleId.enabled).toBeTrue();
    expect(component.form.controls.cropCycleStageId.disabled).toBeTrue();

    // 4. Select Crop Cycle -> cropCycleStageId enabled and stages loaded
    component.form.controls.cropCycleId.setValue('cycle-1');
    expect(mockFarmService.getCycleLifecycle).toHaveBeenCalledWith('cycle-1');
    expect(component.form.controls.cropCycleStageId.enabled).toBeTrue();

    // 5. Select Crop Cycle Stage
    component.form.controls.cropCycleStageId.setValue('stage-1');
    expect(component.form.controls.cropCycleStageId.value).toBe('stage-1');

    // 6. Reset Farm -> everything resets and disables
    component.form.controls.farmId.setValue('');
    expect(component.form.controls.farmAreaId.disabled).toBeTrue();
    expect(component.form.controls.plantationId.disabled).toBeTrue();
    expect(component.form.controls.cropCycleId.disabled).toBeTrue();
    expect(component.form.controls.cropCycleStageId.disabled).toBeTrue();
    expect(component.form.controls.farmAreaId.value).toBe('');
    expect(component.form.controls.plantationId.value).toBe('');
    expect(component.form.controls.cropCycleId.value).toBe('');
    expect(component.form.controls.cropCycleStageId.value).toBe('');
  });

  it('submits a new draft expense successfully with operational linkages', () => {
    mockExpenseService.createDraft.and.returnValue(of(sampleExpense));

    component.form.controls.farmId.setValue('farm-1');
    component.form.controls.farmAreaId.setValue('area-1');
    component.form.controls.plantationId.setValue('plant-1');
    component.form.controls.cropCycleId.setValue('cycle-1');
    component.form.controls.cropCycleStageId.setValue('stage-1');

    component.form.patchValue({
      expenseCategoryId: 'cat-1',
      expenseDate: new Date('2026-10-01'),
      amount: 150.0,
      currencyId: 'curr-inr',
      description: 'Direct fertilizer purchase',
    });

    component.onSubmit();

    expect(mockExpenseService.createDraft).toHaveBeenCalledWith(
      jasmine.objectContaining({
        farmId: 'farm-1',
        farmAreaId: 'area-1',
        plantationId: 'plant-1',
        cropCycleId: 'cycle-1',
        cropCycleStageId: 'stage-1',
        amount: 150.0,
        currencyId: 'curr-inr',
      }),
    );
    expect(mockSnackBar.open).toHaveBeenCalledWith(
      'Expense draft created successfully.',
      'Close',
      jasmine.any(Object),
    );
    expect(mockDialogRef.close).toHaveBeenCalledWith(sampleExpense);
  });
});
