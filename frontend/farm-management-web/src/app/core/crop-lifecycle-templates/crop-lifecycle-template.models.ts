import { PagedResponse } from "../models/paged-response.model";

export interface CropLifecycleStage {
  id: string;
  lifecycleTemplateId: string;
  stageName: string;
  sequenceNumber: number;
  expectedDurationDays: number | null;
  description: string | null;
  isActive: boolean;
}

export interface CropLifecycleTemplate {
  id: string;
  organizationId: string | null;
  cropId: string;
  cropName: string;
  name: string;
  description: string | null;
  isDefault: boolean;
  isSystem: boolean;
  isActive: boolean;
  createdAt: string;
  createdBy: string | null;
  updatedAt: string | null;
  updatedBy: string | null;
  stages: readonly CropLifecycleStage[];
}

export type CropLifecycleTemplateList = PagedResponse<CropLifecycleTemplate>;

export interface CropLifecycleTemplateQuery {
  page?: number;
  pageSize?: number;
  cropId?: string | null;
  isActive?: boolean | null;
}

export interface CreateCropLifecycleStageRequest {
  stageName: string;
  sequenceNumber: number;
  expectedDurationDays?: number | null;
  description?: string | null;
}

export interface UpdateCropLifecycleStageRequest {
  stageName: string;
  sequenceNumber: number;
  expectedDurationDays?: number | null;
  description?: string | null;
}

export interface CreateCropLifecycleTemplateRequest {
  cropId: string;
  name: string;
  description?: string | null;
  isDefault?: boolean;
  stages?: readonly CreateCropLifecycleStageRequest[];
}

export interface UpdateCropLifecycleTemplateRequest {
  cropId: string;
  name: string;
  description?: string | null;
  isDefault?: boolean;
}

export interface ReorderCropLifecycleStageItem {
  stageId: string;
  sequenceNumber: number;
}

export interface ReorderCropLifecycleStagesRequest {
  stages: readonly ReorderCropLifecycleStageItem[];
}
