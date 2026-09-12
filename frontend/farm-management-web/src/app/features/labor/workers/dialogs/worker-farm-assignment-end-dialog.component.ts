import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
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
import { finalize } from "rxjs";

import {
  EndWorkerFarmAssignmentRequest,
  WorkerFarmAssignment,
} from "../../../../core/labor/labor.models";
import { LaborService } from "../../../../core/labor/labor.service";
import {
  getApiValidationErrors,
} from "../../../../core/models/api-error.model";
import { formatDateOnly, parseDateOnly } from "../../../../core/utils/date.utils";
import { ErrorAlertComponent } from "../../../../shared/components/error-alert/error-alert.component";

export interface WorkerFarmAssignmentEndDialogData {
  readonly workerId: string;
  readonly assignment: WorkerFarmAssignment;
}

@Component({
  selector: "app-worker-farm-assignment-end-dialog",
  standalone: true,
  imports: [
    ErrorAlertComponent,
    MatButtonModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    ReactiveFormsModule,
  ],
  template: `
    <h2 mat-dialog-title>
      <mat-icon class="title-icon" color="warn">event_busy</mat-icon>
      End Farm Assignment
    </h2>

    <mat-dialog-content>
      <p class="description">
        You are ending the assignment for <strong>{{ data.assignment.farmName }}</strong>
        (started on {{ data.assignment.assignedFrom }}).
      </p>

      <form [formGroup]="form" (ngSubmit)="submit()" class="end-form">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>End date</mat-label>
          <input
            matInput
            [matDatepicker]="picker"
            [min]="minEndDate"
            formControlName="endDate"
            placeholder="Select assignment conclusion date"
          />
          <mat-datepicker-toggle matIconSuffix [for]="picker" />
          <mat-datepicker #picker />
          @if (apiError("endDate")) {
            <mat-error>{{ apiError("endDate") }}</mat-error>
          } @else if (form.controls.endDate.hasError("required")) {
            <mat-error>End date is required.</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Conclusion notes</mat-label>
          <textarea
            matInput
            rows="3"
            formControlName="notes"
            placeholder="Reason for ending assignment or final comments"
          ></textarea>
          @if (apiError("notes")) {
            <mat-error>{{ apiError("notes") }}</mat-error>
          }
        </mat-form-field>

        @if (errorMessage()) {
          <app-error-alert [error]="errorMessage()" fallback="Failed to end assignment." />
        }
      </form>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close [disabled]="isSubmitting()">Cancel</button>
      <button
        mat-flat-button
        color="warn"
        (click)="submit()"
        [disabled]="isSubmitting()"
      >
        @if (isSubmitting()) {
          <mat-spinner diameter="18" class="btn-spinner" />
        }
        End Assignment
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .title-icon {
      vertical-align: middle;
      margin-right: 8px;
    }
    .description {
      margin: 0 0 16px 0;
      color: #424242;
      font-size: 0.9375rem;
    }
    .end-form {
      display: flex;
      flex-direction: column;
      gap: 8px;
      min-width: 380px;
    }
    .full-width {
      width: 100%;
    }
    .btn-spinner {
      display: inline-block;
      margin-right: 6px;
    }
    @media (max-width: 480px) {
      .end-form {
        min-width: 100%;
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkerFarmAssignmentEndDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly laborService = inject(LaborService);
  private readonly dialogRef = inject(MatDialogRef<WorkerFarmAssignmentEndDialogComponent>);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly data = inject<WorkerFarmAssignmentEndDialogData>(MAT_DIALOG_DATA);

  readonly minEndDate = parseDateOnly(this.data.assignment.assignedFrom);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<unknown>(null);
  readonly apiErrors = signal<Readonly<Record<string, readonly string[]>>>({});

  readonly form = this.fb.group({
    endDate: [new Date(), [Validators.required]],
    notes: [""],
  });

  apiError(field: string): string | null {
    return this.apiErrors()[field]?.[0] ?? null;
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.apiErrors.set({});

    const raw = this.form.getRawValue();
    const req: EndWorkerFarmAssignmentRequest = {
      endDate: formatDateOnly(raw.endDate),
      notes: raw.notes?.trim() || null,
    };

    this.laborService
      .endWorkerFarmAssignment(this.data.workerId, this.data.assignment.id, req)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: (res) => {
          this.snack.open("Farm assignment concluded.", "Dismiss", {
            duration: 3000,
          });
          this.dialogRef.close(res);
        },
        error: (err) => {
          this.errorMessage.set(err);
          this.apiErrors.set(getApiValidationErrors(err));
        },
      });
  }
}
