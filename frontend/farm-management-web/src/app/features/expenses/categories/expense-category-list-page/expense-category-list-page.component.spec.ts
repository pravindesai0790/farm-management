import { ComponentFixture, TestBed, fakeAsync, tick } from "@angular/core/testing";
import { MatDialog } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { provideRouter } from "@angular/router";
import { of } from "rxjs";

import { PermissionService } from "../../../../core/auth/permission.service";
import { ExpenseCategory, ExpenseCategoryList } from "../../../../core/expenses/expense-category.models";
import { ExpenseCategoryService } from "../../../../core/expenses/expense-category.service";
import { ExpenseCategoryListPageComponent } from "./expense-category-list-page.component";

describe("ExpenseCategoryListPageComponent", () => {
  let component: ExpenseCategoryListPageComponent;
  let fixture: ComponentFixture<ExpenseCategoryListPageComponent>;

  let mockCategoryService: jasmine.SpyObj<ExpenseCategoryService>;
  let mockPermissionService: jasmine.SpyObj<PermissionService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;
  let mockDialog: jasmine.SpyObj<MatDialog>;

  const systemCategory: ExpenseCategory = {
    id: "cat-1",
    organizationId: null,
    name: "Fertilizer",
    code: null,
    description: "System standard category",
    isSystemDefault: true,
    isActive: true,
    createdAt: "2026-10-01T00:00:00Z",
    createdBy: null,
    updatedAt: null,
    updatedBy: null,
  };

  const customCategory: ExpenseCategory = {
    id: "cat-2",
    organizationId: "org-1",
    name: "Bio-stimulants",
    code: null,
    description: "Organic plant boosters",
    isSystemDefault: false,
    isActive: true,
    createdAt: "2026-10-01T00:00:00Z",
    createdBy: "usr-1",
    updatedAt: null,
    updatedBy: null,
  };

  const sampleResponse: ExpenseCategoryList = {
    items: [systemCategory, customCategory],
    page: 1,
    pageSize: 50,
    totalCount: 2,
  };

  beforeEach(async () => {
    mockCategoryService = jasmine.createSpyObj("ExpenseCategoryService", [
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

    mockCategoryService.list.and.returnValue(of(sampleResponse));
    mockPermissionService.has.and.returnValue(true);

    await TestBed.configureTestingModule({
      imports: [ExpenseCategoryListPageComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: ExpenseCategoryService, useValue: mockCategoryService },
        { provide: PermissionService, useValue: mockPermissionService },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    })
      .overrideProvider(MatDialog, { useValue: mockDialog })
      .compileComponents();

    fixture = TestBed.createComponent(ExpenseCategoryListPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("creates component and loads categories on init", () => {
    expect(component).toBeTruthy();
    expect(mockCategoryService.list).toHaveBeenCalledWith(1, 50, "", null);
    expect(component.categories().length).toBe(2);
    expect(component.totalCount()).toBe(2);
  });

  it("filters categories when search input changes", fakeAsync(() => {
    mockCategoryService.list.calls.reset();
    component.filterForm.controls.search.setValue("Bio");
    tick(300);

    expect(mockCategoryService.list).toHaveBeenCalledWith(1, 50, "Bio", null);
  }));

  it("does not allow editing system default category", () => {
    component.openEditDialog(systemCategory);
    expect(mockDialog.open).not.toHaveBeenCalled();
  });

  it("opens edit dialog for custom category", () => {
    mockDialog.open.and.returnValue({
      afterClosed: () => of(true),
    } as any);
    mockCategoryService.list.calls.reset();

    component.openEditDialog(customCategory);

    expect(mockDialog.open).toHaveBeenCalled();
    expect(mockCategoryService.list).toHaveBeenCalled();
  });

  it("does not allow deactivating system default category", () => {
    component.toggleStatus(systemCategory);
    expect(mockCategoryService.deactivate).not.toHaveBeenCalled();
    expect(mockCategoryService.activate).not.toHaveBeenCalled();
  });

  it("toggles custom category status", () => {
    mockCategoryService.deactivate.and.returnValue(of(undefined));
    mockCategoryService.list.calls.reset();

    component.toggleStatus(customCategory);

    expect(mockCategoryService.deactivate).toHaveBeenCalledWith("cat-2");
    expect(mockSnackBar.open).toHaveBeenCalledWith(
      "Category deactivated successfully.",
      "Close",
      jasmine.any(Object)
    );
    expect(mockCategoryService.list).toHaveBeenCalled();
  });
});
