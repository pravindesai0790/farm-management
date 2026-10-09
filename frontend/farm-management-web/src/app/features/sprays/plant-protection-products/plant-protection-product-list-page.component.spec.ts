import { ComponentFixture, TestBed, fakeAsync, tick } from "@angular/core/testing";
import { MatDialog } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { of, throwError } from "rxjs";

import { PermissionService } from "../../../core/auth/permission.service";
import {
  PlantProtectionProductResponse,
} from "../../../core/plant-protection/plant-protection.models";
import { PlantProtectionService } from "../../../core/plant-protection/plant-protection.service";
import { ProductTypeResponse } from "../../../core/sprays/spray.models";
import { SprayService } from "../../../core/sprays/spray.service";
import { PlantProtectionProductListPageComponent } from "./plant-protection-product-list-page.component";

describe("PlantProtectionProductListPageComponent", () => {
  let component: PlantProtectionProductListPageComponent;
  let fixture: ComponentFixture<PlantProtectionProductListPageComponent>;

  let mockPlantProtectionService: jasmine.SpyObj<PlantProtectionService>;
  let mockSprayService: jasmine.SpyObj<SprayService>;
  let mockPermissionService: jasmine.SpyObj<PermissionService>;
  let mockDialog: jasmine.SpyObj<MatDialog>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const sampleProductTypes: ProductTypeResponse[] = [
    {
      id: "pt-1",
      code: "FUNG",
      name: "Fungicide",
      description: null,
      displayOrder: 1,
      isSystem: true,
      isActive: true,
    },
  ];

  const sampleProducts: PlantProtectionProductResponse[] = [
    {
      id: "ppp-1",
      organizationId: "org-1",
      inventoryItemId: "item-1",
      inventoryItemName: "Copper Oxychloride 50 WP",
      inventoryItemSku: "COP-50",
      stockUnitId: "u-kg",
      stockUnitCode: "KG",
      stockUnitName: "Kilograms",
      stockUnitSymbol: "kg",
      productTypeId: "pt-1",
      productTypeCode: "FUNG",
      productTypeName: "Fungicide",
      activeIngredient: "Copper Oxychloride 50%",
      manufacturer: "AgroChem",
      description: "Preventative",
      isActive: true,
      hasCompletedSprayUsage: true,
      createdAt: "2026-10-09T08:00:00Z",
      createdBy: "user-1",
      updatedAt: null,
      updatedBy: null,
    },
    {
      id: "ppp-2",
      organizationId: "org-1",
      inventoryItemId: "item-2",
      inventoryItemName: "Sulphur 80 WDG",
      inventoryItemSku: "SUL-80",
      stockUnitId: "u-kg",
      stockUnitCode: "KG",
      stockUnitName: "Kilograms",
      stockUnitSymbol: "kg",
      productTypeId: "pt-1",
      productTypeCode: "FUNG",
      productTypeName: "Fungicide",
      activeIngredient: "Sulphur 80%",
      manufacturer: "AgroChem",
      description: "Dusting powder",
      isActive: false,
      hasCompletedSprayUsage: false,
      createdAt: "2026-10-09T08:00:00Z",
      createdBy: "user-1",
      updatedAt: null,
      updatedBy: null,
    },
  ];

  beforeEach(async () => {
    mockPlantProtectionService = jasmine.createSpyObj("PlantProtectionService", [
      "listProducts",
      "activateProduct",
      "deactivateProduct",
    ]);
    mockSprayService = jasmine.createSpyObj("SprayService", ["getProductTypes"]);
    mockPermissionService = jasmine.createSpyObj("PermissionService", ["has"]);
    mockDialog = jasmine.createSpyObj("MatDialog", ["open"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);

    mockPermissionService.has.and.returnValue(true);
    mockSprayService.getProductTypes.and.returnValue(of(sampleProductTypes));
    mockPlantProtectionService.listProducts.and.returnValue(
      of({ items: sampleProducts, totalCount: 2, page: 1, pageSize: 20, totalPages: 1 }),
    );
    mockPlantProtectionService.activateProduct.and.returnValue(of(undefined as any));
    mockPlantProtectionService.deactivateProduct.and.returnValue(of(undefined as any));

    await TestBed.configureTestingModule({
      imports: [PlantProtectionProductListPageComponent, NoopAnimationsModule],
      providers: [
        { provide: PlantProtectionService, useValue: mockPlantProtectionService },
        { provide: SprayService, useValue: mockSprayService },
        { provide: PermissionService, useValue: mockPermissionService },
        { provide: MatDialog, useValue: mockDialog },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PlantProtectionProductListPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("creates component and loads list and product types", () => {
    expect(component).toBeTruthy();
    expect(component.items().length).toBe(2);
    expect(component.totalCount()).toBe(2);
    expect(component.productTypes().length).toBe(1);
    expect(mockPlantProtectionService.listProducts).toHaveBeenCalled();
  });

  it("filters by search term with debounce", fakeAsync(() => {
    component.onSearchChange("Copper");
    tick(350);

    expect(mockPlantProtectionService.listProducts).toHaveBeenCalledWith(
      jasmine.objectContaining({ search: "Copper", page: 1 }),
    );
  }));

  it("filters by product type", () => {
    component.onTypeChange("pt-1");

    expect(mockPlantProtectionService.listProducts).toHaveBeenCalledWith(
      jasmine.objectContaining({ productTypeId: "pt-1", page: 1 }),
    );
  });

  it("filters by active and inactive status", () => {
    component.onStatusChange("active");
    expect(mockPlantProtectionService.listProducts).toHaveBeenCalledWith(
      jasmine.objectContaining({ isActive: true, page: 1 }),
    );

    component.onStatusChange("inactive");
    expect(mockPlantProtectionService.listProducts).toHaveBeenCalledWith(
      jasmine.objectContaining({ isActive: false, page: 1 }),
    );
  });

  it("handles pagination change", () => {
    component.onPageChange({ pageIndex: 2, pageSize: 10, length: 50 });

    expect(component.pageIndex()).toBe(2);
    expect(component.pageSize()).toBe(10);
    expect(mockPlantProtectionService.listProducts).toHaveBeenCalledWith(
      jasmine.objectContaining({ page: 3, pageSize: 10 }),
    );
  });

  it("opens create dialog and reloads list when created", () => {
    mockDialog.open.and.returnValue({
      afterClosed: () => of(sampleProducts[0]),
    } as any);

    component.openCreateDialog();

    expect(mockDialog.open).toHaveBeenCalled();
    expect(mockPlantProtectionService.listProducts).toHaveBeenCalledTimes(2);
  });

  it("opens edit dialog and reloads list when updated", () => {
    mockDialog.open.and.returnValue({
      afterClosed: () => of(sampleProducts[0]),
    } as any);

    component.openEditDialog(sampleProducts[0]);

    expect(mockDialog.open).toHaveBeenCalledWith(
      jasmine.any(Function),
      jasmine.objectContaining({ data: { product: sampleProducts[0] } }),
    );
    expect(mockPlantProtectionService.listProducts).toHaveBeenCalledTimes(2);
  });

  it("deactivates active product profile", () => {
    component.toggleActive(sampleProducts[0]);

    expect(mockPlantProtectionService.deactivateProduct).toHaveBeenCalledWith("ppp-1");
    expect(mockSnackBar.open).toHaveBeenCalledWith(
      jasmine.stringContaining("deactivated"),
      "Dismiss",
      jasmine.any(Object),
    );
  });

  it("activates inactive product profile", () => {
    component.toggleActive(sampleProducts[1]);

    expect(mockPlantProtectionService.activateProduct).toHaveBeenCalledWith("ppp-2");
    expect(mockSnackBar.open).toHaveBeenCalledWith(
      jasmine.stringContaining("activated"),
      "Dismiss",
      jasmine.any(Object),
    );
  });

  it("handles error during loading", () => {
    mockPlantProtectionService.listProducts.and.returnValue(
      throwError(() => ({ status: 500, error: { message: "Internal server error" } })),
    );

    component.load();

    expect(component.errorMessage()).toContain("Internal server error");
    expect(component.isLoading()).toBeFalse();
  });
});
