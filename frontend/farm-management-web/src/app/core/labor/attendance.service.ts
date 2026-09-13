import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { PagedResponse } from "../models/paged-response.model";
import {
  AttendanceDetail,
  AttendanceEligibleWorker,
  AttendanceHistoryFilter,
  AttendanceRecord,
  AttendanceWagePreviewBatchRequest,
  AttendanceWagePreviewBatchResponse,
  AttendanceWagePreviewRequest,
  AttendanceWagePreviewResponse,
  CopyPreviousDayAttendanceRequest,
  CopyPreviousDayAttendanceResponse,
  CopyPreviousDayPreviewResponse,
  CreateDraftAttendanceRequest,
  DailyAttendanceResponse,
  DailyAttendanceSummary,
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

  getDailySummary(
    farmId: string,
    date: string,
  ): Observable<DailyAttendanceSummary> {
    const params = new HttpParams()
      .set("farmId", farmId)
      .set("date", date);

    return this.http.get<DailyAttendanceSummary>(
      `${this.api}/attendance/summary`,
      { params },
    );
  }

  getAttendanceHistory(
    filter: AttendanceHistoryFilter,
  ): Observable<PagedResponse<AttendanceRecord>> {
    let params = new HttpParams();

    if (filter.farmId && filter.farmId.trim()) {
      params = params.set("farmId", filter.farmId.trim());
    }
    if (filter.workerId && filter.workerId.trim()) {
      params = params.set("workerId", filter.workerId.trim());
    }
    if (filter.fromDate && filter.fromDate.trim()) {
      params = params.set("fromDate", filter.fromDate.trim());
    }
    if (filter.toDate && filter.toDate.trim()) {
      params = params.set("toDate", filter.toDate.trim());
    }
    if (filter.attendanceType && filter.attendanceType.trim()) {
      params = params.set("attendanceType", filter.attendanceType.trim());
    }
    if (filter.status && filter.status.trim()) {
      params = params.set("status", filter.status.trim());
    }
    if (filter.search && filter.search.trim()) {
      params = params.set("search", filter.search.trim());
    }
    if (filter.sortBy && filter.sortBy.trim()) {
      params = params.set("sortBy", filter.sortBy.trim());
    }
    if (filter.sortDescending !== undefined && filter.sortDescending !== null) {
      params = params.set("sortDescending", filter.sortDescending.toString());
    }
    if (filter.page !== undefined && filter.page !== null) {
      params = params.set("page", Math.max(filter.page, 1).toString());
    }
    if (filter.pageSize !== undefined && filter.pageSize !== null) {
      params = params.set("pageSize", Math.min(Math.max(filter.pageSize, 1), 100).toString());
    }

    return this.http.get<PagedResponse<AttendanceRecord>>(
      `${this.api}/attendance/history`,
      { params },
    );
  }

  getAttendanceById(id: string): Observable<AttendanceDetail> {
    return this.http.get<AttendanceDetail>(`${this.api}/attendance/${id}`);
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

  previewCopyPreviousDay(
    farmId: string,
    targetDate: string,
    sourceDate?: string | null,
  ): Observable<CopyPreviousDayPreviewResponse> {
    let params = new HttpParams()
      .set("farmId", farmId)
      .set("targetDate", targetDate);

    if (sourceDate && sourceDate.trim()) {
      params = params.set("sourceDate", sourceDate.trim());
    }

    return this.http.get<CopyPreviousDayPreviewResponse>(
      `${this.api}/attendance/copy-previous-day/preview`,
      { params },
    );
  }

  copyPreviousDay(
    request: CopyPreviousDayAttendanceRequest,
  ): Observable<CopyPreviousDayAttendanceResponse> {
    return this.http.post<CopyPreviousDayAttendanceResponse>(
      `${this.api}/attendance/copy-previous-day`,
      request,
    );
  }
}
