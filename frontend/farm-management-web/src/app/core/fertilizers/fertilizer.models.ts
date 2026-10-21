export type FertilizerApplicationStatus =
  | 'Draft'
  | 'Planned'
  | 'Scheduled'
  | 'InProgress'
  | 'Completed'
  | 'Cancelled';

export interface FertilizerApplicationItemSummaryDto {
  readonly inventoryItemId: string;
  readonly itemName: string;
  readonly itemSku: string | null;
  readonly plannedQuantity: number | null;
  readonly actualQuantity: number | null;
  readonly unitSymbol: string;
}

export interface FertilizerApplicationListItem {
  readonly id: string;
  readonly referenceNumber: string;
  readonly farmId: string | null;
  readonly farmName: string | null;
  readonly farmAreaId: string | null;
  readonly farmAreaName: string | null;
  readonly plantationId: string | null;
  readonly plantationName: string | null;
  readonly cropCycleId: string | null;
  readonly cropCycleName: string | null;
  readonly cropCycleStageId: string | null;
  readonly cropCycleStageName: string | null;
  readonly status: FertilizerApplicationStatus;
  readonly statusName: string;
  readonly applicationMethodId: string | null;
  readonly applicationMethodName: string | null;
  readonly otherMethodDescription: string | null;
  readonly irrigationEventId: string | null;
  readonly plannedDate: string | null;
  readonly scheduledDate: string | null;
  readonly actualApplicationDate: string | null;
  readonly plannedArea: number | null;
  readonly plannedAreaUnitId: string | null;
  readonly plannedAreaUnitName: string | null;
  readonly actualArea: number | null;
  readonly actualAreaUnitId: string | null;
  readonly actualAreaUnitName: string | null;
  readonly purposeReason: string | null;
  readonly productCount: number;
  readonly productsSummary: readonly FertilizerApplicationItemSummaryDto[];
  readonly createdAt: string;
  readonly createdBy: string;
  readonly createdByName?: string | null;
}

export interface FertilizerApplicationProductDto {
  readonly id: string;
  readonly fertilizerApplicationId: string;
  readonly inventoryItemId: string;
  readonly inventoryItemName: string;
  readonly inventoryItemSku: string | null;
  readonly stockUnitId: string;
  readonly stockUnitName: string;
  readonly stockUnitCode: string | null;
  readonly stockUnitSymbol: string | null;
  readonly storageLocationId: string | null;
  readonly storageLocationName: string | null;
  readonly plannedQuantity: number | null;
  readonly actualQuantity: number | null;
  readonly varianceQuantity: number | null;
  readonly stockMovementId: string | null;
  readonly stockMovementReference: string | null;
}

export interface FertilizerApplicationDetailsResponse {
  readonly id: string;
  readonly organizationId: string;
  readonly referenceNumber: string;
  readonly status: FertilizerApplicationStatus;
  readonly statusName: string;
  readonly farmId: string | null;
  readonly farmName: string | null;
  readonly farmAreaId: string | null;
  readonly farmAreaName: string | null;
  readonly plantationId: string | null;
  readonly plantationName: string | null;
  readonly cropCycleId: string | null;
  readonly cropCycleName: string | null;
  readonly cropCycleStageId: string | null;
  readonly cropCycleStageName: string | null;
  readonly applicationMethodId: string | null;
  readonly applicationMethodName: string | null;
  readonly otherMethodDescription: string | null;
  readonly irrigationEventId: string | null;
  readonly irrigationEventReference: string | null;
  readonly plannedDate: string | null;
  readonly scheduledDate: string | null;
  readonly actualApplicationDate: string | null;
  readonly plannedArea: number | null;
  readonly plannedAreaUnitId: string | null;
  readonly plannedAreaUnitName: string | null;
  readonly actualArea: number | null;
  readonly actualAreaUnitId: string | null;
  readonly actualAreaUnitName: string | null;
  readonly purposeReason: string | null;
  readonly cancellationReason: string | null;
  readonly products: readonly FertilizerApplicationProductDto[];
  readonly createdAt: string;
  readonly createdBy: string;
  readonly updatedAt: string | null;
  readonly updatedBy: string | null;
  readonly startedAt: string | null;
  readonly startedBy: string | null;
  readonly completedAt: string | null;
  readonly completedBy: string | null;
  readonly cancelledAt: string | null;
  readonly cancelledBy: string | null;
  readonly createdByName?: string | null;
  readonly updatedByName?: string | null;
  readonly startedByName?: string | null;
  readonly completedByName?: string | null;
  readonly cancelledByName?: string | null;
  readonly version: number;
  readonly concurrencyToken?: string | null;
}

export interface FertilizerProductLookup {
  readonly inventoryItemId: string;
  readonly itemName: string;
  readonly sku: string | null;
  readonly stockUnitId: string;
  readonly stockUnitName: string;
  readonly stockUnitCode: string | null;
  readonly stockUnitSymbol: string | null;
  readonly categoryId: string | null;
  readonly categoryName: string | null;
}

export interface FertilizerStorageLocationLookup {
  readonly storageLocationId: string;
  readonly storageLocationName: string;
  readonly currentStock: number;
  readonly hasStock: boolean;
  readonly stockUnitId: string | null;
  readonly stockUnitName: string | null;
  readonly stockUnitSymbol: string | null;
}

export interface FertilizerApplicationMethodLookup {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly description: string | null;
  readonly displayOrder: number;
  readonly requiresDescription: boolean;
}

export interface FertilizerIrrigationLookup {
  readonly id: string;
  readonly farmId: string;
  readonly farmAreaId: string;
  readonly farmAreaName: string;
  readonly cropCycleId: string | null;
  readonly cropCycleName: string | null;
  readonly status: string;
  readonly plannedAt: string | null;
  readonly scheduledAt: string | null;
  readonly actualStartedAt: string | null;
  readonly actualEndedAt: string | null;
  readonly irrigationMethodName: string | null;
  readonly actualWaterQuantity: number | null;
  readonly waterUnitSymbol: string | null;
}

export interface FertilizerApplicationListQuery {
  farmId?: string;
  farmAreaId?: string;
  plantationId?: string;
  cropCycleId?: string;
  cropCycleStageId?: string;
  inventoryItemId?: string;
  status?: FertilizerApplicationStatus | string;
  dateType?: 'CreatedDate' | 'PlannedDate' | 'ScheduledDate' | 'ActualApplicationDate' | string;
  fromDate?: string;
  toDate?: string;
  search?: string;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc' | string;
}

export interface FertilizerDraftProductLineRequest {
  inventoryItemId: string;
  plannedQuantity: number;
  storageLocationId?: string | null;
  orderIndex?: number;
}

export interface CreateFertilizerDraftRequest {
  farmId?: string | null;
  farmAreaId?: string | null;
  plantationId?: string | null;
  cropCycleId?: string | null;
  cropCycleStageId?: string | null;
  applicationMethodId?: string | null;
  otherMethodDescription?: string | null;
  irrigationEventId?: string | null;
  plannedDate?: string | null;
  scheduledDate?: string | null;
  plannedArea?: number | null;
  plannedAreaUnitId?: string | null;
  purposeReason?: string | null;
  products?: readonly FertilizerDraftProductLineRequest[] | null;
}

export interface UpdateFertilizerDraftRequest {
  farmId?: string | null;
  farmAreaId?: string | null;
  plantationId?: string | null;
  cropCycleId?: string | null;
  cropCycleStageId?: string | null;
  applicationMethodId?: string | null;
  otherMethodDescription?: string | null;
  irrigationEventId?: string | null;
  plannedDate?: string | null;
  scheduledDate?: string | null;
  plannedArea?: number | null;
  plannedAreaUnitId?: string | null;
  purposeReason?: string | null;
  version: number;
  products?: readonly FertilizerDraftProductLineRequest[] | null;
}

export interface PlanFertilizerApplicationRequest {
  plannedDate: string;
  plannedArea?: number | null;
  plannedAreaUnitId?: string | null;
  version: number;
}

export interface ScheduleFertilizerApplicationRequest {
  scheduledDate: string;
  version: number;
}

export interface RescheduleFertilizerApplicationRequest {
  scheduledDate: string;
  reason?: string | null;
  version: number;
}

export interface StartFertilizerApplicationRequest {
  version: number;
}

export interface FertilizerActualProductLineRequest {
  inventoryItemId: string;
  storageLocationId: string;
  actualQuantity: number;
}

export interface CompleteFertilizerApplicationRequest {
  actualApplicationDate: string;
  actualArea?: number | null;
  actualAreaUnitId?: string | null;
  products: readonly FertilizerActualProductLineRequest[];
  varianceReason?: string | null;
  version: number;
}

export interface CancelFertilizerApplicationRequest {
  cancellationReason: string;
  version: number;
}

export interface FertilizerDirectProductLineRequest {
  inventoryItemId: string;
  storageLocationId: string;
  actualQuantity: number;
}

export interface RecordCompletedFertilizerApplicationRequest {
  farmId: string;
  farmAreaId: string;
  plantationId?: string | null;
  cropCycleId?: string | null;
  cropCycleStageId?: string | null;
  applicationMethodId: string;
  otherMethodDescription?: string | null;
  irrigationEventId?: string | null;
  actualApplicationDate: string;
  actualArea?: number | null;
  actualAreaUnitId?: string | null;
  purposeReason?: string | null;
  products: readonly FertilizerDirectProductLineRequest[];
}

export function getFertilizerStatusBadgeClass(status: FertilizerApplicationStatus): string {
  switch (status) {
    case 'Draft':
      return 'status-badge--draft';
    case 'Planned':
      return 'status-badge--planned';
    case 'Scheduled':
      return 'status-badge--scheduled';
    case 'InProgress':
      return 'status-badge--inprogress';
    case 'Completed':
      return 'status-badge--completed';
    case 'Cancelled':
      return 'status-badge--cancelled';
    default:
      return 'status-badge--neutral';
  }
}

export function getFertilizerStatusDisplayName(status: FertilizerApplicationStatus): string {
  switch (status) {
    case 'InProgress':
      return 'In Progress';
    default:
      return status;
  }
}
