import { DatePipe, DecimalPipe } from "@angular/common";
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
import { FormBuilder, ReactiveFormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatDialog, MatDialogModule } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatMenuModule } from "@angular/material/menu";
import { MatPaginatorModule, PageEvent } from "@angular/material/paginator";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTableModule } from "@angular/material/table";
import { MatTooltipModule } from "@angular/material/tooltip";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { finalize, forkJoin } from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import { BreadcrumbService } from "../../../core/breadcrumb/breadcrumb.service";
import {
  PAYMENT_STATUS_OPTIONS,
  PAYMENT_TYPE_OPTIONS,
  PaymentType,
  WorkerDetail,
  WorkerPayment,
  WorkerSettlementCalculation,
  formatEmploymentType,
  formatGender,
  formatPaymentMethod,
  formatPaymentStatus,
  formatPaymentType,
} from "../../../core/labor/labor.models";
import { LaborService } from "../../../core/labor/labor.service";
import { getApiErrorMessage } from "../../../core/models/api-error.model";
import { formatDateOnly } from "../../../core/utils/date.utils";
import { ErrorAlertComponent } from "../../../shared/components/error-alert/error-alert.component";
import {
  WorkerPaymentDialogComponent,
  WorkerPaymentDialogData,
} from "./dialogs/worker-payment-dialog.component";
import {
  WorkerPaymentCancelDialogComponent,
} from "./dialogs/worker-payment-cancel-dialog.component";

@Component({
  selector: "app-worker-payments-page",
  standalone: true,
  imports: [
    DatePipe,
    DecimalPipe,
    ErrorAlertComponent,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: "./worker-payments-page.component.html",
  styleUrl: "./worker-payments-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkerPaymentsPageComponent implements OnInit {
  private readonly laborService = inject(LaborService);
  private readonly route = inject(ActivatedRoute);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly breadcrumbService = inject(BreadcrumbService);
  readonly permissionService = inject(PermissionService);

  readonly workerId = this.route.snapshot.paramMap.get("id")!;

  readonly worker = signal<WorkerDetail | null>(null);
  readonly settlement = signal<WorkerSettlementCalculation | null>(null);
  readonly payments = signal<readonly WorkerPayment[]>([]);

  readonly isLoadingWorker = signal(true);
  readonly isLoadingSettlement = signal(true);
  readonly isLoadingPayments = signal(true);
  readonly errorMessage = signal<string | null>(null);

  // Pagination state
  readonly totalPayments = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);

  // Filter form
  readonly filterForm = this.fb.group({
    paymentType: ["all"],
    status: ["all"],
    fromDate: [null as Date | null],
    toDate: [null as Date | null],
  });

  readonly paymentTypeOptions = [
    { value: "all", label: "All Payment Types" },
    ...PAYMENT_TYPE_OPTIONS,
  ];
  readonly paymentStatusOptions = PAYMENT_STATUS_OPTIONS;

  readonly displayedColumns: readonly string[] = [
    "paymentDate",
    "paymentType",
    "amount",
    "paymentMethod",
    "referenceNumber",
    "period",
    "status",
    "notes",
    "actions",
  ];

  readonly currencySymbol = computed(() => {
    return this.settlement()?.currencySymbol || "₹";
  });

  ngOnInit(): void {
    this.loadWorkerAndSummary();
    this.loadPayments();
  }

  loadWorkerAndSummary(): void {
    this.isLoadingWorker.set(true);
    this.isLoadingSettlement.set(true);

    forkJoin({
      worker: this.laborService.getWorker(this.workerId),
      settlement: this.laborService.getWorkerSettlement(this.workerId),
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.isLoadingWorker.set(false);
          this.isLoadingSettlement.set(false);
        }),
      )
      .subscribe({
        next: ({ worker, settlement }) => {
          this.worker.set(worker);
          this.settlement.set(settlement);

          this.breadcrumbService.setEntityName(worker.id, worker.displayName);
          this.breadcrumbService.setTrail([
            { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
            { label: "Labor", route: "/labor" },
            { label: "Workers", route: "/labor/workers" },
            { label: worker.displayName, route: `/labor/workers/${worker.id}` },
            { label: "Payments & Settlement" },
          ]);
        },
        error: (err) => {
          this.errorMessage.set(
            getApiErrorMessage(err, "Failed to load worker profile or settlement data."),
          );
        },
      });
  }

  loadSettlementOnly(): void {
    this.isLoadingSettlement.set(true);
    this.laborService
      .getWorkerSettlement(this.workerId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoadingSettlement.set(false)),
      )
      .subscribe({
        next: (settlement) => this.settlement.set(settlement),
        error: (err) => {
          this.snack.open(
            getApiErrorMessage(err, "Failed to refresh settlement summary."),
            "Dismiss",
            { duration: 4000 },
          );
        },
      });
  }

  loadPayments(): void {
    this.isLoadingPayments.set(true);

    const raw = this.filterForm.getRawValue();
    const fromStr = raw.fromDate ? formatDateOnly(raw.fromDate) : null;
    const toStr = raw.toDate ? formatDateOnly(raw.toDate) : null;
    const typeFilter = raw.paymentType !== "all" ? raw.paymentType : null;
    const statusFilter = raw.status !== "all" ? raw.status : null;

    this.laborService
      .listWorkerPaymentsPaged(
        this.workerId,
        this.pageIndex() + 1,
        this.pageSize(),
        fromStr,
        toStr,
        typeFilter,
        statusFilter,
      )
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoadingPayments.set(false)),
      )
      .subscribe({
        next: (res) => {
          this.payments.set(res.items);
          this.totalPayments.set(res.totalCount);
        },
        error: (err) => {
          this.payments.set([]);
          this.totalPayments.set(0);
          this.snack.open(
            getApiErrorMessage(err, "Failed to load payment history."),
            "Dismiss",
            { duration: 4000 },
          );
        },
      });
  }

  applyFilters(): void {
    this.pageIndex.set(0);
    this.loadPayments();
  }

  resetFilters(): void {
    this.filterForm.reset({
      paymentType: "all",
      status: "all",
      fromDate: null,
      toDate: null,
    });
    this.pageIndex.set(0);
    this.loadPayments();
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadPayments();
  }

  openRecordDialog(mode: "ADVANCE" | "PARTIAL_PAYOUT" | "FINAL_PAYOUT" | "GENERAL"): void {
    const worker = this.worker();
    const dialogData: WorkerPaymentDialogData = {
      workerId: this.workerId,
      workerDisplayName: worker?.displayName,
      mode,
    };

    const ref = this.dialog.open(WorkerPaymentDialogComponent, {
      width: "600px",
      data: dialogData,
    });

    ref.afterClosed().subscribe((result) => {
      if (result) {
        // Reload settlement summary and payment history
        this.loadSettlementOnly();
        this.loadPayments();
      }
    });
  }

  openCancelDialog(payment: WorkerPayment): void {
    const ref = this.dialog.open(WorkerPaymentCancelDialogComponent, {
      width: "520px",
      data: {
        workerId: this.workerId,
        payment,
      },
    });

    ref.afterClosed().subscribe((result) => {
      if (result) {
        // Reload settlement summary and payment history
        this.loadSettlementOnly();
        this.loadPayments();
      }
    });
  }

  formatType(type: string): string {
    return formatPaymentType(type);
  }

  formatMethod(method: string): string {
    return formatPaymentMethod(method);
  }

  formatStatus(status: string): string {
    return formatPaymentStatus(status);
  }

  formatGenderLabel(gender: string): string {
    return formatGender(gender);
  }

  formatEmploymentTypeLabel(type: string): string {
    return formatEmploymentType(type);
  }
}
