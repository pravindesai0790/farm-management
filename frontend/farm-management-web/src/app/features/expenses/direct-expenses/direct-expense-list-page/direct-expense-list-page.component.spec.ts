import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { PermissionService } from '../../../../core/auth/permission.service';
import { ExpenseCategoryService } from '../../../../core/expenses/expense-category.service';
import { Expense, ExpenseList } from '../../../../core/expenses/expense.models';
import { ExpenseService } from '../../../../core/expenses/expense.service';
import { SupplierService } from '../../../../core/expenses/supplier.service';
import { FarmManagementService } from '../../../../core/farm-management/farm-management.service';
import { DirectExpenseListPageComponent } from './direct-expense-list-page.component';

describe('DirectExpenseListPageComponent', () => {
  let component: DirectExpenseListPageComponent;
  let fixture: ComponentFixture<DirectExpenseListPageComponent>;
  let mockExpenseService: jasmine.SpyObj<ExpenseService>;
  let mockFarmService: jasmine.SpyObj<FarmManagementService>;
  let mockCategoryService: jasmine.SpyObj<ExpenseCategoryService>;
  let mockSupplierService: jasmine.SpyObj<SupplierService>;
  let mockPermissionService: jasmine.SpyObj<PermissionService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;
  let mockDialog: jasmine.SpyObj<MatDialog>;

  const sampleExpense: Expense = {
    id: 'exp-1',
    organizationId: 'org-1',
    farmId: 'farm-1',
    farmName: 'Farm One',
    expenseCategoryId: 'cat-1',
    expenseCategoryName: 'Fertilizers',
    expenseDate: '2026-10-01',
    description: 'Direct fertilizer purchase',
    amount: 150.0,
    currencyId: 'curr-1',
    currencyCode: 'USD',
    currencySymbol: '$',
    status: 'Draft',
    createdAt: '2026-10-01T00:00:00Z',
    createdBy: 'u-1',
  };

  const sampleResponse: ExpenseList = {
    items: [sampleExpense],
    page: 1,
    pageSize: 20,
    totalCount: 1,
  };

  beforeEach(async () => {
    mockExpenseService = jasmine.createSpyObj('ExpenseService', [
      'list',
      'post',
      'reverse',
    ]);
    mockFarmService = jasmine.createSpyObj('FarmManagementService', ['listFarms']);
    mockCategoryService = jasmine.createSpyObj('ExpenseCategoryService', ['list']);
    mockSupplierService = jasmine.createSpyObj('SupplierService', ['list']);
    mockPermissionService = jasmine.createSpyObj('PermissionService', ['has']);
    mockSnackBar = jasmine.createSpyObj('MatSnackBar', ['open']);
    mockDialog = jasmine.createSpyObj('MatDialog', ['open']);

    mockExpenseService.list.and.returnValue(of(sampleResponse));
    mockFarmService.listFarms.and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 20 }));
    mockCategoryService.list.and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 20 }));
    mockSupplierService.list.and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 20 }));
    mockPermissionService.has.and.returnValue(true);
    mockDialog.open.and.returnValue({ afterClosed: () => of(true) } as any);

    await TestBed.configureTestingModule({
      imports: [DirectExpenseListPageComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        provideNativeDateAdapter(),
        { provide: ExpenseService, useValue: mockExpenseService },
        { provide: FarmManagementService, useValue: mockFarmService },
        { provide: ExpenseCategoryService, useValue: mockCategoryService },
        { provide: SupplierService, useValue: mockSupplierService },
        { provide: PermissionService, useValue: mockPermissionService },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    })
      .overrideProvider(MatDialog, { useValue: mockDialog })
      .compileComponents();

    fixture = TestBed.createComponent(DirectExpenseListPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('creates the component and loads initial expense list', () => {
    expect(component).toBeTruthy();
    expect(mockExpenseService.list).toHaveBeenCalled();
    expect(component.expenses().length).toBe(1);
    expect(component.totalCount()).toBe(1);
  });

  it('opens create dialog when openCreateDialog is called', () => {
    component.openCreateDialog();
    expect(mockDialog.open).toHaveBeenCalled();
  });

  it('opens edit dialog with expense data', () => {
    component.openEditDialog(sampleExpense);
    expect(mockDialog.open).toHaveBeenCalledWith(
      jasmine.any(Function),
      jasmine.objectContaining({ data: { expense: sampleExpense } }),
    );
  });

  it('opens details dialog with expense data', () => {
    component.openDetailDialog(sampleExpense);
    expect(mockDialog.open).toHaveBeenCalledWith(
      jasmine.any(Function),
      jasmine.objectContaining({ data: { expense: sampleExpense } }),
    );
  });

  it('resets filters to defaults', () => {
    component.filterForm.patchValue({
      search: 'test search',
      status: 'Posted',
      from: new Date('2026-10-01'),
      to: new Date('2026-10-15'),
    });

    component.resetFilters();

    expect(component.filterForm.value.search).toBe('');
    expect(component.filterForm.value.status).toBe('all');
    expect(component.filterForm.value.from).toBeNull();
    expect(component.filterForm.value.to).toBeNull();
  });

  it('filters expenses by date range with formatted dates', () => {
    component.filterForm.patchValue({
      from: new Date('2026-10-01T00:00:00Z'),
      to: new Date('2026-10-15T00:00:00Z'),
    });

    component.loadExpenses();

    expect(mockExpenseService.list).toHaveBeenCalledWith(
      jasmine.objectContaining({
        from: '2026-10-01',
        to: '2026-10-15',
      }),
      1,
      20,
    );
  });

  it('posts an expense draft when confirmed', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    mockExpenseService.post.and.returnValue(of({ ...sampleExpense, status: 'Posted' }));

    component.postExpense(sampleExpense);

    expect(mockExpenseService.post).toHaveBeenCalledWith('exp-1');
    expect(mockSnackBar.open).toHaveBeenCalledWith(
      'Expense posted successfully.',
      'Close',
      jasmine.any(Object),
    );
  });

  it('cancels post when user declines confirmation', () => {
    spyOn(window, 'confirm').and.returnValue(false);

    component.postExpense(sampleExpense);

    expect(mockExpenseService.post).not.toHaveBeenCalled();
  });
});
