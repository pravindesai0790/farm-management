import { ComponentFixture, TestBed } from "@angular/core/testing";
import { provideRouter, Router } from "@angular/router";
import { signal } from "@angular/core";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { MainLayoutComponent } from "./main-layout.component";
import { AuthService } from "../../core/auth/auth.service";
import { PermissionService } from "../../core/auth/permission.service";
import { BreadcrumbService } from "../../core/breadcrumb/breadcrumb.service";
import { CurrentUser } from "../../core/auth/auth.models";

describe("MainLayoutComponent Navigation", () => {
  let component: MainLayoutComponent;
  let fixture: ComponentFixture<MainLayoutComponent>;
  let mockAuthService: { user: ReturnType<typeof signal<CurrentUser | null>>; logout: jasmine.Spy };
  let mockPermissionService: { has: jasmine.Spy; hasAny: jasmine.Spy };
  let mockBreadcrumbService: { breadcrumbs: ReturnType<typeof signal<any[]>> };
  let router: Router;

  const mockUser: CurrentUser = {
    id: "user-1",
    email: "test@farm.com",
    firstName: "John",
    lastName: "Doe",
    organizationId: "org-1",
    organizationName: "Sunrise Farms",
    roles: ["FarmManager"],
    permissions: [
      "LaborActivity.View",
      "Spray.View",
      "PlantProtectionProduct.View",
      "ProductType.View",
      "Irrigation.View",
      "Crop.View",
      "CropLifecycleTemplate.View",
    ],
  };

  beforeEach(async () => {
    mockAuthService = {
      user: signal<CurrentUser | null>(mockUser),
      logout: jasmine.createSpy("logout"),
    };

    mockPermissionService = {
      has: jasmine.createSpy("has").and.callFake((perm: string) => {
        return mockUser.permissions.includes(perm);
      }),
      hasAny: jasmine.createSpy("hasAny").and.callFake((perms: readonly string[]) => {
        return perms.some((p) => mockUser.permissions.includes(p));
      }),
    };

    mockBreadcrumbService = {
      breadcrumbs: signal<any[]>([]),
    };

    await TestBed.configureTestingModule({
      imports: [MainLayoutComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService },
        { provide: PermissionService, useValue: mockPermissionService },
        { provide: BreadcrumbService, useValue: mockBreadcrumbService },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    fixture = TestBed.createComponent(MainLayoutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create the component", () => {
    expect(component).toBeTruthy();
  });

  it("should structure Activities with Labor, Spray & Protection nested children, and Irrigation", () => {
    const farmOpsGroup = component.navigationGroups.find(
      (g) => g.title === "Farm Operations",
    );
    expect(farmOpsGroup).toBeDefined();

    const activitiesItem = farmOpsGroup?.items.find((i) => i.id === "activities");
    expect(activitiesItem).toBeDefined();
    expect(activitiesItem?.children?.length).toBe(3);

    const childIds = activitiesItem?.children?.map((c) => c.id);
    expect(childIds).toEqual(["labor-activities", "activities-sprays", "irrigations"]);

    const sprayParent = activitiesItem?.children?.find(
      (c) => c.id === "activities-sprays",
    );
    expect(sprayParent?.label).toBe("Spray & Protection");
    expect(sprayParent?.route).toBe("/sprays");
    expect(sprayParent?.children?.length).toBe(3);

    const sprayGrandchildren = sprayParent?.children?.map((c) => ({
      label: c.label,
      route: c.route,
      exactMatch: c.exactMatch,
    }));
    expect(sprayGrandchildren).toEqual([
      { label: "Spray Applications", route: "/sprays", exactMatch: true },
      { label: "Protection products", route: "/sprays/products", exactMatch: undefined },
      { label: "Spray masters", route: "/sprays/master-data", exactMatch: undefined },
    ]);

    const irrigationItem = activitiesItem?.children?.find(
      (c) => c.id === "irrigations",
    );
    expect(irrigationItem?.label).toBe("Irrigation");
    expect(irrigationItem?.route).toBe("/irrigations");
  });

  it("should not contain standalone sprays or irrigations directly under Farm Operations", () => {
    const farmOpsGroup = component.navigationGroups.find(
      (g) => g.title === "Farm Operations",
    );
    const itemIds = farmOpsGroup?.items.map((i) => i.id);
    expect(itemIds).not.toContain("sprays");
    expect(itemIds).not.toContain("irrigations");
  });

  it("should retain only crops and lifecycle templates under Agronomy group", () => {
    const agronomyGroup = component.navigationGroups.find(
      (g) => g.title === "Agronomy",
    );
    expect(agronomyGroup).toBeDefined();
    const itemIds = agronomyGroup?.items.map((i) => i.id);
    expect(itemIds).toEqual(["crops", "crop-lifecycle-templates"]);
    expect(itemIds).not.toContain("plant-protection-products");
    expect(itemIds).not.toContain("spray-masters");
  });

  it("should auto-expand activities and activities-sprays when navigating to a spray route", () => {
    (component as any).autoExpandActiveGroup("/sprays/products");
    const expanded = component.expandedMenus();
    expect(expanded.has("activities")).toBeTrue();
    expect(expanded.has("activities-sprays")).toBeTrue();
  });

  it("should auto-expand activities when navigating to irrigation route", () => {
    (component as any).autoExpandActiveGroup("/irrigations");
    const expanded = component.expandedMenus();
    expect(expanded.has("activities")).toBeTrue();
  });

  it("should toggle child menu expansion via toggleChildChevron", () => {
    const mockChild = { id: "activities-sprays", label: "Spray & Protection" };
    const mockEvent = jasmine.createSpyObj<MouseEvent>("MouseEvent", ["stopPropagation", "preventDefault"]);
    expect(component.isMenuExpanded("activities-sprays")).toBeFalse();

    component.toggleChildChevron(mockChild, mockEvent);
    expect(mockEvent.stopPropagation).toHaveBeenCalled();
    expect(mockEvent.preventDefault).toHaveBeenCalled();
    expect(component.isMenuExpanded("activities-sprays")).toBeTrue();

    component.toggleChildChevron(mockChild, mockEvent);
    expect(component.isMenuExpanded("activities-sprays")).toBeFalse();
  });

  it("should filter visible nested child items when user lacks permissions", () => {
    const allowed = ["LaborActivity.View", "Spray.View", "Irrigation.View"];
    mockPermissionService.has.and.callFake((perm: string) => allowed.includes(perm));
    mockPermissionService.hasAny.and.callFake((perms: readonly string[]) =>
      perms.some((p) => allowed.includes(p)),
    );

    fixture = TestBed.createComponent(MainLayoutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    const visibleGroups = component.visibleNavigationGroups();
    const farmOpsGroup = visibleGroups.find((g) => g.title === "Farm Operations");
    const activitiesItem = farmOpsGroup?.items.find((i) => i.id === "activities");
    const sprayParent = activitiesItem?.children?.find((c) => c.id === "activities-sprays");

    expect(sprayParent).toBeDefined();
    // Only "Spray Applications" should be visible because of Spray.View
    expect(sprayParent?.children?.length).toBe(1);
    expect(sprayParent?.children?.[0].label).toBe("Spray Applications");
  });
});
