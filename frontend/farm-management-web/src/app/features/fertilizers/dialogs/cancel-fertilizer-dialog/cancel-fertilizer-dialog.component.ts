import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormArray,
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';
import { getApiErrorMessage } from '../../../../core/models/api-error.model';
import { DateTimePickerComponent } from '../../../../shared/components/date-time-picker/date-time-picker.component';
import {
  CancelFertilizerApplicationProductLineRequest,
  CancelFertilizerApplicationRequest,
  FertilizerApplicationDetailsResponse,
  FertilizerApplicationProductDto,
  FertilizerStorageLocationLookup,
} from '../../../../core/fertilizers/fertilizer.models';
import { FertilizerService } from '../../../../core/fertilizers/fertilizer.service';

export interface CancelFertilizerDialogData {
  application: FertilizerApplicationDetailsResponse;
}

export function notFutureDateValidator(control: AbstractControl): ValidationErrors | null {
  const val = control.value;
  if (!val) return null;
  const selectedDate = new Date(val);
  const today = new Date();
  today.setHours(23, 59, 59, 999);
  if (selectedDate.getTime() > today.getTime()) {
    return { futureDate: true };
  }
  return null;
}

export interface CancelProductRowForm {
  inventoryItemId: FormControl<string>;
  actualQuantity: FormControl<number | null>;
  storageLocationId: FormControl<string>;
}

@Component({
  selector: 'app-cancel-fertilizer-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatIconModule,
    MatProgressSpinnerModule,
    DateTimePickerComponent,
  ],
  providers: [provideNativeDateAdapter()],
  templateUrl: './cancel-fertilizer-dialog.component.html',
  styleUrl: './cancel-fertilizer-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CancelFertilizerDialogComponent implements OnInit {
  readonly data = inject<CancelFertilizerDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<CancelFertilizerDialogComponent>);
  private readonly fertilizerService = inject(FertilizerService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly shortages = signal<readonly string[]>([]);
  readonly storageLocations = signal<readonly FertilizerStorageLocationLookup[]>([]);

  readonly today: Date = new Date();

  readonly isPostStart = computed(() => this.app.status === 'InProgress');

  readonly hasPositiveUsage = signal(false);

  readonly form = this.fb.group({
    cancellationReason: [
      '',
      [Validators.required, Validators.maxLength(1000)],
    ],
    actualApplicationDate: [
      new Date().toISOString() as string | Date | null,
      [notFutureDateValidator],
    ],
    products: this.fb.array<FormGroup<CancelProductRowForm>>([]),
  });

  get app(): FertilizerApplicationDetailsResponse {
    return this.data.application;
  }

  get productsArray(): FormArray<FormGroup<CancelProductRowForm>> {
    return this.form.get('products') as FormArray<FormGroup<CancelProductRowForm>>;
  }

  ngOnInit(): void {
    if (this.app.farmId && !this.isPostStart()) {
      this.fertilizerService
        .getStorageLocationsLookup(this.app.farmId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (locs) => this.storageLocations.set(locs),
        });
    }

    this.populateProducts();

    this.productsArray.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.updatePositiveUsageState();
      });

    this.updatePositiveUsageState();
  }

  private populateProducts(): void {
    this.productsArray.clear();
    for (const p of this.app.products) {
      const row = this.fb.group<CancelProductRowForm>({
        inventoryItemId: new FormControl(p.inventoryItemId, {
          nonNullable: true,
          validators: [Validators.required],
        }),
        actualQuantity: new FormControl(0, [
          Validators.required,
          Validators.min(0),
        ]),
        storageLocationId: new FormControl(p.storageLocationId || '', {
          nonNullable: true,
        }),
      });
      this.productsArray.push(row);
    }
  }

  private updatePositiveUsageState(): void {
    let positiveFound = false;
    for (let i = 0; i < this.productsArray.length; i++) {
      const ctrl = this.productsArray.at(i);
      const qty = Number(ctrl.value.actualQuantity ?? 0);
      if (qty > 0) {
        positiveFound = true;
        // Storage location required if pre-start and qty > 0
        if (!this.isPostStart()) {
          ctrl.controls.storageLocationId.setValidators([Validators.required]);
        }
      } else {
        if (!this.isPostStart()) {
          ctrl.controls.storageLocationId.clearValidators();
        }
      }
      ctrl.controls.storageLocationId.updateValueAndValidity({ emitEvent: false });
    }

    this.hasPositiveUsage.set(positiveFound);

    const dateCtrl = this.form.get('actualApplicationDate');
    if (positiveFound) {
      dateCtrl?.setValidators([Validators.required, notFutureDateValidator]);
    } else {
      dateCtrl?.clearValidators();
    }
    dateCtrl?.updateValueAndValidity({ emitEvent: false });
  }

  getProductOrig(itemId: string): FertilizerApplicationProductDto | undefined {
    return this.app.products.find((p) => p.inventoryItemId === itemId);
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onSubmit(): void {
    this.updatePositiveUsageState();

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const reason = this.form.value.cancellationReason?.trim();
    if (!reason) {
      this.form.get('cancellationReason')?.setErrors({ required: true });
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.shortages.set([]);

    const formVal = this.form.value;
    const hasPos = this.hasPositiveUsage();
    const appDateStr = typeof formVal.actualApplicationDate === 'string'
      ? formVal.actualApplicationDate
      : formVal.actualApplicationDate
        ? new Date(formVal.actualApplicationDate).toISOString()
        : undefined;

    const productRequests: CancelFertilizerApplicationProductLineRequest[] =
      (formVal.products || []).map((p) => ({
        inventoryItemId: p.inventoryItemId!,
        actualQuantity: Number(p.actualQuantity || 0),
        storageLocationId: p.storageLocationId || undefined,
      }));

    const req: CancelFertilizerApplicationRequest = {
      cancellationReason: reason,
      actualApplicationDate: hasPos ? appDateStr : undefined,
      version: this.app.version,
      idempotencyKey: crypto.randomUUID(),
      products: productRequests,
    };

    this.fertilizerService
      .cancel(this.app.id, req)
      .pipe(
        finalize(() => this.isSubmitting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (updated) => {
          this.snack.open(
            `Fertilizer application '${updated.referenceNumber}' cancelled successfully.`,
            'Dismiss',
            { duration: 4000 },
          );
          this.dialogRef.close(true);
        },
        error: (err) => {
          this.parseCancelErrors(err);
        },
      });
  }

  private parseCancelErrors(err: any): void {
    const extractedShortages: string[] = [];
    if (err?.error?.errors && typeof err.error.errors === 'object') {
      for (const key of Object.keys(err.error.errors)) {
        const val = err.error.errors[key];
        if (Array.isArray(val)) {
          extractedShortages.push(...val);
        } else if (typeof val === 'string') {
          extractedShortages.push(val);
        }
      }
    }

    if (extractedShortages.length > 0) {
      this.shortages.set(extractedShortages);
    }

    if (err?.status === 409) {
      this.errorMessage.set(
        'Application has been modified concurrently. Please refresh the page.',
      );
    } else {
      this.errorMessage.set(
        getApiErrorMessage(err, 'Failed to cancel fertilizer application.'),
      );
    }
  }
}
