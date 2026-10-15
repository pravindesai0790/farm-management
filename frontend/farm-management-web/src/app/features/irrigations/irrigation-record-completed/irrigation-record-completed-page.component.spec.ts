import { ComponentFixture, TestBed } from "@angular/core/testing";
import { FormControl, FormGroup } from "@angular/forms";
import { provideNativeDateAdapter } from "@angular/material/core";
import { MatDialog } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { provideRouter, Router } from "@angular/router";
import { of, throwError } from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import { FarmManagementService } from "../../../core/farm-management/farm-management.service";
import {
  CropCycle,
  CropCycleStage,
  Farm,
  FarmArea,
  Plantation,
  Unit,
} from "../../../core/farm-management/farm-management.models";
import {
  IrrigationDetailsResponse,
  IrrigationMethodDto,
} from "../../../core/irrigations/irrigation.models";
import { IrrigationService } from "../../../core/irrigations/irrigation.service";
import {
  IrrigationRecordCompletedPageComponent,
  startEndDateTimeValidator,
  waterQuantityAndUnitValidator,
} from "./irrigation-record-completed-page.component";

describe("IrrigationRecordCompletedPageComponent", () => {
  let component: IrrigationRecordCompletedPageComponent;
  let fixture: ComponentFixture<IrrigationRecordCompletedPageComponent>;

  let mockIrrigationService: jasmine.SpyObj<IrrigationService>;
  let mockFarmService: jasmine.SpyObj<FarmManagementService>;
  let mockPermissionService: jasmine.SpyObj<PermissionService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockDialog: jasmine.SpyObj<MatDialog>;

  const sampleFarms: any[] = [
    {
      id: "farm-1",
      name: "Sunny Valley Farm",
      totalArea: 100,
      areaUnitSymbol: "ha",
      isActive: true,
    },
  ];

  const sampleAreas: any[] = [
    {
      id: "area-1",
      name: "North Plot",
      farmId: "farm-1",
      totalArea: 25,
      areaUnitSymbol: "ha",
      isActive: true,
    },
  ];

  const samplePlantations: any[] = [
    {
      id: "plant-1",
      farmId: "farm-1",
      farmAreaId: "area-1",
      plantationName: "Block A",
      cropName: "Wheat",
      status: "Active",
      allocatedArea: 10,
    },
  ];

  const sampleCycles: any[] = [
    {
      id: "cycle-1",
      plantationId: "plant-1",
      cycleName: "Wheat 2026",
      status: "Active",
      startDate: "2026-02-01",
    },
  ];

  const sampleStages: any[] = [
    {
      id: "stage-1",
      cropCycleId: "cycle-1",
      stageOrder: 1,
      stageName: "Vegetative",
      status: "IN_PROGRESS",
    },
  ];

  const sampleMethods: IrrigationMethodDto[] = [
    {
      id: "method-1",
      organizationId: null,
      code: "DRIP",
      name: "Drip Irrigation",
      description: "Precision drip system",
      displayOrder: 1,
      isSystem: true,
      isActive: true,
    },
    {
      id: "method-2",
      organizationId: null,
      code: "SPRINKLER",
      name: "Sprinkler System",
      description: "Overhead sprinklers",
      displayOrder: 2,
      isSystem: true,
      isActive: true,
    },
  ];

  const sampleVolumeUnits: Unit[] = [
    {
      id: "unit-l",
      code: "L",
      name: "Liters",
      symbol: "L",
      unitCategory: "Volume",
      isSystem: true,
      isActive: true,
    },
  ];

  beforeEach(async () => {
    mockIrrigationService = jasmine.createSpyObj("IrrigationService", [
      "getMethods",
      "getCropCycleStages",
      "recordCompleted",
    ]);

    mockFarmService = jasmine.createSpyObj("FarmManagementService", [
      "listFarms",
      "listUnits",
      "listAreas",
      "listPlantations",
      "listCycles",
    ]);

    mockPermissionService = jasmine.createSpyObj("PermissionService", [
      "has",
      "hasAny",
    ]);

    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);
    mockRouter = jasmine.createSpyObj("Router", ["navigate"]);
    mockDialog = jasmine.createSpyObj("MatDialog", ["open"]);

    mockFarmService.listFarms.and.returnValue(
      of({ items: sampleFarms, totalCount: 1, page: 1, pageSize: 100, totalPages: 1 } as any),
    );
    mockFarmService.listUnits.and.returnValue(of(sampleVolumeUnits));
    mockFarmService.listAreas.and.returnValue(of(sampleAreas));
    mockFarmService.listPlantations.and.returnValue(
      of({ items: samplePlantations, totalCount: 1, page: 1, pageSize: 100, totalPages: 1 } as any),
    );
    mockFarmService.listCycles.and.returnValue(
      of({ items: sampleCycles, totalCount: 1, page: 1, pageSize: 100, totalPages: 1 } as any),
    );

    mockIrrigationService.getMethods.and.returnValue(of(sampleMethods));
    mockIrrigationService.getCropCycleStages.and.returnValue(of(sampleStages));
    mockPermissionService.has.and.returnValue(true);

    await TestBed.configureTestingModule({
      imports: [IrrigationRecordCompletedPageComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        provideNativeDateAdapter(),
        { provide: IrrigationService, useValue: mockIrrigationService },
        { provide: FarmManagementService, useValue: mockFarmService },
        { provide: PermissionService, useValue: mockPermissionService },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: MatDialog, useValue: mockDialog },
      ],
    }).compileComponents();

    mockRouter = TestBed.inject(Router) as any;
    spyOn(mockRouter, "navigate");

    fixture = TestBed.createComponent(IrrigationRecordCompletedPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create the component and load master data on init", () => {
    expect(component).toBeTruthy();
    expect(mockFarmService.listFarms).toHaveBeenCalledWith(1, 100, "", true);
    expect(mockIrrigationService.getMethods).toHaveBeenCalledWith(true);
    expect(mockFarmService.listUnits).toHaveBeenCalledWith("Volume");
    expect(component.farms().length).toBe(1);
    expect(component.methods().length).toBe(2);
    expect(component.volumeUnits().length).toBe(1);
  });

  describe("Validators", () => {
    it("should validate that Farm, Farm Area, and Irrigation Method are required", () => {
      const form = component.form;
      expect(form.valid).toBeFalse();

      form.patchValue({
        farmId: "farm-1",
        farmAreaId: "area-1",
        irrigationMethodId: "method-1",
      });

      expect(form.valid).toBeTrue();
    });

    it("should validate waterQuantityAndUnitValidator correctly", () => {
      const group = new FormGroup({
        actualWaterQuantity: new FormControl<number | null>(null),
        actualWaterUnitId: new FormControl<string | null>(null),
      });

      expect(waterQuantityAndUnitValidator(group)).toBeNull();

      group.patchValue({ actualWaterQuantity: 100, actualWaterUnitId: null });
      expect(waterQuantityAndUnitValidator(group)).toEqual({ unitRequired: true });

      group.patchValue({ actualWaterQuantity: null, actualWaterUnitId: "unit-1" });
      expect(waterQuantityAndUnitValidator(group)).toEqual({ quantityRequired: true });

      group.patchValue({ actualWaterQuantity: 100, actualWaterUnitId: "unit-1" });
      expect(waterQuantityAndUnitValidator(group)).toBeNull();
    });

    it("should validate startEndDateTimeValidator correctly", () => {
      const group = new FormGroup({
        actualStartedAt: new FormControl<string | null>(null),
        actualEndedAt: new FormControl<string | null>(null),
      });

      expect(startEndDateTimeValidator(group)).toBeNull();

      group.patchValue({
        actualStartedAt: "2026-10-10T14:00:00Z",
        actualEndedAt: "2026-10-10T12:00:00Z",
      });
      expect(startEndDateTimeValidator(group)).toEqual({ endBeforeStart: true });

      group.patchValue({
        actualStartedAt: "2026-10-10T12:00:00Z",
        actualEndedAt: "2026-10-10T14:00:00Z",
      });
      expect(startEndDateTimeValidator(group)).toBeNull();
    });
  });

  describe("Duration Auto-Calculation", () => {
    it("should auto-populate duration when both start and end timestamps are specified", () => {
      component.form.patchValue({
        actualStartedAt: "2026-10-10T10:00:00Z",
        actualEndedAt: "2026-10-10T12:00:00Z",
      });

      expect(component.form.value.actualDurationMinutes).toBe(120);
    });

    it("should update duration when actualEndedAt changes", () => {
      component.form.patchValue({
        actualStartedAt: "2026-10-10T10:00:00Z",
        actualEndedAt: "2026-10-10T11:30:00Z",
      });

      expect(component.form.value.actualDurationMinutes).toBe(90);

      component.form.patchValue({
        actualEndedAt: "2026-10-10T12:15:00Z",
      });

      expect(component.form.value.actualDurationMinutes).toBe(135);
    });

    it("should update duration when actualStartedAt changes", () => {
      component.form.patchValue({
        actualStartedAt: "2026-10-10T10:00:00Z",
        actualEndedAt: "2026-10-10T11:00:00Z",
      });

      expect(component.form.value.actualDurationMinutes).toBe(60);

      component.form.patchValue({
        actualStartedAt: "2026-10-10T10:15:00Z",
      });

      expect(component.form.value.actualDurationMinutes).toBe(45);
    });

    it("should allow manual editing and preserve user manual duration", () => {
      component.form.patchValue({
        actualStartedAt: "2026-10-10T10:00:00Z",
        actualEndedAt: "2026-10-10T12:00:00Z",
      });
      expect(component.form.value.actualDurationMinutes).toBe(120);

      // User manually overrides duration
      component.form.patchValue({ actualDurationMinutes: 110 });
      expect(component.form.value.actualDurationMinutes).toBe(110);
    });
  });

  describe("Cascading Location & Crop Context", () => {
    it("should reset dependent fields and trigger lookups when farm changes", () => {
      component.form.patchValue({
        farmId: "farm-1",
        farmAreaId: "area-1",
        plantationId: "plant-1",
        cropCycleId: "cycle-1",
        cropCycleStageId: "stage-1",
      });

      component.onFarmChange("farm-2");

      expect(component.form.value.farmAreaId).toBe("");
      expect(component.form.value.plantationId).toBe("");
      expect(component.form.value.cropCycleId).toBe("");
      expect(component.form.value.cropCycleStageId).toBe("");
      expect(mockFarmService.listAreas).toHaveBeenCalledWith("farm-2");
    });

    it("should reset plantation and cycle when area changes", () => {
      component.form.patchValue({
        farmId: "farm-1",
        farmAreaId: "area-1",
        plantationId: "plant-1",
      });

      component.onAreaChange("area-2");

      expect(component.form.value.plantationId).toBe("");
      expect(component.form.value.cropCycleId).toBe("");
      expect(mockFarmService.listPlantations).toHaveBeenCalledWith(1, 100, "farm-1", "area-2");
    });

    it("should load cycles when plantation changes", () => {
      component.onPlantationChange("plant-1");

      expect(component.form.value.cropCycleId).toBe("");
      expect(mockFarmService.listCycles).toHaveBeenCalledWith(1, 100, undefined, undefined, "plant-1");
    });

    it("should load stages when cycle changes", () => {
      component.onCropCycleChange("cycle-1");

      expect(component.form.value.cropCycleStageId).toBe("");
      expect(mockIrrigationService.getCropCycleStages).toHaveBeenCalledWith("cycle-1");
    });
  });

  describe("Submission", () => {
    it("should not call service if form is invalid", () => {
      component.onSubmit();

      expect(mockIrrigationService.recordCompleted).not.toHaveBeenCalled();
      expect(component.errorMessage()).toContain("validation errors");
    });

    it("should call recordCompleted and navigate to detail page on successful submission", () => {
      const responseData: IrrigationDetailsResponse = {
        id: "irr-123",
        organizationId: "org-1",
        farmId: "farm-1",
        farmName: "Sunny Valley Farm",
        farmAreaId: "area-1",
        farmAreaName: "North Plot",
        plantationId: null,
        plantationName: null,
        cropCycleId: null,
        cropCycleName: null,
        cropCycleStageId: null,
        cropCycleStageName: null,
        status: 4 as any,
        statusName: "Completed",
        isOverdue: false,
        plannedAt: null,
        scheduledAt: null,
        actualStartedAt: "2026-10-10T10:00:00Z",
        actualEndedAt: "2026-10-10T12:00:00Z",
        actualDurationMinutes: 120,
        irrigationMethodId: "method-1",
        irrigationMethodName: "Drip Irrigation",
        plannedWaterQuantity: null,
        plannedWaterUnitId: null,
        plannedWaterUnitName: null,
        plannedWaterUnitSymbol: null,
        actualWaterQuantity: 2500,
        actualWaterUnitId: "unit-l",
        actualWaterUnitName: "Liters",
        actualWaterUnitSymbol: "L",
        notes: "Field fully irrigated",
        cancellationReason: null,
        completedAt: "2026-10-10T12:00:00Z",
        createdAt: "2026-10-10T12:00:00Z",
        createdBy: "user-1",
        updatedAt: null,
        updatedBy: null,
      };

      mockIrrigationService.recordCompleted.and.returnValue(of(responseData));

      component.form.patchValue({
        farmId: "farm-1",
        farmAreaId: "area-1",
        irrigationMethodId: "method-1",
        actualStartedAt: "2026-10-10T10:00:00Z",
        actualEndedAt: "2026-10-10T12:00:00Z",
        actualDurationMinutes: 120,
        actualWaterQuantity: 2500,
        actualWaterUnitId: "unit-l",
        notes: "Field fully irrigated",
      });

      component.onSubmit();

      expect(mockIrrigationService.recordCompleted).toHaveBeenCalledWith(
        jasmine.objectContaining({
          farmId: "farm-1",
          farmAreaId: "area-1",
          irrigationMethodId: "method-1",
          plantationId: null,
          cropCycleId: null,
          cropCycleStageId: null,
          actualStartedAt: new Date("2026-10-10T10:00:00Z").toISOString(),
          actualEndedAt: new Date("2026-10-10T12:00:00Z").toISOString(),
          actualDurationMinutes: 120,
          actualWaterQuantity: 2500,
          actualWaterUnitId: "unit-l",
          notes: "Field fully irrigated",
          idempotencyKey: jasmine.stringMatching(/^IRR-/),
        }),
      );

      expect(mockSnackBar.open).toHaveBeenCalledWith(
        "Completed irrigation event recorded successfully.",
        "Dismiss",
        jasmine.any(Object),
      );
      expect(mockRouter.navigate).toHaveBeenCalledWith(["/irrigations", "irr-123"]);
    });

    it("should handle submission error gracefully", () => {
      mockIrrigationService.recordCompleted.and.returnValue(
        throwError(() => ({ status: 400, error: { message: "Validation error on server" } })),
      );

      component.form.patchValue({
        farmId: "farm-1",
        farmAreaId: "area-1",
        irrigationMethodId: "method-1",
      });

      component.onSubmit();

      expect(mockIrrigationService.recordCompleted).toHaveBeenCalled();
      expect(component.errorMessage()).toBe("Validation error on server");
      expect(component.isSubmitting()).toBeFalse();
    });
  });

  describe("Cancellation", () => {
    it("should navigate to /irrigations if form is not dirty", () => {
      component.onCancel();
      expect(mockRouter.navigate).toHaveBeenCalledWith(["/irrigations"]);
      expect(mockDialog.open).not.toHaveBeenCalled();
    });

    it("should open confirm dialog if form is dirty and navigate if confirmed", () => {
      component.form.markAsDirty();
      mockDialog.open.and.returnValue({
        afterClosed: () => of(true),
      } as any);

      component.onCancel();

      expect(mockDialog.open).toHaveBeenCalled();
      expect(mockRouter.navigate).toHaveBeenCalledWith(["/irrigations"]);
    });
  });
});
