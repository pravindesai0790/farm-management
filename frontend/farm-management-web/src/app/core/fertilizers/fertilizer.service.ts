import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CropCycleStage } from '../farm-management/farm-management.models';
import { PagedResponse } from '../models/paged-response.model';
import {
  CancelFertilizerApplicationRequest,
  CompleteFertilizerApplicationRequest,
  CreateFertilizerDraftRequest,
  FertilizerApplicationDetailsResponse,
  FertilizerApplicationListItem,
  FertilizerApplicationListQuery,
  FertilizerApplicationMethodLookup,
  FertilizerIrrigationLookup,
  FertilizerProductLookup,
  FertilizerStorageLocationLookup,
  PlanFertilizerApplicationRequest,
  RecordCompletedFertilizerApplicationRequest,
  RescheduleFertilizerApplicationRequest,
  ScheduleFertilizerApplicationRequest,
  StartFertilizerApplicationRequest,
  UpdateFertilizerDraftRequest,
} from './fertilizer.models';

@Injectable({ providedIn: 'root' })
export class FertilizerService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiUrl}/fertilizer-applications`;

  list(query: FertilizerApplicationListQuery = {}): Observable<PagedResponse<FertilizerApplicationListItem>> {
    const params = this.buildQueryParams(query)
      .set('pageNumber', (query.pageNumber ?? 1).toString())
      .set('pageSize', (query.pageSize ?? 20).toString());

    return this.http.get<PagedResponse<FertilizerApplicationListItem>>(this.api, { params });
  }

  get(id: string): Observable<FertilizerApplicationDetailsResponse> {
    return this.http.get<FertilizerApplicationDetailsResponse>(`${this.api}/${id}`);
  }

  exportCsv(query: FertilizerApplicationListQuery = {}): Observable<Blob> {
    const params = this.buildQueryParams(query).set('format', 'csv');
    return this.http.get(`${this.api}/export`, {
      params,
      responseType: 'blob',
    });
  }

  getProductsLookup(
    storageLocationId?: string,
    farmId?: string,
  ): Observable<readonly FertilizerProductLookup[]> {
    let params = new HttpParams();
    if (storageLocationId) {
      params = params.set('storageLocationId', storageLocationId);
    }
    if (farmId) {
      params = params.set('farmId', farmId);
    }
    return this.http.get<readonly FertilizerProductLookup[]>(`${this.api}/lookups/products`, {
      params,
    });
  }

  getStorageLocationsLookup(
    farmId: string,
    inventoryItemId?: string,
  ): Observable<readonly FertilizerStorageLocationLookup[]> {
    let params = new HttpParams().set('farmId', farmId);
    if (inventoryItemId) {
      params = params.set('inventoryItemId', inventoryItemId);
    }
    return this.http.get<readonly FertilizerStorageLocationLookup[]>(
      `${this.api}/lookups/storage-locations`,
      { params },
    );
  }

  getApplicationMethodsLookup(): Observable<readonly FertilizerApplicationMethodLookup[]> {
    return this.http.get<readonly FertilizerApplicationMethodLookup[]>(
      `${this.api}/lookups/application-methods`,
    );
  }

  getIrrigationsLookup(
    farmId?: string,
    farmAreaId?: string,
  ): Observable<readonly FertilizerIrrigationLookup[]> {
    let params = new HttpParams();
    if (farmId) params = params.set('farmId', farmId);
    if (farmAreaId) params = params.set('farmAreaId', farmAreaId);
    return this.http.get<readonly FertilizerIrrigationLookup[]>(
      `${this.api}/lookups/irrigations`,
      { params },
    );
  }

  getCropCycleStages(cropCycleId: string): Observable<readonly CropCycleStage[]> {
    return this.http.get<readonly CropCycleStage[]>(`${environment.apiUrl}/crop-cycles/${cropCycleId}/stages`);
  }

  createDraft(request: CreateFertilizerDraftRequest): Observable<FertilizerApplicationDetailsResponse> {
    return this.http.post<FertilizerApplicationDetailsResponse>(this.api, request);
  }

  updateDraft(
    id: string,
    request: UpdateFertilizerDraftRequest,
  ): Observable<FertilizerApplicationDetailsResponse> {
    return this.http.put<FertilizerApplicationDetailsResponse>(`${this.api}/${id}`, request);
  }

  deleteDraft(id: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/${id}`);
  }

  copyAsNewDraft(id: string): Observable<FertilizerApplicationDetailsResponse> {
    return this.http.post<FertilizerApplicationDetailsResponse>(`${this.api}/${id}/copy`, {});
  }

  plan(
    id: string,
    request: PlanFertilizerApplicationRequest,
  ): Observable<FertilizerApplicationDetailsResponse> {
    return this.http.post<FertilizerApplicationDetailsResponse>(`${this.api}/${id}/plan`, request);
  }

  schedule(
    id: string,
    request: ScheduleFertilizerApplicationRequest,
  ): Observable<FertilizerApplicationDetailsResponse> {
    return this.http.post<FertilizerApplicationDetailsResponse>(`${this.api}/${id}/schedule`, request);
  }

  reschedule(
    id: string,
    request: RescheduleFertilizerApplicationRequest,
  ): Observable<FertilizerApplicationDetailsResponse> {
    return this.http.post<FertilizerApplicationDetailsResponse>(`${this.api}/${id}/reschedule`, request);
  }

  start(
    id: string,
    request: StartFertilizerApplicationRequest,
  ): Observable<FertilizerApplicationDetailsResponse> {
    return this.http.post<FertilizerApplicationDetailsResponse>(`${this.api}/${id}/start`, request);
  }

  complete(
    id: string,
    request: CompleteFertilizerApplicationRequest,
  ): Observable<FertilizerApplicationDetailsResponse> {
    return this.http.post<FertilizerApplicationDetailsResponse>(`${this.api}/${id}/complete`, request);
  }

  cancel(
    id: string,
    request: CancelFertilizerApplicationRequest,
  ): Observable<FertilizerApplicationDetailsResponse> {
    return this.http.post<FertilizerApplicationDetailsResponse>(`${this.api}/${id}/cancel`, request);
  }

  recordCompleted(
    request: RecordCompletedFertilizerApplicationRequest,
  ): Observable<FertilizerApplicationDetailsResponse> {
    return this.http.post<FertilizerApplicationDetailsResponse>(
      `${this.api}/record-completed`,
      request,
    );
  }

  private buildQueryParams(query: FertilizerApplicationListQuery): HttpParams {
    let params = new HttpParams();

    if (query.farmId) params = params.set('farmId', query.farmId);
    if (query.farmAreaId) params = params.set('farmAreaId', query.farmAreaId);
    if (query.plantationId) params = params.set('plantationId', query.plantationId);
    if (query.cropCycleId) params = params.set('cropCycleId', query.cropCycleId);
    if (query.cropCycleStageId) params = params.set('cropCycleStageId', query.cropCycleStageId);
    if (query.inventoryItemId) params = params.set('inventoryItemId', query.inventoryItemId);
    if (query.status) params = params.set('status', query.status);
    if (query.dateType) params = params.set('dateType', query.dateType);
    if (query.fromDate) params = params.set('fromDate', query.fromDate);
    if (query.toDate) params = params.set('toDate', query.toDate);
    if (query.search?.trim()) params = params.set('search', query.search.trim());
    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);

    return params;
  }
}
