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
  templateUrl: "./crop-lifecycle-stage-editor.component.html",
  styleUrl: "./crop-lifecycle-stage-editor.component.scss",
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
