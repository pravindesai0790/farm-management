import { TestBed } from "@angular/core/testing";
import { NavigationEnd, Router } from "@angular/router";
import { Subject } from "rxjs";
import { BreadcrumbService } from "./breadcrumb.service";

describe("BreadcrumbService", () => {
  let service: BreadcrumbService;
  let routerEvents$: Subject<any>;
  let mockRouter: any;

  beforeEach(() => {
    routerEvents$ = new Subject();
    mockRouter = {
      url: "/dashboard",
      events: routerEvents$.asObservable(),
    };

    TestBed.configureTestingModule({
      providers: [
        BreadcrumbService,
        { provide: Router, useValue: mockRouter },
      ],
    });

    service = TestBed.inject(BreadcrumbService);
  });

  it("should generate Dashboard breadcrumb for root or /dashboard", () => {
    const crumbs = service.breadcrumbs();
    expect(crumbs.length).toBe(1);
    expect(crumbs[0]).toEqual({ label: "Dashboard", route: "/dashboard", icon: "space_dashboard" });
  });

  it("should generate breadcrumbs for /irrigations list page under Activities", () => {
    routerEvents$.next(new NavigationEnd(1, "/irrigations", "/irrigations"));

    const crumbs = service.breadcrumbs();
    expect(crumbs.length).toBe(3);
    expect(crumbs[0]).toEqual({ label: "Dashboard", route: "/dashboard", icon: "space_dashboard" });
    expect(crumbs[1]).toEqual({ label: "Activities", route: "/activities" });
    expect(crumbs[2]).toEqual({ label: "Irrigation", route: "/irrigations" });
  });

  it("should generate breadcrumbs for /irrigations/new (Plan Irrigation)", () => {
    routerEvents$.next(new NavigationEnd(1, "/irrigations/new", "/irrigations/new"));

    const crumbs = service.breadcrumbs();
    expect(crumbs.length).toBe(4);
    expect(crumbs[0]).toEqual({ label: "Dashboard", route: "/dashboard", icon: "space_dashboard" });
    expect(crumbs[1]).toEqual({ label: "Activities", route: "/activities" });
    expect(crumbs[2]).toEqual({ label: "Irrigation", route: "/irrigations" });
    expect(crumbs[3]).toEqual({ label: "Plan Irrigation" });
  });

  it("should generate breadcrumbs for /irrigations/record-completed", () => {
    routerEvents$.next(new NavigationEnd(1, "/irrigations/record-completed", "/irrigations/record-completed"));

    const crumbs = service.breadcrumbs();
    expect(crumbs.length).toBe(4);
    expect(crumbs[0]).toEqual({ label: "Dashboard", route: "/dashboard", icon: "space_dashboard" });
    expect(crumbs[1]).toEqual({ label: "Activities", route: "/activities" });
    expect(crumbs[2]).toEqual({ label: "Irrigation", route: "/irrigations" });
    expect(crumbs[3]).toEqual({ label: "Record Completed" });
  });

  it("should generate breadcrumbs for /irrigations/:id with fallback and cached label", () => {
    routerEvents$.next(new NavigationEnd(1, "/irrigations/irr-999", "/irrigations/irr-999"));

    let crumbs = service.breadcrumbs();
    expect(crumbs.length).toBe(4);
    expect(crumbs[3].label).toBe("Irrigation details");

    // When entity label is registered
    service.setEntityName("irr-999", "Green Valley — North Field");
    crumbs = service.breadcrumbs();
    expect(crumbs[3].label).toBe("Green Valley — North Field");
  });

  it("should generate breadcrumbs for /irrigations/:id/edit", () => {
    service.setEntityName("irr-999", "Green Valley — North Field");
    routerEvents$.next(new NavigationEnd(1, "/irrigations/irr-999/edit", "/irrigations/irr-999/edit"));

    const crumbs = service.breadcrumbs();
    expect(crumbs.length).toBe(5);
    expect(crumbs[0]).toEqual({ label: "Dashboard", route: "/dashboard", icon: "space_dashboard" });
    expect(crumbs[1]).toEqual({ label: "Activities", route: "/activities" });
    expect(crumbs[2]).toEqual({ label: "Irrigation", route: "/irrigations" });
    expect(crumbs[3]).toEqual({ label: "Green Valley — North Field", route: ["/irrigations", "irr-999"] });
    expect(crumbs[4]).toEqual({ label: "Edit" });
  });
});
