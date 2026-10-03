import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of } from 'rxjs';

import { PurchaseInvoiceReceiveDialogComponent } from './purchase-invoice-receive-dialog.component';
import { PurchaseInvoiceService } from '../../../../core/expenses/purchase-invoice.service';
import { InventoryService } from '../../../../core/inventory/inventory.service';
import { PurchaseInvoiceResponse } from '../../../../core/expenses/purchase-invoice.models';

describe('PurchaseInvoiceReceiveDialogComponent', () => {
  let component: PurchaseInvoiceReceiveDialogComponent;
  let fixture: ComponentFixture<PurchaseInvoiceReceiveDialogComponent>;

  const mockInvoice: PurchaseInvoiceResponse = {
    id: 'inv-123',
    organizationId: 'org-1',
    supplierId: 'sup-1',
    supplierName: 'Agro Seeds Ltd',
    farmId: 'farm-1',
    farmName: 'Green Valley Farm',
    supplierInvoiceNumber: 'INV-2026-001',
    invoiceDate: '2026-10-01',
    dueDate: '2026-10-31',
    currencyId: 'curr-1',
    currencyCode: 'INR',
    currencySymbol: '₹',
    paymentTerms: 'Net 30',
    subtotal: 1000,
    taxAmount: 50,
    otherCharges: 0,
    discountAmount: 0,
    totalAmount: 1050,
    amountPaid: 0,
    outstandingBalance: 1050,
    status: 'Posted',
    paymentStatus: 'Unpaid',
    dueStatus: 'Current',
    receiptStatus: 'PartiallyReceived',
    createdAt: '2026-10-01T10:00:00Z',
    createdBy: 'user-1',
    lines: [
      {
        id: 'line-1',
        purchaseInvoiceId: 'inv-123',
        lineType: 'InventoryItem',
        inventoryItemId: 'item-1',
        inventoryItemName: 'NPK Fertilizer',
        inventoryItemSku: 'FERT-001',
        quantity: 100,
        unitPrice: 10,
        lineAmount: 1000,
        sortOrder: 1,
      },
    ],
  };

  const mockInvoiceService = {
    getRemainingToReceive: jasmine.createSpy('getRemainingToReceive').and.returnValue(
      of([
        {
          purchaseInvoiceLineId: 'line-1',
          inventoryItemId: 'item-1',
          inventoryItemName: 'NPK Fertilizer',
          inventoryItemSku: 'FERT-001',
          stockUnitId: 'unit-1',
          stockUnitCode: 'KG',
          stockUnitName: 'Kilogram',
          invoicedQuantity: 100,
          receivedQuantity: 40,
          remainingQuantity: 60,
          unitPrice: 10,
          isEligibleForReceipt: true,
        },
      ])
    ),
    receiveItems: jasmine.createSpy('receiveItems').and.returnValue(
      of({
        receiptGroupId: 'group-1',
        purchaseInvoiceId: 'inv-123',
        movementDate: '2026-10-03',
        storageLocationId: 'loc-1',
        storageLocationName: 'Main Store',
        createdAt: '2026-10-03T10:00:00Z',
        createdBy: 'user-1',
        items: [],
      })
    ),
  };

  const mockInventoryService = {
    listLocations: jasmine.createSpy('listLocations').and.returnValue(
      of({
        items: [
          {
            id: 'loc-1',
            organizationId: 'org-1',
            farmId: 'farm-1',
            name: 'Main Store',
            code: 'LOC-01',
            isActive: true,
            createdAt: '2026-10-01T00:00:00Z',
          },
        ],
        page: 1,
        pageSize: 100,
        totalCount: 1,
      })
    ),
  };

  const mockDialogRef = {
    close: jasmine.createSpy('close'),
  };

  const mockSnackBar = {
    open: jasmine.createSpy('open'),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PurchaseInvoiceReceiveDialogComponent, NoopAnimationsModule],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: { invoice: mockInvoice } },
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: PurchaseInvoiceService, useValue: mockInvoiceService },
        { provide: InventoryService, useValue: mockInventoryService },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PurchaseInvoiceReceiveDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load remaining items and locations', () => {
    expect(component).toBeTruthy();
    expect(mockInventoryService.listLocations).toHaveBeenCalledWith(1, 100, 'farm-1', true);
    expect(mockInvoiceService.getRemainingToReceive).toHaveBeenCalledWith('inv-123');
    expect(component.remainingLines().length).toBe(1);
    expect(component.storageLocations().length).toBe(1);
  });

  it('should submit receipt when form is valid', () => {
    component.form.patchValue({
      storageLocationId: 'loc-1',
      movementDate: new Date('2026-10-03'),
      referenceNumber: 'GRN-001',
    });

    component.onSubmit();

    expect(mockInvoiceService.receiveItems).toHaveBeenCalled();
    expect(mockDialogRef.close).toHaveBeenCalled();
  });
});
