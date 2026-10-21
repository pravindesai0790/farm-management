import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';
import { getApiErrorMessage } from '../../../../core/models/api-error.model';
import {
  FertilizerApplicationDetailsResponse,
  StartFertilizerApplicationRequest,
} from '../../../../core/fertilizers/fertilizer.models';
import { FertilizerService } from '../../../../core/fertilizers/fertilizer.service';

export interface StartFertilizerDialogData {
  application: FertilizerApplicationDetailsResponse;
}

@Component({
  selector: 'app-start-fertilizer-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './start-fertilizer-dialog.component.html',
  styleUrl: './start-fertilizer-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StartFertilizerDialogComponent {
  readonly data = inject<StartFertilizerDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<StartFertilizerDialogComponent>);
  private readonly fertilizerService = inject(FertilizerService);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly shortages = signal<readonly string[]>([]);

  get app(): FertilizerApplicationDetailsResponse {
    return this.data.application;
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onConfirmStart(): void {
    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.shortages.set([]);

    const req: StartFertilizerApplicationRequest = {
      version: this.app.version,
      idempotencyKey: crypto.randomUUID(),
    };

    this.fertilizerService
      .start(this.app.id, req)
      .pipe(
        finalize(() => this.isSubmitting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (updated) => {
          this.snack.open(
            `Fertilizer application '${updated.referenceNumber}' started successfully.`,
            'Dismiss',
            { duration: 4000 },
          );
          this.dialogRef.close(true);
        },
        error: (err) => {
          this.parseStartErrors(err);
        },
      });
  }

  private parseStartErrors(err: any): void {
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
      this.errorMessage.set('Application has been modified concurrently. Please refresh the page.');
    } else {
      this.errorMessage.set(getApiErrorMessage(err, 'Failed to start fertilizer application.'));
    }
  }
}
