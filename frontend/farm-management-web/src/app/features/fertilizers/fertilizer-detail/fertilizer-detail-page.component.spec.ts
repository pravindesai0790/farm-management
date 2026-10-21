import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of, throwError } from 'rxjs';

import { BreadcrumbService } from '../../../core/breadcrumb/breadcrumb.service';
import { PermissionService } from '../../../core/auth/permission.service';
import {
  FertilizerApplicationDetailsResponse,
  FertilizerApplicationProductDto,
} from '../../../core/fertilizers/fertilizer.models';
import { FertilizerService } from '../../../core/fertilizers/fertilizer.service';
import { FertilizerDetailPageComponent } from './fertilizer-detail-page.component';

describe('FertilizerDetailPageComponent', () => {
  let component: FertilizerDetailPageComponent;
  let fixture: ComponentFixture<FertilizerDetailPageComponent>;
  let fertilizerService: jasmine.SpyObj<FertilizerService>;
  let breadcrumbService: jasmine.SpyObj<BreadcrumbService>;
  let permissionService: jasmine.SpyObj<PermissionService>;
  let dialog: jasmine.SpyObj<MatDialog>;
  let snackBar: jasmine.SpyObj<MatSnackBar>;
  let router: Router;

  const mockProduct: FertilizerApplicationProductDto = {
    id: 'prod-1',
    fertilizerApplicationId: 'app-1',
    inventoryItemId: 'item-1',
    inventoryItemName: 'Urea 46-0-0',
    inventoryItemSku: 'FERT-001',
    stockUnitId: 'unit-kg',
    stockUnitName: 'Kilograms',
    stockUnitCode: 'KG',
    stockUnitSymbol: 'kg',
    storageLocationId: 'loc-1',
    storageLocationName: 'Main Warehouse',
    plannedQuantity: 50,
    actualQuantity: 55,
    varianceQuantity: 5,
    stockMovementId: 'sm-1',
    stockMovementReference: 'SM-202610-001',
  };

  const createMockApplication = (
    overrides: Partial<FertilizerApplicationDetailsResponse> = {},
  ): FertilizerApplicationDetailsResponse => ({
    id: 'app-1',
    organizationId: 'org-1',
    referenceNumber: 'FA-2026-0001',
    status: 'Planned',
    statusName: 'Planned',
    farmId: 'farm-1',
    farmName: 'Green Valley Farm',
    farmAreaId: 'area-1',
    farmAreaName: 'North Block',
    plantationId: 'plant-1',
    plantationName: 'Avocado Block A',
    cropCycleId: 'cycle-1',
    cropCycleName: 'Cycle 2026',
    cropCycleStageId: 'stage-1',
    cropCycleStageName: 'Vegetative Growth',
    applicationMethodId: 'method-1',
    applicationMethodName: 'Soil Broadcasting',
    otherMethodDescription: null,
    irrigationEventId: null,
    irrigationEventReference: null,
    plannedDate: '2026-10-25',
    scheduledDate: '2026-10-25',
    actualApplicationDate: null,
    plannedArea: 5.5,
    plannedAreaUnitId: 'unit-ha',
    plannedAreaUnitName: 'ha',
    actualArea: null,
    actualAreaUnitId: null,
    actualAreaUnitName: null,
    purposeReason: 'Spring nutrient replenishment',
    cancellationReason: null,
    products: [mockProduct],
    createdAt: '2026-10-20T10:00:00Z',
    createdBy: 'user-1',
    createdByName: 'John Doe',
    updatedAt: '2026-10-20T11:00:00Z',
    updatedBy: 'user-1',
    updatedByName: 'John Doe',
    startedAt: null,
    startedBy: null,
    completedAt: null,
    completedBy: null,
    cancelledAt: null,
    cancelledBy: null,
    version: 1,
    ...overrides,
  });

  function hasActionButton(text: string): boolean {
    const actionButtons = fixture.nativeElement.querySelectorAll('.header-actions button');
    return Array.from(actionButtons).some((btn: any) => btn.textContent.includes(text));
  }

  beforeEach(async () => {
    fertilizerService = jasmine.createSpyObj<FertilizerService>('FertilizerService', [
      'get',
      'copyAsNewDraft',
      'deleteDraft',
    ]);
    breadcrumbService = jasmine.createSpyObj<BreadcrumbService>('BreadcrumbService', [
      'setEntityName',
    ]);
    permissionService = jasmine.createSpyObj<PermissionService>('PermissionService', [
      'has',
    ]);
    permissionService.has.and.returnValue(true); // default: allow all
    dialog = jasmine.createSpyObj<MatDialog>('MatDialog', ['open']);
    snackBar = jasmine.createSpyObj<MatSnackBar>('MatSnackBar', ['open']);

    fertilizerService.get.and.returnValue(of(createMockApplication()));
    const defaultDialogRef = jasmine.createSpyObj({ afterClosed: of(true) });
    dialog.open.and.returnValue(defaultDialogRef);

    await TestBed.configureTestingModule({
      imports: [FertilizerDetailPageComponent],
      providers: [
        provideNoopAnimations(),
        provideRouter([]),
        { provide: FertilizerService, useValue: fertilizerService },
        { provide: BreadcrumbService, useValue: breadcrumbService },
        { provide: PermissionService, useValue: permissionService },
        { provide: MatDialog, useValue: dialog },
        { provide: MatSnackBar, useValue: snackBar },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (key: string) => (key === 'id' ? 'app-1' : null),
              },
            },
          },
        },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    spyOn(router, 'navigate');

    fixture = TestBed.createComponent(FertilizerDetailPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should load application details on init and set breadcrumb', () => {
    expect(fertilizerService.get).toHaveBeenCalledWith('app-1');
    expect(component.application()).toBeTruthy();
    expect(component.application()?.referenceNumber).toBe('FA-2026-0001');
    expect(breadcrumbService.setEntityName).toHaveBeenCalledWith('app-1', 'FA-2026-0001');
    expect(component.isLoading()).toBeFalse();
  });

  it('should display error message when loading details fails', () => {
    fertilizerService.get.and.returnValue(throwError(() => new Error('Network error')));
    component.loadDetails();
    fixture.detectChanges();

    expect(component.errorMessage()).toBe('Network error');
    expect(component.isLoading()).toBeFalse();
  });

  describe('Status-Based Action Visibility', () => {
    it('Draft: should display Edit Draft, Schedule, Delete Draft, and Copy as New Draft', () => {
      component.application.set(createMockApplication({ status: 'Draft', statusName: 'Draft' }));
      fixture.detectChanges();

      expect(hasActionButton('Edit Draft')).toBeTrue();
      expect(hasActionButton('Schedule')).toBeTrue();
      expect(hasActionButton('Delete Draft')).toBeTrue();
      expect(hasActionButton('Copy as New Draft')).toBeTrue();
      expect(hasActionButton('Start Application')).toBeFalse();
      expect(hasActionButton('Complete & Deduct Stock')).toBeFalse();
    });

    it('Planned: should display Edit Plan, Schedule, Start Application, Cancel, and Copy', () => {
      component.application.set(createMockApplication({ status: 'Planned', statusName: 'Planned' }));
      fixture.detectChanges();

      expect(hasActionButton('Edit Plan')).toBeTrue();
      expect(hasActionButton('Schedule')).toBeTrue();
      expect(hasActionButton('Start Application')).toBeTrue();
      expect(hasActionButton('Cancel')).toBeTrue();
      expect(hasActionButton('Copy as New Draft')).toBeTrue();
      expect(hasActionButton('Delete Draft')).toBeFalse();
    });

    it('Scheduled: should display Reschedule, Start Application, Cancel, and Copy', () => {
      component.application.set(createMockApplication({ status: 'Scheduled', statusName: 'Scheduled' }));
      fixture.detectChanges();

      expect(hasActionButton('Reschedule')).toBeTrue();
      expect(hasActionButton('Start Application')).toBeTrue();
      expect(hasActionButton('Cancel')).toBeTrue();
      expect(hasActionButton('Copy as New Draft')).toBeTrue();
      expect(hasActionButton('Delete Draft')).toBeFalse();
    });

    it('InProgress: should display Complete & Deduct Stock, Cancel, and Copy; hide Edit/Start', () => {
      component.application.set(createMockApplication({ status: 'InProgress', statusName: 'InProgress' }));
      fixture.detectChanges();

      expect(hasActionButton('Complete & Deduct Stock')).toBeTrue();
      expect(hasActionButton('Cancel')).toBeTrue();
      expect(hasActionButton('Copy as New Draft')).toBeTrue();
      expect(hasActionButton('Edit Draft')).toBeFalse();
      expect(hasActionButton('Edit Plan')).toBeFalse();
      expect(hasActionButton('Start Application')).toBeFalse();
    });

    it('Completed: should be strictly read-only, showing only Copy as New Draft', () => {
      component.application.set(
        createMockApplication({
          status: 'Completed',
          statusName: 'Completed',
          completedAt: '2026-10-21T08:00:00Z',
          completedByName: 'Jane Operator',
        }),
      );
      fixture.detectChanges();

      expect(hasActionButton('Copy as New Draft')).toBeTrue();
      expect(hasActionButton('Edit Draft')).toBeFalse();
      expect(hasActionButton('Edit Plan')).toBeFalse();
      expect(hasActionButton('Schedule')).toBeFalse();
      expect(hasActionButton('Start Application')).toBeFalse();
      expect(hasActionButton('Complete & Deduct Stock')).toBeFalse();
      expect(hasActionButton('Cancel')).toBeFalse();
      expect(hasActionButton('Delete Draft')).toBeFalse();
    });

    it('Cancelled: should be strictly read-only, displaying cancellation reason and Copy action', () => {
      component.application.set(
        createMockApplication({
          status: 'Cancelled',
          statusName: 'Cancelled',
          cancellationReason: 'Adverse heavy rain',
          cancelledAt: '2026-10-21T09:00:00Z',
          cancelledByName: 'Jane Operator',
        }),
      );
      fixture.detectChanges();

      expect(hasActionButton('Copy as New Draft')).toBeTrue();
      expect(hasActionButton('Edit Draft')).toBeFalse();
      expect(hasActionButton('Start Application')).toBeFalse();
      expect(hasActionButton('Complete & Deduct Stock')).toBeFalse();

      const text = fixture.nativeElement.textContent;
      expect(text).toContain('This fertilizer application was cancelled');
      expect(text).toContain('Adverse heavy rain');
    });
  });

  describe('Empty DRAFT support', () => {
    it('should render empty state message when application has 0 products', () => {
      component.application.set(
        createMockApplication({
          status: 'Draft',
          statusName: 'Draft',
          products: [],
        }),
      );
      fixture.detectChanges();

      const text = fixture.nativeElement.textContent;
      expect(text).toContain('No Products Allocated');
      expect(text).toContain('Add Products in Editor');
    });
  });

  describe('Product Line Variance', () => {
    it('calculateVariance should compute correct over, under, and match directions', () => {
      const match = component.calculateVariance({
        ...mockProduct,
        plannedQuantity: 100,
        actualQuantity: 100,
      });
      expect(match.direction).toBe('match');
      expect(match.val).toBe(0);

      const over = component.calculateVariance({
        ...mockProduct,
        plannedQuantity: 100,
        actualQuantity: 110,
      });
      expect(over.direction).toBe('over');
      expect(over.val).toBe(10);
      expect(over.pct).toBe(10);

      const under = component.calculateVariance({
        ...mockProduct,
        plannedQuantity: 100,
        actualQuantity: 90,
      });
      expect(under.direction).toBe('under');
      expect(under.val).toBe(-10);
      expect(under.pct).toBe(-10);
    });
  });

  describe('Timeline Milestones', () => {
    it('should construct ordered milestones including draft creation, planned, scheduled, started, completed', () => {
      component.application.set(
        createMockApplication({
          status: 'Completed',
          plannedDate: '2026-10-20',
          scheduledDate: '2026-10-20',
          startedAt: '2026-10-21T06:00:00Z',
          completedAt: '2026-10-21T08:00:00Z',
          completedByName: 'Jane Operator',
        }),
      );

      const steps = component.timelineSteps();
      expect(steps.length).toBe(5);
      expect(steps[0].id).toBe('created');
      expect(steps[1].id).toBe('planned');
      expect(steps[2].id).toBe('scheduled');
      expect(steps[3].id).toBe('started');
      expect(steps[4].id).toBe('completed');
      expect(steps[4].actor).toBe('Jane Operator');
    });
  });

  describe('Copy as New Draft', () => {
    it('should open confirm dialog, call copyAsNewDraft, and display snackbar with navigation', () => {
      const newDraftResponse = createMockApplication({
        id: 'new-app-2',
        referenceNumber: 'FA-2026-0002',
        status: 'Draft',
      });
      fertilizerService.copyAsNewDraft.and.returnValue(of(newDraftResponse));

      const snackRefSpy = jasmine.createSpyObj({ onAction: of(null) });
      snackBar.open.and.returnValue(snackRefSpy);

      component.onCopyAsNewDraft();

      expect(dialog.open).toHaveBeenCalled();
      expect(fertilizerService.copyAsNewDraft).toHaveBeenCalledWith('app-1');
      expect(snackBar.open).toHaveBeenCalledWith(
        jasmine.stringMatching(/New draft 'FA-2026-0002' created from 'FA-2026-0001'/),
        'Open Draft',
        jasmine.any(Object),
      );
      expect(router.navigate).toHaveBeenCalledWith(['/fertilizer-applications', 'new-app-2', 'edit']);
    });
  });

  describe('Delete Draft', () => {
    it('should open confirm dialog, call deleteDraft, and navigate to list', () => {
      component.application.set(createMockApplication({ status: 'Draft', statusName: 'Draft' }));
      fertilizerService.deleteDraft.and.returnValue(of(undefined));

      component.onDeleteDraft();

      expect(dialog.open).toHaveBeenCalled();
      expect(fertilizerService.deleteDraft).toHaveBeenCalledWith('app-1');
      expect(snackBar.open).toHaveBeenCalledWith(
        jasmine.stringMatching(/deleted successfully/),
        'OK',
        jasmine.any(Object),
      );
      expect(router.navigate).toHaveBeenCalledWith(['/fertilizer-applications']);
    });
  });

  describe('Dialog Openers', () => {
    it('onSchedule should open ScheduleFertilizerDialogComponent and reload on close', () => {
      spyOn(component, 'loadDetails');

      component.onSchedule(false);

      expect(dialog.open).toHaveBeenCalled();
      expect(component.loadDetails).toHaveBeenCalled();
    });

    it('onStart should open StartFertilizerDialogComponent and reload on close', () => {
      spyOn(component, 'loadDetails');

      component.onStart();

      expect(dialog.open).toHaveBeenCalled();
      expect(component.loadDetails).toHaveBeenCalled();
    });

    it('onComplete should open CompleteFertilizerDialogComponent and reload on close', () => {
      spyOn(component, 'loadDetails');

      component.onComplete();

      expect(dialog.open).toHaveBeenCalled();
      expect(component.loadDetails).toHaveBeenCalled();
    });

    it('onCancel should open CancelFertilizerDialogComponent and reload on close', () => {
      spyOn(component, 'loadDetails');

      component.onCancel();

      expect(dialog.open).toHaveBeenCalled();
      expect(component.loadDetails).toHaveBeenCalled();
    });
  });
});
