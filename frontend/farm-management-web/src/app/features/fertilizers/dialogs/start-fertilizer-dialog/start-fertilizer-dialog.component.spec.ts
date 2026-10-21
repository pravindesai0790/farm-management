import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { FertilizerApplicationDetailsResponse } from '../../../../core/fertilizers/fertilizer.models';
import { FertilizerService } from '../../../../core/fertilizers/fertilizer.service';
import {
  StartFertilizerDialogComponent,
  StartFertilizerDialogData,
} from './start-fertilizer-dialog.component';

describe('StartFertilizerDialogComponent', () => {
  let component: StartFertilizerDialogComponent;
  let fixture: ComponentFixture<StartFertilizerDialogComponent>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<StartFertilizerDialogComponent>>;
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
    plannedDate: '2026-10-28',
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
    startedAt: null,
    startedBy: null,
    completedAt: null,
    completedBy: null,
    cancelledAt: null,
    cancelledBy: null,
    version: 1,
  };

  const dialogData: StartFertilizerDialogData = {
    application: sampleApp,
  };

  beforeEach(async () => {
    mockDialogRef = jasmine.createSpyObj('MatDialogRef', ['close']);
    mockFertilizerService = jasmine.createSpyObj('FertilizerService', ['start']);
    mockSnackBar = jasmine.createSpyObj('MatSnackBar', ['open']);

    await TestBed.configureTestingModule({
      imports: [StartFertilizerDialogComponent, NoopAnimationsModule],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: dialogData },
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: FertilizerService, useValue: mockFertilizerService },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(StartFertilizerDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render application reference number, farm, area, and planned product lines', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('FA-202610-000001');
    expect(compiled.textContent).toContain('Green Valley Farm');
    expect(compiled.textContent).toContain('NPK 20-20-20');
    expect(compiled.textContent).toContain('Main Barn');
    expect(compiled.textContent).toContain('100');
  });

  it('should display informational notice that no inventory is deducted at Start', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('No inventory is reserved or deducted at Start');
  });

  it('should close dialog with false on cancel', () => {
    component.onCancel();
    expect(mockDialogRef.close).toHaveBeenCalledWith(false);
  });

  it('should invoke fertilizerService.start and close with true on success', () => {
    const updated = { ...sampleApp, status: 'InProgress' as const, statusName: 'In Progress' };
    mockFertilizerService.start.and.returnValue(of(updated));

    component.onConfirmStart();

    expect(mockFertilizerService.start).toHaveBeenCalledWith(
      'app-1',
      jasmine.objectContaining({ version: 1 }),
    );
    expect(mockSnackBar.open).toHaveBeenCalledWith(
      jasmine.stringMatching(/started successfully/),
      'Dismiss',
      jasmine.any(Object),
    );
    expect(mockDialogRef.close).toHaveBeenCalledWith(true);
  });

  it('should surface shortage errors and remain open when backend returns shortage validation', () => {
    const shortageError = {
      status: 400,
      error: {
        message: 'Validation failed',
        errors: {
          'products[0]': [
            "Insufficient stock for 'NPK 20-20-20' at 'Main Barn'. Required: 100, Available: 40, Shortage: 60 kg.",
          ],
        },
      },
    };
    mockFertilizerService.start.and.returnValue(throwError(() => shortageError));

    component.onConfirmStart();

    expect(component.shortages().length).toBe(1);
    expect(component.shortages()[0]).toContain('Insufficient stock for \'NPK 20-20-20\'');
    expect(mockDialogRef.close).not.toHaveBeenCalled();

    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Insufficient Stock Detected');
  });

  it('should handle 409 conflict gracefully', () => {
    const conflictError = { status: 409, error: { message: 'Concurrency conflict' } };
    mockFertilizerService.start.and.returnValue(throwError(() => conflictError));

    component.onConfirmStart();

    expect(component.errorMessage()).toContain('concurrently');
    expect(mockDialogRef.close).not.toHaveBeenCalled();
  });
});
