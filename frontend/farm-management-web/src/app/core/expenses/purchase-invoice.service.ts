import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreatePurchaseInvoiceRequest,
  PurchaseInvoiceFilter,
  PurchaseInvoiceList,
  PurchaseInvoiceReceiptGroupResponse,
  PurchaseInvoiceReceiptSummaryResponse,
  PurchaseInvoiceRemainingLineResponse,
  PurchaseInvoiceResponse,
  ReceivePurchaseInvoiceItemsRequest,
  ReversePurchaseInvoiceRequest,
  UpdatePurchaseInvoiceRequest,
} from './purchase-invoice.models';

@Injectable({ providedIn: 'root' })
export class PurchaseInvoiceService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiUrl}/purchase-invoices`;

  list(
    filter: PurchaseInvoiceFilter = {},
    page: number = 1,
    pageSize: number = 20,
  ): Observable<PurchaseInvoiceList> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (filter.search && filter.search.trim()) {
      params = params.set('search', filter.search.trim());
    }
    if (filter.supplierId) params = params.set('supplierId', filter.supplierId);
    if (filter.farmId) params = params.set('farmId', filter.farmId);
    if (filter.status) params = params.set('status', filter.status);
    if (filter.from) params = params.set('from', filter.from);
    if (filter.to) params = params.set('to', filter.to);
    if (filter.paymentStatus) params = params.set('paymentStatus', filter.paymentStatus);
    if (filter.dueStatus) params = params.set('dueStatus', filter.dueStatus);
    if (filter.receiptStatus) params = params.set('receiptStatus', filter.receiptStatus);

    return this.http.get<PurchaseInvoiceList>(this.api, { params });
  }

  get(id: string): Observable<PurchaseInvoiceResponse> {
    return this.http.get<PurchaseInvoiceResponse>(`${this.api}/${id}`);
  }

  createDraft(request: CreatePurchaseInvoiceRequest): Observable<PurchaseInvoiceResponse> {
    return this.http.post<PurchaseInvoiceResponse>(this.api, request);
  }

  updateDraft(id: string, request: UpdatePurchaseInvoiceRequest): Observable<PurchaseInvoiceResponse> {
    return this.http.put<PurchaseInvoiceResponse>(`${this.api}/${id}`, request);
  }

  post(id: string): Observable<PurchaseInvoiceResponse> {
    return this.http.post<PurchaseInvoiceResponse>(`${this.api}/${id}/post`, {});
  }

  reverse(id: string, request: ReversePurchaseInvoiceRequest): Observable<PurchaseInvoiceResponse> {
    return this.http.post<PurchaseInvoiceResponse>(`${this.api}/${id}/reverse`, request);
  }

  getReceiptSummary(id: string): Observable<PurchaseInvoiceReceiptSummaryResponse> {
    return this.http.get<PurchaseInvoiceReceiptSummaryResponse>(`${this.api}/${id}/receipt-summary`);
  }

  getRemainingToReceive(id: string): Observable<PurchaseInvoiceRemainingLineResponse[]> {
    return this.http.get<PurchaseInvoiceRemainingLineResponse[]>(`${this.api}/${id}/lines/remaining-to-receive`);
  }

  getReceiptHistory(id: string): Observable<PurchaseInvoiceReceiptGroupResponse[]> {
    return this.http.get<PurchaseInvoiceReceiptGroupResponse[]>(`${this.api}/${id}/receipts`);
  }

  receiveItems(id: string, request: ReceivePurchaseInvoiceItemsRequest): Observable<PurchaseInvoiceReceiptGroupResponse> {
    return this.http.post<PurchaseInvoiceReceiptGroupResponse>(`${this.api}/${id}/receipts`, request);
  }
}
