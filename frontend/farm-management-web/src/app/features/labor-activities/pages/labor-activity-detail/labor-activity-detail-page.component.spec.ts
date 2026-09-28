import { provideHttpClient } from "@angular/common/http";
import { provideHttpClientTesting } from "@angular/common/http/testing";
import { ComponentFixture, TestBed, fakeAsync, tick } from "@angular/core";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { ActivatedRoute, provideRouter } from "@angular/router";
import { of } from "rxjs";

import { PermissionService } from "../../../../core/auth/permission.service";
import { BreadcrumbService } from "../../../../core/breadcrumb/breadcrumb.service";
import { LaborActivity, NamedReference } from "../../models/labor-activity.models";
import { LaborActivityService } from "../../services/labor-activity.service";
import { LaborActivityDetailPageComponent } from "./labor-activity-detail-page.component";

describe("LaborActivityDetailPageComponent", () => {
  let component: LaborActivityDetailPageComponent;
  let fixture: ComponentFixture<LaborActivityDetailPageComponent>;

  let mockActivityService: jasmine.SpyObj<LaborActivityService>;
  let mockPermissionService: jasmine.SpyObj<PermissionService>;
  let mockBreadcrumbService: jasmine.SpyObj<BreadcrumbService>;
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

  const sampleCancelledActivity: LaborActivity = {
    id: "act-cancelled",
    activityDate: "2026-09-20",
    farm: sampleFarm,
    farmArea: null,
    plantation: null,
    cropCycle: null,
    cropCycleStage: null,
    activityType: sampleType,
    status: "CANCELLED",
    description: "Rain delay",
    cancellationReason: "Heavy rainfall prevented field work",
    createdAt: "2026-09-20T08:00:00Z",
    updatedAt: "2026-09-20T09:00:00Z",
  };

  beforeEach(async () => {
    mockActivityService = jasmine.createSpyObj("LaborActivityService", ["get", "cancel"]);
    mockPermissionService = jasmine.createSpyObj("PermissionService", ["has"]);
    mockBreadcrumbService = jasmine.createSpyObj("BreadcrumbService", ["setEntityName"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);

    mockPermissionService.has.and.returnValue(true);
    mockActivityService.get.and.returnValue(of(sampleActivityWithStage));

    await TestBed.configureTestingModule({
      imports: [LaborActivityDetailPageComponent, NoopAnimationsModule],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (key: string) => (key === "id" ? "act-1" : null),
              },
            },
          },
        },
        { provide: LaborActivityService, useValue: mockActivityService },
        { provide: PermissionService, useValue: mockPermissionService },
        { provide: BreadcrumbService, useValue: mockBreadcrumbService },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LaborActivityDetailPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });

  it("should load activity and render 5th hierarchy card for Stage", () => {
    expect(component.activity()).toEqual(sampleActivityWithStage);

    const nativeElement: HTMLElement = fixture.nativeElement;
    const cards = nativeElement.querySelectorAll(".cards mat-card");
    expect(cards.length).toBe(5);

    const stageCard = cards[4];
    expect(stageCard.textContent).toContain("Crop Cycle Stage");
    expect(stageCard.textContent).toContain("Stage 1: Budbreak");
  });

  it("should render null stage gracefully as em-dash when stage is null", () => {
    mockActivityService.get.and.returnValue(of(sampleCancelledActivity));
    component.loadActivity();
    fixture.detectChanges();

    const nativeElement: HTMLElement = fixture.nativeElement;
    const cards = nativeElement.querySelectorAll(".cards mat-card");
    expect(cards.length).toBe(5);

    const stageCard = cards[4];
    expect(stageCard.textContent).toContain("Crop Cycle Stage");
    expect(stageCard.textContent).toContain("—");
  });

  it("should display cancellation banner and reason for cancelled activity", () => {
    mockActivityService.get.and.returnValue(of(sampleCancelledActivity));
    component.loadActivity();
    fixture.detectChanges();

    const nativeElement: HTMLElement = fixture.nativeElement;
    const banner = nativeElement.querySelector(".cancelled-banner");
    expect(banner).toBeTruthy();
    expect(banner?.textContent).toContain("Heavy rainfall prevented field work");
  });
});
