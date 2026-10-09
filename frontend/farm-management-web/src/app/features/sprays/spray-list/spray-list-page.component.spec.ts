import { ComponentFixture, TestBed, fakeAsync, tick } from "@angular/core/testing";
import { provideNativeDateAdapter } from "@angular/material/core";
import { MatDialog } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { provideRouter } from "@angular/router";
import { of } from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import { FarmManagementService } from "../../../core/farm-management/farm-management.service";
import { PagedResponse } from "../../../core/models/paged-response.model";
import {
  SprayListItem,
  TargetResponse,
} from "../../../core/sprays/spray.models";
import { SprayService } from "../../../core/sprays/spray.service";
import { SprayListPageComponent } from "./spray-list-page.component";

describe("SprayListPageComponent", () => {
  let component: SprayListPageComponent;
  let fixture: ComponentFixture<SprayListPageComponent>;

  let mockSprayService: jasmine.SpyObj<SprayService>;
  let mockFarmService: jasmine.SpyObj<FarmManagementService>;
  let mockPermissionService: jasmine.SpyObj<PermissionService>;
  let mockDialog: jasmine.SpyObj<MatDialog>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const sampleSprayItem: SprayListItem = {
    id: "sp-1",
    referenceNumber: "SP-2026-0001",
    farmId: "farm-1",
    farmName: "Green Valley Farm",
    farmAreaId: "area-1",
    farmAreaName: "North Sector",
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
    targetId: "target-1",
    targetName: "Powdery Mildew",
    applicationMethodId: "app-1",
    applicationMethodName: "Foliar Spray",
    productCount: 2,
    createdAt: "2026-10-09T08:00:00Z",
  };

  const sampleSprayListResponse: PagedResponse<SprayListItem> = {
    items: [sampleSprayItem],
    totalCount: 1,
    page: 1,
    pageSize: 20,
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

  beforeEach(async () => {
    mockSprayService = jasmine.createSpyObj("SprayService", [
      "listSprays",
      "getTargets",
      "getCropCycleStages",
    ]);
    mockFarmService = jasmine.createSpyObj("FarmManagementService", [
      "listFarms",
      "listAreas",
      "listPlantations",
      "listCycles",
    ]);
    mockPermissionService = jasmine.createSpyObj("PermissionService", ["has", "hasAny"]);
    mockDialog = jasmine.createSpyObj("MatDialog", ["open"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);

    mockPermissionService.has.and.returnValue(true);
    mockPermissionService.hasAny.and.returnValue(true);
    mockFarmService.listFarms.and.returnValue(
      of({ items: [{ id: "farm-1", name: "Green Valley Farm" } as any], totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
    );
    mockSprayService.getTargets.and.returnValue(of(sampleTargets));
    mockSprayService.listSprays.and.returnValue(of(sampleSprayListResponse));

    await TestBed.configureTestingModule({
      imports: [SprayListPageComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        provideNativeDateAdapter(),
        { provide: SprayService, useValue: mockSprayService },
        { provide: FarmManagementService, useValue: mockFarmService },
        { provide: PermissionService, useValue: mockPermissionService },
        { provide: MatDialog, useValue: mockDialog },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SprayListPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create component", () => {
    expect(component).toBeTruthy();
  });

  it("should load sprays and targets on init", () => {
    expect(mockSprayService.listSprays).toHaveBeenCalled();
    expect(mockSprayService.getTargets).toHaveBeenCalled();
    expect(component.sprays().length).toBe(1);
    expect(component.totalCount()).toBe(1);
    expect(component.targets().length).toBe(1);
  });

  it("should display table columns", () => {
    expect(component.displayedColumns).toContain("reference");
    expect(component.displayedColumns).toContain("farm");
    expect(component.displayedColumns).toContain("cropContext");
    expect(component.displayedColumns).toContain("target");
    expect(component.displayedColumns).toContain("dates");
    expect(component.displayedColumns).toContain("products");
    expect(component.displayedColumns).toContain("status");
    expect(component.displayedColumns).toContain("actions");
  });

  it("should render spray reference and status pill in template", () => {
    fixture.detectChanges();
    const nativeElement: HTMLElement = fixture.nativeElement;
    const refCell = nativeElement.querySelector("td.cdk-column-reference");
    expect(refCell?.textContent?.trim()).toContain("SP-2026-0001");

    const statusPill = nativeElement.querySelector(".status-pill");
    expect(statusPill?.textContent?.trim()).toContain("Scheduled");
  });

  it("should filter by status", fakeAsync(() => {
    component.onStatusChange("InProgress");
    tick();

    expect(component.status()).toBe("InProgress");
    expect(mockSprayService.listSprays).toHaveBeenCalledWith(
      jasmine.objectContaining({ status: "InProgress" }),
    );
  }));

  it("should filter by search text with debounce", fakeAsync(() => {
    component.onSearchChange("Mildew");
    tick(350);

    expect(mockSprayService.listSprays).toHaveBeenCalledWith(
      jasmine.objectContaining({ search: "Mildew" }),
    );
  }));

  it("should toggle includeOverdue", () => {
    component.toggleIncludeOverdue();
    expect(component.includeOverdue()).toBeTrue();
    expect(mockSprayService.listSprays).toHaveBeenCalledWith(
      jasmine.objectContaining({ includeOverdue: true }),
    );

    component.toggleIncludeOverdue();
    expect(component.includeOverdue()).toBeFalse();
  });

  it("should clear all filters when clearFilters is called", () => {
    component.onStatusChange("Scheduled");
    component.toggleIncludeOverdue();
    expect(component.hasActiveFilters()).toBeTrue();

    component.clearFilters();
    expect(component.status()).toBe("");
    expect(component.includeOverdue()).toBeFalse();
    expect(component.hasActiveFilters()).toBeFalse();
  });

  it("should map status labels and CSS classes correctly", () => {
    expect(component.getStatusClass("Draft")).toBe("status-pill--planned");
    expect(component.getStatusClass("Scheduled")).toBe("status-pill--planned status-pill--scheduled");
    expect(component.getStatusClass("InProgress")).toBe("status-pill--in-progress");
    expect(component.getStatusClass("Completed")).toBe("status-pill--completed");
    expect(component.getStatusClass("Cancelled")).toBe("status-pill--cancelled");

    expect(component.getStatusLabel("InProgress")).toBe("In Progress");
  });
});
