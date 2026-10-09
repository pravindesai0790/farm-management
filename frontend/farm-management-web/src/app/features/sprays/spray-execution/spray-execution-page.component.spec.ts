import { ComponentFixture, TestBed } from "@angular/core/testing";
import { provideNativeDateAdapter } from "@angular/material/core";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from "@angular/router";
import { MatDialog } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { of } from "rxjs";

import { BreadcrumbService } from "../../../core/breadcrumb/breadcrumb.service";
import { PermissionService } from "../../../core/auth/permission.service";
import { FarmManagementService } from "../../../core/farm-management/farm-management.service";
import {
  ApplicationMethodResponse,
  SprayDetailsResponse,
  SprayStorageLocationLookupResponse,
  TargetResponse,
} from "../../../core/sprays/spray.models";
import { SprayService } from "../../../core/sprays/spray.service";
import { SprayExecutionPageComponent } from "./spray-execution-page.component";

describe("SprayExecutionPageComponent", () => {
  let component: SprayExecutionPageComponent;
  let fixture: ComponentFixture<SprayExecutionPageComponent>;
  let mockSprayService: jasmine.SpyObj<SprayService>;
  let mockFarmService: jasmine.SpyObj<FarmManagementService>;
  let mockPermissionService: jasmine.SpyObj<PermissionService>;
  let mockBreadcrumbService: jasmine.SpyObj<BreadcrumbService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockDialog: jasmine.SpyObj<MatDialog>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const sampleSpray: SprayDetailsResponse = {
    id: "sp-1",
    referenceNumber: "SP-2026-0001",
    organizationId: "org-1",
    farmId: "farm-1",
    farmName: "Sunrise Vineyard",
    farmAreaId: "area-1",
    farmAreaName: "North Block",
    plantationId: "plant-1",
    plantationName: "Cabernet Block",
    cropCycleId: "cycle-1",
    cropCycleName: "2026 Season",
    cropCycleStageId: "stage-1",
    cropCycleStageName: "Flowering",
    status: "Scheduled",
    statusName: "Scheduled",
    isOverdue: false,
    plannedDate: "2026-10-15",
    scheduledDateTime: "2026-10-15T08:00:00Z",
    actualApplicationDateTime: null,
    plannedArea: 5.0,
    plannedAreaUnitId: "u-ha",
    plannedAreaUnitName: "Hectares",
    actualTreatedArea: null,
    actualTreatedAreaUnitId: null,
    actualTreatedAreaUnitName: null,
    waterQuantity: 500,
    waterUnitId: "u-l",
    waterUnitName: "Liters",
    targetId: "t-1",
    targetName: "Powdery Mildew",
    targetType: "Disease",
    applicationMethodId: "m-1",
    applicationMethodName: "Air Blast",
    purposeReason: "Preventive treatment",
    cancellationReason: null,
    products: [
      {
        id: "p-1",
        inventoryItemId: "item-1",
        inventoryItemName: "Sulfur 80WG",
        inventoryItemSku: "SULF-80",
        stockUnitId: "u-kg",
        stockUnitName: "Kilograms",
        stockUnitSymbol: "kg",
        storageLocationId: null,
        storageLocationName: null,
        plannedQuantity: 10.0,
        actualQuantity: null,
        dosage: "2 kg/ha",
      },
    ],
    createdAt: "2026-10-09T08:00:00Z",
    createdBy: "admin@farm.local",
    updatedAt: null,
    updatedBy: null,
  };

  const sampleTargets: TargetResponse[] = [
    {
      id: "t-1",
      code: "POWD_MILD",
      name: "Powdery Mildew",
      targetType: "Disease",
      description: null,
      displayOrder: 1,
      isSystem: true,
      isActive: true,
    },
  ];

  const sampleAppMethods: ApplicationMethodResponse[] = [
    {
      id: "m-1",
      code: "AIR_BLAST",
      name: "Air Blast",
      description: null,
      displayOrder: 1,
      isSystem: true,
      isActive: true,
    },
  ];

  const sampleStorageLocations: SprayStorageLocationLookupResponse[] = [
    {
      storageLocationId: "loc-1",
      storageLocationName: "Main Warehouse",
      currentStock: 25.0,
      hasStock: true,
      stockUnitId: "u-kg",
      stockUnitName: "Kilograms",
    },
  ];

  function setupTestBed(urlSegments: { path: string }[]) {
    mockSprayService = jasmine.createSpyObj("SprayService", [
      "getTargets",
      "getApplicationMethods",
      "getProductLookup",
      "getSpray",
      "getStorageLocationLookup",
      "startSpray",
      "saveExecution",
      "completeSpray",
    ]);

    mockFarmService = jasmine.createSpyObj("FarmManagementService", ["listUnits"]);
    mockPermissionService = jasmine.createSpyObj("PermissionService", ["has"]);
    mockBreadcrumbService = jasmine.createSpyObj("BreadcrumbService", ["setEntityName"]);
    mockRouter = jasmine.createSpyObj("Router", ["navigate"]);
    mockDialog = jasmine.createSpyObj("MatDialog", ["open"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);

    mockPermissionService.has.and.returnValue(true);
    mockSprayService.getTargets.and.returnValue(of(sampleTargets));
    mockSprayService.getApplicationMethods.and.returnValue(of(sampleAppMethods));
    mockSprayService.getProductLookup.and.returnValue(of([]));
    mockSprayService.getSpray.and.returnValue(of(sampleSpray));
    mockSprayService.getStorageLocationLookup.and.returnValue(of(sampleStorageLocations));
    mockFarmService.listUnits.and.returnValue(of([]));

    TestBed.configureTestingModule({
      imports: [SprayExecutionPageComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        provideNativeDateAdapter(),
        { provide: SprayService, useValue: mockSprayService },
        { provide: FarmManagementService, useValue: mockFarmService },
        { provide: PermissionService, useValue: mockPermissionService },
        { provide: BreadcrumbService, useValue: mockBreadcrumbService },
        { provide: MatDialog, useValue: mockDialog },
        { provide: MatSnackBar, useValue: mockSnackBar },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap({ id: "sp-1" }),
              url: urlSegments,
            },
          },
        },
      ],
    });

    mockRouter = TestBed.inject(Router) as any;
    spyOn(mockRouter, "navigate");

    return TestBed.compileComponents();
  }

  describe("in Start mode (/sprays/:id/start)", () => {
    beforeEach(async () => {
      await setupTestBed([{ path: "sp-1" }, { path: "start" }]);
      fixture = TestBed.createComponent(SprayExecutionPageComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    it("should initialize component in start mode and load initial lookups", () => {
      expect(component).toBeTruthy();
      expect(component.isStartMode()).toBeTrue();
      expect(mockSprayService.getSpray).toHaveBeenCalledWith("sp-1");
      expect(mockSprayService.getStorageLocationLookup).toHaveBeenCalledWith("farm-1", "item-1");
      expect(component.productsArray.length).toBe(1);
    });

    it("should require storage location and actual quantity to start", () => {
      const row = component.productsArray.at(0);
      expect(row.get("actualQuantity")?.valid).toBeTrue(); // prefilled with planned quantity
      expect(row.get("storageLocationId")?.valid).toBeTrue(); // auto-selected since 1 location

      row.patchValue({ storageLocationId: "", actualQuantity: null });
      expect(row.get("storageLocationId")?.hasError("required")).toBeTrue();
      expect(row.get("actualQuantity")?.hasError("required")).toBeTrue();

      component.onStart();
      expect(mockSprayService.startSpray).not.toHaveBeenCalled();
    });

    it("should call startSpray on valid submit and navigate to execution page", () => {
      mockSprayService.startSpray.and.returnValue(of({ ...sampleSpray, status: "InProgress" }));

      component.onStart();

      expect(mockSprayService.startSpray).toHaveBeenCalledWith("sp-1", jasmine.objectContaining({
        products: [
          jasmine.objectContaining({
            inventoryItemId: "item-1",
            storageLocationId: "loc-1",
            actualQuantity: 10,
          }),
        ],
      }));
      expect(mockRouter.navigate).toHaveBeenCalledWith(["/sprays", "sp-1", "execution"]);
    });

    it("should navigate back when onCancel is called", () => {
      component.onCancel();
      expect(mockRouter.navigate).toHaveBeenCalledWith(["/sprays", "sp-1"]);
    });
  });

  describe("in Execution mode (/sprays/:id/execution)", () => {
    beforeEach(async () => {
      await setupTestBed([{ path: "sp-1" }, { path: "execution" }]);
      fixture = TestBed.createComponent(SprayExecutionPageComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    it("should initialize in execution mode", () => {
      expect(component).toBeTruthy();
      expect(component.isStartMode()).toBeFalse();
    });

    it("should call saveExecution on onSaveProgress", () => {
      mockSprayService.saveExecution.and.returnValue(of(sampleSpray));

      component.onSaveProgress();

      expect(mockSprayService.saveExecution).toHaveBeenCalledWith("sp-1", jasmine.objectContaining({
        products: [
          jasmine.objectContaining({
            inventoryItemId: "item-1",
            actualQuantity: 10,
          }),
        ],
      }));
      expect(mockSnackBar.open).toHaveBeenCalledWith(
        jasmine.stringMatching(/progress saved/i),
        "OK",
        jasmine.any(Object),
      );
    });

    it("should prompt confirmation and call completeSpray on onComplete", () => {
      mockDialog.open.and.returnValue({
        afterClosed: () => of(true),
      } as any);
      mockSprayService.completeSpray.and.returnValue(of({ ...sampleSpray, status: "Completed" }));

      component.onComplete();

      expect(mockDialog.open).toHaveBeenCalled();
      expect(mockSprayService.completeSpray).toHaveBeenCalledWith("sp-1", jasmine.any(Object));
      expect(mockRouter.navigate).toHaveBeenCalledWith(["/sprays", "sp-1"]);
    });
  });
});
