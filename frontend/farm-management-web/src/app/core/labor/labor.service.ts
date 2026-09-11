import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { WorkerDetail, WorkerList } from "./labor.models";

@Injectable({ providedIn: "root" })
export class LaborService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  listWorkers(
    page: number,
    pageSize: number,
    search?: string | null,
    isActive?: boolean | null,
    gender?: string | null,
    employmentType?: string | null,
    contractorId?: string | null,
    laborCategoryId?: string | null,
  ): Observable<WorkerList> {
    let params = new HttpParams()
      .set("page", page.toString())
      .set("pageSize", pageSize.toString());

    if (search && search.trim()) {
      params = params.set("search", search.trim());
    }
    if (isActive !== null && isActive !== undefined) {
      params = params.set("isActive", isActive.toString());
    }
    if (gender && gender !== "all") {
      params = params.set("gender", gender);
    }
    if (employmentType && employmentType !== "all") {
      params = params.set("employmentType", employmentType);
    }
    if (contractorId) {
      params = params.set("contractorId", contractorId);
    }
    if (laborCategoryId) {
      params = params.set("laborCategoryId", laborCategoryId);
    }

    return this.http.get<WorkerList>(`${this.api}/labor/workers`, { params });
  }

  getWorker(id: string): Observable<WorkerDetail> {
    return this.http.get<WorkerDetail>(`${this.api}/labor/workers/${id}`);
  }

  activateWorker(id: string): Observable<void> {
    return this.http.post<void>(`${this.api}/labor/workers/${id}/activate`, {});
  }

  deactivateWorker(id: string): Observable<void> {
    return this.http.post<void>(`${this.api}/labor/workers/${id}/deactivate`, {});
  }
}
