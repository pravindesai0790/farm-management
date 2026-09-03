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
  selector: "app-farm-editor-page",
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
  templateUrl: "./farm-editor-page.component.html",
  styleUrl: "./farm-editor-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FarmEditorPageComponent implements OnInit {
  private readonly service = inject(FarmManagementService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);
  readonly farmId = this.route.snapshot.paramMap.get("id");
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly units = signal<readonly any[]>([]);
  readonly ownershipTypes = signal<readonly any[]>([]);
  readonly form = this.fb.group({
    code: ["", [Validators.required, Validators.maxLength(50)]],
    name: ["", [Validators.required, Validators.maxLength(200)]],
    description: [""],
    ownershipTypeId: ["", [Validators.required]],
    totalArea: [null as number | null],
    areaUnitId: [null as string | null],
    city: [""],
    state: [""],
    latitude: [null as number | null],
    longitude: [null as number | null],
  });
  ngOnInit(): void {
    forkJoin({
      farm: this.farmId ? this.service.getFarm(this.farmId) : of(null),
      units: this.service.listUnits(),
      ownership: this.service.listOwnershipTypes(),
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (r) => {
          this.units.set(r.units);
          this.ownershipTypes.set(r.ownership);
          if (r.farm)
            this.form.patchValue({
              code: r.farm.code,
              name: r.farm.name,
              description: r.farm.description ?? "",
              ownershipTypeId: r.farm.ownershipTypeId,
              totalArea: r.farm.totalArea,
              areaUnitId: r.farm.areaUnitId,
              city: r.farm.city ?? "",
              state: r.farm.state ?? "",
              latitude: r.farm.latitude,
              longitude: r.farm.longitude,
            });
        },
        error: (e) =>
          this.errorMessage.set(
            getApiErrorMessage(e, "Farm form data could not be loaded."),
          ),
      });
  }
  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.isSubmitting.set(true);
    const request = this.farmId
      ? this.service.updateFarm(this.farmId, this.form.getRawValue())
      : this.service.createFarm(this.form.getRawValue());
    request
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: () => {
          this.snack.open(
            this.farmId ? "Farm updated." : "Farm created.",
            "Dismiss",
            { duration: 3000 },
          );
          void this.router.navigateByUrl("/farms");
        },
        error: (e) =>
          this.errorMessage.set(
            getApiErrorMessage(e, "Farm could not be saved."),
          ),
      });
  }
}
