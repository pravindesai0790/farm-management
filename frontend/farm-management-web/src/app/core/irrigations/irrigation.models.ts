export type IrrigationStatus = 'Draft' | 'Scheduled' | 'InProgress' | 'Completed' | 'Cancelled';

export interface IrrigationListItem {
  readonly id: string;
  readonly farmId: string;
  readonly farmName: string;
  readonly farmAreaId: string;
  readonly farmAreaName: string;
  readonly plantationId: string | null;
  readonly plantationName: string | null;
  readonly cropCycleId: string | null;
  readonly cropCycleName: string | null;
  readonly cropCycleStageId: string | null;
  readonly cropCycleStageName: string | null;
  readonly status: IrrigationStatus;
  readonly statusName: string;
  readonly isOverdue: boolean;
  readonly plannedAt: string | null;
  readonly scheduledAt: string | null;
  readonly actualStartedAt: string | null;
  readonly actualEndedAt: string | null;
  readonly actualDurationMinutes: number | null;
  readonly irrigationMethodId: string | null;
  readonly irrigationMethodName: string | null;
  readonly plannedWaterQuantity: number | null;
  readonly plannedWaterUnitId: string | null;
  readonly plannedWaterUnitName: string | null;
  readonly actualWaterQuantity: number | null;
  readonly actualWaterUnitId: string | null;
  readonly actualWaterUnitName: string | null;
  readonly completedAt: string | null;
  readonly createdAt: string;
}

export interface IrrigationListQuery {
  farmId?: string;
  farmAreaId?: string;
  plantationId?: string;
  cropCycleId?: string;
  cropCycleStageId?: string;
  irrigationMethodId?: string;
  status?: string;
  fromDate?: string;
  toDate?: string;
  search?: string;
  includeOverdue?: boolean;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: string;
}

export interface IrrigationSummaryCounts {
  readonly totalCount: number;
  readonly draftCount: number;
  readonly scheduledCount: number;
  readonly inProgressCount: number;
  readonly completedCount: number;
  readonly cancelledCount: number;
  readonly overdueCount: number;
}

export interface IrrigationMethodDto {
  readonly id: string;
  readonly organizationId: string | null;
  readonly code: string;
  readonly name: string;
  readonly description?: string | null;
  readonly displayOrder: number;
  readonly isSystem: boolean;
  readonly isActive: boolean;
}

export interface IrrigationDetailsResponse {
  readonly id: string;
  readonly organizationId: string;
  readonly farmId: string;
  readonly farmName: string;
  readonly farmAreaId: string;
  readonly farmAreaName: string;
  readonly plantationId: string | null;
  readonly plantationName: string | null;
  readonly cropCycleId: string | null;
  readonly cropCycleName: string | null;
  readonly cropCycleStageId: string | null;
  readonly cropCycleStageName: string | null;
  readonly status: IrrigationStatus;
  readonly statusName: string;
  readonly isOverdue: boolean;
  readonly plannedAt: string | null;
  readonly scheduledAt: string | null;
  readonly actualStartedAt: string | null;
  readonly actualEndedAt: string | null;
  readonly actualDurationMinutes: number | null;
  readonly irrigationMethodId: string | null;
  readonly irrigationMethodName: string | null;
  readonly plannedWaterQuantity: number | null;
  readonly plannedWaterUnitId: string | null;
  readonly plannedWaterUnitName: string | null;
  readonly plannedWaterUnitSymbol: string | null;
  readonly actualWaterQuantity: number | null;
  readonly actualWaterUnitId: string | null;
  readonly actualWaterUnitName: string | null;
  readonly actualWaterUnitSymbol: string | null;
  readonly notes: string | null;
  readonly cancellationReason: string | null;
  readonly completedAt: string | null;
  readonly createdAt: string;
  readonly createdBy: string;
  readonly updatedAt: string | null;
  readonly updatedBy: string | null;
  readonly createdByName?: string | null;
  readonly updatedByName?: string | null;
  readonly concurrencyToken?: string | null;
}
