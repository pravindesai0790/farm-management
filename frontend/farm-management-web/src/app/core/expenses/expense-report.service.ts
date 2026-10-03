import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ExpenseReportFilter, ExpenseReportSummaryResponse } from './expense-report.models';

@Injectable({ providedIn: 'root' })
export class ExpenseReportService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiUrl}/expense-reports`;

  getSummaryReport(filter: ExpenseReportFilter = {}): Observable<ExpenseReportSummaryResponse> {
    let params = new HttpParams();
    if (filter.from) params = params.set('from', filter.from);
    if (filter.to) params = params.set('to', filter.to);
    if (filter.farmId) params = params.set('farmId', filter.farmId);
    if (filter.categoryId) params = params.set('categoryId', filter.categoryId);
    if (filter.cropCycleId) params = params.set('cropCycleId', filter.cropCycleId);
    if (filter.currencyId) params = params.set('currencyId', filter.currencyId);

    return this.http.get<ExpenseReportSummaryResponse>(`${this.api}/summary`, { params });
  }
}
