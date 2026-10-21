import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { provideNativeDateAdapter } from '@angular/material/core';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { FarmManagementService } from '../../../../core/farm-management/farm-management.service';
import { FertilizerApplicationDetailsResponse } from '../../../../core/fertilizers/fertilizer.models';
import { FertilizerService } from '../../../../core/fertilizers/fertilizer.service';
import {
  CompleteFertilizerDialogComponent,
  CompleteFertilizerDialogData,
} from './complete-fertilizer-dialog.component';

describe('CompleteFertilizerDialogComponent', () => {
  let component: CompleteFertilizerDialogComponent;
  let fixture: ComponentFixture<CompleteFertilizerDialogComponent>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<CompleteFertilizerDialogComponent>>;
  let mockFertilizerService: jasmine.SpyObj<FertilizerService>;
  let mockFarmService: jasmine.SpyObj<FarmManagementService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const sampleApp: FertilizerApplicationDetailsResponse = {
    id: 'app-1',
    organizationId: 'org-1',
    referenceNumber: 'FA-202610-000001',
    status: 'InProgress',
    statusName: 'In Progress',
    farmId: 'farm-1',
    farmName: 'Green Valley Farm',
    farmAreaId: 'area-1',
    farmAreaName: 'North Sector',
    plantationId: 'plant-1',
    plantationName: 'Block A',
    cropCycleId: 'cycle-1',
    cropCycleName: '2026 Season',
    cropCycleStageId: null,
    cropCycleStageName: null,
    applicationMethodId: 'method-1',
    applicationMethodName: 'Fertigation',
    otherMethodDescription: null,
    irrigationEventId: null,
    irrigationEventReference: null,
    plannedDate: '2026-10-20',
    scheduledDate: null,
    actualApplicationDate: null,
    plannedArea: 5.0,
    plannedAreaUnitId: 'u-ha',
    plannedAreaUnitName: 'Hectares',
    actualArea: null,
    actualAreaUnitId: null,
    actualAreaUnitName: null,
    purposeReason: null,
    cancellationReason: null,
    products: [
      {
        id: 'line-1',
        fertilizerApplicationId: 'app-1',
        inventoryItemId: 'item-1',
        inventoryItemName: 'NPK 20-20-20',
        inventoryItemSku: 'FERT-001',
        stockUnitId: 'u-kg',
        stockUnitName: 'Kilograms',
        stockUnitCode: 'KG',
        stockUnitSymbol: 'kg',
        storageLocationId: 'loc-1',
        storageLocationName: 'Main Barn',
        plannedQuantity: 100,
        actualQuantity: null,
        varianceQuantity: null,
        stockMovementId: null,
        stockMovementReference: null,
      },
    ],
    createdAt: '2026-10-20T10:00:00Z',
    createdBy: 'user-1',
    updatedAt: null,
    updatedBy: null,
    startedAt: '2026-10-21T08:00:00Z',
    startedBy: 'user-1',
    completedAt: null,
    completedBy: null,
    cancelledAt: null,
    cancelledBy: null,
    version: 2,
  };

  const dialogData: CompleteFertilizerDialogData = {
    application: sampleApp,
  };

  beforeEach(async () => {
    mockDialogRef = jasmine.createSpyObj('MatDialogRef', ['close']);
    mockFertilizerService = jasmine.createSpyObj('FertilizerService', ['complete']);
    mockFarmService = jasmine.createSpyObj('FarmManagementService', ['listUnits']);
    mockSnackBar = jasmine.createSpyObj('MatSnackBar', ['open']);

    mockFarmService.listUnits.and.returnValue(
      of([
        {
          id: 'u-ha',
          name: 'Hectares',
          code: 'HA',
          symbol: 'ha',
          category: 'Area',
          unitCategory: 'Area',
          isBaseUnit: true,
          conversionFactor: 1,
          baseUnitId: null,
          description: null,
          isSystem: true,
          isActive: true,
          createdAt: '2026-01-01',
          createdBy: null,
          updatedAt: null,
          updatedBy: null,
        },
      ]),
    );

    await TestBed.configureTestingModule({
      imports: [CompleteFertilizerDialogComponent, NoopAnimationsModule],
      providers: [
        provideNativeDateAdapter(),
        { provide: MAT_DIALOG_DATA, useValue: dialogData },
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: FertilizerService, useValue: mockFertilizerService },
        { provide: FarmManagementService, useValue: mockFarmService },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CompleteFertilizerDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should initialize form with planned quantity and zero variance', () => {
    expect(component.form.valid).toBeTrue();
    expect(component.productsArray.length).toBe(1);
    expect(component.productsArray.at(0).value.actualQuantity).toBe(100);
    expect(component.getLineVariance('item-1')).toBe(0);
    expect(component.hasAnyVariance()).toBeFalse();
  });

  it('should invalidate future actual application date', () => {
    const future = new Date();
    future.setDate(future.getDate() + 5);
    component.form.patchValue({ actualApplicationDate: future });
    expect(component.form.get('actualApplicationDate')?.hasError('futureDate')).toBeTrue();
    expect(component.form.invalid).toBeTrue();
  });

  it('should invalidate zero or negative actual quantity', () => {
    component.productsArray.at(0).patchValue({ actualQuantity: 0 });
    expect(component.productsArray.at(0).get('actualQuantity')?.hasError('min')).toBeTrue();

    component.productsArray.at(0).patchValue({ actualQuantity: -5 });
    expect(component.productsArray.at(0).get('actualQuantity')?.hasError('min')).toBeTrue();
  });

  it('should detect variance and require acknowledgeVariance checkbox', () => {
    component.productsArray.at(0).patchValue({ actualQuantity: 120 });
    component.recalculateVariances();

    expect(component.getLineVariance('item-1')).toBe(20);
    expect(component.hasAnyVariance()).toBeTrue();

    // Without acknowledgement
    component.form.patchValue({ acknowledgeVariance: false });
    component.onSubmit();
    expect(mockFertilizerService.complete).not.toHaveBeenCalled();
    expect(component.errorMessage()).toContain('acknowledge');

    // With acknowledgement
    component.form.patchValue({ acknowledgeVariance: true });
    const completedApp = { ...sampleApp, status: 'Completed' as const, statusName: 'Completed' };
    mockFertilizerService.complete.and.returnValue(of(completedApp));

    component.onSubmit();
    expect(mockFertilizerService.complete).toHaveBeenCalledWith(
      'app-1',
      jasmine.objectContaining({
        actualArea: 5,
        acknowledgeVariance: true,
        version: 2,
        products: [{ inventoryItemId: 'item-1', actualQuantity: 120 }],
      }),
    );
    expect(mockDialogRef.close).toHaveBeenCalledWith(true);
  });

  it('should allow completion without variance checkbox when variance is zero', () => {
    component.productsArray.at(0).patchValue({ actualQuantity: 100 });
    component.recalculateVariances();
    expect(component.hasAnyVariance()).toBeFalse();

    const completedApp = { ...sampleApp, status: 'Completed' as const, statusName: 'Completed' };
    mockFertilizerService.complete.and.returnValue(of(completedApp));

    component.onSubmit();
    expect(mockFertilizerService.complete).toHaveBeenCalled();
    expect(mockDialogRef.close).toHaveBeenCalledWith(true);
  });

  it('should handle shortages returned from backend and keep dialog open', () => {
    const shortageError = {
      status: 400,
      error: {
        message: 'Validation failed',
        errors: {
          'products[0]': [
            "Insufficient stock for 'NPK 20-20-20' at 'Main Barn'. Required: 100, Available: 50, Shortage: 50 kg.",
          ],
        },
      },
    };
    mockFertilizerService.complete.and.returnValue(throwError(() => shortageError));

    component.onSubmit();

    expect(component.shortages().length).toBe(1);
    expect(mockDialogRef.close).not.toHaveBeenCalled();
  });
});
