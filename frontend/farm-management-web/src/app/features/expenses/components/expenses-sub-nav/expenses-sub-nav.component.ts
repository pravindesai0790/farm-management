import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterLink, RouterLinkActive } from "@angular/router";
import { MatIconModule } from "@angular/material/icon";
import { PermissionService } from "../../../../core/auth/permission.service";

export interface ExpenseNavItem {
  readonly label: string;
  readonly route: string;
  readonly icon: string;
  readonly exact?: boolean;
  readonly permission?: string;
}

@Component({
  selector: "app-expenses-sub-nav",
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, MatIconModule],
  templateUrl: "./expenses-sub-nav.component.html",
  styleUrl: "./expenses-sub-nav.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExpensesSubNavComponent {
  readonly permissionService = inject(PermissionService);

  readonly navItems: readonly ExpenseNavItem[] = [
    {
      label: "Direct Expenses",
      route: "/expenses/direct",
      icon: "receipt_long",
      permission: "Expense.View",
    },
    {
      label: "Suppliers",
      route: "/expenses/suppliers",
      icon: "store",
      permission: "Supplier.View",
    },
    {
      label: "Expense Categories",
      route: "/expenses/categories",
      icon: "category",
      permission: "ExpenseCategory.View",
    },
  ];

  canAccess(item: ExpenseNavItem): boolean {
    if (!item.permission) return true;
    return this.permissionService.has(item.permission);
  }
}
