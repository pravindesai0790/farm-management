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
import {
  EMPLOYMENT_TYPE_OPTIONS,
  GENDER_OPTIONS,
  WorkerItem,
  formatEmploymentType,
  formatGender,
} from "../../../core/labor/labor.models";
import { LaborService } from "../../../core/labor/labor.service";
import { getApiErrorMessage } from "../../../core/models/api-error.model";

@Component({
  selector: "app-workers-page",
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
  templateUrl: "./workers-page.component.html",
  styleUrl: "./workers-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkersPageComponent implements OnInit {
  private readonly laborService = inject(LaborService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);

  readonly displayedColumns: readonly string[] = [
    "displayName",
    "gender",
    "mobileNumber",
    "employmentType",
    "status",
    "actions",
  ];

  readonly genderOptions = GENDER_OPTIONS;
  readonly employmentTypeOptions = EMPLOYMENT_TYPE_OPTIONS;

  readonly workers = signal<readonly WorkerItem[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly isLoading = signal(false);
  readonly actionInProgress = signal(false);

  readonly filterForm = this.formBuilder.nonNullable.group({
    search: [""],
    status: ["all"],
    gender: ["all"],
    employmentType: ["all"],
  });

  ngOnInit(): void {
    merge(
      this.filterForm.controls.search.valueChanges.pipe(
        debounceTime(300),
        distinctUntilChanged(),
      ),
      this.filterForm.controls.status.valueChanges,
      this.filterForm.controls.gender.valueChanges,
      this.filterForm.controls.employmentType.valueChanges,
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.load();
      });

    this.load();
  }

  load(): void {
    this.isLoading.set(true);

    const { search, status, gender, employmentType } = this.filterForm.getRawValue();

    const isActive =
      status === "all" ? null : status === "active";

    const normalizedGender =
      gender === "all" || !gender ? null : gender;

    const normalizedEmploymentType =
      employmentType === "all" || !employmentType ? null : employmentType;

    this.laborService
      .listWorkers(
        this.pageIndex() + 1,
        this.pageSize(),
        search,
        isActive,
        normalizedGender,
        normalizedEmploymentType,
      )
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (response) => {
          this.workers.set(response.items);
          this.totalCount.set(response.totalCount);
        },
        error: (error) => {
          this.snack.open(
            getApiErrorMessage(error, "Workers could not be loaded."),
            "Dismiss",
            { duration: 5000 },
          );
        },
      });
  }

  pageChanged(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  changeStatus(worker: WorkerItem, active: boolean): void {
    this.actionInProgress.set(true);

    const request$ = active
      ? this.laborService.activateWorker(worker.id)
      : this.laborService.deactivateWorker(worker.id);

    request$
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.actionInProgress.set(false)),
      )
      .subscribe({
        next: () => {
          this.snack.open(
            `Worker "${worker.displayName}" ${active ? "activated" : "deactivated"}.`,
            "Dismiss",
            { duration: 3000 },
          );
          this.load();
        },
        error: (error) => {
          this.snack.open(
            getApiErrorMessage(error, "Worker status could not be changed."),
            "Dismiss",
            { duration: 5000 },
          );
        },
      });
  }

  getGenderLabel(gender: string): string {
    return formatGender(gender);
  }

  getEmploymentTypeLabel(type: string): string {
    return formatEmploymentType(type);
  }
}
