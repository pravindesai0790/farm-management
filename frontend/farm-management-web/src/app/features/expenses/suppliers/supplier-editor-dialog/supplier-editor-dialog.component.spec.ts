import { ComponentFixture, TestBed } from "@angular/core/testing";
import { MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { of } from "rxjs";

import { Supplier } from "../../../../core/expenses/supplier.models";
import { SupplierService } from "../../../../core/expenses/supplier.service";
import {
  SupplierEditorDialogComponent,
  SupplierEditorDialogData,
} from "./supplier-editor-dialog.component";

describe("SupplierEditorDialogComponent", () => {
  let component: SupplierEditorDialogComponent;
  let fixture: ComponentFixture<SupplierEditorDialogComponent>;

  let mockSupplierService: jasmine.SpyObj<SupplierService>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<SupplierEditorDialogComponent>>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const sampleSupplier: Supplier = {
    id: "sup-1",
    organizationId: "org-1",
    name: "Existing Supplier",
    contactPerson: "Jane",
    phone: "111-222",
    email: "jane@test.com",
    address: "Road 1",
    registrationIdentifier: "REG-01",
    notes: "Note",
    isActive: true,
    createdAt: "2026-10-01T00:00:00Z",
    createdBy: "usr-1",
    updatedAt: null,
    updatedBy: null,
  };

  const initComponent = async (data: SupplierEditorDialogData = {}) => {
    mockSupplierService = jasmine.createSpyObj("SupplierService", ["create", "update"]);
    mockDialogRef = jasmine.createSpyObj("MatDialogRef", ["close"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);

    await TestBed.configureTestingModule({
      imports: [SupplierEditorDialogComponent, NoopAnimationsModule],
      providers: [
        { provide: SupplierService, useValue: mockSupplierService },
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: MAT_DIALOG_DATA, useValue: data },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SupplierEditorDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  };

  it("initializes form with empty values in create mode", async () => {
    await initComponent({});

    expect(component).toBeTruthy();
    expect(component.form.controls.name.value).toBe("");
    expect(component.form.valid).toBeFalse();
  });

  it("initializes form with supplier values in edit mode", async () => {
    await initComponent({ supplier: sampleSupplier });

    expect(component.form.controls.name.value).toBe("Existing Supplier");
    expect(component.form.controls.contactPerson.value).toBe("Jane");
    expect(component.form.valid).toBeTrue();
  });

  it("submits valid create request and closes dialog", async () => {
    await initComponent({});
    mockSupplierService.create.and.returnValue(of(sampleSupplier));

    component.form.patchValue({
      name: "Brand New Supplier",
      contactPerson: "Sam",
      phone: "555-5555",
      email: "sam@example.com",
    });

    component.onSubmit();

    expect(mockSupplierService.create).toHaveBeenCalledWith({
      name: "Brand New Supplier",
      contactPerson: "Sam",
      phone: "555-5555",
      email: "sam@example.com",
      address: null,
      registrationIdentifier: null,
      notes: null,
    });
    expect(mockDialogRef.close).toHaveBeenCalledWith(true);
  });

  it("submits valid update request and closes dialog", async () => {
    await initComponent({ supplier: sampleSupplier });
    mockSupplierService.update.and.returnValue(of(sampleSupplier));

    component.form.patchValue({
      name: "Updated Name",
    });

    component.onSubmit();

    expect(mockSupplierService.update).toHaveBeenCalledWith("sup-1", jasmine.objectContaining({
      name: "Updated Name",
    }));
    expect(mockDialogRef.close).toHaveBeenCalledWith(true);
  });

  it("does not submit if form is invalid", async () => {
    await initComponent({});
    component.form.patchValue({ name: "" });

    component.onSubmit();

    expect(mockSupplierService.create).not.toHaveBeenCalled();
    expect(component.form.controls.name.touched).toBeTrue();
  });
});
