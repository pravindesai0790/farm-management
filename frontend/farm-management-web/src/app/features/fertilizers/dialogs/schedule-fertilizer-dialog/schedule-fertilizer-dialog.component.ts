import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import {
  MAT_DIALOG_DATA,
  MatDialogModule,
  MatDialogRef,
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';
import {
  FertilizerApplicationDetailsResponse,
  FertilizerApplicationStatus,
} from '../../../../core/fertilizers/fertilizer.models';
import { FertilizerService } from '../../../../core/fertilizers/fertilizer.service';
import { getApiErrorMessage } from '../../../../core/models/api-error.model';
import { formatDateOnly, parseDateOnly } from '../../../../core/utils/date.utils';

export interface ScheduleFertilizerDialogData {
  readonly applicationId: string;
  readonly referenceNumber: string;
  readonly farmName?: string | null;
  readonly status: FertilizerApplicationStatus;
  readonly currentScheduledDate?: string | null;
  readonly isReschedule?: boolean;
  readonly version: number;
}

@Component({
  selector: 'app-schedule-fertilizer-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatDatepickerModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './schedule-fertilizer-dialog.component.html',
  styleUrl: './schedule-fertilizer-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScheduleFertilizerDialogComponent {
  readonly data: ScheduleFertilizerDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<ScheduleFertilizerDialogComponent>);
  private readonly fertilizerService = inject(FertilizerService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  // Min date is today
  readonly minDate: Date = new Date();

  readonly form = this.fb.group({
    scheduledDate: [
      this.data.currentScheduledDate ? parseDateOnly(this.data.currentScheduledDate) : new Date(),
      [Validators.required],
    ],
  });

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const dateVal = this.form.value.scheduledDate;
    const formattedDate = formatDateOnly(dateVal);
    if (!formattedDate) {
      this.errorMessage.set('Please select a valid scheduled date.');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const call$ = this.data.isReschedule
      ? this.fertilizerService.reschedule(this.data.applicationId, {
          scheduledDate: formattedDate,
          version: this.data.version,
        })
      : this.fertilizerService.schedule(this.data.applicationId, {
          scheduledDate: formattedDate,
          version: this.data.version,
        });

    call$
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: (result: FertilizerApplicationDetailsResponse) => {
          const actionText = this.data.isReschedule ? 'rescheduled' : 'scheduled';
          this.snack.open(
            `Fertilizer application '${this.data.referenceNumber}' ${actionText} for ${formattedDate}.`,
            'Dismiss',
            { duration: 4000 },
          );
          this.dialogRef.close(result);
        },
        error: (err) => {
          const defaultMsg = this.data.isReschedule
            ? 'Failed to reschedule application.'
            : 'Failed to schedule application.';
          const msg = getApiErrorMessage(err, defaultMsg);
          this.errorMessage.set(msg);
        },
      });
  }

  onCancel(): void {
    this.dialogRef.close(null);
  }
}
