import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { DatePipe, DecimalPipe } from "@angular/common";
import { RouterLink } from "@angular/router";
import { FormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatSelectModule } from "@angular/material/select";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatTooltipModule } from "@angular/material/tooltip";

import { AuthService } from "../../core/auth/auth.service";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import {
  DashboardSummaryResponse,
  Farm,
} from "../../core/farm-management/farm-management.models";

@Component({
  selector: "app-dashboard-page",
  standalone: true,
  imports: [
    DatePipe,
    DecimalPipe,
    RouterLink,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatFormFieldModule,
    MatTooltipModule,
  ],
  templateUrl: "./dashboard-page.component.html",
  styleUrl: "./dashboard-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPageComponent implements OnInit {
  readonly summary = signal<DashboardSummaryResponse | null>(null);
  readonly farms = signal<readonly Farm[]>([]);
  readonly selectedFarmId = signal<string>("");
  readonly isLoading = signal<boolean>(true);
  readonly error = signal<string | null>(null);

  private readonly destroyRef = inject(DestroyRef);
  private readonly snackBar = inject(MatSnackBar);
  private readonly farmService = inject(FarmManagementService);
  private readonly authService = inject(AuthService);

  readonly currentUser = this.authService.user;

  private readonly cropColors = [
    "#2f6f4e",
    "#d97706",
    "#2563eb",
    "#7c3aed",
    "#dc2626",
    "#0d9488",
    "#ea580c",
    "#4f46e5",
  ];

  ngOnInit(): void {
    this.loadFarms();
    this.loadSummary();
  }

  loadFarms(): void {
    this.farmService
      .listFarms(1, 100, "", true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.farms.set(response.items);
        },
        error: () => {
          // Non-blocking: summary endpoint still functions for organization scope
        },
      });
  }

  loadSummary(farmId?: string): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.farmService
      .getDashboardSummary(farmId || undefined)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.summary.set(response);
          this.isLoading.set(false);
        },
        error: () => {
          this.summary.set(null);
          this.isLoading.set(false);
          this.error.set("Unable to load farm dashboard metrics. Please check connection.");
          this.snackBar.open("Failed to load dashboard data.", "Dismiss", {
            duration: 5000,
          });
        },
      });
  }

  onFarmChange(farmId: string): void {
    this.selectedFarmId.set(farmId);
    this.loadSummary(farmId);
  }

  refresh(): void {
    this.loadSummary(this.selectedFarmId());
  }

  getCropColor(index: number): string {
    return this.cropColors[index % this.cropColors.length];
  }
}
