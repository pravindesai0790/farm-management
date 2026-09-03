import { DatePipe } from "@angular/common";
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTableModule } from "@angular/material/table";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import {
  debounceTime,
  distinctUntilChanged,
  finalize,
  forkJoin,
  merge,
} from "rxjs";

import { PermissionService } from "../../core/auth/permission.service";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import { Organization } from "../../core/farm-management/farm-management.models";
import { getApiErrorMessage } from "../../core/models/api-error.model";

type StatusFilter = "all" | "active" | "inactive";

@Component({
  selector: "app-organization-page",
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: "./organization-page.component.html",
  styleUrl: "./organization-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrganizationPageComponent implements OnInit {
  private readonly service = inject(FarmManagementService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formBuilder = inject(FormBuilder);

  readonly permissionService = inject(PermissionService);
  readonly isCreating =
    this.route.snapshot.routeConfig?.path === "organization/new";
  readonly canCreateOrganization =
    this.permissionService.has("Organization.Create") &&
    this.permissionService.hasRole("SuperAdmin");
  readonly displayedColumns = ["name", "code", "status", "createdAt", "scope"];
  readonly organizations = signal<readonly Organization[]>([]);
  readonly organization = signal<Organization | null>(null);
  readonly isLoading = signal(false);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly searchTerm = signal("");
  readonly status = signal<StatusFilter>("all");
  readonly filteredOrganizations = computed(() => {
    const search = this.searchTerm().trim().toLowerCase();
    const status = this.status();
    return this.organizations().filter(
      (organization) =>
        (search.length === 0 ||
          organization.name.toLowerCase().includes(search) ||
          organization.code.toLowerCase().includes(search)) &&
        (status === "all" ||
          (status === "active"
            ? organization.isActive
            : !organization.isActive)),
    );
  });
  readonly filterForm = this.formBuilder.nonNullable.group({
    search: [""],
    status: ["all" as StatusFilter],
  });
  readonly form = this.formBuilder.nonNullable.group({
    name: ["", [Validators.required, Validators.maxLength(200)]],
    code: ["", [Validators.required, Validators.maxLength(50)]],
  });

  ngOnInit(): void {
    if (this.isCreating) return;

    merge(
      this.filterForm.controls.search.valueChanges.pipe(
        debounceTime(300),
        distinctUntilChanged(),
      ),
      this.filterForm.controls.status.valueChanges,
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.searchTerm.set(this.filterForm.controls.search.value);
        this.status.set(this.filterForm.controls.status.value);
      });

    this.isLoading.set(true);
    forkJoin({
      organizations: this.service.listOrganizations(),
      currentOrganization: this.service.getOrganization(),
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (result) => {
          this.organizations.set(result.organizations.items);
          this.setCurrentOrganization(result.currentOrganization);
        },
        error: (error) =>
          this.errorMessage.set(
            getApiErrorMessage(error, "Organizations could not be loaded."),
          ),
      });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    const value = this.form.getRawValue();
    const request = this.isCreating
      ? this.service.createOrganization(value)
      : this.service.updateOrganization(value);
    request
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: (organization) => {
          this.setCurrentOrganization(organization);
          this.snackBar.open(
            this.isCreating ? "Organization created." : "Organization updated.",
            "Dismiss",
            { duration: 3000 },
          );
          void this.router.navigateByUrl("/organization");
        },
        error: (error) =>
          this.errorMessage.set(
            getApiErrorMessage(error, "Organization could not be saved."),
          ),
      });
  }

  changeStatus(active: boolean): void {
    this.isSubmitting.set(true);
    const request = active
      ? this.service.activateOrganization()
      : this.service.deactivateOrganization();
    request
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: () => {
          const organization = this.organization();
          if (organization) {
            this.setCurrentOrganization({ ...organization, isActive: active });
            this.organizations.update((items) =>
              items.map((item) =>
                item.id === organization.id
                  ? { ...item, isActive: active }
                  : item,
              ),
            );
          }
          this.snackBar.open(
            `Organization ${active ? "activated" : "deactivated"}.`,
            "Dismiss",
            { duration: 3000 },
          );
        },
        error: (error) =>
          this.errorMessage.set(
            getApiErrorMessage(
              error,
              "Organization status could not be changed.",
            ),
          ),
      });
  }

  isCurrentOrganization(organization: Organization): boolean {
    return organization.id === this.organization()?.id;
  }

  private setCurrentOrganization(organization: Organization): void {
    this.organization.set(organization);
    this.form.patchValue({ name: organization.name, code: organization.code });
  }
}
