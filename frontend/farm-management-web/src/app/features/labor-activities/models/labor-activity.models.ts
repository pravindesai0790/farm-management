import { PagedResponse } from "../../../core/models/paged-response.model";

export interface NamedReference {
  readonly id: string;
  readonly name: string;
}

export interface StageReference {
  readonly id: string;
  readonly name: string;
  readonly sequenceNumber: number;
}

export type LaborActivityStatus = "DRAFT" | "COMPLETED" | "CANCELLED";

export interface LaborActivity {
  readonly id: string;
  readonly activityDate: string;
  readonly farm: NamedReference;
  readonly farmArea: NamedReference | null;
  readonly plantation: NamedReference | null;
  readonly cropCycle: NamedReference | null;
  readonly cropCycleStage?: StageReference | null;
  readonly activityType: NamedReference;
  readonly status: LaborActivityStatus;
  readonly description: string | null;
  readonly cancellationReason?: string | null;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export type LaborActivityList = PagedResponse<LaborActivity>;

export interface LaborActivityFilter {
  readonly farmId?: string;
  readonly farmAreaId?: string;
  readonly plantationId?: string;
  readonly cropCycleId?: string;
  readonly cropCycleStageId?: string;
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

export interface CreateLaborActivityRequest {
  readonly activityDate: string;
  readonly farmId: string;
  readonly farmAreaId?: string | null;
  readonly plantationId?: string | null;
  readonly cropCycleId?: string | null;
  readonly cropCycleStageId?: string | null;
  readonly laborActivityTypeId: string;
  readonly description?: string | null;
  readonly status?: LaborActivityStatus;
}

export interface UpdateLaborActivityRequest {
  readonly activityDate: string;
  readonly farmId: string;
  readonly farmAreaId?: string | null;
  readonly plantationId?: string | null;
  readonly cropCycleId?: string | null;
  readonly cropCycleStageId?: string | null;
  readonly laborActivityTypeId: string;
  readonly description?: string | null;
  readonly status?: LaborActivityStatus;
}
