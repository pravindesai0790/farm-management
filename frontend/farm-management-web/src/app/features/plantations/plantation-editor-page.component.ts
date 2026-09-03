import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { forkJoin, of, finalize } from "rxjs";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import { getApiErrorMessage } from "../../core/models/api-error.model";
@Component({
  selector: "app-plantation-editor-page",
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
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
  readonly farms = signal<readonly any[]>([]);
  readonly areas = signal<readonly any[]>([]);
  readonly crops = signal<readonly any[]>([]);
  readonly varieties = signal<readonly any[]>([]);
  readonly units = signal<readonly any[]>([]);
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
    plantingDate: ["", [Validators.required]],
    expectedEndDate: [""],
  });
  ngOnInit(): void {
    forkJoin({
      farms: this.service.listFarms(1, 100, "", true),
      crops: this.service.listCrops(1, 100, "", true),
      units: this.service.listUnits(),
      plantation: this.id ? this.service.getPlantation(this.id) : of(null),
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (r) => {
          this.farms.set(r.farms.items);
          this.crops.set(r.crops.items);
          this.units.set(r.units);
          if (r.plantation) {
            this.form.patchValue({
              farmId: r.plantation.farmId,
              farmAreaId: r.plantation.farmAreaId,
              cropId: r.plantation.cropId,
              varietyId: r.plantation.varietyId,
              plantationCode: r.plantation.plantationCode,
              plantationName: r.plantation.plantationName,
              allocatedArea: r.plantation.allocatedArea,
              areaUnitId: r.plantation.areaUnitId,
              plantingDate: r.plantation.plantingDate,
              expectedEndDate: r.plantation.expectedEndDate ?? "",
            });
            this.loadAreas(r.plantation.farmId);
            this.loadVarieties(r.plantation.cropId);
          } else if (r.farms.items[0]) {
            this.form.controls.farmId.setValue(r.farms.items[0].id);
            this.loadAreas(r.farms.items[0].id);
          }
        },
        error: (e) =>
          this.errorMessage.set(
            getApiErrorMessage(e, "Plantation form data could not be loaded."),
          ),
      });
  }
  loadAreas(farmId: string): void {
    this.form.controls.farmAreaId.setValue(null);
    this.service
      .listAreas(farmId, true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((r) => this.areas.set(r));
  }
  loadVarieties(cropId: string): void {
    this.form.controls.varietyId.setValue(null);
    this.service
      .listVarieties(cropId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((r) => this.varieties.set(r.items));
  }
  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.isSubmitting.set(true);
    const value = this.form.getRawValue();
    const request = this.id
      ? this.service.updatePlantation(this.id, value)
      : this.service.createPlantation(value);
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
