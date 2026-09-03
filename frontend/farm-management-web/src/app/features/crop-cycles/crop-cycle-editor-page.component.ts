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
  selector: "app-crop-cycle-editor-page",
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
  templateUrl: "./crop-cycle-editor-page.component.html",
  styleUrl: "./crop-cycle-editor-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CropCycleEditorPageComponent implements OnInit {
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
  readonly plantations = signal<readonly any[]>([]);
  readonly form = this.fb.group({
    plantationId: [null as string | null, [Validators.required]],
    cycleCode: ["", [Validators.required]],
    cycleName: ["", [Validators.required]],
    seasonYear: [new Date().getFullYear(), [Validators.required]],
    seasonName: [""],
    plannedStartDate: ["", [Validators.required]],
    expectedEndDate: [""],
  });
  ngOnInit(): void {
    forkJoin({
      plantations: this.service.listPlantations(),
      cycle: this.id ? this.service.getCycle(this.id) : of(null),
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (r) => {
          this.plantations.set(r.plantations.items);
          if (r.cycle)
            this.form.patchValue({
              plantationId: r.cycle.plantationId,
              cycleCode: r.cycle.cycleCode,
              cycleName: r.cycle.cycleName,
              seasonYear: r.cycle.seasonYear,
              seasonName: r.cycle.seasonName ?? "",
              plannedStartDate: r.cycle.plannedStartDate,
              expectedEndDate: r.cycle.expectedEndDate ?? "",
            });
        },
        error: (e) =>
          this.errorMessage.set(
            getApiErrorMessage(e, "Cycle form data could not be loaded."),
          ),
      });
  }
  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.isSubmitting.set(true);
    const v = this.form.getRawValue();
    const request = this.id
      ? this.service.updateCycle(this.id, v)
      : this.service.createCycle(v);
    request
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: () => {
          this.snack.open(
            this.id ? "Cycle updated." : "Cycle created.",
            "Dismiss",
            { duration: 3000 },
          );
          void this.router.navigateByUrl("/crop-cycles");
        },
        error: (e) =>
          this.errorMessage.set(
            getApiErrorMessage(e, "Cycle could not be saved."),
          ),
      });
  }
}
