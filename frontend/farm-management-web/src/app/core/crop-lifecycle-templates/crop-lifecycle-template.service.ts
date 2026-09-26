import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import {
  CreateCropLifecycleStageRequest,
  CreateCropLifecycleTemplateRequest,
  CropLifecycleStage,
  CropLifecycleTemplate,
  CropLifecycleTemplateList,
  CropLifecycleTemplateQuery,
  ReorderCropLifecycleStageItem,
  UpdateCropLifecycleStageRequest,
  UpdateCropLifecycleTemplateRequest,
} from "./crop-lifecycle-template.models";

@Injectable({ providedIn: "root" })
export class CropLifecycleTemplateService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/crop-lifecycle-templates`;

  list(query: CropLifecycleTemplateQuery = {}): Observable<CropLifecycleTemplateList> {
    let params = new HttpParams();
    if (query.page) params = params.set("page", query.page);
    if (query.pageSize) params = params.set("pageSize", query.pageSize);
    if (query.cropId) params = params.set("cropId", query.cropId);
    if (query.isActive !== undefined && query.isActive !== null) {
      params = params.set("isActive", query.isActive);
    }
    return this.http.get<CropLifecycleTemplateList>(this.baseUrl, { params });
  }

  get(id: string): Observable<CropLifecycleTemplate> {
    return this.http.get<CropLifecycleTemplate>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateCropLifecycleTemplateRequest): Observable<CropLifecycleTemplate> {
    return this.http.post<CropLifecycleTemplate>(this.baseUrl, request);
  }

  update(id: string, request: UpdateCropLifecycleTemplateRequest): Observable<CropLifecycleTemplate> {
    return this.http.put<CropLifecycleTemplate>(`${this.baseUrl}/${id}`, request);
  }

  activate(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/activate`, {});
  }

  deactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/deactivate`, {});
  }

  getStage(templateId: string, stageId: string): Observable<CropLifecycleStage> {
    return this.http.get<CropLifecycleStage>(`${this.baseUrl}/${templateId}/stages/${stageId}`);
  }

  createStage(
    templateId: string,
    request: CreateCropLifecycleStageRequest,
  ): Observable<CropLifecycleStage> {
    return this.http.post<CropLifecycleStage>(`${this.baseUrl}/${templateId}/stages`, request);
  }

  updateStage(
    templateId: string,
    stageId: string,
    request: UpdateCropLifecycleStageRequest,
  ): Observable<CropLifecycleStage> {
    return this.http.put<CropLifecycleStage>(
      `${this.baseUrl}/${templateId}/stages/${stageId}`,
      request,
    );
  }

  activateStage(templateId: string, stageId: string): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/${templateId}/stages/${stageId}/activate`,
      {},
    );
  }

  deactivateStage(templateId: string, stageId: string): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/${templateId}/stages/${stageId}/deactivate`,
      {},
    );
  }

  reorderStages(
    templateId: string,
    stages: readonly ReorderCropLifecycleStageItem[],
  ): Observable<readonly CropLifecycleStage[]> {
    return this.http.put<readonly CropLifecycleStage[]>(
      `${this.baseUrl}/${templateId}/stages/reorder`,
      { stages },
    );
  }
}
