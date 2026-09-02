import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';

import { SystemPingResponse } from '../../core/models/system-ping-response.model';
import { SystemService } from '../../core/services/system.service';
import { AuthService } from '../../core/auth/auth.service';

type ApiStatus = 'checking' | 'connected' | 'unavailable';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [DatePipe, MatButtonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardPageComponent implements OnInit {
  readonly apiStatus = signal<ApiStatus>('checking');
  readonly pingResponse = signal<SystemPingResponse | null>(null);
  readonly lastError = signal<string | null>(null);

  private readonly destroyRef = inject(DestroyRef);
  private readonly snackBar = inject(MatSnackBar);
  private readonly systemService = inject(SystemService);
  private readonly authService = inject(AuthService);

  readonly currentUser = this.authService.user;

  ngOnInit(): void {
    this.refreshApiStatus();
  }

  refreshApiStatus(): void {
    this.apiStatus.set('checking');
    this.lastError.set(null);

    this.systemService.getPing()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.pingResponse.set(response);
          this.apiStatus.set('connected');
        },
        error: () => {
          this.pingResponse.set(null);
          this.apiStatus.set('unavailable');
          this.lastError.set('The API could not be reached. Check that the backend is running.');
          this.snackBar.open('API is currently unavailable.', 'Dismiss', { duration: 5000 });
        }
      });
  }
}
