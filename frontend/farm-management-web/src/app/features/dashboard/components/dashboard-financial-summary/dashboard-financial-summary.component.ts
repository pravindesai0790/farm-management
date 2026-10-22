import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  input,
  OnInit,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { DecimalPipe } from "@angular/common";
import { RouterLink } from "@angular/router";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatButtonModule } from "@angular/material/button";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatTooltipModule } from "@angular/material/tooltip";
import { PermissionService } from "../../../../core/auth/permission.service";
import { FarmManagementService } from "../../../../core/farm-management/farm-management.service";
import {
  CurrencyFinancialSummary,
  DashboardFinancialSummaryResponse,
} from "../../../../core/farm-management/farm-management.models";

@Component({
  selector: "app-dashboard-financial-summary",
  standalone: true,
  imports: [
    DecimalPipe,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: "./dashboard-financial-summary.component.html",
  styleUrl: "./dashboard-financial-summary.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardFinancialSummaryComponent implements OnInit {
  readonly farmId = input<string>("");

  readonly financialData = signal<DashboardFinancialSummaryResponse | null>(null);
  readonly isLoading = signal<boolean>(true);
  readonly selectedCurrencyIndex = signal<number>(0);

  private readonly permissionService = inject(PermissionService);
  private readonly farmService = inject(FarmManagementService);
  private readonly destroyRef = inject(DestroyRef);

  readonly hasAccess = computed(() => {
    return (
      this.permissionService.has("Expense.View") ||
      this.permissionService.has("PurchaseInvoice.View")
    );
  });

  readonly selectedCurrency = computed<CurrencyFinancialSummary | null>(() => {
    const list = this.financialData()?.currencySummaries ?? [];
    if (list.length === 0) return null;
    const idx = this.selectedCurrencyIndex();
    return list[idx] ?? list[0];
  });

  constructor() {
    effect(() => {
      const currentFarmId = this.farmId();
      if (this.hasAccess()) {
        this.loadFinancials(currentFarmId);
      } else {
        this.isLoading.set(false);
      }
    });
  }

  ngOnInit(): void {
    // Initial fetch handled by effect
  }

  selectCurrency(index: number): void {
    this.selectedCurrencyIndex.set(index);
  }

  loadFinancials(farmId?: string): void {
    this.isLoading.set(true);
    this.farmService
      .getDashboardFinancialSummary(farmId || undefined)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.financialData.set(res);
          this.isLoading.set(false);
        },
        error: () => {
          this.isLoading.set(false);
        },
      });
  }
}
