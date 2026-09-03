import { DatePipe } from "@angular/common";
import { HttpErrorResponse } from "@angular/common/http";
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormBuilder, ReactiveFormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatChipsModule } from "@angular/material/chips";
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
import { debounceTime, distinctUntilChanged, finalize, merge } from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import { AdministrationService } from "../../../core/administration/administration.service";
import { User } from "../../../core/administration/administration.models";
import { getApiErrorMessage } from "../../../core/models/api-error.model";

type StatusFilter = "all" | "active" | "inactive";

@Component({
  selector: "app-users-page",
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
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
  templateUrl: "./users-page.component.html",
  styleUrl: "./users-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersPageComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly administrationService = inject(AdministrationService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);

  readonly displayedColumns = [
    "name",
    "email",
    "roles",
    "status",
    "lastLoginAt",
    "actions",
  ];
  readonly users = signal<readonly User[]>([]);
  readonly totalCount = signal(0);
  readonly isLoading = signal(false);
  readonly actionInProgress = signal<string | null>(null);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly filterForm = this.formBuilder.nonNullable.group({
    search: [""],
    status: ["all" as StatusFilter],
  });

  ngOnInit(): void {
    merge(
      this.filterForm.controls.search.valueChanges.pipe(
        debounceTime(300),
        distinctUntilChanged(),
      ),
      this.filterForm.controls.status.valueChanges,
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.loadUsers();
      });
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading.set(true);
    const status = this.filterForm.controls.status.value;
    this.administrationService
      .listUsers(
        this.pageIndex() + 1,
        this.pageSize(),
        this.filterForm.controls.search.value,
        status === "all" ? null : status === "active",
      )
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (response) => {
          this.users.set(response.items);
          this.totalCount.set(response.totalCount);
        },
        error: (error: unknown) =>
          this.snackBar.open(
            getApiErrorMessage(error, "Users could not be loaded."),
            "Dismiss",
            { duration: 5000 },
          ),
      });
  }

  pageChanged(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadUsers();
  }

  performAction(
    user: User,
    action: "activate" | "deactivate" | "unlock",
  ): void {
    this.actionInProgress.set(`${action}:${user.id}`);
    const request$ =
      action === "activate"
        ? this.administrationService.activateUser(user.id)
        : action === "deactivate"
          ? this.administrationService.deactivateUser(user.id)
          : this.administrationService.unlockUser(user.id);
    request$
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.actionInProgress.set(null)),
      )
      .subscribe({
        next: () => {
          this.snackBar.open(
            `User ${this.actionPastTense(action)}.`,
            "Dismiss",
            { duration: 3000 },
          );
          this.loadUsers();
        },
        error: (error: unknown) =>
          this.snackBar.open(
            getApiErrorMessage(
              error,
              `The user could not be ${this.actionPastTense(action)}.`,
            ),
            "Dismiss",
            { duration: 5000 },
          ),
      });
  }

  isActionInProgress(user: User, action: string): boolean {
    return this.actionInProgress() === `${action}:${user.id}`;
  }

  trackUser(_index: number, user: User): string {
    return user.id;
  }

  private actionPastTense(
    action: "activate" | "deactivate" | "unlock",
  ): string {
    return action === "unlock" ? "unlocked" : `${action}d`;
  }
}
