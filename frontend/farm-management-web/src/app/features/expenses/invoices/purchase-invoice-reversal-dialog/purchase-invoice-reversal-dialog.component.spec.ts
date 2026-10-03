import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { PurchaseInvoiceResponse } from '../../../../core/expenses/purchase-invoice.models';
import { PurchaseInvoiceService } from '../../../../core/expenses/purchase-invoice.service';
import { PurchaseInvoiceReversalDialogComponent } from './purchase-invoice-reversal-dialog.component';

describe('PurchaseInvoiceReversalDialogComponent', () => {
  let component: PurchaseInvoiceReversalDialogComponent;
  let fixture: ComponentFixture<PurchaseInvoiceReversalDialogComponent>;
  let mockInvoiceService: jasmine.SpyObj<PurchaseInvoiceService>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<PurchaseInvoiceReversalDialogComponent>>;

  const mockInvoice: Partial<PurchaseInvoiceResponse> = {
    id: 'inv-1',
    supplierInvoiceNumber: 'INV-1001',
    supplierName: 'AgriCorp',
    totalAmount: 1500,
    currencySymbol: '₹',
  };

  beforeEach(async () => {
    mockInvoiceService = jasmine.createSpyObj('PurchaseInvoiceService', ['reverse']);
    mockDialogRef = jasmine.createSpyObj('MatDialogRef', ['close']);

    await TestBed.configureTestingModule({
      imports: [PurchaseInvoiceReversalDialogComponent, NoopAnimationsModule],
      providers: [
        { provide: PurchaseInvoiceService, useValue: mockInvoiceService },
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: { invoice: mockInvoice } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PurchaseInvoiceReversalDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should require a reversal reason', () => {
    component.form.controls.reason.setValue('');
    expect(component.form.invalid).toBeTrue();
  });

  it('should call reverse on service when submitted with valid reason', () => {
    mockInvoiceService.reverse.and.returnValue(of(mockInvoice as PurchaseInvoiceResponse));
    component.form.controls.reason.setValue('Wrong invoice amount');

    component.onSubmit();

    expect(mockInvoiceService.reverse).toHaveBeenCalledWith('inv-1', { reason: 'Wrong invoice amount' });
    expect(mockDialogRef.close).toHaveBeenCalled();
  });
});
