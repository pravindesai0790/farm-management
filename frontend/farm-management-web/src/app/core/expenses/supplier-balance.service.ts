import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { OverdueInvoiceItem } from './expense-report.models';
import { SupplierBalanceFilter, SupplierBalanceSummaryResponse } from './supplier-balance.models';

@Injectable({ providedIn: 'root' })
export class SupplierBalanceService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiUrl}/supplier-balances`;

  getSupplierBalances(filter: SupplierBalanceFilter = {}): Observable<SupplierBalanceSummaryResponse> {
    let params = new HttpParams();
    if (filter.asOfDate) params = params.set('asOfDate', filter.asOfDate);
    if (filter.supplierId) params = params.set('supplierId', filter.supplierId);
    if (filter.farmId) params = params.set('farmId', filter.farmId);
    if (filter.currencyId) params = params.set('currencyId', filter.currencyId);

    return this.http.get<SupplierBalanceSummaryResponse>(this.api, { params });
  }

  getOverdueInvoices(filter: SupplierBalanceFilter = {}): Observable<OverdueInvoiceItem[]> {
    let params = new HttpParams();
    if (filter.asOfDate) params = params.set('asOfDate', filter.asOfDate);
    if (filter.supplierId) params = params.set('supplierId', filter.supplierId);
    if (filter.farmId) params = params.set('farmId', filter.farmId);
    if (filter.currencyId) params = params.set('currencyId', filter.currencyId);

    return this.http.get<OverdueInvoiceItem[]>(`${this.api}/overdue`, { params });
  }
}
