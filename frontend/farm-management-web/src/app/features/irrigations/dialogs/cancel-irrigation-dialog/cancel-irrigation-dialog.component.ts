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

import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import { IrrigationService } from "../../../../core/irrigations/irrigation.service";

export interface CancelIrrigationDialogData {
  readonly irrigationId: string;
  readonly farmName: string;
  readonly farmAreaName: string;
  readonly currentStatus: string;
  readonly concurrencyToken?: string | null;
}

@Component({
  selector: "app-cancel-irrigation-dialog",
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: "./cancel-irrigation-dialog.component.html",
  styleUrl: "./cancel-irrigation-dialog.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CancelIrrigationDialogComponent {
  readonly data: CancelIrrigationDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<CancelIrrigationDialogComponent>);
  private readonly irrigationService = inject(IrrigationService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);

  readonly isSubmitting = signal(false);

  readonly form = this.fb.group({
    reason: ["", [Validators.required, Validators.maxLength(1000)]],
  });

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onSubmit(): void {
    const rawReason = this.form.value.reason?.trim();
    if (!rawReason) {
      this.form.get("reason")?.setErrors({ required: true });
      return;
    }

    this.isSubmitting.set(true);
    this.irrigationService
      .cancel(this.data.irrigationId, {
        cancellationReason: rawReason,
        concurrencyToken: this.data.concurrencyToken,
      })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.snack.open("Irrigation event has been cancelled.", "Dismiss", {
            duration: 3500,
          });
          this.dialogRef.close(true);
        },
        error: (err) => {
          this.snack.open(getApiErrorMessage(err, "Failed to cancel irrigation event."), "Dismiss", {
            duration: 5000,
          });
        },
      });
  }
}
