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

export interface SprayProductDto {
  readonly id: string;
  readonly inventoryItemId: string;
  readonly inventoryItemName: string;
  readonly inventoryItemSku?: string | null;
  readonly stockUnitId: string;
  readonly stockUnitName: string;
  readonly stockUnitSymbol?: string | null;
  readonly storageLocationId?: string | null;
  readonly storageLocationName?: string | null;
  readonly plannedQuantity?: number | null;
  readonly actualQuantity?: number | null;
  readonly dosage?: string | null;
}

export interface SprayDetailsResponse {
  readonly id: string;
  readonly referenceNumber: string | null;
  readonly organizationId: string;
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
  readonly plannedArea: number | null;
  readonly plannedAreaUnitId: string | null;
  readonly plannedAreaUnitName: string | null;
  readonly actualTreatedArea: number | null;
  readonly actualTreatedAreaUnitId: string | null;
  readonly actualTreatedAreaUnitName: string | null;
  readonly waterQuantity: number | null;
  readonly waterUnitId: string | null;
  readonly waterUnitName: string | null;
  readonly targetId: string | null;
  readonly targetName: string | null;
  readonly targetType: string | null;
  readonly applicationMethodId: string | null;
  readonly applicationMethodName: string | null;
  readonly purposeReason: string | null;
  readonly cancellationReason: string | null;
  readonly products: readonly SprayProductDto[];
  readonly createdAt: string;
  readonly createdBy: string;
  readonly createdByName?: string | null;
  readonly updatedAt: string | null;
  readonly updatedBy: string | null;
  readonly updatedByName?: string | null;
}

export interface SprayProductItemRequest {
  readonly inventoryItemId: string;
  readonly plannedQuantity?: number | null;
  readonly dosage?: string | null;
}

export interface CreateSprayDraftRequest {
  readonly farmId: string;
  readonly farmAreaId?: string | null;
  readonly plantationId?: string | null;
  readonly cropCycleId?: string | null;
  readonly cropCycleStageId?: string | null;
  readonly plannedDate?: string | null;
  readonly plannedArea?: number | null;
  readonly plannedAreaUnitId?: string | null;
  readonly waterQuantity?: number | null;
  readonly waterUnitId?: string | null;
  readonly targetId?: string | null;
  readonly applicationMethodId?: string | null;
  readonly purposeReason?: string | null;
  readonly products?: readonly SprayProductItemRequest[] | null;
}

export interface UpdateSprayDraftRequest {
  readonly farmId: string;
  readonly farmAreaId?: string | null;
  readonly plantationId?: string | null;
  readonly cropCycleId?: string | null;
  readonly cropCycleStageId?: string | null;
  readonly plannedDate?: string | null;
  readonly plannedArea?: number | null;
  readonly plannedAreaUnitId?: string | null;
  readonly waterQuantity?: number | null;
  readonly waterUnitId?: string | null;
  readonly targetId?: string | null;
  readonly applicationMethodId?: string | null;
  readonly purposeReason?: string | null;
  readonly products?: readonly SprayProductItemRequest[] | null;
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

export interface StartSprayProductItemRequest {
  readonly inventoryItemId: string;
  readonly storageLocationId: string;
  readonly actualQuantity: number;
  readonly dosage?: string | null;
}

export interface StartSprayRequest {
  readonly actualApplicationDateTime: string;
  readonly products: readonly StartSprayProductItemRequest[];
}

export interface UpdateSprayExecutionProductItemRequest {
  readonly inventoryItemId: string;
  readonly actualQuantity: number;
  readonly dosage?: string | null;
}

export interface UpdateSprayExecutionRequest {
  readonly actualApplicationDateTime: string;
  readonly actualTreatedArea?: number | null;
  readonly actualTreatedAreaUnitId?: string | null;
  readonly waterQuantity?: number | null;
  readonly waterUnitId?: string | null;
  readonly targetId?: string | null;
  readonly applicationMethodId?: string | null;
  readonly purposeReason?: string | null;
  readonly products?: readonly UpdateSprayExecutionProductItemRequest[] | null;
}

export interface CompleteSprayProductItemRequest {
  readonly inventoryItemId: string;
  readonly actualQuantity: number;
  readonly dosage?: string | null;
}

export interface CompleteSprayRequest {
  readonly actualApplicationDateTime?: string | null;
  readonly actualTreatedArea?: number | null;
  readonly actualTreatedAreaUnitId?: string | null;
  readonly waterQuantity?: number | null;
  readonly waterUnitId?: string | null;
  readonly targetId?: string | null;
  readonly applicationMethodId?: string | null;
  readonly purposeReason?: string | null;
  readonly products?: readonly CompleteSprayProductItemRequest[] | null;
}

export interface RecordCompletedSprayProductItemRequest {
  readonly inventoryItemId: string;
  readonly storageLocationId: string;
  readonly actualQuantity: number;
  readonly dosage?: string | null;
}

export interface RecordCompletedSprayRequest {
  readonly farmId: string;
  readonly actualApplicationDateTime: string;
  readonly products: readonly RecordCompletedSprayProductItemRequest[];
  readonly farmAreaId?: string | null;
  readonly plantationId?: string | null;
  readonly cropCycleId?: string | null;
  readonly cropCycleStageId?: string | null;
  readonly actualTreatedArea?: number | null;
  readonly actualTreatedAreaUnitId?: string | null;
  readonly waterQuantity?: number | null;
  readonly waterUnitId?: string | null;
  readonly targetId?: string | null;
  readonly applicationMethodId?: string | null;
  readonly purposeReason?: string | null;
}

