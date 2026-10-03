import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ExpenseCategoryService } from '../../../../core/expenses/expense-category.service';
import { ExpenseService } from '../../../../core/expenses/expense.service';
import { PurchaseInvoiceResponse } from '../../../../core/expenses/purchase-invoice.models';
import { PurchaseInvoiceService } from '../../../../core/expenses/purchase-invoice.service';
import { SupplierService } from '../../../../core/expenses/supplier.service';
import { FarmManagementService } from '../../../../core/farm-management/farm-management.service';
import { InventoryService } from '../../../../core/inventory/inventory.service';
import { PurchaseInvoiceEditorPageComponent } from './purchase-invoice-editor-page.component';

describe('PurchaseInvoiceEditorPageComponent', () => {
  let component: PurchaseInvoiceEditorPageComponent;
  let fixture: ComponentFixture<PurchaseInvoiceEditorPageComponent>;
  let mockInvoiceService: jasmine.SpyObj<PurchaseInvoiceService>;
  let mockFarmService: jasmine.SpyObj<FarmManagementService>;
  let mockSupplierService: jasmine.SpyObj<SupplierService>;
  let mockCategoryService: jasmine.SpyObj<ExpenseCategoryService>;
  let mockInventoryService: jasmine.SpyObj<InventoryService>;
  let mockExpenseService: jasmine.SpyObj<ExpenseService>;
  let router: Router;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  beforeEach(async () => {
    mockInvoiceService = jasmine.createSpyObj('PurchaseInvoiceService', [
      'get',
      'createDraft',
      'updateDraft',
    ]);
    mockFarmService = jasmine.createSpyObj('FarmManagementService', [
      'listFarms',
      'listAreas',
      'listPlantations',
      'listCycles',
      'getCycleLifecycle',
    ]);
    mockSupplierService = jasmine.createSpyObj('SupplierService', ['list']);
    mockCategoryService = jasmine.createSpyObj('ExpenseCategoryService', ['list']);
    mockInventoryService = jasmine.createSpyObj('InventoryService', ['listItems']);
    mockExpenseService = jasmine.createSpyObj('ExpenseService', ['listCurrencies']);
    mockSnackBar = jasmine.createSpyObj('MatSnackBar', ['open']);

    mockFarmService.listFarms.and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 100, totalPages: 0 }));
    mockFarmService.listAreas.and.returnValue(of([]));
    mockFarmService.listPlantations.and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 100, totalPages: 0 }));
    mockFarmService.listCycles.and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 100, totalPages: 0 }));
    mockFarmService.getCycleLifecycle.and.returnValue(of({ stages: [] } as any));
    mockSupplierService.list.and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 100, totalPages: 0 }));
    mockCategoryService.list.and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 100, totalPages: 0 }));
    mockInventoryService.listItems.and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 200, totalPages: 0 }));
    mockExpenseService.listCurrencies.and.returnValue(of([{ id: 'cur-1', code: 'INR', symbol: '₹', name: 'Indian Rupee', isSystem: true, isActive: true, displayOrder: 1 }]));

    await TestBed.configureTestingModule({
      imports: [PurchaseInvoiceEditorPageComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: PurchaseInvoiceService, useValue: mockInvoiceService },
        { provide: FarmManagementService, useValue: mockFarmService },
        { provide: SupplierService, useValue: mockSupplierService },
        { provide: ExpenseCategoryService, useValue: mockCategoryService },
        { provide: InventoryService, useValue: mockInventoryService },
        { provide: ExpenseService, useValue: mockExpenseService },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: new Map() } } },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    spyOn(router, 'navigate');

    fixture = TestBed.createComponent(PurchaseInvoiceEditorPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and initialize default values', () => {
    expect(component).toBeTruthy();
    expect(component.linesArray.length).toBe(1);
    expect(component.form.controls.currencyId.value).toBe('cur-1');
  });

  it('should add and remove line items', () => {
    component.addLine();
    expect(component.linesArray.length).toBe(2);

    component.removeLine(1);
    expect(component.linesArray.length).toBe(1);
  });

  it('should auto calculate totals', () => {
    const lineGroup = component.lineFormGroups[0];
    lineGroup.patchValue({
      lineType: 'InventoryItem',
      quantity: 10,
      unitPrice: 50,
    });
    component.form.patchValue({ taxAmount: 20, discountAmount: 10 });

    expect(component.subtotal).toBe(500);
    expect(component.totalAmount).toBe(510); // 500 + 20 - 10
  });

  it('should submit new draft invoice when form is valid', () => {
    mockInvoiceService.createDraft.and.returnValue(of({ id: 'inv-new' } as PurchaseInvoiceResponse));

    component.form.patchValue({
      supplierId: 'sup-1',
      farmId: 'farm-1',
      supplierInvoiceNumber: 'INV-001',
      invoiceDate: new Date(),
      currencyId: 'cur-1',
    });

    const lineGroup = component.lineFormGroups[0];
    lineGroup.patchValue({
      lineType: 'InventoryItem',
      inventoryItemId: 'item-1',
      quantity: 5,
      unitPrice: 100,
    });

    component.onSubmit();

    expect(mockInvoiceService.createDraft).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/expenses/invoices', 'inv-new']);
  });
});
