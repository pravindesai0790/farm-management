import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
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
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import { CycleCancellationReason } from "../../core/farm-management/farm-management.models";

export interface CropCycleCancelDialogData {
  cycleId: string;
  cycleCode: string;
  cycleName: string;
}

export interface CropCycleCancelDialogResult {
  cancellationReasonId: string;
  cancellationDate: string;
  notes: string;
}

@Component({
  selector: "app-crop-cycle-cancel-dialog",
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  template: `
    <h2 mat-dialog-title>Cancel Crop Cycle</h2>
    <mat-dialog-content>
      <p class="dialog-description">
        Cancelling <strong>{{ data.cycleName }}</strong> ({{ data.cycleCode }})
        will stop active production tracking for this season.
      </p>

      @if (isLoadingReasons()) {
        <div class="loading-reasons">
          <mat-spinner diameter="32"></mat-spinner>
          <span>Loading cancellation reasons…</span>
        </div>
      } @else {
        <form [formGroup]="form" class="cancel-form">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Cancellation Reason</mat-label>
            <mat-select formControlName="cancellationReasonId" required>
              @for (reason of cancellationReasons(); track reason.id) {
                <mat-option [value]="reason.id">
                  {{ reason.name }}
                </mat-option>
              }
            </mat-select>
            @if (form.get('cancellationReasonId')?.hasError('required')) {
              <mat-error>Cancellation reason is required</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Cancellation Date</mat-label>
            <input matInput type="date" formControlName="cancellationDate" required />
            @if (form.get('cancellationDate')?.hasError('required')) {
              <mat-error>Cancellation date is required</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Notes</mat-label>
            <textarea
              matInput
              rows="3"
              formControlName="notes"
              placeholder="Reason details or remarks…"
            ></textarea>
          </mat-form-field>
        </form>
      }
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="onCancel()">Back</button>
      <button
        mat-flat-button
        color="warn"
        type="button"
        [disabled]="form.invalid || isLoadingReasons()"
        (click)="onSubmit()"
      >
        Confirm Cancellation
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
    .loading-reasons {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 1.5rem 0;
      color: #718096;
    }
    .cancel-form {
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
export class CropCycleCancelDialogComponent implements OnInit {
  readonly data: CropCycleCancelDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(
    MatDialogRef<CropCycleCancelDialogComponent, CropCycleCancelDialogResult>,
  );
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(FarmManagementService);

  readonly cancellationReasons = signal<readonly CycleCancellationReason[]>([]);
  readonly isLoadingReasons = signal(true);

  readonly form: FormGroup = this.fb.group({
    cancellationReasonId: ["", Validators.required],
    cancellationDate: [
      new Date().toISOString().slice(0, 10),
      Validators.required,
    ],
    notes: ["Cancelled from cycle details."],
  });

  ngOnInit(): void {
    this.service.listCycleCancellationReasons().subscribe({
      next: (reasons) => {
        this.cancellationReasons.set(reasons.filter((r) => r.isActive));
        this.isLoadingReasons.set(false);
      },
      error: () => {
        this.isLoadingReasons.set(false);
      },
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.dialogRef.close(this.form.value as CropCycleCancelDialogResult);
  }
}
