import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { provideNativeDateAdapter } from '@angular/material/core';
import { provideRouter, Router } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import {
  Farm,
  FarmArea,
  Plantation,
} from '../../../core/farm-management/farm-management.models';
import { FarmManagementService } from '../../../core/farm-management/farm-management.service';
import {
  FertilizerApplicationMethodLookup,
  FertilizerProductLookup,
  FertilizerStorageLocationLookup,
} from '../../../core/fertilizers/fertilizer.models';
import { FertilizerService } from '../../../core/fertilizers/fertilizer.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { FertilizerRecordCompletedPageComponent } from './fertilizer-record-completed-page.component';

describe('FertilizerRecordCompletedPageComponent', () => {
  let component: FertilizerRecordCompletedPageComponent;
  let fixture: ComponentFixture<FertilizerRecordCompletedPageComponent>;
  let mockFertilizerService: jasmine.SpyObj<FertilizerService>;
  let mockFarmService: jasmine.SpyObj<FarmManagementService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;
  let mockDialog: jasmine.SpyObj<MatDialog>;
  let router: Router;

  const sampleFarms: Farm[] = [
    {
      id: 'farm-1',
      name: 'Green Valley Farm',
      totalArea: 100,
      areaUnitId: 'u-ha',
      areaUnitCode: 'HA',
      areaUnitName: 'Hectares',
      areaUnitSymbol: 'ha',
      isActive: true,
    } as any,
  ];

  const sampleAreas: FarmArea[] = [
    {
      id: 'area-1',
      farmId: 'farm-1',
      name: 'North Sector',
      totalArea: 40,
      isActive: true,
    } as any,
  ];

  const samplePlantations: Plantation[] = [
    {
      id: 'plant-1',
      farmId: 'farm-1',
      farmAreaId: 'area-1',
      plantationName: 'Block A',
      allocatedArea: 10,
      isActive: true,
    } as any,
  ];

  const sampleMethods: FertilizerApplicationMethodLookup[] = [
    { id: 'method-1', code: 'FERTIGATION', name: 'Fertigation', description: null, displayOrder: 1, requiresDescription: false },
    { id: 'method-2', code: 'BROADCAST', name: 'Broadcast', description: null, displayOrder: 2, requiresDescription: false },
    { id: 'method-3', code: 'OTHER', name: 'Other Method', description: null, displayOrder: 3, requiresDescription: true },
  ];

  const sampleProducts: FertilizerProductLookup[] = [
    {
      inventoryItemId: 'item-1',
      itemName: 'NPK 20-20-20',
      sku: 'FERT-001',
      stockUnitId: 'u-kg',
      stockUnitName: 'Kilograms',
      stockUnitCode: 'KG',
      stockUnitSymbol: 'kg',
      categoryId: 'cat-1',
      categoryName: 'Fertilizers',
    },
    {
      inventoryItemId: 'item-2',
      itemName: 'Urea 46%',
      sku: 'FERT-002',
      stockUnitId: 'u-kg',
      stockUnitName: 'Kilograms',
      stockUnitCode: 'KG',
      stockUnitSymbol: 'kg',
      categoryId: 'cat-1',
      categoryName: 'Fertilizers',
    },
  ];

  const sampleLocations: FertilizerStorageLocationLookup[] = [
    {
      storageLocationId: 'loc-1',
      storageLocationName: 'Main Barn',
      currentStock: 500,
      hasStock: true,
      stockUnitId: 'u-kg',
      stockUnitName: 'Kilograms',
      stockUnitSymbol: 'kg',
    },
  ];

  beforeEach(async () => {
    mockFertilizerService = jasmine.createSpyObj('FertilizerService', [
      'getApplicationMethodsLookup',
      'getProductsLookup',
      'getStorageLocationsLookup',
      'getIrrigationsLookup',
      'getCropCycleStages',
      'recordCompleted',
    ]);
    mockFarmService = jasmine.createSpyObj('FarmManagementService', [
      'listFarms',
      'listFarmAreas',
      'listPlantations',
      'listCycles',
      'listUnits',
    ]);
    mockSnackBar = jasmine.createSpyObj('MatSnackBar', ['open']);
    mockDialog = jasmine.createSpyObj('MatDialog', ['open']);

    mockFarmService.listFarms.and.returnValue(
      of({ items: sampleFarms, totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
    );
    mockFarmService.listFarmAreas.and.returnValue(
      of({ items: sampleAreas, totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
    );
    mockFarmService.listPlantations.and.returnValue(
      of({ items: samplePlantations, totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
    );
    mockFarmService.listCycles.and.returnValue(
      of({ items: [], totalCount: 0, page: 1, pageSize: 100, totalPages: 0 }),
    );
    mockFarmService.listUnits.and.returnValue(of([]));

    mockFertilizerService.getApplicationMethodsLookup.and.returnValue(of(sampleMethods));
    mockFertilizerService.getProductsLookup.and.returnValue(of(sampleProducts));
    mockFertilizerService.getStorageLocationsLookup.and.returnValue(of(sampleLocations));
    mockFertilizerService.getIrrigationsLookup.and.returnValue(of([]));
    mockFertilizerService.getCropCycleStages.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [FertilizerRecordCompletedPageComponent, NoopAnimationsModule],
      providers: [
        provideNativeDateAdapter(),
        provideRouter([]),
        { provide: FertilizerService, useValue: mockFertilizerService },
        { provide: FarmManagementService, useValue: mockFarmService },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: MatDialog, useValue: mockDialog },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    spyOn(router, 'navigate');

    fixture = TestBed.createComponent(FertilizerRecordCompletedPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should initialize with 1 product line and default today date', () => {
    expect(component.productsArray.length).toBe(1);
    expect(component.form.value.actualApplicationDate).toBeDefined();
    expect(component.isRetrospective()).toBeFalse();
  });

  it('should invalidate future dates', () => {
    const future = new Date();
    future.setDate(future.getDate() + 2);
    component.form.patchValue({ actualApplicationDate: future });
    expect(component.form.get('actualApplicationDate')?.hasError('futureDate')).toBeTrue();
  });

  it('should trigger retrospective state and require checkbox when date is in the past', () => {
    const past = new Date('2026-01-15');
    component.form.patchValue({ actualApplicationDate: past });
    expect(component.isRetrospective()).toBeTrue();

    // Fill valid required fields
    component.form.patchValue({
      farmId: 'farm-1',
      farmAreaId: 'area-1',
      applicationMethodId: 'method-2',
      acknowledgeRetrospectiveStockDeduction: false,
    });
    component.productsArray.at(0).patchValue({
      inventoryItemId: 'item-1',
      storageLocationId: 'loc-1',
      actualQuantity: 50,
    });

    component.onSaveDirectCompleted();
    expect(mockDialog.open).not.toHaveBeenCalled();
    expect(component.errorMessage()).toContain('retrospective');

    // Acknowledge retrospective
    component.form.patchValue({ acknowledgeRetrospectiveStockDeduction: true });
    mockDialog.open.and.returnValue({
      afterClosed: () => of(true),
    } as any);
    mockFertilizerService.recordCompleted.and.returnValue(
      of({ id: 'app-new', referenceNumber: 'FA-202601-000001' } as any),
    );

    component.onSaveDirectCompleted();
    expect(mockDialog.open).toHaveBeenCalledWith(ConfirmDialogComponent, jasmine.any(Object));
    expect(mockFertilizerService.recordCompleted).toHaveBeenCalledWith(
      jasmine.objectContaining({
        farmId: 'farm-1',
        actualApplicationDate: '2026-01-15',
        acknowledgeRetrospectiveStockDeduction: true,
      }),
    );
    expect(router.navigate).toHaveBeenCalledWith(['/fertilizer-applications']);
  });

  it('should open exactly one confirmation dialog and submit on confirmation', () => {
    component.form.patchValue({
      farmId: 'farm-1',
      farmAreaId: 'area-1',
      applicationMethodId: 'method-2',
      actualApplicationDate: new Date(),
    });
    component.productsArray.at(0).patchValue({
      inventoryItemId: 'item-1',
      storageLocationId: 'loc-1',
      actualQuantity: 75,
    });

    mockDialog.open.and.returnValue({
      afterClosed: () => of(true),
    } as any);
    mockFertilizerService.recordCompleted.and.returnValue(
      of({ id: 'app-new', referenceNumber: 'FA-202610-000002' } as any),
    );

    component.onSaveDirectCompleted();

    expect(mockDialog.open).toHaveBeenCalledTimes(1);
    expect(mockFertilizerService.recordCompleted).toHaveBeenCalledTimes(1);
    expect(router.navigate).toHaveBeenCalledWith(['/fertilizer-applications']);
  });

  it('should detect duplicate products and prevent submission', () => {
    component.form.patchValue({
      farmId: 'farm-1',
      farmAreaId: 'area-1',
      applicationMethodId: 'method-2',
    });
    component.addProduct();
    component.productsArray.at(0).patchValue({ inventoryItemId: 'item-1', storageLocationId: 'loc-1', actualQuantity: 20 });
    component.productsArray.at(1).patchValue({ inventoryItemId: 'item-1', storageLocationId: 'loc-1', actualQuantity: 30 });

    expect(component.hasDuplicateProducts()).toBeTrue();
    component.onSaveDirectCompleted();

    expect(mockDialog.open).not.toHaveBeenCalled();
    expect(component.errorMessage()).toContain('Duplicate');
  });

  it('should surface shortage errors and stay on page on submission failure', () => {
    component.form.patchValue({
      farmId: 'farm-1',
      farmAreaId: 'area-1',
      applicationMethodId: 'method-2',
      actualApplicationDate: new Date(),
    });
    component.productsArray.at(0).patchValue({
      inventoryItemId: 'item-1',
      storageLocationId: 'loc-1',
      actualQuantity: 100,
    });

    mockDialog.open.and.returnValue({
      afterClosed: () => of(true),
    } as any);

    const shortageError = {
      status: 400,
      error: {
        message: 'Insufficient stock',
        errors: {
          'products[0]': ["Insufficient stock for 'NPK 20-20-20' at 'Main Barn'."],
        },
      },
    };
    mockFertilizerService.recordCompleted.and.returnValue(throwError(() => shortageError));

    component.onSaveDirectCompleted();

    expect(component.shortages().length).toBe(1);
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
