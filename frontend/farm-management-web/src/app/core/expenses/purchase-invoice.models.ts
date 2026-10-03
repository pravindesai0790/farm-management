import { PagedResponse } from '../models/paged-response.model';

export type PurchaseInvoiceStatus = 'Draft' | 'Posted' | 'Reversed';
export type PurchaseInvoicePaymentStatus = 'Unpaid' | 'PartiallyPaid' | 'Paid';
export type PurchaseInvoiceDueStatus = 'Overdue' | 'DueSoon' | 'Current';
export type PurchaseInvoiceReceiptStatus = 'Unlinked' | 'PartiallyReceived' | 'FullyReceived';
export type PurchaseInvoiceLineType = 'InventoryItem' | 'NonInventoryExpense';

export interface PurchaseInvoiceLineResponse {
  readonly id: string;
  readonly purchaseInvoiceId: string;
  readonly lineType: PurchaseInvoiceLineType;
  readonly inventoryItemId?: string | null;
  readonly inventoryItemName?: string | null;
  readonly inventoryItemSku?: string | null;
  readonly expenseCategoryId?: string | null;
  readonly expenseCategoryName?: string | null;
  readonly description?: string | null;
  readonly quantity?: number | null;
  readonly stockUnitId?: string | null;
  readonly stockUnitCode?: string | null;
  readonly stockUnitName?: string | null;
  readonly unitPrice: number;
  readonly lineAmount: number;
  readonly farmAreaId?: string | null;
  readonly farmAreaName?: string | null;
  readonly plantationId?: string | null;
  readonly plantationName?: string | null;
  readonly cropCycleId?: string | null;
  readonly cropCycleName?: string | null;
  readonly cropCycleStageId?: string | null;
  readonly cropCycleStageName?: string | null;
  readonly sortOrder: number;
}

export interface PurchaseInvoiceResponse {
  readonly id: string;
  readonly organizationId: string;
  readonly supplierId: string;
  readonly supplierName: string;
  readonly farmId: string;
  readonly farmName: string;
  readonly supplierInvoiceNumber: string;
  readonly invoiceDate: string;
  readonly dueDate?: string | null;
  readonly currencyId: string;
  readonly currencyCode: string;
  readonly currencySymbol: string;
  readonly paymentTerms?: string | null;
  readonly subtotal: number;
  readonly taxAmount: number;
  readonly otherCharges: number;
  readonly discountAmount: number;
  readonly totalAmount: number;
  readonly amountPaid: number;
  readonly outstandingBalance: number;
  readonly status: PurchaseInvoiceStatus;
  readonly paymentStatus: PurchaseInvoicePaymentStatus;
  readonly dueStatus: PurchaseInvoiceDueStatus;
  readonly receiptStatus: PurchaseInvoiceReceiptStatus;
  readonly notes?: string | null;
  readonly attachmentReference?: string | null;
  readonly postedAt?: string | null;
  readonly postedBy?: string | null;
  readonly reversedAt?: string | null;
  readonly reversedBy?: string | null;
  readonly reversalReason?: string | null;
  readonly createdAt: string;
  readonly createdBy: string;
  readonly updatedAt?: string | null;
  readonly lines: PurchaseInvoiceLineResponse[];
}

export interface CreatePurchaseInvoiceLineRequest {
  readonly lineType: PurchaseInvoiceLineType;
  readonly inventoryItemId?: string | null;
  readonly stockUnitId?: string | null;
  readonly quantity?: number | null;
  readonly unitPrice?: number | null;
  readonly expenseCategoryId?: string | null;
  readonly amount?: number | null;
  readonly description?: string | null;
  readonly farmAreaId?: string | null;
  readonly plantationId?: string | null;
  readonly cropCycleId?: string | null;
  readonly cropCycleStageId?: string | null;
  readonly sortOrder?: number;
}

export interface CreatePurchaseInvoiceRequest {
  readonly supplierId: string;
  readonly farmId: string;
  readonly supplierInvoiceNumber: string;
  readonly invoiceDate: string;
  readonly dueDate?: string | null;
  readonly currencyId: string;
  readonly paymentTerms?: string | null;
  readonly taxAmount?: number;
  readonly otherCharges?: number;
  readonly discountAmount?: number;
  readonly notes?: string | null;
  readonly attachmentReference?: string | null;
  readonly lines: CreatePurchaseInvoiceLineRequest[];
}

export interface UpdatePurchaseInvoiceRequest {
  readonly supplierId: string;
  readonly farmId: string;
  readonly supplierInvoiceNumber: string;
  readonly invoiceDate: string;
  readonly dueDate?: string | null;
  readonly currencyId: string;
  readonly paymentTerms?: string | null;
  readonly taxAmount?: number;
  readonly otherCharges?: number;
  readonly discountAmount?: number;
  readonly notes?: string | null;
  readonly attachmentReference?: string | null;
  readonly lines: CreatePurchaseInvoiceLineRequest[];
}

export interface ReversePurchaseInvoiceRequest {
  readonly reason: string;
}

export interface PurchaseInvoiceFilter {
  readonly search?: string | null;
  readonly supplierId?: string | null;
  readonly farmId?: string | null;
  readonly status?: PurchaseInvoiceStatus | null;
  readonly from?: string | null;
  readonly to?: string | null;
  readonly paymentStatus?: PurchaseInvoicePaymentStatus | null;
  readonly dueStatus?: PurchaseInvoiceDueStatus | null;
  readonly receiptStatus?: PurchaseInvoiceReceiptStatus | null;
}

export type PurchaseInvoiceList = PagedResponse<PurchaseInvoiceResponse>;
