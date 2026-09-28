import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from "@angular/material/dialog";
import { MatIconModule } from "@angular/material/icon";

export interface CropCyclePlantationPromptDialogData {
  cycleName: string;
  cropName: string;
  cropDurationType: string;
  plantationName: string;
  farmAreaName?: string | null;
}

@Component({
  selector: "app-crop-cycle-plantation-prompt-dialog",
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatIconModule],
  template: `
    <div class="dialog-header">
      <mat-icon color="warn" class="header-icon">agriculture</mat-icon>
      <h2 mat-dialog-title>Terminate Plantation?</h2>
    </div>

    <mat-dialog-content>
      <p class="dialog-lead">
        Crop cycle <strong>{{ data.cycleName }}</strong> has been completed.
      </p>
      <p class="dialog-text">
        Since <strong>{{ data.cropName }}</strong> is a non-perennial crop
        (<em>{{ data.cropDurationType }}</em>), the production lifecycle for
        <strong>{{ data.plantationName }}</strong>
        {{ data.farmAreaName ? 'on ' + data.farmAreaName : '' }} has concluded.
      </p>
      <div class="callout-box">
        <mat-icon class="callout-icon">info</mat-icon>
        <div>
          <strong>Recommendation:</strong>
          Terminating the plantation will release the allocated area on this farm area so it can be reused for new plantings and crop cycles.
        </div>
      </div>
      <p class="dialog-subtext">
        Would you like to terminate this plantation now?
      </p>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="onKeepActive()">
        Keep Active (Decide Later)
      </button>
      <button mat-flat-button color="warn" type="button" (click)="onConfirmTerminate()">
        Terminate Plantation
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .dialog-header {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 1.5rem 1.5rem 0.5rem;
    }
    .header-icon {
      font-size: 2rem;
      width: 2rem;
      height: 2rem;
    }
    h2[mat-dialog-title] {
      margin: 0;
      padding: 0;
      font-size: 1.25rem;
      font-weight: 600;
    }
    mat-dialog-content {
      font-size: 0.95rem;
      line-height: 1.55;
      color: #374151;
    }
    .dialog-lead {
      font-size: 1rem;
      margin-bottom: 0.75rem;
    }
    .dialog-text {
      margin-bottom: 1rem;
    }
    .callout-box {
      display: flex;
      gap: 0.75rem;
      align-items: flex-start;
      background-color: #f3f4f6;
      border-left: 4px solid #3b82f6;
      border-radius: 4px;
      padding: 0.85rem 1rem;
      margin-bottom: 1rem;
      font-size: 0.88rem;
      color: #1e293b;
    }
    .callout-icon {
      color: #2563eb;
      font-size: 1.25rem;
      width: 1.25rem;
      height: 1.25rem;
      margin-top: 2px;
    }
    .dialog-subtext {
      font-weight: 500;
      color: #111827;
      margin-bottom: 0.5rem;
    }
    mat-dialog-actions {
      padding: 1rem 1.5rem;
      gap: 0.5rem;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CropCyclePlantationPromptDialogComponent {
  readonly data: CropCyclePlantationPromptDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(
    MatDialogRef<CropCyclePlantationPromptDialogComponent, boolean>,
  );

  onKeepActive(): void {
    this.dialogRef.close(false);
  }

  onConfirmTerminate(): void {
    this.dialogRef.close(true);
  }
}
