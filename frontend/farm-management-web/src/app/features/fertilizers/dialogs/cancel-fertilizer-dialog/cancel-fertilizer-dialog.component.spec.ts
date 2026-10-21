import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { provideNativeDateAdapter } from '@angular/material/core';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { FertilizerApplicationDetailsResponse } from '../../../../core/fertilizers/fertilizer.models';
import { FertilizerService } from '../../../../core/fertilizers/fertilizer.service';
import {
  CancelFertilizerDialogComponent,
  CancelFertilizerDialogData,
} from './cancel-fertilizer-dialog.component';

describe('CancelFertilizerDialogComponent', () => {
  let component: CancelFertilizerDialogComponent;
  let fixture: ComponentFixture<CancelFertilizerDialogComponent>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<CancelFertilizerDialogComponent>>;
  let mockFertilizerService: jasmine.SpyObj<FertilizerService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const sampleApp: FertilizerApplicationDetailsResponse = {
    id: 'app-1',
    organizationId: 'org-1',
    referenceNumber: 'FA-202610-000001',
    status: 'Planned',
    statusName: 'Planned',
    farmId: 'farm-1',
    farmName: 'Green Valley Farm',
    farmAreaId: 'area-1',
    farmAreaName: 'North Sector',
    plantationId: null,
    plantationName: null,
    cropCycleId: null,
    cropCycleName: null,
    cropCycleStageId: null,
    cropCycleStageName: null,
    applicationMethodId: 'method-1',
    applicationMethodName: 'Fertigation',
    otherMethodDescription: null,
    irrigationEventId: null,
    irrigationEventReference: null,
    plannedDate: '2026-10-25',
    scheduledDate: null,
    actualApplicationDate: null,
    plannedArea: null,
    plannedAreaUnitId: null,
    plannedAreaUnitName: null,
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
        storageLocationId: null,
        storageLocationName: null,
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

  const createComponentWithApp = async (app: FertilizerApplicationDetailsResponse) => {
    mockDialogRef = jasmine.createSpyObj('MatDialogRef', ['close']);
    mockFertilizerService = jasmine.createSpyObj('FertilizerService', [
      'cancel',
      'getStorageLocationsLookup',
    ]);
    mockSnackBar = jasmine.createSpyObj('MatSnackBar', ['open']);

    mockFertilizerService.getStorageLocationsLookup.and.returnValue(
      of([
        {
          storageLocationId: 'loc-1',
          storageLocationName: 'Main Barn',
          currentStock: 250,
          hasStock: true,
          stockUnitId: 'u-kg',
          stockUnitName: 'Kilograms',
          stockUnitSymbol: 'kg',
        },
      ]),
    );

    const dialogData: CancelFertilizerDialogData = { application: app };

    await TestBed.configureTestingModule({
      imports: [CancelFertilizerDialogComponent, NoopAnimationsModule],
      providers: [
        provideNativeDateAdapter(),
        { provide: MAT_DIALOG_DATA, useValue: dialogData },
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: FertilizerService, useValue: mockFertilizerService },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CancelFertilizerDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  };

  describe('Pre-Start Cancellation (Planned)', () => {
    beforeEach(async () => {
      await createComponentWithApp(sampleApp);
    });

    it('should require non-empty cancellationReason', () => {
      component.form.patchValue({ cancellationReason: '' });
      component.onSubmit();
      expect(mockFertilizerService.cancel).not.toHaveBeenCalled();
      expect(component.form.get('cancellationReason')?.hasError('required')).toBeTrue();
    });

    it('should allow zero-usage cancellation without date or storage locations', () => {
      component.form.patchValue({ cancellationReason: 'Weather adverse conditions' });
      expect(component.hasPositiveUsage()).toBeFalse();

      const cancelledApp = { ...sampleApp, status: 'Cancelled' as const, statusName: 'Cancelled' };
      mockFertilizerService.cancel.and.returnValue(of(cancelledApp));

      component.onSubmit();

      expect(mockFertilizerService.cancel).toHaveBeenCalledWith(
        'app-1',
        jasmine.objectContaining({
          cancellationReason: 'Weather adverse conditions',
          actualApplicationDate: undefined,
          products: [{ inventoryItemId: 'item-1', actualQuantity: 0, storageLocationId: undefined }],
        }),
      );
      expect(mockDialogRef.close).toHaveBeenCalledWith(true);
    });

    it('should require storage location and application date when positive usage is entered', () => {
      component.form.patchValue({ cancellationReason: 'Partial test application' });
      component.productsArray.at(0).patchValue({ actualQuantity: 25 });
      fixture.detectChanges();

      expect(component.hasPositiveUsage()).toBeTrue();
      // Initially storageLocationId is empty, so form is invalid
      expect(component.form.invalid).toBeTrue();

      // Set storage location and date
      component.productsArray.at(0).patchValue({ storageLocationId: 'loc-1' });
      component.form.patchValue({ actualApplicationDate: new Date() });
      expect(component.form.valid).toBeTrue();

      const cancelledApp = { ...sampleApp, status: 'Cancelled' as const, statusName: 'Cancelled' };
      mockFertilizerService.cancel.and.returnValue(of(cancelledApp));

      component.onSubmit();

      expect(mockFertilizerService.cancel).toHaveBeenCalledWith(
        'app-1',
        jasmine.objectContaining({
          cancellationReason: 'Partial test application',
          products: [{ inventoryItemId: 'item-1', actualQuantity: 25, storageLocationId: 'loc-1' }],
        }),
      );
      expect(mockDialogRef.close).toHaveBeenCalledWith(true);
    });
  });

  describe('Post-Start Cancellation (InProgress)', () => {
    beforeEach(async () => {
      const inProgressApp: FertilizerApplicationDetailsResponse = {
        ...sampleApp,
        status: 'InProgress',
        statusName: 'In Progress',
        products: [
          {
            ...sampleApp.products[0],
            storageLocationId: 'loc-1',
            storageLocationName: 'Main Barn',
          },
        ],
      };
      await createComponentWithApp(inProgressApp);
    });

    it('should use locked saved location and not require manual location selection', () => {
      expect(component.isPostStart()).toBeTrue();
      component.form.patchValue({ cancellationReason: 'Equipment failure during application' });
      component.productsArray.at(0).patchValue({ actualQuantity: 15 });
      fixture.detectChanges();

      expect(component.hasPositiveUsage()).toBeTrue();
      expect(component.form.valid).toBeTrue();

      const cancelledApp = { ...sampleApp, status: 'Cancelled' as const, statusName: 'Cancelled' };
      mockFertilizerService.cancel.and.returnValue(of(cancelledApp));

      component.onSubmit();

      expect(mockFertilizerService.cancel).toHaveBeenCalledWith(
        'app-1',
        jasmine.objectContaining({
          cancellationReason: 'Equipment failure during application',
          products: [{ inventoryItemId: 'item-1', actualQuantity: 15, storageLocationId: 'loc-1' }],
        }),
      );
    });

    it('should surface shortages when positive usage cannot be deducted', () => {
      component.form.patchValue({ cancellationReason: 'Damaged' });
      component.productsArray.at(0).patchValue({ actualQuantity: 50 });

      const shortageError = {
        status: 400,
        error: {
          message: 'Insufficient stock',
          errors: {
            'products[0]': ["Insufficient stock for 'NPK 20-20-20' at 'Main Barn'."],
          },
        },
      };
      mockFertilizerService.cancel.and.returnValue(throwError(() => shortageError));

      component.onSubmit();

      expect(component.shortages().length).toBe(1);
      expect(mockDialogRef.close).not.toHaveBeenCalled();
    });
  });
});
