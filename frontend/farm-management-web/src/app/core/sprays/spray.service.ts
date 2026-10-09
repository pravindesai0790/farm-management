import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { PagedResponse } from "../models/paged-response.model";
import { CropCycleStage } from "../farm-management/farm-management.models";
import {
  ApplicationMethodResponse,
  CancelSprayRequest,
  CreateSprayDraftRequest,
  ProductTypeResponse,
  RescheduleSprayRequest,
  ScheduleSprayRequest,
  SprayDetailsResponse,
  SprayListItem,
  SprayListQuery,
  SprayProductLookupResponse,
  SprayStorageLocationLookupResponse,
  TargetResponse,
  UpdateSprayDraftRequest,
} from "./spray.models";

@Injectable({ providedIn: "root" })
export class SprayService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiUrl}/sprays`;

  listSprays(query: SprayListQuery = {}): Observable<PagedResponse<SprayListItem>> {
    let params = new HttpParams()
      .set("pageNumber", (query.pageNumber ?? 1).toString())
      .set("pageSize", (query.pageSize ?? 20).toString());

    if (query.farmId) params = params.set("farmId", query.farmId);
    if (query.farmAreaId) params = params.set("farmAreaId", query.farmAreaId);
    if (query.plantationId) params = params.set("plantationId", query.plantationId);
    if (query.cropCycleId) params = params.set("cropCycleId", query.cropCycleId);
    if (query.cropCycleStageId) params = params.set("cropCycleStageId", query.cropCycleStageId);
    if (query.status) params = params.set("status", query.status);
    if (query.targetId) params = params.set("targetId", query.targetId);
    if (query.fromDate) params = params.set("fromDate", query.fromDate);
    if (query.toDate) params = params.set("toDate", query.toDate);
    if (query.search?.trim()) params = params.set("search", query.search.trim());
    if (query.includeOverdue) params = params.set("includeOverdue", "true");
    if (query.sortBy) params = params.set("sortBy", query.sortBy);
    if (query.sortDirection) params = params.set("sortDirection", query.sortDirection);

    return this.http.get<PagedResponse<SprayListItem>>(this.api, { params });
  }

  getSpray(id: string): Observable<SprayDetailsResponse> {
    return this.http.get<SprayDetailsResponse>(`${this.api}/${id}`);
  }

  createDraft(request: CreateSprayDraftRequest): Observable<SprayDetailsResponse> {
    return this.http.post<SprayDetailsResponse>(this.api, request);
  }

  updateDraft(id: string, request: UpdateSprayDraftRequest): Observable<SprayDetailsResponse> {
    return this.http.put<SprayDetailsResponse>(`${this.api}/${id}`, request);
  }

  cancelSpray(id: string, cancellationReason: string): Observable<any> {
    const payload: CancelSprayRequest = { cancellationReason };
    return this.http.post<any>(`${this.api}/${id}/cancel`, payload);
  }

  scheduleSpray(id: string, request: ScheduleSprayRequest): Observable<any> {
    return this.http.post<any>(`${this.api}/${id}/schedule`, request);
  }

  rescheduleSpray(id: string, request: RescheduleSprayRequest): Observable<any> {
    return this.http.post<any>(`${this.api}/${id}/reschedule`, request);
  }

  getTargets(type?: string): Observable<readonly TargetResponse[]> {
    let params = new HttpParams();
    if (type?.trim()) {
      params = params.set("type", type.trim());
    }
    return this.http.get<readonly TargetResponse[]>(`${environment.apiUrl}/master-data/targets`, { params });
  }

  getProductTypes(): Observable<readonly ProductTypeResponse[]> {
    return this.http.get<readonly ProductTypeResponse[]>(`${environment.apiUrl}/master-data/product-types`);
  }

  getApplicationMethods(): Observable<readonly ApplicationMethodResponse[]> {
    return this.http.get<readonly ApplicationMethodResponse[]>(`${environment.apiUrl}/master-data/application-methods`);
  }

  getCropCycleStages(cropCycleId: string): Observable<readonly CropCycleStage[]> {
    return this.http.get<readonly CropCycleStage[]>(`${environment.apiUrl}/crop-cycles/${cropCycleId}/stages`);
  }

  getProductLookup(): Observable<readonly SprayProductLookupResponse[]> {
    return this.http.get<readonly SprayProductLookupResponse[]>(`${this.api}/lookup/products`);
  }

  getStorageLocationLookup(farmId: string, inventoryItemId: string): Observable<readonly SprayStorageLocationLookupResponse[]> {
    const params = new HttpParams()
      .set("farmId", farmId)
      .set("inventoryItemId", inventoryItemId);
    return this.http.get<readonly SprayStorageLocationLookupResponse[]>(`${this.api}/lookup/storage-locations`, { params });
  }
}
