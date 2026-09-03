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
import { RouterLink } from "@angular/router";
import { debounceTime, distinctUntilChanged, finalize, merge } from "rxjs";
import { PermissionService } from "../../core/auth/permission.service";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import { Farm } from "../../core/farm-management/farm-management.models";
import { getApiErrorMessage } from "../../core/models/api-error.model";

@Component({
  selector: "app-farms-page",
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
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: "./farms-page.component.html",
  styleUrl: "./farms-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FarmsPageComponent implements OnInit {
  private readonly service = inject(FarmManagementService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);
  readonly columns = [
    "farm",
    "ownership",
    "location",
    "area",
    "status",
    "actions",
  ];
  readonly farms = signal<readonly Farm[]>([]);
  readonly totalCount = signal(0);
  readonly isLoading = signal(false);
  readonly actionInProgress = signal(false);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly filterForm = this.formBuilder.nonNullable.group({
    search: [""],
    status: ["all"],
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
        this.load();
      });
    this.load();
  }
  load(): void {
    this.isLoading.set(true);
    const status = this.filterForm.controls.status.value;
    this.service
      .listFarms(
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
        next: (r) => {
          this.farms.set(r.items);
          this.totalCount.set(r.totalCount);
        },
        error: (e) =>
          this.snack.open(
            getApiErrorMessage(e, "Farms could not be loaded."),
            "Dismiss",
            { duration: 5000 },
          ),
      });
  }
  pageChanged(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }
  changeStatus(farm: Farm, active: boolean): void {
    this.actionInProgress.set(true);
    const request = active
      ? this.service.activateFarm(farm.id)
      : this.service.deactivateFarm(farm.id);
    request
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.actionInProgress.set(false)),
      )
      .subscribe({
        next: () => {
          this.snack.open(
            `Farm ${active ? "activated" : "deactivated"}.`,
            "Dismiss",
            { duration: 3000 },
          );
          this.load();
        },
        error: (e) =>
          this.snack.open(
            getApiErrorMessage(e, "Farm status could not be changed."),
            "Dismiss",
            { duration: 5000 },
          ),
      });
  }
}
