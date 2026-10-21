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
import { MatCheckboxModule } from '@angular/material/checkbox';
import { finalize } from 'rxjs';
import { Unit } from '../../../../core/farm-management/farm-management.models';
import { FarmManagementService } from '../../../../core/farm-management/farm-management.service';
import { getApiErrorMessage } from '../../../../core/models/api-error.model';
import { DateTimePickerComponent } from '../../../../shared/components/date-time-picker/date-time-picker.component';
import {
  CompleteFertilizerApplicationProductLineRequest,
  CompleteFertilizerApplicationRequest,
  FertilizerApplicationDetailsResponse,
  FertilizerApplicationProductDto,
} from '../../../../core/fertilizers/fertilizer.models';
import { FertilizerService } from '../../../../core/fertilizers/fertilizer.service';

export interface CompleteFertilizerDialogData {
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

export interface CompleteProductRowForm {
  inventoryItemId: FormControl<string>;
  actualQuantity: FormControl<number | null>;
}

@Component({
  selector: 'app-complete-fertilizer-dialog',
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
    MatCheckboxModule,
    MatIconModule,
    MatProgressSpinnerModule,
    DateTimePickerComponent,
  ],
  providers: [provideNativeDateAdapter()],
  templateUrl: './complete-fertilizer-dialog.component.html',
  styleUrl: './complete-fertilizer-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CompleteFertilizerDialogComponent implements OnInit {
  readonly data = inject<CompleteFertilizerDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<CompleteFertilizerDialogComponent>);
  private readonly fertilizerService = inject(FertilizerService);
  private readonly farmService = inject(FarmManagementService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly shortages = signal<readonly string[]>([]);
  readonly areaUnits = signal<readonly Unit[]>([]);

  readonly today: Date = new Date();

  // Reactive signal tracking line variances
  readonly lineVariances = signal<{ [itemId: string]: number }>({});
  readonly hasAnyVariance = computed(() => {
    const vars = this.lineVariances();
    return Object.values(vars).some((v) => Math.abs(v) > 0.0001);
  });

  readonly form = this.fb.group({
    actualApplicationDate: [
      new Date().toISOString() as string | Date | null,
      [Validators.required, notFutureDateValidator],
    ],
    actualArea: [
      this.app.plannedArea ?? null,
      [Validators.min(0.0001)],
    ],
    actualAreaUnitId: [this.app.plannedAreaUnitId || ''],
    acknowledgeVariance: [false],
    products: this.fb.array<FormGroup<CompleteProductRowForm>>([]),
  });

  get app(): FertilizerApplicationDetailsResponse {
    return this.data.application;
  }

  get productsArray(): FormArray<FormGroup<CompleteProductRowForm>> {
    return this.form.get('products') as FormArray<FormGroup<CompleteProductRowForm>>;
  }

  ngOnInit(): void {
    this.farmService
      .listUnits('Area')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (units) => this.areaUnits.set(units),
      });

    this.populateProducts();

    this.productsArray.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.recalculateVariances();
      });

    this.recalculateVariances();
  }

  private populateProducts(): void {
    this.productsArray.clear();
    for (const p of this.app.products) {
      const row = this.fb.group<CompleteProductRowForm>({
        inventoryItemId: new FormControl(p.inventoryItemId, {
          nonNullable: true,
          validators: [Validators.required],
        }),
        actualQuantity: new FormControl(
          p.plannedQuantity ?? null,
          [Validators.required, Validators.min(0.0001)],
        ),
      });
      this.productsArray.push(row);
    }
  }

  recalculateVariances(): void {
    const vars: { [itemId: string]: number } = {};
    for (let i = 0; i < this.app.products.length; i++) {
      const orig = this.app.products[i];
      const ctrl = this.productsArray.at(i);
      const actual = Number(ctrl?.value?.actualQuantity ?? 0);
      const planned = orig.plannedQuantity ?? 0;
      vars[orig.inventoryItemId] = actual - planned;
    }
    this.lineVariances.set(vars);
  }

  getLineVariance(itemId: string): number {
    return this.lineVariances()[itemId] ?? 0;
  }

  getProductOrig(itemId: string): FertilizerApplicationProductDto | undefined {
    return this.app.products.find((p) => p.inventoryItemId === itemId);
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (this.hasAnyVariance() && !this.form.value.acknowledgeVariance) {
      this.errorMessage.set(
        'Please acknowledge the variance between planned and actual quantities before completing.',
      );
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.shortages.set([]);

    const formVal = this.form.value;
    const appDateStr = typeof formVal.actualApplicationDate === 'string'
      ? formVal.actualApplicationDate
      : formVal.actualApplicationDate
        ? new Date(formVal.actualApplicationDate).toISOString()
        : '';

    const productRequests: CompleteFertilizerApplicationProductLineRequest[] =
      (formVal.products || []).map((p) => ({
        inventoryItemId: p.inventoryItemId!,
        actualQuantity: Number(p.actualQuantity),
      }));

    const req: CompleteFertilizerApplicationRequest = {
      actualApplicationDate: appDateStr,
      actualArea: formVal.actualArea ? Number(formVal.actualArea) : undefined,
      actualAreaUnitId: formVal.actualAreaUnitId || undefined,
      acknowledgeVariance: !!formVal.acknowledgeVariance,
      products: productRequests,
      version: this.app.version,
      idempotencyKey: crypto.randomUUID(),
    };

    this.fertilizerService
      .complete(this.app.id, req)
      .pipe(
        finalize(() => this.isSubmitting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (updated) => {
          this.snack.open(
            `Fertilizer application '${updated.referenceNumber}' completed and inventory deducted successfully.`,
            'Dismiss',
            { duration: 4000 },
          );
          this.dialogRef.close(true);
        },
        error: (err) => {
          this.parseCompleteErrors(err);
        },
      });
  }

  private parseCompleteErrors(err: any): void {
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
        getApiErrorMessage(err, 'Failed to complete fertilizer application.'),
      );
    }
  }
}
