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
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import { PlantationEndReason } from "../../core/farm-management/farm-management.models";
import { formatDateOnly } from "../../core/utils/date.utils";

export interface PlantationTerminateDialogData {
  plantationId: string;
  plantationName: string;
  defaultNotes?: string;
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
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatSelectModule,
    MatInputModule,
    MatCheckboxModule,
    MatProgressSpinnerModule,
  ],
  template: `
    <h2 mat-dialog-title class="dialog-title">
      <mat-icon class="title-icon">warning_amber</mat-icon>
      Terminate Plantation
    </h2>
    <mat-dialog-content>
      <p class="dialog-description">
        Terminating <strong>{{ data.plantationName }}</strong> will conclude operations for this plot and release the allocated area.
      </p>

      @if (isLoadingReasons()) {
        <div class="loading-reasons">
          <mat-spinner diameter="28"></mat-spinner>
          <span>Loading termination reasons…</span>
        </div>
      } @else {
        <form [formGroup]="form" class="terminate-form">
          <mat-form-field appearance="outline" class="full-width" subscriptSizing="dynamic">
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

          <mat-form-field appearance="outline" class="full-width" subscriptSizing="dynamic">
            <mat-label>Termination Date</mat-label>
            <input
              matInput
              [matDatepicker]="terminationDatePicker"
              formControlName="terminationDate"
              required
              placeholder="Choose a date"
            />
            <mat-datepicker-toggle matIconSuffix [for]="terminationDatePicker" />
            <mat-datepicker #terminationDatePicker />
            @if (form.get('terminationDate')?.hasError('required')) {
              <mat-error>Termination date is required</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width" subscriptSizing="dynamic">
            <mat-label>Notes & Reason Details</mat-label>
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

    <mat-dialog-actions align="end" class="dialog-actions">
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
    .dialog-title {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 1.15rem;
      font-weight: 600;
      color: #b91c1c;
      margin: 0;
      padding: 18px 24px 12px;

      .title-icon {
        font-size: 22px;
        width: 22px;
        height: 22px;
        color: #dc2626;
      }
    }
    .dialog-description {
      margin-bottom: 14px;
      color: #475569;
      font-size: 0.875rem;
      line-height: 1.5;
    }
    .loading-reasons {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 20px 0;
      color: #64748b;
      font-size: 0.85rem;
    }
    .terminate-form {
      display: flex;
      flex-direction: column;
      gap: 12px;
      min-width: 420px;
    }
    .full-width {
      width: 100%;
    }
    .checkbox-row {
      margin-top: 4px;
      margin-bottom: 4px;
      font-size: 0.85rem;
    }
    .dialog-actions {
      padding: 12px 24px 18px;
      gap: 8px;

      button {
        height: 36px;
        font-size: 0.85rem;
      }
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
    terminationDate: [new Date(), Validators.required],
    notes: [this.data.defaultNotes ?? "Terminated from plantation details."],
    cancelActiveCycles: [true],
  });

  ngOnInit(): void {
    this.service.listEndReasons().subscribe({
      next: (reasons) => {
        const activeReasons = reasons.filter((r) => r.isActive);
        this.endReasons.set(activeReasons);
        this.isLoadingReasons.set(false);

        if (!this.form.get("endReasonId")?.value) {
          const harvestReason = activeReasons.find(
            (r) => r.code?.toUpperCase() === "HARVEST_COMPLETED",
          );
          if (harvestReason) {
            this.form.patchValue({ endReasonId: harvestReason.id });
          }
        }
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
    const value = this.form.value;
    this.dialogRef.close({
      ...value,
      terminationDate: formatDateOnly(value.terminationDate) ?? "",
    } as PlantationTerminateDialogResult);
  }
}
