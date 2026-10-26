import { ComponentFixture, TestBed } from "@angular/core/testing";
import { provideRouter } from "@angular/router";
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

    fixture = TestBed.createComponent(MainLayoutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create the component", () => {
    expect(component).toBeTruthy();
  });

  it("should structure Activities with Labor, Spray & Protection nested children, Irrigation, and Fertilizer", () => {
    const farmOpsGroup = component.navigationGroups.find(
      (g) => g.title === "Farm Operations",
    );
    expect(farmOpsGroup).toBeDefined();

    const activitiesItem = farmOpsGroup?.items.find((i) => i.id === "activities");
    expect(activitiesItem).toBeDefined();
    expect(activitiesItem?.children?.length).toBe(4);

    const childIds = activitiesItem?.children?.map((c) => c.id);
    expect(childIds).toEqual([
      "labor-activities",
      "activities-sprays",
      "irrigations",
      "fertilizer-applications",
    ]);

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

    const fertilizerItem = activitiesItem?.children?.find(
      (c) => c.id === "fertilizer-applications",
    );
    expect(fertilizerItem?.label).toBe("Fertilizer");
    expect(fertilizerItem?.route).toBe("/fertilizer-applications");
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

  describe("Sidebar Toggle & Collapsed Rail", () => {
    beforeEach(() => {
      localStorage.removeItem(MainLayoutComponent.SIDEBAR_COLLAPSED_KEY);
    });

    afterEach(() => {
      localStorage.removeItem(MainLayoutComponent.SIDEBAR_COLLAPSED_KEY);
    });

    it("should initialize isSidebarCollapsed to false by default", () => {
      expect(component.isSidebarCollapsed()).toBeFalse();
      const sidenavEl = fixture.nativeElement.querySelector(".navigation");
      expect(sidenavEl.classList.contains("navigation-collapsed")).toBeFalse();
    });

    it("should toggle sidebar collapse state and persist to localStorage", () => {
      expect(component.isSidebarCollapsed()).toBeFalse();
      expect(component.isCollapsedRail()).toBeFalse();

      component.toggleSidebarCollapse();
      expect(component.isSidebarCollapsed()).toBeTrue();
      expect(component.isCollapsedRail()).toBeTrue();
      expect(localStorage.getItem(MainLayoutComponent.SIDEBAR_COLLAPSED_KEY)).toBe("true");

      fixture.detectChanges();
      const containerEl = fixture.nativeElement.querySelector(".layout-container");
      const sidenavEl = fixture.nativeElement.querySelector(".navigation");
      expect(containerEl.classList.contains("sidebar-collapsed")).toBeTrue();
      expect(sidenavEl.classList.contains("navigation-collapsed")).toBeTrue();

      component.toggleSidebarCollapse();
      expect(component.isSidebarCollapsed()).toBeFalse();
      expect(component.isCollapsedRail()).toBeFalse();
      expect(localStorage.getItem(MainLayoutComponent.SIDEBAR_COLLAPSED_KEY)).toBe("false");

      fixture.detectChanges();
      expect(containerEl.classList.contains("sidebar-collapsed")).toBeFalse();
      expect(sidenavEl.classList.contains("navigation-collapsed")).toBeFalse();
    });

    it("should not activate collapsed rail if sidenav is closed", () => {
      component.isSidebarCollapsed.set(true);
      component.isSidenavOpened.set(false);
      expect(component.isCollapsedRail()).toBeFalse();

      fixture.detectChanges();
      const containerEl = fixture.nativeElement.querySelector(".layout-container");
      expect(containerEl.classList.contains("sidebar-collapsed")).toBeFalse();
    });

    it("should toggle sidebar when sidebar header toggle button is clicked", () => {
      const toggleBtn = fixture.nativeElement.querySelector(".sidebar-toggle-btn");
      expect(toggleBtn).toBeTruthy();

      toggleBtn.click();
      fixture.detectChanges();

      expect(component.isSidebarCollapsed()).toBeTrue();
      expect(toggleBtn.getAttribute("aria-label")).toBe("Expand sidebar");

      toggleBtn.click();
      fixture.detectChanges();

      expect(component.isSidebarCollapsed()).toBeFalse();
      expect(toggleBtn.getAttribute("aria-label")).toBe("Collapse sidebar");
    });

    it("should toggle sidebar when topbar hamburger button is clicked", () => {
      const topbarBtn = fixture.nativeElement.querySelector(".topbar-left button[aria-label='Toggle navigation']");
      expect(topbarBtn).toBeTruthy();

      topbarBtn.click();
      fixture.detectChanges();

      expect(component.isSidebarCollapsed()).toBeTrue();

      topbarBtn.click();
      fixture.detectChanges();

      expect(component.isSidebarCollapsed()).toBeFalse();
    });

    it("should expand collapsed sidebar when a parent menu item is clicked", () => {
      component.isSidebarCollapsed.set(true);
      const farmOpsGroup = component.navigationGroups.find((g) => g.title === "Farm Operations");
      const farmsItem = farmOpsGroup?.items.find((i) => i.id === "farms")!;

      const mockEvent = new MouseEvent("click");
      component.onParentClick(farmsItem, mockEvent);

      expect(component.isSidebarCollapsed()).toBeFalse();
      expect(localStorage.getItem(MainLayoutComponent.SIDEBAR_COLLAPSED_KEY)).toBe("false");
      expect(component.isMenuExpanded("farms")).toBeTrue();
    });

    it("should apply sidebar-closed class when sidenav is not opened", () => {
      component.isSidenavOpened.set(false);
      fixture.detectChanges();

      const containerEl = fixture.nativeElement.querySelector(".layout-container");
      expect(containerEl.classList.contains("sidebar-closed")).toBeTrue();
    });

    it("should correctly transition container classes when starting collapsed and toggling to expanded", () => {
      // Simulate component initializing in collapsed state (e.g. from localStorage)
      component.isSidebarCollapsed.set(true);
      fixture.detectChanges();

      const containerEl = fixture.nativeElement.querySelector(".layout-container");
      expect(containerEl.classList.contains("sidebar-collapsed")).toBeTrue();

      // Now toggle to expand
      component.toggleSidebarCollapse();
      fixture.detectChanges();

      expect(component.isSidebarCollapsed()).toBeFalse();
      expect(containerEl.classList.contains("sidebar-collapsed")).toBeFalse();
      expect(containerEl.classList.contains("sidebar-closed")).toBeFalse();
    });
  });
});
