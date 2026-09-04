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
import { MatSnackBar } from "@angular/material/snack-bar";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { forkJoin, finalize } from "rxjs";
import { PermissionService } from "../../core/auth/permission.service";
import { BreadcrumbService } from "../../core/breadcrumb/breadcrumb.service";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import {
  Crop,
  CropVariety,
} from "../../core/farm-management/farm-management.models";
import { getApiErrorMessage } from "../../core/models/api-error.model";
@Component({
  selector: "app-crop-detail-page",
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: "./crop-detail-page.component.html",
  styleUrl: "./crop-detail-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CropDetailPageComponent implements OnInit {
  private readonly service = inject(FarmManagementService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly breadcrumbService = inject(BreadcrumbService);
  private readonly snack = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);
  readonly permissionService = inject(PermissionService);
  readonly id = this.route.snapshot.paramMap.get("id")!;
  readonly crop = signal<Crop | null>(null);
  readonly varieties = signal<readonly CropVariety[]>([]);
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly varietyForm = this.fb.nonNullable.group({
    code: ["", [Validators.required]],
    name: ["", [Validators.required]],
  });
  ngOnInit(): void {
    forkJoin({
      crop: this.service.getCrop(this.id),
      varieties: this.service.listVarieties(this.id),
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (r) => {
          this.crop.set(r.crop);
          this.varieties.set(r.varieties.items);
          this.breadcrumbService.setEntityName(r.crop.id, r.crop.name);
          this.breadcrumbService.setTrail([
            { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
            { label: "Crop catalog", route: "/crops" },
            { label: r.crop.name },
          ]);
        },
        error: (e) =>
          this.snack.open(
            getApiErrorMessage(e, "Crop could not be loaded."),
            "Dismiss",
            { duration: 5000 },
          ),
      });
  }
  addVariety(): void {
    if (this.varietyForm.invalid) return;
    this.isSubmitting.set(true);
    this.service
      .createVariety({ ...this.varietyForm.getRawValue(), cropId: this.id })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: (r) => {
          this.varieties.set([...this.varieties(), r]);
          this.varietyForm.reset();
        },
        error: (e) =>
          this.snack.open(
            getApiErrorMessage(e, "Variety could not be added."),
            "Dismiss",
            { duration: 5000 },
          ),
      });
  }
}
