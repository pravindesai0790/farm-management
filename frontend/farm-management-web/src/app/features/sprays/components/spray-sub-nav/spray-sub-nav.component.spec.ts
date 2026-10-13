import { ComponentFixture, TestBed } from "@angular/core/testing";
import { provideRouter } from "@angular/router";
import { SpraySubNavComponent } from "./spray-sub-nav.component";
import { PermissionService } from "../../../../core/auth/permission.service";

describe("SpraySubNavComponent", () => {
  let component: SpraySubNavComponent;
  let fixture: ComponentFixture<SpraySubNavComponent>;
  let mockPermissionService: { has: jasmine.Spy; hasAny: jasmine.Spy };

  beforeEach(async () => {
    mockPermissionService = {
      has: jasmine.createSpy("has").and.returnValue(true),
      hasAny: jasmine.createSpy("hasAny").and.returnValue(true),
    };

    await TestBed.configureTestingModule({
      imports: [SpraySubNavComponent],
      providers: [
        provideRouter([]),
        { provide: PermissionService, useValue: mockPermissionService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SpraySubNavComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create the component", () => {
    expect(component).toBeTruthy();
  });

  it("should define 3 navigation tabs for sprays hub", () => {
    expect(component.navItems.length).toBe(3);
    expect(component.navItems.map((i) => i.label)).toEqual([
      "Spray Applications",
      "Protection Products",
      "Spray Masters",
    ]);
  });

  it("should allow access when user has corresponding permissions", () => {
    expect(component.canAccess(component.navItems[0])).toBeTrue();
    expect(component.canAccess(component.navItems[1])).toBeTrue();
    expect(component.canAccess(component.navItems[2])).toBeTrue();
  });

  it("should deny access when permission is missing", () => {
    mockPermissionService.has.and.returnValue(false);
    mockPermissionService.hasAny.and.returnValue(false);

    expect(component.canAccess(component.navItems[0])).toBeFalse();
    expect(component.canAccess(component.navItems[1])).toBeFalse();
    expect(component.canAccess(component.navItems[2])).toBeFalse();
  });
});
