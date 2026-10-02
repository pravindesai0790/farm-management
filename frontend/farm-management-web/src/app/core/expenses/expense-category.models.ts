import { PagedResponse } from "../models/paged-response.model";

export interface ExpenseCategory {
  readonly id: string;
  readonly organizationId?: string | null;
  readonly name: string;
  readonly code?: string | null;
  readonly description?: string | null;
  readonly isSystemDefault: boolean;
  readonly isActive: boolean;
  readonly createdAt: string;
  readonly createdBy?: string | null;
  readonly updatedAt?: string | null;
  readonly updatedBy?: string | null;
}

export type ExpenseCategoryList = PagedResponse<ExpenseCategory>;

export interface CreateExpenseCategoryRequest {
  readonly name: string;
  readonly description?: string | null;
}

export interface UpdateExpenseCategoryRequest {
  readonly name: string;
  readonly description?: string | null;
}

export interface UpdateExpenseCategoryStatusRequest {
  readonly isActive: boolean;
}
