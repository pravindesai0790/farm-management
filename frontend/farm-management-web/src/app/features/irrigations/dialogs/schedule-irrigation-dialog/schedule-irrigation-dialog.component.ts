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

import { DatePipe } from "@angular/common";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import { IrrigationService } from "../../../../core/irrigations/irrigation.service";
import { DateTimePickerComponent } from "../../../../shared/components/date-time-picker/date-time-picker.component";

export interface ScheduleIrrigationDialogData {
  readonly irrigationId: string;
  readonly farmName: string;
  readonly farmAreaName: string;
  readonly currentScheduledAt?: string | null;
  readonly isReschedule?: boolean;
  readonly concurrencyToken?: string | null;
}

@Component({
  selector: "app-schedule-irrigation-dialog",
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatProgressSpinnerModule,
    DateTimePickerComponent,
  ],
  templateUrl: "./schedule-irrigation-dialog.component.html",
  styleUrl: "./schedule-irrigation-dialog.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScheduleIrrigationDialogComponent {
  readonly data: ScheduleIrrigationDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<ScheduleIrrigationDialogComponent>);
  private readonly irrigationService = inject(IrrigationService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);

  readonly isSubmitting = signal(false);

  readonly form = this.fb.group({
    scheduledAt: [
      this.formatInitialDateTime(this.data.currentScheduledAt),
      [Validators.required],
    ],
  });

  private formatInitialDateTime(isoString?: string | null): string {
    if (isoString) {
      const d = new Date(isoString);
      if (!isNaN(d.getTime())) {
        return d.toISOString();
      }
    }
    const nextHour = new Date();
    nextHour.setHours(nextHour.getHours() + 1, 0, 0, 0);
    return nextHour.toISOString();
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    const rawValue = this.form.value.scheduledAt;
    if (!rawValue) return;

    const isoDateTime = new Date(rawValue).toISOString();
    this.isSubmitting.set(true);

    const call$ = this.data.isReschedule
      ? this.irrigationService.reschedule(this.data.irrigationId, {
          scheduledAt: isoDateTime,
          concurrencyToken: this.data.concurrencyToken,
        })
      : this.irrigationService.schedule(this.data.irrigationId, {
          scheduledAt: isoDateTime,
          concurrencyToken: this.data.concurrencyToken,
        });

    call$
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          const actionText = this.data.isReschedule ? "rescheduled" : "scheduled";
          this.snack.open(`Irrigation event ${actionText} successfully.`, "Dismiss", {
            duration: 3500,
          });
          this.dialogRef.close(true);
        },
        error: (err) => {
          const actionText = this.data.isReschedule ? "reschedule" : "schedule";
          this.snack.open(getApiErrorMessage(err, `Failed to ${actionText} irrigation event.`), "Dismiss", {
            duration: 5000,
          });
        },
      });
  }
}
