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
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { finalize } from 'rxjs';

import { BreadcrumbService } from '../../../core/breadcrumb/breadcrumb.service';
import { PermissionService } from '../../../core/auth/permission.service';
import {
  FertilizerApplicationDetailsResponse,
  FertilizerApplicationProductDto,
  FertilizerApplicationStatus,
} from '../../../core/fertilizers/fertilizer.models';
import { FertilizerService } from '../../../core/fertilizers/fertilizer.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { getApiErrorMessage } from '../../../core/models/api-error.model';
import { ScheduleFertilizerDialogComponent } from '../dialogs/schedule-fertilizer-dialog/schedule-fertilizer-dialog.component';
import { StartFertilizerDialogComponent } from '../dialogs/start-fertilizer-dialog/start-fertilizer-dialog.component';
import { CompleteFertilizerDialogComponent } from '../dialogs/complete-fertilizer-dialog/complete-fertilizer-dialog.component';
import { CancelFertilizerDialogComponent } from '../dialogs/cancel-fertilizer-dialog/cancel-fertilizer-dialog.component';

export interface FertilizerTimelineStep {
  readonly id: string;
  readonly title: string;
  readonly icon: string;
  readonly date: string | null;
  readonly actor?: string | null;
  readonly note?: string | null;
  readonly state: 'completed' | 'current' | 'future' | 'cancelled';
}

@Component({
  selector: 'app-fertilizer-detail-page',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDividerModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './fertilizer-detail-page.component.html',
  styleUrl: './fertilizer-detail-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FertilizerDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fertilizerService = inject(FertilizerService);
  private readonly breadcrumbService = inject(BreadcrumbService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);

  readonly id = this.route.snapshot.paramMap.get('id') ?? '';

  readonly application = signal<FertilizerApplicationDetailsResponse | null>(null);
  readonly isLoading = signal(true);
  readonly isActionLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly hasPositiveActualUsage = computed(() => {
    const app = this.application();
    if (!app || !app.products) return false;
    return app.products.some((p) => p.actualQuantity !== null && p.actualQuantity > 0);
  });

  readonly timelineSteps = computed<readonly FertilizerTimelineStep[]>(() => {
    const app = this.application();
    if (!app) return [];

    const steps: FertilizerTimelineStep[] = [];

    // 1. Draft Created
    steps.push({
      id: 'created',
      title: 'Draft Created',
      icon: 'edit_note',
      date: app.createdAt,
      actor: app.createdByName || app.createdBy,
      state: 'completed',
    });

    // 2. Planned
    if (app.plannedDate || app.status !== 'Draft') {
      const isPastOrCurrent = app.status !== 'Draft';
      steps.push({
        id: 'planned',
        title: 'Application Planned',
        icon: 'event_note',
        date: app.plannedDate,
        state: isPastOrCurrent ? 'completed' : 'current',
      });
    }

    // 3. Scheduled
    if (app.scheduledDate || ['Scheduled', 'InProgress', 'Completed'].includes(app.status)) {
      const isPastOrCurrent = ['InProgress', 'Completed'].includes(app.status);
      steps.push({
        id: 'scheduled',
        title: 'Application Scheduled',
        icon: 'calendar_month',
        date: app.scheduledDate,
        state: isPastOrCurrent ? 'completed' : (app.status === 'Scheduled' ? 'current' : 'completed'),
      });
    }

    // 4. Started / In Progress
    if (app.startedAt || ['InProgress', 'Completed'].includes(app.status)) {
      const isCompleted = app.status === 'Completed';
      steps.push({
        id: 'started',
        title: 'Execution Started',
        icon: 'play_circle',
        date: app.startedAt,
        actor: app.startedByName || app.startedBy,
        state: isCompleted ? 'completed' : 'current',
      });
    }

    // 5. Terminal State: Completed OR Cancelled
    if (app.status === 'Completed') {
      steps.push({
        id: 'completed',
        title: 'Application Completed & Stock Deducted',
        icon: 'check_circle',
        date: app.completedAt || app.actualApplicationDate,
        actor: app.completedByName || app.completedBy,
        state: 'completed',
      });
    } else if (app.status === 'Cancelled') {
      steps.push({
        id: 'cancelled',
        title: 'Application Cancelled',
        icon: 'cancel',
        date: app.cancelledAt,
        actor: app.cancelledByName || app.cancelledBy,
        note: app.cancellationReason ? `Reason: ${app.cancellationReason}` : undefined,
        state: 'cancelled',
      });
    }

    return steps;
  });

  ngOnInit(): void {
    if (!this.id) {
      this.errorMessage.set('No fertilizer application ID specified.');
      this.isLoading.set(false);
      return;
    }

    this.loadDetails();
  }

  loadDetails(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.fertilizerService
      .get(this.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (res) => {
          this.application.set(res);
          if (res.referenceNumber) {
            this.breadcrumbService.setEntityName(this.id, res.referenceNumber);
          }
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, 'Failed to load fertilizer application details.');
          this.errorMessage.set(msg);
          this.snack.open(msg, 'Dismiss', { duration: 4000 });
        },
      });
  }

  onEdit(): void {
    this.router.navigate(['/fertilizer-applications', this.id, 'edit']);
  }

  onSchedule(isReschedule: boolean = false): void {
    const app = this.application();
    if (!app) return;

    const ref = this.dialog.open(ScheduleFertilizerDialogComponent, {
      width: '500px',
      data: {
        applicationId: app.id,
        referenceNumber: app.referenceNumber || 'Draft',
        farmName: app.farmName,
        status: app.status,
        currentScheduledDate: app.scheduledDate,
        isReschedule,
        version: app.version,
      },
    });

    ref.afterClosed().subscribe((scheduled) => {
      if (scheduled) {
        this.loadDetails();
      }
    });
  }

  onStart(): void {
    const app = this.application();
    if (!app) return;

    const ref = this.dialog.open(StartFertilizerDialogComponent, {
      width: '650px',
      data: { application: app },
    });

    ref.afterClosed().subscribe((started) => {
      if (started) {
        this.loadDetails();
      }
    });
  }

  onComplete(): void {
    const app = this.application();
    if (!app) return;

    const ref = this.dialog.open(CompleteFertilizerDialogComponent, {
      width: '720px',
      data: { application: app },
    });

    ref.afterClosed().subscribe((completed) => {
      if (completed) {
        this.loadDetails();
      }
    });
  }

  onCancel(): void {
    const app = this.application();
    if (!app) return;

    const ref = this.dialog.open(CancelFertilizerDialogComponent, {
      width: '720px',
      data: { application: app },
    });

    ref.afterClosed().subscribe((cancelled) => {
      if (cancelled) {
        this.loadDetails();
      }
    });
  }

  onDeleteDraft(): void {
    const app = this.application();
    if (!app || app.status !== 'Draft') return;

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Delete Fertilizer Draft',
        message: `Permanently delete draft '${app.referenceNumber}'? This action cannot be undone.`,
        confirmText: 'Delete Draft',
        cancelText: 'Keep Draft',
        color: 'warn',
        icon: 'delete_forever',
      },
    });

    dialogRef.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;

      this.isActionLoading.set(true);
      this.fertilizerService
        .deleteDraft(this.id)
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isActionLoading.set(false)),
        )
        .subscribe({
          next: () => {
            this.snack.open(`Draft '${app.referenceNumber}' deleted successfully.`, 'OK', {
              duration: 3500,
            });
            this.router.navigate(['/fertilizer-applications']);
          },
          error: (err) => {
            const msg = getApiErrorMessage(err, 'Failed to delete fertilizer draft.');
            this.snack.open(msg, 'Dismiss', { duration: 5000 });
          },
        });
    });
  }

  onCopyAsNewDraft(): void {
    const app = this.application();
    if (!app) return;

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Copy as New Draft',
        message: `Create a new Draft copy of application '${app.referenceNumber}'? Operational context, method, purpose, and planned products will be duplicated into a separate Draft record.`,
        confirmText: 'Create Copy',
        cancelText: 'Cancel',
        color: 'primary',
        icon: 'content_copy',
      },
    });

    dialogRef.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;

      this.isActionLoading.set(true);
      this.fertilizerService
        .copyAsNewDraft(this.id)
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.isActionLoading.set(false)),
        )
        .subscribe({
          next: (newDraft) => {
            this.snack
              .open(
                `New draft '${newDraft.referenceNumber}' created from '${app.referenceNumber}'.`,
                'Open Draft',
                { duration: 5000 },
              )
              .onAction()
              .subscribe(() => {
                this.router.navigate(['/fertilizer-applications', newDraft.id, 'edit']);
              });
          },
          error: (err) => {
            const msg = getApiErrorMessage(err, 'Failed to copy fertilizer application.');
            this.snack.open(msg, 'Dismiss', { duration: 5000 });
          },
        });
    });
  }

  getStatusBadgeClass(status: FertilizerApplicationStatus | string | undefined): string {
    const s = (status || '').toLowerCase();
    switch (s) {
      case 'draft':
        return 'status-badge--draft';
      case 'planned':
        return 'status-badge--planned';
      case 'scheduled':
        return 'status-badge--scheduled';
      case 'inprogress':
        return 'status-badge--inprogress';
      case 'completed':
        return 'status-badge--completed';
      case 'cancelled':
        return 'status-badge--cancelled';
      default:
        return 'status-badge--draft';
    }
  }

  getStatusDisplayName(status: FertilizerApplicationStatus | string | undefined): string {
    const s = (status || '').toLowerCase();
    switch (s) {
      case 'draft':
        return 'Draft';
      case 'planned':
        return 'Planned';
      case 'scheduled':
        return 'Scheduled';
      case 'inprogress':
        return 'In Progress';
      case 'completed':
        return 'Completed';
      case 'cancelled':
        return 'Cancelled';
      default:
        return status || 'Unknown';
    }
  }

  calculateVariance(product: FertilizerApplicationProductDto): {
    val: number | null;
    pct: number | null;
    direction: 'over' | 'under' | 'match' | 'none';
  } {
    if (product.actualQuantity === null || product.plannedQuantity === null) {
      return { val: null, pct: null, direction: 'none' };
    }
    const diff = product.actualQuantity - product.plannedQuantity;
    const pct = product.plannedQuantity > 0 ? (diff / product.plannedQuantity) * 100 : 0;
    const direction = diff > 0 ? 'over' : diff < 0 ? 'under' : 'match';
    return { val: diff, pct, direction };
  }
}
