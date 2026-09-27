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
import { MatDatepickerModule } from "@angular/material/datepicker";
import {
  MAT_DIALOG_DATA,
  MatDialogModule,
  MatDialogRef,
} from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { CycleCancellationReason } from "../../core/farm-management/farm-management.models";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import { formatDateOnly } from "../../core/utils/date.utils";

export interface CropCycleCancelDialogData {
  cycleId: string;
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
    MatDatepickerModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: "./crop-cycle-cancel-dialog.component.html",
  styleUrl: "./crop-cycle-cancel-dialog.component.scss",
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
    cancellationDate: [new Date(), Validators.required],
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
    const value = this.form.value;
    this.dialogRef.close({
      ...value,
      cancellationDate: formatDateOnly(value.cancellationDate) ?? "",
    } as CropCycleCancelDialogResult);
  }
}
