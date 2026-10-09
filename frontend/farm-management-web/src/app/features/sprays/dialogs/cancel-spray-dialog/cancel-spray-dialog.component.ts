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

export interface CancelSprayDialogData {
  readonly sprayId: string;
  readonly referenceNumber: string;
  readonly farmName: string;
  readonly status: string;
}

@Component({
  selector: "app-cancel-spray-dialog",
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
  templateUrl: "./cancel-spray-dialog.component.html",
  styleUrl: "./cancel-spray-dialog.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CancelSprayDialogComponent {
  readonly data: CancelSprayDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<CancelSprayDialogComponent>);
  private readonly sprayService = inject(SprayService);
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
    this.sprayService
      .cancelSpray(this.data.sprayId, rawReason)
      .pipe(
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: () => {
          this.snack.open(`Spray ${this.data.referenceNumber} has been cancelled.`, "Dismiss", {
            duration: 4000,
          });
          this.dialogRef.close(true);
        },
        error: (err) => {
          this.snack.open(getApiErrorMessage(err, "Failed to cancel spray application."), "Dismiss", {
            duration: 5000,
          });
        },
      });
  }
}
