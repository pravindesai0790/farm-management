import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import {
  ApplicationMethodItem,
  CreateApplicationMethodRequest,
  CreateProductTypeRequest,
  CreateTargetRequest,
  ProductTypeItem,
  TargetItem,
  UpdateApplicationMethodRequest,
  UpdateProductTypeRequest,
  UpdateTargetRequest,
} from "./spray-master-data.models";

@Injectable({ providedIn: "root" })
export class SprayMasterDataService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiUrl}/master-data`;

  // --- Product Types ---
  listProductTypes(includeInactive = false): Observable<ProductTypeItem[]> {
    const params = new HttpParams().set("includeInactive", includeInactive.toString());
    return this.http.get<ProductTypeItem[]>(`${this.api}/product-types`, { params });
  }

  createProductType(request: CreateProductTypeRequest): Observable<ProductTypeItem> {
    return this.http.post<ProductTypeItem>(`${this.api}/product-types`, request);
  }

  updateProductType(id: string, request: UpdateProductTypeRequest): Observable<ProductTypeItem> {
    return this.http.put<ProductTypeItem>(`${this.api}/product-types/${id}`, request);
  }

  activateProductType(id: string): Observable<void> {
    return this.http.patch<void>(`${this.api}/product-types/${id}/activate`, {});
  }

  deactivateProductType(id: string): Observable<void> {
    return this.http.patch<void>(`${this.api}/product-types/${id}/deactivate`, {});
  }

  // --- Targets ---
  listTargets(type?: string | null, includeInactive = false): Observable<TargetItem[]> {
    let params = new HttpParams().set("includeInactive", includeInactive.toString());
    if (type?.trim()) {
      params = params.set("type", type.trim());
    }
    return this.http.get<TargetItem[]>(`${this.api}/targets`, { params });
  }

  createTarget(request: CreateTargetRequest): Observable<TargetItem> {
    return this.http.post<TargetItem>(`${this.api}/targets`, request);
  }

  updateTarget(id: string, request: UpdateTargetRequest): Observable<TargetItem> {
    return this.http.put<TargetItem>(`${this.api}/targets/${id}`, request);
  }

  activateTarget(id: string): Observable<void> {
    return this.http.patch<void>(`${this.api}/targets/${id}/activate`, {});
  }

  deactivateTarget(id: string): Observable<void> {
    return this.http.patch<void>(`${this.api}/targets/${id}/deactivate`, {});
  }

  // --- Application Methods ---
  listApplicationMethods(includeInactive = false): Observable<ApplicationMethodItem[]> {
    const params = new HttpParams().set("includeInactive", includeInactive.toString());
    return this.http.get<ApplicationMethodItem[]>(`${this.api}/application-methods`, { params });
  }

  createApplicationMethod(request: CreateApplicationMethodRequest): Observable<ApplicationMethodItem> {
    return this.http.post<ApplicationMethodItem>(`${this.api}/application-methods`, request);
  }

  updateApplicationMethod(id: string, request: UpdateApplicationMethodRequest): Observable<ApplicationMethodItem> {
    return this.http.put<ApplicationMethodItem>(`${this.api}/application-methods/${id}`, request);
  }

  activateApplicationMethod(id: string): Observable<void> {
    return this.http.patch<void>(`${this.api}/application-methods/${id}/activate`, {});
  }

  deactivateApplicationMethod(id: string): Observable<void> {
    return this.http.patch<void>(`${this.api}/application-methods/${id}/deactivate`, {});
  }
}
