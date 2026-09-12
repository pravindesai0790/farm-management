import { CurrencyPipe, DatePipe, DecimalPipe } from "@angular/common";
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
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
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { finalize } from "rxjs";

import {
  CurrencyItem,
  PAYMENT_METHOD_OPTIONS,
  PAYMENT_TYPE_OPTIONS,
  PaymentMethod,
  PaymentType,
  RecordWorkerPaymentRequest,
  WorkerPayment,
  WorkerSettlementCalculation,
} from "../../../../core/labor/labor.models";
import { LaborService } from "../../../../core/labor/labor.service";
import {
  getApiErrorMessage,
  getApiValidationErrors,
} from "../../../../core/models/api-error.model";
import { formatDateOnly, parseDateOnly } from "../../../../core/utils/date.utils";
import { ErrorAlertComponent } from "../../../../shared/components/error-alert/error-alert.component";

export interface WorkerPaymentDialogData {
  readonly workerId: string;
  readonly workerDisplayName?: string;
  readonly mode?: "ADVANCE" | "PARTIAL_PAYOUT" | "FINAL_PAYOUT" | "GENERAL";
  readonly defaultType?: PaymentType;
  readonly defaultPeriodFrom?: string | null;
  readonly defaultPeriodTo?: string | null;
}

@Component({
  selector: "app-worker-payment-dialog",
  standalone: true,
  imports: [
    DecimalPipe,
    ErrorAlertComponent,
    MatButtonModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: "./worker-payment-dialog.component.html",
  styleUrl: "./worker-payment-dialog.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkerPaymentDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly laborService = inject(LaborService);
  private readonly dialogRef = inject(MatDialogRef<WorkerPaymentDialogComponent>);
  readonly data = inject<WorkerPaymentDialogData>(MAT_DIALOG_DATA);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  readonly isSubmitting = signal(false);
  readonly isLoadingSettlement = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly validationErrors = signal<Readonly<Record<string, readonly string[]>>>({});

  readonly settlement = signal<WorkerSettlementCalculation | null>(null);
  readonly currencies = signal<readonly CurrencyItem[]>([]);

  readonly paymentTypes = PAYMENT_TYPE_OPTIONS;
  readonly paymentMethods = PAYMENT_METHOD_OPTIONS;

  readonly form = this.fb.group({
    paymentDate: [new Date(), [Validators.required]],
    paymentType: [this.resolveInitialPaymentType(), [Validators.required]],
    amount: [null as number | null, [Validators.required, Validators.min(0.01)]],
    currencyId: ["", [Validators.required]],
    paymentMethod: ["CASH" as PaymentMethod, [Validators.required]],
    referenceNumber: [""],
    paymentPeriodFrom: [this.data.defaultPeriodFrom ? parseDateOnly(this.data.defaultPeriodFrom) : null],
    paymentPeriodTo: [this.data.defaultPeriodTo ? parseDateOnly(this.data.defaultPeriodTo) : null],
    notes: [""],
  }, { validators: [this.periodRangeValidator(), this.payoutAmountValidator()] });

  readonly currencySymbol = computed(() => {
    const s = this.settlement();
    if (s?.currencySymbol) {
      return s.currencySymbol;
    }
    const cid = this.form.controls.currencyId.value;
    const curr = this.currencies().find(c => c.id === cid);
    return curr?.symbol || "₹";
  });

  readonly availablePayable = computed(() => {
    return this.settlement()?.amountAvailableForPayout ?? 0;
  });

  readonly isPayoutType = computed(() => {
    const type = this.form.controls.paymentType.value;
    return type === "PAYOUT";
  });

  readonly isAdvanceType = computed(() => {
    const type = this.form.controls.paymentType.value;
    return type === "ADVANCE";
  });

  readonly dialogTitle = computed(() => {
    switch (this.data.mode) {
      case "ADVANCE":
        return "Record Worker Advance";
      case "PARTIAL_PAYOUT":
        return "Record Partial Payout";
      case "FINAL_PAYOUT":
        return "Record Final Payout";
      default:
        return "Record Worker Payment";
    }
  });

  ngOnInit(): void {
    this.loadCurrencies();
    this.loadSettlement();

    // Listen to period date changes to dynamically refresh settlement summary
    this.form.controls.paymentPeriodFrom.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.onPeriodChanged());

    this.form.controls.paymentPeriodTo.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.onPeriodChanged());

    // Revalidate form on payment type change
    this.form.controls.paymentType.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((type) => {
        this.form.updateValueAndValidity();
        if (this.data.mode === "FINAL_PAYOUT" && type === "PAYOUT") {
          const avail = this.availablePayable();
          if (avail > 0 && (!this.form.controls.amount.value || this.form.controls.amount.value === 0)) {
            this.form.controls.amount.setValue(avail);
          }
        }
      });
  }

  loadCurrencies(): void {
    this.laborService
      .listCurrencies()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => {
          this.currencies.set(items);
          if (!this.form.controls.currencyId.value && items.length > 0) {
            const inr = items.find((c) => c.code === "INR");
            const selected = inr ?? items[0];
            this.form.controls.currencyId.setValue(selected.id);
          }
        },
      });
  }

  loadSettlement(): void {
    this.isLoadingSettlement.set(true);
    const fromStr = this.form.controls.paymentPeriodFrom.value
      ? formatDateOnly(this.form.controls.paymentPeriodFrom.value)
      : null;
    const toStr = this.form.controls.paymentPeriodTo.value
      ? formatDateOnly(this.form.controls.paymentPeriodTo.value)
      : null;

    this.laborService
      .getWorkerSettlement(this.data.workerId, fromStr, toStr)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoadingSettlement.set(false)),
      )
      .subscribe({
        next: (settlement) => {
          this.settlement.set(settlement);

          // Update currency if available
          if (settlement.currencyId && !this.form.controls.currencyId.value) {
            this.form.controls.currencyId.setValue(settlement.currencyId);
          }

          // If final payout mode, prefill amount with available payable
          if (this.data.mode === "FINAL_PAYOUT") {
            if (settlement.amountAvailableForPayout > 0) {
              this.form.controls.amount.setValue(settlement.amountAvailableForPayout);
            }
          }

          this.form.updateValueAndValidity();
        },
        error: (err) => {
          this.errorMessage.set(
            getApiErrorMessage(err, "Could not load authoritative settlement summary."),
          );
        },
      });
  }

  private onPeriodChanged(): void {
    const from = this.form.controls.paymentPeriodFrom.value;
    const to = this.form.controls.paymentPeriodTo.value;
    if (from && to && parseDateOnly(to)! < parseDateOnly(from)!) {
      return; // Invalid range, let validator handle
    }
    this.loadSettlement();
  }

  private resolveInitialPaymentType(): PaymentType {
    if (this.data.mode === "ADVANCE") {
      return "ADVANCE";
    }
    if (this.data.mode === "PARTIAL_PAYOUT" || this.data.mode === "FINAL_PAYOUT") {
      return "PAYOUT";
    }
    return this.data.defaultType ?? "ADVANCE";
  }

  private periodRangeValidator(): ValidatorFn {
    return (group: AbstractControl): ValidationErrors | null => {
      const from = group.get("paymentPeriodFrom")?.value;
      const to = group.get("paymentPeriodTo")?.value;
      if (from && to) {
        const fromDate = parseDateOnly(from);
        const toDate = parseDateOnly(to);
        if (fromDate && toDate && toDate < fromDate) {
          return { invalidPeriodRange: "Applicable period end date cannot be earlier than start date." };
        }
      }
      return null;
    };
  }

  private payoutAmountValidator(): ValidatorFn {
    return (group: AbstractControl): ValidationErrors | null => {
      const type = group.get("paymentType")?.value;
      const amount = group.get("amount")?.value;
      if (type === "PAYOUT" && amount !== null && amount !== undefined) {
        const available = this.availablePayable();
        if (available <= 0) {
          return {
            noPayableEarnings: "Worker has no available payable earnings to payout. Record an ADVANCE instead.",
          };
        }
        if (amount > available) {
          return {
            amountExceedsPayable: `Payout amount exceeds available payable balance (${available}).`,
          };
        }
      }
      return null;
    };
  }

  apiError(field: string): string | null {
    const errors = this.validationErrors()[field];
    return errors && errors.length > 0 ? errors[0] : null;
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const paymentDateStr = formatDateOnly(raw.paymentDate!);
    if (!paymentDateStr) {
      this.form.controls.paymentDate.setErrors({ required: true });
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.validationErrors.set({});

    let finalNotes = raw.notes?.trim() || null;
    if (this.data.mode === "FINAL_PAYOUT") {
      if (!finalNotes) {
        finalNotes = "Final Payout";
      } else if (!finalNotes.toLowerCase().includes("final")) {
        finalNotes = `${finalNotes} (Final Payout)`;
      }
    }

    const request: RecordWorkerPaymentRequest = {
      workerId: this.data.workerId,
      paymentDate: paymentDateStr,
      paymentType: raw.paymentType!,
      amount: Number(raw.amount),
      currencyId: raw.currencyId!,
      paymentMethod: raw.paymentMethod!,
      referenceNumber: raw.referenceNumber?.trim() || null,
      paymentPeriodFrom: raw.paymentPeriodFrom ? formatDateOnly(raw.paymentPeriodFrom) : null,
      paymentPeriodTo: raw.paymentPeriodTo ? formatDateOnly(raw.paymentPeriodTo) : null,
      notes: finalNotes,
      status: "COMPLETED",
      autoAllocate: true,
    };

    this.laborService
      .recordWorkerPayment(this.data.workerId, request)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: (created) => {
          const modeLabel = this.data.mode === "FINAL_PAYOUT" ? "Final payout" : `${raw.paymentType} payment`;
          this.snack.open(
            `${modeLabel} of ${this.currencySymbol()}${request.amount.toFixed(2)} recorded successfully.`,
            "Dismiss",
            { duration: 4000 },
          );
          this.dialogRef.close(created);
        },
        error: (err) => {
          this.errorMessage.set(
            getApiErrorMessage(err, "Failed to record worker payment."),
          );
          this.validationErrors.set(getApiValidationErrors(err));
        },
      });
  }

  cancel(): void {
    this.dialogRef.close(null);
  }
}
