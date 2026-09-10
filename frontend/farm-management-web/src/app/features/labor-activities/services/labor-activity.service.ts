import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../../environments/environment";
import {
  CancelLaborActivityRequest,
  LaborActivity,
  LaborActivityFilter,
  LaborActivityList,
  NamedReference,
} from "../models/labor-activity.models";

@Injectable({ providedIn: "root" })
export class LaborActivityService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  list(filter: LaborActivityFilter): Observable<LaborActivityList> {
    let params = new HttpParams()
      .set("page", filter.page.toString())
      .set("pageSize", filter.pageSize.toString());

    if (filter.farmId) {
      params = params.set("farmId", filter.farmId);
    }
    if (filter.farmAreaId) {
      params = params.set("farmAreaId", filter.farmAreaId);
    }
    if (filter.plantationId) {
      params = params.set("plantationId", filter.plantationId);
    }
    if (filter.cropCycleId) {
      params = params.set("cropCycleId", filter.cropCycleId);
    }
    if (filter.activityTypeId) {
      params = params.set("activityTypeId", filter.activityTypeId);
    }
    if (filter.fromDate) {
      params = params.set("fromDate", filter.fromDate);
    }
    if (filter.toDate) {
      params = params.set("toDate", filter.toDate);
    }
    if (filter.status) {
      params = params.set("status", filter.status);
    }

    return this.http.get<LaborActivityList>(`${this.api}/labor-activities`, { params });
  }

  getTypes(): Observable<readonly NamedReference[]> {
    return this.http.get<readonly NamedReference[]>(`${this.api}/labor-activities/types`);
  }

  get(id: string): Observable<LaborActivity> {
    return this.http.get<LaborActivity>(`${this.api}/labor-activities/${id}`);
  }

  cancel(id: string, reason: string): Observable<void> {
    const payload: CancelLaborActivityRequest = { reason };
    return this.http.post<void>(`${this.api}/labor-activities/${id}/cancel`, payload);
  }
}
