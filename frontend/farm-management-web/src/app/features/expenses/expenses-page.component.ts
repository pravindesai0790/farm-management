import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterLink } from "@angular/router";
import { MatCardModule } from "@angular/material/card";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatTooltipModule } from "@angular/material/tooltip";
import { PermissionService } from "../../core/auth/permission.service";

export interface ExpenseCard {
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
  selector: "app-expenses-page",
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
  ],
  templateUrl: "./expenses-page.component.html",
  styleUrl: "./expenses-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExpensesPageComponent {
  readonly permissionService = inject(PermissionService);

  readonly cards: readonly ExpenseCard[] = [
    {
      id: "direct-expenses",
      title: "Direct Expenses",
      category: "Direct Outflow",
      description:
        "Record and manage direct operational expenses, cash disbursements, farm overheads, and receipt attachments.",
      icon: "receipt_long",
      route: "/expenses/direct",
      status: "AVAILABLE",
      requiredPermission: "Expense.View",
      createPermission: "Expense.Create",
      highlights: [
        "Cash & direct spend",
        "Cycle & area linkages",
        "Receipt attachments",
        "Post & reversal workflows",
      ],
    },
    {
      id: "supplier-invoices",
      title: "Supplier Invoices",
      category: "Payables & Billing",
      description:
        "Manage incoming vendor purchase invoices, itemized goods receipts, payment statuses, and tax breakdowns.",
      icon: "request_quote",
      route: "/expenses/invoices",
      status: "AVAILABLE",
      requiredPermission: "PurchaseInvoice.View",
      createPermission: "PurchaseInvoice.Create",
      createRoute: "/expenses/invoices/new",
      highlights: [
        "Invoice drafting & posting",
        "Itemized line items",
        "Payment status tracking",
        "Tax & totals calculation",
      ],
    },
    {
      id: "supplier-payments",
      title: "Supplier Payments",
      category: "Disbursements",
      description:
        "Record outgoing disbursements to suppliers, settle outstanding invoices, and track payment references.",
      icon: "payments",
      route: "/expenses/payments",
      status: "AVAILABLE",
      requiredPermission: "SupplierPayment.View",
      createPermission: "SupplierPayment.Create",
      createRoute: "/expenses/payments/new",
      highlights: [
        "Payment recording",
        "Invoice allocations",
        "Bank / Cash / Cheque",
        "Receipt & transaction logs",
      ],
    },
    {
      id: "supplier-balances",
      title: "Supplier Balances",
      category: "Financial Summary",
      description:
        "Monitor accounts payable, total invoiced amounts, settled payments, and outstanding balances across all vendors.",
      icon: "account_balance",
      route: "/expenses/balances",
      status: "AVAILABLE",
      requiredPermission: "SupplierBalance.View",
      highlights: [
        "Accounts payable overview",
        "Total billed vs. paid",
        "Outstanding balances",
        "Aging summary",
      ],
    },
    {
      id: "expense-reports",
      title: "Expense Reports",
      category: "Financial Analytics",
      description:
        "View categorized expense breakdowns, monthly cost trends, top supplier spend analysis, and cycle costing.",
      icon: "analytics",
      route: "/expenses/reports",
      status: "AVAILABLE",
      requiredPermission: "Expense.Report.View",
      highlights: [
        "Category breakdown",
        "Spend analytics",
        "Monthly cost trends",
        "Cycle & plantation costing",
      ],
    },
    {
      id: "suppliers",
      title: "Suppliers",
      category: "Vendor Master",
      description:
        "Maintain vendor directory, contact persons, tax registration numbers, and standard payment terms.",
      icon: "store",
      route: "/expenses/suppliers",
      status: "AVAILABLE",
      requiredPermission: "Supplier.View",
      createPermission: "Supplier.Create",
      highlights: [
        "Vendor directory",
        "Tax & GST details",
        "Payment terms",
        "Contact management",
      ],
    },
    {
      id: "expense-categories",
      title: "Expense Categories",
      category: "Master Data",
      description:
        "Configure organizational expense classification, system defaults, custom categories, and status management.",
      icon: "category",
      route: "/expenses/categories",
      status: "AVAILABLE",
      requiredPermission: "ExpenseCategory.View",
      createPermission: "ExpenseCategory.Create",
      highlights: [
        "Category taxonomy",
        "System & custom categories",
        "Tax deduction rules",
        "Active status toggles",
      ],
    },
  ];

  canAccess(card: ExpenseCard): boolean {
    if (!card.requiredPermission) return true;
    return this.permissionService.has(card.requiredPermission);
  }

  canCreate(card: ExpenseCard): boolean {
    if (!card.createRoute || !card.createPermission) return false;
    return this.permissionService.has(card.createPermission);
  }
}
