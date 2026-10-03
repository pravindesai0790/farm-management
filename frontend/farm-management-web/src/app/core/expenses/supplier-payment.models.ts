import { PagedResponse } from '../models/paged-response.model';

export type PaymentMethod = 'BankTransfer' | 'Cash' | 'Upi' | 'Cheque' | 'Other';
export type SupplierPaymentStatus = 'Completed' | 'Reversed';

export interface SupplierPaymentAllocationRequest {
  readonly purchaseInvoiceId: string;
  readonly allocatedAmount: number;
}

export interface RecordSupplierPaymentRequest {
  readonly supplierId: string;
  readonly paymentDate: string;
  readonly amount: number;
  readonly currencyId: string;
  readonly paymentMethod: PaymentMethod;
  readonly allocations: SupplierPaymentAllocationRequest[];
  readonly referenceNumber?: string | null;
  readonly notes?: string | null;
  readonly idempotencyKey?: string | null;
}

export interface ReverseSupplierPaymentRequest {
  readonly reason: string;
}

export interface SupplierPaymentFilter {
  readonly from?: string | null;
  readonly to?: string | null;
  readonly supplierId?: string | null;
  readonly invoiceId?: string | null;
  readonly status?: SupplierPaymentStatus | null;
  readonly search?: string | null;
}

export interface SupplierPaymentAllocationResponse {
  readonly id: string;
  readonly supplierPaymentId: string;
  readonly purchaseInvoiceId: string;
  readonly supplierInvoiceNumber: string;
  readonly invoiceDate: string;
  readonly invoiceTotalAmount: number;
  readonly allocatedAmount: number;
}

export interface SupplierPaymentResponse {
  readonly id: string;
  readonly organizationId: string;
  readonly supplierId: string;
  readonly supplierName: string;
  readonly paymentDate: string;
  readonly amount: number;
  readonly currencyId: string;
  readonly currencyCode: string;
  readonly currencySymbol: string;
  readonly paymentMethod: PaymentMethod;
  readonly status: SupplierPaymentStatus;
  readonly referenceNumber?: string | null;
  readonly notes?: string | null;
  readonly reversedAt?: string | null;
  readonly reversedBy?: string | null;
  readonly reversalReason?: string | null;
  readonly createdAt: string;
  readonly createdBy: string;
  readonly updatedAt?: string | null;
  readonly updatedBy?: string | null;
  readonly allocations: SupplierPaymentAllocationResponse[];
}

export interface UnpaidPurchaseInvoiceSummaryResponse {
  readonly id: string;
  readonly supplierInvoiceNumber: string;
  readonly invoiceDate: string;
  readonly dueDate?: string | null;
  readonly currencyId: string;
  readonly currencyCode: string;
  readonly currencySymbol: string;
  readonly totalAmount: number;
  readonly amountPaid: number;
  readonly outstandingBalance: number;
  readonly paymentStatus: string;
  readonly dueStatus: string;
}

export type SupplierPaymentList = PagedResponse<SupplierPaymentResponse>;
