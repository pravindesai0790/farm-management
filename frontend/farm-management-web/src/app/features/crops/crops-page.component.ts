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
import { Crop } from "../../core/farm-management/farm-management.models";
import { getApiErrorMessage } from "../../core/models/api-error.model";
@Component({
  selector: "app-crops-page",
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
  templateUrl: "./crops-page.component.html",
  styleUrl: "./crops-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CropsPageComponent implements OnInit {
  private readonly service = inject(FarmManagementService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);
  readonly columns = ["code", "name", "type", "status", "actions"];
  readonly crops = signal<readonly Crop[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly isLoading = signal(false);
  readonly filterForm = this.fb.nonNullable.group({
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
    const status = this.filterForm.controls.status.value;
    this.isLoading.set(true);
    this.service
      .listCrops(
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
          this.crops.set(r.items);
          this.totalCount.set(r.totalCount);
        },
        error: (e) =>
          this.snack.open(
            getApiErrorMessage(e, "Crops could not be loaded."),
            "Dismiss",
            { duration: 5000 },
          ),
      });
  }
  pageChanged(e: PageEvent): void {
    this.pageIndex.set(e.pageIndex);
    this.pageSize.set(e.pageSize);
    this.load();
  }
  changeStatus(crop: Crop, active: boolean): void {
    const request = active
      ? this.service.activateCrop(crop.id)
      : this.service.deactivateCrop(crop.id);
    request
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.load(),
        error: (e) =>
          this.snack.open(
            getApiErrorMessage(e, "Crop status could not be changed."),
            "Dismiss",
            { duration: 5000 },
          ),
      });
  }
}
