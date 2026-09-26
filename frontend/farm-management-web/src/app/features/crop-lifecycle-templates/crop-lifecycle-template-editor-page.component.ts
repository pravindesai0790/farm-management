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
import { MatCheckboxModule } from "@angular/material/checkbox";
import { MatDialog } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { finalize, of } from "rxjs";
import { BreadcrumbService } from "../../core/breadcrumb/breadcrumb.service";
import {
  CreateCropLifecycleStageRequest,
  CropLifecycleStage,
  CropLifecycleTemplate,
} from "../../core/crop-lifecycle-templates/crop-lifecycle-template.models";
import { CropLifecycleTemplateService } from "../../core/crop-lifecycle-templates/crop-lifecycle-template.service";
import { Crop } from "../../core/farm-management/farm-management.models";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import { ErrorAlertComponent } from "../../shared/components/error-alert/error-alert.component";
import {
  CropLifecycleStageEditorComponent,
  StageDraft,
} from "./components/crop-lifecycle-stage-editor.component";
import {
  CropLifecycleStageDialogComponent,
  CropLifecycleStageDialogResult,
} from "./dialogs/crop-lifecycle-stage-dialog.component";

@Component({
  selector: "app-crop-lifecycle-template-editor-page",
  standalone: true,
  imports: [
    ErrorAlertComponent,
    CropLifecycleStageEditorComponent,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: "./crop-lifecycle-template-editor-page.component.html",
  styleUrl: "./crop-lifecycle-template-editor-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CropLifecycleTemplateEditorPageComponent implements OnInit {
  private readonly templateService = inject(CropLifecycleTemplateService);
  private readonly farmService = inject(FarmManagementService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);
  private readonly breadcrumbService = inject(BreadcrumbService);
  private readonly dialog = inject(MatDialog);

  readonly id = this.route.snapshot.paramMap.get("id");
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<unknown>(null);

  readonly crops = signal<readonly Crop[]>([]);
  readonly stagesDraft = signal<readonly StageDraft[]>([]);

  readonly form = this.fb.nonNullable.group({
    cropId: ["", [Validators.required]],
    name: ["", [Validators.required, Validators.maxLength(150)]],
    description: ["", [Validators.maxLength(2000)]],
    isDefault: [false],
  });

  ngOnInit(): void {
    this.loadCrops();

    if (this.id) {
      this.templateService
        .get(this.id)
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isLoading.set(false)),
        )
        .subscribe({
          next: (t) => {
            this.form.patchValue({
              cropId: t.cropId,
              name: t.name,
              description: t.description ?? "",
              isDefault: t.isDefault,
            });
            this.stagesDraft.set(t.stages || []);
            this.breadcrumbService.setEntityName(t.id, t.name);
            this.breadcrumbService.setTrail([
              { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
              { label: "Lifecycle templates", route: "/crop-lifecycle-templates" },
              { label: t.name, route: `/crop-lifecycle-templates/${t.id}` },
              { label: "Edit" },
            ]);
          },
          error: (e) => this.errorMessage.set(e),
        });
    } else {
      this.isLoading.set(false);
      this.breadcrumbService.setTrail([
        { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
        { label: "Lifecycle templates", route: "/crop-lifecycle-templates" },
        { label: "New template" },
      ]);
    }
  }

  loadCrops(): void {
    this.farmService
      .listCrops(1, 100, "", true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => this.crops.set(res.items),
        error: () => {},
      });
  }

  onAddStage(): void {
    const current = this.stagesDraft();
    const nextSeq = current.length + 1;

    const dialogRef = this.dialog.open(CropLifecycleStageDialogComponent, {
      data: { isEdit: false, nextSequenceNumber: nextSeq },
    });

    dialogRef
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((res: CropLifecycleStageDialogResult | undefined) => {
        if (!res) return;
        const newStage: StageDraft = {
          stageName: res.stageName,
          sequenceNumber: res.sequenceNumber,
          expectedDurationDays: res.expectedDurationDays,
          description: res.description,
          isActive: true,
        };
        const updated = [...current, newStage].map((s, idx) => ({
          ...s,
          sequenceNumber: idx + 1,
        }));
        this.stagesDraft.set(updated);
      });
  }

  onEditStage(event: { stage: StageDraft; index: number }): void {
    const dialogRef = this.dialog.open(CropLifecycleStageDialogComponent, {
      data: { isEdit: true, stage: event.stage },
    });

    dialogRef
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((res: CropLifecycleStageDialogResult | undefined) => {
        if (!res) return;
        const current = [...this.stagesDraft()];
        current[event.index] = {
          ...current[event.index],
          stageName: res.stageName,
          sequenceNumber: res.sequenceNumber,
          expectedDurationDays: res.expectedDurationDays,
          description: res.description,
          isActive: res.isActive,
        };
        const updated = current.map((s, idx) => ({
          ...s,
          sequenceNumber: idx + 1,
        }));
        this.stagesDraft.set(updated);
      });
  }

  onRemoveStage(index: number): void {
    const current = [...this.stagesDraft()];
    current.splice(index, 1);
    const updated = current.map((s, idx) => ({
      ...s,
      sequenceNumber: idx + 1,
    }));
    this.stagesDraft.set(updated);
  }

  onReorderStages(updated: readonly StageDraft[]): void {
    this.stagesDraft.set(updated);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    this.isSubmitting.set(true);

    const val = this.form.getRawValue();

    if (this.id) {
      this.templateService
        .update(this.id, {
          cropId: val.cropId,
          name: val.name,
          description: val.description || null,
          isDefault: val.isDefault,
        })
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isSubmitting.set(false)),
        )
        .subscribe({
          next: () => {
            this.snack.open("Lifecycle template updated.", "Dismiss", {
              duration: 3000,
            });
            void this.router.navigateByUrl(`/crop-lifecycle-templates/${this.id}`);
          },
          error: (e) => this.errorMessage.set(e),
        });
    } else {
      const stagesPayload: CreateCropLifecycleStageRequest[] = this.stagesDraft().map(
        (s, idx) => ({
          stageName: s.stageName,
          sequenceNumber: idx + 1,
          expectedDurationDays: s.expectedDurationDays,
          description: s.description,
        }),
      );

      this.templateService
        .create({
          cropId: val.cropId,
          name: val.name,
          description: val.description || null,
          isDefault: val.isDefault,
          stages: stagesPayload.length > 0 ? stagesPayload : undefined,
        })
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isSubmitting.set(false)),
        )
        .subscribe({
          next: (created) => {
            this.snack.open("Lifecycle template created.", "Dismiss", {
              duration: 3000,
            });
            void this.router.navigateByUrl(`/crop-lifecycle-templates/${created.id}`);
          },
          error: (e) => this.errorMessage.set(e),
        });
    }
  }
}
