import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { PermissionService } from '../../../core/auth/permission.service';
import { CropCycleStage } from '../../../core/farm-management/farm-management.models';
import { FarmManagementService } from '../../../core/farm-management/farm-management.service';
import { PagedResponse } from '../../../core/models/paged-response.model';
import {
  FertilizerApplicationListItem,
  FertilizerApplicationMethodLookup,
  FertilizerProductLookup,
} from '../../../core/fertilizers/fertilizer.models';
import { FertilizerService } from '../../../core/fertilizers/fertilizer.service';
import { FertilizerListPageComponent } from './fertilizer-list-page.component';

describe('FertilizerListPageComponent', () => {
  let component: FertilizerListPageComponent;
  let fixture: ComponentFixture<FertilizerListPageComponent>;

  let mockFertilizerService: jasmine.SpyObj<FertilizerService>;
  let mockFarmService: jasmine.SpyObj<FarmManagementService>;
  let mockPermissionService: jasmine.SpyObj<PermissionService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;
  let router: Router;

  const sampleFertilizerItem: FertilizerApplicationListItem = {
    id: 'f1111111-1111-1111-1111-111111111111',
    referenceNumber: 'FA-202610-001',
    status: 'Scheduled',
    statusName: 'Scheduled',
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
    irrigationEventId: null,
    plannedDate: '2026-10-25',
    scheduledDate: '2026-10-25T08:00:00Z',
    actualApplicationDate: null,
    plannedArea: 2.5,
    plannedAreaUnitId: 'u-ha',
    plannedAreaUnitName: 'Hectares',
    actualArea: null,
    actualAreaUnitId: null,
    actualAreaUnitName: null,
    purposeReason: 'Regular fertigation',
    productCount: 1,
    productsSummary: [
      {
        inventoryItemId: 'item-1',
        itemName: 'NPK 20-20-20',
        itemSku: 'FERT-001',
        plannedQuantity: 50,
        actualQuantity: null,
        unitSymbol: 'kg',
      },
    ],
    createdAt: '2026-10-20T10:00:00Z',
    createdBy: 'user-1',
  };

  const sampleListResponse: PagedResponse<FertilizerApplicationListItem> = {
    items: [sampleFertilizerItem],
    totalCount: 1,
    page: 1,
    pageSize: 20,
    totalPages: 1,
  };

  const sampleMethods: FertilizerApplicationMethodLookup[] = [
    {
      id: 'method-1',
      code: 'FERTIGATION',
      name: 'Fertigation',
      description: 'Application via drip irrigation',
      displayOrder: 1,
      requiresDescription: false,
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
  ];

  const sampleStages: CropCycleStage[] = [
    {
      id: 'stage-1',
      cropCycleId: 'cycle-1',
      stageName: 'Flowering',
      order: 1,
      plannedDurationDays: 14,
      startDate: null,
      endDate: null,
      status: 'In Progress',
    } as any,
  ];

  beforeEach(async () => {
    mockFertilizerService = jasmine.createSpyObj('FertilizerService', [
      'list',
      'exportCsv',
      'getApplicationMethodsLookup',
      'getProductsLookup',
      'getCropCycleStages',
      'cancel',
      'deleteDraft',
      'copyAsNewDraft',
    ]);
    mockFarmService = jasmine.createSpyObj('FarmManagementService', [
      'listFarms',
      'listFarmAreas',
      'listPlantations',
      'listCycles',
    ]);
    mockPermissionService = jasmine.createSpyObj('PermissionService', ['has', 'hasAny']);
    mockSnackBar = jasmine.createSpyObj('MatSnackBar', ['open']);

    mockPermissionService.has.and.returnValue(true);
    mockPermissionService.hasAny.and.returnValue(true);

    mockFarmService.listFarms.and.returnValue(
      of({ items: [{ id: 'farm-1', name: 'Green Valley Farm' } as any], totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
    );
    mockFarmService.listFarmAreas.and.returnValue(
      of({ items: [{ id: 'area-1', name: 'North Sector' } as any], totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
    );
    mockFarmService.listPlantations.and.returnValue(
      of({ items: [{ id: 'plant-1', name: 'Block A' } as any], totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
    );
    mockFarmService.listCycles.and.returnValue(
      of({ items: [{ id: 'cycle-1', name: '2026 Season' } as any], totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
    );

    mockFertilizerService.getApplicationMethodsLookup.and.returnValue(of(sampleMethods));
    mockFertilizerService.getProductsLookup.and.returnValue(of(sampleProducts));
    mockFertilizerService.getCropCycleStages.and.returnValue(of(sampleStages));
    mockFertilizerService.list.and.returnValue(of(sampleListResponse));

    await TestBed.configureTestingModule({
      imports: [FertilizerListPageComponent, NoopAnimationsModule],
      providers: [
        provideNativeDateAdapter(),
        provideRouter([]),
        { provide: FertilizerService, useValue: mockFertilizerService },
        { provide: FarmManagementService, useValue: mockFarmService },
        { provide: PermissionService, useValue: mockPermissionService },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    spyOn(router, 'navigate');

    fixture = TestBed.createComponent(FertilizerListPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create component and load initial lookups and applications', () => {
    expect(component).toBeTruthy();
    expect(mockFertilizerService.getProductsLookup).toHaveBeenCalled();
    expect(mockFarmService.listFarms).toHaveBeenCalled();
    expect(mockFertilizerService.list).toHaveBeenCalled();
    expect(component.applications().length).toBe(1);
    expect(component.selectedTab()).toBe('all');
  });

  it('should switch tabs and preserve existing filters while setting appropriate status', () => {
    // Set a farm filter first
    component.onFarmChange('farm-1');
    expect(component.farmId()).toBe('farm-1');

    mockFertilizerService.list.calls.reset();

    // Switch to Scheduled tab
    component.selectTab('scheduled');
    expect(component.selectedTab()).toBe('scheduled');
    expect(mockFertilizerService.list).toHaveBeenCalledWith(
      jasmine.objectContaining({
        status: 'Scheduled',
        farmId: 'farm-1',
        pageNumber: 1,
      }),
    );

    mockFertilizerService.list.calls.reset();

    // Switch back to All tab
    component.selectTab('all');
    expect(component.selectedTab()).toBe('all');
    expect(mockFertilizerService.list).toHaveBeenCalledWith(
      jasmine.objectContaining({
        status: undefined,
        farmId: 'farm-1',
        pageNumber: 1,
      }),
    );
  });

  it('should cascade farm area, plantation, and cycles when farm is changed', () => {
    component.onFarmChange('farm-1');
    expect(mockFarmService.listFarmAreas).toHaveBeenCalledWith(1, 100, 'farm-1');
    expect(mockFarmService.listPlantations).toHaveBeenCalledWith(1, 100, 'farm-1');

    component.onAreaChange('area-1');
    expect(component.farmAreaId()).toBe('area-1');

    component.onPlantationChange('plant-1');
    expect(mockFarmService.listCycles).toHaveBeenCalledWith(1, 100, undefined, undefined, 'plant-1');

    component.onCropCycleChange('cycle-1');
    expect(mockFertilizerService.getCropCycleStages).toHaveBeenCalledWith('cycle-1');

    component.onCropCycleStageChange('stage-1');
    expect(component.cropCycleStageId()).toBe('stage-1');
  });

  it('should reset downstream selections when farm is cleared', () => {
    component.onFarmChange('farm-1');
    component.onAreaChange('area-1');
    component.onPlantationChange('plant-1');
    component.onCropCycleChange('cycle-1');
    component.onCropCycleStageChange('stage-1');

    component.onFarmChange('');

    expect(component.farmAreaId()).toBe('');
    expect(component.plantationId()).toBe('');
    expect(component.cropCycleId()).toBe('');
    expect(component.cropCycleStageId()).toBe('');
    expect(component.areas().length).toBe(0);
  });

  it('should re-query when search input changes with debounce', fakeAsync(() => {
    mockFertilizerService.list.calls.reset();

    component.onSearchInput('NPK');
    tick(100);
    expect(mockFertilizerService.list).not.toHaveBeenCalled();

    tick(250);
    expect(mockFertilizerService.list).toHaveBeenCalledWith(
      jasmine.objectContaining({
        search: 'NPK',
      }),
    );
  }));

  it('should handle date range changes with datepicker', () => {
    mockFertilizerService.list.calls.reset();

    const from = new Date(2026, 9, 1);
    const to = new Date(2026, 9, 31);

    component.onFromDateChange(from);
    expect(component.fromDate()).toEqual(from);

    component.onToDateChange(to);
    expect(component.toDate()).toEqual(to);

    component.onDateTypeChange('PlannedDate');
    expect(component.dateType()).toBe('PlannedDate');

    expect(mockFertilizerService.list).toHaveBeenCalledWith(
      jasmine.objectContaining({
        dateType: 'PlannedDate',
        fromDate: '2026-10-01',
        toDate: '2026-10-31',
      }),
    );

    component.onFromDateChange(null);
    component.onToDateChange(null);
    expect(component.fromDate()).toBeNull();
    expect(component.toDate()).toBeNull();
  });

  it('should export CSV using current filters and download blob', () => {
    const blob = new Blob(['reference,status\nFA-001,Scheduled'], { type: 'text/csv' });
    mockFertilizerService.exportCsv.and.returnValue(of(blob));

    spyOn(window.URL, 'createObjectURL').and.returnValue('blob:http://localhost/test-blob');
    spyOn(window.URL, 'revokeObjectURL');

    component.exportCsv();

    expect(mockFertilizerService.exportCsv).toHaveBeenCalled();
    expect(component.isExporting()).toBeFalse();
    expect(mockSnackBar.open).toHaveBeenCalledWith(
      'Fertilizer applications exported successfully.',
      'Dismiss',
      jasmine.any(Object),
    );
  });

  it('should show error snackbar when CSV export fails', () => {
    mockFertilizerService.exportCsv.and.returnValue(throwError(() => new Error('Network error')));

    component.exportCsv();

    expect(component.isExporting()).toBeFalse();
    expect(mockSnackBar.open).toHaveBeenCalledWith(
      'Network error',
      'Dismiss',
      jasmine.any(Object),
    );
  });

  it('should copy as draft on confirm and show snackbar action', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    const snackRefSpy = jasmine.createSpyObj('MatSnackBarRef', ['onAction']);
    snackRefSpy.onAction.and.returnValue(of(void 0));
    mockSnackBar.open.and.returnValue(snackRefSpy);

    mockFertilizerService.copyAsNewDraft.and.returnValue(
      of({ id: 'app-copied', referenceNumber: 'FA-202610-002' } as any),
    );

    component.copyAsNewDraft(sampleFertilizerItem);

    expect(mockFertilizerService.copyAsNewDraft).toHaveBeenCalledWith(sampleFertilizerItem.id);
    expect(mockSnackBar.open).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/fertilizer-applications', 'app-copied', 'edit']);
  });

  it('should delete draft on confirm and reload list', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    const draftItem: FertilizerApplicationListItem = { ...sampleFertilizerItem, status: 'Draft' };
    mockFertilizerService.deleteDraft.and.returnValue(of(void 0));
    mockFertilizerService.list.calls.reset();

    component.deleteDraft(draftItem);

    expect(mockFertilizerService.deleteDraft).toHaveBeenCalledWith(draftItem.id);
    expect(mockSnackBar.open).toHaveBeenCalledWith(
      `Draft '${draftItem.referenceNumber}' deleted successfully.`,
      'Dismiss',
      jasmine.any(Object),
    );
    expect(mockFertilizerService.list).toHaveBeenCalled();
  });

  it('should format tooltip correctly without summing unlike units', () => {
    const tooltip = component.getProductsTooltip(sampleFertilizerItem);
    expect(tooltip).toBe('NPK 20-20-20: 50 kg');

    const emptyApp: FertilizerApplicationListItem = { ...sampleFertilizerItem, productsSummary: [] };
    expect(component.getProductsTooltip(emptyApp)).toBe('No products configured');
  });

  it('should return correct badge and display name for statuses', () => {
    expect(component.getStatusBadgeClass('Draft')).toBe('status-badge--draft');
    expect(component.getStatusDisplayName('Draft')).toBe('Draft');
    expect(component.getStatusDisplayName('InProgress')).toBe('In Progress');
  });

  it('should reset all filters and reload list on resetFilters', () => {
    component.onSearchInput('custom query');
    component.onFarmChange('farm-1');
    mockFertilizerService.list.calls.reset();

    component.resetFilters();

    expect(component.search()).toBe('');
    expect(component.farmId()).toBe('');
    expect(component.dateType()).toBe('CreatedDate');
    expect(mockFertilizerService.list).toHaveBeenCalled();
  });
});
