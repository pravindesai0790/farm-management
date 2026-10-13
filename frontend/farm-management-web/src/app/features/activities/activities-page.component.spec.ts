import { ComponentFixture, TestBed } from "@angular/core/testing";
import { provideRouter } from "@angular/router";
import { ActivitiesPageComponent } from "./activities-page.component";
import { PermissionService } from "../../core/auth/permission.service";

describe("ActivitiesPageComponent", () => {
  let component: ActivitiesPageComponent;
  let fixture: ComponentFixture<ActivitiesPageComponent>;
  let mockPermissionService: { has: jasmine.Spy };

  beforeEach(async () => {
    mockPermissionService = {
      has: jasmine.createSpy("has").and.returnValue(true),
    };

    await TestBed.configureTestingModule({
      imports: [ActivitiesPageComponent],
      providers: [
        provideRouter([]),
        { provide: PermissionService, useValue: mockPermissionService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ActivitiesPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create the component", () => {
    expect(component).toBeTruthy();
  });

  it("should mark Spray applications and Irrigation management as AVAILABLE with routes", () => {
    const sprayCard = component.activityCards.find((c) => c.id === "spraying");
    expect(sprayCard).toBeDefined();
    expect(sprayCard?.status).toBe("AVAILABLE");
    expect(sprayCard?.route).toBe("/sprays");
    expect(sprayCard?.requiredPermission).toBe("Spray.View");
    expect(sprayCard?.createRoute).toBe("/sprays/new");

    const irrigationCard = component.activityCards.find((c) => c.id === "irrigation");
    expect(irrigationCard).toBeDefined();
    expect(irrigationCard?.status).toBe("AVAILABLE");
    expect(irrigationCard?.route).toBe("/irrigations");
    expect(irrigationCard?.requiredPermission).toBe("Irrigation.View");
    expect(irrigationCard?.createRoute).toBe("/irrigations/new");
  });
});
