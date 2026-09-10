import {
  ChangeDetectionStrategy,
  Component,
  inject,
} from "@angular/core";
import {
  FormBuilder,
  FormGroup,
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
import { MatInputModule } from "@angular/material/input";

export interface LaborActivityCancelDialogData {
  readonly activityId: string;
  readonly activityDate: string;
  readonly activityTypeName: string;
  readonly farmName: string;
}

@Component({
  selector: "app-labor-activity-cancel-dialog",
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
  ],
  template: `
    <h2 mat-dialog-title>Cancel Labor Activity</h2>
    <mat-dialog-content>
      <p class="dialog-description">
        Cancelling the <strong>{{ data.activityTypeName }}</strong> activity
        recorded on <strong>{{ data.activityDate }}</strong> at
        <strong>{{ data.farmName }}</strong> cannot be undone.
      </p>

      <form [formGroup]="form" class="cancel-form">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Cancellation Reason</mat-label>
          <textarea
            matInput
            rows="3"
            formControlName="reason"
            placeholder="Please provide the reason for cancellation…"
            required
          ></textarea>
          @if (form.get('reason')?.hasError('required') && form.get('reason')?.touched) {
            <mat-error>Cancellation reason is required</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="onBack()">Back</button>
      <button
        mat-flat-button
        color="warn"
        type="button"
        [disabled]="form.invalid"
        (click)="onConfirm()"
      >
        Cancel Activity
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .dialog-description {
      margin-bottom: 1.25rem;
      color: #4a5568;
      font-size: 0.95rem;
      line-height: 1.5;
    }
    .cancel-form {
      display: flex;
      flex-direction: column;
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
export class LaborActivityCancelDialogComponent {
  readonly data: LaborActivityCancelDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<LaborActivityCancelDialogComponent, string>);
  private readonly fb = inject(FormBuilder);

  readonly form: FormGroup = this.fb.group({
    reason: ["", [Validators.required, Validators.minLength(3), Validators.maxLength(500)]],
  });

  onBack(): void {
    this.dialogRef.close();
  }

  onConfirm(): void {
    if (this.form.invalid) return;
    this.dialogRef.close(this.form.value.reason?.trim());
  }
}
