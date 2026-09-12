import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable, catchError, map, of } from "rxjs";
import { environment } from "../../../environments/environment";
import { PagedResponse } from "../models/paged-response.model";
import {
  CancelWorkerPaymentRequest,
  ContractorItem,
  CurrencyItem,
  CreateWorkerFarmAssignmentRequest,
  CreateWorkerRequest,
  CreateLaborWageRateRequest,
  EndWorkerFarmAssignmentRequest,
  LaborCategoryItem,
  LaborWageRate,
  RecordWorkerPaymentRequest,
  UpdateLaborWageRateRequest,
  UpdateWorkerFarmAssignmentRequest,
  UpdateWorkerRequest,
  WorkerDetail,
  WorkerEarningsLedgerItem,
  WorkerFarmAssignment,
  WorkerFinancialSummary,
  WorkerList,
  WorkerPayment,
  WorkerPaymentAllocationItem,
  WorkerSettlementCalculation,
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
    fromDate?: string | null,
    toDate?: string | null,
  ): Observable<readonly WorkerPayment[]> {
    let params = new HttpParams().set("pageSize", "100");
    if (fromDate) {
      params = params.set("fromDate", fromDate);
    }
    if (toDate) {
      params = params.set("toDate", toDate);
    }

    return this.http
      .get<readonly WorkerPayment[] | PagedResponse<WorkerPayment>>(
        `${this.api}/labor/workers/${workerId}/payments`,
        { params },
      )
      .pipe(
        catchError(() => of([] as readonly WorkerPayment[])),
        map((val) => {
          if (Array.isArray(val)) {
            return val;
          }
          if (val && "items" in val && Array.isArray((val as any).items)) {
            return (val as any).items;
          }
          return [];
        }),
      );
  }

  listWorkerPaymentsPaged(
    workerId: string,
    page = 1,
    pageSize = 20,
    fromDate?: string | null,
    toDate?: string | null,
    paymentType?: string | null,
    status?: string | null,
  ): Observable<PagedResponse<WorkerPayment>> {
    let params = new HttpParams()
      .set("page", page.toString())
      .set("pageSize", pageSize.toString());

    if (fromDate) {
      params = params.set("fromDate", fromDate);
    }
    if (toDate) {
      params = params.set("toDate", toDate);
    }
    if (paymentType && paymentType !== "all") {
      params = params.set("paymentType", paymentType);
    }
    if (status && status !== "all") {
      params = params.set("status", status);
    }

    return this.http.get<PagedResponse<WorkerPayment>>(
      `${this.api}/labor/workers/${workerId}/payments`,
      { params },
    );
  }

  getWorkerPayment(workerId: string, paymentId: string): Observable<WorkerPayment> {
    return this.http.get<WorkerPayment>(
      `${this.api}/labor/workers/${workerId}/payments/${paymentId}`,
    );
  }

  recordWorkerPayment(
    workerId: string,
    request: RecordWorkerPaymentRequest,
  ): Observable<WorkerPayment> {
    return this.http.post<WorkerPayment>(
      `${this.api}/labor/workers/${workerId}/payments`,
      request,
    );
  }

  cancelWorkerPayment(
    workerId: string,
    paymentId: string,
    request: CancelWorkerPaymentRequest,
  ): Observable<WorkerPayment> {
    return this.http.post<WorkerPayment>(
      `${this.api}/labor/workers/${workerId}/payments/${paymentId}/cancel`,
      request,
    );
  }

  getWorkerSettlement(
    workerId: string,
    periodFrom?: string | null,
    periodTo?: string | null,
    asOfDate?: string | null,
  ): Observable<WorkerSettlementCalculation> {
    let params = new HttpParams();
    if (periodFrom) {
      params = params.set("periodFrom", periodFrom);
    }
    if (periodTo) {
      params = params.set("periodTo", periodTo);
    }
    if (asOfDate) {
      params = params.set("asOfDate", asOfDate);
    }

    return this.http.get<WorkerSettlementCalculation>(
      `${this.api}/labor/workers/${workerId}/settlement`,
      { params },
    );
  }

  getWorkerFinancialSummary(
    workerId: string,
    asOfDate?: string | null,
  ): Observable<WorkerFinancialSummary> {
    let params = new HttpParams();
    if (asOfDate) {
      params = params.set("asOfDate", asOfDate);
    }

    return this.http.get<WorkerFinancialSummary>(
      `${this.api}/labor/workers/${workerId}/payments/summary`,
      { params },
    );
  }

  listWorkerEarnings(
    workerId: string,
    fromDate?: string | null,
    toDate?: string | null,
    status?: string | null,
    entryType?: string | null,
    page = 1,
    pageSize = 100,
  ): Observable<PagedResponse<WorkerEarningsLedgerItem>> {
    let params = new HttpParams()
      .set("page", page.toString())
      .set("pageSize", pageSize.toString());

    if (fromDate) {
      params = params.set("fromDate", fromDate);
    }
    if (toDate) {
      params = params.set("toDate", toDate);
    }
    if (status && status !== "all") {
      params = params.set("status", status);
    }
    if (entryType && entryType !== "all") {
      params = params.set("entryType", entryType);
    }

    return this.http.get<PagedResponse<WorkerEarningsLedgerItem>>(
      `${this.api}/labor/workers/${workerId}/earnings`,
      { params },
    ).pipe(
      catchError(() =>
        of({
          items: [] as WorkerEarningsLedgerItem[],
          totalCount: 0,
          page: 1,
          pageSize,
          totalPages: 0,
          hasNextPage: false,
          hasPreviousPage: false,
        } as PagedResponse<WorkerEarningsLedgerItem>),
      ),
    );
  }

  listWorkerPaymentAllocations(
    workerId: string,
  ): Observable<readonly WorkerPaymentAllocationItem[]> {
    return this.http
      .get<readonly WorkerPaymentAllocationItem[]>(
        `${this.api}/labor/workers/${workerId}/allocations`,
      )
      .pipe(catchError(() => of([] as readonly WorkerPaymentAllocationItem[])));
  }
}


