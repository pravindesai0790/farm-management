import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from "@angular/core";
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
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import {
  catchError,
  finalize,
  forkJoin,
  merge,
  of,
  switchMap,
} from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import { BreadcrumbService } from "../../../core/breadcrumb/breadcrumb.service";
import {
  ContractorItem,
  CreateWorkerRequest,
  EmploymentType,
  Gender,
  LaborCategoryItem,
  UpdateWorkerRequest,
  WORKER_EMPLOYMENT_TYPE_OPTIONS,
  WORKER_GENDER_OPTIONS,
  WorkerDetail,
} from "../../../core/labor/labor.models";
import { LaborService } from "../../../core/labor/labor.service";
import { getApiValidationErrors } from "../../../core/models/api-error.model";
import { formatDateOnly, parseDateOnly } from "../../../core/utils/date.utils";
import { ErrorAlertComponent } from "../../../shared/components/error-alert/error-alert.component";

interface SelectOption {
  readonly id: string;
  readonly name: string;
  readonly isActive: boolean;
}

function leavingDateAfterJoiningDate(
  control: AbstractControl,
): ValidationErrors | null {
  const joining = control.get("joiningDate")?.value as Date | string | null;
  const leaving = control.get("leavingDate")?.value as Date | string | null;

  if (joining && leaving) {
    const joiningDate = parseDateOnly(joining);
    const leavingDate = parseDateOnly(leaving);
    if (joiningDate && leavingDate && leavingDate < joiningDate) {
      return { leavingDateBeforeJoiningDate: true };
    }
  }
  return null;
}

@Component({
  selector: "app-worker-editor-page",
  standalone: true,
  imports: [
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
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: "./worker-editor-page.component.html",
  styleUrl: "./worker-editor-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkerEditorPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly laborService = inject(LaborService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly breadcrumbService = inject(BreadcrumbService);
  readonly permissionService = inject(PermissionService);

  readonly workerId = this.route.snapshot.paramMap.get("id");
  readonly isEditing = this.workerId !== null;

  readonly genderOptions = WORKER_GENDER_OPTIONS;
  readonly employmentTypeOptions = WORKER_EMPLOYMENT_TYPE_OPTIONS;

  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<unknown>(null);
  readonly apiErrors = signal<Readonly<Record<string, readonly string[]>>>({});

  readonly contractors = signal<readonly SelectOption[]>([]);
  readonly laborCategories = signal<readonly SelectOption[]>([]);

  private initialActiveStatus = true;
  private isDisplayNameCustomized = false;

  readonly form = this.fb.group(
    {
      firstName: ["", [Validators.required, Validators.maxLength(100)]],
      lastName: ["", [Validators.maxLength(100)]],
      displayName: ["", [Validators.maxLength(200)]],
      gender: ["" as Gender, [Validators.required]],
      mobileNumber: ["", [Validators.maxLength(30)]],
      alternateMobileNumber: ["", [Validators.maxLength(30)]],
      laborCategoryId: [null as string | null],
      employmentType: ["" as EmploymentType, [Validators.required]],
      contractorId: [{ value: null as string | null, disabled: true }],
      joiningDate: [null as Date | null],
      leavingDate: [null as Date | null],
      notes: [""],
      isActive: [true],
    },
    { validators: leavingDateAfterJoiningDate },
  );

  ngOnInit(): void {
    this.setupEmploymentTypeListener();
    this.setupDisplayNameAutoGeneration();
    this.loadData();
  }

  private setupEmploymentTypeListener(): void {
    this.form.controls.employmentType.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((type) => {
        this.updateContractorControlState(type);
      });
  }

  private updateContractorControlState(type: string | null): void {
    const contractorControl = this.form.controls.contractorId;
    if (type === "CONTRACT") {
      if (contractorControl.disabled) {
        contractorControl.enable();
      }
      contractorControl.setValidators(Validators.required);
    } else {
      contractorControl.clearValidators();
      contractorControl.setValue(null);
      contractorControl.disable();
    }
    contractorControl.updateValueAndValidity();
  }

  private setupDisplayNameAutoGeneration(): void {
    merge(
      this.form.controls.firstName.valueChanges,
      this.form.controls.lastName.valueChanges,
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        if (!this.isDisplayNameCustomized && !this.isEditing) {
          const first = this.form.controls.firstName.value?.trim() ?? "";
          const last = this.form.controls.lastName.value?.trim() ?? "";
          const full = [first, last].filter(Boolean).join(" ");
          this.form.controls.displayName.setValue(full, { emitEvent: false });
        }
      });

    this.form.controls.displayName.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        if (this.form.controls.displayName.dirty) {
          this.isDisplayNameCustomized = true;
        }
      });
  }

  private loadData(): void {
    const worker$ = this.workerId
      ? this.laborService.getWorker(this.workerId)
      : of(null);

    const contractors$ = this.laborService
      .listContractors(1, 100, undefined, true)
      .pipe(
        catchError(() =>
          of({ items: [] as readonly ContractorItem[], totalCount: 0 }),
        ),
      );

    const categories$ = this.laborService
      .listLaborCategories(1, 100, undefined, true)
      .pipe(
        catchError(() =>
          of({ items: [] as readonly LaborCategoryItem[], totalCount: 0 }),
        ),
      );

    forkJoin({
      worker: worker$,
      contractors: contractors$,
      categories: categories$,
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: ({ worker, contractors, categories }) => {
          const contractorOptions: SelectOption[] = contractors.items.map(
            (c) => ({
              id: c.id,
              name: c.name,
              isActive: c.isActive,
            }),
          );

          const categoryOptions: SelectOption[] = categories.items.map((c) => ({
            id: c.id,
            name: c.isSystem ? `${c.name} (System)` : c.name,
            isActive: c.isActive,
          }));

          if (worker) {
            if (
              worker.contractorId &&
              !contractorOptions.some((c) => c.id === worker.contractorId)
            ) {
              contractorOptions.unshift({
                id: worker.contractorId,
                name: worker.contractorName
                  ? `${worker.contractorName} (Inactive)`
                  : "Assigned Contractor (Inactive)",
                isActive: false,
              });
            }

            if (
              worker.laborCategoryId &&
              !categoryOptions.some((c) => c.id === worker.laborCategoryId)
            ) {
              categoryOptions.unshift({
                id: worker.laborCategoryId,
                name: worker.laborCategoryName
                  ? `${worker.laborCategoryName} (Inactive)`
                  : "Assigned Category (Inactive)",
                isActive: false,
              });
            }

            this.populateForm(worker);
            this.breadcrumbService.setEntityName(worker.id, worker.displayName);
            this.breadcrumbService.setTrail([
              { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
              { label: "Labor", route: "/labor" },
              { label: "Workers", route: "/labor/workers" },
              { label: worker.displayName },
              { label: "Edit" },
            ]);
          } else {
            this.breadcrumbService.setTrail([
              { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
              { label: "Labor", route: "/labor" },
              { label: "Workers", route: "/labor/workers" },
              { label: "New worker" },
            ]);
          }

          this.contractors.set(contractorOptions);
          this.laborCategories.set(categoryOptions);
        },
        error: (error: unknown) => {
          this.errorMessage.set(error);
        },
      });
  }

  private populateForm(worker: WorkerDetail): void {
    this.initialActiveStatus = worker.isActive;
    this.isDisplayNameCustomized = true;

    this.form.patchValue({
      firstName: worker.firstName,
      lastName: worker.lastName ?? "",
      displayName: worker.displayName,
      gender: worker.gender as Gender,
      employmentType: worker.employmentType as EmploymentType,
      mobileNumber: worker.mobileNumber ?? "",
      alternateMobileNumber: worker.alternateMobileNumber ?? "",
      laborCategoryId: worker.laborCategoryId ?? null,
      contractorId: worker.contractorId ?? null,
      joiningDate: parseDateOnly(worker.joiningDate),
      leavingDate: parseDateOnly(worker.leavingDate),
      notes: worker.notes ?? "",
      isActive: worker.isActive,
    });

    this.updateContractorControlState(worker.employmentType);

    const canToggleStatus = worker.isActive
      ? this.permissionService.has("Worker.Deactivate")
      : this.permissionService.has("Worker.Activate");

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
    const firstName = raw.firstName?.trim() ?? "";
    const gender = (raw.gender ?? "OTHER") as Gender;
    const employmentType = (raw.employmentType ?? "PERMANENT") as EmploymentType;

    if (this.isEditing && this.workerId) {
      const updateRequest: UpdateWorkerRequest = {
        firstName,
        lastName: raw.lastName?.trim() || null,
        displayName: raw.displayName?.trim() || null,
        gender,
        employmentType,
        mobileNumber: raw.mobileNumber?.trim() || null,
        alternateMobileNumber: raw.alternateMobileNumber?.trim() || null,
        laborCategoryId: raw.laborCategoryId || null,
        contractorId:
          employmentType === "CONTRACT" ? raw.contractorId || null : null,
        joiningDate: formatDateOnly(raw.joiningDate),
        leavingDate: formatDateOnly(raw.leavingDate),
        notes: raw.notes?.trim() || null,
      };

      const shouldChangeStatus =
        this.form.controls.isActive.enabled &&
        raw.isActive !== this.initialActiveStatus;

      this.laborService
        .updateWorker(this.workerId, updateRequest)
        .pipe(
          switchMap((updated) => {
            if (shouldChangeStatus) {
              const statusAction$ = raw.isActive
                ? this.laborService.activateWorker(this.workerId!)
                : this.laborService.deactivateWorker(this.workerId!);
              return statusAction$.pipe(switchMap(() => of(updated)));
            }
            return of(updated);
          }),
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isSubmitting.set(false)),
        )
        .subscribe({
          next: () => {
            this.snack.open("Worker updated successfully.", "Dismiss", {
              duration: 3000,
            });
            void this.router.navigateByUrl(`/labor/workers/${this.workerId}`);
          },
          error: (error: unknown) => {
            this.errorMessage.set(error);
            this.apiErrors.set(getApiValidationErrors(error));
          },
        });
    } else {
      const createRequest: CreateWorkerRequest = {
        firstName,
        lastName: raw.lastName?.trim() || null,
        displayName: raw.displayName?.trim() || null,
        gender,
        employmentType,
        mobileNumber: raw.mobileNumber?.trim() || null,
        alternateMobileNumber: raw.alternateMobileNumber?.trim() || null,
        laborCategoryId: raw.laborCategoryId || null,
        contractorId:
          employmentType === "CONTRACT" ? raw.contractorId || null : null,
        joiningDate: formatDateOnly(raw.joiningDate),
        leavingDate: formatDateOnly(raw.leavingDate),
        notes: raw.notes?.trim() || null,
      };

      this.laborService
        .createWorker(createRequest)
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isSubmitting.set(false)),
        )
        .subscribe({
          next: (created) => {
            this.snack.open("Worker created successfully.", "Dismiss", {
              duration: 3000,
            });
            void this.router.navigateByUrl(`/labor/workers/${created.id}`);
          },
          error: (error: unknown) => {
            this.errorMessage.set(error);
            this.apiErrors.set(getApiValidationErrors(error));
          },
        });
    }
  }
}
