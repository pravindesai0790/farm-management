export interface ExpenseReportFilter {
  from?: string | null;
  to?: string | null;
  farmId?: string | null;
  categoryId?: string | null;
  cropCycleId?: string | null;
  currencyId?: string | null;
}

export interface OverdueInvoiceItem {
  invoiceId: string;
  supplierInvoiceNumber: string;
  supplierId: string;
  supplierName: string;
  farmId?: string | null;
  farmName?: string | null;
  invoiceDate: string;
  dueDate: string;
  daysOverdue: number;
  totalAmount: number;
  amountPaid: number;
  outstandingAmount: number;
  currencyId: string;
  currencyCode: string;
  currencySymbol: string;
}

export interface ExpenseCategorySummaryItem {
  categoryId: string;
  categoryName: string;
  directExpensesAmount: number;
  invoicedAmount: number;
  totalAmount: number;
  percentage: number;
}

export interface ExpenseFarmSummaryItem {
  farmId: string;
  farmName: string;
  directExpensesAmount: number;
  invoicedAmount: number;
  totalAmount: number;
  percentage: number;
}

export interface ExpenseMonthlyTrendItem {
  year: number;
  month: number;
  monthLabel: string;
  directExpensesAmount: number;
  invoicedAmount: number;
  totalAmount: number;
}

export interface ExpenseReportCurrencySummary {
  currencyId: string;
  currencyCode: string;
  currencySymbol: string;
  totalDirectExpensesAmount: number;
  directExpensesCount: number;
  totalInvoicedAmount: number;
  invoicesCount: number;
  totalSettledAmount: number;
  totalOutstandingPayables: number;
  grandTotalExpenses: number;
  categoryBreakdown: ExpenseCategorySummaryItem[];
  farmBreakdown: ExpenseFarmSummaryItem[];
  monthlyTrend: ExpenseMonthlyTrendItem[];
  topOverdueInvoices: OverdueInvoiceItem[];
}

export interface ExpenseReportSummaryResponse {
  filter: ExpenseReportFilter;
  currencySummaries: ExpenseReportCurrencySummary[];
}
