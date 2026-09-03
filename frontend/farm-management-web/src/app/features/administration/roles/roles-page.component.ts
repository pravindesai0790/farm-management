import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
  computed,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormBuilder, ReactiveFormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatPaginatorModule, PageEvent } from "@angular/material/paginator";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTableModule } from "@angular/material/table";
import { MatTooltipModule } from "@angular/material/tooltip";
import { RouterLink } from "@angular/router";
import { debounceTime, distinctUntilChanged, finalize } from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import { AdministrationService } from "../../../core/administration/administration.service";
import { Role } from "../../../core/administration/administration.models";
import { getApiErrorMessage } from "../../../core/models/api-error.model";

@Component({
  selector: "app-roles-page",
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: "./roles-page.component.html",
  styleUrl: "./roles-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RolesPageComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly administrationService = inject(AdministrationService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);
  readonly roles = signal<readonly Role[]>([]);
  readonly isLoading = signal(true);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(10);
  readonly search = signal("");
  readonly status = signal<"all" | "active" | "inactive">("all");
  readonly filterForm = this.formBuilder.nonNullable.group({
    search: [""],
    status: ["all" as "all" | "active" | "inactive"],
  });
  readonly filteredRoles = computed(() => {
    const search = this.search().trim().toLowerCase();
    const status = this.status();
    return this.roles().filter(
      (role) =>
        (search.length === 0 ||
          role.name.toLowerCase().includes(search) ||
          (role.description ?? "").toLowerCase().includes(search)) &&
        (status === "all" ||
          (status === "active" ? role.isActive : !role.isActive)),
    );
  });
  readonly visibleRoles = computed(() => {
    const start = this.pageIndex() * this.pageSize();
    return this.filteredRoles().slice(start, start + this.pageSize());
  });
  readonly displayedColumns = [
    "name",
    "description",
    "permissions",
    "status",
    "actions",
  ];

  ngOnInit(): void {
    this.filterForm.valueChanges
      .pipe(
        debounceTime(200),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((value) => {
        this.search.set(value.search ?? "");
        this.status.set(value.status ?? "all");
        this.pageIndex.set(0);
      });
    this.loadRoles();
  }

  loadRoles(): void {
    this.isLoading.set(true);
    this.administrationService
      .listRoles()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (roles) => this.roles.set(roles),
        error: (error: unknown) =>
          this.snackBar.open(
            getApiErrorMessage(error, "Roles could not be loaded."),
            "Dismiss",
            { duration: 5000 },
          ),
      });
  }

  pageChanged(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
  }

  toggleRole(role: Role, activate: boolean): void {
    const request$ = activate
      ? this.administrationService.activateRole(role.id)
      : this.administrationService.deactivateRole(role.id);
    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.snackBar.open(
          `Role ${activate ? "activated" : "deactivated"}.`,
          "Dismiss",
          { duration: 3000 },
        );
        this.loadRoles();
      },
      error: (error: unknown) =>
        this.snackBar.open(
          getApiErrorMessage(error, "The role status could not be changed."),
          "Dismiss",
          { duration: 5000 },
        ),
    });
  }
}
