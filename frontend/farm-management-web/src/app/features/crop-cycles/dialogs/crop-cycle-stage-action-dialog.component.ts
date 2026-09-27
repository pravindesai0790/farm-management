import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from "@angular/core";
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
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
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { FarmManagementService } from "../../../core/farm-management/farm-management.service";
import {
  getApiErrorMessage,
  getApiValidationErrors,
} from "../../../core/models/api-error.model";
import { formatDateOnly, parseDateOnly } from "../../../core/utils/date.utils";
import { ErrorAlertComponent } from "../../../shared/components/error-alert/error-alert.component";

export type StageActionMode = "complete" | "skip" | "reopen" | "override";

export interface CropCycleStageActionDialogData {
  actionMode: StageActionMode;
  stageId: string;
  stageName: string;
  sequenceNumber: number;
  currentStatus: string;
  actualStartDate?: string | null;
  actualEndDate?: string | null;
  nextStageName?: string | null;
  nextStageSequence?: number | null;
}

export interface CropCycleStageActionDialogResult {
  success: boolean;
  actionMode: StageActionMode;
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
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    ErrorAlertComponent,
  ],
  templateUrl: "./crop-cycle-stage-action-dialog.component.html",
  styleUrl: "./crop-cycle-stage-action-dialog.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CropCycleStageActionDialogComponent implements OnInit {
  readonly data: CropCycleStageActionDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(
    MatDialogRef<CropCycleStageActionDialogComponent, CropCycleStageActionDialogResult>,
  );
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(FarmManagementService);
  private readonly snack = inject(MatSnackBar);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly validationErrors = signal<Readonly<Record<string, readonly string[]>>>({});

  form!: FormGroup;

  ngOnInit(): void {
    const today = new Date();
    const existingStart = this.data.actualStartDate ? parseDateOnly(this.data.actualStartDate) : null;
    const existingEnd = this.data.actualEndDate ? parseDateOnly(this.data.actualEndDate) : null;

    this.form = this.fb.group(
      {
        actualEndDate: [
          existingEnd ?? today,
          this.data.actionMode === "complete" ? [Validators.required] : [],
        ],
        actualStartDate: [existingStart],
        skipDate: [today],
        reason: [
          "",
          this.data.actionMode !== "complete"
            ? [Validators.required, Validators.minLength(3), Validators.maxLength(500)]
            : [],
        ],
        targetStatus: [
          this.data.currentStatus || "IN_PROGRESS",
          this.data.actionMode === "override" ? [Validators.required] : [],
        ],
        notes: ["", [Validators.maxLength(1000)]],
      },
      { validators: [this.dateRangeValidator()] },
    );

    if (this.data.actionMode === "override") {
      this.form.get("targetStatus")?.valueChanges.subscribe((status) => {
        const endDateControl = this.form.get("actualEndDate");
        if (status === "COMPLETED") {
          endDateControl?.setValidators([Validators.required]);
        } else {
          endDateControl?.clearValidators();
        }
        endDateControl?.updateValueAndValidity();
      });
    }
  }

  private dateRangeValidator(): ValidatorFn {
    return (group: AbstractControl): ValidationErrors | null => {
      const startVal = group.get("actualStartDate")?.value;
      const endVal = group.get("actualEndDate")?.value;
      if (startVal && endVal) {
        const startDate = startVal instanceof Date ? startVal : parseDateOnly(startVal);
        const endDate = endVal instanceof Date ? endVal : parseDateOnly(endVal);
        if (startDate && endDate && endDate < startDate) {
          return { beforeStartDate: true };
        }
      }
      return null;
    };
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
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.validationErrors.set({});

    const val = this.form.value;
    let req;

    switch (this.data.actionMode) {
      case "complete":
        req = this.service.completeCycleStage(this.data.stageId, {
          actualEndDate: val.actualEndDate ? formatDateOnly(val.actualEndDate) : null,
          notes: val.notes?.trim() || null,
        });
        break;
      case "skip":
        req = this.service.skipCycleStage(this.data.stageId, {
          reason: val.reason.trim(),
          skipDate: val.skipDate ? formatDateOnly(val.skipDate) : null,
        });
        break;
      case "reopen":
        req = this.service.reopenCycleStage(this.data.stageId, {
          reason: val.reason.trim(),
        });
        break;
      case "override":
        req = this.service.overrideCycleStage(this.data.stageId, {
          targetStatus: val.targetStatus,
          actualStartDate: val.actualStartDate ? formatDateOnly(val.actualStartDate) : null,
          actualEndDate: val.actualEndDate ? formatDateOnly(val.actualEndDate) : null,
          reason: val.reason.trim(),
        });
        break;
    }

    req.subscribe({
      next: () => {
        this.isSubmitting.set(false);
        const actionLabel =
          this.data.actionMode === "complete"
            ? "completed"
            : this.data.actionMode === "skip"
              ? "skipped"
              : this.data.actionMode === "reopen"
                ? "reopened"
                : "overridden";

        this.snack.open(
          `Stage "${this.data.stageName}" ${actionLabel} successfully.`,
          "Dismiss",
          { duration: 3500 },
        );
        this.dialogRef.close({ success: true, actionMode: this.data.actionMode });
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(
          getApiErrorMessage(err, "Stage action could not be performed."),
        );
        this.validationErrors.set(getApiValidationErrors(err));
      },
    });
  }
}
