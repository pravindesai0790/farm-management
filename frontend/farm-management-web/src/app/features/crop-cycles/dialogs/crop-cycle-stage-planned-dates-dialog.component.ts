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
import { DatePipe } from "@angular/common";
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
import { MatSnackBar } from "@angular/material/snack-bar";
import { FarmManagementService } from "../../../core/farm-management/farm-management.service";
import {
  getApiErrorMessage,
  getApiValidationErrors,
} from "../../../core/models/api-error.model";
import { formatDateOnly, parseDateOnly } from "../../../core/utils/date.utils";
import { ErrorAlertComponent } from "../../../shared/components/error-alert/error-alert.component";

import { provideNativeDateAdapter } from "@angular/material/core";

export interface CropCycleStagePlannedDatesDialogData {
  stageId: string;
  stageName: string;
  sequenceNumber: number;
  currentStatus: string;
  plannedStartDate?: string | null;
  plannedEndDate?: string | null;
  actualStartDate?: string | null;
  actualEndDate?: string | null;
  expectedDurationDays?: number | null;
  prevStageName?: string | null;
  prevStagePlannedStartDate?: string | null;
  nextStageName?: string | null;
  nextStagePlannedEndDate?: string | null;
}

export interface CropCycleStagePlannedDatesDialogResult {
  success: boolean;
}

@Component({
  selector: "app-crop-cycle-stage-planned-dates-dialog",
  standalone: true,
  imports: [
    ReactiveFormsModule,
    DatePipe,
    MatDialogModule,
    MatButtonModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    ErrorAlertComponent,
  ],
  providers: [provideNativeDateAdapter()],
  templateUrl: "./crop-cycle-stage-planned-dates-dialog.component.html",
  styleUrl: "./crop-cycle-stage-planned-dates-dialog.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CropCycleStagePlannedDatesDialogComponent implements OnInit {
  readonly data: CropCycleStagePlannedDatesDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(
    MatDialogRef<CropCycleStagePlannedDatesDialogComponent, CropCycleStagePlannedDatesDialogResult>,
  );
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(FarmManagementService);
  private readonly snack = inject(MatSnackBar);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly validationErrors = signal<Readonly<Record<string, readonly string[]>>>({});

  form!: FormGroup;

  ngOnInit(): void {
    const existingStart = this.data.plannedStartDate ? parseDateOnly(this.data.plannedStartDate) : null;
    const existingEnd = this.data.plannedEndDate ? parseDateOnly(this.data.plannedEndDate) : null;

    this.form = this.fb.group(
      {
        plannedStartDate: [existingStart, [Validators.required]],
        plannedEndDate: [existingEnd],
      },
      { validators: [this.plannedDatesValidator()] },
    );
  }

  private plannedDatesValidator(): ValidatorFn {
    return (group: AbstractControl): ValidationErrors | null => {
      const startVal = group.get("plannedStartDate")?.value;
      const endVal = group.get("plannedEndDate")?.value;

      if (!startVal) return null;

      const startDate = startVal instanceof Date ? startVal : parseDateOnly(startVal);
      const endDate = endVal ? (endVal instanceof Date ? endVal : parseDateOnly(endVal)) : null;

      if (startDate && endDate && endDate < startDate) {
        return { beforeStartDate: true };
      }

      if (this.data.prevStagePlannedStartDate && startDate) {
        const prevStart = parseDateOnly(this.data.prevStagePlannedStartDate);
        if (prevStart && startDate < prevStart) {
          return { beforePrevStageStart: true };
        }
      }

      if (this.data.nextStagePlannedEndDate && endDate) {
        const nextEnd = parseDateOnly(this.data.nextStagePlannedEndDate);
        if (nextEnd && endDate > nextEnd) {
          return { afterNextStageEnd: true };
        }
      }

      return null;
    };
  }

  get calculatedDurationDays(): number | null {
    const startVal = this.form?.get("plannedStartDate")?.value;
    const endVal = this.form?.get("plannedEndDate")?.value;
    if (!startVal || !endVal) return null;
    const startDate = startVal instanceof Date ? startVal : parseDateOnly(startVal);
    const endDate = endVal instanceof Date ? endVal : parseDateOnly(endVal);
    if (!startDate || !endDate || endDate < startDate) return null;

    const diffTime = endDate.getTime() - startDate.getTime();
    return Math.round(diffTime / (1000 * 60 * 60 * 24));
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

    this.service
      .updateCycleStagePlannedDates(this.data.stageId, {
        plannedStartDate: val.plannedStartDate ? formatDateOnly(val.plannedStartDate) : null,
        plannedEndDate: val.plannedEndDate ? formatDateOnly(val.plannedEndDate) : null,
      })
      .subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.snack.open(
            `Planned dates for Stage ${this.data.sequenceNumber} ("${this.data.stageName}") updated successfully.`,
            "Dismiss",
            { duration: 3500 },
          );
          this.dialogRef.close({ success: true });
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.errorMessage.set(
            getApiErrorMessage(err, "Planned dates could not be updated."),
          );
          this.validationErrors.set(getApiValidationErrors(err));
        },
      });
  }
}
