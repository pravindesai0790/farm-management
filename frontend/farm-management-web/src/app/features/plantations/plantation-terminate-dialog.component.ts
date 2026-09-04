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
import { MatCheckboxModule } from "@angular/material/checkbox";
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
import { PlantationEndReason } from "../../core/farm-management/farm-management.models";

export interface PlantationTerminateDialogData {
  plantationId: string;
  plantationCode: string;
  plantationName: string;
}

export interface PlantationTerminateDialogResult {
  endReasonId: string;
  terminationDate: string;
  notes: string;
  cancelActiveCycles: boolean;
}

@Component({
  selector: "app-plantation-terminate-dialog",
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatCheckboxModule,
    MatProgressSpinnerModule,
  ],
  template: `
    <h2 mat-dialog-title>Terminate Plantation</h2>
    <mat-dialog-content>
      <p class="dialog-description">
        Terminating <strong>{{ data.plantationName }}</strong> ({{ data.plantationCode }})
        will conclude operations for this plot and release the allocated area.
      </p>

      @if (isLoadingReasons()) {
        <div class="loading-reasons">
          <mat-spinner diameter="32"></mat-spinner>
          <span>Loading termination reasons…</span>
        </div>
      } @else {
        <form [formGroup]="form" class="terminate-form">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>End Reason</mat-label>
            <mat-select formControlName="endReasonId" required>
              @for (reason of endReasons(); track reason.id) {
                <mat-option [value]="reason.id">
                  {{ reason.name }}
                </mat-option>
              }
            </mat-select>
            @if (form.get('endReasonId')?.hasError('required')) {
              <mat-error>Termination reason is required</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Termination Date</mat-label>
            <input matInput type="date" formControlName="terminationDate" required />
            @if (form.get('terminationDate')?.hasError('required')) {
              <mat-error>Termination date is required</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Notes</mat-label>
            <textarea
              matInput
              rows="3"
              formControlName="notes"
              placeholder="Provide reason or operational details…"
            ></textarea>
          </mat-form-field>

          <div class="checkbox-row">
            <mat-checkbox formControlName="cancelActiveCycles" color="primary">
              Cancel all active crop cycles for this plantation
            </mat-checkbox>
          </div>
        </form>
      }
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="onCancel()">Cancel</button>
      <button
        mat-flat-button
        color="warn"
        type="button"
        [disabled]="form.invalid || isLoadingReasons()"
        (click)="onSubmit()"
      >
        Terminate Plantation
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
    .terminate-form {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      min-width: 420px;
    }
    .full-width {
      width: 100%;
    }
    .checkbox-row {
      margin-top: 0.25rem;
      margin-bottom: 0.5rem;
    }
    mat-dialog-actions {
      padding: 1rem 1.5rem;
      gap: 0.5rem;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlantationTerminateDialogComponent implements OnInit {
  readonly data: PlantationTerminateDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(
    MatDialogRef<PlantationTerminateDialogComponent, PlantationTerminateDialogResult>,
  );
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(FarmManagementService);

  readonly endReasons = signal<readonly PlantationEndReason[]>([]);
  readonly isLoadingReasons = signal(true);

  readonly form: FormGroup = this.fb.group({
    endReasonId: ["", Validators.required],
    terminationDate: [
      new Date().toISOString().slice(0, 10),
      Validators.required,
    ],
    notes: ["Terminated from plantation details."],
    cancelActiveCycles: [true],
  });

  ngOnInit(): void {
    this.service.listEndReasons().subscribe({
      next: (reasons) => {
        this.endReasons.set(reasons.filter((r) => r.isActive));
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
    this.dialogRef.close(this.form.value as PlantationTerminateDialogResult);
  }
}
