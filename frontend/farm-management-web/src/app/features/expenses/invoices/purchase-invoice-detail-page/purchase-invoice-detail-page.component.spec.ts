import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { PermissionService } from '../../../../core/auth/permission.service';
import { PurchaseInvoiceResponse } from '../../../../core/expenses/purchase-invoice.models';
import { PurchaseInvoiceService } from '../../../../core/expenses/purchase-invoice.service';
import { PurchaseInvoiceDetailPageComponent } from './purchase-invoice-detail-page.component';

describe('PurchaseInvoiceDetailPageComponent', () => {
  let component: PurchaseInvoiceDetailPageComponent;
  let fixture: ComponentFixture<PurchaseInvoiceDetailPageComponent>;
  let mockInvoiceService: jasmine.SpyObj<PurchaseInvoiceService>;
  let mockPermissionService: jasmine.SpyObj<PermissionService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;
  let mockDialog: jasmine.SpyObj<MatDialog>;

  const mockInvoice: Partial<PurchaseInvoiceResponse> = {
    id: 'inv-1',
    supplierInvoiceNumber: 'INV-1001',
    supplierName: 'AgriCorp',
    farmName: 'Green Farm',
    invoiceDate: '2026-10-01',
    currencySymbol: '₹',
    currencyCode: 'INR',
    subtotal: 1000,
    taxAmount: 50,
    otherCharges: 0,
    discountAmount: 0,
    totalAmount: 1050,
    amountPaid: 0,
    outstandingBalance: 1050,
    status: 'Draft',
    paymentStatus: 'Unpaid',
    dueStatus: 'Current',
    receiptStatus: 'Unlinked',
    lines: [],
  };

  beforeEach(async () => {
    mockInvoiceService = jasmine.createSpyObj('PurchaseInvoiceService', ['get', 'post']);
    mockPermissionService = jasmine.createSpyObj('PermissionService', ['has']);
    mockSnackBar = jasmine.createSpyObj('MatSnackBar', ['open']);
    mockDialog = jasmine.createSpyObj('MatDialog', ['open']);

    mockInvoiceService.get.and.returnValue(of(mockInvoice as PurchaseInvoiceResponse));
    mockPermissionService.has.and.returnValue(true);

    await TestBed.configureTestingModule({
      imports: [PurchaseInvoiceDetailPageComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: PurchaseInvoiceService, useValue: mockInvoiceService },
        { provide: PermissionService, useValue: mockPermissionService },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: MatDialog, useValue: mockDialog },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: new Map([['id', 'inv-1']]) } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PurchaseInvoiceDetailPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load invoice details', () => {
    expect(component).toBeTruthy();
    expect(mockInvoiceService.get).toHaveBeenCalledWith('inv-1');
    expect(component.invoice()?.supplierInvoiceNumber).toBe('INV-1001');
  });

  it('should post invoice when post button clicked and confirmed', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    mockInvoiceService.post.and.returnValue(of({ ...mockInvoice, status: 'Posted' } as PurchaseInvoiceResponse));

    component.postInvoice();

    expect(mockInvoiceService.post).toHaveBeenCalledWith('inv-1');
    expect(component.invoice()?.status).toBe('Posted');
  });
});
