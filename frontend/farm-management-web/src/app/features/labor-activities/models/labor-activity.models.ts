import { PagedResponse } from "../../../core/models/paged-response.model";

export interface NamedReference {
  readonly id: string;
  readonly name: string;
}

export type LaborActivityStatus = "DRAFT" | "COMPLETED" | "CANCELLED";

export interface LaborActivity {
  readonly id: string;
  readonly activityDate: string;
  readonly farm: NamedReference;
  readonly farmArea: NamedReference | null;
  readonly plantation: NamedReference | null;
  readonly cropCycle: NamedReference | null;
  readonly activityType: NamedReference;
  readonly workerCount: number;
  readonly totalWorkingHours: number | null;
  readonly costAmount: number | null;
  readonly currency: string;
  readonly status: LaborActivityStatus;
  readonly description: string | null;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export type LaborActivityList = PagedResponse<LaborActivity>;

export interface LaborActivityFilter {
  readonly farmId?: string;
  readonly farmAreaId?: string;
  readonly plantationId?: string;
  readonly cropCycleId?: string;
  readonly activityTypeId?: string;
  readonly fromDate?: string;
  readonly toDate?: string;
  readonly status?: string;
  readonly page: number;
  readonly pageSize: number;
}

export interface CancelLaborActivityRequest {
  readonly reason: string;
}
