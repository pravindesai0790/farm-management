import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { PermissionService } from '../../../../core/auth/permission.service';
import { ExpenseService } from '../../../../core/expenses/expense.service';
import { PurchaseInvoiceResponse } from '../../../../core/expenses/purchase-invoice.models';
import { PurchaseInvoiceService } from '../../../../core/expenses/purchase-invoice.service';
import { SupplierService } from '../../../../core/expenses/supplier.service';
import { FarmManagementService } from '../../../../core/farm-management/farm-management.service';
import { PurchaseInvoiceListPageComponent } from './purchase-invoice-list-page.component';

describe('PurchaseInvoiceListPageComponent', () => {
  let component: PurchaseInvoiceListPageComponent;
  let fixture: ComponentFixture<PurchaseInvoiceListPageComponent>;
  let mockInvoiceService: jasmine.SpyObj<PurchaseInvoiceService>;
  let mockFarmService: jasmine.SpyObj<FarmManagementService>;
  let mockSupplierService: jasmine.SpyObj<SupplierService>;
  let mockExpenseService: jasmine.SpyObj<ExpenseService>;
  let mockPermissionService: jasmine.SpyObj<PermissionService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;
  let mockDialog: jasmine.SpyObj<MatDialog>;

  const mockInvoicesResponse = {
    items: [
      {
        id: 'inv-1',
        supplierInvoiceNumber: 'INV-101',
        supplierName: 'Supplier A',
        farmName: 'Farm A',
        totalAmount: 1000,
        currencySymbol: '₹',
        status: 'Draft',
        paymentStatus: 'Unpaid',
        dueStatus: 'Current',
        lines: [],
      } as unknown as PurchaseInvoiceResponse,
    ],
    totalCount: 1,
    page: 1,
    pageSize: 20,
    totalPages: 1,
  };

  beforeEach(async () => {
    mockInvoiceService = jasmine.createSpyObj('PurchaseInvoiceService', ['list', 'post']);
    mockFarmService = jasmine.createSpyObj('FarmManagementService', ['listFarms']);
    mockSupplierService = jasmine.createSpyObj('SupplierService', ['list']);
    mockExpenseService = jasmine.createSpyObj('ExpenseService', ['listCurrencies']);
    mockPermissionService = jasmine.createSpyObj('PermissionService', ['has']);
    mockSnackBar = jasmine.createSpyObj('MatSnackBar', ['open']);
    mockDialog = jasmine.createSpyObj('MatDialog', ['open']);

    mockInvoiceService.list.and.returnValue(of(mockInvoicesResponse));
    mockFarmService.listFarms.and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 100, totalPages: 0 }));
    mockSupplierService.list.and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 100, totalPages: 0 }));
    mockPermissionService.has.and.returnValue(true);

    await TestBed.configureTestingModule({
      imports: [PurchaseInvoiceListPageComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: PurchaseInvoiceService, useValue: mockInvoiceService },
        { provide: FarmManagementService, useValue: mockFarmService },
        { provide: SupplierService, useValue: mockSupplierService },
        { provide: ExpenseService, useValue: mockExpenseService },
        { provide: PermissionService, useValue: mockPermissionService },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: MatDialog, useValue: mockDialog },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: new Map() } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PurchaseInvoiceListPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load invoices on init', () => {
    expect(mockInvoiceService.list).toHaveBeenCalled();
    expect(component.invoices().length).toBe(1);
    expect(component.totalCount()).toBe(1);
  });

  it('should reset filter form', () => {
    component.filterForm.controls.search.setValue('INV-99');
    component.resetFilters();
    expect(component.filterForm.controls.search.value).toBe('');
  });
});
