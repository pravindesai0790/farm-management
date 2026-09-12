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
import { MatButtonModule } from "@angular/material/button";
import { MatButtonToggleModule } from "@angular/material/button-toggle";
import { MatCardModule } from "@angular/material/card";
import { MatDialog, MatDialogModule } from "@angular/material/dialog";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTableModule } from "@angular/material/table";
import { MatTabsModule } from "@angular/material/tabs";
import { MatTooltipModule } from "@angular/material/tooltip";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { finalize, forkJoin } from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import { BreadcrumbService } from "../../../core/breadcrumb/breadcrumb.service";
import {
  LaborWageRate,
  WorkerDetail,
  WorkerFarmAssignment,
  WorkerPayment,
  WorkerSettlementCalculation,
  formatEmploymentType,
  formatGender,
} from "../../../core/labor/labor.models";
import { LaborService } from "../../../core/labor/labor.service";
import { getApiErrorMessage } from "../../../core/models/api-error.model";
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
    MatButtonModule,
    MatButtonToggleModule,
    MatCardModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatTabsModule,
    MatTooltipModule,
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
  readonly settlement = signal<WorkerSettlementCalculation | null>(null);

  readonly isLoading = signal(true);
  readonly isLoadingAssignments = signal(false);
  readonly isLoadingWageRates = signal(false);
  readonly isLoadingPayments = signal(false);
  readonly isLoadingSettlement = signal(false);
  readonly actionInProgress = signal(false);

  readonly currencySymbol = computed(() => {
    return this.settlement()?.currencySymbol || "₹";
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

  readonly paymentColumns: readonly string[] = [
    "paymentDate",
    "paymentType",
    "amount",
    "paymentMethod",
    "referenceNumber",
    "status",
    "notes",
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
          // Load settlement summary
          this.loadSettlement();
          // Load payment history
          this.loadPayments();
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

  loadPayments(): void {
    this.isLoadingPayments.set(true);
    this.laborService
      .listWorkerPayments(this.workerId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoadingPayments.set(false)),
      )
      .subscribe({
        next: (records) => this.payments.set(records),
        error: () => this.payments.set([]),
      });
  }

  loadSettlement(): void {
    this.isLoadingSettlement.set(true);
    this.laborService
      .getWorkerSettlement(this.workerId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoadingSettlement.set(false)),
      )
      .subscribe({
        next: (res) => this.settlement.set(res),
        error: () => this.settlement.set(null),
      });
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
        this.loadSettlement();
        this.loadPayments();
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
        this.loadSettlement();
        this.loadPayments();
      }
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
