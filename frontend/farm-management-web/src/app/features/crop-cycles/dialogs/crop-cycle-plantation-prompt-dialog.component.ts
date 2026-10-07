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
      gap: 8px;
      padding: 18px 24px 10px;
    }
    .header-icon {
      font-size: 22px;
      width: 22px;
      height: 22px;
    }
    h2[mat-dialog-title] {
      margin: 0;
      padding: 0;
      font-size: 1.15rem;
      font-weight: 600;
    }
    mat-dialog-content {
      font-size: 0.875rem;
      line-height: 1.5;
      color: #374151;
      padding: 8px 24px 12px !important;
    }
    .dialog-lead {
      font-size: 0.95rem;
      margin-bottom: 8px;
    }
    .dialog-text {
      margin-bottom: 12px;
    }
    .callout-box {
      display: flex;
      gap: 10px;
      align-items: flex-start;
      background-color: #f0f9ff;
      border-left: 3px solid #0284c7;
      border-radius: 4px;
      padding: 10px 14px;
      margin-bottom: 12px;
      font-size: 0.825rem;
      color: #0369a1;
    }
    .callout-icon {
      color: #0284c7;
      font-size: 18px;
      width: 18px;
      height: 18px;
      margin-top: 1px;
      flex-shrink: 0;
    }
    .dialog-subtext {
      font-weight: 500;
      color: #111827;
      margin-bottom: 4px;
    }
    mat-dialog-actions {
      padding: 12px 24px 18px;
      gap: 8px;

      button {
        height: 36px;
        font-size: 0.85rem;
      }
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
