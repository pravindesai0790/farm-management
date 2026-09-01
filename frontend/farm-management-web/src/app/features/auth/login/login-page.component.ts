import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    ReactiveFormsModule
  ],
  templateUrl: './login-page.component.html',
  styleUrl: './login-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly activatedRoute = inject(ActivatedRoute);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly loginForm = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  submit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.authService.login(this.loginForm.getRawValue()).subscribe({
      next: () => {
        void this.router.navigateByUrl(this.getReturnUrl());
      },
      error: (error: unknown) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(this.getErrorMessage(error));
      }
    });
  }

  private getReturnUrl(): string {
    const returnUrl = this.activatedRoute.snapshot.queryParamMap.get('returnUrl');
    return returnUrl !== null && returnUrl.startsWith('/') && !returnUrl.startsWith('//')
      ? returnUrl
      : '/dashboard';
  }

  private getErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse && error.status === 400) {
      const apiMessage = this.getApiMessage(error.error);
      return apiMessage ?? 'Enter a valid email and password.';
    }

    if (error instanceof HttpErrorResponse && error.status === 401) {
      return 'The email or password is incorrect.';
    }

    return 'Unable to sign in right now. Please try again.';
  }

  private getApiMessage(errorBody: unknown): string | null {
    if (typeof errorBody !== 'object' || errorBody === null || !('message' in errorBody)) {
      return null;
    }

    const message = errorBody.message;
    return typeof message === 'string' && message.length > 0 ? message : null;
  }
}

