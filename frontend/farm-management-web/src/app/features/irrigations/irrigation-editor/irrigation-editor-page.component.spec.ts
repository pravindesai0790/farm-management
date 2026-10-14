import { ComponentFixture, TestBed } from "@angular/core/testing";
import { FormControl, FormGroup } from "@angular/forms";
import { provideNativeDateAdapter } from "@angular/material/core";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { ActivatedRoute, Router } from "@angular/router";
import { of, throwError } from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import { BreadcrumbService } from "../../../core/breadcrumb/breadcrumb.service";
import { FarmManagementService } from "../../../core/farm-management/farm-management.service";
import {
  CropCycle,
  CropCycleStage,
  CycleList,
  Farm,
  FarmArea,
  Plantation,
  PlantationList,
  Unit,
} from "../../../core/farm-management/farm-management.models";
import {
  IrrigationDetailsResponse,
  IrrigationMethodDto,
} from "../../../core/irrigations/irrigation.models";
import { IrrigationService } from "../../../core/irrigations/irrigation.service";
import {
  IrrigationEditorPageComponent,
  waterQuantityAndUnitValidator,
} from "./irrigation-editor-page.component";

describe("IrrigationEditorPageComponent", () => {
  let component: IrrigationEditorPageComponent;
  let fixture: ComponentFixture<IrrigationEditorPageComponent>;

  let mockIrrigationService: jasmine.SpyObj<IrrigationService>;
  let mockFarmService: jasmine.SpyObj<FarmManagementService>;
  let mockPermissionService: jasmine.SpyObj<PermissionService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockBreadcrumbService: jasmine.SpyObj<BreadcrumbService>;

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
      cropName: "Wheat",
      varietyName: "Durum",
      status: "Active",
      allocatedArea: 10,
    },
  ];

  const sampleCycles: any[] = [
    {
      id: "cycle-1",
      plantationId: "plant-1",
      cropName: "Wheat",
      seasonName: "Spring 2026",
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
      plannedStartDate: "2026-02-01",
      plannedEndDate: "2026-03-01",
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
    {
      id: "unit-m3",
      code: "M3",
      name: "Cubic Meters",
      symbol: "m³",
      unitCategory: "Volume",
      isSystem: true,
      isActive: true,
    },
  ];

  const sampleDraftEvent: IrrigationDetailsResponse = {
    id: "irr-123",
    organizationId: "org-1",
    farmId: "farm-1",
    farmName: "Sunny Valley Farm",
    farmAreaId: "area-1",
    farmAreaName: "North Plot",
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
    plannedWaterQuantity: 1500,
    plannedWaterUnitId: "unit-l",
    plannedWaterUnitName: "Liters",
    plannedWaterUnitSymbol: "L",
    actualWaterQuantity: null,
    actualWaterUnitId: null,
    actualWaterUnitName: null,
    actualWaterUnitSymbol: null,
    cancellationReason: null,
    completedAt: null,
    notes: "Initial draft plan",
    concurrencyToken: "ct-123-abc",
    createdAt: "2026-10-12T00:00:00Z",
    createdBy: "user-1",
    updatedAt: null,
    updatedBy: null,
  };

  const setupTestingModule = async (routeId: string | null = null) => {
    mockIrrigationService = jasmine.createSpyObj("IrrigationService", [
      "getMethods",
      "getIrrigation",
      "createDraft",
      "updateDraft",
      "getCropCycleStages",
    ]);
    mockFarmService = jasmine.createSpyObj("FarmManagementService", [
      "listFarms",
      "listAreas",
      "listPlantations",
      "listCycles",
      "listUnits",
    ]);
    mockPermissionService = jasmine.createSpyObj("PermissionService", ["has"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);
    mockRouter = jasmine.createSpyObj("Router", ["navigate"]);
    mockBreadcrumbService = jasmine.createSpyObj("BreadcrumbService", ["setEntityName"]);

    mockPermissionService.has.and.returnValue(true);
    mockFarmService.listFarms.and.returnValue(
      of({ items: sampleFarms, totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
    );
    mockFarmService.listAreas.and.returnValue(of(sampleAreas));
    mockFarmService.listPlantations.and.returnValue(
      of({ items: samplePlantations, totalCount: 1, page: 1, pageSize: 100, totalPages: 1 } as PlantationList),
    );
    mockFarmService.listCycles.and.returnValue(
      of({ items: sampleCycles, totalCount: 1, page: 1, pageSize: 100, totalPages: 1 } as CycleList),
    );
    mockFarmService.listUnits.and.returnValue(of(sampleVolumeUnits));

    mockIrrigationService.getMethods.and.returnValue(of(sampleMethods));
    mockIrrigationService.getCropCycleStages.and.returnValue(of(sampleStages));
    mockIrrigationService.getIrrigation.and.returnValue(of(sampleDraftEvent));
    mockIrrigationService.createDraft.and.returnValue(of(sampleDraftEvent));
    mockIrrigationService.updateDraft.and.returnValue(of(sampleDraftEvent));

    await TestBed.configureTestingModule({
      imports: [IrrigationEditorPageComponent, NoopAnimationsModule],
      providers: [
        provideNativeDateAdapter(),
        { provide: IrrigationService, useValue: mockIrrigationService },
        { provide: FarmManagementService, useValue: mockFarmService },
        { provide: PermissionService, useValue: mockPermissionService },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: Router, useValue: mockRouter },
        { provide: BreadcrumbService, useValue: mockBreadcrumbService },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: { get: () => routeId } },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(IrrigationEditorPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  };

  describe("Unit: waterQuantityAndUnitValidator", () => {
    it("should return waterUnitRequired if quantity is provided without unit", () => {
      const group = new FormGroup({
        plannedWaterQuantity: new FormControl(500),
        plannedWaterUnitId: new FormControl(""),
      });
      const error = waterQuantityAndUnitValidator(group);
      expect(error).toEqual({ waterUnitRequired: true });
    });

    it("should return waterQuantityRequired if unit is selected without quantity", () => {
      const group = new FormGroup({
        plannedWaterQuantity: new FormControl(null),
        plannedWaterUnitId: new FormControl("unit-l"),
      });
      const error = waterQuantityAndUnitValidator(group);
      expect(error).toEqual({ waterQuantityRequired: true });
    });

    it("should return null if both quantity and unit are provided", () => {
      const group = new FormGroup({
        plannedWaterQuantity: new FormControl(500),
        plannedWaterUnitId: new FormControl("unit-l"),
      });
      const error = waterQuantityAndUnitValidator(group);
      expect(error).toBeNull();
    });

    it("should return null if neither quantity nor unit is provided", () => {
      const group = new FormGroup({
        plannedWaterQuantity: new FormControl(null),
        plannedWaterUnitId: new FormControl(""),
      });
      const error = waterQuantityAndUnitValidator(group);
      expect(error).toBeNull();
    });
  });

  describe("Create Mode (id = null)", () => {
    beforeEach(async () => {
      await setupTestingModule(null);
    });

    it("should initialize component in create mode and load lookup data", () => {
      expect(component).toBeTruthy();
      expect(component.isEditMode()).toBeFalse();
      expect(mockFarmService.listFarms).toHaveBeenCalledWith(1, 100, "", true);
      expect(mockIrrigationService.getMethods).toHaveBeenCalledWith(true);
      expect(mockFarmService.listUnits).toHaveBeenCalledWith("Volume");
      expect(component.farms().length).toBe(1);
      expect(component.methods().length).toBe(2);
      expect(component.volumeUnits().length).toBe(2);
      expect(mockIrrigationService.getIrrigation).not.toHaveBeenCalled();
    });

    it("should have invalid form initially because farmId and farmAreaId are required", () => {
      expect(component.form.valid).toBeFalse();
      expect(component.form.controls.farmId.valid).toBeFalse();
      expect(component.form.controls.farmAreaId.valid).toBeFalse();
      expect(component.form.controls.farmId.hasError("required")).toBeTrue();
      expect(component.form.controls.farmAreaId.hasError("required")).toBeTrue();
    });

    it("should reload areas and reset dependent controls when onFarmChange is called", () => {
      component.form.patchValue({
        farmAreaId: "area-1",
        plantationId: "plant-1",
        cropCycleId: "cycle-1",
        cropCycleStageId: "stage-1",
      });

      component.onFarmChange("farm-1");

      expect(component.form.value.farmAreaId).toBe("");
      expect(component.form.value.plantationId).toBe("");
      expect(component.form.value.cropCycleId).toBe("");
      expect(component.form.value.cropCycleStageId).toBe("");
      expect(mockFarmService.listAreas).toHaveBeenCalledWith("farm-1");
      expect(mockFarmService.listPlantations).toHaveBeenCalledWith(1, 100, "farm-1", undefined);
      expect(component.areas().length).toBe(1);
    });

    it("should reset cycle and stage and reload plantations when onAreaChange is called", () => {
      component.form.patchValue({
        farmId: "farm-1",
        farmAreaId: "area-1",
        plantationId: "plant-1",
        cropCycleId: "cycle-1",
        cropCycleStageId: "stage-1",
      });

      component.onAreaChange("area-1");

      expect(component.form.value.plantationId).toBe("");
      expect(component.form.value.cropCycleId).toBe("");
      expect(component.form.value.cropCycleStageId).toBe("");
      expect(mockFarmService.listPlantations).toHaveBeenCalledWith(1, 100, "farm-1", "area-1");
    });

    it("should reset stage and reload cycles when onPlantationChange is called", () => {
      component.form.patchValue({
        cropCycleId: "cycle-1",
        cropCycleStageId: "stage-1",
      });

      component.onPlantationChange("plant-1");

      expect(component.form.value.cropCycleId).toBe("");
      expect(component.form.value.cropCycleStageId).toBe("");
      expect(mockFarmService.listCycles).toHaveBeenCalledWith(1, 100, undefined, undefined, "plant-1");
      expect(component.cropCycles().length).toBe(1);
    });

    it("should reload stages when onCropCycleChange is called", () => {
      component.form.patchValue({
        cropCycleStageId: "stage-1",
      });

      component.onCropCycleChange("cycle-1");

      expect(component.form.value.cropCycleStageId).toBe("");
      expect(mockIrrigationService.getCropCycleStages).toHaveBeenCalledWith("cycle-1");
      expect(component.stages().length).toBe(1);
    });

    it("should show validation warning and not submit if form is invalid on save", () => {
      component.onSave();

      expect(mockSnackBar.open).toHaveBeenCalledWith(
        "Please correct form errors before saving",
        "Dismiss",
        jasmine.any(Object),
      );
      expect(mockIrrigationService.createDraft).not.toHaveBeenCalled();
    });

    it("should submit createDraft and navigate to detail page when form is valid", () => {
      component.form.patchValue({
        farmId: "farm-1",
        farmAreaId: "area-1",
        plantationId: "plant-1",
        cropCycleId: "cycle-1",
        cropCycleStageId: "stage-1",
        irrigationMethodId: "method-1",
        plannedAt: "2026-10-25T08:00:00.000Z",
        plannedWaterQuantity: 1500,
        plannedWaterUnitId: "unit-l",
        notes: "Planned morning cycle",
      });

      component.onSave();

      expect(mockIrrigationService.createDraft).toHaveBeenCalledWith(
        jasmine.objectContaining({
          farmId: "farm-1",
          farmAreaId: "area-1",
          plantationId: "plant-1",
          cropCycleId: "cycle-1",
          cropCycleStageId: "stage-1",
          irrigationMethodId: "method-1",
          plannedWaterQuantity: 1500,
          plannedWaterUnitId: "unit-l",
          notes: "Planned morning cycle",
        }),
      );
      expect(mockSnackBar.open).toHaveBeenCalledWith(
        "Irrigation event draft created successfully",
        "OK",
        jasmine.any(Object),
      );
      expect(mockRouter.navigate).toHaveBeenCalledWith(["/irrigations", "irr-123"]);
    });

    it("should handle error when createDraft fails", () => {
      mockIrrigationService.createDraft.and.returnValue(
        throwError(() => ({ status: 400, error: { message: "Invalid hierarchy" } })),
      );

      component.form.patchValue({
        farmId: "farm-1",
        farmAreaId: "area-1",
      });

      component.onSave();

      expect(component.isSubmitting()).toBeFalse();
      expect(component.errorMessage()).toBe("Invalid hierarchy");
      expect(mockSnackBar.open).toHaveBeenCalledWith("Invalid hierarchy", "Dismiss", jasmine.any(Object));
      expect(mockRouter.navigate).not.toHaveBeenCalled();
    });

    it("should navigate back to /irrigations on cancel", () => {
      component.onCancel();
      expect(mockRouter.navigate).toHaveBeenCalledWith(["/irrigations"]);
    });
  });

  describe("Edit Mode (id = 'irr-123')", () => {
    beforeEach(async () => {
      await setupTestingModule("irr-123");
    });

    it("should initialize component in edit mode and fetch existing draft", () => {
      expect(component.isEditMode()).toBeTrue();
      expect(mockIrrigationService.getIrrigation).toHaveBeenCalledWith("irr-123");
      expect(component.irrigationDetails()).toEqual(sampleDraftEvent);

      expect(component.form.value.farmId).toBe("farm-1");
      expect(component.form.value.farmAreaId).toBe("area-1");
      expect(component.form.value.plantationId).toBe("plant-1");
      expect(component.form.value.cropCycleId).toBe("cycle-1");
      expect(component.form.value.cropCycleStageId).toBe("stage-1");
      expect(component.form.value.irrigationMethodId).toBe("method-1");
      expect(component.form.value.plannedWaterQuantity).toBe(1500);
      expect(component.form.value.plannedWaterUnitId).toBe("unit-l");
      expect(component.form.value.notes).toBe("Initial draft plan");
      expect(mockBreadcrumbService.setEntityName).toHaveBeenCalledWith(
        "irr-123",
        "Sunny Valley Farm — North Plot",
      );
    });

    it("should redirect to detail page if event is in non-editable status (such as InProgress or Completed)", () => {
      const inProgressEvent = { ...sampleDraftEvent, status: "InProgress" as any };
      mockIrrigationService.getIrrigation.and.returnValue(of(inProgressEvent));

      component["loadExistingIrrigation"]("irr-123");

      expect(mockSnackBar.open).toHaveBeenCalledWith(
        'Cannot edit irrigation event in status "InProgress". Only Draft and Scheduled events can be edited.',
        "OK",
        jasmine.any(Object),
      );
      expect(mockRouter.navigate).toHaveBeenCalledWith(["/irrigations", "irr-123"]);
    });

    it("should allow editing when event is in Scheduled status", () => {
      const scheduledEvent = { ...sampleDraftEvent, status: "Scheduled" as any };
      mockIrrigationService.getIrrigation.and.returnValue(of(scheduledEvent));

      component["loadExistingIrrigation"]("irr-123");

      expect(mockRouter.navigate).not.toHaveBeenCalled();
      expect(component.form.value.farmId).toBe("farm-1");
    });

    it("should submit updateDraft with concurrencyToken and navigate to detail page on save", () => {
      component.onSave();

      expect(mockIrrigationService.updateDraft).toHaveBeenCalledWith(
        "irr-123",
        jasmine.objectContaining({
          farmId: "farm-1",
          farmAreaId: "area-1",
          concurrencyToken: "ct-123-abc",
        }),
      );
      expect(mockSnackBar.open).toHaveBeenCalledWith(
        "Irrigation event draft updated successfully",
        "OK",
        jasmine.any(Object),
      );
      expect(mockRouter.navigate).toHaveBeenCalledWith(["/irrigations", "irr-123"]);
    });

    it("should handle error when updateDraft fails", () => {
      mockIrrigationService.updateDraft.and.returnValue(
        throwError(() => ({ status: 409, error: { message: "Concurrency conflict" } })),
      );

      component.onSave();

      expect(component.isSubmitting()).toBeFalse();
      expect(component.errorMessage()).toBe("Concurrency conflict");
      expect(mockSnackBar.open).toHaveBeenCalledWith("Concurrency conflict", "Dismiss", jasmine.any(Object));
    });

    it("should navigate back to /irrigations/irr-123 on cancel in edit mode", () => {
      component.onCancel();
      expect(mockRouter.navigate).toHaveBeenCalledWith(["/irrigations", "irr-123"]);
    });

    it("should handle error when loadExistingIrrigation fails", () => {
      mockIrrigationService.getIrrigation.and.returnValue(
        throwError(() => ({ status: 404, error: { message: "Irrigation event not found" } })),
      );

      component["loadExistingIrrigation"]("irr-not-found");

      expect(component.errorMessage()).toBe("Irrigation event not found");
      expect(mockSnackBar.open).toHaveBeenCalledWith(
        "Irrigation event not found",
        "Dismiss",
        jasmine.any(Object),
      );
    });
  });
});
