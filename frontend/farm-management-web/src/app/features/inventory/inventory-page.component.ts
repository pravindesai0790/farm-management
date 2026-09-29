import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterLink } from "@angular/router";
import { MatCardModule } from "@angular/material/card";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatChipsModule } from "@angular/material/chips";
import { MatTooltipModule } from "@angular/material/tooltip";
import { PermissionService } from "../../core/auth/permission.service";

export interface InventoryCard {
  readonly id: string;
  readonly title: string;
  readonly category: string;
  readonly description: string;
  readonly icon: string;
  readonly route?: string;
  readonly status: "AVAILABLE" | "UPCOMING";
  readonly highlights: readonly string[];
  readonly requiredPermission?: string;
  readonly createPermission?: string;
  readonly createRoute?: string;
}

@Component({
  selector: "app-inventory-page",
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatTooltipModule,
  ],
  templateUrl: "./inventory-page.component.html",
  styleUrl: "./inventory-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InventoryPageComponent {
  readonly permissionService = inject(PermissionService);

  readonly cards: readonly InventoryCard[] = [
    {
      id: "balances",
      title: "Stock Balances",
      category: "Stock & Balances",
      description:
        "Monitor real-time on-hand stock quantities across all farm storage sheds, warehouses, and catalog items.",
      icon: "inventory_2",
      route: "/inventory/balances",
      status: "AVAILABLE",
      requiredPermission: "InventoryStock.View",
      highlights: [
        "Real-time on-hand",
        "Farm & location breakdown",
        "Zero-stock indicator",
        "Direct transaction launcher",
      ],
    },
    {
      id: "ledger",
      title: "Stock Movement Ledger",
      category: "Audit & Transactions",
      description:
        "Authoritative append-only transaction ledger tracking stock receipts, issues, opening stock, adjustments, and transfers.",
      icon: "receipt_long",
      route: "/inventory/ledger",
      status: "AVAILABLE",
      requiredPermission: "InventoryStock.View",
      highlights: [
        "Complete audit trail",
        "Movement history",
        "Date & type filters",
        "Reference & purpose tracking",
      ],
    },
    {
      id: "items",
      title: "Inventory Items",
      category: "Catalog & Master Data",
      description:
        "Organization-wide catalog of agricultural inputs, fertilizers, agrochemicals, seeds, and unit of measures.",
      icon: "category",
      route: "/inventory/items",
      status: "AVAILABLE",
      requiredPermission: "InventoryItem.View",
      createPermission: "InventoryItem.Create",
      highlights: [
        "SKU & product codes",
        "Stock unit of measure",
        "Category classification",
        "Active catalog master",
      ],
    },
    {
      id: "locations",
      title: "Storage Locations",
      category: "Facilities & Storage",
      description:
        "Farm-specific storage facilities including chemical sheds, cold rooms, dry warehouses, and storage bins.",
      icon: "warehouse",
      route: "/inventory/locations",
      status: "AVAILABLE",
      requiredPermission: "StorageLocation.View",
      createPermission: "StorageLocation.Create",
      highlights: [
        "Farm facility master",
        "Storage sheds & bays",
        "Multi-farm partitioning",
        "Capacity tracking",
      ],
    },
    {
      id: "suppliers",
      title: "Suppliers & Purchase Orders",
      category: "Procurement",
      description:
        "Vendor catalog, agricultural input procurement orders, delivery tracking, and purchase invoicing.",
      icon: "local_shipping",
      status: "UPCOMING",
      highlights: [
        "Vendor catalog",
        "Purchase orders",
        "Delivery receipting",
        "Invoicing link",
      ],
    },
    {
      id: "batches",
      title: "Batch & Expiry Tracking",
      category: "Quality & Safety",
      description:
        "Track manufacturing lots, chemical expiration dates, seed germination ratings, and quarantine holds.",
      icon: "event_available",
      status: "UPCOMING",
      highlights: [
        "Lot / Batch numbers",
        "Expiry alerts",
        "Chemical safety",
        "Quarantine tracking",
      ],
    },
  ];

  canAccess(card: InventoryCard): boolean {
    if (!card.requiredPermission) return true;
    return this.permissionService.has(card.requiredPermission);
  }
}
