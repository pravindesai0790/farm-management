import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormBuilder, ReactiveFormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatPaginatorModule, PageEvent } from "@angular/material/paginator";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTableModule } from "@angular/material/table";
import { MatTooltipModule } from "@angular/material/tooltip";
import { RouterLink } from "@angular/router";
import { finalize, merge } from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import { BreadcrumbService } from "../../../core/breadcrumb/breadcrumb.service";
import {
  GENDER_OPTIONS,
  LaborWageRate,
  RateLifecycle,
  WAGE_TYPE_OPTIONS,
  formatGender,
  formatWageType,
  getRateLifecycle,
} from "../../../core/labor/labor.models";
import { LaborService } from "../../../core/labor/labor.service";
import { getApiErrorMessage } from "../../../core/models/api-error.model";

@Component({
  selector: "app-wage-rates-page",
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: "./wage-rates-page.component.html",
  styleUrl: "./wage-rates-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WageRatesPageComponent implements OnInit {
  private readonly laborService = inject(LaborService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly breadcrumbService = inject(BreadcrumbService);
  readonly permissionService = inject(PermissionService);

  readonly displayedColumns: readonly string[] = [
    "gender",
    "wageType",
    "wageRate",
    "currency",
    "effectivePeriod",
    "periodStatus",
    "status",
    "actions",
  ];

  readonly genderOptions = GENDER_OPTIONS;
  readonly wageTypeOptions = WAGE_TYPE_OPTIONS;

  readonly wageRates = signal<readonly LaborWageRate[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly isLoading = signal(false);
  readonly actionInProgress = signal(false);

  readonly filterForm = this.formBuilder.nonNullable.group({
    status: ["all"],
    gender: ["all"],
    wageType: ["all"],
    effectiveAsOf: [""],
  });

  ngOnInit(): void {
    this.breadcrumbService.setTrail([
      { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
      { label: "Labor", route: "/labor" },
      { label: "Wage rates" },
    ]);

    merge(
      this.filterForm.controls.status.valueChanges,
      this.filterForm.controls.gender.valueChanges,
      this.filterForm.controls.wageType.valueChanges,
      this.filterForm.controls.effectiveAsOf.valueChanges,
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.load();
      });

    this.load();
  }

  load(): void {
    this.isLoading.set(true);

    const { status, gender, wageType, effectiveAsOf } = this.filterForm.getRawValue();

    const isActive =
      status === "all" ? null : status === "active";

    const normalizedGender =
      gender === "all" || !gender ? null : gender;

    const normalizedWageType =
      wageType === "all" || !wageType ? null : wageType;

    const normalizedBusinessDate =
      effectiveAsOf?.trim() || null;

    this.laborService
      .listWageRatesPaged(
        this.pageIndex() + 1,
        this.pageSize(),
        normalizedGender,
        normalizedWageType,
        isActive,
        normalizedBusinessDate,
      )
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (response) => {
          this.wageRates.set(response.items);
          this.totalCount.set(response.totalCount);
        },
        error: (error) => {
          this.snack.open(
            getApiErrorMessage(error, "Wage rates could not be loaded."),
            "Dismiss",
            { duration: 5000 },
          );
        },
      });
  }

  pageChanged(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  clearEffectiveAsOf(): void {
    this.filterForm.controls.effectiveAsOf.setValue("");
  }

  changeStatus(rate: LaborWageRate, active: boolean): void {
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
            `Wage rate for ${this.getGenderLabel(rate.gender as string)} - ${this.getWageTypeLabel(rate.wageType)} ${active ? "activated" : "deactivated"}.`,
            "Dismiss",
            { duration: 3000 },
          );
          this.load();
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

  getLifecycle(rate: LaborWageRate): RateLifecycle {
    return getRateLifecycle(rate);
  }

  getLifecycleLabel(rate: LaborWageRate): string {
    const lifecycle = this.getLifecycle(rate);
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
    const lifecycle = this.getLifecycle(rate);
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
