import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatNativeDateModule } from "@angular/material/core";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { finalize, forkJoin, of, switchMap } from "rxjs";

import { PermissionService } from "../../../../core/auth/permission.service";
import {
  CropCycle,
  Farm,
  FarmArea,
  Plantation,
} from "../../../../core/farm-management/farm-management.models";
import { FarmManagementService } from "../../../../core/farm-management/farm-management.service";
import { formatDateOnly, parseDateOnly } from "../../../../core/utils/date.utils";
import { ErrorAlertComponent } from "../../../../shared/components/error-alert/error-alert.component";
import {
  CreateLaborActivityRequest,
  LaborActivity,
  LaborActivityStatus,
  NamedReference,
  UpdateLaborActivityRequest,
} from "../../models/labor-activity.models";
import { LaborActivityService } from "../../services/labor-activity.service";

@Component({
  selector: "app-labor-activity-editor-page",
  standalone: true,
  imports: [
    CommonModule,
    ErrorAlertComponent,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatNativeDateModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: "./labor-activity-editor-page.component.html",
  styleUrl: "./labor-activity-editor-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LaborActivityEditorPageComponent implements OnInit {
  private readonly activityService = inject(LaborActivityService);
  private readonly farmService = inject(FarmManagementService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);
  readonly permissionService = inject(PermissionService);

  readonly id = this.route.snapshot.paramMap.get("id");
  readonly isEditMode = computed(() => !!this.id);

  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<unknown>(null);
  readonly isCancelled = signal(false);

  // Loading signals for dependent dropdowns
  readonly isLoadingAreas = signal(false);
  readonly isLoadingPlantations = signal(false);
  readonly isLoadingCycles = signal(false);

  // Master data signals
  readonly farms = signal<readonly Farm[]>([]);
  readonly areas = signal<readonly FarmArea[]>([]);
  readonly plantations = signal<readonly Plantation[]>([]);
  readonly cropCycles = signal<readonly CropCycle[]>([]);
  readonly activityTypes = signal<readonly NamedReference[]>([]);

  readonly activityCurrency = signal("INR");

  readonly form: FormGroup = this.fb.group({
    activityDate: [new Date(), [Validators.required]],
    farmId: [null as string | null, [Validators.required]],
    farmAreaId: [null as string | null, [Validators.required]],
    plantationId: [null as string | null, [Validators.required]],
    cropCycleId: [null as string | null],
    laborActivityTypeId: [null as string | null, [Validators.required]],
    workerCount: [1, [Validators.required, Validators.min(1)]],
    totalWorkingHours: [null as number | null, [Validators.min(0.01)]],
    costAmount: [null as number | null, [Validators.min(0)]],
    status: ["COMPLETED" as LaborActivityStatus, [Validators.required]],
    description: ["", [Validators.maxLength(1000)]],
  });

  ngOnInit(): void {
    if (this.id) {
      this.loadExistingActivity(this.id);
    } else {
      this.loadInitialData();
    }
  }

  private loadInitialData(): void {
    forkJoin({
      farms: this.farmService.listFarms(1, 100, "", true),
      types: this.activityService.getTypes(),
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: ({ farms, types }) => {
          this.farms.set(farms.items);
          this.activityTypes.set(types);
        },
        error: (err) => this.errorMessage.set(err),
      });
  }

  private loadExistingActivity(id: string): void {
    forkJoin({
      farms: this.farmService.listFarms(1, 100, "", true),
      types: this.activityService.getTypes(),
      activity: this.activityService.get(id),
    })
      .pipe(
        switchMap(({ farms, types, activity }) => {
          this.farms.set(farms.items);
          this.activityTypes.set(types);
          this.activityCurrency.set("INR");

          if (activity.status === "CANCELLED") {
            this.isCancelled.set(true);
            this.form.disable();
          }

          const farmId = activity.farm.id;
          const areaId = activity.farmArea?.id ?? null;
          const plantationId = activity.plantation?.id ?? null;

          const areas$ = farmId ? this.farmService.listAreas(farmId, true) : of([]);
          const plantations$ =
            farmId && areaId
              ? this.farmService.listPlantations(1, 100, farmId, areaId)
              : of({ items: [] as Plantation[], totalCount: 0 });
          const cycles$ =
            plantationId
              ? this.farmService.listCycles(1, 100, farmId, areaId ?? undefined, plantationId)
              : of({ items: [] as CropCycle[], totalCount: 0 });

          return forkJoin({
            areas: areas$,
            plantations: plantations$,
            cycles: cycles$,
          }).pipe(
            finalize(() => this.isLoading.set(false)),
            switchMap(({ areas, plantations, cycles }) => {
              this.areas.set(areas);
              this.plantations.set(plantations.items);
              this.cropCycles.set(cycles.items);

              this.form.patchValue({
                activityDate: parseDateOnly(activity.activityDate),
                farmId: activity.farm.id,
                farmAreaId: activity.farmArea?.id ?? null,
                plantationId: activity.plantation?.id ?? null,
                cropCycleId: activity.cropCycle?.id ?? null,
                laborActivityTypeId: activity.activityType.id,
                status: activity.status,
                description: activity.description ?? "",
              });

              return of(activity);
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(err);
        },
      });
  }

  onFarmChange(newFarmId: string | null): void {
    // Reset dependent controls
    this.form.patchValue({
      farmAreaId: null,
      plantationId: null,
      cropCycleId: null,
    });
    this.areas.set([]);
    this.plantations.set([]);
    this.cropCycles.set([]);

    if (!newFarmId) {
      return;
    }

    this.isLoadingAreas.set(true);
    this.farmService
      .listAreas(newFarmId, true)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoadingAreas.set(false)),
      )
      .subscribe({
        next: (areas) => this.areas.set(areas),
        error: (err) => this.errorMessage.set(err),
      });
  }

  onAreaChange(newAreaId: string | null): void {
    // Reset child controls
    this.form.patchValue({
      plantationId: null,
      cropCycleId: null,
    });
    this.plantations.set([]);
    this.cropCycles.set([]);

    const farmId = this.form.get("farmId")?.value;
    if (!farmId || !newAreaId) {
      return;
    }

    this.isLoadingPlantations.set(true);
    this.farmService
      .listPlantations(1, 100, farmId, newAreaId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoadingPlantations.set(false)),
      )
      .subscribe({
        next: (res) => this.plantations.set(res.items),
        error: (err) => this.errorMessage.set(err),
      });
  }

  onPlantationChange(newPlantationId: string | null): void {
    // Reset crop cycle
    this.form.patchValue({
      cropCycleId: null,
    });
    this.cropCycles.set([]);

    const farmId = this.form.get("farmId")?.value;
    const farmAreaId = this.form.get("farmAreaId")?.value;
    if (!newPlantationId) {
      return;
    }

    this.isLoadingCycles.set(true);
    this.farmService
      .listCycles(1, 100, farmId ?? undefined, farmAreaId ?? undefined, newPlantationId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoadingCycles.set(false)),
      )
      .subscribe({
        next: (res) => this.cropCycles.set(res.items),
        error: (err) => this.errorMessage.set(err),
      });
  }

  submit(): void {
    if (this.isCancelled()) {
      return;
    }

    this.errorMessage.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const formValue = this.form.getRawValue();
    const formattedDate = formatDateOnly(formValue.activityDate);
    if (!formattedDate) {
      this.form.get("activityDate")?.setErrors({ required: true });
      return;
    }

    this.isSubmitting.set(true);

    const payload: CreateLaborActivityRequest = {
      activityDate: formattedDate,
      farmId: formValue.farmId,
      farmAreaId: formValue.farmAreaId,
      plantationId: formValue.plantationId,
      cropCycleId: formValue.cropCycleId || null,
      laborActivityTypeId: formValue.laborActivityTypeId,
      workerCount: Number(formValue.workerCount),
      totalWorkingHours:
        formValue.totalWorkingHours !== null && formValue.totalWorkingHours !== ""
          ? Number(formValue.totalWorkingHours)
          : null,
      costAmount:
        formValue.costAmount !== null && formValue.costAmount !== ""
          ? Number(formValue.costAmount)
          : null,
      status: formValue.status,
      description: formValue.description?.trim() || null,
    };

    const action$ = this.id
      ? this.activityService.update(this.id, payload as UpdateLaborActivityRequest)
      : this.activityService.create(payload);

    action$
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: () => {
          const message = this.id
            ? "Labor activity updated successfully."
            : "Labor activity recorded successfully.";
          this.snack.open(message, "Dismiss", { duration: 4000 });
          void this.router.navigateByUrl("/activities/labor-activities");
        },
        error: (err) => {
          this.errorMessage.set(err);
        },
      });
  }
}
