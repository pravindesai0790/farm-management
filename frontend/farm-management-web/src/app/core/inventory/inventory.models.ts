import { PagedResponse } from "../models/paged-response.model";

export type StockMovementType =
  | "OpeningStock"
  | "Receipt"
  | "Issue"
  | "AdjustmentIn"
  | "AdjustmentOut"
  | "TransferIn"
  | "TransferOut"
  | "OpeningStockReversal"
  | "ReceiptReversal"
  | "IssueReversal"
  | "AdjustmentInReversal"
  | "AdjustmentOutReversal"
  | "TransferInReversal"
  | "TransferOutReversal";

export interface InventoryItem {
  readonly id: string;
  readonly organizationId: string;
  readonly name: string;
  readonly sku?: string | null;
  readonly description?: string | null;
  readonly category?: string | null;
  readonly stockUnitId: string;
  readonly stockUnitCode: string;
  readonly stockUnitName: string;
  readonly stockUnitSymbol: string;
  readonly isActive: boolean;
  readonly createdAt: string;
  readonly createdBy: string;
  readonly updatedAt?: string | null;
  readonly updatedBy?: string | null;
}

export type InventoryItemList = PagedResponse<InventoryItem>;

export interface CreateInventoryItemRequest {
  readonly name: string;
  readonly stockUnitId: string;
  readonly sku?: string | null;
  readonly description?: string | null;
  readonly category?: string | null;
}

export interface UpdateInventoryItemRequest {
  readonly name: string;
  readonly stockUnitId: string;
  readonly sku?: string | null;
  readonly description?: string | null;
  readonly category?: string | null;
}

export interface StorageLocation {
  readonly id: string;
  readonly organizationId: string;
  readonly farmId: string;
  readonly farmName: string;
  readonly name: string;
  readonly description?: string | null;
  readonly isActive: boolean;
  readonly createdAt: string;
  readonly createdBy: string;
  readonly updatedAt?: string | null;
  readonly updatedBy?: string | null;
}

export type StorageLocationList = PagedResponse<StorageLocation>;

export interface CreateStorageLocationRequest {
  readonly farmId: string;
  readonly name: string;
  readonly description?: string | null;
}

export interface UpdateStorageLocationRequest {
  readonly name: string;
  readonly description?: string | null;
}

export interface StockBalance {
  readonly id: string;
  readonly organizationId: string;
  readonly farmId: string;
  readonly farmName: string;
  readonly storageLocationId: string;
  readonly storageLocationName: string;
  readonly inventoryItemId: string;
  readonly inventoryItemName: string;
  readonly inventoryItemSku?: string | null;
  readonly inventoryItemCategory?: string | null;
  readonly stockUnitId: string;
  readonly stockUnitCode: string;
  readonly stockUnitName: string;
  readonly stockUnitSymbol: string;
  readonly quantityOnHand: number;
  readonly createdAt: string;
  readonly updatedAt?: string | null;
}

export type StockBalanceList = PagedResponse<StockBalance>;

export interface StockMovement {
  readonly id: string;
  readonly organizationId: string;
  readonly movementType: StockMovementType;
  readonly movementTypeName: string;
  readonly inventoryItemId: string;
  readonly inventoryItemName: string;
  readonly inventoryItemSku?: string | null;
  readonly farmId: string;
  readonly farmName: string;
  readonly storageLocationId: string;
  readonly storageLocationName: string;
  readonly quantity: number;
  readonly stockUnitId: string;
  readonly stockUnitCode: string;
  readonly stockUnitSymbol: string;
  readonly movementDate: string;
  readonly referenceNumber?: string | null;
  readonly notes?: string | null;
  readonly parentTransactionId?: string | null;
  readonly createdAt: string;
  readonly createdBy: string;
  readonly cropCycleId?: string | null;
  readonly cropCycleTitle?: string | null;
  readonly cropCycleStageId?: string | null;
  readonly cropCycleStageName?: string | null;
  readonly plantationId?: string | null;
  readonly plantationName?: string | null;
  readonly farmAreaId?: string | null;
  readonly farmAreaName?: string | null;
  readonly laborActivityId?: string | null;
  readonly laborActivityTypeName?: string | null;
  readonly isReversed: boolean;
  readonly reversalMovementId?: string | null;
  readonly reversedMovementId?: string | null;
  readonly reversalReason?: string | null;
}

export interface ReverseStockMovementRequest {
  readonly reason: string;
  readonly reversalDate?: string | null;
}

export type StockMovementList = PagedResponse<StockMovement>;

export interface RecordOpeningStockRequest {
  readonly farmId: string;
  readonly storageLocationId: string;
  readonly inventoryItemId: string;
  readonly quantity: number;
  readonly movementDate: string;
  readonly notes?: string | null;
}

export interface RecordStockReceiptRequest {
  readonly farmId: string;
  readonly storageLocationId: string;
  readonly inventoryItemId: string;
  readonly quantity: number;
  readonly movementDate: string;
  readonly referenceNumber?: string | null;
  readonly notes?: string | null;
}

export interface RecordStockIssueRequest {
  readonly farmId: string;
  readonly storageLocationId: string;
  readonly inventoryItemId: string;
  readonly quantity: number;
  readonly movementDate: string;
  readonly referenceNumber?: string | null;
  readonly purposeNotes?: string | null;
  readonly cropCycleId?: string | null;
  readonly cropCycleStageId?: string | null;
  readonly plantationId?: string | null;
  readonly farmAreaId?: string | null;
  readonly laborActivityId?: string | null;
}

export interface RecordStockAdjustmentRequest {
  readonly farmId: string;
  readonly storageLocationId: string;
  readonly inventoryItemId: string;
  readonly adjustmentType: "AdjustmentIn" | "AdjustmentOut";
  readonly quantity: number;
  readonly movementDate: string;
  readonly reasonNotes: string;
}

export interface RecordStockTransferRequest {
  readonly sourceFarmId: string;
  readonly sourceStorageLocationId: string;
  readonly destinationFarmId: string;
  readonly destinationStorageLocationId: string;
  readonly inventoryItemId: string;
  readonly quantity: number;
  readonly movementDate: string;
  readonly referenceNumber?: string | null;
  readonly notes?: string | null;
}
