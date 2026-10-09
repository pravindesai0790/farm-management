import { ComponentFixture, TestBed } from "@angular/core/testing";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { ActivatedRoute, Router, provideRouter } from "@angular/router";
import { MatDialog } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { of } from "rxjs";

import { BreadcrumbService } from "../../../core/breadcrumb/breadcrumb.service";
import { PermissionService } from "../../../core/auth/permission.service";
import { SprayDetailsResponse } from "../../../core/sprays/spray.models";
import { SprayService } from "../../../core/sprays/spray.service";
import { SprayDetailPageComponent } from "./spray-detail-page.component";

describe("SprayDetailPageComponent", () => {
  let component: SprayDetailPageComponent;
  let fixture: ComponentFixture<SprayDetailPageComponent>;
  let mockSprayService: jasmine.SpyObj<SprayService>;
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
        storageLocationId: "loc-1",
        storageLocationName: "Main Warehouse",
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

  beforeEach(async () => {
    mockSprayService = jasmine.createSpyObj("SprayService", ["getSpray", "completeSpray"]);
    mockPermissionService = jasmine.createSpyObj("PermissionService", ["has"]);
    mockBreadcrumbService = jasmine.createSpyObj("BreadcrumbService", ["setEntityName"]);
    mockRouter = jasmine.createSpyObj("Router", ["navigate"]);
    mockDialog = jasmine.createSpyObj("MatDialog", ["open"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);

    mockPermissionService.has.and.returnValue(true);
    mockSprayService.getSpray.and.returnValue(of(sampleSpray));

    await TestBed.configureTestingModule({
      imports: [SprayDetailPageComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: SprayService, useValue: mockSprayService },
        { provide: PermissionService, useValue: mockPermissionService },
        { provide: BreadcrumbService, useValue: mockBreadcrumbService },
        { provide: MatDialog, useValue: mockDialog },
        { provide: MatSnackBar, useValue: mockSnackBar },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (key: string) => (key === "id" ? "sp-1" : null),
              },
            },
          },
        },
      ],
    }).compileComponents();

    mockRouter = TestBed.inject(Router) as any;
    spyOn(mockRouter, "navigate");

    fixture = TestBed.createComponent(SprayDetailPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create component and load spray details", () => {
    expect(component).toBeTruthy();
    expect(mockSprayService.getSpray).toHaveBeenCalledWith("sp-1");
    expect(component.spray()).toEqual(sampleSpray);
    expect(mockBreadcrumbService.setEntityName).toHaveBeenCalledWith("sp-1", "SP-2026-0001");
  });

  it("should render reference number, status pill, and details in template", () => {
    const nativeElement: HTMLElement = fixture.nativeElement;
    const h1 = nativeElement.querySelector("h1");
    expect(h1?.textContent?.trim()).toContain("SP-2026-0001");

    const statusPill = nativeElement.querySelector(".status-pill");
    expect(statusPill?.textContent?.trim()).toContain("Scheduled");
  });

  it("should navigate to edit on onEdit", () => {
    component.onEdit();
    expect(mockRouter.navigate).toHaveBeenCalledWith(["/sprays", "sp-1", "edit"]);
  });

  it("should navigate to start on onStart", () => {
    component.onStart();
    expect(mockRouter.navigate).toHaveBeenCalledWith(["/sprays", "sp-1", "start"]);
  });

  it("should navigate to execution on onContinueExecution", () => {
    component.onContinueExecution();
    expect(mockRouter.navigate).toHaveBeenCalledWith(["/sprays", "sp-1", "execution"]);
  });

  it("should open schedule dialog on onSchedule and reload on success", () => {
    const dialogRefSpy = jasmine.createSpyObj({ afterClosed: of(true) });
    mockDialog.open.and.returnValue(dialogRefSpy);

    component.onSchedule();

    expect(mockDialog.open).toHaveBeenCalled();
    expect(mockSprayService.getSpray).toHaveBeenCalledTimes(2); // on init + reload
  });

  it("should open reschedule dialog on onReschedule", () => {
    const dialogRefSpy = jasmine.createSpyObj({ afterClosed: of(true) });
    mockDialog.open.and.returnValue(dialogRefSpy);

    component.onReschedule();

    expect(mockDialog.open).toHaveBeenCalledWith(
      jasmine.any(Function),
      jasmine.objectContaining({
        data: jasmine.objectContaining({ isReschedule: true }),
      }),
    );
  });

  it("should open cancel dialog on onCancelSpray and reload on success", () => {
    const dialogRefSpy = jasmine.createSpyObj({ afterClosed: of(true) });
    mockDialog.open.and.returnValue(dialogRefSpy);

    component.onCancelSpray();

    expect(mockDialog.open).toHaveBeenCalled();
    expect(mockSprayService.getSpray).toHaveBeenCalledTimes(2);
  });

  it("should complete spray directly on onCompleteDirect after confirmation", () => {
    const dialogRefSpy = jasmine.createSpyObj({ afterClosed: of(true) });
    mockDialog.open.and.returnValue(dialogRefSpy);
    mockSprayService.completeSpray.and.returnValue(of({ ...sampleSpray, status: "Completed" }));

    component.onCompleteDirect();

    expect(mockDialog.open).toHaveBeenCalled();
    expect(mockSprayService.completeSpray).toHaveBeenCalledWith("sp-1");
    expect(mockSnackBar.open).toHaveBeenCalledWith(
      jasmine.stringMatching(/completed and stock deducted/i),
      "OK",
      jasmine.any(Object),
    );
  });

  it("should map status labels and CSS classes", () => {
    expect(component.getStatusClass("Draft")).toBe("status-pill--planned");
    expect(component.getStatusClass("Scheduled")).toBe("status-pill--planned status-pill--scheduled");
    expect(component.getStatusClass("InProgress")).toBe("status-pill--in-progress");
    expect(component.getStatusClass("Completed")).toBe("status-pill--completed");
    expect(component.getStatusClass("Cancelled")).toBe("status-pill--cancelled");

    expect(component.getStatusLabel("InProgress")).toBe("In Progress");
  });
});
