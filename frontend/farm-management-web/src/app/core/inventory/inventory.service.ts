import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import {
  CreateInventoryItemRequest,
  CreateStorageLocationRequest,
  InventoryItem,
  InventoryItemList,
  RecordOpeningStockRequest,
  RecordStockAdjustmentRequest,
  RecordStockIssueRequest,
  RecordStockReceiptRequest,
  RecordStockTransferRequest,
  StockBalance,
  StockBalanceList,
  StockMovement,
  StockMovementList,
  StorageLocation,
  StorageLocationList,
  UpdateInventoryItemRequest,
  UpdateStorageLocationRequest,
} from "./inventory.models";

@Injectable({ providedIn: "root" })
export class InventoryService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiUrl}/inventory`;

  // Items
  listItems(
    page: number = 1,
    pageSize: number = 20,
    search?: string | null,
    category?: string | null,
    isActive?: boolean | null,
  ): Observable<InventoryItemList> {
    let params = new HttpParams()
      .set("page", page.toString())
      .set("pageSize", pageSize.toString());

    if (search) params = params.set("search", search.trim());
    if (category) params = params.set("category", category.trim());
    if (isActive !== null && isActive !== undefined) {
      params = params.set("isActive", isActive.toString());
    }

    return this.http.get<InventoryItemList>(`${this.api}/items`, { params });
  }

  getItem(id: string): Observable<InventoryItem> {
    return this.http.get<InventoryItem>(`${this.api}/items/${id}`);
  }

  createItem(request: CreateInventoryItemRequest): Observable<InventoryItem> {
    return this.http.post<InventoryItem>(`${this.api}/items`, request);
  }

  updateItem(id: string, request: UpdateInventoryItemRequest): Observable<InventoryItem> {
    return this.http.put<InventoryItem>(`${this.api}/items/${id}`, request);
  }

  activateItem(id: string): Observable<void> {
    return this.http.patch<void>(`${this.api}/items/${id}/activate`, null);
  }

  deactivateItem(id: string): Observable<void> {
    return this.http.patch<void>(`${this.api}/items/${id}/deactivate`, null);
  }

  // Locations
  listLocations(
    page: number = 1,
    pageSize: number = 20,
    farmId?: string | null,
    isActive?: boolean | null,
  ): Observable<StorageLocationList> {
    let params = new HttpParams()
      .set("page", page.toString())
      .set("pageSize", pageSize.toString());

    if (farmId) params = params.set("farmId", farmId);
    if (isActive !== null && isActive !== undefined) {
      params = params.set("isActive", isActive.toString());
    }

    return this.http.get<StorageLocationList>(`${this.api}/storage-locations`, { params });
  }

  getLocation(id: string): Observable<StorageLocation> {
    return this.http.get<StorageLocation>(`${this.api}/storage-locations/${id}`);
  }

  createLocation(request: CreateStorageLocationRequest): Observable<StorageLocation> {
    return this.http.post<StorageLocation>(`${this.api}/storage-locations`, request);
  }

  updateLocation(id: string, request: UpdateStorageLocationRequest): Observable<StorageLocation> {
    return this.http.put<StorageLocation>(`${this.api}/storage-locations/${id}`, request);
  }

  activateLocation(id: string): Observable<void> {
    return this.http.patch<void>(`${this.api}/storage-locations/${id}/activate`, null);
  }

  deactivateLocation(id: string): Observable<void> {
    return this.http.patch<void>(`${this.api}/storage-locations/${id}/deactivate`, null);
  }

  // Stock Overview & Ledger

  /**
   * Fetches real-time on-hand stock balance for a specific storage location and inventory item.
   * Used for real-time stock balance validation during transaction entry.
   */
  getBalance(storageLocationId: string, inventoryItemId: string): Observable<StockBalance | null> {
    const params = new HttpParams()
      .set("storageLocationId", storageLocationId)
      .set("inventoryItemId", inventoryItemId);
    return this.http.get<StockBalance | null>(`${this.api}/stock/balance`, { params });
  }

  getOverview(
    page: number = 1,
    pageSize: number = 20,
    farmId?: string | null,
    storageLocationId?: string | null,
    inventoryItemId?: string | null,
  ): Observable<StockBalanceList> {
    let params = new HttpParams()
      .set("page", page.toString())
      .set("pageSize", pageSize.toString());

    if (farmId) params = params.set("farmId", farmId);
    if (storageLocationId) params = params.set("storageLocationId", storageLocationId);
    if (inventoryItemId) params = params.set("inventoryItemId", inventoryItemId);

    return this.http.get<StockBalanceList>(`${this.api}/stock/overview`, { params });
  }

  getLedger(
    page: number = 1,
    pageSize: number = 20,
    farmId?: string | null,
    storageLocationId?: string | null,
    inventoryItemId?: string | null,
    movementType?: string | null,
    fromDate?: string | null,
    toDate?: string | null,
  ): Observable<StockMovementList> {
    let params = new HttpParams()
      .set("page", page.toString())
      .set("pageSize", pageSize.toString());

    if (farmId) params = params.set("farmId", farmId);
    if (storageLocationId) params = params.set("storageLocationId", storageLocationId);
    if (inventoryItemId) params = params.set("inventoryItemId", inventoryItemId);
    if (movementType) params = params.set("movementType", movementType);
    if (fromDate) params = params.set("fromDate", fromDate);
    if (toDate) params = params.set("toDate", toDate);

    return this.http.get<StockMovementList>(`${this.api}/stock/ledger`, { params });
  }

  // Stock Transactions
  recordOpeningStock(request: RecordOpeningStockRequest): Observable<StockMovement> {
    return this.http.post<StockMovement>(`${this.api}/stock/opening-stock`, request);
  }

  recordReceipt(request: RecordStockReceiptRequest): Observable<StockMovement> {
    return this.http.post<StockMovement>(`${this.api}/stock/receipts`, request);
  }

  recordIssue(request: RecordStockIssueRequest): Observable<StockMovement> {
    return this.http.post<StockMovement>(`${this.api}/stock/issues`, request);
  }

  recordAdjustment(request: RecordStockAdjustmentRequest): Observable<StockMovement> {
    return this.http.post<StockMovement>(`${this.api}/stock/adjustments`, request);
  }

  recordTransfer(request: RecordStockTransferRequest): Observable<readonly StockMovement[]> {
    return this.http.post<readonly StockMovement[]>(`${this.api}/stock/transfers`, request);
  }
}
