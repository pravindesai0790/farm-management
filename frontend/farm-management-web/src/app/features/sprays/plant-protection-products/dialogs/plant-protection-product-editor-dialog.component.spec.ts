import { ComponentFixture, TestBed } from "@angular/core/testing";
import { MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { of, throwError } from "rxjs";

import { InventoryItem } from "../../../../core/inventory/inventory.models";
import { InventoryService } from "../../../../core/inventory/inventory.service";
import {
  PlantProtectionProductResponse,
} from "../../../../core/plant-protection/plant-protection.models";
import { PlantProtectionService } from "../../../../core/plant-protection/plant-protection.service";
import { ProductTypeResponse } from "../../../../core/sprays/spray.models";
import { SprayService } from "../../../../core/sprays/spray.service";
import {
  PlantProtectionProductEditorDialogComponent,
  PlantProtectionProductEditorDialogData,
} from "./plant-protection-product-editor-dialog.component";

describe("PlantProtectionProductEditorDialogComponent", () => {
  let component: PlantProtectionProductEditorDialogComponent;
  let fixture: ComponentFixture<PlantProtectionProductEditorDialogComponent>;

  let mockDialogRef: jasmine.SpyObj<MatDialogRef<PlantProtectionProductEditorDialogComponent>>;
  let mockPlantProtectionService: jasmine.SpyObj<PlantProtectionService>;
  let mockSprayService: jasmine.SpyObj<SprayService>;
  let mockInventoryService: jasmine.SpyObj<InventoryService>;
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
    {
      id: "pt-2",
      code: "INSECT",
      name: "Insecticide",
      description: null,
      displayOrder: 2,
      isSystem: true,
      isActive: true,
    },
  ];

  const sampleInventoryItems: InventoryItem[] = [
    {
      id: "item-1",
      organizationId: "org-1",
      name: "Copper Oxychloride 50 WP",
      sku: "COP-50",
      categoryId: "cat-1",
      categoryName: "Chemicals",
      categoryIcon: "science",
      stockUnitId: "u-kg",
      stockUnitCode: "KG",
      stockUnitName: "Kilograms",
      stockUnitSymbol: "kg",
      description: "Fungicide powder",
      isActive: true,
      createdAt: "2026-10-01T00:00:00Z",
      createdBy: "user-1",
    },
  ];

  const sampleProductResponse: PlantProtectionProductResponse = {
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
    hasCompletedSprayUsage: false,
    createdAt: "2026-10-09T08:00:00Z",
    createdBy: "user-1",
    updatedAt: null,
    updatedBy: null,
  };

  async function createComponent(dialogData: PlantProtectionProductEditorDialogData = {}) {
    mockDialogRef = jasmine.createSpyObj("MatDialogRef", ["close"]);
    mockPlantProtectionService = jasmine.createSpyObj("PlantProtectionService", [
      "createProduct",
      "updateProduct",
    ]);
    mockSprayService = jasmine.createSpyObj("SprayService", ["getProductTypes"]);
    mockInventoryService = jasmine.createSpyObj("InventoryService", ["listItems"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);

    mockSprayService.getProductTypes.and.returnValue(of(sampleProductTypes));
    mockInventoryService.listItems.and.returnValue(
      of({ items: sampleInventoryItems, totalCount: 1, page: 1, pageSize: 100, totalPages: 1 } as any),
    );
    mockPlantProtectionService.createProduct.and.returnValue(of(sampleProductResponse));
    mockPlantProtectionService.updateProduct.and.returnValue(of(sampleProductResponse));

    await TestBed.configureTestingModule({
      imports: [PlantProtectionProductEditorDialogComponent, NoopAnimationsModule],
      providers: [
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: dialogData },
        { provide: PlantProtectionService, useValue: mockPlantProtectionService },
        { provide: SprayService, useValue: mockSprayService },
        { provide: InventoryService, useValue: mockInventoryService },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PlantProtectionProductEditorDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it("initializes in Create mode and loads lookups", async () => {
    await createComponent({});

    expect(component.isEditMode()).toBeFalse();
    expect(component.productTypes().length).toBe(2);
    expect(component.availableInventoryItems().length).toBe(1);
    expect(component.form.controls.inventoryItemId.valid).toBeFalse();
    expect(component.form.controls.productTypeId.valid).toBeFalse();
  });

  it("submits valid create payload and closes dialog", async () => {
    await createComponent({});

    component.form.patchValue({
      inventoryItemId: "item-1",
      productTypeId: "pt-1",
      activeIngredient: "Copper Oxychloride 50%",
      manufacturer: "AgroChem",
      description: "Preventative",
    });

    component.onSubmit();

    expect(mockPlantProtectionService.createProduct).toHaveBeenCalledWith({
      inventoryItemId: "item-1",
      productTypeId: "pt-1",
      activeIngredient: "Copper Oxychloride 50%",
      manufacturer: "AgroChem",
      description: "Preventative",
    });

    expect(mockDialogRef.close).toHaveBeenCalledWith(sampleProductResponse);
  });

  it("initializes in Edit mode without spray usage allowing product type changes", async () => {
    await createComponent({ product: sampleProductResponse });

    expect(component.isEditMode()).toBeTrue();
    expect(component.hasCompletedSprayUsage()).toBeFalse();
    expect(component.form.controls.productTypeId.enabled).toBeTrue();
    expect(component.form.controls.activeIngredient.value).toBe("Copper Oxychloride 50%");
  });

  it("locks ProductType in Edit mode when product has completed spray usage", async () => {
    const usedProduct: PlantProtectionProductResponse = {
      ...sampleProductResponse,
      hasCompletedSprayUsage: true,
    };

    await createComponent({ product: usedProduct });

    expect(component.isEditMode()).toBeTrue();
    expect(component.hasCompletedSprayUsage()).toBeTrue();
    expect(component.form.controls.productTypeId.disabled).toBeTrue();
    expect(component.form.controls.activeIngredient.enabled).toBeTrue();
    expect(component.form.controls.manufacturer.enabled).toBeTrue();
  });

  it("submits valid update payload and closes dialog", async () => {
    await createComponent({ product: sampleProductResponse });

    component.form.patchValue({
      productTypeId: "pt-2",
      activeIngredient: "Updated Active",
      manufacturer: "Updated Mfg",
      description: "Updated desc",
    });

    component.onSubmit();

    expect(mockPlantProtectionService.updateProduct).toHaveBeenCalledWith("ppp-1", {
      productTypeId: "pt-2",
      activeIngredient: "Updated Active",
      manufacturer: "Updated Mfg",
      description: "Updated desc",
    });

    expect(mockDialogRef.close).toHaveBeenCalledWith(sampleProductResponse);
  });

  it("handles backend error on submission", async () => {
    await createComponent({});

    mockPlantProtectionService.createProduct.and.returnValue(
      throwError(() => ({
        status: 409,
        error: { message: "A plant protection product profile already exists for this item." },
      })),
    );

    component.form.patchValue({
      inventoryItemId: "item-1",
      productTypeId: "pt-1",
    });

    component.onSubmit();

    expect(component.errorMessage()).toContain("already exists");
    expect(component.isSubmitting()).toBeFalse();
    expect(mockDialogRef.close).not.toHaveBeenCalled();
  });

  it("closes dialog on cancel", async () => {
    await createComponent({});

    component.onCancel();
    expect(mockDialogRef.close).toHaveBeenCalled();
  });
});
