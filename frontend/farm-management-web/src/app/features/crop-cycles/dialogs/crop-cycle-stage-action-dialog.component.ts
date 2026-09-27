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
import { MatDatepickerModule } from "@angular/material/datepicker";
import {
  MAT_DIALOG_DATA,
  MatDialogModule,
  MatDialogRef,
} from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatSelectModule } from "@angular/material/select";
import { formatDateOnly } from "../../../core/utils/date.utils";

export type StageActionMode = "complete" | "skip" | "reopen" | "override";

export interface CropCycleStageActionDialogData {
  actionMode: StageActionMode;
  stageId: string;
  stageName: string;
  sequenceNumber: number;
  currentStatus: string;
  actualStartDate?: string | null;
  actualEndDate?: string | null;
}

export interface CropCycleStageActionDialogResult {
  actionMode: StageActionMode;
  actualEndDate?: string | null;
  actualStartDate?: string | null;
  skipDate?: string | null;
  reason?: string;
  targetStatus?: string;
  notes?: string;
}

@Component({
  selector: "app-crop-cycle-stage-action-dialog",
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
  ],
  template: `
    <h2 mat-dialog-title>
      @switch (data.actionMode) {
        @case ('complete') { Complete Stage {{ data.sequenceNumber }}: {{ data.stageName }} }
        @case ('skip') { Skip Stage {{ data.sequenceNumber }}: {{ data.stageName }} }
        @case ('reopen') { Reopen Stage {{ data.sequenceNumber }}: {{ data.stageName }} }
        @case ('override') { Override Stage {{ data.sequenceNumber }}: {{ data.stageName }} }
      }
    </h2>

    <mat-dialog-content>
      <p class="dialog-description">
        @switch (data.actionMode) {
          @case ('complete') {
            Completing this stage will automatically mark it as <strong>COMPLETED</strong> and advance the next stage to <strong>IN_PROGRESS</strong>.
          }
          @case ('skip') {
            Skipping this stage requires a mandatory reason and will advance the next stage to <strong>IN_PROGRESS</strong>.
          }
          @case ('reopen') {
            Reopening this stage will set it back to <strong>IN_PROGRESS</strong> and reconcile any subsequent active or completed stages back to <strong>NOT_STARTED</strong>.
          }
          @case ('override') {
            Overriding this stage allows manually overriding its status and actual dates.
          }
        }
      </p>

      <form [formGroup]="form" class="action-form">
        @if (data.actionMode === 'override') {
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Target Status</mat-label>
            <mat-select formControlName="targetStatus" required>
              <mat-option value="NOT_STARTED">Not Started</mat-option>
              <mat-option value="IN_PROGRESS">In Progress</mat-option>
              <mat-option value="COMPLETED">Completed</mat-option>
              <mat-option value="SKIPPED">Skipped</mat-option>
              <mat-option value="CANCELLED">Cancelled</mat-option>
            </mat-select>
            @if (form.get('targetStatus')?.hasError('required')) {
              <mat-error>Target status is required</mat-error>
            }
          </mat-form-field>
        }

        @if (data.actionMode === 'override') {
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Actual Start Date</mat-label>
            <input
              matInput
              [matDatepicker]="startDatePicker"
              formControlName="actualStartDate"
              placeholder="Actual start date"
            />
            <mat-datepicker-toggle matIconSuffix [for]="startDatePicker" />
            <mat-datepicker #startDatePicker />
          </mat-form-field>
        }

        @if (data.actionMode === 'complete' || data.actionMode === 'override') {
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Actual Completion Date</mat-label>
            <input
              matInput
              [matDatepicker]="endDatePicker"
              formControlName="actualEndDate"
              placeholder="Actual completion date"
            />
            <mat-datepicker-toggle matIconSuffix [for]="endDatePicker" />
            <mat-datepicker #endDatePicker />
          </mat-form-field>
        }

        @if (data.actionMode === 'skip') {
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Skip Date</mat-label>
            <input
              matInput
              [matDatepicker]="skipDatePicker"
              formControlName="skipDate"
              placeholder="Skip date"
            />
            <mat-datepicker-toggle matIconSuffix [for]="skipDatePicker" />
            <mat-datepicker #skipDatePicker />
          </mat-form-field>
        }

        @if (data.actionMode === 'skip' || data.actionMode === 'reopen' || data.actionMode === 'override') {
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Reason (Mandatory)</mat-label>
            <textarea
              matInput
              rows="3"
              formControlName="reason"
              required
              placeholder="Provide justification for this action..."
            ></textarea>
            @if (form.get('reason')?.hasError('required')) {
              <mat-error>A reason is required</mat-error>
            }
          </mat-form-field>
        }

        @if (data.actionMode === 'complete') {
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Notes (Optional)</mat-label>
            <textarea
              matInput
              rows="2"
              formControlName="notes"
              placeholder="Operational remarks or notes..."
            ></textarea>
          </mat-form-field>
        }
      </form>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="onCancel()">Cancel</button>
      <button
        mat-flat-button
        [color]="getButtonColor()"
        type="button"
        [disabled]="form.invalid"
        (click)="onSubmit()"
      >
        Confirm {{ getActionButtonText() }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .dialog-description {
      margin-bottom: 1rem;
      color: #4a5568;
      font-size: 0.95rem;
      line-height: 1.5;
    }
    .action-form {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      min-width: 420px;
    }
    .full-width {
      width: 100%;
    }
    mat-dialog-actions {
      padding: 1rem 1.5rem;
      gap: 0.5rem;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CropCycleStageActionDialogComponent implements OnInit {
  readonly data: CropCycleStageActionDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(
    MatDialogRef<CropCycleStageActionDialogComponent, CropCycleStageActionDialogResult>,
  );
  private readonly fb = inject(FormBuilder);

  form!: FormGroup;

  ngOnInit(): void {
    const today = new Date();
    this.form = this.fb.group({
      actualEndDate: [this.data.actualEndDate ? new Date(this.data.actualEndDate) : today],
      actualStartDate: [this.data.actualStartDate ? new Date(this.data.actualStartDate) : null],
      skipDate: [today],
      reason: ["", this.data.actionMode !== "complete" ? [Validators.required] : []],
      targetStatus: [this.data.currentStatus || "IN_PROGRESS", this.data.actionMode === "override" ? [Validators.required] : []],
      notes: [""],
    });
  }

  getButtonColor(): "primary" | "warn" | "accent" {
    switch (this.data.actionMode) {
      case "complete":
        return "primary";
      case "skip":
        return "warn";
      case "reopen":
        return "accent";
      case "override":
        return "primary";
      default:
        return "primary";
    }
  }

  getActionButtonText(): string {
    switch (this.data.actionMode) {
      case "complete":
        return "Completion";
      case "skip":
        return "Skip";
      case "reopen":
        return "Reopen";
      case "override":
        return "Override";
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    const value = this.form.value;

    this.dialogRef.close({
      actionMode: this.data.actionMode,
      actualEndDate: value.actualEndDate ? formatDateOnly(value.actualEndDate) : null,
      actualStartDate: value.actualStartDate ? formatDateOnly(value.actualStartDate) : null,
      skipDate: value.skipDate ? formatDateOnly(value.skipDate) : null,
      reason: value.reason?.trim() || undefined,
      targetStatus: value.targetStatus || undefined,
      notes: value.notes?.trim() || undefined,
    });
  }
}
