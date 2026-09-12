import { PagedResponse } from "../models/paged-response.model";

export type AttendanceType =
  | "FULL_DAY"
  | "HALF_DAY"
  | "HOURLY"
  | "NOT_WORKED";

export type AttendanceStatus = "DRAFT" | "FINALIZED";

export interface AttendanceEligibleWorker {
  readonly workerId: string;
  readonly displayName: string;
  readonly firstName: string;
  readonly lastName?: string | null;
  readonly gender: string;
  readonly employmentType: string;
  readonly mobileNumber?: string | null;
  readonly laborCategoryId?: string | null;
  readonly laborCategoryName?: string | null;
  readonly contractorId?: string | null;
  readonly contractorName?: string | null;
  readonly joiningDate?: string | null;
  readonly leavingDate?: string | null;
  readonly assignmentId: string;
  readonly farmId: string;
  readonly assignedFrom: string;
  readonly assignedTo?: string | null;
}

export type AttendanceEligibleWorkerList = PagedResponse<AttendanceEligibleWorker>;

export interface AttendanceRecord {
  readonly id: string;
  readonly organizationId: string;
  readonly farmId: string;
  readonly farmName: string;
  readonly workerId: string;
  readonly workerDisplayName: string;
  readonly workerFirstName: string;
  readonly workerLastName?: string | null;
  readonly gender: string;
  readonly laborCategoryId?: string | null;
  readonly laborCategoryName?: string | null;
  readonly employmentType: string;
  readonly attendanceDate: string;
  readonly attendanceType: AttendanceType | string;
  readonly workingHours?: number | null;
  readonly calculatedRate?: number | null;
  readonly calculatedAmount?: number | null;
  readonly currencyId?: string | null;
  readonly currencyCode?: string | null;
  readonly currencySymbol?: string | null;
  readonly status: AttendanceStatus | string;
  readonly notes?: string | null;
  readonly finalizedAt?: string | null;
  readonly finalizedBy?: string | null;
  readonly createdAt: string;
  readonly createdBy: string;
  readonly updatedAt?: string | null;
  readonly updatedBy?: string | null;
}

export interface DailyAttendanceSummary {
  readonly totalCount: number;
  readonly workedCount: number;
  readonly fullDayCount: number;
  readonly halfDayCount: number;
  readonly hourlyCount: number;
  readonly notWorkedCount: number;
  readonly estimatedEarnings: number;
  readonly status: AttendanceStatus | string;
}

export interface DailyAttendanceResponse {
  readonly farmId: string;
  readonly farmName: string;
  readonly attendanceDate: string;
  readonly summary: DailyAttendanceSummary;
  readonly records: readonly AttendanceRecord[];
}

export interface DailyDraftAttendanceItemRequest {
  readonly id?: string | null;
  readonly workerId: string;
  readonly attendanceType: AttendanceType | string;
  readonly workingHours?: number | null;
  readonly notes?: string | null;
}

export interface SaveDailyDraftAttendanceBatchRequest {
  readonly farmId: string;
  readonly attendanceDate: string;
  readonly items: readonly DailyDraftAttendanceItemRequest[];
  readonly removeOmittedDrafts?: boolean;
}

export interface CreateDraftAttendanceRequest {
  readonly farmId: string;
  readonly workerId: string;
  readonly attendanceDate: string;
  readonly attendanceType: AttendanceType | string;
  readonly workingHours?: number | null;
  readonly notes?: string | null;
}

export interface UpdateDraftAttendanceRequest {
  readonly attendanceType: AttendanceType | string;
  readonly workingHours?: number | null;
  readonly notes?: string | null;
}

export interface AttendanceWagePreviewRequest {
  readonly workerId: string;
  readonly attendanceDate: string;
  readonly attendanceType: AttendanceType | string;
  readonly workingHours?: number | null;
  readonly farmId?: string | null;
}

export interface AttendanceWagePreviewResponse {
  readonly workerId: string;
  readonly workerDisplayName: string;
  readonly gender: string;
  readonly attendanceDate: string;
  readonly attendanceType: string;
  readonly resolvedWageType?: string | null;
  readonly wageType?: string | null;
  readonly rate: number;
  readonly quantity: number;
  readonly workingHours?: number | null;
  readonly calculatedAmount: number;
  readonly currencyId?: string | null;
  readonly currencyCode: string;
  readonly currencySymbol: string;
  readonly currency: string;
  readonly isEarningEligible: boolean;
  readonly isWorkerEligible: boolean;
  readonly ineligibilityReason?: string | null;
}

export interface AttendanceWagePreviewBatchItemRequest {
  readonly workerId: string;
  readonly attendanceType: AttendanceType | string;
  readonly workingHours?: number | null;
}

export interface AttendanceWagePreviewBatchRequest {
  readonly attendanceDate: string;
  readonly items: readonly AttendanceWagePreviewBatchItemRequest[];
  readonly farmId?: string | null;
}

export interface AttendanceWagePreviewBatchResponse {
  readonly attendanceDate: string;
  readonly totalCount: number;
  readonly workedCount: number;
  readonly fullDayCount: number;
  readonly halfDayCount: number;
  readonly hourlyCount: number;
  readonly notWorkedCount: number;
  readonly totalEstimatedEarnings: number;
  readonly items: readonly AttendanceWagePreviewResponse[];
}

export interface FinalizeAttendanceRequest {
  readonly farmId: string;
  readonly attendanceDate: string;
  readonly attendanceIds?: readonly string[] | null;
}

export interface FinalizeAttendanceResponse {
  readonly farmId: string;
  readonly farmName: string;
  readonly attendanceDate: string;
  readonly finalizedCount: number;
  readonly paidCount: number;
  readonly notWorkedCount: number;
  readonly totalEarnings: number;
  readonly summary: DailyAttendanceSummary;
  readonly records: readonly AttendanceRecord[];
}

export interface AttendanceGridRow {
  readonly id?: string | null;
  readonly workerId: string;
  readonly workerDisplayName: string;
  readonly workerFirstName: string;
  readonly workerLastName?: string | null;
  readonly gender: string;
  readonly mobileNumber?: string | null;
  readonly laborCategoryName?: string | null;
  readonly contractorName?: string | null;
  readonly employmentType: string;
  attendanceType: AttendanceType;
  workingHours?: number | null;
  calculatedRate?: number | null;
  calculatedAmount?: number | null;
  currencySymbol?: string | null;
  status: AttendanceStatus;
  notes?: string | null;
  isModified?: boolean;
}

export const ATTENDANCE_TYPE_OPTIONS: readonly {
  readonly value: AttendanceType;
  readonly label: string;
  readonly description: string;
}[] = [
  { value: "FULL_DAY", label: "Full Day", description: "Standard full day rate" },
  { value: "HALF_DAY", label: "Half Day", description: "Standard half day rate" },
  { value: "HOURLY", label: "Hourly", description: "Rate multiplied by hours" },
  { value: "NOT_WORKED", label: "Not Worked", description: "No earnings generated" },
];

export function formatAttendanceType(type?: string | null): string {
  switch (type?.toUpperCase()) {
    case "FULL_DAY":
      return "Full Day";
    case "HALF_DAY":
      return "Half Day";
    case "HOURLY":
      return "Hourly";
    case "NOT_WORKED":
      return "Not Worked";
    default:
      return type || "—";
  }
}

export function formatAttendanceStatus(status?: string | null): string {
  switch (status?.toUpperCase()) {
    case "DRAFT":
      return "Draft";
    case "FINALIZED":
      return "Finalized";
    default:
      return status || "—";
  }
}

export function getAttendanceTypeBadgeClass(type?: string | null): string {
  switch (type?.toUpperCase()) {
    case "FULL_DAY":
      return "type-badge-full-day";
    case "HALF_DAY":
      return "type-badge-half-day";
    case "HOURLY":
      return "type-badge-hourly";
    case "NOT_WORKED":
      return "type-badge-not-worked";
    default:
      return "type-badge-default";
  }
}
