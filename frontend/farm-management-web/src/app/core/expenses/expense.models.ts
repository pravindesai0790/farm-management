import { PagedResponse } from '../models/paged-response.model';

export type ExpenseStatus = 'Draft' | 'Posted' | 'Reversed';

export interface Expense {
  readonly id: string;
  readonly organizationId: string;
  readonly farmId: string;
  readonly farmName: string;
  readonly expenseCategoryId: string;
  readonly expenseCategoryName: string;
  readonly expenseDate: string;
  readonly description: string;
  readonly amount: number;
  readonly currencyId: string;
  readonly currencyCode: string;
  readonly currencySymbol: string;
  readonly supplierId?: string | null;
  readonly supplierName?: string | null;
  readonly referenceNumber?: string | null;
  readonly farmAreaId?: string | null;
  readonly farmAreaName?: string | null;
  readonly plantationId?: string | null;
  readonly plantationName?: string | null;
  readonly cropCycleId?: string | null;
  readonly cropCycleName?: string | null;
  readonly cropCycleStageId?: string | null;
  readonly cropCycleStageName?: string | null;
  readonly attachmentReference?: string | null;
  readonly status: ExpenseStatus;
  readonly postedAt?: string | null;
  readonly postedBy?: string | null;
  readonly reversedAt?: string | null;
  readonly reversedBy?: string | null;
  readonly reversalReason?: string | null;
  readonly createdAt: string;
  readonly createdBy: string;
  readonly updatedAt?: string | null;
  readonly updatedBy?: string | null;
}

export interface CreateExpenseRequest {
  readonly farmId: string;
  readonly expenseCategoryId: string;
  readonly expenseDate: string;
  readonly description: string;
  readonly amount: number;
  readonly currencyId: string;
  readonly supplierId?: string | null;
  readonly referenceNumber?: string | null;
  readonly farmAreaId?: string | null;
  readonly plantationId?: string | null;
  readonly cropCycleId?: string | null;
  readonly cropCycleStageId?: string | null;
  readonly attachmentReference?: string | null;
}

export interface UpdateExpenseRequest {
  readonly farmId: string;
  readonly expenseCategoryId: string;
  readonly expenseDate: string;
  readonly description: string;
  readonly amount: number;
  readonly currencyId: string;
  readonly supplierId?: string | null;
  readonly referenceNumber?: string | null;
  readonly farmAreaId?: string | null;
  readonly plantationId?: string | null;
  readonly cropCycleId?: string | null;
  readonly cropCycleStageId?: string | null;
  readonly attachmentReference?: string | null;
}

export interface ReverseExpenseRequest {
  readonly reason: string;
}

export interface ExpenseFilter {
  readonly from?: string | null;
  readonly to?: string | null;
  readonly farmId?: string | null;
  readonly categoryId?: string | null;
  readonly supplierId?: string | null;
  readonly status?: ExpenseStatus | null;
  readonly search?: string | null;
}

export type ExpenseList = PagedResponse<Expense>;
