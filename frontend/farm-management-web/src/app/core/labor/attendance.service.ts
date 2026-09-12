import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { PagedResponse } from "../models/paged-response.model";
import {
  AttendanceEligibleWorker,
  AttendanceRecord,
  AttendanceWagePreviewBatchRequest,
  AttendanceWagePreviewBatchResponse,
  AttendanceWagePreviewRequest,
  AttendanceWagePreviewResponse,
  CreateDraftAttendanceRequest,
  DailyAttendanceResponse,
  FinalizeAttendanceRequest,
  FinalizeAttendanceResponse,
  SaveDailyDraftAttendanceBatchRequest,
  UpdateDraftAttendanceRequest,
} from "./attendance.models";

@Injectable({ providedIn: "root" })
export class AttendanceService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  getDailyAttendance(
    farmId: string,
    date: string,
  ): Observable<DailyAttendanceResponse> {
    const params = new HttpParams()
      .set("farmId", farmId)
      .set("date", date);

    return this.http.get<DailyAttendanceResponse>(
      `${this.api}/attendance/daily`,
      { params },
    );
  }

  getEligibleWorkers(
    farmId: string,
    date: string,
    search?: string | null,
    page = 1,
    pageSize = 100,
  ): Observable<PagedResponse<AttendanceEligibleWorker>> {
    const safePageSize = Math.min(Math.max(pageSize, 1), 100);
    let params = new HttpParams()
      .set("farmId", farmId)
      .set("date", date)
      .set("page", Math.max(page, 1).toString())
      .set("pageSize", safePageSize.toString());

    if (search && search.trim()) {
      params = params.set("search", search.trim());
    }

    return this.http.get<PagedResponse<AttendanceEligibleWorker>>(
      `${this.api}/attendance/eligible-workers`,
      { params },
    );
  }

  saveDailyDraftBatch(
    request: SaveDailyDraftAttendanceBatchRequest,
  ): Observable<DailyAttendanceResponse> {
    return this.http.post<DailyAttendanceResponse>(
      `${this.api}/attendance/draft/batch`,
      request,
    );
  }

  createDraft(
    request: CreateDraftAttendanceRequest,
  ): Observable<AttendanceRecord> {
    return this.http.post<AttendanceRecord>(
      `${this.api}/attendance/draft`,
      request,
    );
  }

  updateDraft(
    id: string,
    request: UpdateDraftAttendanceRequest,
  ): Observable<AttendanceRecord> {
    return this.http.put<AttendanceRecord>(
      `${this.api}/attendance/${id}`,
      request,
    );
  }

  deleteDraft(id: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/attendance/${id}`);
  }

  previewWage(
    request: AttendanceWagePreviewRequest,
  ): Observable<AttendanceWagePreviewResponse> {
    return this.http.post<AttendanceWagePreviewResponse>(
      `${this.api}/attendance/preview`,
      request,
    );
  }

  previewWageBatch(
    request: AttendanceWagePreviewBatchRequest,
  ): Observable<AttendanceWagePreviewBatchResponse> {
    return this.http.post<AttendanceWagePreviewBatchResponse>(
      `${this.api}/attendance/preview-batch`,
      request,
    );
  }

  finalizeAttendance(
    request: FinalizeAttendanceRequest,
  ): Observable<FinalizeAttendanceResponse> {
    return this.http.post<FinalizeAttendanceResponse>(
      `${this.api}/attendance/finalize`,
      request,
    );
  }

  finalizeSingleAttendance(id: string): Observable<AttendanceRecord> {
    return this.http.post<AttendanceRecord>(
      `${this.api}/attendance/${id}/finalize`,
      {},
    );
  }
}
