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
import { forkJoin, finalize, of } from "rxjs";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import { getApiErrorMessage } from "../../core/models/api-error.model";
@Component({
  selector: "app-farm-area-editor-page",
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
  templateUrl: "./farm-area-editor-page.component.html",
  styleUrl: "./farm-area-editor-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FarmAreaEditorPageComponent implements OnInit {
  private readonly service = inject(FarmManagementService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);
  readonly areaId = this.route.snapshot.paramMap.get("id");
  readonly farmId = this.route.snapshot.queryParamMap.get("farmId");
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly farms = signal<readonly any[]>([]);
  readonly parentAreas = signal<readonly any[]>([]);
  readonly units = signal<readonly any[]>([]);
  readonly form = this.fb.group({
    farmId: [this.farmId, [Validators.required]],
    parentFarmAreaId: [null as string | null],
    code: ["", [Validators.required]],
    name: ["", [Validators.required]],
    description: [""],
    totalArea: [
      null as number | null,
      [Validators.required, Validators.min(0.01)],
    ],
    areaUnitId: [null as string | null, [Validators.required]],
  });
  ngOnInit(): void {
    const farmList$ = this.service.listFarms(1, 100, "", true);
    const area$ = this.areaId ? this.service.getArea(this.areaId) : of(null);
    forkJoin({ farms: farmList$, area: area$, units: this.service.listUnits() })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (r) => {
          this.farms.set(r.farms.items);
          this.units.set(r.units);
          const selectedFarm =
            this.areaId && r.area
              ? r.area.farmId
              : (this.farmId ?? r.farms.items[0]?.id);
          if (selectedFarm) this.loadParents(selectedFarm);
          if (r.area)
            this.form.patchValue({
              farmId: r.area.farmId,
              parentFarmAreaId: r.area.parentFarmAreaId,
              code: r.area.code,
              name: r.area.name,
              description: r.area.description ?? "",
              totalArea: r.area.totalArea,
              areaUnitId: r.area.areaUnitId,
            });
        },
        error: (e) =>
          this.errorMessage.set(
            getApiErrorMessage(e, "Area form data could not be loaded."),
          ),
      });
  }
  loadParents(farmId: string): void {
    this.service
      .listAreas(farmId, true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((areas) =>
        this.parentAreas.set(
          areas.filter(
            (area) => area.id !== this.areaId && !area.parentFarmAreaId,
          ),
        ),
      );
  }
  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.isSubmitting.set(true);
    const value = this.form.getRawValue();
    const request = this.areaId
      ? this.service.updateArea(this.areaId, value)
      : this.service.createArea(value);
    request
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: (r) => {
          this.snack.open(
            this.areaId ? "Area updated." : "Area created.",
            "Dismiss",
            { duration: 3000 },
          );
          void this.router.navigate(["/farms", r.farmId]);
        },
        error: (e) =>
          this.errorMessage.set(
            getApiErrorMessage(e, "Area could not be saved."),
          ),
      });
  }
}
