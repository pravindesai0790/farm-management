import { provideHttpClient } from "@angular/common/http";
import { provideHttpClientTesting } from "@angular/common/http/testing";
import { ComponentFixture, TestBed, fakeAsync, tick } from "@angular/core/testing";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { provideRouter } from "@angular/router";
import { of } from "rxjs";

import { PermissionService } from "../../../../core/auth/permission.service";
import { FarmManagementService } from "../../../../core/farm-management/farm-management.service";
import { LaborActivity, NamedReference } from "../../models/labor-activity.models";
import { LaborActivityService } from "../../services/labor-activity.service";
import { LaborActivityListPageComponent } from "./labor-activity-list-page.component";

describe("LaborActivityListPageComponent", () => {
  let component: LaborActivityListPageComponent;
  let fixture: ComponentFixture<LaborActivityListPageComponent>;

  let mockActivityService: jasmine.SpyObj<LaborActivityService>;
  let mockFarmService: jasmine.SpyObj<FarmManagementService>;
  let mockPermissionService: jasmine.SpyObj<PermissionService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const sampleFarm: NamedReference = { id: "farm-1", name: "Green Valley Farm" };
  const sampleArea: NamedReference = { id: "area-1", name: "North Sector" };
  const samplePlantation: NamedReference = { id: "plant-1", name: "Block 1 Grapes" };
  const sampleCycle: NamedReference = { id: "cycle-1", name: "2026 Season" };
  const sampleType: NamedReference = { id: "type-1", name: "Pruning" };

  const sampleActivityWithStage: LaborActivity = {
    id: "act-1",
    activityDate: "2026-09-28",
    farm: sampleFarm,
    farmArea: sampleArea,
    plantation: samplePlantation,
    cropCycle: sampleCycle,
    cropCycleStage: { id: "stage-1", name: "Budbreak", sequenceNumber: 1 },
    activityType: sampleType,
    status: "COMPLETED",
    description: "Manual canopy trimming",
    createdAt: "2026-09-28T10:00:00Z",
    updatedAt: null,
  };

  const sampleActivityWithoutStage: LaborActivity = {
    id: "act-2",
    activityDate: "2026-09-27",
    farm: sampleFarm,
    farmArea: null,
    plantation: null,
    cropCycle: null,
    cropCycleStage: null,
    activityType: sampleType,
    status: "DRAFT",
    description: null,
    createdAt: "2026-09-27T10:00:00Z",
    updatedAt: null,
  };

  beforeEach(async () => {
    mockActivityService = jasmine.createSpyObj("LaborActivityService", [
      "list",
      "getTypes",
      "cancel",
    ]);
    mockFarmService = jasmine.createSpyObj("FarmManagementService", [
      "listFarms",
      "listAreas",
      "listPlantations",
    ]);
    mockPermissionService = jasmine.createSpyObj("PermissionService", ["has"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);

    mockPermissionService.has.and.returnValue(true);
    mockFarmService.listFarms.and.returnValue(
      of({ items: [sampleFarm as any], totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
    );
    mockFarmService.listAreas.and.returnValue(of([]));
    mockFarmService.listPlantations.and.returnValue(
      of({ items: [], totalCount: 0, page: 1, pageSize: 100, totalPages: 0 }),
    );
    mockActivityService.getTypes.and.returnValue(of([sampleType]));
    mockActivityService.list.and.returnValue(
      of({
        items: [sampleActivityWithStage, sampleActivityWithoutStage],
        totalCount: 2,
        page: 1,
        pageSize: 20,
        totalPages: 1,
      }),
    );

    await TestBed.configureTestingModule({
      imports: [LaborActivityListPageComponent, NoopAnimationsModule],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: LaborActivityService, useValue: mockActivityService },
        { provide: FarmManagementService, useValue: mockFarmService },
        { provide: PermissionService, useValue: mockPermissionService },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LaborActivityListPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });

  it("should load activities on init and populate signals", () => {
    expect(component.activities().length).toBe(2);
    expect(component.totalCount()).toBe(2);
    expect(mockActivityService.list).toHaveBeenCalled();
  });

  it("should display Stage column in table", () => {
    expect(component.displayedColumns).toContain("stage");
    const nativeElement: HTMLElement = fixture.nativeElement;
    const stageHeader = nativeElement.querySelector(".mat-mdc-header-cell.cdk-column-stage");
    expect(stageHeader).toBeTruthy();
    expect(stageHeader?.textContent?.trim()).toBe("Stage");
  });

  it("should render stage badge when stage is present and em-dash when stage is null", () => {
    fixture.detectChanges();
    const nativeElement: HTMLElement = fixture.nativeElement;
    const stageCells = nativeElement.querySelectorAll(".mat-mdc-cell.cdk-column-stage");

    expect(stageCells.length).toBe(2);
    expect(stageCells[0].textContent?.trim()).toContain("Stage 1: Budbreak");
    expect(stageCells[1].textContent?.trim()).toBe("—");
  });

  it("should filter activities by status", fakeAsync(() => {
    component.onStatusChange("COMPLETED");
    tick();
    expect(component.status()).toBe("COMPLETED");
    expect(mockActivityService.list).toHaveBeenCalledWith(
      jasmine.objectContaining({ status: "COMPLETED" }),
    );
  }));

  it("should clear active filters when clearFilters is called", () => {
    component.onStatusChange("DRAFT");
    expect(component.hasActiveFilters()).toBeTrue();

    component.clearFilters();
    expect(component.status()).toBe("");
    expect(component.hasActiveFilters()).toBeFalse();
  });
});
