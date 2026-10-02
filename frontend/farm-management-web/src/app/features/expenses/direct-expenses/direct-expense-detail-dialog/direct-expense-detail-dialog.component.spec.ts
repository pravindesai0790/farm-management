import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Expense } from '../../../../core/expenses/expense.models';
import { DirectExpenseDetailDialogComponent } from './direct-expense-detail-dialog.component';

describe('DirectExpenseDetailDialogComponent', () => {
  let component: DirectExpenseDetailDialogComponent;
  let fixture: ComponentFixture<DirectExpenseDetailDialogComponent>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<DirectExpenseDetailDialogComponent>>;

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
    status: 'Posted',
    createdAt: '2026-10-01T00:00:00Z',
    createdBy: 'u-1',
  };

  beforeEach(async () => {
    mockDialogRef = jasmine.createSpyObj('MatDialogRef', ['close']);

    await TestBed.configureTestingModule({
      imports: [DirectExpenseDetailDialogComponent, NoopAnimationsModule],
      providers: [
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: { expense: sampleExpense } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(DirectExpenseDetailDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('creates the component and displays expense data', () => {
    expect(component).toBeTruthy();
    expect(component.data.expense.id).toBe('exp-1');
  });

  it('correctly maps status classes', () => {
    expect(component.getStatusClass('Draft')).toBe('status-draft');
    expect(component.getStatusClass('Posted')).toBe('status-posted');
    expect(component.getStatusClass('Reversed')).toBe('status-reversed');
    expect(component.getStatusClass('Unknown')).toBe('');
  });
});
