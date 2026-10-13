import { ComponentFixture, TestBed } from "@angular/core/testing";
import { MatDialog } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { ActivatedRoute, Router, provideRouter } from "@angular/router";
import { of, throwError } from "rxjs";

import { BreadcrumbService } from "../../../core/breadcrumb/breadcrumb.service";
import { PermissionService } from "../../../core/auth/permission.service";
import { IrrigationDetailsResponse } from "../../../core/irrigations/irrigation.models";
import { IrrigationService } from "../../../core/irrigations/irrigation.service";
import { IrrigationDetailPageComponent } from "./irrigation-detail-page.component";
import { CancelIrrigationDialogComponent } from "../dialogs/cancel-irrigation-dialog/cancel-irrigation-dialog.component";
import { CompleteIrrigationDialogComponent } from "../dialogs/complete-irrigation-dialog/complete-irrigation-dialog.component";
import { ScheduleIrrigationDialogComponent } from "../dialogs/schedule-irrigation-dialog/schedule-irrigation-dialog.component";
import { StartIrrigationDialogComponent } from "../dialogs/start-irrigation-dialog/start-irrigation-dialog.component";

describe("IrrigationDetailPageComponent", () => {
  let component: IrrigationDetailPageComponent;
  let fixture: ComponentFixture<IrrigationDetailPageComponent>;

  let mockIrrigationService: jasmine.SpyObj<IrrigationService>;
  let mockBreadcrumbService: jasmine.SpyObj<BreadcrumbService>;
  let mockPermissionService: jasmine.SpyObj<PermissionService>;
  let mockDialog: jasmine.SpyObj<MatDialog>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;
  let mockRouter: jasmine.SpyObj<Router>;

  const sampleIrrigation: IrrigationDetailsResponse = {
    id: "irr-123",
    organizationId: "org-1",
    farmId: "farm-1",
    farmName: "Green Valley Farm",
    farmAreaId: "area-1",
    farmAreaName: "North Block",
    plantationId: "plant-1",
    plantationName: "Wheat Field",
    cropCycleId: "cycle-1",
    cropCycleName: "Spring 2026",
    cropCycleStageId: "stage-1",
    cropCycleStageName: "Vegetative",
    irrigationMethodId: "method-1",
    irrigationMethodName: "Drip Irrigation",
    status: "Draft",
    statusName: "Draft",
    isOverdue: false,
    plannedAt: "2026-10-25T08:00:00Z",
    scheduledAt: null,
    actualStartedAt: null,
    actualEndedAt: null,
    actualDurationMinutes: null,
    plannedWaterQuantity: 1000,
    plannedWaterUnitId: "unit-l",
    plannedWaterUnitName: "Liters",
    plannedWaterUnitSymbol: "L",
    actualWaterQuantity: null,
    actualWaterUnitId: null,
    actualWaterUnitName: null,
    actualWaterUnitSymbol: null,
    cancellationReason: null,
    notes: "Planned watering notes",
    concurrencyToken: "ct-100",
    createdAt: "2026-10-12T00:00:00Z",
    createdBy: "user-1",
    createdByName: "John Doe",
    updatedAt: null,
    updatedBy: null,
    completedAt: null,
  };

  const setupTest = async (
    routeId: string | null = "irr-123",
    initialEvent?: IrrigationDetailsResponse,
    error?: any,
  ) => {
    mockIrrigationService = jasmine.createSpyObj("IrrigationService", ["getIrrigation"]);
    mockBreadcrumbService = jasmine.createSpyObj("BreadcrumbService", ["setEntityName"]);
    mockPermissionService = jasmine.createSpyObj("PermissionService", ["has"]);
    mockDialog = jasmine.createSpyObj("MatDialog", ["open"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);

    mockPermissionService.has.and.returnValue(true);
    if (error) {
      mockIrrigationService.getIrrigation.and.returnValue(throwError(() => error));
    } else {
      mockIrrigationService.getIrrigation.and.returnValue(of(initialEvent ?? sampleIrrigation));
    }

    await TestBed.configureTestingModule({
      imports: [IrrigationDetailPageComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: IrrigationService, useValue: mockIrrigationService },
        { provide: BreadcrumbService, useValue: mockBreadcrumbService },
        { provide: PermissionService, useValue: mockPermissionService },
        { provide: MatDialog, useValue: mockDialog },
        { provide: MatSnackBar, useValue: mockSnackBar },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: { get: (key: string) => (key === "id" ? routeId : null) } },
          },
        },
      ],
    }).compileComponents();

    mockRouter = TestBed.inject(Router) as any;
    spyOn(mockRouter, "navigate");

    fixture = TestBed.createComponent(IrrigationDetailPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  };

  describe("Initialization & Data Loading", () => {
    it("should load irrigation event details and update breadcrumbs", async () => {
      await setupTest("irr-123");
      expect(component).toBeTruthy();
      expect(mockIrrigationService.getIrrigation).toHaveBeenCalledWith("irr-123");
      expect(mockBreadcrumbService.setEntityName).toHaveBeenCalledWith(
        "irr-123",
        "Green Valley Farm - North Block",
      );
      expect(component.irrigation()).toEqual(sampleIrrigation);
      expect(component.isLoading()).toBeFalse();
    });

    it("should display error message when route id is missing", async () => {
      await setupTest(null);
      expect(component.errorMessage()).toBe("No irrigation event ID specified.");
      expect(mockIrrigationService.getIrrigation).not.toHaveBeenCalled();
    });

    it("should handle error when loadIrrigation fails", async () => {
      await setupTest("irr-missing", undefined, { status: 404, error: { message: "Irrigation event not found" } });

      expect(component.errorMessage()).toBe("Irrigation event not found");
      expect(mockSnackBar.open).toHaveBeenCalledWith("Irrigation event not found", "Dismiss", jasmine.any(Object));
    });
  });

  describe("Lifecycle Actions & Dialog Invocations", () => {
    beforeEach(async () => {
      await setupTest("irr-123");
    });

    it("should navigate to edit draft route on onEdit", () => {
      component.onEdit();
      expect(mockRouter.navigate).toHaveBeenCalledWith(["/irrigations", "irr-123", "edit"]);
    });

    it("should open ScheduleIrrigationDialogComponent on onSchedule", () => {
      const dialogRefSpy = jasmine.createSpyObj({ afterClosed: of(true) });
      mockDialog.open.and.returnValue(dialogRefSpy);

      component.onSchedule();

      expect(mockDialog.open).toHaveBeenCalledWith(
        ScheduleIrrigationDialogComponent,
        jasmine.objectContaining({
          data: jasmine.objectContaining({
            irrigationId: "irr-123",
            isReschedule: false,
          }),
        }),
      );
      expect(mockIrrigationService.getIrrigation).toHaveBeenCalledTimes(2); // reloaded
    });

    it("should open ScheduleIrrigationDialogComponent in reschedule mode on onReschedule", () => {
      const scheduledEvent = { ...sampleIrrigation, status: "Scheduled" as any, scheduledAt: "2026-10-25T10:00:00Z" };
      component.irrigation.set(scheduledEvent);

      const dialogRefSpy = jasmine.createSpyObj({ afterClosed: of(true) });
      mockDialog.open.and.returnValue(dialogRefSpy);

      component.onReschedule();

      expect(mockDialog.open).toHaveBeenCalledWith(
        ScheduleIrrigationDialogComponent,
        jasmine.objectContaining({
          data: jasmine.objectContaining({
            irrigationId: "irr-123",
            isReschedule: true,
            currentScheduledAt: "2026-10-25T10:00:00Z",
          }),
        }),
      );
    });

    it("should open StartIrrigationDialogComponent on onStart", () => {
      const dialogRefSpy = jasmine.createSpyObj({ afterClosed: of(true) });
      mockDialog.open.and.returnValue(dialogRefSpy);

      component.onStart();

      expect(mockDialog.open).toHaveBeenCalledWith(
        StartIrrigationDialogComponent,
        jasmine.objectContaining({
          data: jasmine.objectContaining({
            irrigationId: "irr-123",
          }),
        }),
      );
    });

    it("should open CompleteIrrigationDialogComponent on onComplete", () => {
      const dialogRefSpy = jasmine.createSpyObj({ afterClosed: of(true) });
      mockDialog.open.and.returnValue(dialogRefSpy);

      component.onComplete();

      expect(mockDialog.open).toHaveBeenCalledWith(
        CompleteIrrigationDialogComponent,
        jasmine.objectContaining({
          data: jasmine.objectContaining({
            irrigation: sampleIrrigation,
          }),
        }),
      );
    });

    it("should open CancelIrrigationDialogComponent on onCancel", () => {
      const dialogRefSpy = jasmine.createSpyObj({ afterClosed: of(true) });
      mockDialog.open.and.returnValue(dialogRefSpy);

      component.onCancel();

      expect(mockDialog.open).toHaveBeenCalledWith(
        CancelIrrigationDialogComponent,
        jasmine.objectContaining({
          data: jasmine.objectContaining({
            irrigationId: "irr-123",
            currentStatus: "Draft",
          }),
        }),
      );
    });
  });

  describe("Terminal State Enforcement", () => {
    it("should recognize Completed status as terminal", async () => {
      const completedEvent = { ...sampleIrrigation, status: "Completed" as any, completedAt: "2026-10-25T12:00:00Z" };
      await setupTest("irr-123", completedEvent);

      expect(component.isTerminal()).toBeTrue();
    });

    it("should recognize Cancelled status as terminal", async () => {
      const cancelledEvent = {
        ...sampleIrrigation,
        status: "Cancelled" as any,
        cancellationReason: "Heavy rain",
      };
      await setupTest("irr-123", cancelledEvent);

      expect(component.isTerminal()).toBeTrue();
    });
  });
});
