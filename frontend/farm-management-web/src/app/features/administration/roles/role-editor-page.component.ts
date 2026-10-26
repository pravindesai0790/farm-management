import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed, toSignal } from "@angular/core/rxjs-interop";
import {
  FormBuilder,
  FormControl,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatCheckboxModule } from "@angular/material/checkbox";
import { MatDividerModule } from "@angular/material/divider";
import { MatExpansionModule } from "@angular/material/expansion";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTooltipModule } from "@angular/material/tooltip";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import {
  forkJoin,
  finalize,
  Observable,
  of,
  startWith,
  switchMap,
} from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import { AdministrationService } from "../../../core/administration/administration.service";
import {
  Permission,
  Role,
} from "../../../core/administration/administration.models";
import {
  getApiValidationErrors,
} from "../../../core/models/api-error.model";
import { ErrorAlertComponent } from "../../../shared/components/error-alert/error-alert.component";

@Component({
  selector: "app-role-editor-page",
  standalone: true,
  imports: [
    ErrorAlertComponent,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatDividerModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: "./role-editor-page.component.html",
  styleUrl: "./role-editor-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoleEditorPageComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly administrationService = inject(AdministrationService);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);
  readonly roleId = this.activatedRoute.snapshot.paramMap.get("id");
  readonly isEditing = this.roleId !== null;
  readonly roleForm = this.formBuilder.nonNullable.group({
    name: ["", [Validators.required, Validators.maxLength(100)]],
    description: ["", Validators.maxLength(500)],
    permissionIds: this.formBuilder.nonNullable.control<string[]>([]),
  });
  readonly permissions = signal<readonly Permission[]>([]);
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<unknown>(null);
  readonly apiErrors = signal<Readonly<Record<string, readonly string[]>>>({});
  private initialPermissionIds: readonly string[] = [];

  readonly permissionSearchControl = new FormControl("", { nonNullable: true });
  readonly searchFilter = toSignal(
    this.permissionSearchControl.valueChanges.pipe(startWith("")),
    { initialValue: "" },
  );
  readonly selectedPermissionIds = signal<readonly string[]>([]);
  readonly selectedPermissionIdSet = computed(
    () => new Set(this.selectedPermissionIds()),
  );
  readonly expandedModules = signal<Set<string>>(new Set<string>());

  readonly filteredModules = computed(() => {
    const query = this.searchFilter().trim().toLowerCase();
    const allModules = this.modules();
    if (!query) return allModules;
    return allModules.filter((module) => {
      if (module.toLowerCase().includes(query)) return true;
      return this.permissionsForModule(module).some(
        (p) =>
          p.name.toLowerCase().includes(query) ||
          (p.description?.toLowerCase().includes(query) ?? false),
      );
    });
  });

  ngOnInit(): void {
    const permissionRequest$ = this.permissionService.has(
      "Roles.ManagePermissions",
    )
      ? this.administrationService.listPermissions()
      : of([] as readonly Permission[]);
    const roleRequest$ =
      this.roleId === null
        ? of(null)
        : this.administrationService.getRole(this.roleId);

    forkJoin({ permissions: permissionRequest$, role: roleRequest$ })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: ({ permissions, role }) => {
          this.permissions.set(permissions);
          if (role !== null) this.populateForm(role);
        },
        error: (error: unknown) => this.errorMessage.set(error),
      });
  }

  submit(): void {
    if (this.roleForm.invalid) {
      this.roleForm.markAllAsTouched();
      return;
    }
    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.apiErrors.set({});
    const value = this.roleForm.getRawValue();
    const save$: Observable<Role> = this.isEditing
      ? this.administrationService
          .updateRole(this.roleId!, {
            name: value.name.trim(),
            description: value.description.trim() || null,
          })
          .pipe(
            switchMap((role) =>
              this.shouldUpdatePermissions(value.permissionIds)
                ? this.administrationService.updateRolePermissions(role.id, {
                    permissionIds: value.permissionIds,
                  })
                : of(role),
            ),
          )
      : this.administrationService.createRole({
          name: value.name.trim(),
          description: value.description.trim() || null,
        });
    save$
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: (role) => {
          const permissionUpdate$ =
            !this.isEditing &&
            this.permissionService.has("Roles.ManagePermissions") &&
            value.permissionIds.length > 0
              ? this.administrationService.updateRolePermissions(role.id, {
                  permissionIds: value.permissionIds,
                })
              : of(role);
          permissionUpdate$.subscribe({
            next: () => {
              this.snackBar.open(
                this.isEditing ? "Role updated." : "Role created.",
                "Dismiss",
                { duration: 3000 },
              );
              void this.router.navigateByUrl("/administration/roles");
            },
            error: (error: unknown) => this.errorMessage.set(error),
          });
        },
        error: (error: unknown) => {
          this.errorMessage.set(error);
          this.apiErrors.set(getApiValidationErrors(error));
        },
      });
  }

  apiError(field: string): string | null {
    return this.apiErrors()[field]?.[0] ?? null;
  }

  permissionsForModule(module: string): readonly Permission[] {
    return this.permissions().filter(
      (permission) => permission.module === module,
    );
  }

  filteredPermissionsForModule(module: string): readonly Permission[] {
    const query = this.searchFilter().trim().toLowerCase();
    const perms = this.permissionsForModule(module);
    if (!query) return perms;
    if (module.toLowerCase().includes(query)) return perms;
    return perms.filter(
      (p) =>
        p.name.toLowerCase().includes(query) ||
        (p.description?.toLowerCase().includes(query) ?? false),
    );
  }

  modules(): readonly string[] {
    return [
      ...new Set(this.permissions().map((permission) => permission.module)),
    ].sort();
  }

  isPermissionSelected(permissionId: string): boolean {
    return this.selectedPermissionIdSet().has(permissionId);
  }

  togglePermission(permissionId: string, checked: boolean): void {
    const current = this.selectedPermissionIds();
    const next = checked
      ? [...current, permissionId]
      : current.filter((id) => id !== permissionId);
    this.syncPermissionSelection(next);
  }

  isModuleFullySelected(module: string): boolean {
    const modulePermissions = this.permissionsForModule(module);
    if (modulePermissions.length === 0) return false;
    const set = this.selectedPermissionIdSet();
    return modulePermissions.every((p) => set.has(p.id));
  }

  isModulePartiallySelected(module: string): boolean {
    const modulePermissions = this.permissionsForModule(module);
    const set = this.selectedPermissionIdSet();
    const count = modulePermissions.filter((p) => set.has(p.id)).length;
    return count > 0 && count < modulePermissions.length;
  }

  toggleModulePermissions(module: string, selectAll: boolean): void {
    const modulePermissionIds = new Set(
      this.permissionsForModule(module).map((p) => p.id),
    );
    const current = new Set(this.selectedPermissionIds());
    if (selectAll) {
      modulePermissionIds.forEach((id) => current.add(id));
    } else {
      modulePermissionIds.forEach((id) => current.delete(id));
    }
    this.syncPermissionSelection(Array.from(current));
  }

  getSelectedCountForModule(module: string): number {
    const set = this.selectedPermissionIdSet();
    return this.permissionsForModule(module).filter((p) => set.has(p.id)).length;
  }

  selectAllPermissions(): void {
    const allIds = this.permissions().map((p) => p.id);
    this.syncPermissionSelection(allIds);
  }

  clearAllPermissions(): void {
    this.syncPermissionSelection([]);
  }

  private syncPermissionSelection(ids: string[]): void {
    this.selectedPermissionIds.set(ids);
    this.roleForm.controls.permissionIds.setValue(ids);
    this.roleForm.controls.permissionIds.markAsDirty();
    this.roleForm.controls.permissionIds.markAsTouched();
  }

  isModuleExpanded(module: string): boolean {
    if (this.searchFilter().trim().length > 0) return true;
    return this.expandedModules().has(module);
  }

  setModuleExpanded(module: string, expanded: boolean): void {
    const current = new Set(this.expandedModules());
    if (expanded) {
      current.add(module);
    } else {
      current.delete(module);
    }
    this.expandedModules.set(current);
  }

  expandAll(): void {
    this.expandedModules.set(new Set(this.modules()));
  }

  collapseAll(): void {
    this.expandedModules.set(new Set());
  }

  private populateForm(role: Role): void {
    const permissionIds = role.permissions.map((permission) => permission.id);
    this.roleForm.patchValue({
      name: role.name,
      description: role.description ?? "",
      permissionIds,
    });
    this.selectedPermissionIds.set(permissionIds);
    this.initialPermissionIds = [...permissionIds].sort();

    const initialExpanded = new Set<string>();
    for (const permission of role.permissions) {
      initialExpanded.add(permission.module);
    }
    this.expandedModules.set(initialExpanded);

    if (!this.permissionService.has("Roles.ManagePermissions")) {
      this.roleForm.controls.permissionIds.disable();
      this.permissionSearchControl.disable();
    }
  }

  private shouldUpdatePermissions(permissionIds: readonly string[]): boolean {
    return (
      this.permissionService.has("Roles.ManagePermissions") &&
      JSON.stringify([...permissionIds].sort()) !==
        JSON.stringify(this.initialPermissionIds)
    );
  }
}
