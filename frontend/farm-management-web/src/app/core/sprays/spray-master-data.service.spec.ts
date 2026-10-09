import { TestBed } from "@angular/core/testing";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { provideHttpClient } from "@angular/common/http";
import { environment } from "../../../environments/environment";
import { SprayMasterDataService } from "./spray-master-data.service";
import {
  ApplicationMethodItem,
  CreateApplicationMethodRequest,
  CreateProductTypeRequest,
  CreateTargetRequest,
  ProductTypeItem,
  TargetItem,
  UpdateApplicationMethodRequest,
  UpdateProductTypeRequest,
  UpdateTargetRequest,
} from "./spray-master-data.models";

describe("SprayMasterDataService", () => {
  let service: SprayMasterDataService;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiUrl}/master-data`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), SprayMasterDataService],
    });
    service = TestBed.inject(SprayMasterDataService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  // --- Product Types ---
  it("should list product types with includeInactive query param", () => {
    const mockData: ProductTypeItem[] = [
      { id: "1", code: "FUNG", name: "Fungicide", description: null, displayOrder: 1, isSystem: true, isActive: true },
    ];

    service.listProductTypes(true).subscribe((res) => {
      expect(res).toEqual(mockData);
    });

    const req = httpMock.expectOne(`${baseUrl}/product-types?includeInactive=true`);
    expect(req.request.method).toBe("GET");
    req.flush(mockData);
  });

  it("should create product type", () => {
    const request: CreateProductTypeRequest = { code: "BIO", name: "Biological", description: "Bio agent", displayOrder: 2 };
    const mockCreated: ProductTypeItem = { id: "2", code: "BIO", name: "Biological", description: "Bio agent", displayOrder: 2, isSystem: false, isActive: true };

    service.createProductType(request).subscribe((res) => {
      expect(res).toEqual(mockCreated);
    });

    const req = httpMock.expectOne(`${baseUrl}/product-types`);
    expect(req.request.method).toBe("POST");
    expect(req.request.body).toEqual(request);
    req.flush(mockCreated);
  });

  it("should update product type", () => {
    const request: UpdateProductTypeRequest = { name: "Updated Name", description: "New desc", displayOrder: 3 };
    const mockUpdated: ProductTypeItem = { id: "2", code: "BIO", name: "Updated Name", description: "New desc", displayOrder: 3, isSystem: false, isActive: true };

    service.updateProductType("2", request).subscribe((res) => {
      expect(res).toEqual(mockUpdated);
    });

    const req = httpMock.expectOne(`${baseUrl}/product-types/2`);
    expect(req.request.method).toBe("PUT");
    expect(req.request.body).toEqual(request);
    req.flush(mockUpdated);
  });

  it("should activate and deactivate product type", () => {
    service.activateProductType("2").subscribe();
    const reqAct = httpMock.expectOne(`${baseUrl}/product-types/2/activate`);
    expect(reqAct.request.method).toBe("PATCH");
    reqAct.flush(null);

    service.deactivateProductType("2").subscribe();
    const reqDeact = httpMock.expectOne(`${baseUrl}/product-types/2/deactivate`);
    expect(reqDeact.request.method).toBe("PATCH");
    reqDeact.flush(null);
  });

  // --- Targets ---
  it("should list targets with optional type filter", () => {
    const mockTargets: TargetItem[] = [
      { id: "t1", code: "MILDEW", name: "Powdery Mildew", targetType: "Disease", description: null, displayOrder: 1, isSystem: true, isActive: true },
    ];

    service.listTargets("Disease", false).subscribe((res) => {
      expect(res).toEqual(mockTargets);
    });

    const req = httpMock.expectOne(`${baseUrl}/targets?includeInactive=false&type=Disease`);
    expect(req.request.method).toBe("GET");
    req.flush(mockTargets);
  });

  it("should create and update target", () => {
    const createReq: CreateTargetRequest = { code: "NEW_PEST", name: "New Pest", targetType: "Insect", displayOrder: 5 };
    const mockTarget: TargetItem = { id: "t2", code: "NEW_PEST", name: "New Pest", targetType: "Insect", description: null, displayOrder: 5, isSystem: false, isActive: true };

    service.createTarget(createReq).subscribe((res) => {
      expect(res).toEqual(mockTarget);
    });

    const reqCreate = httpMock.expectOne(`${baseUrl}/targets`);
    expect(reqCreate.request.method).toBe("POST");
    reqCreate.flush(mockTarget);

    const updateReq: UpdateTargetRequest = { name: "Updated Pest", targetType: "Insect", displayOrder: 6 };
    service.updateTarget("t2", updateReq).subscribe((res) => {
      expect(res.name).toBe("Updated Pest");
    });

    const reqUpdate = httpMock.expectOne(`${baseUrl}/targets/t2`);
    expect(reqUpdate.request.method).toBe("PUT");
    reqUpdate.flush({ ...mockTarget, name: "Updated Pest", displayOrder: 6 });
  });

  it("should activate and deactivate target", () => {
    service.activateTarget("t2").subscribe();
    const reqAct = httpMock.expectOne(`${baseUrl}/targets/t2/activate`);
    expect(reqAct.request.method).toBe("PATCH");
    reqAct.flush(null);

    service.deactivateTarget("t2").subscribe();
    const reqDeact = httpMock.expectOne(`${baseUrl}/targets/t2/deactivate`);
    expect(reqDeact.request.method).toBe("PATCH");
    reqDeact.flush(null);
  });

  // --- Application Methods ---
  it("should list, create, update, and toggle application methods", () => {
    const mockMethods: ApplicationMethodItem[] = [
      { id: "m1", code: "DRONE", name: "Drone", description: null, displayOrder: 1, isSystem: true, isActive: true },
    ];

    service.listApplicationMethods(true).subscribe((res) => {
      expect(res).toEqual(mockMethods);
    });

    const reqList = httpMock.expectOne(`${baseUrl}/application-methods?includeInactive=true`);
    expect(reqList.request.method).toBe("GET");
    reqList.flush(mockMethods);

    const createReq: CreateApplicationMethodRequest = { code: "CUST_METH", name: "Custom Method", displayOrder: 4 };
    service.createApplicationMethod(createReq).subscribe();
    const reqCreate = httpMock.expectOne(`${baseUrl}/application-methods`);
    expect(reqCreate.request.method).toBe("POST");
    reqCreate.flush({ ...mockMethods[0], id: "m2", code: "CUST_METH", name: "Custom Method" });

    const updateReq: UpdateApplicationMethodRequest = { name: "Renamed Method" };
    service.updateApplicationMethod("m2", updateReq).subscribe();
    const reqUpdate = httpMock.expectOne(`${baseUrl}/application-methods/m2`);
    expect(reqUpdate.request.method).toBe("PUT");
    reqUpdate.flush({ ...mockMethods[0], id: "m2", name: "Renamed Method" });

    service.activateApplicationMethod("m2").subscribe();
    const reqAct = httpMock.expectOne(`${baseUrl}/application-methods/m2/activate`);
    expect(reqAct.request.method).toBe("PATCH");
    reqAct.flush(null);

    service.deactivateApplicationMethod("m2").subscribe();
    const reqDeact = httpMock.expectOne(`${baseUrl}/application-methods/m2/deactivate`);
    expect(reqDeact.request.method).toBe("PATCH");
    reqDeact.flush(null);
  });
});
