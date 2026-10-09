import { CommonModule } from "@angular/common";
import { Component, OnInit, inject, signal } from "@angular/core";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import {
  ApplicationMethodItem,
  CreateApplicationMethodRequest,
  CreateProductTypeRequest,
  CreateTargetRequest,
  MasterTypeKind,
  ProductTypeItem,
  TargetItem,
  TargetType,
  UpdateApplicationMethodRequest,
  UpdateProductTypeRequest,
  UpdateTargetRequest,
} from "../../../../core/sprays/spray-master-data.models";
import { SprayMasterDataService } from "../../../../core/sprays/spray-master-data.service";

export interface SprayMasterEditorDialogData {
  readonly kind: MasterTypeKind;
  readonly item?: ProductTypeItem | TargetItem | ApplicationMethodItem;
}

@Component({
  selector: "app-spray-master-editor-dialog",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: "./spray-master-editor-dialog.component.html",
  styleUrl: "./spray-master-editor-dialog.component.scss",
})
export class SprayMasterEditorDialogComponent implements OnInit {
  readonly data = inject<SprayMasterEditorDialogData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<SprayMasterEditorDialogComponent>);
  private readonly fb = inject(FormBuilder);
  private readonly masterService = inject(SprayMasterDataService);
  private readonly snack = inject(MatSnackBar);

  readonly isSubmitting = signal(false);
  readonly isEditMode = signal(false);
  readonly isSystemItem = signal(false);

  readonly targetTypes: readonly TargetType[] = ["Disease", "Insect", "Mite", "Weed", "Other"];

  readonly form = this.fb.group({
    code: ["", [Validators.required, Validators.maxLength(50), Validators.pattern(/^[A-Za-z0-9_-]+$/)]],
    name: ["", [Validators.required, Validators.maxLength(150)]],
    targetType: [""],
    description: ["", [Validators.maxLength(500)]],
    displayOrder: [0, [Validators.required, Validators.min(0)]],
  });

  get title(): string {
    const action = this.isEditMode() ? "Edit" : "Add";
    switch (this.data.kind) {
      case "product-type":
        return `${action} Product Type`;
      case "target":
        return `${action} Target`;
      case "application-method":
        return `${action} Application Method`;
    }
  }

  get kindLabel(): string {
    switch (this.data.kind) {
      case "product-type":
        return "Product Type";
      case "target":
        return "Target";
      case "application-method":
        return "Application Method";
    }
  }

  ngOnInit(): void {
    if (this.data.kind === "target") {
      this.form.controls.targetType.setValidators([Validators.required]);
      this.form.controls.targetType.updateValueAndValidity();
    }

    if (this.data.item) {
      this.isEditMode.set(true);
      this.isSystemItem.set(!!this.data.item.isSystem);

      const targetItem = this.data.kind === "target" ? (this.data.item as TargetItem) : null;

      this.form.patchValue({
        code: this.data.item.code,
        name: this.data.item.name,
        targetType: targetItem ? String(targetItem.targetType) : "",
        description: this.data.item.description ?? "",
        displayOrder: this.data.item.displayOrder,
      });

      this.form.controls.code.disable();

      if (this.isSystemItem()) {
        this.form.disable();
      }
    }
  }

  onSubmit(): void {
    if (this.form.invalid || this.isSubmitting() || this.isSystemItem()) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    const val = this.form.getRawValue();

    const code = (val.code ?? "").trim().toUpperCase();
    const name = (val.name ?? "").trim();
    const description = val.description?.trim() ? val.description.trim() : null;
    const displayOrder = val.displayOrder ?? 0;

    let operation$;

    if (this.data.kind === "product-type") {
      if (this.isEditMode()) {
        const updateReq: UpdateProductTypeRequest = { name, description, displayOrder };
        operation$ = this.masterService.updateProductType(this.data.item!.id, updateReq);
      } else {
        const createReq: CreateProductTypeRequest = { code, name, description, displayOrder };
        operation$ = this.masterService.createProductType(createReq);
      }
    } else if (this.data.kind === "target") {
      const targetType = val.targetType ?? "";
      if (this.isEditMode()) {
        const updateReq: UpdateTargetRequest = { name, targetType, description, displayOrder };
        operation$ = this.masterService.updateTarget(this.data.item!.id, updateReq);
      } else {
        const createReq: CreateTargetRequest = { code, name, targetType, description, displayOrder };
        operation$ = this.masterService.createTarget(createReq);
      }
    } else {
      if (this.isEditMode()) {
        const updateReq: UpdateApplicationMethodRequest = { name, description, displayOrder };
        operation$ = this.masterService.updateApplicationMethod(this.data.item!.id, updateReq);
      } else {
        const createReq: CreateApplicationMethodRequest = { code, name, description, displayOrder };
        operation$ = this.masterService.createApplicationMethod(createReq);
      }
    }

    operation$.subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.snack.open(
          `${this.kindLabel} ${this.isEditMode() ? "updated" : "created"} successfully.`,
          "Close",
          { duration: 3000 },
        );
        this.dialogRef.close(true);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        const msg = getApiErrorMessage(err, `Failed to save ${this.kindLabel.toLowerCase()}.`);
        this.snack.open(msg, "Close", { duration: 5000 });
      },
    });
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }
}
