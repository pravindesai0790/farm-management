import { Component, OnInit, inject, signal } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatSnackBar } from "@angular/material/snack-bar";
import { Supplier } from "../../../../core/expenses/supplier.models";
import { SupplierService } from "../../../../core/expenses/supplier.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";

export interface SupplierEditorDialogData {
  readonly supplier?: Supplier;
}

@Component({
  selector: "app-supplier-editor-dialog",
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
  templateUrl: "./supplier-editor-dialog.component.html",
  styleUrl: "./supplier-editor-dialog.component.scss",
})
export class SupplierEditorDialogComponent implements OnInit {
  readonly data = inject<SupplierEditorDialogData | null>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<SupplierEditorDialogComponent>);
  private readonly fb = inject(FormBuilder);
  private readonly supplierService = inject(SupplierService);
  private readonly snack = inject(MatSnackBar);

  readonly isSubmitting = signal(false);

  readonly form = this.fb.group({
    name: [this.data?.supplier?.name || "", [Validators.required, Validators.maxLength(200)]],
    contactPerson: [this.data?.supplier?.contactPerson || "", [Validators.maxLength(150)]],
    phone: [this.data?.supplier?.phone || "", [Validators.maxLength(50)]],
    email: [this.data?.supplier?.email || "", [Validators.email, Validators.maxLength(150)]],
    address: [this.data?.supplier?.address || "", [Validators.maxLength(500)]],
    registrationIdentifier: [this.data?.supplier?.registrationIdentifier || "", [Validators.maxLength(100)]],
    notes: [this.data?.supplier?.notes || "", [Validators.maxLength(1000)]],
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
      contactPerson: val.contactPerson?.trim() || null,
      phone: val.phone?.trim() || null,
      email: val.email?.trim() || null,
      address: val.address?.trim() || null,
      registrationIdentifier: val.registrationIdentifier?.trim() || null,
      notes: val.notes?.trim() || null,
    };

    const action$ = this.data?.supplier
      ? this.supplierService.update(this.data.supplier.id, request)
      : this.supplierService.create(request);

    action$.subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.snack.open(
          this.data?.supplier ? "Supplier updated successfully." : "Supplier created successfully.",
          "Close",
          { duration: 3000 }
        );
        this.dialogRef.close(true);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        const msg = getApiErrorMessage(err, "Failed to save supplier.");
        this.snack.open(msg, "Close", { duration: 5000 });
      },
    });
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }
}
