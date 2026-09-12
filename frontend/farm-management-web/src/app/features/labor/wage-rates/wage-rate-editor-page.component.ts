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
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatCheckboxModule } from "@angular/material/checkbox";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTooltipModule } from "@angular/material/tooltip";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { catchError, finalize, forkJoin, of, switchMap } from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import { BreadcrumbService } from "../../../core/breadcrumb/breadcrumb.service";
import {
  CreateLaborWageRateRequest,
  CurrencyItem,
  GENDER_OPTIONS,
  LaborWageRate,
  UpdateLaborWageRateRequest,
  WAGE_TYPE_OPTIONS,
  formatGender,
  formatWageType,
} from "../../../core/labor/labor.models";
import { LaborService } from "../../../core/labor/labor.service";
import { getApiValidationErrors } from "../../../core/models/api-error.model";
import { formatDateOnly, parseDateOnly } from "../../../core/utils/date.utils";
import { ErrorAlertComponent } from "../../../shared/components/error-alert/error-alert.component";

function effectiveToAfterEffectiveFrom(
  control: AbstractControl,
): ValidationErrors | null {
  const from = control.get("effectiveFrom")?.value as Date | string | null;
  const to = control.get("effectiveTo")?.value as Date | string | null;

  if (from && to) {
    const fromDate = parseDateOnly(from);
    const toDate = parseDateOnly(to);
    if (fromDate && toDate && toDate < fromDate) {
      return { effectiveToBeforeEffectiveFrom: true };
    }
  }
  return null;
}

@Component({
  selector: "app-wage-rate-editor-page",
  standalone: true,
  imports: [
    CommonModule,
    ErrorAlertComponent,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTooltipModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: "./wage-rate-editor-page.component.html",
  styleUrl: "./wage-rate-editor-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WageRateEditorPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly laborService = inject(LaborService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly breadcrumbService = inject(BreadcrumbService);
  readonly permissionService = inject(PermissionService);

  readonly rateId = this.route.snapshot.paramMap.get("id");
  readonly isEditing = this.rateId !== null;

  readonly genderOptions = GENDER_OPTIONS.filter((opt) => opt.value !== "all");
  readonly wageTypeOptions = WAGE_TYPE_OPTIONS;

  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<unknown>(null);
  readonly apiErrors = signal<Readonly<Record<string, readonly string[]>>>({});

  readonly currencies = signal<readonly CurrencyItem[]>([]);
  private initialActiveStatus = true;

  readonly form = this.fb.group(
    {
      gender: ["MALE", [Validators.required]],
      wageType: ["FULL_DAY", [Validators.required]],
      wageRate: [
        null as number | null,
        [Validators.required, Validators.min(0.01)],
      ],
      currencyId: ["", [Validators.required]],
      effectiveFrom: [new Date(), [Validators.required]],
      effectiveTo: [null as Date | null],
      notes: ["", [Validators.maxLength(500)]],
      isActive: [true],
    },
    { validators: effectiveToAfterEffectiveFrom },
  );

  ngOnInit(): void {
    this.loadData();
  }

  private loadData(): void {
    const rate$ = this.rateId
      ? this.laborService.getWageRate(this.rateId)
      : of(null);

    const currencies$ = this.laborService.listCurrencies().pipe(
      catchError(() =>
        of([
          {
            id: "10000000-0000-0000-0000-000000000001",
            code: "INR",
            name: "Indian Rupee",
            symbol: "₹",
            isSystem: true,
            isActive: true,
            displayOrder: 1,
          },
        ] as readonly CurrencyItem[]),
      ),
    );

    forkJoin({
      rate: rate$,
      currencies: currencies$,
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: ({ rate, currencies }) => {
          this.currencies.set(currencies);

          if (rate) {
            this.populateForm(rate);
            const title = `${formatGender(rate.gender)} · ${formatWageType(rate.wageType)}`;
            this.breadcrumbService.setTrail([
              { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
              { label: "Labor", route: "/labor" },
              { label: "Wage rates", route: "/labor/wage-rates" },
              { label: title, route: `/labor/wage-rates/${rate.id}` },
              { label: "Edit" },
            ]);
          } else {
            // Pick default currency if available (INR or first active currency)
            const defaultCurrency =
              currencies.find((c) => c.code === "INR" && c.isActive) ??
              currencies.find((c) => c.isActive) ??
              currencies[0];
            if (defaultCurrency) {
              this.form.controls.currencyId.setValue(defaultCurrency.id);
            }

            this.breadcrumbService.setTrail([
              { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
              { label: "Labor", route: "/labor" },
              { label: "Wage rates", route: "/labor/wage-rates" },
              { label: "New wage rate" },
            ]);
          }
        },
        error: (error: unknown) => {
          this.errorMessage.set(error);
        },
      });
  }

  private populateForm(rate: LaborWageRate): void {
    this.initialActiveStatus = rate.isActive;

    this.form.patchValue({
      gender: rate.gender,
      wageType: rate.wageType,
      wageRate: rate.wageRate,
      currencyId: rate.currencyId ?? "",
      effectiveFrom: parseDateOnly(rate.effectiveFrom),
      effectiveTo: parseDateOnly(rate.effectiveTo),
      notes: rate.notes ?? "",
      isActive: rate.isActive,
    });

    // Gender and WageType are immutable once created
    this.form.controls.gender.disable();
    this.form.controls.wageType.disable();

    const canToggleStatus = this.permissionService.has("WorkerWage.Update");
    if (!canToggleStatus) {
      this.form.controls.isActive.disable();
    }
  }

  apiError(field: string): string | null {
    return this.apiErrors()[field]?.[0] ?? null;
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.apiErrors.set({});

    const raw = this.form.getRawValue();

    if (this.isEditing && this.rateId) {
      const updateRequest: UpdateLaborWageRateRequest = {
        wageRate: Number(raw.wageRate),
        currencyId: raw.currencyId!,
        effectiveFrom: formatDateOnly(raw.effectiveFrom)!,
        effectiveTo: formatDateOnly(raw.effectiveTo),
        notes: raw.notes?.trim() || null,
      };

      const shouldChangeStatus =
        this.form.controls.isActive.enabled &&
        raw.isActive !== this.initialActiveStatus;

      this.laborService
        .updateWageRate(this.rateId, updateRequest)
        .pipe(
          switchMap((updated) => {
            if (shouldChangeStatus) {
              const statusAction$ = raw.isActive
                ? this.laborService.activateWageRate(this.rateId!)
                : this.laborService.deactivateWageRate(this.rateId!);
              return statusAction$.pipe(switchMap(() => of(updated)));
            }
            return of(updated);
          }),
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isSubmitting.set(false)),
        )
        .subscribe({
          next: () => {
            this.snack.open("Wage rate updated successfully.", "Dismiss", {
              duration: 3000,
            });
            void this.router.navigateByUrl(`/labor/wage-rates/${this.rateId}`);
          },
          error: (error: unknown) => {
            this.errorMessage.set(error);
            this.apiErrors.set(getApiValidationErrors(error));
          },
        });
    } else {
      const createRequest: CreateLaborWageRateRequest = {
        gender: raw.gender!,
        wageType: raw.wageType!,
        wageRate: Number(raw.wageRate),
        currencyId: raw.currencyId!,
        effectiveFrom: formatDateOnly(raw.effectiveFrom)!,
        effectiveTo: formatDateOnly(raw.effectiveTo),
        notes: raw.notes?.trim() || null,
      };

      this.laborService
        .createWageRate(createRequest)
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isSubmitting.set(false)),
        )
        .subscribe({
          next: (created) => {
            this.snack.open("Wage rate created successfully.", "Dismiss", {
              duration: 3000,
            });
            void this.router.navigateByUrl(`/labor/wage-rates/${created.id}`);
          },
          error: (error: unknown) => {
            this.errorMessage.set(error);
            this.apiErrors.set(getApiValidationErrors(error));
          },
        });
    }
  }
}
