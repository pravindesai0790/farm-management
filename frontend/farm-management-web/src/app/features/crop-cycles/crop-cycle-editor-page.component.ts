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
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { debounceTime, distinctUntilChanged, finalize } from "rxjs";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import { Plantation } from "../../core/farm-management/farm-management.models";
import { ErrorAlertComponent } from "../../shared/components/error-alert/error-alert.component";
import { formatDateOnly, parseDateOnly } from "../../core/utils/date.utils";

@Component({
  selector: "app-crop-cycle-editor-page",
  standalone: true,
  imports: [
    ErrorAlertComponent,
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
  readonly isLoadingPlantations = signal(false);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<unknown>(null);
  readonly plantations = signal<readonly Plantation[]>([]);

  private initialPlantationId: string | null = null;

  readonly form = this.fb.group({
    plantationId: [null as string | null, [Validators.required]],
    cycleCode: ["", [Validators.required]],
    cycleName: ["", [Validators.required]],
    seasonYear: [new Date().getFullYear(), [Validators.required]],
    seasonName: [""],
    plannedStartDate: [null as Date | null, [Validators.required]],
    expectedEndDate: [null as Date | null],
  });

  ngOnInit(): void {
    if (this.id) {
      this.service
        .getCycle(this.id)
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isLoading.set(false)),
        )
        .subscribe({
          next: (cycle) => {
            this.initialPlantationId = cycle.plantationId;
            this.form.patchValue({
              plantationId: cycle.plantationId,
              cycleCode: cycle.cycleCode,
              cycleName: cycle.cycleName,
              seasonYear: cycle.seasonYear,
              seasonName: cycle.seasonName ?? "",
              plannedStartDate: parseDateOnly(cycle.plannedStartDate),
              expectedEndDate: parseDateOnly(cycle.expectedEndDate),
            });
            this.loadPlantations(cycle.seasonYear, cycle.plantationId);
            this.listenToSeasonYearChanges();
          },
          error: (e) => this.errorMessage.set(e),
        });
    } else {
      this.isLoading.set(false);
      const initialYear =
        this.form.get("seasonYear")?.value ?? new Date().getFullYear();
      this.loadPlantations(initialYear);
      this.listenToSeasonYearChanges();
    }
  }

  private listenToSeasonYearChanges(): void {
    this.form
      .get("seasonYear")
      ?.valueChanges.pipe(
        takeUntilDestroyed(this.destroyRef),
        debounceTime(300),
        distinctUntilChanged(),
      )
      .subscribe((year) => {
        const parsedYear = Number(year);
        if (parsedYear && parsedYear > 1900 && parsedYear < 2200) {
          this.loadPlantations(
            parsedYear,
            this.id ? this.initialPlantationId : undefined,
          );
        }
      });
  }

  private loadPlantations(
    seasonYear: number,
    currentPlantationId?: string | null,
  ): void {
    this.isLoadingPlantations.set(true);
    this.service
      .listPlantations(
        1,
        100,
        undefined,
        undefined,
        undefined,
        undefined,
        seasonYear,
        currentPlantationId ?? undefined,
      )
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoadingPlantations.set(false)),
      )
      .subscribe({
        next: (r) => {
          this.plantations.set(r.items);
          const selected = this.form.get("plantationId")?.value;
          if (selected && !r.items.some((p) => p.id === selected)) {
            this.form.patchValue({ plantationId: null });
          }
        },
        error: (e) => this.errorMessage.set(e),
      });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.errorMessage.set(null);
    this.isSubmitting.set(true);
    const v = this.form.getRawValue();
    const payload = {
      plantationId: v.plantationId,
      cycleCode: v.cycleCode,
      cycleName: v.cycleName,
      seasonYear: v.seasonYear,
      seasonName: v.seasonName,
      plannedStartDate: formatDateOnly(v.plannedStartDate) ?? "",
      expectedEndDate: formatDateOnly(v.expectedEndDate),
    };
    const request = this.id
      ? this.service.updateCycle(this.id, payload)
      : this.service.createCycle(payload);
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
        error: (e) => this.errorMessage.set(e),
      });
  }
}
