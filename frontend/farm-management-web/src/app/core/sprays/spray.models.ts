export type SprayStatus = 'Draft' | 'Scheduled' | 'InProgress' | 'Completed' | 'Cancelled';

export interface SprayListItem {
  readonly id: string;
  readonly referenceNumber: string;
  readonly farmId: string;
  readonly farmName: string;
  readonly farmAreaId: string | null;
  readonly farmAreaName: string | null;
  readonly plantationId: string | null;
  readonly plantationName: string | null;
  readonly cropCycleId: string | null;
  readonly cropCycleName: string | null;
  readonly cropCycleStageId: string | null;
  readonly cropCycleStageName: string | null;
  readonly status: SprayStatus;
  readonly statusName: string;
  readonly isOverdue: boolean;
  readonly plannedDate: string | null;
  readonly scheduledDateTime: string | null;
  readonly actualApplicationDateTime: string | null;
  readonly targetId: string | null;
  readonly targetName: string | null;
  readonly applicationMethodId: string | null;
  readonly applicationMethodName: string | null;
  readonly productCount: number;
  readonly createdAt: string;
}

export interface SprayListQuery {
  farmId?: string;
  farmAreaId?: string;
  plantationId?: string;
  cropCycleId?: string;
  cropCycleStageId?: string;
  status?: string;
  targetId?: string;
  fromDate?: string;
  toDate?: string;
  search?: string;
  includeOverdue?: boolean;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: string;
}

export interface TargetResponse {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly targetType: string;
  readonly description?: string | null;
  readonly displayOrder: number;
  readonly isSystem: boolean;
  readonly isActive: boolean;
}

export interface ProductTypeResponse {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly description?: string | null;
  readonly displayOrder: number;
  readonly isSystem: boolean;
  readonly isActive: boolean;
}

export interface ApplicationMethodResponse {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly description?: string | null;
  readonly displayOrder: number;
  readonly isSystem: boolean;
  readonly isActive: boolean;
}

export interface CancelSprayRequest {
  readonly cancellationReason: string;
}

export interface ScheduleSprayRequest {
  readonly scheduledDateTime: string;
  readonly plannedDate?: string | null;
}

export interface RescheduleSprayRequest {
  readonly scheduledDateTime: string;
}

export interface SprayProductLookupResponse {
  readonly inventoryItemId: string;
  readonly name: string;
  readonly sku?: string | null;
  readonly stockUnitId: string;
  readonly stockUnitName: string;
  readonly stockUnitCode?: string | null;
  readonly stockUnitSymbol?: string | null;
  readonly productTypeId: string;
  readonly productTypeCode: string;
  readonly productTypeName: string;
  readonly activeIngredient?: string | null;
  readonly manufacturer?: string | null;
  readonly plantProtectionProductId: string;
}

export interface SprayStorageLocationLookupResponse {
  readonly storageLocationId: string;
  readonly storageLocationName: string;
  readonly currentStock: number;
  readonly hasStock: boolean;
  readonly stockUnitId?: string | null;
  readonly stockUnitName?: string | null;
}
