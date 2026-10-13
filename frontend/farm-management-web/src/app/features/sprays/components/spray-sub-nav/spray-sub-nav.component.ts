import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterLink, RouterLinkActive } from "@angular/router";
import { MatIconModule } from "@angular/material/icon";
import { PermissionService } from "../../../../core/auth/permission.service";

export interface SprayNavItem {
  readonly label: string;
  readonly route: string;
  readonly icon: string;
  readonly exact?: boolean;
  readonly permission?: string;
  readonly anyPermissions?: readonly string[];
}

@Component({
  selector: "app-spray-sub-nav",
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, MatIconModule],
  templateUrl: "./spray-sub-nav.component.html",
  styleUrl: "./spray-sub-nav.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SpraySubNavComponent {
  readonly permissionService = inject(PermissionService);

  readonly navItems: readonly SprayNavItem[] = [
    {
      label: "Spray Applications",
      route: "/sprays",
      icon: "pest_control",
      permission: "Spray.View",
      exact: true,
    },
    {
      label: "Protection Products",
      route: "/sprays/products",
      icon: "science",
      permission: "PlantProtectionProduct.View",
    },
    {
      label: "Spray Masters",
      route: "/sprays/master-data",
      icon: "tune",
      anyPermissions: [
        "ProductType.View",
        "Target.View",
        "ApplicationMethod.View",
      ],
    },
  ];

  canAccess(item: SprayNavItem): boolean {
    if (item.permission && !this.permissionService.has(item.permission)) {
      return false;
    }
    if (item.anyPermissions && item.anyPermissions.length > 0) {
      if (typeof this.permissionService.hasAny === "function") {
        return this.permissionService.hasAny(item.anyPermissions);
      }
      return item.anyPermissions.some((p) => this.permissionService.has(p));
    }
    return true;
  }
}
