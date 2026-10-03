import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  RecordSupplierPaymentRequest,
  ReverseSupplierPaymentRequest,
  SupplierPaymentFilter,
  SupplierPaymentList,
  SupplierPaymentResponse,
  UnpaidPurchaseInvoiceSummaryResponse,
} from './supplier-payment.models';

@Injectable({ providedIn: 'root' })
export class SupplierPaymentService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiUrl}/supplier-payments`;

  list(
    filter: SupplierPaymentFilter = {},
    page: number = 1,
    pageSize: number = 20,
  ): Observable<SupplierPaymentList> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (filter.search && filter.search.trim()) {
      params = params.set('search', filter.search.trim());
    }
    if (filter.supplierId) params = params.set('supplierId', filter.supplierId);
    if (filter.invoiceId) params = params.set('invoiceId', filter.invoiceId);
    if (filter.status) params = params.set('status', filter.status);
    if (filter.from) params = params.set('from', filter.from);
    if (filter.to) params = params.set('to', filter.to);

    return this.http.get<SupplierPaymentList>(this.api, { params });
  }

  getById(id: string): Observable<SupplierPaymentResponse> {
    return this.http.get<SupplierPaymentResponse>(`${this.api}/${id}`);
  }

  getUnpaidInvoices(supplierId: string, currencyId?: string): Observable<UnpaidPurchaseInvoiceSummaryResponse[]> {
    let params = new HttpParams().set('supplierId', supplierId);
    if (currencyId) {
      params = params.set('currencyId', currencyId);
    }
    return this.http.get<UnpaidPurchaseInvoiceSummaryResponse[]>(`${this.api}/unpaid-invoices`, { params });
  }

  recordPayment(request: RecordSupplierPaymentRequest): Observable<SupplierPaymentResponse> {
    return this.http.post<SupplierPaymentResponse>(this.api, request);
  }

  reversePayment(id: string, request: ReverseSupplierPaymentRequest): Observable<SupplierPaymentResponse> {
    return this.http.post<SupplierPaymentResponse>(`${this.api}/${id}/reverse`, request);
  }
}
