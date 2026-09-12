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
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatButtonToggleModule } from "@angular/material/button-toggle";
import { MatCardModule } from "@angular/material/card";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatDialog, MatDialogModule } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatMenuModule } from "@angular/material/menu";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTableModule } from "@angular/material/table";
import { MatTabsModule } from "@angular/material/tabs";
import { MatTooltipModule } from "@angular/material/tooltip";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { catchError, finalize, forkJoin, of } from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import { BreadcrumbService } from "../../../core/breadcrumb/breadcrumb.service";
import {
  FinancialTransactionType,
  LaborWageRate,
  WorkerDetail,
  WorkerEarningsLedgerItem,
  WorkerFarmAssignment,
  WorkerFinancialTransaction,
  WorkerPayment,
  WorkerPaymentAllocationItem,
  WorkerSettlementCalculation,
  formatEmploymentType,
  formatFinancialTransactionType,
  formatGender,
  formatPaymentMethod,
  formatPaymentStatus,
  formatPaymentType,
  formatWageType,
} from "../../../core/labor/labor.models";
import { LaborService } from "../../../core/labor/labor.service";
import { getApiErrorMessage } from "../../../core/models/api-error.model";
import { formatDateOnly, parseDateOnly } from "../../../core/utils/date.utils";
import { WorkerFarmAssignmentDialogComponent } from "./dialogs/worker-farm-assignment-dialog.component";
import { WorkerFarmAssignmentEndDialogComponent } from "./dialogs/worker-farm-assignment-end-dialog.component";
import {
  WorkerPaymentDialogComponent,
  WorkerPaymentDialogData,
} from "./dialogs/worker-payment-dialog.component";
import { WorkerPaymentCancelDialogComponent } from "./dialogs/worker-payment-cancel-dialog.component";

@Component({
  selector: "app-worker-detail-page",
  standalone: true,
  imports: [
    DatePipe,
    DecimalPipe,
    FormsModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatCardModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    MatTabsModule,
    MatTooltipModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: "./worker-detail-page.component.html",
  styleUrl: "./worker-detail-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkerDetailPageComponent implements OnInit {
  private readonly laborService = inject(LaborService);
  private readonly route = inject(ActivatedRoute);
  private readonly snack = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  private readonly breadcrumbService = inject(BreadcrumbService);
  readonly permissionService = inject(PermissionService);

  readonly workerId = this.route.snapshot.paramMap.get("id")!;

  readonly worker = signal<WorkerDetail | null>(null);
  readonly assignments = signal<readonly WorkerFarmAssignment[]>([]);
  readonly wageRates = signal<readonly LaborWageRate[]>([]);
  readonly payments = signal<readonly WorkerPayment[]>([]);
  readonly earnings = signal<readonly WorkerEarningsLedgerItem[]>([]);
  readonly allocations = signal<readonly WorkerPaymentAllocationItem[]>([]);
  readonly settlement = signal<WorkerSettlementCalculation | null>(null);

  readonly isLoading = signal(true);
  readonly isLoadingAssignments = signal(false);
  readonly isLoadingWageRates = signal(false);
  readonly isLoadingPayments = signal(false);
  readonly isLoadingSettlement = signal(false);
  readonly actionInProgress = signal(false);

  // Date range filter signals
  readonly periodFrom = signal<Date | null>(null);
  readonly periodTo = signal<Date | null>(null);
  readonly activePreset = signal<"all" | "this_month" | "last_month" | "last_30_days">("all");

  // Transaction table filter signals
  readonly selectedTxType = signal<string>("all");
  readonly selectedStatus = signal<string>("all");

  readonly currencySymbol = computed(() => {
    return this.settlement()?.currencySymbol || "₹";
  });

  readonly lastPayoutDate = computed<string | null>(() => {
    const payouts = this.payments()
      .filter((p) => p.status === "COMPLETED" && p.paymentType === "PAYOUT")
      .sort((a, b) => (b.paymentDate > a.paymentDate ? 1 : b.paymentDate < a.paymentDate ? -1 : 0));
    return payouts.length > 0 ? payouts[0].paymentDate : null;
  });

  readonly unallocatedPaymentsCount = computed(() => {
    const list = this.payments();
    const allocationsList = this.allocations();
    const hasUnsettled = (this.settlement()?.unsettledEarningCount ?? 0) > 0;
    if (!hasUnsettled) return 0;

    return list.filter((p) => {
      if (p.status !== "COMPLETED") return false;
      const pAllocations = allocationsList.filter((a) => a.workerPaymentId === p.id);
      const totalAllocated = pAllocations.reduce((acc, curr) => acc + curr.allocatedAmount, 0);
      return p.amount - totalAllocated > 0;
    }).length;
  });

  // Unified financial transaction stream combining payments and earnings
  readonly transactions = computed<readonly WorkerFinancialTransaction[]>(() => {
    const paymentsList = this.payments();
    const earningsList = this.earnings();
    const allocationsList = this.allocations();
    const symbol = this.currencySymbol();

    const results: WorkerFinancialTransaction[] = [];

    // 1. Process payments (Advance, Payout, Adjustment)
    for (const p of paymentsList) {
      const pAllocations = allocationsList.filter((a) => a.workerPaymentId === p.id);
      let relatedText: string | null = null;
      if (pAllocations.length > 0) {
        const totalAllocated = pAllocations.reduce((acc, curr) => acc + curr.allocatedAmount, 0);
        const dates = pAllocations
          .map((a) => a.earningDate)
          .filter(Boolean)
          .join(", ");
        relatedText = dates
          ? `Applied ${symbol}${totalAllocated.toFixed(2)} to earnings of ${dates}`
          : `Applied ${symbol}${totalAllocated.toFixed(2)} to wage settlement`;
        if (p.amount > totalAllocated) {
          relatedText += ` (${symbol}${(p.amount - totalAllocated).toFixed(2)} unallocated)`;
        }
      } else if (p.paymentType === "ADVANCE" && p.status === "COMPLETED") {
        relatedText = "Unapplied advance balance";
      } else if (p.paymentType === "PAYOUT" && p.status === "COMPLETED") {
        relatedText = "Unallocated payout balance";
      }

      let txType: FinancialTransactionType = "ADVANCE";
      let label = "Advance";
      if (p.paymentType === "PAYOUT") {
        const isFinal =
          (p.notes || "").toLowerCase().includes("final") ||
          (p.referenceNumber || "").toLowerCase().includes("final");
        txType = isFinal ? "FINAL_PAYOUT" : "PARTIAL_PAYOUT";
        label = isFinal ? "Final Payout" : "Partial Payout";
      } else if (p.paymentType === "ADJUSTMENT") {
        txType = "ADJUSTMENT";
        label = "Payment Adjustment";
      } else if (p.paymentType === "ADVANCE") {
        txType = "ADVANCE";
        label = "Advance";
      }

      results.push({
        id: p.id,
        date: p.paymentDate,
        transactionType: txType,
        typeLabel: label,
        description:
          p.notes ||
          (p.paymentType === "ADVANCE"
            ? "Cash advance to worker"
            : `${label} disbursed`),
        amount: p.amount,
        isEarning: false,
        status: p.status,
        paymentMethod: p.paymentMethod,
        referenceNumber: p.referenceNumber,
        relatedEarningOrAllocation: relatedText,
        rawPayment: p,
      });
    }

    // 2. Process earnings ledger items
    for (const e of earningsList) {
      const eAllocations = allocationsList.filter((a) => a.workerEarningsLedgerId === e.id);
      let relatedText: string | null = null;
      if (eAllocations.length > 0) {
        const settledAmt = eAllocations.reduce((acc, curr) => acc + curr.allocatedAmount, 0);
        relatedText = `Settled ${symbol}${settledAmt.toFixed(2)} via settlement`;
      } else {
        relatedText = "Unsettled earning entry";
      }

      results.push({
        id: e.id,
        date: e.earningsDate,
        transactionType: e.entryType === "ADJUSTMENT" ? "ADJUSTMENT" : "EARNING",
        typeLabel: e.entryType === "ADJUSTMENT" ? "Wage Adjustment" : "Attendance Earning",
        description:
          e.description ||
          `${formatWageType(e.wageType)} (${e.quantity} @ ${symbol}${e.wageRate})`,
        amount: e.grossAmount,
        isEarning: true,
        status: e.status,
        paymentMethod: null,
        referenceNumber: e.attendanceId ? `Att: ${e.attendanceId.slice(0, 8)}` : null,
        relatedEarningOrAllocation: relatedText,
        rawEarning: e,
      });
    }

    // Sort descending by date
    return results.sort((a, b) => (b.date > a.date ? 1 : b.date < a.date ? -1 : 0));
  });

  // Filtered transactions for the table
  readonly filteredTransactions = computed<readonly WorkerFinancialTransaction[]>(() => {
    const list = this.transactions();
    const typeFilter = this.selectedTxType();
    const statusFilter = this.selectedStatus();

    return list.filter((tx) => {
      if (typeFilter !== "all") {
        if (typeFilter === "PAYOUT") {
          if (tx.transactionType !== "PARTIAL_PAYOUT" && tx.transactionType !== "FINAL_PAYOUT") {
            return false;
          }
        } else if (tx.transactionType !== typeFilter) {
          return false;
        }
      }
      if (statusFilter !== "all") {
        if (statusFilter === "COMPLETED") {
          if (tx.status !== "COMPLETED" && tx.status !== "APPROVED") {
            return false;
          }
        } else if (tx.status !== statusFilter) {
          return false;
        }
      }
      return true;
    });
  });

  readonly assignmentFilter = signal<"all" | "active">("all");

  readonly filteredAssignments = computed(() => {
    const filter = this.assignmentFilter();
    const list = this.assignments();
    if (filter === "active") {
      return list.filter((a) => a.isActive);
    }
    return list;
  });

  readonly activeAssignmentsCount = computed(
    () => this.assignments().filter((a) => a.isActive).length,
  );

  readonly assignmentColumns: readonly string[] = [
    "farm",
    "assignedFrom",
    "assignedTo",
    "status",
    "notes",
    "actions",
  ];

  readonly wageRateColumns: readonly string[] = [
    "wageType",
    "wageRate",
    "effectiveFrom",
    "effectiveTo",
    "status",
    "notes",
  ];

  readonly transactionColumns: readonly string[] = [
    "date",
    "transactionType",
    "description",
    "amount",
    "status",
    "paymentMethod",
    "reference",
    "relatedAllocation",
    "actions",
  ];

  ngOnInit(): void {
    this.loadWorker();
  }

  loadWorker(): void {
    this.isLoading.set(true);

    forkJoin({
      worker: this.laborService.getWorker(this.workerId),
      assignments: this.laborService.listWorkerFarmAssignments(this.workerId),
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: ({ worker, assignments }) => {
          this.worker.set(worker);
          this.assignments.set(assignments);

          this.breadcrumbService.setEntityName(worker.id, worker.displayName);
          this.breadcrumbService.setTrail([
            { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
            { label: "Labor", route: "/labor" },
            { label: "Workers", route: "/labor/workers" },
            { label: worker.displayName },
          ]);

          // Load applicable wage rates based on worker gender
          this.loadWageRates(worker.gender);
          // Load unified financial settlement and transaction records
          this.loadFinancials();
        },
        error: (error: unknown) => {
          this.snack.open(
            getApiErrorMessage(error, "Worker details could not be loaded."),
            "Dismiss",
            { duration: 5000 },
          );
        },
      });
  }

  loadAssignments(): void {
    this.isLoadingAssignments.set(true);
    this.laborService
      .listWorkerFarmAssignments(this.workerId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoadingAssignments.set(false)),
      )
      .subscribe({
        next: (items) => this.assignments.set(items),
        error: (err) => {
          this.snack.open(
            getApiErrorMessage(err, "Farm assignments could not be loaded."),
            "Dismiss",
            { duration: 4000 },
          );
        },
      });
  }

  loadWageRates(gender: string): void {
    this.isLoadingWageRates.set(true);
    this.laborService
      .listWageRates(gender, true)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoadingWageRates.set(false)),
      )
      .subscribe({
        next: (rates) => this.wageRates.set(rates),
        error: () => this.wageRates.set([]),
      });
  }

  loadFinancials(): void {
    this.isLoadingPayments.set(true);
    this.isLoadingSettlement.set(true);

    const fromDateStr = formatDateOnly(this.periodFrom());
    const toDateStr = formatDateOnly(this.periodTo());

    forkJoin({
      settlement: this.laborService.getWorkerSettlement(this.workerId, fromDateStr, toDateStr),
      payments: this.laborService.listWorkerPayments(this.workerId, fromDateStr, toDateStr),
      earnings: this.laborService.listWorkerEarnings(this.workerId, fromDateStr, toDateStr, undefined, undefined, 1, 100),
      allocations: this.laborService.listWorkerPaymentAllocations(this.workerId),
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.isLoadingPayments.set(false);
          this.isLoadingSettlement.set(false);
        }),
      )
      .subscribe({
        next: ({ settlement, payments, earnings, allocations }) => {
          this.settlement.set(settlement);
          this.payments.set(payments);
          this.earnings.set(earnings?.items || []);
          this.allocations.set(allocations || []);
        },
        error: (err) => {
          this.snack.open(
            getApiErrorMessage(err, "Financial data could not be loaded."),
            "Dismiss",
            { duration: 4000 },
          );
        },
      });
  }

  loadPayments(): void {
    this.loadFinancials();
  }

  loadSettlement(): void {
    this.loadFinancials();
  }

  setPreset(preset: "all" | "this_month" | "last_month" | "last_30_days"): void {
    this.activePreset.set(preset);
    const now = new Date();

    switch (preset) {
      case "all":
        this.periodFrom.set(null);
        this.periodTo.set(null);
        break;
      case "this_month":
        this.periodFrom.set(new Date(now.getFullYear(), now.getMonth(), 1));
        this.periodTo.set(new Date(now.getFullYear(), now.getMonth(), now.getDate()));
        break;
      case "last_month":
        this.periodFrom.set(new Date(now.getFullYear(), now.getMonth() - 1, 1));
        this.periodTo.set(new Date(now.getFullYear(), now.getMonth(), 0));
        break;
      case "last_30_days": {
        const past = new Date();
        past.setDate(now.getDate() - 30);
        this.periodFrom.set(past);
        this.periodTo.set(now);
        break;
      }
    }

    this.loadFinancials();
  }

  onDateRangeChange(): void {
    this.activePreset.set("all");
    this.loadFinancials();
  }

  clearDateRange(): void {
    this.setPreset("all");
  }

  openRecordPaymentDialog(mode: "ADVANCE" | "PARTIAL_PAYOUT" | "FINAL_PAYOUT" | "GENERAL" = "GENERAL"): void {
    const currentWorker = this.worker();
    const data: WorkerPaymentDialogData = {
      workerId: this.workerId,
      workerDisplayName: currentWorker?.displayName,
      mode,
    };

    const ref = this.dialog.open(WorkerPaymentDialogComponent, {
      width: "600px",
      data,
    });

    ref.afterClosed().subscribe((res) => {
      if (res) {
        this.loadFinancials();
      }
    });
  }

  openCancelPaymentDialog(payment: WorkerPayment): void {
    const ref = this.dialog.open(WorkerPaymentCancelDialogComponent, {
      width: "520px",
      data: {
        workerId: this.workerId,
        payment,
      },
    });

    ref.afterClosed().subscribe((res) => {
      if (res) {
        this.loadFinancials();
      }
    });
  }

  canAllocatePayment(p?: WorkerPayment): boolean {
    if (!p || p.status !== "COMPLETED") {
      return false;
    }
    const pAllocations = this.allocations().filter((a) => a.workerPaymentId === p.id);
    const totalAllocated = pAllocations.reduce((acc, curr) => acc + curr.allocatedAmount, 0);
    const remaining = p.amount - totalAllocated;
    const hasUnsettled = (this.settlement()?.unsettledEarningCount ?? 0) > 0;
    return remaining > 0 && hasUnsettled;
  }

  autoAllocatePayment(payment: WorkerPayment): void {
    this.actionInProgress.set(true);
    this.laborService
      .autoAllocatePayment(this.workerId, payment.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.actionInProgress.set(false)),
      )
      .subscribe({
        next: (allocations) => {
          const total = allocations.reduce((acc, a) => acc + a.allocatedAmount, 0);
          this.snack.open(
            `Successfully allocated ${this.currencySymbol()}${total.toFixed(2)} to settle pending earnings.`,
            "Dismiss",
            { duration: 4000 },
          );
          this.loadFinancials();
        },
        error: (err) => {
          this.snack.open(
            getApiErrorMessage(err, "Failed to allocate payment to earnings."),
            "Dismiss",
            { duration: 4000 },
          );
        },
      });
  }

  autoAllocateAll(): void {
    const unallocated = this.payments().filter((p) => this.canAllocatePayment(p));
    if (unallocated.length === 0) {
      return;
    }

    this.actionInProgress.set(true);
    const requests = unallocated.map((p) =>
      this.laborService.autoAllocatePayment(this.workerId, p.id).pipe(catchError(() => of([]))),
    );

    forkJoin(requests)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.actionInProgress.set(false)),
      )
      .subscribe({
        next: () => {
          this.snack.open("Successfully settled eligible pending earnings.", "Dismiss", { duration: 4000 });
          this.loadFinancials();
        },
        error: (err) => {
          this.snack.open(getApiErrorMessage(err, "Failed to settle earnings."), "Dismiss", { duration: 4000 });
        },
      });
  }

  toggleWorkerStatus(): void {
    const currentWorker = this.worker();
    if (!currentWorker) {
      return;
    }

    const nextActive = !currentWorker.isActive;
    this.actionInProgress.set(true);

    const action$ = nextActive
      ? this.laborService.activateWorker(currentWorker.id)
      : this.laborService.deactivateWorker(currentWorker.id);

    action$
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.actionInProgress.set(false)),
      )
      .subscribe({
        next: () => {
          this.snack.open(
            `Worker "${currentWorker.displayName}" ${nextActive ? "activated" : "deactivated"}.`,
            "Dismiss",
            { duration: 3000 },
          );
          this.laborService.getWorker(currentWorker.id).subscribe((w) => {
            this.worker.set(w);
          });
        },
        error: (err) => {
          this.snack.open(
            getApiErrorMessage(err, "Failed to update worker status."),
            "Dismiss",
            { duration: 5000 },
          );
        },
      });
  }

  openAssignFarmDialog(): void {
    const ref = this.dialog.open(WorkerFarmAssignmentDialogComponent, {
      width: "520px",
      data: { workerId: this.workerId },
    });

    ref.afterClosed().subscribe((result) => {
      if (result) {
        this.loadAssignments();
      }
    });
  }

  openEditAssignmentDialog(assignment: WorkerFarmAssignment): void {
    const ref = this.dialog.open(WorkerFarmAssignmentDialogComponent, {
      width: "520px",
      data: {
        workerId: this.workerId,
        assignment,
      },
    });

    ref.afterClosed().subscribe((result) => {
      if (result) {
        this.loadAssignments();
      }
    });
  }

  openEndAssignmentDialog(assignment: WorkerFarmAssignment): void {
    const ref = this.dialog.open(WorkerFarmAssignmentEndDialogComponent, {
      width: "480px",
      data: {
        workerId: this.workerId,
        assignment,
      },
    });

    ref.afterClosed().subscribe((result) => {
      if (result) {
        this.loadAssignments();
      }
    });
  }

  toggleAssignmentStatus(assignment: WorkerFarmAssignment): void {
    this.actionInProgress.set(true);

    const action$ = assignment.isActive
      ? this.laborService.deactivateWorkerFarmAssignment(
          this.workerId,
          assignment.id,
        )
      : this.laborService.activateWorkerFarmAssignment(
          this.workerId,
          assignment.id,
        );

    action$
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.actionInProgress.set(false)),
      )
      .subscribe({
        next: () => {
          this.snack.open(
            `Farm assignment for "${assignment.farmName}" ${assignment.isActive ? "deactivated" : "activated"}.`,
            "Dismiss",
            { duration: 3000 },
          );
          this.loadAssignments();
        },
        error: (err) => {
          this.snack.open(
            getApiErrorMessage(err, "Failed to update assignment status."),
            "Dismiss",
            { duration: 4000 },
          );
        },
      });
  }

  getGenderLabel(gender: string): string {
    return formatGender(gender);
  }

  getEmploymentTypeLabel(type: string): string {
    return formatEmploymentType(type);
  }

  formatWageType(type: string): string {
    switch (type?.toUpperCase()) {
      case "FULL_DAY":
        return "Full Day";
      case "HALF_DAY":
        return "Half Day";
      case "HOURLY":
        return "Hourly";
      case "MONTHLY":
        return "Monthly";
      default:
        return type || "—";
    }
  }

  formatPaymentType(type: string): string {
    switch (type?.toUpperCase()) {
      case "ADVANCE":
        return "Advance";
      case "PAYOUT":
        return "Payout";
      case "ADJUSTMENT":
        return "Adjustment";
      default:
        return type || "—";
    }
  }

  formatPaymentMethod(method: string): string {
    switch (method?.toUpperCase()) {
      case "CASH":
        return "Cash";
      case "BANK_TRANSFER":
        return "Bank Transfer";
      case "UPI":
        return "UPI";
      case "CHEQUE":
        return "Cheque";
      case "OTHER":
        return "Other";
      default:
        return method || "—";
    }
  }
}
