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
import {
  LifecycleTemplate,
  Plantation,
} from "../../core/farm-management/farm-management.models";
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
  readonly isLoadingLifecycleTemplates = signal(false);
  readonly isSubmitting = signal(false);
  readonly isStarted = signal(false);
  readonly errorMessage = signal<unknown>(null);
  readonly plantations = signal<readonly Plantation[]>([]);
  readonly lifecycleTemplates = signal<readonly LifecycleTemplate[]>([]);
  readonly currentCropId = signal<string | null>(null);
  readonly assignedTemplateName = signal<string | null>(null);

  private initialPlantationId: string | null = null;
  private isEditingExistingCycle = false;

  readonly form = this.fb.group({
    plantationId: [null as string | null, [Validators.required]],
    cycleName: ["", [Validators.required]],
    seasonYear: [new Date().getFullYear(), [Validators.required]],
    seasonName: [""],
    plannedStartDate: [null as Date | null, [Validators.required]],
    expectedEndDate: [null as Date | null],
    lifecycleTemplateId: [null as string | null],
  });

  ngOnInit(): void {
    if (this.id) {
      this.isEditingExistingCycle = true;
      this.service
        .getCycle(this.id)
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isLoading.set(false)),
        )
        .subscribe({
          next: (cycle) => {
            this.initialPlantationId = cycle.plantationId;
            const started = cycle.status !== "PLANNED";
            this.isStarted.set(started);
            if (started) {
              this.form.get("lifecycleTemplateId")?.disable();
            }

            this.assignedTemplateName.set(cycle.lifecycleTemplateName ?? null);
            this.form.patchValue({
              plantationId: cycle.plantationId,
              cycleName: cycle.cycleName,
              seasonYear: cycle.seasonYear,
              seasonName: cycle.seasonName ?? "",
              plannedStartDate: parseDateOnly(cycle.plannedStartDate),
              expectedEndDate: parseDateOnly(cycle.expectedEndDate),
              lifecycleTemplateId: cycle.lifecycleTemplateId ?? null,
            });

            this.loadPlantations(cycle.seasonYear, cycle.plantationId);
            if (cycle.cropId) {
              this.currentCropId.set(cycle.cropId);
              this.loadLifecycleTemplates(cycle.cropId, cycle.lifecycleTemplateId);
            }
            this.listenToFormChanges();
          },
          error: (e) => this.errorMessage.set(e),
        });
    } else {
      this.isLoading.set(false);
      const initialYear =
        this.form.get("seasonYear")?.value ?? new Date().getFullYear();
      this.loadPlantations(initialYear);
      this.listenToFormChanges();
    }
  }

  private listenToFormChanges(): void {
    this.listenToSeasonYearChanges();
    this.listenToPlantationChanges();
    this.listenToTemplateChanges();
    this.listenToPlannedStartDateChanges();
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

  private listenToPlantationChanges(): void {
    this.form
      .get("plantationId")
      ?.valueChanges.pipe(
        takeUntilDestroyed(this.destroyRef),
        distinctUntilChanged(),
      )
      .subscribe((plantationId) => {
        if (!plantationId) {
          this.currentCropId.set(null);
          this.lifecycleTemplates.set([]);
          if (!this.isEditingExistingCycle) {
            this.form.patchValue({ lifecycleTemplateId: null });
          }
          return;
        }

        const selectedPlantation = this.plantations().find(
          (p) => p.id === plantationId,
        );

        if (selectedPlantation && selectedPlantation.cropId) {
          const cropIdChanged = selectedPlantation.cropId !== this.currentCropId();
          this.currentCropId.set(selectedPlantation.cropId);
          if (cropIdChanged || this.lifecycleTemplates().length === 0) {
            const currentSelectedTplId = this.form.get("lifecycleTemplateId")?.value;
            this.loadLifecycleTemplates(selectedPlantation.cropId, currentSelectedTplId);
          }
        }
      });
  }

  private listenToTemplateChanges(): void {
    this.form
      .get("lifecycleTemplateId")
      ?.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((templateId) => {
        if (!templateId) {
          return;
        }
        const template = this.lifecycleTemplates().find((t) => t.id === templateId);
        if (template) {
          this.updateExpectedEndDateFromTemplate(template);
        }
      });
  }

  private listenToPlannedStartDateChanges(): void {
    this.form
      .get("plannedStartDate")
      ?.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        const templateId = this.form.get("lifecycleTemplateId")?.value;
        if (templateId) {
          const template = this.lifecycleTemplates().find((t) => t.id === templateId);
          if (template) {
            this.updateExpectedEndDateFromTemplate(template);
          }
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
          if (selected) {
            const p = r.items.find((item) => item.id === selected);
            if (p && p.cropId) {
              if (p.cropId !== this.currentCropId()) {
                this.currentCropId.set(p.cropId);
                const tplId = this.form.get("lifecycleTemplateId")?.value;
                this.loadLifecycleTemplates(p.cropId, tplId);
              }
            } else if (!p) {
              this.form.patchValue({ plantationId: null });
            }
          }
        },
        error: (e) => this.errorMessage.set(e),
      });
  }

  private loadLifecycleTemplates(
    cropId: string,
    preselectedTemplateId?: string | null,
  ): void {
    this.isLoadingLifecycleTemplates.set(true);
    this.service
      .listLifecycleTemplates(cropId, 1, 100)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoadingLifecycleTemplates.set(false)),
      )
      .subscribe({
        next: (res) => {
          this.lifecycleTemplates.set(res.items);
          if (preselectedTemplateId) {
            this.form.patchValue({ lifecycleTemplateId: preselectedTemplateId });
          } else if (!this.id) {
            // New cycle creation: automatically select active default template when available
            const defaultTemplate = res.items.find(
              (t) => t.isActive && t.isDefault,
            );
            if (defaultTemplate) {
              this.form.patchValue({ lifecycleTemplateId: defaultTemplate.id });
              this.updateExpectedEndDateFromTemplate(defaultTemplate);
            } else {
              this.form.patchValue({ lifecycleTemplateId: null });
            }
          }
        },
        error: (e) => this.errorMessage.set(e),
      });
  }

  private updateExpectedEndDateFromTemplate(template: LifecycleTemplate): void {
    const startDate = this.form.get("plannedStartDate")?.value;
    if (!startDate) {
      return;
    }
    const durationDays = this.getTemplateDuration(template);
    if (durationDays !== null && durationDays > 0) {
      const calculatedEnd = new Date(startDate.getTime());
      calculatedEnd.setDate(calculatedEnd.getDate() + durationDays);
      this.form.patchValue({ expectedEndDate: calculatedEnd });
    }
  }

  private getTemplateDuration(
    template: LifecycleTemplate | null | undefined,
  ): number | null {
    if (!template || !template.stages || template.stages.length === 0) {
      return null;
    }
    const activeStages = template.stages.filter((s) => s.isActive);
    if (activeStages.length === 0) {
      return null;
    }
    const hasUnknownDuration = activeStages.some(
      (s) => s.expectedDurationDays == null || s.expectedDurationDays <= 0,
    );
    if (hasUnknownDuration) {
      return null;
    }
    return activeStages.reduce(
      (sum, s) => sum + (s.expectedDurationDays ?? 0),
      0,
    );
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
      cycleName: v.cycleName,
      seasonYear: v.seasonYear,
      seasonName: v.seasonName,
      plannedStartDate: formatDateOnly(v.plannedStartDate) ?? "",
      expectedEndDate: formatDateOnly(v.expectedEndDate),
      lifecycleTemplateId: v.lifecycleTemplateId || null,
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
