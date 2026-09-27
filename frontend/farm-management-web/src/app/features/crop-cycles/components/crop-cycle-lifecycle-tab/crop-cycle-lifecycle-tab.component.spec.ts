import { provideHttpClient } from "@angular/common/http";
import { provideHttpClientTesting } from "@angular/common/http/testing";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { MatDialog } from "@angular/material/dialog";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { provideRouter } from "@angular/router";
import { of } from "rxjs";
import { PermissionService } from "../../../../core/auth/permission.service";
import { CropCycle, CropCycleLifecycle } from "../../../../core/farm-management/farm-management.models";
import { CropCycleLifecycleTabComponent } from "./crop-cycle-lifecycle-tab.component";

describe("CropCycleLifecycleTabComponent", () => {
  let component: CropCycleLifecycleTabComponent;
  let fixture: ComponentFixture<CropCycleLifecycleTabComponent>;
  let mockPermissionService: jasmine.SpyObj<PermissionService>;
  let mockDialog: jasmine.SpyObj<MatDialog>;

  const mockCycle: CropCycle = {
    id: "cycle-1",
    plantationId: "plant-1",
    plantationName: "Block A Grape",
    farmName: "Sunrise Farm",
    farmAreaName: "North Field",
    cropId: "crop-1",
    cropName: "Grape",
    cycleName: "Grape 2026-27",
    seasonYear: 2026,
    seasonName: "Spring",
    plannedStartDate: "2026-03-01",
    actualStartDate: "2026-03-01",
    expectedEndDate: "2026-10-30",
    status: "ACTIVE",
    lifecycleTemplateId: "tmpl-1",
    lifecycleTemplateName: "Standard Grape Lifecycle",
  };

  const mockLifecycle: CropCycleLifecycle = {
    cropCycleId: "cycle-1",
    cycleName: "Grape 2026-27",
    overallStatus: "ACTIVE",
    lifecycleTemplateId: "tmpl-1",
    lifecycleTemplateName: "Standard Grape Lifecycle",
    currentStageName: "Pruning",
    currentStageSequence: 2,
    totalStagesCount: 3,
    completedStagesCount: 1,
    progressPercentage: 33,
    hasGeneratedStages: true,
    stages: [
      {
        id: "stage-1",
        cropCycleId: "cycle-1",
        lifecycleTemplateStageId: "lts-1",
        stageName: "Dormancy",
        sequenceNumber: 1,
        expectedDurationDays: 30,
        plannedStartDate: "2026-03-01",
        plannedEndDate: "2026-03-31",
        actualStartDate: "2026-03-01",
        actualEndDate: "2026-03-31",
        status: "COMPLETED",
        notes: null,
      },
      {
        id: "stage-2",
        cropCycleId: "cycle-1",
        lifecycleTemplateStageId: "lts-2",
        stageName: "Pruning",
        sequenceNumber: 2,
        expectedDurationDays: 15,
        plannedStartDate: "2026-04-01",
        plannedEndDate: "2026-04-15",
        actualStartDate: "2026-04-01",
        actualEndDate: null,
        status: "IN_PROGRESS",
        notes: null,
      },
      {
        id: "stage-3",
        cropCycleId: "cycle-1",
        lifecycleTemplateStageId: "lts-3",
        stageName: "Bud Break",
        sequenceNumber: 3,
        expectedDurationDays: 10,
        plannedStartDate: "2026-04-16",
        plannedEndDate: "2026-04-26",
        actualStartDate: null,
        actualEndDate: null,
        status: "NOT_STARTED",
        notes: null,
      },
    ],
  };

  beforeEach(async () => {
    mockPermissionService = jasmine.createSpyObj("PermissionService", ["has"]);
    mockPermissionService.has.and.returnValue(false);

    mockDialog = jasmine.createSpyObj("MatDialog", ["open"]);
    mockDialog.open.and.returnValue({
      afterClosed: () => of(null),
    } as any);

    await TestBed.configureTestingModule({
      imports: [CropCycleLifecycleTabComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: PermissionService, useValue: mockPermissionService },
      ],
    })
      .overrideProvider(MatDialog, { useValue: mockDialog })
      .compileComponents();

    fixture = TestBed.createComponent(CropCycleLifecycleTabComponent);
    component = fixture.componentInstance;
    component.cycle = mockCycle;
    component.lifecycle = mockLifecycle;
    fixture.detectChanges();
  });

  it("should create component and render lifecycle metrics", () => {
    expect(component).toBeTruthy();
    expect(component.canComplete(mockLifecycle.stages[1])).toBeFalse();
  });

  it("should calculate action eligibility based on stage status and permissions", () => {
    mockPermissionService.has.and.callFake((perm: string) => {
      return (
        perm === "CropCycleLifecycle.UpdateStage" ||
        perm === "CropCycleLifecycle.SkipStage" ||
        perm === "CropCycleLifecycle.ReopenStage" ||
        perm === "CropCycleLifecycle.OverrideStage"
      );
    });

    const stage1Completed = mockLifecycle.stages[0];
    const stage2InProgress = mockLifecycle.stages[1];
    const stage3NotStarted = mockLifecycle.stages[2];

    expect(component.canComplete(stage2InProgress)).toBeTrue();
    expect(component.canSkip(stage2InProgress)).toBeTrue();
    expect(component.canReopen(stage1Completed)).toBeTrue();
    expect(component.canReopen(stage2InProgress)).toBeFalse();
    expect(component.canOverride(stage3NotStarted)).toBeTrue();
    expect(component.hasMenuActions(stage2InProgress)).toBeTrue();
  });

  it("should open action dialog and emit stageActionCompleted on success", () => {
    mockPermissionService.has.and.returnValue(true);
    mockDialog.open.and.returnValue({
      afterClosed: () => of({ success: true, actionMode: "complete" }),
    } as any);

    spyOn(component.stageActionCompleted, "emit");

    component.openActionDialog(mockLifecycle.stages[1], "complete");

    expect(mockDialog.open).toHaveBeenCalled();
    expect(component.stageActionCompleted.emit).toHaveBeenCalled();
  });
});
