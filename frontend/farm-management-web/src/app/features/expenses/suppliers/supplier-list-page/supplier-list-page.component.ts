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
import { Supplier } from "../../../../core/expenses/supplier.models";
import { SupplierService } from "../../../../core/expenses/supplier.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import { ExpensesSubNavComponent } from "../../components/expenses-sub-nav/expenses-sub-nav.component";
import { SupplierEditorDialogComponent } from "../supplier-editor-dialog/supplier-editor-dialog.component";

@Component({
  selector: "app-supplier-list-page",
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
  templateUrl: "./supplier-list-page.component.html",
  styleUrl: "./supplier-list-page.component.scss",
})
export class SupplierListPageComponent implements OnInit {
  private readonly supplierService = inject(SupplierService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);

  readonly columns = [
    "name",
    "contactPerson",
    "phone",
    "email",
    "registrationIdentifier",
    "status",
    "actions",
  ];

  readonly suppliers = signal<readonly Supplier[]>([]);
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
        distinctUntilChanged()
      ),
      this.filterForm.controls.status.valueChanges
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.loadSuppliers();
      });

    this.loadSuppliers();
  }

  loadSuppliers(): void {
    this.isLoading.set(true);

    const { search, status } = this.filterForm.getRawValue();
    const isActive = status === "active" ? true : status === "inactive" ? false : null;

    this.supplierService
      .list(this.pageIndex() + 1, this.pageSize(), search, isActive)
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (res) => {
          this.suppliers.set(res.items);
          this.totalCount.set(res.totalCount);
        },
        error: (err) => {
          const msg = getApiErrorMessage(err, "Failed to load suppliers.");
          this.snack.open(msg, "Close", { duration: 5000 });
        },
      });
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadSuppliers();
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(SupplierEditorDialogComponent, {
      width: "560px",
      data: {},
    });

    ref.afterClosed().subscribe((saved) => {
      if (saved) {
        this.loadSuppliers();
      }
    });
  }

  openEditDialog(supplier: Supplier): void {
    const ref = this.dialog.open(SupplierEditorDialogComponent, {
      width: "560px",
      data: { supplier },
    });

    ref.afterClosed().subscribe((saved) => {
      if (saved) {
        this.loadSuppliers();
      }
    });
  }

  toggleStatus(supplier: Supplier): void {
    const action$ = supplier.isActive
      ? this.supplierService.deactivate(supplier.id)
      : this.supplierService.activate(supplier.id);

    action$.subscribe({
      next: () => {
        this.snack.open(
          `Supplier ${supplier.isActive ? "deactivated" : "activated"} successfully.`,
          "Close",
          { duration: 3000 }
        );
        this.loadSuppliers();
      },
      error: (err) => {
        const msg = getApiErrorMessage(err, "Failed to update supplier status.");
        this.snack.open(msg, "Close", { duration: 5000 });
      },
    });
  }
}
