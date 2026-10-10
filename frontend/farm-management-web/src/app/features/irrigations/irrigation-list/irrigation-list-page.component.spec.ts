import { ComponentFixture, TestBed, fakeAsync, tick } from "@angular/core/testing";
import { provideNativeDateAdapter } from "@angular/material/core";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { provideRouter } from "@angular/router";
import { of, throwError } from "rxjs";
import { PermissionService } from "../../../core/auth/permission.service";
import { FarmManagementService } from "../../../core/farm-management/farm-management.service";
import { PagedResponse } from "../../../core/models/paged-response.model";
import {
  IrrigationListItem,
  IrrigationMethodDto,
  IrrigationSummaryCounts,
} from "../../../core/irrigations/irrigation.models";
import { IrrigationService } from "../../../core/irrigations/irrigation.service";
import { IrrigationListPageComponent } from "./irrigation-list-page.component";

describe("IrrigationListPageComponent", () => {
  let component: IrrigationListPageComponent;
  let fixture: ComponentFixture<IrrigationListPageComponent>;

  let mockIrrigationService: jasmine.SpyObj<IrrigationService>;
  let mockFarmService: jasmine.SpyObj<FarmManagementService>;
  let mockPermissionService: jasmine.SpyObj<PermissionService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const sampleIrrigationItem: IrrigationListItem = {
    id: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
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
    plannedAt: "2026-10-15T08:00:00Z",
    scheduledAt: "2026-10-15T08:00:00Z",
    actualStartedAt: null,
    actualEndedAt: null,
    actualDurationMinutes: null,
    irrigationMethodId: "method-1",
    irrigationMethodName: "Drip Irrigation",
    plannedWaterQuantity: 5000,
    plannedWaterUnitId: "unit-1",
    plannedWaterUnitName: "Liter",
    actualWaterQuantity: null,
    actualWaterUnitId: null,
    actualWaterUnitName: null,
    completedAt: null,
    createdAt: "2026-10-09T08:00:00Z",
  };

  const sampleListResponse: PagedResponse<IrrigationListItem> = {
    items: [sampleIrrigationItem],
    totalCount: 1,
    page: 1,
    pageSize: 20,
    totalPages: 1,
  };

  const sampleSummary: IrrigationSummaryCounts = {
    totalCount: 10,
    draftCount: 2,
    scheduledCount: 4,
    inProgressCount: 1,
    completedCount: 2,
    cancelledCount: 1,
    overdueCount: 1,
  };

  const sampleMethods: IrrigationMethodDto[] = [
    {
      id: "method-1",
      organizationId: null,
      code: "DRIP",
      name: "Drip Irrigation",
      description: null,
      displayOrder: 1,
      isSystem: true,
      isActive: true,
    },
  ];

  beforeEach(async () => {
    mockIrrigationService = jasmine.createSpyObj("IrrigationService", [
      "listIrrigations",
      "getSummaryCounts",
      "getMethods",
      "getCropCycleStages",
    ]);
    mockFarmService = jasmine.createSpyObj("FarmManagementService", [
      "listFarms",
      "listFarmAreas",
      "listPlantations",
      "listCycles",
    ]);
    mockPermissionService = jasmine.createSpyObj("PermissionService", ["has", "hasAny"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);

    mockPermissionService.has.and.returnValue(true);
    mockPermissionService.hasAny.and.returnValue(true);

    mockFarmService.listFarms.and.returnValue(
      of({ items: [{ id: "farm-1", name: "Green Valley Farm" } as any], totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
    );
    mockIrrigationService.getMethods.and.returnValue(of(sampleMethods));
    mockIrrigationService.getSummaryCounts.and.returnValue(of(sampleSummary));
    mockIrrigationService.listIrrigations.and.returnValue(of(sampleListResponse));

    await TestBed.configureTestingModule({
      imports: [IrrigationListPageComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        provideNativeDateAdapter(),
        { provide: IrrigationService, useValue: mockIrrigationService },
        { provide: FarmManagementService, useValue: mockFarmService },
        { provide: PermissionService, useValue: mockPermissionService },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(IrrigationListPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create component and load data on init", () => {
    expect(component).toBeTruthy();
    expect(mockIrrigationService.listIrrigations).toHaveBeenCalled();
    expect(mockIrrigationService.getSummaryCounts).toHaveBeenCalled();
    expect(mockIrrigationService.getMethods).toHaveBeenCalled();
    expect(component.irrigations().length).toBe(1);
    expect(component.summary()?.scheduledCount).toBe(4);
    expect(component.summary()?.overdueCount).toBe(1);
  });

  it("should display table columns", () => {
    expect(component.displayedColumns).toContain("reference");
    expect(component.displayedColumns).toContain("location");
    expect(component.displayedColumns).toContain("cropContext");
    expect(component.displayedColumns).toContain("method");
    expect(component.displayedColumns).toContain("dates");
    expect(component.displayedColumns).toContain("waterQuantity");
    expect(component.displayedColumns).toContain("status");
    expect(component.displayedColumns).toContain("actions");
  });

  it("should render event reference link and status pill in template", fakeAsync(() => {
    tick();
    fixture.detectChanges();
    const nativeElement: HTMLElement = fixture.nativeElement;
    const refCell = nativeElement.querySelector("td.cdk-column-reference");
    expect(refCell?.textContent?.trim()).toContain("IRR-A1B2C3D4");

    const statusPill = nativeElement.querySelector("td.cdk-column-status .status-pill");
    expect(statusPill?.textContent?.trim()).toContain("Scheduled");
  }));

  it("should render separate overdue chip when isOverdue is true without changing Scheduled status", fakeAsync(() => {
    const overdueItem: IrrigationListItem = {
      ...sampleIrrigationItem,
      isOverdue: true,
      status: "Scheduled",
      statusName: "Scheduled",
    };
    mockIrrigationService.listIrrigations.and.returnValue(
      of({ items: [overdueItem], totalCount: 1, page: 1, pageSize: 20, totalPages: 1 }),
    );
    component.loadIrrigations();
    tick();
    fixture.detectChanges();

    const nativeElement: HTMLElement = fixture.nativeElement;
    const overdueChip = nativeElement.querySelector(".overdue-chip");
    expect(overdueChip).toBeTruthy();
    expect(overdueChip?.textContent?.trim()).toContain("OVERDUE");

    const statusPill = nativeElement.querySelector("td.cdk-column-status .status-pill");
    expect(statusPill?.textContent?.trim()).toContain("Scheduled");
  }));

  it("should filter by status on status change", fakeAsync(() => {
    component.onStatusChange("InProgress");
    tick();

    expect(component.status()).toBe("InProgress");
    expect(mockIrrigationService.listIrrigations).toHaveBeenCalledWith(
      jasmine.objectContaining({ status: "InProgress" }),
    );
  }));

  it("should filter by search text with debounce", fakeAsync(() => {
    component.onSearchChange("North");
    tick(350);

    expect(mockIrrigationService.listIrrigations).toHaveBeenCalledWith(
      jasmine.objectContaining({ search: "North" }),
    );
  }));

  it("should toggle includeOverdue", () => {
    component.toggleIncludeOverdue();
    expect(component.includeOverdue()).toBeTrue();
    expect(mockIrrigationService.listIrrigations).toHaveBeenCalledWith(
      jasmine.objectContaining({ includeOverdue: true }),
    );

    component.toggleIncludeOverdue();
    expect(component.includeOverdue()).toBeFalse();
  });

  it("should filter when KPI card is clicked", () => {
    component.onKpiClick("Completed");
    expect(component.status()).toBe("Completed");
    expect(component.includeOverdue()).toBeFalse();
    expect(mockIrrigationService.listIrrigations).toHaveBeenCalledWith(
      jasmine.objectContaining({ status: "Completed" }),
    );

    component.onKpiClick("Overdue");
    expect(component.includeOverdue()).toBeTrue();
    expect(component.status()).toBe("");
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

  it("should handle server-side page changes", () => {
    component.onPageChange({ pageIndex: 1, pageSize: 50, length: 100 });
    expect(component.pageIndex()).toBe(1);
    expect(component.pageSize()).toBe(50);
    expect(mockIrrigationService.listIrrigations).toHaveBeenCalledWith(
      jasmine.objectContaining({ pageNumber: 2, pageSize: 50 }),
    );
  });

  it("should display loading state when isLoading is true", () => {
    component.isLoading.set(true);
    fixture.detectChanges();

    const nativeElement: HTMLElement = fixture.nativeElement;
    const loadingState = nativeElement.querySelector(".loading-state");
    expect(loadingState).toBeTruthy();
    expect(loadingState?.textContent).toContain("Loading irrigation events");
  });

  it("should display error state with retry button when error occurs", () => {
    component.isLoading.set(false);
    component.errorMessage.set("Network timeout error");
    fixture.detectChanges();

    const nativeElement: HTMLElement = fixture.nativeElement;
    const errorState = nativeElement.querySelector(".error-state");
    expect(errorState).toBeTruthy();
    expect(errorState?.textContent).toContain("Network timeout error");

    const retryBtn: HTMLButtonElement | null = nativeElement.querySelector(".error-state button");
    expect(retryBtn).toBeTruthy();
    spyOn(component, "loadAll");
    retryBtn?.click();
    expect(component.loadAll).toHaveBeenCalled();
  });

  it("should display empty state when no records match", () => {
    component.isLoading.set(false);
    component.errorMessage.set(null);
    (component as any).irrigations.set([]);
    fixture.detectChanges();

    const nativeElement: HTMLElement = fixture.nativeElement;
    const emptyState = nativeElement.querySelector(".empty-state");
    expect(emptyState).toBeTruthy();
    expect(emptyState?.textContent).toContain("No irrigation events found");
  });

  it("should check permissions for header creation action buttons", () => {
    mockPermissionService.has.and.callFake((perm: string) => perm === "Irrigation.Create");
    fixture.detectChanges();

    const nativeElement: HTMLElement = fixture.nativeElement;
    const buttons = nativeElement.querySelectorAll(".header-actions a");
    expect(buttons.length).toBe(1);
    expect(buttons[0]?.textContent).toContain("Plan Irrigation");
  });
});
