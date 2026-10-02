import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import {
  CreateExpenseCategoryRequest,
  ExpenseCategory,
  ExpenseCategoryList,
  UpdateExpenseCategoryRequest,
  UpdateExpenseCategoryStatusRequest,
} from "./expense-category.models";

@Injectable({ providedIn: "root" })
export class ExpenseCategoryService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiUrl}/expense-categories`;

  list(
    page: number = 1,
    pageSize: number = 50,
    search?: string | null,
    isActive?: boolean | null,
  ): Observable<ExpenseCategoryList> {
    let params = new HttpParams()
      .set("page", page.toString())
      .set("pageSize", pageSize.toString());

    if (search) params = params.set("search", search.trim());
    if (isActive !== null && isActive !== undefined) {
      params = params.set("isActive", isActive.toString());
    }

    return this.http.get<ExpenseCategoryList>(this.api, { params });
  }

  get(id: string): Observable<ExpenseCategory> {
    return this.http.get<ExpenseCategory>(`${this.api}/${id}`);
  }

  create(request: CreateExpenseCategoryRequest): Observable<ExpenseCategory> {
    return this.http.post<ExpenseCategory>(this.api, request);
  }

  update(id: string, request: UpdateExpenseCategoryRequest): Observable<ExpenseCategory> {
    return this.http.put<ExpenseCategory>(`${this.api}/${id}`, request);
  }

  updateStatus(id: string, request: UpdateExpenseCategoryStatusRequest): Observable<void> {
    return this.http.patch<void>(`${this.api}/${id}/status`, request);
  }

  activate(id: string): Observable<void> {
    return this.http.patch<void>(`${this.api}/${id}/activate`, {});
  }

  deactivate(id: string): Observable<void> {
    return this.http.patch<void>(`${this.api}/${id}/deactivate`, {});
  }
}
