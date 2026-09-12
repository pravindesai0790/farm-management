import { PagedResponse } from "../models/paged-response.model";

export type Gender = "MALE" | "FEMALE" | "OTHER";

export type EmploymentType =
  | "PERMANENT"
  | "SEASONAL"
  | "DAILY_WAGE"
  | "CONTRACT";

export interface WorkerItem {
  readonly id: string;
  readonly organizationId: string;
  readonly firstName: string;
  readonly lastName?: string | null;
  readonly displayName: string;
  readonly gender: Gender | string;
  readonly employmentType: EmploymentType | string;
  readonly mobileNumber?: string | null;
  readonly alternateMobileNumber?: string | null;
  readonly laborCategoryId?: string | null;
  readonly laborCategoryName?: string | null;
  readonly contractorId?: string | null;
  readonly contractorName?: string | null;
  readonly joiningDate?: string | null;
  readonly leavingDate?: string | null;
  readonly isActive: boolean;
  readonly createdAt: string;
  readonly updatedAt?: string | null;
}

export interface WorkerDetail {
  readonly id: string;
  readonly organizationId: string;
  readonly firstName: string;
  readonly lastName?: string | null;
  readonly displayName: string;
  readonly gender: Gender | string;
  readonly employmentType: EmploymentType | string;
  readonly mobileNumber?: string | null;
  readonly alternateMobileNumber?: string | null;
  readonly laborCategoryId?: string | null;
  readonly laborCategoryName?: string | null;
  readonly contractorId?: string | null;
  readonly contractorName?: string | null;
  readonly joiningDate?: string | null;
  readonly leavingDate?: string | null;
  readonly notes?: string | null;
  readonly isActive: boolean;
  readonly createdAt: string;
  readonly createdBy: string;
  readonly updatedAt?: string | null;
  readonly updatedBy?: string | null;
}

export type WorkerList = PagedResponse<WorkerItem>;

export const GENDER_OPTIONS: readonly { readonly value: string; readonly label: string }[] = [
  { value: "all", label: "All genders" },
  { value: "MALE", label: "Male" },
  { value: "FEMALE", label: "Female" },
  { value: "OTHER", label: "Other" },
];

export const EMPLOYMENT_TYPE_OPTIONS: readonly { readonly value: string; readonly label: string }[] = [
  { value: "all", label: "All employment types" },
  { value: "PERMANENT", label: "Permanent" },
  { value: "SEASONAL", label: "Seasonal" },
  { value: "DAILY_WAGE", label: "Daily wage" },
  { value: "CONTRACT", label: "Contract" },
];

export const WORKER_GENDER_OPTIONS: readonly { readonly value: Gender; readonly label: string }[] = [
  { value: "MALE", label: "Male" },
  { value: "FEMALE", label: "Female" },
  { value: "OTHER", label: "Other" },
];

export const WORKER_EMPLOYMENT_TYPE_OPTIONS: readonly { readonly value: EmploymentType; readonly label: string }[] = [
  { value: "PERMANENT", label: "Permanent" },
  { value: "SEASONAL", label: "Seasonal" },
  { value: "DAILY_WAGE", label: "Daily wage" },
  { value: "CONTRACT", label: "Contract" },
];

export interface CreateWorkerRequest {
  readonly firstName: string;
  readonly lastName?: string | null;
  readonly displayName?: string | null;
  readonly gender: Gender | string;
  readonly employmentType: EmploymentType | string;
  readonly mobileNumber?: string | null;
  readonly alternateMobileNumber?: string | null;
  readonly laborCategoryId?: string | null;
  readonly contractorId?: string | null;
  readonly joiningDate?: string | null;
  readonly leavingDate?: string | null;
  readonly notes?: string | null;
}

export interface UpdateWorkerRequest {
  readonly firstName: string;
  readonly lastName?: string | null;
  readonly displayName?: string | null;
  readonly gender: Gender | string;
  readonly employmentType: EmploymentType | string;
  readonly mobileNumber?: string | null;
  readonly alternateMobileNumber?: string | null;
  readonly laborCategoryId?: string | null;
  readonly contractorId?: string | null;
  readonly joiningDate?: string | null;
  readonly leavingDate?: string | null;
  readonly notes?: string | null;
}

export interface ContractorItem {
  readonly id: string;
  readonly organizationId: string;
  readonly name: string;
  readonly contactPerson?: string | null;
  readonly phoneNumber?: string | null;
  readonly email?: string | null;
  readonly address?: string | null;
  readonly notes?: string | null;
  readonly isActive: boolean;
  readonly workerCount: number;
  readonly createdAt: string;
  readonly createdBy: string;
  readonly updatedAt?: string | null;
  readonly updatedBy?: string | null;
}

export interface LaborCategoryItem {
  readonly id: string;
  readonly organizationId?: string | null;
  readonly name: string;
  readonly description?: string | null;
  readonly isSystem: boolean;
  readonly isActive: boolean;
  readonly workerCount: number;
  readonly createdAt: string;
  readonly createdBy?: string | null;
  readonly updatedAt?: string | null;
  readonly updatedBy?: string | null;
}

export function formatGender(gender: string): string {
  switch (gender?.toUpperCase()) {
    case "MALE":
      return "Male";
    case "FEMALE":
      return "Female";
    case "OTHER":
      return "Other";
    default:
      return gender || "—";
  }
}

export function formatEmploymentType(type: string): string {
  switch (type?.toUpperCase()) {
    case "PERMANENT":
      return "Permanent";
    case "SEASONAL":
      return "Seasonal";
    case "DAILY_WAGE":
      return "Daily wage";
    case "CONTRACT":
      return "Contract";
    default:
      return type || "—";
  }
}

export interface WorkerFarmAssignment {
  readonly id: string;
  readonly organizationId: string;
  readonly workerId: string;
  readonly workerName: string;
  readonly farmId: string;
  readonly farmCode: string;
  readonly farmName: string;
  readonly assignedFrom: string;
  readonly assignedTo?: string | null;
  readonly isActive: boolean;
  readonly notes?: string | null;
  readonly createdAt: string;
  readonly createdBy: string;
  readonly updatedAt?: string | null;
  readonly updatedBy?: string | null;
}

export interface CreateWorkerFarmAssignmentRequest {
  readonly farmId: string;
  readonly assignedFrom: string;
  readonly assignedTo?: string | null;
  readonly notes?: string | null;
}

export interface UpdateWorkerFarmAssignmentRequest {
  readonly assignedFrom: string;
  readonly assignedTo?: string | null;
  readonly notes?: string | null;
}

export interface EndWorkerFarmAssignmentRequest {
  readonly endDate?: string | null;
  readonly notes?: string | null;
}

export interface LaborWageRate {
  readonly id: string;
  readonly organizationId: string;
  readonly gender: Gender | string;
  readonly wageType: string;
  readonly wageRate: number;
  readonly currencyId?: string;
  readonly currencyCode?: string;
  readonly currencySymbol?: string;
  readonly effectiveFrom: string;
  readonly effectiveTo?: string | null;
  readonly isActive: boolean;
  readonly notes?: string | null;
  readonly createdAt?: string;
  readonly updatedAt?: string | null;
}

export interface CreateLaborWageRateRequest {
  readonly gender: string;
  readonly wageType: string;
  readonly wageRate: number;
  readonly currencyId: string;
  readonly effectiveFrom: string;
  readonly effectiveTo?: string | null;
  readonly notes?: string | null;
}

export interface UpdateLaborWageRateRequest {
  readonly wageRate: number;
  readonly currencyId: string;
  readonly effectiveFrom: string;
  readonly effectiveTo?: string | null;
  readonly notes?: string | null;
}

export interface CurrencyItem {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly symbol: string;
  readonly isSystem: boolean;
  readonly isActive: boolean;
  readonly displayOrder: number;
}

export const WAGE_TYPE_OPTIONS: readonly { readonly value: string; readonly label: string }[] = [
  { value: "FULL_DAY", label: "Full Day" },
  { value: "HALF_DAY", label: "Half Day" },
  { value: "HOURLY", label: "Hourly" },
  { value: "MONTHLY", label: "Monthly" },
] as const;

export function formatWageType(wageType: string | null | undefined): string {
  switch ((wageType || "").toUpperCase()) {
    case "FULL_DAY":
      return "Full Day";
    case "HALF_DAY":
      return "Half Day";
    case "HOURLY":
      return "Hourly";
    case "MONTHLY":
      return "Monthly";
    default:
      return wageType || "—";
  }
}

export type RateLifecycle = "CURRENT" | "FUTURE" | "HISTORICAL";

export function getRateLifecycle(
  rate: Pick<LaborWageRate, "effectiveFrom" | "effectiveTo">,
  asOfDate: string = new Date().toISOString().slice(0, 10),
): RateLifecycle {
  if (rate.effectiveFrom > asOfDate) {
    return "FUTURE";
  }
  if (rate.effectiveTo && rate.effectiveTo < asOfDate) {
    return "HISTORICAL";
  }
  return "CURRENT";
}

export interface WorkerPayment {
  readonly id: string;
  readonly organizationId: string;
  readonly workerId: string;
  readonly paymentDate: string;
  readonly paymentType: "ADVANCE" | "PAYOUT" | "ADJUSTMENT" | string;
  readonly amount: number;
  readonly currencyCode?: string;
  readonly paymentMethod: "CASH" | "BANK_TRANSFER" | "UPI" | "CHEQUE" | "OTHER" | string;
  readonly referenceNumber?: string | null;
  readonly paymentPeriodFrom?: string | null;
  readonly paymentPeriodTo?: string | null;
  readonly status: "PENDING" | "COMPLETED" | "CANCELLED" | string;
  readonly notes?: string | null;
  readonly createdAt: string;
}


