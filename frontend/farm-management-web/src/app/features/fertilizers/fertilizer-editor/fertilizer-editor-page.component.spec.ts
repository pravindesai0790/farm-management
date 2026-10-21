import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { PermissionService } from '../../../core/auth/permission.service';
import {
  CropCycle,
  CropCycleStage,
  Farm,
  FarmArea,
  Plantation,
  Unit,
} from '../../../core/farm-management/farm-management.models';
import { FarmManagementService } from '../../../core/farm-management/farm-management.service';
import {
  FertilizerApplicationDetailsResponse,
  FertilizerApplicationMethodLookup,
  FertilizerIrrigationLookup,
  FertilizerProductLookup,
  FertilizerStorageLocationLookup,
} from '../../../core/fertilizers/fertilizer.models';
import { FertilizerService } from '../../../core/fertilizers/fertilizer.service';
import { FertilizerEditorPageComponent } from './fertilizer-editor-page.component';

describe('FertilizerEditorPageComponent', () => {
  let component: FertilizerEditorPageComponent;
  let fixture: ComponentFixture<FertilizerEditorPageComponent>;

  let mockFertilizerService: jasmine.SpyObj<FertilizerService>;
  let mockFarmService: jasmine.SpyObj<FarmManagementService>;
  let mockPermissionService: jasmine.SpyObj<PermissionService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;
  let mockDialog: jasmine.SpyObj<MatDialog>;
  let router: Router;

  const sampleFarms: Farm[] = [
    { id: 'farm-1', name: 'Green Valley Farm', totalArea: 100 } as any,
  ];

  const sampleAreas: FarmArea[] = [
    { id: 'area-1', name: 'North Sector', farmId: 'farm-1' } as any,
  ];

  const samplePlantations: Plantation[] = [
    { id: 'plant-1', name: 'Block A', farmAreaId: 'area-1' } as any,
  ];

  const sampleCycles: CropCycle[] = [
    { id: 'cycle-1', cycleName: '2026 Season', plantationId: 'plant-1' } as any,
  ];

  const sampleStages: CropCycleStage[] = [
    { id: 'stage-1', stageName: 'Flowering', cropCycleId: 'cycle-1' } as any,
  ];

  const sampleMethods: FertilizerApplicationMethodLookup[] = [
    {
      id: 'method-1',
      code: 'FERTIGATION',
      name: 'Fertigation',
      description: 'Drip fertigation',
      displayOrder: 1,
      requiresDescription: false,
    },
    {
      id: 'method-2',
      code: 'BROADCAST',
      name: 'Broadcast',
      description: 'Surface spreading',
      displayOrder: 2,
      requiresDescription: false,
    },
    {
      id: 'method-3',
      code: 'OTHER',
      name: 'Other',
      description: 'Special method',
      displayOrder: 3,
      requiresDescription: true,
    },
  ];

  const sampleProducts: FertilizerProductLookup[] = [
    {
      inventoryItemId: 'item-1',
      itemName: 'NPK 20-20-20',
      sku: 'FERT-001',
      categoryName: 'Fertilizer',
      stockUnitId: 'u-kg',
      stockUnitName: 'Kilograms',
      stockUnitCode: 'KG',
      stockUnitSymbol: 'kg',
      categoryId: 'cat-1',
    },
    {
      inventoryItemId: 'item-2',
      itemName: 'Urea 46-0-0',
      sku: 'FERT-002',
      categoryName: 'Fertilizer',
      stockUnitId: 'u-kg',
      stockUnitName: 'Kilograms',
      stockUnitCode: 'KG',
      stockUnitSymbol: 'kg',
      categoryId: 'cat-1',
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

  const sampleIrrigations: FertilizerIrrigationLookup[] = [
    {
      id: 'irr-1',
      farmId: 'farm-1',
      farmAreaId: 'area-1',
      farmAreaName: 'North Sector',
      cropCycleId: 'cycle-1',
      cropCycleName: '2026 Season',
      status: 'Scheduled',
      plannedAt: '2026-10-25T08:00:00Z',
      scheduledAt: '2026-10-25T08:00:00Z',
      actualStartedAt: null,
      actualEndedAt: null,
      irrigationMethodName: 'Drip',
      actualWaterQuantity: 5000,
      waterUnitSymbol: 'L',
    },
  ];

  const sampleAreaUnits: Unit[] = [
    { id: 'u-ha', name: 'Hectares', symbol: 'ha', category: 'Area' } as any,
  ];

  const sampleAppDetails: FertilizerApplicationDetailsResponse = {
    id: 'app-1',
    organizationId: 'org-1',
    referenceNumber: 'FA-202610-000001',
    status: 'Draft',
    statusName: 'Draft',
    farmId: 'farm-1',
    farmName: 'Green Valley Farm',
    farmAreaId: 'area-1',
    farmAreaName: 'North Sector',
    plantationId: 'plant-1',
    plantationName: 'Block A',
    cropCycleId: 'cycle-1',
    cropCycleName: '2026 Season',
    cropCycleStageId: 'stage-1',
    cropCycleStageName: 'Flowering',
    applicationMethodId: 'method-1',
    applicationMethodName: 'Fertigation',
    otherMethodDescription: null,
    irrigationEventId: 'irr-1',
    irrigationEventReference: 'IRR-001',
    plannedDate: '2026-10-28',
    scheduledDate: null,
    actualApplicationDate: null,
    plannedArea: 5.0,
    plannedAreaUnitId: 'u-ha',
    plannedAreaUnitName: 'Hectares',
    actualArea: null,
    actualAreaUnitId: null,
    actualAreaUnitName: null,
    purposeReason: 'Pre-flowering nutrient feed',
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
    startedAt: null,
    startedBy: null,
    completedAt: null,
    completedBy: null,
    cancelledAt: null,
    cancelledBy: null,
    version: 1,
  };

  const setupTestBed = async (
    routeParams: Record<string, string> = {},
    queryParams: Record<string, string> = {},
    details: FertilizerApplicationDetailsResponse = sampleAppDetails,
  ) => {
    mockFertilizerService = jasmine.createSpyObj('FertilizerService', [
      'get',
      'createDraft',
      'updateDraft',
      'deleteDraft',
      'copyAsNewDraft',
      'plan',
      'schedule',
      'reschedule',
      'getApplicationMethodsLookup',
      'getProductsLookup',
      'getStorageLocationsLookup',
      'getIrrigationsLookup',
      'getCropCycleStages',
    ]);
    mockFarmService = jasmine.createSpyObj('FarmManagementService', [
      'listFarms',
      'listFarmAreas',
      'listPlantations',
      'listCycles',
      'listUnits',
    ]);
    mockPermissionService = jasmine.createSpyObj('PermissionService', ['has', 'hasAny']);
    mockSnackBar = jasmine.createSpyObj('MatSnackBar', ['open']);
    mockDialog = jasmine.createSpyObj('MatDialog', ['open']);

    mockPermissionService.has.and.returnValue(true);
    mockPermissionService.hasAny.and.returnValue(true);

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
      of({ items: sampleCycles, totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
    );
    mockFarmService.listUnits.and.returnValue(of(sampleAreaUnits));

    mockFertilizerService.getApplicationMethodsLookup.and.returnValue(of(sampleMethods));
    mockFertilizerService.getProductsLookup.and.returnValue(of(sampleProducts));
    mockFertilizerService.getStorageLocationsLookup.and.returnValue(of(sampleLocations));
    mockFertilizerService.getIrrigationsLookup.and.returnValue(of(sampleIrrigations));
    mockFertilizerService.getCropCycleStages.and.returnValue(of(sampleStages));
    mockFertilizerService.get.and.returnValue(of(details));

    await TestBed.configureTestingModule({
      imports: [FertilizerEditorPageComponent, NoopAnimationsModule],
      providers: [
        provideNativeDateAdapter(),
        provideRouter([]),
        { provide: FertilizerService, useValue: mockFertilizerService },
        { provide: FarmManagementService, useValue: mockFarmService },
        { provide: PermissionService, useValue: mockPermissionService },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: MatDialog, useValue: mockDialog },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (key: string) => routeParams[key] || null,
              },
              queryParamMap: {
                get: (key: string) => queryParams[key] || null,
              },
            },
          },
        },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    spyOn(router, 'navigate');

    fixture = TestBed.createComponent(FertilizerEditorPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  };

  describe('New Mode (/fertilizer-applications/new)', () => {
    beforeEach(async () => {
      await setupTestBed();
    });

    it('should create in new mode and load initial master lookups', () => {
      expect(component).toBeTruthy();
      expect(component.isEditMode()).toBeFalse();
      expect(mockFarmService.listFarms).toHaveBeenCalled();
      expect(mockFertilizerService.getApplicationMethodsLookup).toHaveBeenCalled();
      expect(mockFertilizerService.getProductsLookup).toHaveBeenCalled();
      expect(mockFarmService.listUnits).toHaveBeenCalledWith('Area');
      expect(component.currentStatus()).toBe('Draft');
    });

    it('should save an empty draft without requiring farm, area, or products', () => {
      mockFertilizerService.createDraft.and.returnValue(
        of({ id: 'app-created-1', referenceNumber: 'FA-202610-000002', status: 'Draft' } as any),
      );

      component.onSaveDraft();

      expect(mockFertilizerService.createDraft).toHaveBeenCalledWith(
        jasmine.objectContaining({
          farmId: null,
          farmAreaId: null,
          products: null,
        }),
      );
      expect(mockSnackBar.open).toHaveBeenCalledWith(
        jasmine.stringMatching(/created/),
        'Dismiss',
        jasmine.any(Object),
      );
      expect(router.navigate).toHaveBeenCalledWith(['/fertilizer-applications', 'app-created-1', 'edit']);
    });

    it('should cascade areas, plantations, and storage locations when farm changes', () => {
      component.form.patchValue({ farmId: 'farm-1' });
      component.onFarmChange('farm-1');

      expect(mockFarmService.listFarmAreas).toHaveBeenCalledWith(1, 100, 'farm-1');
      expect(mockFarmService.listPlantations).toHaveBeenCalledWith(1, 100, 'farm-1', undefined);
      expect(mockFertilizerService.getStorageLocationsLookup).toHaveBeenCalledWith('farm-1');
    });

    it('should reset downstream fields when farm is cleared', () => {
      component.form.patchValue({
        farmId: 'farm-1',
        farmAreaId: 'area-1',
        plantationId: 'plant-1',
        cropCycleId: 'cycle-1',
        cropCycleStageId: 'stage-1',
      });

      component.onFarmChange('');

      expect(component.form.value.farmAreaId).toBe('');
      expect(component.form.value.plantationId).toBe('');
      expect(component.form.value.cropCycleId).toBe('');
      expect(component.form.value.cropCycleStageId).toBe('');
      expect(component.areas().length).toBe(0);
    });

    it('should toggle other method description when Other method is selected', () => {
      component.form.patchValue({ applicationMethodId: 'method-3' }); // OTHER
      expect(component.requiresOtherDescription()).toBeTrue();

      component.form.patchValue({ applicationMethodId: 'method-2' }); // BROADCAST
      component.onMethodChange();
      expect(component.requiresOtherDescription()).toBeFalse();
      expect(component.form.value.otherMethodDescription).toBe('');
    });

    it('should load irrigations when Fertigation is selected and clear them when switched', () => {
      component.form.patchValue({ farmId: 'farm-1', applicationMethodId: 'method-1' }); // FERTIGATION
      component.onMethodChange();

      expect(component.isFertigation()).toBeTrue();
      expect(mockFertilizerService.getIrrigationsLookup).toHaveBeenCalledWith('farm-1', undefined);

      // Switch away to broadcast
      component.form.patchValue({ applicationMethodId: 'method-2', irrigationEventId: 'irr-1' });
      component.onMethodChange();

      expect(component.isFertigation()).toBeFalse();
      expect(component.form.value.irrigationEventId).toBe('');
      expect(component.irrigations().length).toBe(0);
    });

    it('should add, remove, and validate duplicate products', () => {
      expect(component.productsArray.length).toBe(0);

      component.addProduct({ inventoryItemId: 'item-1', plannedQuantity: 50 });
      expect(component.productsArray.length).toBe(1);

      // Add duplicate
      component.addProduct({ inventoryItemId: 'item-1', plannedQuantity: 20 });
      expect(component.productsArray.length).toBe(2);
      expect(component.hasDuplicateProducts()).toBeTrue();

      // Remove duplicate
      component.removeProduct(1);
      expect(component.productsArray.length).toBe(1);
      expect(component.hasDuplicateProducts()).toBeFalse();
    });

    it('should block planning when requirements are missing', () => {
      component.onPlan();

      expect(component.validationErrors().length).toBeGreaterThan(0);
      expect(mockFertilizerService.plan).not.toHaveBeenCalled();
    });

    it('should save draft and call plan when all planning invariants are satisfied', () => {
      const futureDate = new Date();
      futureDate.setDate(futureDate.getDate() + 5);

      component.form.patchValue({
        farmId: 'farm-1',
        farmAreaId: 'area-1',
        applicationMethodId: 'method-2',
        plannedDate: futureDate,
      });

      component.addProduct({
        inventoryItemId: 'item-1',
        plannedQuantity: 50,
        storageLocationId: 'loc-1',
      });

      mockFertilizerService.createDraft.and.returnValue(
        of({ id: 'app-created-2', referenceNumber: 'FA-202610-000003', version: 1 } as any),
      );
      mockFertilizerService.plan.and.returnValue(
        of({ id: 'app-created-2', referenceNumber: 'FA-202610-000003', status: 'Planned', version: 2 } as any),
      );

      component.onPlan();

      expect(mockFertilizerService.createDraft).toHaveBeenCalled();
      expect(mockFertilizerService.plan).toHaveBeenCalledWith('app-created-2', jasmine.objectContaining({ version: 1 }));
    });
  });

  describe('Edit Mode (/fertilizer-applications/:id/edit)', () => {
    beforeEach(async () => {
      await setupTestBed({ id: 'app-1' });
    });

    it('should load and populate existing application in edit mode', () => {
      expect(component.isEditMode()).toBeTrue();
      expect(mockFertilizerService.get).toHaveBeenCalledWith('app-1');
      expect(component.form.value.farmId).toBe('farm-1');
      expect(component.form.value.farmAreaId).toBe('area-1');
      expect(component.productsArray.length).toBe(1);
      expect(component.applicationDetails()?.referenceNumber).toBe('FA-202610-000001');
    });

    it('should update draft on onSaveDraft', () => {
      mockFertilizerService.updateDraft.and.returnValue(
        of({ ...sampleAppDetails, version: 2 }),
      );

      component.onSaveDraft();

      expect(mockFertilizerService.updateDraft).toHaveBeenCalledWith(
        'app-1',
        jasmine.objectContaining({
          farmId: 'farm-1',
          version: 1,
        }),
      );
    });

    it('should handle 409 concurrency conflict by setting isStaleConflict', () => {
      mockFertilizerService.updateDraft.and.returnValue(
        throwError(() => ({ status: 409, message: 'Conflict' })),
      );

      component.onSaveDraft();

      expect(component.isStaleConflict()).toBeTrue();
      expect(component.errorMessage()).toContain('updated by another user');
    });

    it('should reload application details on reloadAfterConflict', () => {
      mockFertilizerService.get.calls.reset();
      component.isStaleConflict.set(true);

      component.reloadAfterConflict();

      expect(component.isStaleConflict()).toBeFalse();
      expect(mockFertilizerService.get).toHaveBeenCalledWith('app-1');
    });

    it('should trigger delete draft with confirmation dialog', () => {
      spyOn(window, 'confirm').and.returnValue(true);
      mockFertilizerService.deleteDraft.and.returnValue(of(void 0));

      component.onDeleteDraft();

      expect(mockFertilizerService.deleteDraft).toHaveBeenCalledWith('app-1');
      expect(router.navigate).toHaveBeenCalledWith(['/fertilizer-applications']);
    });

    it('should trigger copy as new draft on onCopyAsDraft', () => {
      spyOn(window, 'confirm').and.returnValue(true);
      mockFertilizerService.copyAsNewDraft.and.returnValue(
        of({ id: 'app-copied-1', referenceNumber: 'FA-202610-000002' } as any),
      );

      component.onCopyAsDraft();

      expect(mockFertilizerService.copyAsNewDraft).toHaveBeenCalledWith('app-1');
      expect(router.navigate).toHaveBeenCalledWith(['/fertilizer-applications', 'app-copied-1', 'edit']);
    });

    it('should open StartFertilizerDialogComponent on onStart', () => {
      mockDialog.open.and.returnValue({
        afterClosed: () => of(true),
      } as any);

      component.onStart();

      expect(mockDialog.open).toHaveBeenCalledWith(
        jasmine.any(Function),
        jasmine.objectContaining({
          data: jasmine.objectContaining({ application: jasmine.any(Object) }),
        }),
      );
      expect(mockFertilizerService.get).toHaveBeenCalledWith('app-1');
    });

    it('should open CompleteFertilizerDialogComponent on onComplete', () => {
      mockDialog.open.and.returnValue({
        afterClosed: () => of(true),
      } as any);

      component.onComplete();

      expect(mockDialog.open).toHaveBeenCalledWith(
        jasmine.any(Function),
        jasmine.objectContaining({
          data: jasmine.objectContaining({ application: jasmine.any(Object) }),
        }),
      );
      expect(mockFertilizerService.get).toHaveBeenCalledWith('app-1');
    });

    it('should open CancelFertilizerDialogComponent on onCancelApplication', () => {
      mockDialog.open.and.returnValue({
        afterClosed: () => of(true),
      } as any);

      component.onCancelApplication();

      expect(mockDialog.open).toHaveBeenCalledWith(
        jasmine.any(Function),
        jasmine.objectContaining({
          data: jasmine.objectContaining({ application: jasmine.any(Object) }),
        }),
      );
      expect(mockFertilizerService.get).toHaveBeenCalledWith('app-1');
    });
  });

  describe('Template Copy Query Param (/fertilizer-applications/new?copyFrom=app-1)', () => {
    beforeEach(async () => {
      await setupTestBed({}, { copyFrom: 'app-1' });
    });

    it('should load template application and populate form without planned dates or references', () => {
      expect(mockFertilizerService.get).toHaveBeenCalledWith('app-1');
      expect(component.form.value.farmId).toBe('farm-1');
      expect(component.form.value.plannedDate).toBeNull();
      expect(component.isEditMode()).toBeFalse();
    });
  });

  describe('Frozen State Handling', () => {
    beforeEach(async () => {
      const frozenDetails = { ...sampleAppDetails, status: 'InProgress' as const };
      await setupTestBed({ id: 'app-frozen' }, {}, frozenDetails);
    });

    it('should disable form and identify frozen state when status is InProgress', () => {
      expect(component.isFrozen()).toBeTrue();
      expect(component.form.disabled).toBeTrue();
    });
  });
});
