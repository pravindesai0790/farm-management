export interface CreatePlantProtectionProductRequest {
  readonly inventoryItemId: string;
  readonly productTypeId: string;
  readonly activeIngredient?: string | null;
  readonly manufacturer?: string | null;
  readonly description?: string | null;
}

export interface UpdatePlantProtectionProductRequest {
  readonly productTypeId: string;
  readonly activeIngredient?: string | null;
  readonly manufacturer?: string | null;
  readonly description?: string | null;
}

export interface PlantProtectionProductResponse {
  readonly id: string;
  readonly organizationId: string;
  readonly inventoryItemId: string;
  readonly inventoryItemName: string;
  readonly inventoryItemSku?: string | null;
  readonly stockUnitId: string;
  readonly stockUnitCode: string;
  readonly stockUnitName: string;
  readonly stockUnitSymbol: string;
  readonly productTypeId: string;
  readonly productTypeCode: string;
  readonly productTypeName: string;
  readonly activeIngredient?: string | null;
  readonly manufacturer?: string | null;
  readonly description?: string | null;
  readonly isActive: boolean;
  readonly hasCompletedSprayUsage: boolean;
  readonly createdAt: string;
  readonly createdBy: string;
  readonly updatedAt?: string | null;
  readonly updatedBy?: string | null;
}

export interface PlantProtectionProductQuery {
  readonly page?: number;
  readonly pageSize?: number;
  readonly search?: string | null;
  readonly productTypeId?: string | null;
  readonly isActive?: boolean | null;
}
