import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable, catchError, of } from "rxjs";
import { environment } from "../../../environments/environment";
import { PagedResponse } from "../models/paged-response.model";
import {
  ContractorItem,
  CurrencyItem,
  CreateWorkerFarmAssignmentRequest,
  CreateWorkerRequest,
  CreateLaborWageRateRequest,
  EndWorkerFarmAssignmentRequest,
  LaborCategoryItem,
  LaborWageRate,
  UpdateLaborWageRateRequest,
  UpdateWorkerFarmAssignmentRequest,
  UpdateWorkerRequest,
  WorkerDetail,
  WorkerFarmAssignment,
  WorkerList,
  WorkerPayment,
} from "./labor.models";

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

  createWorker(request: CreateWorkerRequest): Observable<WorkerDetail> {
    return this.http.post<WorkerDetail>(`${this.api}/labor/workers`, request);
  }

  updateWorker(
    id: string,
    request: UpdateWorkerRequest,
  ): Observable<WorkerDetail> {
    return this.http.put<WorkerDetail>(`${this.api}/labor/workers/${id}`, request);
  }

  activateWorker(id: string): Observable<void> {
    return this.http.post<void>(`${this.api}/labor/workers/${id}/activate`, {});
  }

  deactivateWorker(id: string): Observable<void> {
    return this.http.post<void>(`${this.api}/labor/workers/${id}/deactivate`, {});
  }

  listContractors(
    page = 1,
    pageSize = 100,
    search?: string | null,
    isActive?: boolean | null,
  ): Observable<PagedResponse<ContractorItem>> {
    let params = new HttpParams()
      .set("page", page.toString())
      .set("pageSize", pageSize.toString());

    if (search && search.trim()) {
      params = params.set("search", search.trim());
    }
    if (isActive !== null && isActive !== undefined) {
      params = params.set("isActive", isActive.toString());
    }

    return this.http.get<PagedResponse<ContractorItem>>(
      `${this.api}/labor/contractors`,
      { params },
    );
  }

  listLaborCategories(
    page = 1,
    pageSize = 100,
    search?: string | null,
    isActive?: boolean | null,
  ): Observable<PagedResponse<LaborCategoryItem>> {
    let params = new HttpParams()
      .set("page", page.toString())
      .set("pageSize", pageSize.toString());

    if (search && search.trim()) {
      params = params.set("search", search.trim());
    }
    if (isActive !== null && isActive !== undefined) {
      params = params.set("isActive", isActive.toString());
    }

    return this.http.get<PagedResponse<LaborCategoryItem>>(
      `${this.api}/labor/categories`,
      { params },
    );
  }

  listWorkerFarmAssignments(
    workerId: string,
    isActive?: boolean | null,
  ): Observable<readonly WorkerFarmAssignment[]> {
    let params = new HttpParams();
    if (isActive !== null && isActive !== undefined) {
      params = params.set("isActive", isActive.toString());
    }

    return this.http.get<readonly WorkerFarmAssignment[]>(
      `${this.api}/labor/workers/${workerId}/farms`,
      { params },
    );
  }

  createWorkerFarmAssignment(
    workerId: string,
    request: CreateWorkerFarmAssignmentRequest,
  ): Observable<WorkerFarmAssignment> {
    return this.http.post<WorkerFarmAssignment>(
      `${this.api}/labor/workers/${workerId}/farms`,
      request,
    );
  }

  updateWorkerFarmAssignment(
    workerId: string,
    assignmentId: string,
    request: UpdateWorkerFarmAssignmentRequest,
  ): Observable<WorkerFarmAssignment> {
    return this.http.put<WorkerFarmAssignment>(
      `${this.api}/labor/workers/${workerId}/farms/${assignmentId}`,
      request,
    );
  }

  endWorkerFarmAssignment(
    workerId: string,
    assignmentId: string,
    request: EndWorkerFarmAssignmentRequest,
  ): Observable<WorkerFarmAssignment> {
    return this.http.post<WorkerFarmAssignment>(
      `${this.api}/labor/workers/${workerId}/farms/${assignmentId}/end`,
      request,
    );
  }

  deactivateWorkerFarmAssignment(
    workerId: string,
    assignmentId: string,
  ): Observable<void> {
    return this.http.post<void>(
      `${this.api}/labor/workers/${workerId}/farms/${assignmentId}/deactivate`,
      {},
    );
  }

  activateWorkerFarmAssignment(
    workerId: string,
    assignmentId: string,
  ): Observable<void> {
    return this.http.post<void>(
      `${this.api}/labor/workers/${workerId}/farms/${assignmentId}/activate`,
      {},
    );
  }

  listWageRates(
    gender?: string | null,
    isActive = true,
  ): Observable<readonly LaborWageRate[]> {
    let params = new HttpParams();
    if (gender && gender !== "all") {
      params = params.set("gender", gender);
    }
    if (isActive !== null && isActive !== undefined) {
      params = params.set("isActive", isActive.toString());
    }

    return this.http
      .get<readonly LaborWageRate[] | PagedResponse<LaborWageRate>>(
        `${this.api}/labor/wage-rates`,
        { params },
      )
      .pipe(
        catchError(() => of([] as readonly LaborWageRate[])),
        // Handle array or paged response seamlessly
        (source$) =>
          new Observable<readonly LaborWageRate[]>((observer) => {
            return source$.subscribe({
              next: (val) => {
                if (Array.isArray(val)) {
                  observer.next(val);
                } else if (val && "items" in val && Array.isArray((val as any).items)) {
                  observer.next((val as any).items);
                } else {
                  observer.next([]);
                }
              },
              error: () => observer.next([]),
              complete: () => observer.complete(),
            });
          }),
      );
  }

  listWageRatesPaged(
    page = 1,
    pageSize = 20,
    gender?: string | null,
    wageType?: string | null,
    isActive?: boolean | null,
    businessDate?: string | null,
  ): Observable<PagedResponse<LaborWageRate>> {
    let params = new HttpParams()
      .set("page", page.toString())
      .set("pageSize", pageSize.toString());

    if (gender && gender !== "all") {
      params = params.set("gender", gender);
    }
    if (wageType && wageType !== "all") {
      params = params.set("wageType", wageType);
    }
    if (isActive !== null && isActive !== undefined) {
      params = params.set("isActive", isActive.toString());
    }
    if (businessDate) {
      params = params.set("businessDate", businessDate);
    }

    return this.http.get<PagedResponse<LaborWageRate>>(
      `${this.api}/labor/wage-rates`,
      { params },
    );
  }

  getWageRate(id: string): Observable<LaborWageRate> {
    return this.http.get<LaborWageRate>(`${this.api}/labor/wage-rates/${id}`);
  }

  createWageRate(
    request: CreateLaborWageRateRequest,
  ): Observable<LaborWageRate> {
    return this.http.post<LaborWageRate>(
      `${this.api}/labor/wage-rates`,
      request,
    );
  }

  updateWageRate(
    id: string,
    request: UpdateLaborWageRateRequest,
  ): Observable<LaborWageRate> {
    return this.http.put<LaborWageRate>(
      `${this.api}/labor/wage-rates/${id}`,
      request,
    );
  }

  activateWageRate(id: string): Observable<void> {
    return this.http.post<void>(
      `${this.api}/labor/wage-rates/${id}/activate`,
      {},
    );
  }

  deactivateWageRate(id: string): Observable<void> {
    return this.http.post<void>(
      `${this.api}/labor/wage-rates/${id}/deactivate`,
      {},
    );
  }

  getApplicableWageRate(
    gender: string,
    wageType: string,
    businessDate: string,
  ): Observable<LaborWageRate | null> {
    const params = new HttpParams()
      .set("gender", gender)
      .set("wageType", wageType)
      .set("businessDate", businessDate);

    return this.http
      .get<LaborWageRate>(`${this.api}/labor/wage-rates/applicable`, { params })
      .pipe(catchError(() => of(null)));
  }

  listCurrencies(): Observable<readonly CurrencyItem[]> {
    return this.http
      .get<readonly CurrencyItem[]>(`${this.api}/master-data/currencies`)
      .pipe(
        catchError(() =>
          of([
            {
              id: "10000000-0000-0000-0000-000000000001",
              code: "INR",
              name: "Indian Rupee",
              symbol: "₹",
              isSystem: true,
              isActive: true,
              displayOrder: 1,
            },
          ] as readonly CurrencyItem[]),
        ),
      );
  }

  listWorkerPayments(
    workerId: string,
  ): Observable<readonly WorkerPayment[]> {
    return this.http
      .get<readonly WorkerPayment[] | PagedResponse<WorkerPayment>>(
        `${this.api}/labor/workers/${workerId}/payments`,
      )
      .pipe(
        catchError(() => of([] as readonly WorkerPayment[])),
        (source$) =>
          new Observable<readonly WorkerPayment[]>((observer) => {
            return source$.subscribe({
              next: (val) => {
                if (Array.isArray(val)) {
                  observer.next(val);
                } else if (val && "items" in val && Array.isArray((val as any).items)) {
                  observer.next((val as any).items);
                } else {
                  observer.next([]);
                }
              },
              error: () => observer.next([]),
              complete: () => observer.complete(),
            });
          }),
      );
  }
}


