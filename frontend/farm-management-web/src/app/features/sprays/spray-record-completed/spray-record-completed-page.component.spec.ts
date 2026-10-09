import { ComponentFixture, TestBed } from "@angular/core/testing";
import { MatDialog } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { ActivatedRoute, Router } from "@angular/router";
import { of, throwError } from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import { FarmManagementService } from "../../../core/farm-management/farm-management.service";
import {
  ApplicationMethodResponse,
  RecordCompletedSprayRequest,
  SprayDetailsResponse,
  SprayProductLookupResponse,
  SprayStorageLocationLookupResponse,
  TargetResponse,
} from "../../../core/sprays/spray.models";
import { SprayService } from "../../../core/sprays/spray.service";
import { SprayRecordCompletedPageComponent } from "./spray-record-completed-page.component";

describe("SprayRecordCompletedPageComponent", () => {
  let component: SprayRecordCompletedPageComponent;
  let fixture: ComponentFixture<SprayRecordCompletedPageComponent>;

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

  const sampleStorageLocations: SprayStorageLocationLookupResponse[] = [
    {
      storageLocationId: "loc-1",
      storageLocationName: "Chemical Shed A",
      currentStock: 45.0,
      hasStock: true,
      stockUnitId: "u-kg",
      stockUnitName: "Kilograms",
    },
  ];

  const sampleCompletedSpray: SprayDetailsResponse = {
    id: "sp-completed-123",
    referenceNumber: "SP-2026-0099",
    organizationId: "org-1",
    farmId: "farm-1",
    farmName: "Green Valley Farm",
    farmAreaId: "area-1",
    farmAreaName: "Block 1",
    plantationId: "plant-1",
    plantationName: "Cabernet Sauvignon",
    cropCycleId: "cycle-1",
    cropCycleName: "2026 Season",
    cropCycleStageId: "stage-1",
    cropCycleStageName: "Flowering",
    status: "Completed",
    statusName: "Completed",
    isOverdue: false,
    plannedDate: null,
    scheduledDateTime: null,
    actualApplicationDateTime: "2026-10-09T08:00:00Z",
    plannedArea: null,
    plannedAreaUnitId: null,
    plannedAreaUnitName: null,
    actualTreatedArea: 2.5,
    actualTreatedAreaUnitId: "u-ha",
    actualTreatedAreaUnitName: "Hectares",
    waterQuantity: 400,
    waterUnitId: "u-l",
    waterUnitName: "Liters",
    targetId: "target-1",
    targetName: "Powdery Mildew",
    targetType: "Disease",
    applicationMethodId: "method-1",
    applicationMethodName: "Foliar Spray",
    purposeReason: "Direct completed treatment",
    cancellationReason: null,
    products: [
      {
        id: "p-1",
        inventoryItemId: "prod-1",
        inventoryItemName: "Copper Oxychloride 50 WP",
        inventoryItemSku: "COP-50",
        stockUnitId: "u-kg",
        stockUnitName: "Kilograms",
        stockUnitSymbol: "kg",
        storageLocationId: "loc-1",
        storageLocationName: "Chemical Shed A",
        plannedQuantity: null,
        actualQuantity: 5.0,
        dosage: "2 kg/ha",
      },
    ],
    createdAt: "2026-10-09T08:30:00Z",
    createdBy: "user-1",
    updatedAt: null,
    updatedBy: null,
  };

  beforeEach(async () => {
    mockSprayService = jasmine.createSpyObj("SprayService", [
      "getTargets",
      "getApplicationMethods",
      "getProductLookup",
      "getStorageLocationLookup",
      "recordCompleted",
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
    mockSprayService.getTargets.and.returnValue(of(sampleTargets));
    mockSprayService.getApplicationMethods.and.returnValue(of(sampleAppMethods));
    mockSprayService.getProductLookup.and.returnValue(of(sampleProducts));
    mockSprayService.getStorageLocationLookup.and.returnValue(of(sampleStorageLocations));
    mockSprayService.recordCompleted.and.returnValue(of(sampleCompletedSpray));
    mockSprayService.getCropCycleStages.and.returnValue(of([]));

    mockFarmService.listFarms.and.returnValue(of(sampleFarms));
    mockFarmService.listAreas.and.returnValue(of([]));
    mockFarmService.listPlantations.and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 100, totalPages: 1 } as any));
    mockFarmService.listCycles.and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 100, totalPages: 1 } as any));
    mockFarmService.listUnits.and.callFake((category?: string) => {
      if (category === "Area") return of(sampleAreaUnits);
      if (category === "Volume") return of(sampleVolumeUnits);
      return of([]);
    });

    await TestBed.configureTestingModule({
      imports: [SprayRecordCompletedPageComponent, NoopAnimationsModule],
      providers: [
        { provide: SprayService, useValue: mockSprayService },
        { provide: FarmManagementService, useValue: mockFarmService },
        { provide: PermissionService, useValue: mockPermissionService },
        { provide: MatDialog, useValue: mockDialog },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: Router, useValue: mockRouter },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: { get: () => null },
              queryParamMap: { get: () => null },
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SprayRecordCompletedPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("creates component and loads initial master lookups", () => {
    expect(component).toBeTruthy();
    expect(component.farms().length).toBe(1);
    expect(component.targets().length).toBe(1);
    expect(component.applicationMethods().length).toBe(1);
    expect(component.areaUnits().length).toBe(1);
    expect(component.volumeUnits().length).toBe(1);
    expect(component.availableProducts().length).toBe(2);
  });

  it("initializes with 1 empty product row and valid application datetime", () => {
    expect(component.productsArray.length).toBe(1);
    const row = component.productsArray.at(0);
    expect(row.value.inventoryItemId).toBe("");
    expect(row.value.storageLocationId).toBe("");
    expect(row.value.actualQuantity).toBeNull();
    expect(component.form.get("actualApplicationDateTime")?.value).toBeTruthy();
  });

  it("validates that farm is required", () => {
    component.form.patchValue({ farmId: "" });
    expect(component.form.get("farmId")?.valid).toBeFalse();
    expect(component.form.get("farmId")?.hasError("required")).toBeTrue();
  });

  it("validates that application date/time cannot be in the future", () => {
    const futureDate = new Date(Date.now() + 86400000 * 2).toISOString();
    component.form.patchValue({ actualApplicationDateTime: futureDate });
    expect(component.form.get("actualApplicationDateTime")?.hasError("futureDate")).toBeTrue();

    const pastDate = new Date(Date.now() - 3600000).toISOString();
    component.form.patchValue({ actualApplicationDateTime: pastDate });
    expect(component.form.get("actualApplicationDateTime")?.hasError("futureDate")).toBeFalsy();
  });

  it("handles farm change by resetting child hierarchy and clearing storage locations", () => {
    mockFarmService.listAreas.and.returnValue(of([{ id: "area-1", name: "Block 1" } as any]));
    mockFarmService.listPlantations.and.returnValue(
      of({ items: [{ id: "plant-1", plantationName: "Plot A" }], totalCount: 1, page: 1, pageSize: 100, totalPages: 1 } as any),
    );

    component.form.patchValue({
      farmAreaId: "old-area",
      plantationId: "old-plant",
      cropCycleId: "old-cycle",
      cropCycleStageId: "old-stage",
    });

    component.onFarmChange("farm-1");

    expect(component.form.value.farmAreaId).toBe("");
    expect(component.form.value.plantationId).toBe("");
    expect(component.form.value.cropCycleId).toBe("");
    expect(component.form.value.cropCycleStageId).toBe("");
    expect(mockFarmService.listAreas).toHaveBeenCalledWith("farm-1");
  });

  it("allows adding and removing product rows", () => {
    expect(component.productsArray.length).toBe(1);
    component.addProduct();
    expect(component.productsArray.length).toBe(2);

    component.removeProduct(1);
    expect(component.productsArray.length).toBe(1);

    // Cannot remove when only 1 row remains
    component.removeProduct(0);
    expect(component.productsArray.length).toBe(1);
  });

  it("loads storage locations when farm is selected and product is chosen", () => {
    component.form.patchValue({ farmId: "farm-1" });
    const row = component.productsArray.at(0);
    row.patchValue({ inventoryItemId: "prod-1" });

    component.onProductChange(0, "prod-1");

    expect(mockSprayService.getStorageLocationLookup).toHaveBeenCalledWith("farm-1", "prod-1");
    const locs = component.getStorageLocations("prod-1");
    expect(locs.length).toBe(1);
    expect(locs[0].storageLocationId).toBe("loc-1");
  });

  it("disables duplicate product options across different rows", () => {
    component.addProduct();
    component.productsArray.at(0).patchValue({ inventoryItemId: "prod-1" });

    // In row 1, prod-1 should be disabled
    expect(component.isProductOptionDisabled("prod-1", 1)).toBeTrue();
    // In row 1, prod-2 should be enabled
    expect(component.isProductOptionDisabled("prod-2", 1)).toBeFalse();
    // In row 0, prod-1 should NOT be disabled for itself
    expect(component.isProductOptionDisabled("prod-1", 0)).toBeFalse();
  });

  it("validates paired treated area and area unit on submit", () => {
    component.form.patchValue({
      farmId: "farm-1",
      actualTreatedArea: 2.5,
      actualTreatedAreaUnitId: "", // missing unit!
    });
    component.onSubmit();
    expect(component.errorMessage()).toContain("area unit is required");

    component.form.patchValue({
      actualTreatedArea: null,
      actualTreatedAreaUnitId: "u-ha", // unit without area!
    });
    component.onSubmit();
    expect(component.errorMessage()).toContain("Actual treated area is required");
  });

  it("validates paired water volume and water unit on submit", () => {
    component.form.patchValue({
      farmId: "farm-1",
      waterQuantity: 400,
      waterUnitId: "", // missing unit!
    });
    component.onSubmit();
    expect(component.errorMessage()).toContain("water unit is required");

    component.form.patchValue({
      waterQuantity: null,
      waterUnitId: "u-l", // unit without quantity!
    });
    component.onSubmit();
    expect(component.errorMessage()).toContain("Water quantity is required");
  });

  it("shows confirmation dialog on valid submit and does not proceed if cancelled", () => {
    mockDialog.open.and.returnValue({
      afterClosed: () => of(false),
    } as any);

    component.form.patchValue({
      farmId: "farm-1",
      actualApplicationDateTime: new Date().toISOString(),
    });
    component.productsArray.at(0).patchValue({
      inventoryItemId: "prod-1",
      storageLocationId: "loc-1",
      actualQuantity: 5.0,
      dosage: "2 kg/ha",
    });

    component.onSubmit();

    expect(mockDialog.open).toHaveBeenCalled();
    expect(mockSprayService.recordCompleted).not.toHaveBeenCalled();
  });

  it("submits valid completed spray when user confirms dialog, shows snackbar, and navigates", () => {
    mockDialog.open.and.returnValue({
      afterClosed: () => of(true),
    } as any);

    component.form.patchValue({
      farmId: "farm-1",
      actualApplicationDateTime: new Date().toISOString(),
      actualTreatedArea: 2.5,
      actualTreatedAreaUnitId: "u-ha",
      waterQuantity: 400,
      waterUnitId: "u-l",
      targetId: "target-1",
      applicationMethodId: "method-1",
      purposeReason: "Direct completed treatment",
    });

    component.productsArray.at(0).patchValue({
      inventoryItemId: "prod-1",
      storageLocationId: "loc-1",
      actualQuantity: 5.0,
      dosage: "2 kg/ha",
    });

    component.onSubmit();

    expect(mockDialog.open).toHaveBeenCalled();
    expect(mockSprayService.recordCompleted).toHaveBeenCalled();
    const callArgs = mockSprayService.recordCompleted.calls.mostRecent().args[0] as RecordCompletedSprayRequest;
    expect(callArgs.farmId).toBe("farm-1");
    expect(callArgs.products.length).toBe(1);
    expect(callArgs.products[0].inventoryItemId).toBe("prod-1");
    expect(callArgs.products[0].storageLocationId).toBe("loc-1");
    expect(callArgs.products[0].actualQuantity).toBe(5.0);

    expect(mockSnackBar.open).toHaveBeenCalledWith(
      "Spray application recorded and completed successfully.",
      "Dismiss",
      jasmine.any(Object),
    );
    expect(mockRouter.navigate).toHaveBeenCalledWith(["/sprays", "sp-completed-123"]);
  });

  it("handles submit error gracefully by showing error message", () => {
    mockDialog.open.and.returnValue({
      afterClosed: () => of(true),
    } as any);

    mockSprayService.recordCompleted.and.returnValue(
      throwError(() => ({
        status: 400,
        error: { message: "Insufficient stock in storage location" },
      })),
    );

    component.form.patchValue({
      farmId: "farm-1",
      actualApplicationDateTime: new Date().toISOString(),
    });

    component.productsArray.at(0).patchValue({
      inventoryItemId: "prod-1",
      storageLocationId: "loc-1",
      actualQuantity: 100.0,
    });

    component.onSubmit();

    expect(component.errorMessage()).toContain("Insufficient stock");
    expect(component.isSubmitting()).toBeFalse();
    expect(mockRouter.navigate).not.toHaveBeenCalled();
  });

  it("navigates to /sprays on cancel", () => {
    component.onCancel();
    expect(mockRouter.navigate).toHaveBeenCalledWith(["/sprays"]);
  });
});
