import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CurrencyItem } from '../labor/labor.models';
import {
  CreateExpenseRequest,
  Expense,
  ExpenseFilter,
  ExpenseList,
  ReverseExpenseRequest,
  UpdateExpenseRequest,
} from './expense.models';

@Injectable({ providedIn: 'root' })
export class ExpenseService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiUrl}/expenses`;
  private readonly masterDataApi = `${environment.apiUrl}/master-data`;

  list(
    filter: ExpenseFilter = {},
    page: number = 1,
    pageSize: number = 20,
  ): Observable<ExpenseList> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (filter.farmId) params = params.set('farmId', filter.farmId);
    if (filter.categoryId) params = params.set('categoryId', filter.categoryId);
    if (filter.supplierId) params = params.set('supplierId', filter.supplierId);
    if (filter.status) params = params.set('status', filter.status);
    if (filter.from) params = params.set('from', filter.from);
    if (filter.to) params = params.set('to', filter.to);
    if (filter.search && filter.search.trim()) {
      params = params.set('search', filter.search.trim());
    }

    return this.http.get<ExpenseList>(this.api, { params });
  }

  get(id: string): Observable<Expense> {
    return this.http.get<Expense>(`${this.api}/${id}`);
  }

  createDraft(request: CreateExpenseRequest): Observable<Expense> {
    return this.http.post<Expense>(this.api, request);
  }

  updateDraft(id: string, request: UpdateExpenseRequest): Observable<Expense> {
    return this.http.put<Expense>(`${this.api}/${id}`, request);
  }

  post(id: string): Observable<Expense> {
    return this.http.post<Expense>(`${this.api}/${id}/post`, {});
  }

  reverse(id: string, request: ReverseExpenseRequest): Observable<Expense> {
    return this.http.post<Expense>(`${this.api}/${id}/reverse`, request);
  }

  listCurrencies(): Observable<readonly CurrencyItem[]> {
    return this.http.get<readonly CurrencyItem[]>(`${this.masterDataApi}/currencies`);
  }
}
