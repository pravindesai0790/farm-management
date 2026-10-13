import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from "@angular/core";
import {
  FormBuilder,
  ReactiveFormsModule,
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

import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import { IrrigationService } from "../../../../core/irrigations/irrigation.service";
import { DateTimePickerComponent } from "../../../../shared/components/date-time-picker/date-time-picker.component";

export interface StartIrrigationDialogData {
  readonly irrigationId: string;
  readonly farmName: string;
  readonly farmAreaName: string;
  readonly concurrencyToken?: string | null;
}

@Component({
  selector: "app-start-irrigation-dialog",
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
  templateUrl: "./start-irrigation-dialog.component.html",
  styleUrl: "./start-irrigation-dialog.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StartIrrigationDialogComponent {
  readonly data: StartIrrigationDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<StartIrrigationDialogComponent>);
  private readonly irrigationService = inject(IrrigationService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);

  readonly isSubmitting = signal(false);

  readonly form = this.fb.group({
    actualStartedAt: [null as string | null],
  });

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onSubmit(): void {
    const rawValue = this.form.value.actualStartedAt?.trim();
    const isoDateTime = rawValue ? new Date(rawValue).toISOString() : null;

    this.isSubmitting.set(true);

    this.irrigationService
      .start(this.data.irrigationId, {
        actualStartedAt: isoDateTime,
        concurrencyToken: this.data.concurrencyToken,
      })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.snack.open("Irrigation event started successfully.", "Dismiss", {
            duration: 3500,
          });
          this.dialogRef.close(true);
        },
        error: (err) => {
          this.snack.open(getApiErrorMessage(err, "Failed to start irrigation event."), "Dismiss", {
            duration: 5000,
          });
        },
      });
  }
}
