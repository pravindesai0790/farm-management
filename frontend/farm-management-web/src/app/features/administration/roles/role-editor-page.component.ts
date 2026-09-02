import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
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
import { AdministrationService } from '../../../core/administration/administration.service';
import { Permission, Role } from '../../../core/administration/administration.models';
import { getApiErrorMessage, getApiValidationErrors } from '../../../core/models/api-error.model';

@Component({
  selector: 'app-role-editor-page',
  standalone: true,
  imports: [MatButtonModule, MatCardModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule, MatSelectModule, ReactiveFormsModule, RouterLink],
  templateUrl: './role-editor-page.component.html',
  styleUrl: './role-editor-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RoleEditorPageComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly administrationService = inject(AdministrationService);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);
  readonly roleId = this.activatedRoute.snapshot.paramMap.get('id');
  readonly isEditing = this.roleId !== null;
  readonly roleForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', Validators.maxLength(500)],
    permissionIds: this.formBuilder.nonNullable.control<string[]>([])
  });
  readonly permissions = signal<readonly Permission[]>([]);
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly apiErrors = signal<Readonly<Record<string, readonly string[]>>>({});
  private initialPermissionIds: readonly string[] = [];

  ngOnInit(): void {
    const permissionRequest$ = this.permissionService.has('Roles.ManagePermissions') ? this.administrationService.listPermissions() : of([] as readonly Permission[]);
    const roleRequest$ = this.roleId === null ? of(null) : this.administrationService.getRole(this.roleId);
    forkJoin({ permissions: permissionRequest$, role: roleRequest$ }).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.isLoading.set(false))).subscribe({
      next: ({ permissions, role }) => { this.permissions.set(permissions); if (role !== null) this.populateForm(role); },
      error: (error: unknown) => this.errorMessage.set(getApiErrorMessage(error, 'The role could not be loaded.'))
    });
  }

  submit(): void {
    if (this.roleForm.invalid) { this.roleForm.markAllAsTouched(); return; }
    this.isSubmitting.set(true); this.errorMessage.set(null); this.apiErrors.set({});
    const value = this.roleForm.getRawValue();
    const save$: Observable<Role> = this.isEditing
      ? this.administrationService.updateRole(this.roleId!, { name: value.name.trim(), description: value.description.trim() || null }).pipe(
        switchMap((role) => this.shouldUpdatePermissions(value.permissionIds) ? this.administrationService.updateRolePermissions(role.id, { permissionIds: value.permissionIds }) : of(role)))
      : this.administrationService.createRole({ name: value.name.trim(), description: value.description.trim() || null });
    save$.pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.isSubmitting.set(false))).subscribe({
      next: (role) => {
        const permissionUpdate$ = !this.isEditing && this.permissionService.has('Roles.ManagePermissions') && value.permissionIds.length > 0
          ? this.administrationService.updateRolePermissions(role.id, { permissionIds: value.permissionIds })
          : of(role);
        permissionUpdate$.subscribe({
          next: () => { this.snackBar.open(this.isEditing ? 'Role updated.' : 'Role created.', 'Dismiss', { duration: 3000 }); void this.router.navigateByUrl('/administration/roles'); },
          error: (error: unknown) => { this.errorMessage.set(getApiErrorMessage(error, 'The role was saved, but permissions could not be updated.')); }
        });
      },
      error: (error: unknown) => { this.errorMessage.set(getApiErrorMessage(error, 'The role could not be saved.')); this.apiErrors.set(getApiValidationErrors(error)); }
    });
  }

  apiError(field: string): string | null { return this.apiErrors()[field]?.[0] ?? null; }

  permissionsForModule(module: string): readonly Permission[] { return this.permissions().filter((permission) => permission.module === module); }
  modules(): readonly string[] { return [...new Set(this.permissions().map((permission) => permission.module))].sort(); }

  private populateForm(role: Role): void {
    const permissionIds = role.permissions.map((permission) => permission.id);
    this.roleForm.patchValue({ name: role.name, description: role.description ?? '', permissionIds });
    this.initialPermissionIds = [...permissionIds].sort();
    if (!this.permissionService.has('Roles.ManagePermissions')) this.roleForm.controls.permissionIds.disable();
  }

  private shouldUpdatePermissions(permissionIds: readonly string[]): boolean {
    return this.permissionService.has('Roles.ManagePermissions') && JSON.stringify([...permissionIds].sort()) !== JSON.stringify(this.initialPermissionIds);
  }
}

