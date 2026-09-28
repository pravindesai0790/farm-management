import { provideHttpClient } from "@angular/common/http";
import { provideHttpClientTesting } from "@angular/common/http/testing";
import { ComponentFixture, TestBed, fakeAsync, tick } from "@angular/core/testing";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { ActivatedRoute, Router, provideRouter } from "@angular/router";
import { of } from "rxjs";

import { PermissionService } from "../../../../core/auth/permission.service";
import {
  CropCycle,
  CropCycleLifecycle,
  CropCycleStage,
  Farm,
  FarmArea,
  Plantation,
} from "../../../../core/farm-management/farm-management.models";
import { FarmManagementService } from "../../../../core/farm-management/farm-management.service";
import {
  CreateLaborActivityRequest,
  LaborActivity,
  NamedReference,
  UpdateLaborActivityRequest,
} from "../../models/labor-activity.models";
import { LaborActivityService } from "../../services/labor-activity.service";
import { LaborActivityEditorPageComponent } from "./labor-activity-editor-page.component";

describe("LaborActivityEditorPageComponent", () => {
  let component: LaborActivityEditorPageComponent;
  let fixture: ComponentFixture<LaborActivityEditorPageComponent>;

  let mockActivityService: jasmine.SpyObj<LaborActivityService>;
  let mockFarmService: jasmine.SpyObj<FarmManagementService>;
  let mockPermissionService: jasmine.SpyObj<PermissionService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;
  let router: Router;
  let routerNavigateSpy: jasmine.Spy;
  let activatedRouteParamMapGet: jasmine.Spy;

  const sampleFarm: Farm = {
    id: "farm-1",
    name: "Green Valley Farm",
    description: null,
    ownershipTypeId: "own-1",
    ownershipTypeCode: "OWNED",
    ownershipTypeName: "Owned",
    totalArea: 100,
    areaUnitId: "unit-1",
    areaUnitCode: "HA",
    areaUnitName: "Hectare",
    areaUnitSymbol: "ha",
    addressLine1: null,
    addressLine2: null,
    city: null,
    district: null,
    state: null,
    country: null,
    postalCode: null,
    latitude: null,
    longitude: null,
    isActive: true,
    createdAt: "2026-01-01T00:00:00Z",
    createdBy: "user-1",
    updatedAt: null,
    updatedBy: null,
  };

  const sampleArea: FarmArea = {
    id: "area-1",
    farmId: "farm-1",
    farmName: "Green Valley Farm",
    parentFarmAreaId: null,
    name: "North Sector",
    description: null,
    totalArea: 40,
    areaUnitId: "unit-1",
    areaUnitCode: "HA",
    areaUnitName: "Hectare",
    areaUnitSymbol: "ha",
    isActive: true,
    createdAt: "2026-01-01T00:00:00Z",
    createdBy: "user-1",
    updatedAt: null,
    updatedBy: null,
  };

  const samplePlantation: Plantation = {
    id: "plant-1",
    farmId: "farm-1",
    farmName: "Green Valley Farm",
    farmAreaId: "area-1",
    farmAreaName: "North Sector",
    cropId: "crop-1",
    cropName: "Grapevine",
    varietyId: null,
    varietyCode: null,
    varietyName: null,
    lifecycleTemplateId: "tpl-1",
    lifecycleTemplateName: "Grape Standard",
    plantationName: "Block 1 Grapes",
    allocatedArea: 10,
    areaUnitId: "unit-1",
    areaUnitCode: "HA",
    areaUnitName: "Hectare",
    areaUnitSymbol: "ha",
    plantingDate: "2026-01-15",
    expectedEndDate: null,
    actualEndDate: null,
    status: "ACTIVE",
    endReasonId: null,
    endReasonCode: null,
    endReasonName: null,
    endNotes: null,
    isActive: true,
    createdAt: "2026-01-15T00:00:00Z",
    createdBy: "user-1",
    updatedAt: null,
    updatedBy: null,
  };

  const sampleCycle: CropCycle = {
    id: "cycle-1",
    plantationId: "plant-1",
    plantationName: "Block 1 Grapes",
    farmName: "Green Valley Farm",
    farmAreaName: "North Sector",
    cropId: "crop-1",
    cropName: "Grapevine",
    cycleName: "2026 Harvest Cycle",
    seasonYear: 2026,
    seasonName: "Summer",
    plannedStartDate: "2026-02-01",
    actualStartDate: "2026-02-01",
    expectedEndDate: "2026-08-30",
    status: "IN_PROGRESS",
  };

  const sampleStage: CropCycleStage = {
    id: "stage-1",
    cropCycleId: "cycle-1",
    lifecycleTemplateStageId: "tpl-stage-1",
    stageName: "Budbreak",
    sequenceNumber: 1,
    expectedDurationDays: 14,
    plannedStartDate: "2026-02-01",
    plannedEndDate: "2026-02-15",
    actualStartDate: "2026-02-01",
    actualEndDate: null,
    status: "IN_PROGRESS",
    notes: null,
  };

  const sampleLifecycle: CropCycleLifecycle = {
    cropCycleId: "cycle-1",
    cycleName: "2026 Harvest Cycle",
    overallStatus: "IN_PROGRESS",
    lifecycleTemplateId: "tpl-1",
    lifecycleTemplateName: "Grape Standard",
    currentStageName: "Budbreak",
    currentStageSequence: 1,
    totalStagesCount: 5,
    completedStagesCount: 0,
    progressPercentage: 20,
    hasGeneratedStages: true,
    stages: [sampleStage],
  };

  const sampleType: NamedReference = { id: "type-1", name: "Pruning" };

  const sampleActivity: LaborActivity = {
    id: "act-1",
    activityDate: "2026-09-28",
    farm: { id: "farm-1", name: "Green Valley Farm" },
    farmArea: { id: "area-1", name: "North Sector" },
    plantation: { id: "plant-1", name: "Block 1 Grapes" },
    cropCycle: { id: "cycle-1", name: "2026 Harvest Cycle" },
    cropCycleStage: { id: "stage-1", name: "Budbreak", sequenceNumber: 1 },
    activityType: { id: "type-1", name: "Pruning" },
    status: "COMPLETED",
    description: "Trimming secondary canes",
    createdAt: "2026-09-28T10:00:00Z",
    updatedAt: null,
  };

  beforeEach(async () => {
    mockActivityService = jasmine.createSpyObj("LaborActivityService", [
      "getTypes",
      "get",
      "create",
      "update",
    ]);
    mockFarmService = jasmine.createSpyObj("FarmManagementService", [
      "listFarms",
      "listAreas",
      "listPlantations",
      "listCycles",
      "getCycleLifecycle",
    ]);
    mockPermissionService = jasmine.createSpyObj("PermissionService", ["hasPermission"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);

    activatedRouteParamMapGet = jasmine.createSpy("get").and.returnValue(null);

    mockFarmService.listFarms.and.returnValue(
      of({ items: [sampleFarm], totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
    );
    mockActivityService.getTypes.and.returnValue(of([sampleType]));
    mockFarmService.listAreas.and.returnValue(of([sampleArea]));
    mockFarmService.listPlantations.and.returnValue(
      of({ items: [samplePlantation], totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
    );
    mockFarmService.listCycles.and.returnValue(
      of({ items: [sampleCycle], totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
    );
    mockFarmService.getCycleLifecycle.and.returnValue(of(sampleLifecycle));

    await TestBed.configureTestingModule({
      imports: [LaborActivityEditorPageComponent, NoopAnimationsModule],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: LaborActivityService, useValue: mockActivityService },
        { provide: FarmManagementService, useValue: mockFarmService },
        { provide: PermissionService, useValue: mockPermissionService },
        { provide: MatSnackBar, useValue: mockSnackBar },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: activatedRouteParamMapGet,
              },
            },
          },
        },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    routerNavigateSpy = spyOn(router, "navigateByUrl").and.returnValue(Promise.resolve(true));
  });

  function createComponent(): void {
    fixture = TestBed.createComponent(LaborActivityEditorPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it("initializes form in create mode with obsolete fields omitted", () => {
    createComponent();

    expect(component.isEditMode()).toBeFalse();
    expect(component.farms().length).toBe(1);
    expect(component.activityTypes().length).toBe(1);

    const formVal = component.form.value;
    expect(formVal.workerCount).toBeUndefined();
    expect(formVal.totalWorkingHours).toBeUndefined();
    expect(formVal.costAmount).toBeUndefined();
    expect((component as unknown as Record<string, unknown>)["activityCurrency"]).toBeUndefined();

    expect(component.form.get("farmId")?.value).toBeNull();
    expect(component.form.get("farmAreaId")?.value).toBeNull();
    expect(component.form.get("plantationId")?.value).toBeNull();
    expect(component.form.get("cropCycleId")?.value).toBeNull();
    expect(component.form.get("cropCycleStageId")?.value).toBeNull();
    expect(component.form.get("status")?.value).toBe("COMPLETED");
  });

  it("validates form requiring Farm, Type, Date and Status, while Area/Plantation/Cycle/Stage/Description remain optional", () => {
    createComponent();

    expect(component.form.valid).toBeFalse();

    component.form.patchValue({
      farmId: "farm-1",
      laborActivityTypeId: "type-1",
      activityDate: new Date("2026-09-28"),
      status: "COMPLETED",
    });

    expect(component.form.valid).toBeTrue();
  });

  it("clears all descendants when Farm changes and loads areas and farm plantations", () => {
    createComponent();

    component.form.patchValue({
      farmId: "farm-1",
      farmAreaId: "area-1",
      plantationId: "plant-1",
      cropCycleId: "cycle-1",
      cropCycleStageId: "stage-1",
    });

    component.onFarmChange("farm-2");

    expect(component.form.get("farmAreaId")?.value).toBeNull();
    expect(component.form.get("plantationId")?.value).toBeNull();
    expect(component.form.get("cropCycleId")?.value).toBeNull();
    expect(component.form.get("cropCycleStageId")?.value).toBeNull();

    expect(mockFarmService.listAreas).toHaveBeenCalledWith("farm-2", true);
    expect(mockFarmService.listPlantations).toHaveBeenCalledWith(1, 100, "farm-2");
  });

  it("clears descendants when Area changes and reloads plantations filtered by area", () => {
    createComponent();

    component.form.patchValue({
      farmId: "farm-1",
      plantationId: "plant-1",
      cropCycleId: "cycle-1",
      cropCycleStageId: "stage-1",
    });

    component.onAreaChange("area-1");

    expect(component.form.get("plantationId")?.value).toBeNull();
    expect(component.form.get("cropCycleId")?.value).toBeNull();
    expect(component.form.get("cropCycleStageId")?.value).toBeNull();

    expect(mockFarmService.listPlantations).toHaveBeenCalledWith(1, 100, "farm-1", "area-1");
  });

  it("clears descendants when Plantation changes, auto-infers Area if missing, and loads crop cycles", () => {
    createComponent();

    component.form.patchValue({
      farmId: "farm-1",
      farmAreaId: null,
      cropCycleId: "cycle-1",
      cropCycleStageId: "stage-1",
    });
    component.plantations.set([samplePlantation]);

    component.onPlantationChange("plant-1");

    expect(component.form.get("farmAreaId")?.value).toBe("area-1");
    expect(component.form.get("cropCycleId")?.value).toBeNull();
    expect(component.form.get("cropCycleStageId")?.value).toBeNull();

    expect(mockFarmService.listCycles).toHaveBeenCalledWith(1, 100, "farm-1", "area-1", "plant-1");
  });

  it("loads lifecycle stages when Cycle changes and clears Stage control", () => {
    createComponent();

    component.form.patchValue({
      cropCycleId: "cycle-1",
      cropCycleStageId: "old-stage",
    });

    component.onCropCycleChange("cycle-1");

    expect(component.form.get("cropCycleStageId")?.value).toBeNull();
    expect(mockFarmService.getCycleLifecycle).toHaveBeenCalledWith("cycle-1");
    expect(component.cropCycleStages().length).toBe(1);
    expect(component.cropCycleStages()[0].id).toBe("stage-1");
  });

  it("clears stages and disables Stage control when Cycle is empty/null", () => {
    createComponent();

    component.cropCycleStages.set([sampleStage]);
    component.form.patchValue({ cropCycleStageId: "stage-1" });

    component.onCropCycleChange(null);

    expect(component.form.get("cropCycleStageId")?.value).toBeNull();
    expect(component.cropCycleStages().length).toBe(0);
  });

  it("initializes form in edit mode, fetching existing activity and saved stage", fakeAsync(() => {
    activatedRouteParamMapGet.and.returnValue("act-1");
    mockActivityService.get.and.returnValue(of(sampleActivity));

    createComponent();
    tick();

    expect(component.isEditMode()).toBeTrue();
    expect(mockActivityService.get).toHaveBeenCalledWith("act-1");
    expect(mockFarmService.getCycleLifecycle).toHaveBeenCalledWith("cycle-1");

    expect(component.form.get("farmId")?.value).toBe("farm-1");
    expect(component.form.get("farmAreaId")?.value).toBe("area-1");
    expect(component.form.get("plantationId")?.value).toBe("plant-1");
    expect(component.form.get("cropCycleId")?.value).toBe("cycle-1");
    expect(component.form.get("cropCycleStageId")?.value).toBe("stage-1");
    expect(component.form.get("status")?.value).toBe("COMPLETED");
  }));

  it("disables form and displays cancelled notice when editing a cancelled activity", fakeAsync(() => {
    activatedRouteParamMapGet.and.returnValue("act-1");
    const cancelledActivity: LaborActivity = {
      ...sampleActivity,
      status: "CANCELLED",
      cancellationReason: "Rainstorm",
    };
    mockActivityService.get.and.returnValue(of(cancelledActivity));

    createComponent();
    tick();

    expect(component.isCancelled()).toBeTrue();
    expect(component.form.disabled).toBeTrue();
  }));

  it("submits create request payload without obsolete fields and navigates to list page", () => {
    createComponent();

    mockActivityService.create.and.returnValue(of(sampleActivity));

    component.form.patchValue({
      activityDate: new Date("2026-09-28"),
      farmId: "farm-1",
      farmAreaId: "area-1",
      plantationId: "plant-1",
      cropCycleId: "cycle-1",
      cropCycleStageId: "stage-1",
      laborActivityTypeId: "type-1",
      status: "COMPLETED",
      description: "Pruning canopy",
    });

    component.submit();

    const expectedPayload: CreateLaborActivityRequest = {
      activityDate: "2026-09-28",
      farmId: "farm-1",
      farmAreaId: "area-1",
      plantationId: "plant-1",
      cropCycleId: "cycle-1",
      cropCycleStageId: "stage-1",
      laborActivityTypeId: "type-1",
      status: "COMPLETED",
      description: "Pruning canopy",
    };

    expect(mockActivityService.create).toHaveBeenCalledWith(expectedPayload);
    expect(mockSnackBar.open).toHaveBeenCalledWith(
      "Labor activity recorded successfully.",
      "Dismiss",
      { duration: 4000 },
    );
    expect(routerNavigateSpy).toHaveBeenCalledWith("/activities/labor-activities");
  });

  it("submits update request payload when editing an activity", fakeAsync(() => {
    activatedRouteParamMapGet.and.returnValue("act-1");
    mockActivityService.get.and.returnValue(of(sampleActivity));
    mockActivityService.update.and.returnValue(of(sampleActivity));

    createComponent();
    tick();

    component.submit();

    const expectedUpdatePayload: UpdateLaborActivityRequest = {
      activityDate: "2026-09-28",
      farmId: "farm-1",
      farmAreaId: "area-1",
      plantationId: "plant-1",
      cropCycleId: "cycle-1",
      cropCycleStageId: "stage-1",
      laborActivityTypeId: "type-1",
      status: "COMPLETED",
      description: "Trimming secondary canes",
    };

    expect(mockActivityService.update).toHaveBeenCalledWith("act-1", expectedUpdatePayload);
    expect(mockSnackBar.open).toHaveBeenCalledWith(
      "Labor activity updated successfully.",
      "Dismiss",
      { duration: 4000 },
    );
  }));
});
