import { CommonModule } from "@angular/common";
import { Component, DestroyRef, OnInit, inject, signal } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormBuilder, ReactiveFormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatChipsModule } from "@angular/material/chips";
import { MatDialog, MatDialogModule } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatPaginatorModule, PageEvent } from "@angular/material/paginator";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTableModule } from "@angular/material/table";
import { MatTooltipModule } from "@angular/material/tooltip";
import { debounceTime, distinctUntilChanged, finalize, merge } from "rxjs";

import { PermissionService } from "../../../../core/auth/permission.service";
import { ExpenseCategory } from "../../../../core/expenses/expense-category.models";
import { ExpenseCategoryService } from "../../../../core/expenses/expense-category.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import { ExpensesSubNavComponent } from "../../components/expenses-sub-nav/expenses-sub-nav.component";
import { ExpenseCategoryEditorDialogComponent } from "../expense-category-editor-dialog/expense-category-editor-dialog.component";

@Component({
  selector: "app-expense-category-list-page",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule,
    ExpensesSubNavComponent,
  ],
  templateUrl: "./expense-category-list-page.component.html",
  styleUrl: "./expense-category-list-page.component.scss",
})
export class ExpenseCategoryListPageComponent implements OnInit {
  private readonly categoryService = inject(ExpenseCategoryService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);

  readonly columns = ["name", "description", "type", "status", "actions"];

  readonly categories = signal<readonly ExpenseCategory[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(50);
  readonly isLoading = signal(false);

  readonly filterForm = this.fb.nonNullable.group({
    search: [""],
    status: ["all"],
  });

  ngOnInit(): void {
    merge(
      this.filterForm.controls.search.valueChanges.pipe(
        debounceTime(300),
        distinctUntilChanged()
      ),
      this.filterForm.controls.status.valueChanges
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.loadCategories();
      });

    this.loadCategories();
  }

  loadCategories(): void {
    this.isLoading.set(true);

    const { search, status } = this.filterForm.getRawValue();
    const isActive = status === "active" ? true : status === "inactive" ? false : null;

    this.categoryService
      .list(this.pageIndex() + 1, this.pageSize(), search, isActive)
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (res) => {
          this.categories.set(res.items);
          this.totalCount.set(res.totalCount);
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to load expense categories.");
          this.snack.open(msg, "Close", { duration: 5000 });
        },
      });
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadCategories();
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(ExpenseCategoryEditorDialogComponent, {
      width: "500px",
      data: {},
    });

    ref.afterClosed().subscribe((saved) => {
      if (saved) {
        this.loadCategories();
      }
    });
  }

  openEditDialog(category: ExpenseCategory): void {
    if (category.isSystemDefault) return;

    const ref = this.dialog.open(ExpenseCategoryEditorDialogComponent, {
      width: "500px",
      data: { category },
    });

    ref.afterClosed().subscribe((saved) => {
      if (saved) {
        this.loadCategories();
      }
    });
  }

  toggleStatus(category: ExpenseCategory): void {
    if (category.isSystemDefault) return;

    const action$ = category.isActive
      ? this.categoryService.deactivate(category.id)
      : this.categoryService.activate(category.id);

    action$.subscribe({
      next: () => {
        this.snack.open(
          `Category ${category.isActive ? "deactivated" : "activated"} successfully.`,
          "Close",
          { duration: 3000 }
        );
        this.loadCategories();
      },
      error: (err) => {
        const msg = getApiErrorMessage(err, "Failed to update category status.");
        this.snack.open(msg, "Close", { duration: 5000 });
      },
    });
  }
}
