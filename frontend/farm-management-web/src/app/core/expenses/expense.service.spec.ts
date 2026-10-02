import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import {
  CreateExpenseRequest,
  Expense,
  ExpenseFilter,
  ExpenseList,
  ReverseExpenseRequest,
  UpdateExpenseRequest,
} from './expense.models';
import { ExpenseService } from './expense.service';

describe('ExpenseService', () => {
  let service: ExpenseService;
  let httpTesting: HttpTestingController;

  const sampleExpense: Expense = {
    id: 'exp-1',
    organizationId: 'org-1',
    farmId: 'farm-1',
    farmName: 'Green Valley Farm',
    expenseCategoryId: 'cat-1',
    expenseCategoryName: 'Fertilizers',
    expenseDate: '2026-10-01',
    description: 'Purchased NPK fertilizer bags',
    amount: 350.0,
    currencyId: 'curr-1',
    currencyCode: 'USD',
    currencySymbol: '$',
    supplierId: 'sup-1',
    supplierName: 'Agri Corp',
    referenceNumber: 'REF-001',
    farmAreaId: 'area-1',
    farmAreaName: 'North Field',
    plantationId: 'plant-1',
    plantationName: 'Corn 2026',
    cropCycleId: 'cycle-1',
    cropCycleName: 'Cycle 1',
    attachmentReference: 'https://docs.farm.org/receipt.pdf',
    status: 'Draft',
    postedAt: null,
    postedBy: null,
    reversedAt: null,
    reversedBy: null,
    reversalReason: null,
    createdAt: '2026-10-01T10:00:00Z',
    createdBy: 'user-1',
    updatedAt: null,
    updatedBy: null,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ExpenseService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(ExpenseService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('lists expenses with filter parameters', () => {
    const mockResponse: ExpenseList = {
      items: [sampleExpense],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    };

    const filter: ExpenseFilter = {
      farmId: 'farm-1',
      categoryId: 'cat-1',
      status: 'Draft',
      search: 'fertilizer',
    };

    service.list(filter, 1, 20).subscribe((res) => {
      expect(res.items.length).toBe(1);
      expect(res.items[0].description).toBe('Purchased NPK fertilizer bags');
    });

    const req = httpTesting.expectOne((r) =>
      r.url === `${environment.apiUrl}/expenses` &&
      r.params.get('farmId') === 'farm-1' &&
      r.params.get('categoryId') === 'cat-1' &&
      r.params.get('status') === 'Draft' &&
      r.params.get('search') === 'fertilizer',
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('gets an expense by id', () => {
    service.get('exp-1').subscribe((res) => {
      expect(res.id).toBe('exp-1');
      expect(res.amount).toBe(350.0);
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/expenses/exp-1`);
    expect(req.request.method).toBe('GET');
    req.flush(sampleExpense);
  });

  it('creates an expense draft', () => {
    const request: CreateExpenseRequest = {
      farmId: 'farm-1',
      expenseCategoryId: 'cat-1',
      expenseDate: '2026-10-01',
      description: 'Purchased seeds',
      amount: 120.0,
      currencyId: 'curr-1',
    };

    service.createDraft(request).subscribe((res) => {
      expect(res.amount).toBe(120.0);
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/expenses`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({ ...sampleExpense, amount: 120.0, description: 'Purchased seeds' });
  });

  it('updates an expense draft', () => {
    const request: UpdateExpenseRequest = {
      farmId: 'farm-1',
      expenseCategoryId: 'cat-1',
      expenseDate: '2026-10-01',
      description: 'Updated description',
      amount: 200.0,
      currencyId: 'curr-1',
    };

    service.updateDraft('exp-1', request).subscribe((res) => {
      expect(res.description).toBe('Updated description');
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/expenses/exp-1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush({ ...sampleExpense, description: 'Updated description', amount: 200.0 });
  });

  it('posts an expense draft', () => {
    service.post('exp-1').subscribe((res) => {
      expect(res.status).toBe('Posted');
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/expenses/exp-1/post`);
    expect(req.request.method).toBe('POST');
    req.flush({ ...sampleExpense, status: 'Posted' });
  });

  it('reverses a posted expense', () => {
    const request: ReverseExpenseRequest = { reason: 'Incorrect vendor invoice' };

    service.reverse('exp-1', request).subscribe((res) => {
      expect(res.status).toBe('Reversed');
      expect(res.reversalReason).toBe('Incorrect vendor invoice');
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/expenses/exp-1/reverse`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({ ...sampleExpense, status: 'Reversed', reversalReason: 'Incorrect vendor invoice' });
  });

  it('lists active currencies from master data', () => {
    const currencies = [
      { id: 'curr-1', code: 'USD', name: 'US Dollar', symbol: '$', isSystem: true, isActive: true, displayOrder: 1 },
    ];

    service.listCurrencies().subscribe((res) => {
      expect(res.length).toBe(1);
      expect(res[0].code).toBe('USD');
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/master-data/currencies`);
    expect(req.request.method).toBe('GET');
    req.flush(currencies);
  });
});
