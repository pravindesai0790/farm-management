import { PagedResponse } from "../administration/administration.models";

export interface Organization {
  id: string;
  name: string;
  code: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}
export interface OrganizationListResponse {
  items: readonly Organization[];
}
export interface Unit {
  id: string;
  code: string;
  name: string;
  symbol: string;
  unitCategory: string;
  isSystem: boolean;
  isActive: boolean;
}
export interface FarmOwnershipType {
  id: string;
  code: string;
  name: string;
  isSystem: boolean;
  isActive: boolean;
}
export interface PlantationEndReason {
  id: string;
  code: string;
  name: string;
  description: string | null;
  isSystem: boolean;
  isActive: boolean;
}
export type CycleCancellationReason = PlantationEndReason;

export interface Farm {
  id: string;
  code: string;
  name: string;
  description: string | null;
  ownershipTypeId: string;
  ownershipTypeCode: string;
  ownershipTypeName: string;
  totalArea: number | null;
  areaUnitId: string | null;
  areaUnitCode: string | null;
  areaUnitName: string | null;
  areaUnitSymbol: string | null;
  addressLine1: string | null;
  addressLine2: string | null;
  city: string | null;
  district: string | null;
  state: string | null;
  country: string | null;
  postalCode: string | null;
  latitude: number | null;
  longitude: number | null;
  isActive: boolean;
  createdAt: string;
  createdBy: string;
  updatedAt: string | null;
  updatedBy: string | null;
}
export interface FarmArea {
  id: string;
  farmId: string;
  farmCode: string;
  farmName: string;
  parentFarmAreaId: string | null;
  code: string;
  name: string;
  description: string | null;
  totalArea: number;
  areaUnitId: string;
  areaUnitCode: string;
  areaUnitName: string;
  areaUnitSymbol: string;
  isActive: boolean;
  createdAt: string;
  createdBy: string;
  updatedAt: string | null;
  updatedBy: string | null;
}
export interface FarmAreaAvailability {
  farmAreaId: string;
  totalArea: number;
  allocatedArea: number;
  availableArea: number;
  unit: string;
}
export interface Crop {
  id: string;
  organizationId: string | null;
  code: string;
  name: string;
  scientificName: string | null;
  cropType: string;
  cropDurationType: string;
  description: string | null;
  isSystem: boolean;
  isActive: boolean;
  createdAt: string;
  createdBy: string | null;
  updatedAt: string | null;
  updatedBy: string | null;
}
export interface CropVariety {
  id: string;
  organizationId: string | null;
  cropId: string;
  cropCode: string;
  cropName: string;
  code: string;
  name: string;
  description: string | null;
  isSystem: boolean;
  isActive: boolean;
  createdAt: string;
  createdBy: string | null;
  updatedAt: string | null;
  updatedBy: string | null;
}
export interface Plantation {
  id: string;
  farmId: string;
  farmCode: string;
  farmName: string;
  farmAreaId: string;
  farmAreaCode: string;
  farmAreaName: string;
  cropId: string;
  cropCode: string;
  cropName: string;
  varietyId: string | null;
  varietyCode: string | null;
  varietyName: string | null;
  lifecycleTemplateId: string | null;
  lifecycleTemplateName: string | null;
  plantationCode: string;
  plantationName: string;
  allocatedArea: number;
  areaUnitId: string;
  areaUnitCode: string;
  areaUnitName: string;
  areaUnitSymbol: string;
  plantingDate: string;
  expectedEndDate: string | null;
  actualEndDate: string | null;
  status: string;
  endReasonId: string | null;
  endReasonCode: string | null;
  endReasonName: string | null;
  endNotes: string | null;
  isActive: boolean;
  createdAt: string;
  createdBy: string;
  updatedAt: string | null;
  updatedBy: string | null;
}
export interface CropCycle {
  id: string;
  plantationId: string;
  plantationCode: string;
  plantationName: string;
  farmId: string;
  farmCode: string;
  farmName: string;
  farmAreaId: string;
  farmAreaCode: string;
  farmAreaName: string;
  cropId: string;
  cropCode: string;
  cropName: string;
  cropDurationType: string;
  cycleCode: string;
  cycleName: string;
  seasonYear: number;
  seasonName: string | null;
  plannedStartDate: string;
  actualStartDate: string | null;
  expectedEndDate: string | null;
  actualEndDate: string | null;
  status: string;
  cancellationReasonId: string | null;
  cancellationReasonCode: string | null;
  cancellationReasonName: string | null;
  cancellationNotes: string | null;
  createdAt: string;
  createdBy: string;
  updatedAt: string | null;
  updatedBy: string | null;
}
export interface LifecycleStage {
  id: string;
  lifecycleTemplateId: string;
  stageCode: string;
  stageName: string;
  sequenceNumber: number;
  description: string | null;
  isActive: boolean;
}
export interface LifecycleTemplate {
  id: string;
  organizationId: string | null;
  cropId: string;
  cropCode: string;
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
  stages: readonly LifecycleStage[];
}

export type FarmList = PagedResponse<Farm>;
export type CropList = PagedResponse<Crop>;
export type VarietyList = PagedResponse<CropVariety>;
export interface PlantationList {
  items: readonly Plantation[];
  totalCount: number;
}
export interface CycleList {
  items: readonly CropCycle[];
  totalCount: number;
}
export interface LifecycleList extends PagedResponse<LifecycleTemplate> {}

export interface KpiSummary {
  totalFarms: number;
  totalAreas: number;
  totalArea: number;
  allocatedArea: number;
  availableArea: number;
  utilizationPercentage: number;
  areaUnitSymbol: string;
  activePlantationsCount: number;
  plannedPlantationsCount: number;
  activeCyclesCount: number;
}

export interface VarietyAllocationSummary {
  varietyId: string | null;
  varietyName: string;
  allocatedArea: number;
  areaUnitSymbol: string;
  percentageOfCrop: number;
}

export interface CropAllocationSummary {
  cropId: string;
  cropName: string;
  cropCode: string;
  totalAllocatedArea: number;
  areaUnitSymbol: string;
  percentageOfAllocated: number;
  varieties: readonly VarietyAllocationSummary[];
}

export interface ActiveCycleSummary {
  cycleId: string;
  cycleCode: string;
  cycleName: string;
  seasonYear: number;
  seasonName: string | null;
  plantationId: string;
  plantationName: string;
  farmName: string;
  farmAreaName: string;
  cropName: string;
  varietyName: string | null;
  allocatedArea: number;
  areaUnitSymbol: string;
  startDate: string;
  expectedEndDate: string | null;
  progressPercentage: number | null;
  status: string;
}

export interface FarmUtilizationSummary {
  farmId: string;
  farmCode: string;
  farmName: string;
  totalArea: number;
  allocatedArea: number;
  availableArea: number;
  utilizationPercentage: number;
  areaUnitSymbol: string;
  activePlantationsCount: number;
}

export interface DashboardSummaryResponse {
  kpi: KpiSummary;
  cropAllocations: readonly CropAllocationSummary[];
  activeCycles: readonly ActiveCycleSummary[];
  farmUtilizations: readonly FarmUtilizationSummary[];
  currentSeason: string | null;
}

