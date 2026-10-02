import { ComponentFixture, TestBed, fakeAsync, tick } from "@angular/core/testing";
import { MatDialog } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { provideRouter } from "@angular/router";
import { of } from "rxjs";

import { PermissionService } from "../../../../core/auth/permission.service";
import { Supplier, SupplierList } from "../../../../core/expenses/supplier.models";
import { SupplierService } from "../../../../core/expenses/supplier.service";
import { SupplierListPageComponent } from "./supplier-list-page.component";

describe("SupplierListPageComponent", () => {
  let component: SupplierListPageComponent;
  let fixture: ComponentFixture<SupplierListPageComponent>;

  let mockSupplierService: jasmine.SpyObj<SupplierService>;
  let mockPermissionService: jasmine.SpyObj<PermissionService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;
  let mockDialog: jasmine.SpyObj<MatDialog>;

  const sampleSupplier: Supplier = {
    id: "sup-1",
    organizationId: "org-1",
    name: "Agri Corp",
    contactPerson: "Bob Smith",
    phone: "1234567890",
    email: "bob@agricorp.com",
    address: "100 Field Way",
    registrationIdentifier: "REG-100",
    notes: "Main vendor",
    isActive: true,
    createdAt: "2026-10-01T00:00:00Z",
    createdBy: "usr-1",
    updatedAt: null,
    updatedBy: null,
  };

  const sampleResponse: SupplierList = {
    items: [sampleSupplier],
    page: 1,
    pageSize: 20,
    totalCount: 1,
  };

  beforeEach(async () => {
    mockSupplierService = jasmine.createSpyObj("SupplierService", [
      "list",
      "get",
      "create",
      "update",
      "activate",
      "deactivate",
    ]);
    mockPermissionService = jasmine.createSpyObj("PermissionService", ["has"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);
    mockDialog = jasmine.createSpyObj("MatDialog", ["open"]);

    mockSupplierService.list.and.returnValue(of(sampleResponse));
    mockPermissionService.has.and.returnValue(true);

    await TestBed.configureTestingModule({
      imports: [SupplierListPageComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: SupplierService, useValue: mockSupplierService },
        { provide: PermissionService, useValue: mockPermissionService },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    })
      .overrideProvider(MatDialog, { useValue: mockDialog })
      .compileComponents();

    fixture = TestBed.createComponent(SupplierListPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("creates component and loads suppliers on init", () => {
    expect(component).toBeTruthy();
    expect(mockSupplierService.list).toHaveBeenCalledWith(1, 20, "", null);
    expect(component.suppliers().length).toBe(1);
    expect(component.totalCount()).toBe(1);
  });

  it("filters suppliers when search input changes", fakeAsync(() => {
    mockSupplierService.list.calls.reset();
    component.filterForm.controls.search.setValue("Agri");
    tick(300);

    expect(mockSupplierService.list).toHaveBeenCalledWith(1, 20, "Agri", null);
  }));

  it("filters suppliers by status", () => {
    mockSupplierService.list.calls.reset();
    component.filterForm.controls.status.setValue("active");

    expect(mockSupplierService.list).toHaveBeenCalledWith(1, 20, "", true);
  });

  it("toggles supplier status from active to inactive", () => {
    mockSupplierService.deactivate.and.returnValue(of(undefined));
    mockSupplierService.list.calls.reset();

    component.toggleStatus(sampleSupplier);

    expect(mockSupplierService.deactivate).toHaveBeenCalledWith("sup-1");
    expect(mockSnackBar.open).toHaveBeenCalledWith(
      "Supplier deactivated successfully.",
      "Close",
      jasmine.any(Object)
    );
    expect(mockSupplierService.list).toHaveBeenCalled();
  });

  it("opens create dialog and reloads list when closed with success", () => {
    mockDialog.open.and.returnValue({
      afterClosed: () => of(true),
    } as any);
    mockSupplierService.list.calls.reset();

    component.openCreateDialog();

    expect(mockDialog.open).toHaveBeenCalled();
    expect(mockSupplierService.list).toHaveBeenCalled();
  });
});
