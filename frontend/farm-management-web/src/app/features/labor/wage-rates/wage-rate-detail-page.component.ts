import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTooltipModule } from "@angular/material/tooltip";
import { finalize } from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import { BreadcrumbService } from "../../../core/breadcrumb/breadcrumb.service";
import {
  LaborWageRate,
  formatGender,
  formatWageType,
  getRateLifecycle,
} from "../../../core/labor/labor.models";
import { LaborService } from "../../../core/labor/labor.service";
import { getApiErrorMessage } from "../../../core/models/api-error.model";

@Component({
  selector: "app-wage-rate-detail-page",
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    RouterLink,
  ],
  templateUrl: "./wage-rate-detail-page.component.html",
  styleUrl: "./wage-rate-detail-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WageRateDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly laborService = inject(LaborService);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly breadcrumbService = inject(BreadcrumbService);
  readonly permissionService = inject(PermissionService);

  readonly wageRate = signal<LaborWageRate | null>(null);
  readonly isLoading = signal(true);
  readonly actionInProgress = signal(false);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get("id");
    if (!id) {
      this.router.navigate(["/labor/wage-rates"]);
      return;
    }

    const cached = this.breadcrumbService.getEntityName(id);
    this.breadcrumbService.setTrail([
      { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
      { label: "Labor", route: "/labor" },
      { label: "Wage rates", route: "/labor/wage-rates" },
      { label: cached ?? "Wage rate details" },
    ]);

    this.load(id);
  }

  load(id: string): void {
    this.isLoading.set(true);

    this.laborService
      .getWageRate(id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (rate) => {
          this.wageRate.set(rate);
          const title = `${this.getGenderLabel(rate.gender)} · ${this.getWageTypeLabel(rate.wageType)}`;
          this.breadcrumbService.setEntityName(rate.id, title);
          this.breadcrumbService.setTrail([
            { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
            { label: "Labor", route: "/labor" },
            { label: "Wage rates", route: "/labor/wage-rates" },
            { label: title },
          ]);
        },
        error: (error) => {
          this.snack.open(
            getApiErrorMessage(error, "Wage rate could not be loaded."),
            "Dismiss",
            { duration: 5000 },
          );
          this.router.navigate(["/labor/wage-rates"]);
        },
      });
  }

  changeStatus(active: boolean): void {
    const rate = this.wageRate();
    if (!rate) return;

    this.actionInProgress.set(true);

    const request$ = active
      ? this.laborService.activateWageRate(rate.id)
      : this.laborService.deactivateWageRate(rate.id);

    request$
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.actionInProgress.set(false)),
      )
      .subscribe({
        next: () => {
          this.snack.open(
            `Wage rate ${active ? "activated" : "deactivated"}.`,
            "Dismiss",
            { duration: 3000 },
          );
          this.load(rate.id);
        },
        error: (error) => {
          this.snack.open(
            getApiErrorMessage(error, "Wage rate status could not be changed."),
            "Dismiss",
            { duration: 5000 },
          );
        },
      });
  }

  getGenderLabel(gender: string): string {
    return formatGender(gender);
  }

  getWageTypeLabel(wageType: string): string {
    return formatWageType(wageType);
  }

  getLifecycleLabel(rate: LaborWageRate): string {
    const lifecycle = getRateLifecycle(rate);
    switch (lifecycle) {
      case "CURRENT":
        return "Current";
      case "FUTURE":
        return "Future";
      case "HISTORICAL":
        return "Historical";
    }
  }

  getLifecycleClass(rate: LaborWageRate): string {
    const lifecycle = getRateLifecycle(rate);
    switch (lifecycle) {
      case "CURRENT":
        return "period-pill-current";
      case "FUTURE":
        return "period-pill-future";
      case "HISTORICAL":
        return "period-pill-historical";
    }
  }
}
