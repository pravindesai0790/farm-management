import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { PagedResponse } from "../models/paged-response.model";
import {
  IrrigationDetailsResponse,
  IrrigationListItem,
  IrrigationListQuery,
  IrrigationMethodDto,
  IrrigationSummaryCounts,
} from "./irrigation.models";

@Injectable({ providedIn: "root" })
export class IrrigationService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiUrl}/irrigations`;

  listIrrigations(query: IrrigationListQuery = {}): Observable<PagedResponse<IrrigationListItem>> {
    let params = new HttpParams()
      .set("pageNumber", (query.pageNumber ?? 1).toString())
      .set("pageSize", (query.pageSize ?? 20).toString());

    if (query.farmId) params = params.set("farmId", query.farmId);
    if (query.farmAreaId) params = params.set("farmAreaId", query.farmAreaId);
    if (query.plantationId) params = params.set("plantationId", query.plantationId);
    if (query.cropCycleId) params = params.set("cropCycleId", query.cropCycleId);
    if (query.cropCycleStageId) params = params.set("cropCycleStageId", query.cropCycleStageId);
    if (query.irrigationMethodId) params = params.set("irrigationMethodId", query.irrigationMethodId);
    if (query.status) params = params.set("status", query.status);
    if (query.fromDate) params = params.set("fromDate", query.fromDate);
    if (query.toDate) params = params.set("toDate", query.toDate);
    if (query.search?.trim()) params = params.set("search", query.search.trim());
    if (query.includeOverdue) params = params.set("includeOverdue", "true");
    if (query.sortBy) params = params.set("sortBy", query.sortBy);
    if (query.sortDirection) params = params.set("sortDirection", query.sortDirection);

    return this.http.get<PagedResponse<IrrigationListItem>>(this.api, { params });
  }

  getSummaryCounts(farmId?: string): Observable<IrrigationSummaryCounts> {
    let params = new HttpParams();
    if (farmId) {
      params = params.set("farmId", farmId);
    }
    return this.http.get<IrrigationSummaryCounts>(`${this.api}/summary`, { params });
  }

  getMethods(activeOnly: boolean = true): Observable<readonly IrrigationMethodDto[]> {
    const params = new HttpParams().set("activeOnly", activeOnly.toString());
    return this.http.get<readonly IrrigationMethodDto[]>(`${this.api}/methods`, { params });
  }

  getIrrigation(id: string): Observable<IrrigationDetailsResponse> {
    return this.http.get<IrrigationDetailsResponse>(`${this.api}/${id}`);
  }

  getCropCycleStages(cropCycleId: string): Observable<readonly import("../farm-management/farm-management.models").CropCycleStage[]> {
    return this.http.get<readonly import("../farm-management/farm-management.models").CropCycleStage[]>(`${environment.apiUrl}/crop-cycles/${cropCycleId}/stages`);
  }
}
