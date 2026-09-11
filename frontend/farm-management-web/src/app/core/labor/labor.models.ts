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
