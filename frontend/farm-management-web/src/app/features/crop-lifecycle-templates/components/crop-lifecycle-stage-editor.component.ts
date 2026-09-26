import {
  CdkDrag,
  CdkDragDrop,
  CdkDragHandle,
  CdkDropList,
  moveItemInArray,
} from "@angular/cdk/drag-drop";
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
} from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatChipsModule } from "@angular/material/chips";
import { MatIconModule } from "@angular/material/icon";
import { MatTooltipModule } from "@angular/material/tooltip";

export interface StageDraft {
  id?: string;
  stageName: string;
  sequenceNumber: number;
  expectedDurationDays: number | null;
  description: string | null;
  isActive: boolean;
}

@Component({
  selector: "app-crop-lifecycle-stage-editor",
  standalone: true,
  imports: [
    CdkDropList,
    CdkDrag,
    CdkDragHandle,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatTooltipModule,
  ],
  template: `
    <div class="stage-editor-container">
      <div class="stage-editor-header">
        <div>
          <h3>Stages & Sequence</h3>
          <p class="subtitle">
            Configure ordered growth stages for this lifecycle template. Drag or use arrows to reorder.
          </p>
        </div>
        <div class="header-actions">
          <span class="summary-chip" matTooltip="Total expected timeline duration">
            <mat-icon>schedule</mat-icon>
            {{ totalDurationDays() }} days (~{{ totalDurationMonths() }} months)
          </span>
          @if (editable()) {
            <button
              type="button"
              mat-flat-button
              color="primary"
              (click)="addStage.emit()"
            >
              <mat-icon>add</mat-icon>Add stage
            </button>
          }
        </div>
      </div>

      @if (stages().length === 0) {
        <div class="empty-stages-state">
          <mat-icon class="empty-icon">schema</mat-icon>
          <p>No growth stages defined yet.</p>
          @if (editable()) {
            <button
              type="button"
              mat-stroked-button
              color="primary"
              (click)="addStage.emit()"
            >
              <mat-icon>add</mat-icon>Add first stage
            </button>
          }
        </div>
      } @else {
        <div
          cdkDropList
          class="stage-list"
          [cdkDropListDisabled]="!editable()"
          (cdkDropListDropped)="onDrop($event)"
        >
          @for (stage of stages(); track stage.id || stage.stageName; let i = $index) {
            <div class="stage-card" cdkDrag [cdkDragDisabled]="!editable()">
              <div class="stage-left">
                @if (editable()) {
                  <button
                    type="button"
                    class="drag-handle"
                    cdkDragHandle
                    matTooltip="Drag to reorder stage"
                  >
                    <mat-icon>drag_indicator</mat-icon>
                  </button>
                }
                <span class="sequence-badge">#{{ i + 1 }}</span>
                <div class="stage-details">
                  <div class="stage-name-row">
                    <strong class="stage-name">{{ stage.stageName }}</strong>
                    @if (stage.expectedDurationDays) {
                      <span class="duration-badge">
                        <mat-icon>timer</mat-icon>{{ stage.expectedDurationDays }} days
                      </span>
                    } @else {
                      <span class="duration-badge no-duration">No duration set</span>
                    }
                    <span
                      class="status-pill"
                      [class.status-pill-inactive]="!stage.isActive"
                    >
                      {{ stage.isActive ? 'Active' : 'Inactive' }}
                    </span>
                  </div>
                  @if (stage.description) {
                    <p class="stage-desc">{{ stage.description }}</p>
                  }
                </div>
              </div>

              @if (editable()) {
                <div class="stage-actions">
                  <div class="reorder-buttons">
                    <button
                      type="button"
                      mat-icon-button
                      [disabled]="i === 0"
                      (click)="moveUp(i)"
                      matTooltip="Move stage up"
                    >
                      <mat-icon>keyboard_arrow_up</mat-icon>
                    </button>
                    <button
                      type="button"
                      mat-icon-button
                      [disabled]="i === stages().length - 1"
                      (click)="moveDown(i)"
                      matTooltip="Move stage down"
                    >
                      <mat-icon>keyboard_arrow_down</mat-icon>
                    </button>
                  </div>
                  <button
                    type="button"
                    mat-icon-button
                    color="primary"
                    (click)="editStage.emit({ stage, index: i })"
                    matTooltip="Edit stage"
                  >
                    <mat-icon>edit</mat-icon>
                  </button>
                  <button
                    type="button"
                    mat-icon-button
                    color="warn"
                    (click)="removeStage.emit(i)"
                    matTooltip="Remove stage"
                  >
                    <mat-icon>delete</mat-icon>
                  </button>
                </div>
              }
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: `
    .stage-editor-container {
      display: flex;
      flex-direction: column;
      gap: 1rem;
      margin-top: 1rem;
    }
    .stage-editor-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      flex-wrap: wrap;
      gap: 1rem;
      h3 {
        margin: 0;
        font-size: 1.15rem;
        font-weight: 600;
      }
      .subtitle {
        margin: 0.25rem 0 0 0;
        color: #64748b;
        font-size: 0.875rem;
      }
    }
    .header-actions {
      display: flex;
      align-items: center;
      gap: 1rem;
    }
    .summary-chip {
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
      background: #f1f5f9;
      color: #334155;
      padding: 0.35rem 0.75rem;
      border-radius: 9999px;
      font-size: 0.85rem;
      font-weight: 500;
      mat-icon {
        font-size: 1.1rem;
        width: 1.1rem;
        height: 1.1rem;
        color: #0284c7;
      }
    }
    .empty-stages-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 2.5rem 1rem;
      background: #fafafa;
      border: 2px dashed #e2e8f0;
      border-radius: 8px;
      color: #64748b;
      gap: 0.75rem;
      .empty-icon {
        font-size: 2.5rem;
        width: 2.5rem;
        height: 2.5rem;
        color: #94a3b8;
      }
    }
    .stage-list {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }
    .stage-card {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0.85rem 1.25rem;
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
      transition: box-shadow 0.2s, border-color 0.2s;
      &:hover {
        border-color: #cbd5e1;
        box-shadow: 0 3px 6px rgba(0, 0, 0, 0.06);
      }
    }
    .stage-left {
      display: flex;
      align-items: center;
      gap: 0.85rem;
      flex: 1;
    }
    .drag-handle {
      background: transparent;
      border: none;
      cursor: grab;
      color: #94a3b8;
      display: flex;
      align-items: center;
      padding: 0.25rem;
      border-radius: 4px;
      &:hover {
        background: #f1f5f9;
        color: #475569;
      }
    }
    .sequence-badge {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      min-width: 2rem;
      height: 2rem;
      background: #e0f2fe;
      color: #0369a1;
      font-weight: 700;
      font-size: 0.85rem;
      border-radius: 50%;
    }
    .stage-details {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }
    .stage-name-row {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      flex-wrap: wrap;
    }
    .stage-name {
      font-size: 1rem;
      color: #1e293b;
    }
    .duration-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
      font-size: 0.8rem;
      color: #0284c7;
      background: #f0f9ff;
      padding: 0.15rem 0.5rem;
      border-radius: 4px;
      border: 1px solid #bae6fd;
      mat-icon {
        font-size: 0.95rem;
        width: 0.95rem;
        height: 0.95rem;
      }
      &.no-duration {
        color: #64748b;
        background: #f8fafc;
        border-color: #e2e8f0;
      }
    }
    .status-pill {
      font-size: 0.75rem;
      font-weight: 600;
      padding: 0.15rem 0.5rem;
      border-radius: 9999px;
      background: #dcfce7;
      color: #166534;
      &.status-pill-inactive {
        background: #f1f5f9;
        color: #64748b;
      }
    }
    .stage-desc {
      margin: 0;
      font-size: 0.85rem;
      color: #64748b;
    }
    .stage-actions {
      display: flex;
      align-items: center;
      gap: 0.25rem;
    }
    .reorder-buttons {
      display: flex;
      align-items: center;
    }
    .cdk-drag-preview {
      box-sizing: border-box;
      border-radius: 8px;
      box-shadow: 0 5px 15px rgba(0, 0, 0, 0.15);
      background: #ffffff;
    }
    .cdk-drag-placeholder {
      opacity: 0.3;
    }
    .cdk-drag-animating {
      transition: transform 250ms cubic-bezier(0, 0, 0.2, 1);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CropLifecycleStageEditorComponent {
  readonly stages = input.required<readonly StageDraft[]>();
  readonly editable = input<boolean>(true);

  readonly addStage = output<void>();
  readonly editStage = output<{ stage: StageDraft; index: number }>();
  readonly removeStage = output<number>();
  readonly reorderStages = output<readonly StageDraft[]>();

  readonly totalDurationDays = computed(() => {
    return this.stages().reduce(
      (sum, s) => sum + (s.expectedDurationDays || 0),
      0,
    );
  });

  readonly totalDurationMonths = computed(() => {
    const days = this.totalDurationDays();
    return (days / 30.4).toFixed(1);
  });

  onDrop(event: CdkDragDrop<StageDraft[]>): void {
    if (!this.editable()) return;
    const current = [...this.stages()];
    moveItemInArray(current, event.previousIndex, event.currentIndex);
    const updated = current.map((s, idx) => ({ ...s, sequenceNumber: idx + 1 }));
    this.reorderStages.emit(updated);
  }

  moveUp(index: number): void {
    if (index <= 0 || !this.editable()) return;
    const current = [...this.stages()];
    const temp = current[index];
    current[index] = current[index - 1];
    current[index - 1] = temp;
    const updated = current.map((s, idx) => ({ ...s, sequenceNumber: idx + 1 }));
    this.reorderStages.emit(updated);
  }

  moveDown(index: number): void {
    if (index >= this.stages().length - 1 || !this.editable()) return;
    const current = [...this.stages()];
    const temp = current[index];
    current[index] = current[index + 1];
    current[index + 1] = temp;
    const updated = current.map((s, idx) => ({ ...s, sequenceNumber: idx + 1 }));
    this.reorderStages.emit(updated);
  }
}
