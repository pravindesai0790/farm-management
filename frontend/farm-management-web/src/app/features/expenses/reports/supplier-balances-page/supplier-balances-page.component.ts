import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatExpansionModule } from '@angular/material/expansion';
import { ExpensesSubNavComponent } from '../../components/expenses-sub-nav/expenses-sub-nav.component';
import { FarmManagementService } from '../../../../core/farm-management/farm-management.service';
import { Farm } from '../../../../core/farm-management/farm-management.models';
import { SupplierBalanceService } from '../../../../core/expenses/supplier-balance.service';
import {
  SupplierBalanceCurrencySummary,
  SupplierBalanceFilter,
  SupplierBalanceSummaryResponse,
} from '../../../../core/expenses/supplier-balance.models';

@Component({
  selector: 'app-supplier-balances-page',
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
    MatChipsModule,
    MatTooltipModule,
    MatExpansionModule,
    ExpensesSubNavComponent,
  ],
  templateUrl: './supplier-balances-page.component.html',
  styleUrl: './supplier-balances-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SupplierBalancesPageComponent implements OnInit {
  private readonly balanceService = inject(SupplierBalanceService);
  private readonly farmService = inject(FarmManagementService);
  private readonly router = inject(Router);

  readonly loading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  readonly farms = signal<Farm[]>([]);
  readonly responseData = signal<SupplierBalanceSummaryResponse | null>(null);

  readonly selectedCurrencyId = signal<string | null>(null);
  readonly selectedFarmId = signal<string | null>(null);
  readonly asOfDate = signal<string>(new Date().toISOString().split('T')[0]);

  ngOnInit(): void {
    this.loadFarms();
    this.loadBalances();
  }

  private loadFarms(): void {
    this.farmService.listFarms(1, 100, '', null).subscribe({
      next: (res) => this.farms.set([...res.items]),
      error: () => {},
    });
  }

  loadBalances(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    const filter: SupplierBalanceFilter = {
      asOfDate: this.asOfDate(),
      farmId: this.selectedFarmId(),
      currencyId: this.selectedCurrencyId(),
    };

    this.balanceService.getSupplierBalances(filter).subscribe({
      next: (data) => {
        this.responseData.set(data);
        if (data.currencySummaries.length > 0 && !this.selectedCurrencyId()) {
          this.selectedCurrencyId.set(data.currencySummaries[0].currencyId);
        }
        this.loading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err?.error?.message || 'Failed to load supplier balances.');
        this.loading.set(false);
      },
    });
  }

  selectCurrency(currencyId: string): void {
    this.selectedCurrencyId.set(currencyId);
  }

  onFilterChange(): void {
    this.loadBalances();
  }

  get activeCurrencySummary(): SupplierBalanceCurrencySummary | null {
    const data = this.responseData();
    if (!data || !data.currencySummaries.length) return null;
    const selected = this.selectedCurrencyId();
    return data.currencySummaries.find((c) => c.currencyId === selected) || data.currencySummaries[0];
  }

  recordPaymentForInvoice(supplierId: string, invoiceId: string): void {
    this.router.navigate(['/expenses/payments/new'], {
      queryParams: { supplierId, invoiceId },
    });
  }

  getOverdueBadgeClass(daysOverdue: number): string {
    if (daysOverdue > 90) return 'badge-critical';
    if (daysOverdue > 30) return 'badge-warning';
    if (daysOverdue > 0) return 'badge-info';
    return 'badge-success';
  }
}
