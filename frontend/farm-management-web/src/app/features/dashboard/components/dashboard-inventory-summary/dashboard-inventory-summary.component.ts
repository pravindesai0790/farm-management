import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  input,
  OnInit,
  signal,
  effect,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { DecimalPipe } from "@angular/common";
import { RouterLink } from "@angular/router";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatButtonModule } from "@angular/material/button";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatTooltipModule } from "@angular/material/tooltip";
import { FarmManagementService } from "../../../../core/farm-management/farm-management.service";
import { DashboardInventorySummaryResponse } from "../../../../core/farm-management/farm-management.models";

@Component({
  selector: "app-dashboard-inventory-summary",
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
  templateUrl: "./dashboard-inventory-summary.component.html",
  styleUrl: "./dashboard-inventory-summary.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardInventorySummaryComponent implements OnInit {
  readonly farmId = input<string>("");

  readonly inventoryData = signal<DashboardInventorySummaryResponse | null>(null);
  readonly isLoading = signal<boolean>(true);

  private readonly destroyRef = inject(DestroyRef);
  private readonly farmService = inject(FarmManagementService);

  constructor() {
    effect(() => {
      // Re-fetch whenever farmId changes
      const currentFarmId = this.farmId();
      this.loadInventory(currentFarmId);
    });
  }

  ngOnInit(): void {
    // Initial fetch handled by effect
  }

  loadInventory(farmId?: string): void {
    this.isLoading.set(true);
    this.farmService
      .getDashboardInventorySummary(farmId || undefined)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.inventoryData.set(res);
          this.isLoading.set(false);
        },
        error: () => {
          this.isLoading.set(false);
        },
      });
  }
}
