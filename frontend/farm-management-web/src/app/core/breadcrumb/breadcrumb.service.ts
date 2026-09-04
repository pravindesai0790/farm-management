import { Injectable, computed, inject, signal } from "@angular/core";
import { NavigationEnd, Router } from "@angular/router";
import { filter } from "rxjs";

export interface BreadcrumbItem {
  readonly label: string;
  readonly route?: string | readonly (string | number)[];
  readonly icon?: string;
}

@Injectable({ providedIn: "root" })
export class BreadcrumbService {
  private readonly router = inject(Router);

  private readonly overrideTrail = signal<readonly BreadcrumbItem[] | null>(null);
  private readonly currentUrl = signal<string>(this.router.url);
  private readonly labelCache = new Map<string, string>();

  readonly breadcrumbs = computed<readonly BreadcrumbItem[]>(() => {
    const override = this.overrideTrail();
    if (override !== null && override.length > 0) {
      return override;
    }
    return this.buildAutoBreadcrumbs(this.currentUrl());
  });

  constructor() {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => {
        this.overrideTrail.set(null);
        this.currentUrl.set(event.urlAfterRedirects);
      });
  }

  setTrail(items: readonly BreadcrumbItem[]): void {
    this.overrideTrail.set(items);
  }

  setEntityName(id: string, name: string): void {
    this.labelCache.set(id, name);
    // Trigger signal re-evaluation
    this.currentUrl.set(this.currentUrl());
  }

  getEntityName(id: string): string | undefined {
    return this.labelCache.get(id);
  }

  clearTrail(): void {
    this.overrideTrail.set(null);
  }

  private buildAutoBreadcrumbs(rawUrl: string): readonly BreadcrumbItem[] {
    const cleanUrl = rawUrl.split("?")[0].split("#")[0];
    if (cleanUrl === "" || cleanUrl === "/" || cleanUrl === "/dashboard") {
      return [{ label: "Dashboard", route: "/dashboard", icon: "space_dashboard" }];
    }

    const segments = cleanUrl.split("/").filter(Boolean);
    const rootItem: BreadcrumbItem = {
      label: "Dashboard",
      route: "/dashboard",
      icon: "space_dashboard",
    };
    const items: BreadcrumbItem[] = [rootItem];

    if (segments.length === 0) {
      return items;
    }

    const section = segments[0];

    switch (section) {
      case "farms":
        items.push({ label: "Farms", route: "/farms" });
        if (segments[1] === "new") {
          items.push({ label: "Create farm" });
        } else if (segments[1]) {
          const farmId = segments[1];
          const farmName = this.labelCache.get(farmId) ?? "Farm details";
          if (segments[2] === "edit") {
            items.push({ label: farmName, route: ["/farms", farmId] });
            items.push({ label: "Edit" });
          } else {
            items.push({ label: farmName });
          }
        }
        break;

      case "farm-areas":
        items.push({ label: "Farms", route: "/farms" });
        items.push({ label: "Farm areas", route: "/farm-areas" });
        if (segments[1] === "new") {
          items.push({ label: "Create farm area" });
        } else if (segments[1]) {
          const areaId = segments[1];
          const areaName = this.labelCache.get(areaId) ?? "Area details";
          if (segments[2] === "edit") {
            items.push({ label: areaName, route: ["/farm-areas", areaId] });
            items.push({ label: "Edit" });
          } else {
            items.push({ label: areaName });
          }
        }
        break;

      case "plantations":
        items.push({ label: "Plantations", route: "/plantations" });
        if (segments[1] === "new") {
          items.push({ label: "Create plantation" });
        } else if (segments[1]) {
          const plantationId = segments[1];
          const plantationName = this.labelCache.get(plantationId) ?? "Plantation details";
          if (segments[2] === "edit") {
            items.push({ label: plantationName, route: ["/plantations", plantationId] });
            items.push({ label: "Edit" });
          } else {
            items.push({ label: plantationName });
          }
        }
        break;

      case "crop-cycles":
        items.push({ label: "Crop cycles", route: "/crop-cycles" });
        if (segments[1] === "new") {
          items.push({ label: "Create crop cycle" });
        } else if (segments[1]) {
          const cycleId = segments[1];
          const cycleName = this.labelCache.get(cycleId) ?? "Crop cycle details";
          if (segments[2] === "edit") {
            items.push({ label: cycleName, route: ["/crop-cycles", cycleId] });
            items.push({ label: "Edit" });
          } else {
            items.push({ label: cycleName });
          }
        }
        break;

      case "crops":
        items.push({ label: "Crop catalog", route: "/crops" });
        if (segments[1] === "new") {
          items.push({ label: "Create crop" });
        } else if (segments[1]) {
          const cropId = segments[1];
          const cropName = this.labelCache.get(cropId) ?? "Crop details";
          if (segments[2] === "edit") {
            items.push({ label: cropName, route: ["/crops", cropId] });
            items.push({ label: "Edit" });
          } else {
            items.push({ label: cropName });
          }
        }
        break;

      case "organization":
        items.push({ label: "Organization", route: "/organization" });
        if (segments[1] === "new") {
          items.push({ label: "Create organization" });
        }
        break;

      case "administration":
        items.push({ label: "Administration", route: "/administration" });
        if (segments[1] === "users") {
          items.push({ label: "Users", route: "/administration/users" });
          if (segments[2] === "new") {
            items.push({ label: "Create user" });
          } else if (segments[3] === "edit") {
            items.push({ label: "Edit user" });
          }
        } else if (segments[1] === "roles") {
          items.push({ label: "Roles", route: "/administration/roles" });
          if (segments[2] === "new") {
            items.push({ label: "Create role" });
          } else if (segments[3] === "edit") {
            items.push({ label: "Edit role" });
          }
        } else if (segments[1] === "permissions") {
          items.push({ label: "Permissions" });
        }
        break;

      case "settings":
        items.push({ label: "Settings", route: "/settings" });
        if (segments[1] === "change-password") {
          items.push({ label: "Change password" });
        }
        break;

      case "activities":
        items.push({ label: "Activities" });
        break;

      default: {
        const readable = section.charAt(0).toUpperCase() + section.slice(1).replace(/-/g, " ");
        items.push({ label: readable });
        break;
      }
    }

    return items;
  }
}
