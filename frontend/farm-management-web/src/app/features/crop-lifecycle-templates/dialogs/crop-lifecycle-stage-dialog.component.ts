import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
} from "@angular/core";
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCheckboxModule } from "@angular/material/checkbox";
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { CropLifecycleStage } from "../../../core/crop-lifecycle-templates/crop-lifecycle-template.models";

export interface CropLifecycleStageDialogData {
  stage?: Partial<CropLifecycleStage>;
  nextSequenceNumber?: number;
  isEdit?: boolean;
}

export interface CropLifecycleStageDialogResult {
  stageName: string;
  sequenceNumber: number;
  expectedDurationDays: number | null;
  description: string | null;
  isActive: boolean;
}

@Component({
  selector: "app-crop-lifecycle-stage-dialog",
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatIconModule,
  ],
  template: `
    <h2 mat-dialog-title>
      <mat-icon color="primary">{{ data.isEdit ? 'edit' : 'add' }}</mat-icon>
      {{ data.isEdit ? 'Edit Stage' : 'Add Stage' }}
    </h2>

    <mat-dialog-content>
      <form [formGroup]="form" class="stage-form">
        <div class="form-row">
          <mat-form-field appearance="outline" class="flex-2">
            <mat-label>Stage Name</mat-label>
            <input
              matInput
              formControlName="stageName"
              placeholder="e.g. Dormancy, Bud Break, Flowering"
              maxLength="150"
              required
            />
            @if (form.get('stageName')?.hasError('required')) {
              <mat-error>Stage name is required.</mat-error>
            }
            @if (form.get('stageName')?.hasError('maxlength')) {
              <mat-error>Stage name cannot exceed 150 characters.</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline" class="flex-1">
            <mat-label>Sequence #</mat-label>
            <input
              matInput
              type="number"
              formControlName="sequenceNumber"
              min="1"
              required
            />
            @if (form.get('sequenceNumber')?.hasError('required')) {
              <mat-error>Sequence is required.</mat-error>
            }
            @if (form.get('sequenceNumber')?.hasError('min')) {
              <mat-error>Sequence must be at least 1.</mat-error>
            }
          </mat-form-field>
        </div>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Expected Duration (Days)</mat-label>
          <input
            matInput
            type="number"
            formControlName="expectedDurationDays"
            placeholder="e.g. 15, 30"
            min="1"
          />
          <mat-hint>Estimated calendar days expected for this growing phase.</mat-hint>
          @if (form.get('expectedDurationDays')?.hasError('min')) {
            <mat-error>Duration must be at least 1 day.</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Description</mat-label>
          <textarea
            matInput
            rows="3"
            formControlName="description"
            placeholder="Agronomic guidance, key visual indicators, or field tasks for this stage..."
            maxLength="2000"
          ></textarea>
        </mat-form-field>

        @if (data.isEdit) {
          <div class="checkbox-row">
            <mat-checkbox formControlName="isActive" color="primary">
              Active stage
            </mat-checkbox>
          </div>
        }
      </form>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="onCancel()">Cancel</button>
      <button
        mat-flat-button
        color="primary"
        type="button"
        [disabled]="form.invalid"
        (click)="onSubmit()"
      >
        {{ data.isEdit ? 'Save Changes' : 'Add Stage' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    h2[mat-dialog-title] {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin: 0;
      padding: 1.25rem 1.5rem;
    }
    mat-dialog-content {
      min-width: 440px;
      max-width: 520px;
      padding: 0.5rem 1.5rem 1rem 1.5rem !important;
    }
    .stage-form {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }
    .form-row {
      display: flex;
      gap: 1rem;
      align-items: flex-start;
    }
    .flex-1 {
      flex: 1;
    }
    .flex-2 {
      flex: 2;
    }
    .full-width {
      width: 100%;
    }
    .checkbox-row {
      margin-top: 0.25rem;
    }
    mat-dialog-actions {
      padding: 1rem 1.5rem;
      gap: 0.5rem;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CropLifecycleStageDialogComponent implements OnInit {
  readonly data: CropLifecycleStageDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(
    MatDialogRef<CropLifecycleStageDialogComponent, CropLifecycleStageDialogResult>,
  );
  private readonly fb = inject(FormBuilder);

  readonly form: FormGroup = this.fb.group({
    stageName: ["", [Validators.required, Validators.maxLength(150)]],
    sequenceNumber: [1, [Validators.required, Validators.min(1)]],
    expectedDurationDays: [null as number | null, [Validators.min(1)]],
    description: ["", [Validators.maxLength(2000)]],
    isActive: [true],
  });

  ngOnInit(): void {
    if (this.data.stage) {
      this.form.patchValue({
        stageName: this.data.stage.stageName ?? "",
        sequenceNumber: this.data.stage.sequenceNumber ?? this.data.nextSequenceNumber ?? 1,
        expectedDurationDays: this.data.stage.expectedDurationDays ?? null,
        description: this.data.stage.description ?? "",
        isActive: this.data.stage.isActive ?? true,
      });
    } else if (this.data.nextSequenceNumber) {
      this.form.patchValue({ sequenceNumber: this.data.nextSequenceNumber });
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const val = this.form.value;
    this.dialogRef.close({
      stageName: val.stageName.trim(),
      sequenceNumber: Number(val.sequenceNumber),
      expectedDurationDays: val.expectedDurationDays ? Number(val.expectedDurationDays) : null,
      description: val.description?.trim() || null,
      isActive: Boolean(val.isActive),
    });
  }
}
