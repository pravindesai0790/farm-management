import { ComponentFixture, TestBed } from "@angular/core/testing";
import { MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { of } from "rxjs";
import {
  ProductTypeItem,
  TargetItem,
} from "../../../../core/sprays/spray-master-data.models";
import { SprayMasterDataService } from "../../../../core/sprays/spray-master-data.service";
import {
  SprayMasterEditorDialogComponent,
  SprayMasterEditorDialogData,
} from "./spray-master-editor-dialog.component";

describe("SprayMasterEditorDialogComponent", () => {
  let fixture: ComponentFixture<SprayMasterEditorDialogComponent>;
  let component: SprayMasterEditorDialogComponent;
  let mockMasterService: jasmine.SpyObj<SprayMasterDataService>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<SprayMasterEditorDialogComponent>>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const initComponent = (data: SprayMasterEditorDialogData) => {
    mockMasterService = jasmine.createSpyObj("SprayMasterDataService", [
      "createProductType",
      "updateProductType",
      "createTarget",
      "updateTarget",
      "createApplicationMethod",
      "updateApplicationMethod",
    ]);

    mockDialogRef = jasmine.createSpyObj("MatDialogRef", ["close"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);

    TestBed.configureTestingModule({
      imports: [NoopAnimationsModule, SprayMasterEditorDialogComponent],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: data },
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: SprayMasterDataService, useValue: mockMasterService },
      ],
    });

    fixture = TestBed.createComponent(SprayMasterEditorDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  };

  it("should initialize in create mode for product type", () => {
    initComponent({ kind: "product-type" });
    expect(component.isEditMode()).toBeFalse();
    expect(component.title).toBe("Add Product Type");
    expect(component.form.controls.code.enabled).toBeTrue();
  });

  it("should initialize in edit mode and disable code", () => {
    const existing: ProductTypeItem = {
      id: "pt-1",
      code: "FUNG",
      name: "Fungicide",
      description: "Controls fungus",
      displayOrder: 1,
      isSystem: false,
      isActive: true,
    };

    initComponent({ kind: "product-type", item: existing });
    expect(component.isEditMode()).toBeTrue();
    expect(component.title).toBe("Edit Product Type");
    expect(component.form.controls.code.disabled).toBeTrue();
    expect(component.form.controls.name.value).toBe("Fungicide");
  });

  it("should disable entire form if system item", () => {
    const systemItem: ProductTypeItem = {
      id: "pt-sys",
      code: "INSECT",
      name: "Insecticide",
      description: null,
      displayOrder: 2,
      isSystem: true,
      isActive: true,
    };

    initComponent({ kind: "product-type", item: systemItem });
    expect(component.isSystemItem()).toBeTrue();
    expect(component.form.disabled).toBeTrue();
  });

  it("should require targetType when kind is target", () => {
    initComponent({ kind: "target" });
    expect(component.form.controls.targetType.validator).toBeTruthy();

    component.form.patchValue({ code: "MILDEW", name: "Powdery Mildew", targetType: "" });
    expect(component.form.controls.targetType.invalid).toBeTrue();

    component.form.patchValue({ targetType: "Disease" });
    expect(component.form.controls.targetType.valid).toBeTrue();
  });

  it("should submit create product type and close dialog", () => {
    initComponent({ kind: "product-type" });
    mockMasterService.createProductType.and.returnValue(
      of({
        id: "pt-new",
        code: "BIO",
        name: "Bio Agent",
        description: null,
        displayOrder: 0,
        isSystem: false,
        isActive: true,
      }),
    );

    component.form.patchValue({
      code: "bio",
      name: "Bio Agent",
      displayOrder: 0,
    });

    component.onSubmit();

    expect(mockMasterService.createProductType).toHaveBeenCalledWith({
      code: "BIO",
      name: "Bio Agent",
      description: null,
      displayOrder: 0,
    });
    expect(mockDialogRef.close).toHaveBeenCalledWith(true);
  });

  it("should submit update target and close dialog", () => {
    const existingTarget: TargetItem = {
      id: "tgt-1",
      code: "APHID",
      name: "Aphids",
      targetType: "Insect",
      description: "Sap-sucking pest",
      displayOrder: 3,
      isSystem: false,
      isActive: true,
    };

    initComponent({ kind: "target", item: existingTarget });
    mockMasterService.updateTarget.and.returnValue(
      of({
        ...existingTarget,
        name: "Green Aphids",
      }),
    );

    component.form.patchValue({
      name: "Green Aphids",
      targetType: "Insect",
    });

    component.onSubmit();

    expect(mockMasterService.updateTarget).toHaveBeenCalledWith(
      "tgt-1",
      jasmine.objectContaining({
        name: "Green Aphids",
        targetType: "Insect",
      }),
    );
    expect(mockDialogRef.close).toHaveBeenCalledWith(true);
  });
});
