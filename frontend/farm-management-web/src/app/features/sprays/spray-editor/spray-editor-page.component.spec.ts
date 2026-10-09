import { ComponentFixture, TestBed } from "@angular/core/testing";
import { provideNativeDateAdapter } from "@angular/material/core";
import { MatDialog } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { ActivatedRoute, Router } from "@angular/router";
import { of } from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import { FarmManagementService } from "../../../core/farm-management/farm-management.service";
import {
  ApplicationMethodResponse,
  SprayDetailsResponse,
  SprayProductLookupResponse,
  TargetResponse,
} from "../../../core/sprays/spray.models";
import { SprayService } from "../../../core/sprays/spray.service";
import { SprayEditorPageComponent } from "./spray-editor-page.component";

describe("SprayEditorPageComponent", () => {
  let component: SprayEditorPageComponent;
  let fixture: ComponentFixture<SprayEditorPageComponent>;

  let mockSprayService: jasmine.SpyObj<SprayService>;
  let mockFarmService: jasmine.SpyObj<FarmManagementService>;
  let mockPermissionService: jasmine.SpyObj<PermissionService>;
  let mockDialog: jasmine.SpyObj<MatDialog>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;
  let mockRouter: jasmine.SpyObj<Router>;

  const sampleFarms: any = {
    items: [{ id: "farm-1", name: "Green Valley Farm" }],
    totalCount: 1,
    page: 1,
    pageSize: 100,
    totalPages: 1,
  };

  const sampleTargets: TargetResponse[] = [
    {
      id: "target-1",
      code: "POWD_MILD",
      targetType: "Disease",
      name: "Powdery Mildew",
      description: null,
      displayOrder: 1,
      isSystem: true,
      isActive: true,
    },
  ];

  const sampleAppMethods: ApplicationMethodResponse[] = [
    {
      id: "method-1",
      code: "FOLIAR",
      name: "Foliar Spray",
      description: null,
      displayOrder: 1,
      isSystem: true,
      isActive: true,
    },
  ];

  const sampleAreaUnits: any[] = [
    { id: "u-ha", code: "HA", name: "Hectares", symbol: "ha", unitCategory: "Area" },
  ];

  const sampleVolumeUnits: any[] = [
    { id: "u-l", code: "L", name: "Liters", symbol: "L", unitCategory: "Volume" },
  ];

  const sampleProducts: SprayProductLookupResponse[] = [
    {
      inventoryItemId: "prod-1",
      name: "Copper Oxychloride 50 WP",
      sku: "COP-50",
      stockUnitId: "u-kg",
      stockUnitName: "Kilograms",
      stockUnitCode: "KG",
      stockUnitSymbol: "kg",
      productTypeId: "pt-1",
      productTypeCode: "FUNG",
      productTypeName: "Fungicide",
      activeIngredient: "Copper Oxychloride 50%",
      manufacturer: "AgroChem",
      plantProtectionProductId: "ppp-1",
    },
    {
      inventoryItemId: "prod-2",
      name: "Sulphur 80 WDG",
      sku: "SUL-80",
      stockUnitId: "u-kg",
      stockUnitName: "Kilograms",
      stockUnitCode: "KG",
      stockUnitSymbol: "kg",
      productTypeId: "pt-1",
      productTypeCode: "FUNG",
      productTypeName: "Fungicide",
      activeIngredient: "Sulphur 80%",
      manufacturer: "AgroChem",
      plantProtectionProductId: "ppp-2",
    },
  ];

  const sampleCreatedSpray: SprayDetailsResponse = {
    id: "sp-new-123",
    referenceNumber: "SP-2026-0050",
    organizationId: "org-1",
    farmId: "farm-1",
    farmName: "Green Valley Farm",
    farmAreaId: null,
    farmAreaName: null,
    plantationId: null,
    plantationName: null,
    cropCycleId: null,
    cropCycleName: null,
    cropCycleStageId: null,
    cropCycleStageName: null,
    status: "Draft",
    statusName: "Draft",
    isOverdue: false,
    plannedDate: "2026-10-20",
    scheduledDateTime: null,
    actualApplicationDateTime: null,
    plannedArea: 4.5,
    plannedAreaUnitId: "u-ha",
    plannedAreaUnitName: "Hectares",
    actualTreatedArea: null,
    actualTreatedAreaUnitId: null,
    actualTreatedAreaUnitName: null,
    waterQuantity: 600,
    waterUnitId: "u-l",
    waterUnitName: "Liters",
    targetId: "target-1",
    targetName: "Powdery Mildew",
    targetType: "Disease",
    applicationMethodId: "method-1",
    applicationMethodName: "Foliar Spray",
    purposeReason: "Preventative",
    cancellationReason: null,
    products: [],
    createdAt: "2026-10-09T10:00:00Z",
    createdBy: "user-1",
    updatedAt: null,
    updatedBy: null,
  };

  beforeEach(async () => {
    mockSprayService = jasmine.createSpyObj("SprayService", [
      "getTargets",
      "getApplicationMethods",
      "getProductLookup",
      "getSpray",
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
    mockDialog = jasmine.createSpyObj("MatDialog", ["open"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);
    mockRouter = jasmine.createSpyObj("Router", ["navigate"]);

    mockPermissionService.has.and.returnValue(true);
    mockFarmService.listFarms.and.returnValue(of(sampleFarms));
    mockFarmService.listAreas.and.returnValue(of([]));
    mockFarmService.listPlantations.and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 100, totalPages: 0 } as any));
    mockFarmService.listCycles.and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 100, totalPages: 0 } as any));
    mockFarmService.listUnits.and.callFake((category?: string | null) => {
      if (category === "Area") return of(sampleAreaUnits);
      if (category === "Volume") return of(sampleVolumeUnits);
      return of([]);
    });
    mockSprayService.getTargets.and.returnValue(of(sampleTargets));
    mockSprayService.getApplicationMethods.and.returnValue(of(sampleAppMethods));
    mockSprayService.getProductLookup.and.returnValue(of(sampleProducts));
    mockSprayService.getCropCycleStages.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [SprayEditorPageComponent, NoopAnimationsModule],
      providers: [
        provideNativeDateAdapter(),
        { provide: SprayService, useValue: mockSprayService },
        { provide: FarmManagementService, useValue: mockFarmService },
        { provide: PermissionService, useValue: mockPermissionService },
        { provide: MatDialog, useValue: mockDialog },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: Router, useValue: mockRouter },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: { get: () => null } },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SprayEditorPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create in create mode and load initial lookup data", () => {
    expect(component).toBeTruthy();
    expect(component.isEditMode()).toBeFalse();
    expect(component.farms().length).toBe(1);
    expect(component.targets().length).toBe(1);
    expect(component.applicationMethods().length).toBe(1);
    expect(component.availableProducts().length).toBe(2);
    expect(component.productsArray.length).toBe(0);
  });

  it("should validate that farmId is required", () => {
    expect(component.form.valid).toBeFalse();
    expect(component.form.get("farmId")?.hasError("required")).toBeTrue();

    component.form.patchValue({ farmId: "farm-1" });
    expect(component.form.valid).toBeTrue();
  });

  it("should handle cascading when farm changes", () => {
    mockFarmService.listAreas.and.returnValue(of([]));
    mockFarmService.listPlantations.and.returnValue(
      of({ items: [], totalCount: 0, page: 1, pageSize: 100, totalPages: 0 } as any),
    );

    component.onFarmChange("farm-1");

    expect(mockFarmService.listAreas).toHaveBeenCalledWith("farm-1");
    expect(mockFarmService.listPlantations).toHaveBeenCalledWith(1, 100, "farm-1");
    expect(component.form.value.farmAreaId).toBe("");
    expect(component.form.value.plantationId).toBe("");
  });

  it("should handle cascading when plantation changes", () => {
    mockFarmService.listCycles.and.returnValue(
      of({ items: [], totalCount: 0, page: 1, pageSize: 100, totalPages: 0 } as any),
    );

    component.onPlantationChange("plant-1");

    expect(mockFarmService.listCycles).toHaveBeenCalledWith(1, 100, undefined, undefined, "plant-1");
    expect(component.form.value.cropCycleId).toBe("");
  });

  it("should handle cascading when crop cycle changes", () => {
    mockSprayService.getCropCycleStages.and.returnValue(of([]));

    component.onCropCycleChange("cycle-1");

    expect(mockSprayService.getCropCycleStages).toHaveBeenCalledWith("cycle-1");
    expect(component.form.value.cropCycleStageId).toBe("");
  });

  it("should add, edit, and remove product rows in FormArray", () => {
    expect(component.productsArray.length).toBe(0);

    component.addProduct({
      inventoryItemId: "prod-1",
      plannedQuantity: 2.5,
      dosage: "1 L/ha",
    });
    expect(component.productsArray.length).toBe(1);
    expect(component.productsArray.at(0).value.inventoryItemId).toBe("prod-1");
    expect(component.productsArray.at(0).value.plannedQuantity).toBe(2.5);

    component.addProduct({
      inventoryItemId: "prod-2",
      plannedQuantity: 1.0,
      dosage: "500 g/ha",
    });
    expect(component.productsArray.length).toBe(2);

    component.removeProduct(0);
    expect(component.productsArray.length).toBe(1);
    expect(component.productsArray.at(0).value.inventoryItemId).toBe("prod-2");
  });

  it("should detect duplicate products", () => {
    component.addProduct({ inventoryItemId: "prod-1" });
    component.addProduct({ inventoryItemId: "prod-1" });

    expect(component.hasDuplicateProducts()).toBeTrue();
    expect(component.isProductOptionDisabled("prod-1", 1)).toBeTrue();

    component.productsArray.at(1).patchValue({ inventoryItemId: "prod-2" });
    expect(component.hasDuplicateProducts()).toBeFalse();
  });

  it("should call createDraft and navigate on saveDraft in create mode", () => {
    mockSprayService.createDraft.and.returnValue(of(sampleCreatedSpray));

    component.form.patchValue({
      farmId: "farm-1",
      plannedDate: "2026-10-20",
      plannedArea: 4.5,
      plannedAreaUnitId: "u-ha",
      waterQuantity: 600,
      waterUnitId: "u-l",
      targetId: "target-1",
      applicationMethodId: "method-1",
      purposeReason: "Preventative",
    });
    component.addProduct({
      inventoryItemId: "prod-1",
      plannedQuantity: 2.5,
      dosage: "1 L/ha",
    });

    component.onSaveDraft();

    expect(mockSprayService.createDraft).toHaveBeenCalledWith(
      jasmine.objectContaining({
        farmId: "farm-1",
        plannedDate: "2026-10-20",
        plannedArea: 4.5,
        waterQuantity: 600,
        purposeReason: "Preventative",
        products: [
          jasmine.objectContaining({
            inventoryItemId: "prod-1",
            plannedQuantity: 2.5,
            dosage: "1 L/ha",
          }),
        ],
      }),
    );
    expect(mockRouter.navigate).toHaveBeenCalledWith(["/sprays", "sp-new-123"]);
  });

  it("should navigate back to /sprays on cancel in create mode", () => {
    component.onCancel();
    expect(mockRouter.navigate).toHaveBeenCalledWith(["/sprays"]);
  });

  it("should allow saving Farm-only draft when sub-hierarchy is omitted", () => {
    mockSprayService.createDraft.and.returnValue(of(sampleCreatedSpray));

    component.form.patchValue({
      farmId: "farm-1",
      farmAreaId: "",
      plantationId: "",
      cropCycleId: "",
      cropCycleStageId: "",
      plannedDate: "2026-10-25",
    });
    component.addProduct({
      inventoryItemId: "prod-1",
      plannedQuantity: 5.0,
    });

    component.onSaveDraft();

    expect(mockSprayService.createDraft).toHaveBeenCalledWith(
      jasmine.objectContaining({
        farmId: "farm-1",
        farmAreaId: null,
        plantationId: null,
        cropCycleId: null,
        cropCycleStageId: null,
        plannedDate: "2026-10-25",
      }),
    );
  });

  it("should reset downstream hierarchy selections when parent level changes", () => {
    component.form.patchValue({
      farmId: "farm-1",
      farmAreaId: "area-1",
      plantationId: "plant-1",
      cropCycleId: "cycle-1",
      cropCycleStageId: "stage-1",
    });

    component.onPlantationChange("plant-2");
    expect(component.form.value.cropCycleId).toBe("");
    expect(component.form.value.cropCycleStageId).toBe("");

    component.form.patchValue({
      plantationId: "plant-2",
      cropCycleId: "cycle-2",
      cropCycleStageId: "stage-2",
    });
    component.onAreaChange("area-2");
    expect(component.form.value.plantationId).toBe("");
    expect(component.form.value.cropCycleId).toBe("");
    expect(component.form.value.cropCycleStageId).toBe("");
  });
});
