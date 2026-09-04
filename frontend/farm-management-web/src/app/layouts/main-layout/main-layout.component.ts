import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
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
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from "@angular/router";

import { AuthService } from "../../core/auth/auth.service";
import { PermissionService } from "../../core/auth/permission.service";
import { BreadcrumbService } from "../../core/breadcrumb/breadcrumb.service";

export interface NavigationItem {
  readonly label: string;
  readonly icon: string;
  readonly route: string;
  readonly permissions?: readonly string[];
  readonly exactMatch?: boolean;
  readonly isSubItem?: boolean;
  readonly badge?: string;
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
          label: "Farms",
          icon: "landscape",
          route: "/farms",
          permissions: ["Farm.View"],
        },
        {
          label: "Farm areas",
          icon: "grid_view",
          route: "/farm-areas",
          permissions: ["FarmArea.View"],
          isSubItem: true,
        },
        {
          label: "Plantations",
          icon: "spa",
          route: "/plantations",
          permissions: ["Plantation.View"],
        },
        {
          label: "Crop cycles",
          icon: "calendar_month",
          route: "/crop-cycles",
          permissions: ["CropCycle.View"],
          isSubItem: true,
        },
        {
          label: "Activities",
          icon: "event_note",
          route: "/activities",
          isSubItem: true,
        },
      ],
    },
    {
      title: "Agronomy",
      items: [
        {
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
          label: "Organization",
          icon: "business",
          route: "/organization",
          permissions: ["Organization.View"],
        },
        {
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
        items: group.items.filter(
          (item) =>
            item.permissions === undefined ||
            this.permissionService.hasAny(item.permissions),
        ),
      }))
      .filter((group) => group.items.length > 0),
  );

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
