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
import { finalize, of } from "rxjs";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import { getApiErrorMessage } from "../../core/models/api-error.model";
@Component({
  selector: "app-crop-editor-page",
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
  templateUrl: "./crop-editor-page.component.html",
  styleUrl: "./crop-editor-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CropEditorPageComponent implements OnInit {
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
  readonly form = this.fb.nonNullable.group({
    code: ["", [Validators.required]],
    name: ["", [Validators.required]],
    scientificName: [""],
    cropType: ["GENERAL", [Validators.required]],
    cropDurationType: ["ANNUAL", [Validators.required]],
    description: [""],
  });
  ngOnInit(): void {
    const request$ = this.id ? this.service.getCrop(this.id) : of(null as any);
    request$
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (r) => {
          if (r)
            this.form.patchValue({
              code: r.code,
              name: r.name,
              scientificName: r.scientificName ?? "",
              cropType: r.cropType,
              cropDurationType: r.cropDurationType,
              description: r.description ?? "",
            });
        },
        error: (e) =>
          this.errorMessage.set(
            getApiErrorMessage(e, "Crop could not be loaded."),
          ),
      });
  }
  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.isSubmitting.set(true);
    const request = this.id
      ? this.service.updateCrop(this.id, this.form.getRawValue())
      : this.service.createCrop(this.form.getRawValue());
    request
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: () => {
          this.snack.open(
            this.id ? "Crop updated." : "Crop created.",
            "Dismiss",
            { duration: 3000 },
          );
          void this.router.navigateByUrl("/crops");
        },
        error: (e) =>
          this.errorMessage.set(
            getApiErrorMessage(e, "Crop could not be saved."),
          ),
      });
  }
}
