import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import {
  getApiErrorMessage,
  getApiValidationMessages,
} from '../../../core/models/api-error.model';

@Component({
  selector: 'app-error-alert',
  standalone: true,
  imports: [MatIconModule],
  template: `
    @if (validationErrors().length > 0) {
      <div class="error-alert" role="alert">
        <mat-icon class="error-icon">error_outline</mat-icon>
        <div class="error-content">
          @if (validationErrors().length === 1) {
            <p class="error-message">{{ validationErrors()[0] }}</p>
          } @else {
            <ul class="error-list">
              @for (err of validationErrors(); track err) {
                <li>{{ err }}</li>
              }
            </ul>
          }
        </div>
      </div>
    } @else if (fallbackOrMessage(); as message) {
      <div class="error-alert" role="alert">
        <mat-icon class="error-icon">error_outline</mat-icon>
        <div class="error-content">
          <p class="error-message">{{ message }}</p>
        </div>
      </div>
    }
  `,
  styleUrl: './error-alert.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ErrorAlertComponent {
  readonly error = input<unknown>(null);
  readonly fallback = input<string | null>(null);

  protected readonly hasError = computed(() => {
    const err = this.error();
    return err !== null && err !== undefined && err !== '';
  });

  protected readonly validationErrors = computed(() => {
    if (!this.hasError()) {
      return [];
    }
    return getApiValidationMessages(this.error());
  });

  protected readonly fallbackOrMessage = computed(() => {
    if (!this.hasError()) {
      return null;
    }
    const msg = getApiErrorMessage(this.error(), this.fallback() ?? '');
    return msg.trim() ? msg.trim() : null;
  });
}

