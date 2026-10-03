import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ExpensesSubNavComponent } from '../../components/expenses-sub-nav/expenses-sub-nav.component';
import { FarmManagementService } from '../../../../core/farm-management/farm-management.service';
import { Farm } from '../../../../core/farm-management/farm-management.models';
import { ExpenseCategoryService } from '../../../../core/expenses/expense-category.service';
import { ExpenseCategory } from '../../../../core/expenses/expense-category.models';
import { ExpenseReportService } from '../../../../core/expenses/expense-report.service';
import {
  ExpenseReportCurrencySummary,
  ExpenseReportFilter,
  ExpenseReportSummaryResponse,
} from '../../../../core/expenses/expense-report.models';

@Component({
  selector: 'app-expense-reports-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    MatProgressBarModule,
    MatTooltipModule,
    ExpensesSubNavComponent,
  ],
  templateUrl: './expense-reports-page.component.html',
  styleUrl: './expense-reports-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExpenseReportsPageComponent implements OnInit {
  private readonly reportService = inject(ExpenseReportService);
  private readonly farmService = inject(FarmManagementService);
  private readonly categoryService = inject(ExpenseCategoryService);

  readonly loading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  readonly farms = signal<Farm[]>([]);
  readonly categories = signal<ExpenseCategory[]>([]);
  readonly reportData = signal<ExpenseReportSummaryResponse | null>(null);

  readonly selectedCurrencyId = signal<string | null>(null);
  readonly selectedFarmId = signal<string | null>(null);
  readonly selectedCategoryId = signal<string | null>(null);
  readonly fromDate = signal<string | null>(null);
  readonly toDate = signal<string | null>(null);

  readonly activePreset = signal<string>('this-month');

  ngOnInit(): void {
    this.loadFarms();
    this.loadCategories();
    this.applyPeriodPreset('this-month');
  }

  private loadFarms(): void {
    this.farmService.listFarms(1, 100, '', null).subscribe({
      next: (res) => this.farms.set([...res.items]),
      error: () => {},
    });
  }

  private loadCategories(): void {
    this.categoryService.list().subscribe({
      next: (res) => this.categories.set([...res.items]),
      error: () => {},
    });
  }

  applyPeriodPreset(preset: string): void {
    this.activePreset.set(preset);
    const now = new Date();
    const year = now.getFullYear();
    const month = now.getMonth();

    if (preset === 'this-month') {
      const first = new Date(year, month, 1);
      const last = new Date(year, month + 1, 0);
      this.fromDate.set(first.toISOString().split('T')[0]);
      this.toDate.set(last.toISOString().split('T')[0]);
    } else if (preset === 'last-month') {
      const first = new Date(year, month - 1, 1);
      const last = new Date(year, month, 0);
      this.fromDate.set(first.toISOString().split('T')[0]);
      this.toDate.set(last.toISOString().split('T')[0]);
    } else if (preset === 'this-quarter') {
      const qMonth = Math.floor(month / 3) * 3;
      const first = new Date(year, qMonth, 1);
      const last = new Date(year, qMonth + 3, 0);
      this.fromDate.set(first.toISOString().split('T')[0]);
      this.toDate.set(last.toISOString().split('T')[0]);
    } else if (preset === 'this-year') {
      const first = new Date(year, 0, 1);
      const last = new Date(year, 11, 31);
      this.fromDate.set(first.toISOString().split('T')[0]);
      this.toDate.set(last.toISOString().split('T')[0]);
    } else if (preset === 'all') {
      this.fromDate.set(null);
      this.toDate.set(null);
    }

    this.loadReport();
  }

  loadReport(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    const filter: ExpenseReportFilter = {
      from: this.fromDate(),
      to: this.toDate(),
      farmId: this.selectedFarmId(),
      categoryId: this.selectedCategoryId(),
      currencyId: this.selectedCurrencyId(),
    };

    this.reportService.getSummaryReport(filter).subscribe({
      next: (data) => {
        this.reportData.set(data);
        if (data.currencySummaries.length > 0 && !this.selectedCurrencyId()) {
          this.selectedCurrencyId.set(data.currencySummaries[0].currencyId);
        }
        this.loading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err?.error?.message || 'Failed to load expense report.');
        this.loading.set(false);
      },
    });
  }

  selectCurrency(currencyId: string): void {
    this.selectedCurrencyId.set(currencyId);
  }

  onFilterChange(): void {
    this.activePreset.set('custom');
    this.loadReport();
  }

  get activeCurrencySummary(): ExpenseReportCurrencySummary | null {
    const data = this.reportData();
    if (!data || !data.currencySummaries.length) return null;
    const selected = this.selectedCurrencyId();
    return data.currencySummaries.find((c) => c.currencyId === selected) || data.currencySummaries[0];
  }
}
