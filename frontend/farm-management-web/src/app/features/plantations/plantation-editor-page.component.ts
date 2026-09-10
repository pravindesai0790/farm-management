import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import {
  forkJoin,
  of,
  finalize,
  switchMap,
  map,
  catchError,
  tap,
  Observable,
} from "rxjs";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import {
  Crop,
  CropVariety,
  Farm,
  FarmAreaOption,
  Plantation,
  Unit,
} from "../../core/farm-management/farm-management.models";
import { getApiErrorMessage } from "../../core/models/api-error.model";
import {
  formatDateOnly,
  parseDateOnly,
} from "../../core/utils/date.utils";
@Component({
  selector: "app-plantation-editor-page",
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: "./plantation-editor-page.component.html",
  styleUrl: "./plantation-editor-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlantationEditorPageComponent implements OnInit {
  private readonly service = inject(FarmManagementService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);
  readonly id = this.route.snapshot.paramMap.get("id");
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly currentPlantation = signal<Plantation | null>(null);
  readonly isLoadingAreas = signal(false);
  readonly farms = signal<readonly Farm[]>([]);
  readonly areas = signal<readonly FarmAreaOption[]>([]);
  readonly crops = signal<readonly Crop[]>([]);
  readonly varieties = signal<readonly CropVariety[]>([]);
  readonly units = signal<readonly Unit[]>([]);
  readonly selectedArea = computed(() =>
    this.areas().find((a) => a.id === this.form.controls.farmAreaId.value),
  );
  readonly form = this.fb.group({
    farmId: [null as string | null, [Validators.required]],
    farmAreaId: [null as string | null, [Validators.required]],
    cropId: [null as string | null, [Validators.required]],
    varietyId: [null as string | null],
    plantationCode: ["", [Validators.required]],
    plantationName: ["", [Validators.required]],
    allocatedArea: [
      null as number | null,
      [Validators.required, Validators.min(0.01)],
    ],
    areaUnitId: [null as string | null, [Validators.required]],
    plantingDate: [null as Date | null, [Validators.required]],
    expectedEndDate: [null as Date | null],
  });
  ngOnInit(): void {
    const base$ = forkJoin({
      farms: this.service.listFarms(1, 100, "", true),
      crops: this.service.listCrops(1, 100, "", true),
      units: this.service.listUnits(),
    });

    if (this.id) {
      forkJoin({
        base: base$,
        plantation: this.service.getPlantation(this.id),
      })
        .pipe(
          switchMap(({ base, plantation }) => {
            this.currentPlantation.set(plantation);
            const areas$ = plantation.farmId
              ? this.loadAreas(plantation.farmId, plantation)
              : of([] as readonly FarmAreaOption[]);
            const varieties$ = plantation.cropId
              ? this.service.listVarieties(plantation.cropId)
              : of({ items: [] as readonly CropVariety[], totalCount: 0 });

            return forkJoin({
              areas: areas$,
              varieties: varieties$,
            }).pipe(
              map(({ areas, varieties }) => ({
                ...base,
                plantation,
                areas,
                varieties: varieties.items,
              })),
            );
          }),
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isLoading.set(false)),
        )
        .subscribe({
          next: (r) => {
            this.farms.set(r.farms.items);
            this.crops.set(r.crops.items);
            this.units.set(r.units);
            this.varieties.set(r.varieties);
            this.form.patchValue({
              farmId: r.plantation.farmId,
              farmAreaId: r.plantation.farmAreaId,
              cropId: r.plantation.cropId,
              varietyId: r.plantation.varietyId,
              plantationCode: r.plantation.plantationCode,
              plantationName: r.plantation.plantationName,
              allocatedArea: r.plantation.allocatedArea,
              areaUnitId: r.plantation.areaUnitId,
              plantingDate: parseDateOnly(r.plantation.plantingDate),
              expectedEndDate: parseDateOnly(r.plantation.expectedEndDate),
            });
          },
          error: (e) =>
            this.errorMessage.set(
              getApiErrorMessage(e, "Plantation form data could not be loaded."),
            ),
        });
    } else {
      base$
        .pipe(
          switchMap((base) => {
            const defaultFarmId = base.farms.items[0]?.id;
            const areas$ = defaultFarmId
              ? this.loadAreas(defaultFarmId, null)
              : of([] as readonly FarmAreaOption[]);
            return areas$.pipe(
              map((areas) => ({
                ...base,
                areas,
                defaultFarmId,
              })),
            );
          }),
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isLoading.set(false)),
        )
        .subscribe({
          next: (r) => {
            this.farms.set(r.farms.items);
            this.crops.set(r.crops.items);
            this.units.set(r.units);
            if (r.defaultFarmId) {
              this.form.controls.farmId.setValue(r.defaultFarmId);
            }
          },
          error: (e) =>
            this.errorMessage.set(
              getApiErrorMessage(e, "Plantation form data could not be loaded."),
            ),
        });
    }
  }
  onFarmChange(farmId: string | null): void {
    this.form.controls.farmAreaId.setValue(null);
    if (!farmId) {
      this.areas.set([]);
      return;
    }
    this.loadAreas(farmId, this.currentPlantation())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe();
  }
  onAreaChange(areaId: string | null): void {
    if (!areaId) return;
    const area = this.areas().find((a) => a.id === areaId);
    if (area && !this.form.controls.areaUnitId.value) {
      this.form.controls.areaUnitId.setValue(area.areaUnitId);
    }
  }
  onCropChange(cropId: string | null): void {
    this.form.controls.varietyId.setValue(null);
    if (!cropId) {
      this.varieties.set([]);
      return;
    }
    this.loadVarieties(cropId);
  }
  loadAreas(
    farmId: string,
    currentPlantation?: Plantation | null,
  ): Observable<readonly FarmAreaOption[]> {
    this.isLoadingAreas.set(true);
    return this.service.listAreas(farmId, true).pipe(
      switchMap((areas) => {
        if (areas.length === 0) {
          return of([] as readonly FarmAreaOption[]);
        }
        return forkJoin(
          areas.map((area) =>
            this.service.getAreaAvailability(area.id).pipe(
              map((avail) => {
                const isCurrent = currentPlantation?.farmAreaId === area.id;
                const effectiveAvailable = isCurrent
                  ? avail.availableArea + (currentPlantation?.allocatedArea ?? 0)
                  : avail.availableArea;
                const isFullyUtilized =
                  !isCurrent && avail.availableArea <= 0.0001;
                return {
                  ...area,
                  allocatedArea: avail.allocatedArea,
                  availableArea: Number(effectiveAvailable.toFixed(4)),
                  isFullyUtilized,
                } as FarmAreaOption;
              }),
              catchError(() =>
                of({
                  ...area,
                  allocatedArea: 0,
                  availableArea: area.totalArea,
                  isFullyUtilized: false,
                } as FarmAreaOption),
              ),
            ),
          ),
        );
      }),
      tap((enriched) => {
        this.areas.set(enriched);
        this.isLoadingAreas.set(false);
      }),
      catchError((e) => {
        this.isLoadingAreas.set(false);
        this.errorMessage.set(
          getApiErrorMessage(e, "Farm areas could not be loaded."),
        );
        return of([] as readonly FarmAreaOption[]);
      }),
    );
  }
  loadVarieties(cropId: string): void {
    this.service
      .listVarieties(cropId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (r) => this.varieties.set(r.items),
        error: (e) =>
          this.errorMessage.set(
            getApiErrorMessage(e, "Varieties could not be loaded."),
          ),
      });
  }
  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.isSubmitting.set(true);
    const value = this.form.getRawValue();
    const payload = {
      ...value,
      plantingDate: formatDateOnly(value.plantingDate) ?? "",
      expectedEndDate: formatDateOnly(value.expectedEndDate),
    };
    const request = this.id
      ? this.service.updatePlantation(this.id, payload)
      : this.service.createPlantation(payload);
    request
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: () => {
          this.snack.open(
            this.id ? "Plantation updated." : "Plantation created.",
            "Dismiss",
            { duration: 3000 },
          );
          void this.router.navigateByUrl("/plantations");
        },
        error: (e) =>
          this.errorMessage.set(
            getApiErrorMessage(e, "Plantation could not be saved."),
          ),
      });
  }
}
