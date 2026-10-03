import { OverdueInvoiceItem } from './expense-report.models';

export interface SupplierBalanceFilter {
  asOfDate?: string | null;
  supplierId?: string | null;
  farmId?: string | null;
  currencyId?: string | null;
}

export interface SupplierAgingTotals {
  currentNotDue: number;
  days1To30: number;
  days31To60: number;
  days61To90: number;
  daysOver90: number;
}

export interface SupplierOpenInvoiceItem {
  invoiceId: string;
  supplierInvoiceNumber: string;
  farmId?: string | null;
  farmName?: string | null;
  invoiceDate: string;
  dueDate: string;
  daysOverdue: number;
  totalAmount: number;
  amountPaid: number;
  outstandingAmount: number;
  paymentStatus: string;
  dueStatus: string;
}

export interface SupplierBalanceItem {
  supplierId: string;
  supplierName: string;
  contactPerson?: string | null;
  phone?: string | null;
  email?: string | null;
  totalOutstandingBalance: number;
  overdueBalance: number;
  aging: SupplierAgingTotals;
  openInvoices: SupplierOpenInvoiceItem[];
}

export interface SupplierBalanceCurrencySummary {
  currencyId: string;
  currencyCode: string;
  currencySymbol: string;
  totalOutstandingBalance: number;
  totalOverdueBalance: number;
  totalCurrentNotDueBalance: number;
  suppliersCount: number;
  openInvoicesCount: number;
  agingTotals: SupplierAgingTotals;
  supplierBalances: SupplierBalanceItem[];
}

export interface SupplierBalanceSummaryResponse {
  asOfDate: string;
  currencySummaries: SupplierBalanceCurrencySummary[];
}
