import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatListModule } from "@angular/material/list";
import { MatSidenavModule } from "@angular/material/sidenav";
import { MatToolbarModule } from "@angular/material/toolbar";
import {
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from "@angular/router";

import { AuthService } from "../../core/auth/auth.service";
import { PermissionService } from "../../core/auth/permission.service";

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
    MatIconModule,
    MatListModule,
    MatSidenavModule,
    MatToolbarModule,
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

  readonly currentUser = this.authService.user;

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
