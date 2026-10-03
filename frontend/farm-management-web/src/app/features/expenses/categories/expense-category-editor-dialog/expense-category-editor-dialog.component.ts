import { Component, OnInit, inject, signal } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatSnackBar } from "@angular/material/snack-bar";
import { ExpenseCategory } from "../../../../core/expenses/expense-category.models";
import { ExpenseCategoryService } from "../../../../core/expenses/expense-category.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";

export interface ExpenseCategoryEditorDialogData {
  readonly category?: ExpenseCategory;
}

@Component({
  selector: "app-expense-category-editor-dialog",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
  ],
  templateUrl: "./expense-category-editor-dialog.component.html",
  styleUrl: "./expense-category-editor-dialog.component.scss",
})
export class ExpenseCategoryEditorDialogComponent implements OnInit {
  readonly data = inject<ExpenseCategoryEditorDialogData | null>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<ExpenseCategoryEditorDialogComponent>);
  private readonly fb = inject(FormBuilder);
  private readonly categoryService = inject(ExpenseCategoryService);
  private readonly snack = inject(MatSnackBar);

  readonly isSubmitting = signal(false);

  readonly form = this.fb.group({
    name: [this.data?.category?.name || "", [Validators.required, Validators.maxLength(100)]],
    description: [this.data?.category?.description || "", [Validators.maxLength(500)]],
  });

  ngOnInit(): void {}

  onSubmit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    const val = this.form.getRawValue();

    const request = {
      name: val.name?.trim() || "",
      description: val.description?.trim() || null,
    };

    const action$ = this.data?.category
      ? this.categoryService.update(this.data.category.id, request)
      : this.categoryService.create(request);

    action$.subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.snack.open(
          this.data?.category ? "Category updated successfully." : "Category created successfully.",
          "Close",
          { duration: 3000 }
        );
        this.dialogRef.close(true);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        const msg = getApiErrorMessage(err, "Failed to save category.");
        this.snack.open(msg, "Close", { duration: 5000 });
      },
    });
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }
}
