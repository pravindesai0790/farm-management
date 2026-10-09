import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { PagedResponse } from "../models/paged-response.model";
import { SprayProductLookupResponse } from "../sprays/spray.models";
import {
  CreatePlantProtectionProductRequest,
  PlantProtectionProductQuery,
  PlantProtectionProductResponse,
  UpdatePlantProtectionProductRequest,
} from "./plant-protection.models";

@Injectable({ providedIn: "root" })
export class PlantProtectionService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiUrl}/plant-protection-products`;

  listProducts(
    query: PlantProtectionProductQuery = {},
  ): Observable<PagedResponse<PlantProtectionProductResponse>> {
    let params = new HttpParams()
      .set("page", (query.page ?? 1).toString())
      .set("pageSize", (query.pageSize ?? 20).toString());

    if (query.search?.trim()) {
      params = params.set("search", query.search.trim());
    }
    if (query.productTypeId) {
      params = params.set("productTypeId", query.productTypeId);
    }
    if (query.isActive !== undefined && query.isActive !== null) {
      params = params.set("isActive", query.isActive.toString());
    }

    return this.http.get<PagedResponse<PlantProtectionProductResponse>>(this.api, { params });
  }

  getProduct(id: string): Observable<PlantProtectionProductResponse> {
    return this.http.get<PlantProtectionProductResponse>(`${this.api}/${id}`);
  }

  getProductLookup(): Observable<readonly SprayProductLookupResponse[]> {
    return this.http.get<readonly SprayProductLookupResponse[]>(`${this.api}/lookup`);
  }

  createProduct(
    request: CreatePlantProtectionProductRequest,
  ): Observable<PlantProtectionProductResponse> {
    return this.http.post<PlantProtectionProductResponse>(this.api, request);
  }

  updateProduct(
    id: string,
    request: UpdatePlantProtectionProductRequest,
  ): Observable<PlantProtectionProductResponse> {
    return this.http.put<PlantProtectionProductResponse>(`${this.api}/${id}`, request);
  }

  activateProduct(id: string): Observable<void> {
    return this.http.patch<void>(`${this.api}/${id}/activate`, {});
  }

  deactivateProduct(id: string): Observable<void> {
    return this.http.patch<void>(`${this.api}/${id}/deactivate`, {});
  }
}
