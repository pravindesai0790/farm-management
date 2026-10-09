import { TestBed } from "@angular/core/testing";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { provideHttpClient } from "@angular/common/http";
import { environment } from "../../../environments/environment";
import { PlantProtectionService } from "./plant-protection.service";
import {
  CreatePlantProtectionProductRequest,
  PlantProtectionProductResponse,
  UpdatePlantProtectionProductRequest,
} from "./plant-protection.models";

describe("PlantProtectionService", () => {
  let service: PlantProtectionService;
  let httpTesting: HttpTestingController;

  const sampleProduct: PlantProtectionProductResponse = {
    id: "ppp-1",
    organizationId: "org-1",
    inventoryItemId: "item-1",
    inventoryItemName: "Copper Oxychloride 50 WP",
    inventoryItemSku: "COP-50",
    stockUnitId: "unit-1",
    stockUnitCode: "KG",
    stockUnitName: "Kilograms",
    stockUnitSymbol: "kg",
    productTypeId: "pt-1",
    productTypeCode: "FUNG",
    productTypeName: "Fungicide",
    activeIngredient: "Copper Oxychloride 50%",
    manufacturer: "AgroChem",
    description: "Curative and preventive fungicide",
    isActive: true,
    hasCompletedSprayUsage: false,
    createdAt: "2026-10-09T08:00:00Z",
    createdBy: "user-1",
    updatedAt: null,
    updatedBy: null,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        PlantProtectionService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(PlantProtectionService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it("lists plant protection products with query parameters", () => {
    service
      .listProducts({
        page: 2,
        pageSize: 10,
        search: "Copper",
        productTypeId: "pt-1",
        isActive: true,
      })
      .subscribe((res) => {
        expect(res.items.length).toBe(1);
        expect(res.items[0].inventoryItemName).toBe("Copper Oxychloride 50 WP");
      });

    const req = httpTesting.expectOne((r) => {
      return (
        r.url === `${environment.apiUrl}/plant-protection-products` &&
        r.params.get("page") === "2" &&
        r.params.get("pageSize") === "10" &&
        r.params.get("search") === "Copper" &&
        r.params.get("productTypeId") === "pt-1" &&
        r.params.get("isActive") === "true"
      );
    });

    expect(req.request.method).toBe("GET");
    req.flush({ items: [sampleProduct], totalCount: 1, page: 2, pageSize: 10, totalPages: 1 });
  });

  it("gets product by ID", () => {
    service.getProduct("ppp-1").subscribe((res) => {
      expect(res.id).toBe("ppp-1");
      expect(res.productTypeName).toBe("Fungicide");
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/plant-protection-products/ppp-1`);
    expect(req.request.method).toBe("GET");
    req.flush(sampleProduct);
  });

  it("gets product lookup list", () => {
    service.getProductLookup().subscribe((res) => {
      expect(res.length).toBe(1);
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/plant-protection-products/lookup`);
    expect(req.request.method).toBe("GET");
    req.flush([
      {
        inventoryItemId: "item-1",
        name: "Copper Oxychloride 50 WP",
        stockUnitId: "unit-1",
        stockUnitName: "Kilograms",
        productTypeId: "pt-1",
        productTypeCode: "FUNG",
        productTypeName: "Fungicide",
        plantProtectionProductId: "ppp-1",
      },
    ]);
  });

  it("creates a plant protection product profile", () => {
    const payload: CreatePlantProtectionProductRequest = {
      inventoryItemId: "item-1",
      productTypeId: "pt-1",
      activeIngredient: "Copper Oxychloride 50%",
      manufacturer: "AgroChem",
      description: "Preventive",
    };

    service.createProduct(payload).subscribe((res) => {
      expect(res.id).toBe("ppp-1");
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/plant-protection-products`);
    expect(req.request.method).toBe("POST");
    expect(req.request.body).toEqual(payload);
    req.flush(sampleProduct);
  });

  it("updates a plant protection product profile", () => {
    const payload: UpdatePlantProtectionProductRequest = {
      productTypeId: "pt-1",
      activeIngredient: "Updated Ingredient",
      manufacturer: "Updated Mfg",
      description: "Updated description",
    };

    service.updateProduct("ppp-1", payload).subscribe((res) => {
      expect(res.id).toBe("ppp-1");
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/plant-protection-products/ppp-1`);
    expect(req.request.method).toBe("PUT");
    expect(req.request.body).toEqual(payload);
    req.flush(sampleProduct);
  });

  it("activates a plant protection product profile", () => {
    service.activateProduct("ppp-1").subscribe();

    const req = httpTesting.expectOne(`${environment.apiUrl}/plant-protection-products/ppp-1/activate`);
    expect(req.request.method).toBe("PATCH");
    req.flush(null, { status: 204, statusText: "No Content" });
  });

  it("deactivates a plant protection product profile", () => {
    service.deactivateProduct("ppp-1").subscribe();

    const req = httpTesting.expectOne(`${environment.apiUrl}/plant-protection-products/ppp-1/deactivate`);
    expect(req.request.method).toBe("PATCH");
    req.flush(null, { status: 204, statusText: "No Content" });
  });
});
