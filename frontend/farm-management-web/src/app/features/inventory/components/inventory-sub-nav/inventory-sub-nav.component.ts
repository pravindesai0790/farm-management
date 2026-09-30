import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterLink, RouterLinkActive } from "@angular/router";
import { MatIconModule } from "@angular/material/icon";
import { PermissionService } from "../../../../core/auth/permission.service";

export interface InventoryNavItem {
  readonly label: string;
  readonly route: string;
  readonly icon: string;
  readonly exact?: boolean;
  readonly permission?: string;
}

/**
  Reusable sub-navigation tab strip component for switching seamlessly between
  inventory features (Hub, Balances, Ledger, Items, Storage Locations).
 */
@Component({
  selector: "app-inventory-sub-nav",
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, MatIconModule],
  templateUrl: "./inventory-sub-nav.component.html",
  styleUrl: "./inventory-sub-nav.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InventorySubNavComponent {
  readonly permissionService = inject(PermissionService);

  readonly navItems: readonly InventoryNavItem[] = [
    {
      label: "Hub Overview",
      route: "/inventory",
      icon: "apps",
      exact: true,
    },
    {
      label: "Stock Balances",
      route: "/inventory/balances",
      icon: "inventory_2",
      permission: "InventoryStock.View",
    },
    {
      label: "Movement Ledger",
      route: "/inventory/ledger",
      icon: "receipt_long",
      permission: "InventoryStock.View",
    },
    {
      label: "Catalog Items",
      route: "/inventory/items",
      icon: "category",
      permission: "InventoryItem.View",
    },
    {
      label: "Storage Locations",
      route: "/inventory/locations",
      icon: "warehouse",
      permission: "StorageLocation.View",
    },
  ];

  canAccess(item: InventoryNavItem): boolean {
    if (!item.permission) return true;
    return this.permissionService.has(item.permission);
  }
}
