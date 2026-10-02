import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { Expense } from '../../../../core/expenses/expense.models';
import { ExpenseService } from '../../../../core/expenses/expense.service';
import { DirectExpenseReversalDialogComponent } from './direct-expense-reversal-dialog.component';

describe('DirectExpenseReversalDialogComponent', () => {
  let component: DirectExpenseReversalDialogComponent;
  let fixture: ComponentFixture<DirectExpenseReversalDialogComponent>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<DirectExpenseReversalDialogComponent>>;
  let mockExpenseService: jasmine.SpyObj<ExpenseService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

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
    mockExpenseService = jasmine.createSpyObj('ExpenseService', ['reverse']);
    mockSnackBar = jasmine.createSpyObj('MatSnackBar', ['open']);

    await TestBed.configureTestingModule({
      imports: [DirectExpenseReversalDialogComponent, NoopAnimationsModule],
      providers: [
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: { expense: sampleExpense } },
        { provide: ExpenseService, useValue: mockExpenseService },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(DirectExpenseReversalDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('creates the component and shows expense summary', () => {
    expect(component).toBeTruthy();
    expect(component.data.expense.id).toBe('exp-1');
    expect(component.form.valid).toBeFalse();
  });

  it('validates mandatory reversal reason', () => {
    component.form.controls.reason.setValue('');
    expect(component.form.valid).toBeFalse();
    expect(component.form.controls.reason.hasError('required')).toBeTrue();

    component.form.controls.reason.setValue('Valid explanation for reversal');
    expect(component.form.valid).toBeTrue();
  });

  it('calls reverse service on submit and closes dialog', () => {
    const reversedExpense: Expense = {
      ...sampleExpense,
      status: 'Reversed',
      reversalReason: 'Duplicate invoice entered',
    };
    mockExpenseService.reverse.and.returnValue(of(reversedExpense));

    component.form.controls.reason.setValue('Duplicate invoice entered');
    component.onSubmit();

    expect(mockExpenseService.reverse).toHaveBeenCalledWith('exp-1', {
      reason: 'Duplicate invoice entered',
    });
    expect(mockSnackBar.open).toHaveBeenCalledWith(
      'Expense successfully reversed.',
      'Close',
      jasmine.any(Object),
    );
    expect(mockDialogRef.close).toHaveBeenCalledWith(reversedExpense);
  });
});
