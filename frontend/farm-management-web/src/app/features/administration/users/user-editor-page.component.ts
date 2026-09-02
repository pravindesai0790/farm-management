import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, ValidationErrors, AbstractControl, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin, finalize, Observable, of, switchMap } from 'rxjs';

import { PermissionService } from '../../../core/auth/permission.service';
import { AuthService } from '../../../core/auth/auth.service';
import { AdministrationService } from '../../../core/administration/administration.service';
import { Role, User } from '../../../core/administration/administration.models';
import { getApiErrorMessage, getApiValidationErrors } from '../../../core/models/api-error.model';
import { FarmManagementService } from '../../../core/farm-management/farm-management.service';
import { Organization } from '../../../core/farm-management/farm-management.models';

function passwordsMatch(control: AbstractControl): ValidationErrors | null {
  const password = control.get('password')?.value as string | undefined;
  const confirmation = control.get('confirmPassword')?.value as string | undefined;
  return password === confirmation ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-user-editor-page',
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    ReactiveFormsModule,
    RouterLink
  ],
  templateUrl: './user-editor-page.component.html',
  styleUrl: './user-editor-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserEditorPageComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly administrationService = inject(AdministrationService);
  private readonly authService = inject(AuthService);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly farmManagementService = inject(FarmManagementService);
  readonly permissionService = inject(PermissionService);

  readonly userForm = this.formBuilder.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
    password: ['', [Validators.required, Validators.minLength(12), Validators.pattern(/[A-Z]/), Validators.pattern(/[a-z]/), Validators.pattern(/[0-9]/), Validators.pattern(/[^A-Za-z0-9]/)]],
    confirmPassword: ['', Validators.required],
    organizationId: ['', Validators.pattern(/^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i)],
    roleIds: this.formBuilder.nonNullable.control<string[]>([])
  }, { validators: passwordsMatch });

  readonly roles = signal<readonly Role[]>([]);
  readonly organizations = signal<readonly Organization[]>([]);
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly apiErrors = signal<Readonly<Record<string, readonly string[]>>>({});
  readonly userId = this.activatedRoute.snapshot.paramMap.get('id');
  readonly isEditing = this.userId !== null;
  readonly isGlobalAdministrator = this.authService.user()?.roles.includes('SuperAdmin') ?? false;
  private initialRoleIds: readonly string[] = [];

  ngOnInit(): void {
    if (!this.isEditing && this.isGlobalAdministrator) {
      this.userForm.controls.organizationId.addValidators(Validators.required);
      this.userForm.controls.organizationId.updateValueAndValidity();
    }
    const roles$ = this.administrationService.listRoles(true);
    const organizations$ = !this.isEditing && this.isGlobalAdministrator
      ? this.farmManagementService.listOrganizations()
      : of({ items: [] as readonly Organization[] });
    const user$ = this.userId === null ? of(null) : this.administrationService.getUser(this.userId);
    forkJoin({ roles: roles$, user: user$, organizations: organizations$ }).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.isLoading.set(false))
    ).subscribe({
      next: ({ roles, user, organizations }) => {
        this.roles.set(roles);
        this.organizations.set(organizations.items);
        if (user !== null) {
          this.populateForm(user);
        } else if (!this.permissionService.has('Users.ManageRoles')) {
          this.userForm.controls.roleIds.disable();
        }
      },
      error: (error: unknown) => {
        this.errorMessage.set(getApiErrorMessage(error, 'The user could not be loaded.'));
      }
    });
  }

  submit(): void {
    if (this.userForm.invalid) {
      this.userForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.apiErrors.set({});
    const value = this.userForm.getRawValue();
    const roleIds = this.userForm.controls.roleIds.value;
    const request$: Observable<User> = this.isEditing
      ? this.administrationService.updateUser(this.userId!, { firstName: value.firstName.trim(), lastName: value.lastName.trim() }).pipe(
        switchMap((user) => this.shouldUpdateRoles(roleIds)
          ? this.administrationService.assignUserRoles(user.id, { roleIds })
          : of(user))
      )
      : this.administrationService.createUser({
        firstName: value.firstName.trim(),
        lastName: value.lastName.trim(),
        email: value.email.trim(),
        password: value.password,
        organizationId: this.isGlobalAdministrator && value.organizationId.trim().length > 0 ? value.organizationId.trim() : undefined,
        roleIds
      });

    request$.pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.isSubmitting.set(false))).subscribe({
      next: () => {
        this.snackBar.open(this.isEditing ? 'User updated.' : 'User created.', 'Dismiss', { duration: 3000 });
        void this.router.navigateByUrl('/administration/users');
      },
      error: (error: unknown) => {
        this.errorMessage.set(getApiErrorMessage(error, 'The user could not be saved.'));
        this.apiErrors.set(getApiValidationErrors(error));
      }
    });
  }

  apiError(field: string): string | null {
    return this.apiErrors()[field]?.[0] ?? null;
  }

  private populateForm(user: User): void {
    this.userForm.patchValue({
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      roleIds: user.roles.map((role) => role.id)
    });
    this.userForm.controls.email.disable();
    this.userForm.controls.password.clearValidators();
    this.userForm.controls.confirmPassword.clearValidators();
    this.userForm.controls.password.updateValueAndValidity();
    this.userForm.controls.confirmPassword.updateValueAndValidity();
    this.userForm.updateValueAndValidity();
    this.initialRoleIds = user.roles.map((role) => role.id).sort();
    if (!this.permissionService.has('Users.ManageRoles')) {
      this.userForm.controls.roleIds.disable();
    }
  }

  private shouldUpdateRoles(roleIds: readonly string[]): boolean {
    return this.permissionService.has('Users.ManageRoles') &&
      JSON.stringify([...roleIds].sort()) !== JSON.stringify(this.initialRoleIds);
  }
}
