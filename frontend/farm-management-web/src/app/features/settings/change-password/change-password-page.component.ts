import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import {
  FormBuilder,
  ReactiveFormsModule,
  AbstractControl,
  ValidationErrors,
  Validators,
} from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSnackBar } from "@angular/material/snack-bar";
import { Router, RouterLink } from "@angular/router";
import { finalize } from "rxjs";

import { AuthService } from "../../../core/auth/auth.service";
import {
  getApiErrorMessage,
  getApiValidationErrors,
} from "../../../core/models/api-error.model";

function passwordsMatch(control: AbstractControl): ValidationErrors | null {
  return control.get("newPassword")?.value ===
    control.get("confirmPassword")?.value
    ? null
    : { passwordMismatch: true };
}

@Component({
  selector: "app-change-password-page",
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: "./change-password-page.component.html",
  styleUrl: "./change-password-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangePasswordPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly apiErrors = signal<Readonly<Record<string, readonly string[]>>>({});
  readonly passwordForm = this.formBuilder.nonNullable.group(
    {
      currentPassword: ["", Validators.required],
      newPassword: [
        "",
        [
          Validators.required,
          Validators.minLength(12),
          Validators.pattern(/[A-Z]/),
          Validators.pattern(/[a-z]/),
          Validators.pattern(/[0-9]/),
          Validators.pattern(/[^A-Za-z0-9]/),
        ],
      ],
      confirmPassword: ["", Validators.required],
    },
    { validators: passwordsMatch },
  );

  submit(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }
    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.apiErrors.set({});
    const value = this.passwordForm.getRawValue();
    this.authService
      .changePassword({
        currentPassword: value.currentPassword,
        newPassword: value.newPassword,
      })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: () => {
          this.authService.clearSession();
          this.snackBar.open(
            "Password changed. Please sign in again.",
            "Dismiss",
            { duration: 4000 },
          );
          void this.router.navigateByUrl("/login");
        },
        error: (error: unknown) => {
          this.errorMessage.set(
            getApiErrorMessage(error, "The password could not be changed."),
          );
          this.apiErrors.set(getApiValidationErrors(error));
        },
      });
  }

  apiError(field: string): string | null {
    return this.apiErrors()[field]?.[0] ?? null;
  }
}
