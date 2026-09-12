import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { MatButtonModule } from "@angular/material/button";
import { MatDividerModule } from "@angular/material/divider";
import { MatIconModule } from "@angular/material/icon";
import { MatListModule } from "@angular/material/list";
import { MatMenuModule } from "@angular/material/menu";
import { MatSidenavModule } from "@angular/material/sidenav";
import { MatToolbarModule } from "@angular/material/toolbar";
import { MatTooltipModule } from "@angular/material/tooltip";
import {
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from "@angular/router";
import { filter } from "rxjs";

import { AuthService } from "../../core/auth/auth.service";
import { PermissionService } from "../../core/auth/permission.service";
import { BreadcrumbService } from "../../core/breadcrumb/breadcrumb.service";

export interface NavigationChildItem {
  readonly label: string;
  readonly route: string;
  readonly permissions?: readonly string[];
  readonly exactMatch?: boolean;
  readonly badge?: string;
}

export interface NavigationItem {
  readonly id: string;
  readonly label: string;
  readonly icon: string;
  readonly route?: string;
  readonly permissions?: readonly string[];
  readonly exactMatch?: boolean;
  readonly badge?: string;
  readonly children?: readonly NavigationChildItem[];
}

export interface NavigationGroup {
  readonly title: string;
  readonly items: readonly NavigationItem[];
}

@Component({
  selector: "app-main-layout",
  standalone: true,
  imports: [
    MatButtonModule,
    MatDividerModule,
    MatIconModule,
    MatListModule,
    MatMenuModule,
    MatSidenavModule,
    MatToolbarModule,
    MatTooltipModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
  ],
  templateUrl: "./main-layout.component.html",
  styleUrl: "./main-layout.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MainLayoutComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly permissionService = inject(PermissionService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly breadcrumbService = inject(BreadcrumbService);

  readonly currentUser = this.authService.user;
  readonly breadcrumbs = this.breadcrumbService.breadcrumbs;

  readonly currentUrl = signal<string>(this.router.url);
  readonly expandedMenus = signal<ReadonlySet<string>>(
    new Set<string>(["farms", "plantations", "activities", "labor"]),
  );

  readonly organizationName = computed(
    () => this.currentUser()?.organizationName || "Farm Management",
  );
  readonly userDisplayName = computed(() => {
    const user = this.currentUser();
    return user ? `${user.firstName} ${user.lastName}`.trim() : "User";
  });
  readonly userRole = computed(() => {
    const roles = this.currentUser()?.roles;
    if (!roles || roles.length === 0) return "";
    return roles[0].replace(/([a-z])([A-Z])/g, "$1 $2");
  });
  readonly todayFormatted = computed(() =>
    new Intl.DateTimeFormat("en-US", {
      weekday: "short",
      month: "short",
      day: "numeric",
    }).format(new Date()),
  );

  readonly navigationGroups: readonly NavigationGroup[] = [
    {
      title: "Overview",
      items: [
        {
          id: "dashboard",
          label: "Dashboard",
          icon: "space_dashboard",
          route: "/dashboard",
          exactMatch: true,
        },
      ],
    },
    {
      title: "Farm Operations",
      items: [
        {
          id: "farms",
          label: "Farms",
          icon: "landscape",
          route: "/farms",
          permissions: ["Farm.View"],
          children: [
            {
              label: "Farm areas",
              route: "/farm-areas",
              permissions: ["FarmArea.View"],
            },
          ],
        },
        {
          id: "plantations",
          label: "Plantations",
          icon: "spa",
          route: "/plantations",
          permissions: ["Plantation.View"],
          children: [
            {
              label: "Crop cycles",
              route: "/crop-cycles",
              permissions: ["CropCycle.View"],
            },
          ],
        },
        {
          id: "activities",
          label: "Activities",
          icon: "event_note",
          route: "/activities",
          exactMatch: true,
          children: [
            {
              label: "Labor activities",
              route: "/activities/labor-activities",
              permissions: ["LaborActivity.View"],
            },
          ],
        },
      ],
    },
    {
      title: "Labor",
      items: [
        {
          id: "labor",
          label: "Labor",
          icon: "engineering",
          route: "/labor",
          exactMatch: true,
          children: [
            {
              label: "Attendance",
              route: "/labor/attendance",
              permissions: ["Attendance.View"],
            },
            {
              label: "Workers",
              route: "/labor/workers",
              permissions: ["Worker.View"],
            },
            {
              label: "Contractors",
              route: "/labor/contractors",
              permissions: ["Contractor.View"],
            },
            {
              label: "Wage rates",
              route: "/labor/wage-rates",
              permissions: ["WorkerWage.View"],
            },
          ],
        },
      ],
    },
    {
      title: "Agronomy",
      items: [
        {
          id: "crops",
          label: "Crop catalog",
          icon: "grass",
          route: "/crops",
          permissions: ["Crop.View"],
        },
      ],
    },
    {
      title: "Administration",
      items: [
        {
          id: "organization",
          label: "Organization",
          icon: "business",
          route: "/organization",
          permissions: ["Organization.View"],
        },
        {
          id: "administration",
          label: "Access & Roles",
          icon: "admin_panel_settings",
          route: "/administration",
          permissions: ["Users.View", "Roles.View", "Permissions.View"],
        },
      ],
    },
    {
      title: "Account",
      items: [
        {
          id: "settings",
          label: "Settings",
          icon: "settings",
          route: "/settings",
        },
      ],
    },
  ];

  readonly visibleNavigationGroups = computed(() =>
    this.navigationGroups
      .map((group) => ({
        ...group,
        items: group.items
          .map((item) => {
            const visibleChildren = item.children?.filter(
              (c) =>
                c.permissions === undefined ||
                this.permissionService.hasAny(c.permissions),
            );
            return {
              ...item,
              children: visibleChildren,
            };
          })
          .filter(
            (item) =>
              (item.permissions === undefined ||
                this.permissionService.hasAny(item.permissions)) &&
              (item.route !== undefined || (item.children && item.children.length > 0)),
          ),
      }))
      .filter((group) => group.items.length > 0),
  );

  constructor() {
    this.autoExpandActiveGroup(this.router.url);

    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((e) => {
        const url = e.urlAfterRedirects || e.url;
        this.currentUrl.set(url);
        this.autoExpandActiveGroup(url);
      });
  }

  isMenuExpanded(id: string): boolean {
    return this.expandedMenus().has(id);
  }

  toggleMenu(id: string): void {
    const current = new Set(this.expandedMenus());
    if (current.has(id)) {
      current.delete(id);
    } else {
      current.add(id);
    }
    this.expandedMenus.set(current);
  }

  toggleChevron(item: NavigationItem, event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.toggleMenu(item.id);
  }

  onParentClick(item: NavigationItem, event: MouseEvent): void {
    if (item.children && item.children.length > 0) {
      const current = new Set(this.expandedMenus());
      if (!current.has(item.id)) {
        current.add(item.id);
        this.expandedMenus.set(current);
      }
      if (!item.route) {
        event.preventDefault();
        this.toggleMenu(item.id);
      }
    }
  }

  isParentActive(item: NavigationItem): boolean {
    const url = this.currentUrl();
    if (item.route && (item.exactMatch ? url === item.route : url.startsWith(item.route))) {
      return true;
    }
    if (item.children) {
      return item.children.some((c) =>
        c.exactMatch ? url === c.route : url.startsWith(c.route),
      );
    }
    return false;
  }

  private autoExpandActiveGroup(url: string): void {
    const toExpand: string[] = [];
    for (const group of this.navigationGroups) {
      for (const item of group.items) {
        if (item.children && item.children.length > 0) {
          const matchesParent = item.route && (item.exactMatch ? url === item.route : url.startsWith(item.route));
          const matchesChild = item.children.some((c) =>
            c.exactMatch ? url === c.route : url.startsWith(c.route),
          );
          if (matchesParent || matchesChild) {
            toExpand.push(item.id);
          }
        }
      }
    }
    if (toExpand.length > 0) {
      const current = new Set(this.expandedMenus());
      let changed = false;
      for (const id of toExpand) {
        if (!current.has(id)) {
          current.add(id);
          changed = true;
        }
      }
      if (changed) {
        this.expandedMenus.set(current);
      }
    }
  }

  logout(): void {
    this.authService
      .logout()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => void this.router.navigateByUrl("/login"),
        error: () => void this.router.navigateByUrl("/login"),
      });
  }

  get userInitials(): string {
    const user = this.currentUser();
    return user === null
      ? "FM"
      : `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase();
  }
}
