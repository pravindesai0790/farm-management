import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from "@angular/core";
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
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
import { finalize } from "rxjs";
import { SprayService } from "../../../../core/sprays/spray.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";

import { DateTimePickerComponent } from "../../../../shared/components/date-time-picker/date-time-picker.component";

export interface ScheduleSprayDialogData {
  readonly sprayId: string;
  readonly referenceNumber: string;
  readonly farmName: string;
  readonly currentScheduledDateTime?: string | null;
  readonly isReschedule?: boolean;
}

@Component({
  selector: "app-schedule-spray-dialog",
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatProgressSpinnerModule,
    DateTimePickerComponent,
  ],
  templateUrl: "./schedule-spray-dialog.component.html",
  styleUrl: "./schedule-spray-dialog.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScheduleSprayDialogComponent {
  readonly data: ScheduleSprayDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<ScheduleSprayDialogComponent>);
  private readonly sprayService = inject(SprayService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);

  readonly isSubmitting = signal(false);

  readonly form = this.fb.group({
    scheduledDateTime: [
      this.formatInitialDateTime(this.data.currentScheduledDateTime),
      [Validators.required],
    ],
  });

  private formatInitialDateTime(isoString?: string | null): string {
    if (isoString) {
      const d = new Date(isoString);
      if (!isNaN(d.getTime())) {
        return d.toISOString().slice(0, 16);
      }
    }
    const now = new Date();
    now.setHours(now.getHours() + 1, 0, 0, 0);
    return now.toISOString().slice(0, 16);
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    const rawValue = this.form.value.scheduledDateTime;
    if (!rawValue) return;

    const isoDateTime = new Date(rawValue).toISOString();

    this.isSubmitting.set(true);

    const call$ = this.data.isReschedule
      ? this.sprayService.rescheduleSpray(this.data.sprayId, { scheduledDateTime: isoDateTime })
      : this.sprayService.scheduleSpray(this.data.sprayId, { scheduledDateTime: isoDateTime });

    call$
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          const actionWord = this.data.isReschedule ? "rescheduled" : "scheduled";
          this.snack.open(`Spray ${this.data.referenceNumber} has been ${actionWord}.`, "Dismiss", {
            duration: 4000,
          });
          this.dialogRef.close(true);
        },
        error: (err) => {
          this.snack.open(getApiErrorMessage(err, "Failed to schedule spray."), "Dismiss", {
            duration: 5000,
          });
        },
      });
  }
}
