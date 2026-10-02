import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import {
  CreateSupplierRequest,
  Supplier,
  SupplierList,
  UpdateSupplierRequest,
  UpdateSupplierStatusRequest,
} from "./supplier.models";

@Injectable({ providedIn: "root" })
export class SupplierService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiUrl}/suppliers`;

  list(
    page: number = 1,
    pageSize: number = 20,
    search?: string | null,
    isActive?: boolean | null,
  ): Observable<SupplierList> {
    let params = new HttpParams()
      .set("page", page.toString())
      .set("pageSize", pageSize.toString());

    if (search) params = params.set("search", search.trim());
    if (isActive !== null && isActive !== undefined) {
      params = params.set("isActive", isActive.toString());
    }

    return this.http.get<SupplierList>(this.api, { params });
  }

  get(id: string): Observable<Supplier> {
    return this.http.get<Supplier>(`${this.api}/${id}`);
  }

  create(request: CreateSupplierRequest): Observable<Supplier> {
    return this.http.post<Supplier>(this.api, request);
  }

  update(id: string, request: UpdateSupplierRequest): Observable<Supplier> {
    return this.http.put<Supplier>(`${this.api}/${id}`, request);
  }

  updateStatus(id: string, request: UpdateSupplierStatusRequest): Observable<void> {
    return this.http.patch<void>(`${this.api}/${id}/status`, request);
  }

  activate(id: string): Observable<void> {
    return this.http.patch<void>(`${this.api}/${id}/activate`, {});
  }

  deactivate(id: string): Observable<void> {
    return this.http.patch<void>(`${this.api}/${id}/deactivate`, {});
  }
}
