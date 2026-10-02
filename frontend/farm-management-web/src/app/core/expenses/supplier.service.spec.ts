import { provideHttpClient } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";

import { environment } from "../../../environments/environment";
import { CreateSupplierRequest, Supplier, SupplierList, UpdateSupplierRequest } from "./supplier.models";
import { SupplierService } from "./supplier.service";

describe("SupplierService", () => {
  let service: SupplierService;
  let httpTesting: HttpTestingController;

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

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SupplierService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(SupplierService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it("lists suppliers with query parameters", () => {
    const mockResponse: SupplierList = {
      items: [sampleSupplier],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    };

    service.list(1, 20, "Agri", true).subscribe((res) => {
      expect(res.items.length).toBe(1);
      expect(res.items[0].name).toBe("Agri Corp");
    });

    const req = httpTesting.expectOne((r) =>
      r.url === `${environment.apiUrl}/suppliers` &&
      r.params.get("page") === "1" &&
      r.params.get("pageSize") === "20" &&
      r.params.get("search") === "Agri" &&
      r.params.get("isActive") === "true"
    );
    expect(req.request.method).toBe("GET");
    req.flush(mockResponse);
  });

  it("gets a supplier by id", () => {
    service.get("sup-1").subscribe((res) => {
      expect(res.id).toBe("sup-1");
      expect(res.name).toBe("Agri Corp");
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/suppliers/sup-1`);
    expect(req.request.method).toBe("GET");
    req.flush(sampleSupplier);
  });

  it("creates a supplier", () => {
    const request: CreateSupplierRequest = {
      name: "New Supplier",
      contactPerson: "Alice",
      phone: "555-1234",
      email: "alice@test.com",
      address: null,
      registrationIdentifier: null,
      notes: null,
    };

    service.create(request).subscribe((res) => {
      expect(res.name).toBe("New Supplier");
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/suppliers`);
    expect(req.request.method).toBe("POST");
    expect(req.request.body).toEqual(request);
    req.flush({ ...sampleSupplier, name: "New Supplier" });
  });

  it("updates a supplier", () => {
    const request: UpdateSupplierRequest = {
      name: "Updated Supplier",
      contactPerson: "Alice Updated",
      phone: null,
      email: null,
      address: null,
      registrationIdentifier: null,
      notes: null,
    };

    service.update("sup-1", request).subscribe((res) => {
      expect(res.name).toBe("Updated Supplier");
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/suppliers/sup-1`);
    expect(req.request.method).toBe("PUT");
    expect(req.request.body).toEqual(request);
    req.flush({ ...sampleSupplier, name: "Updated Supplier" });
  });

  it("activates and deactivates a supplier", () => {
    service.activate("sup-1").subscribe();
    const actReq = httpTesting.expectOne(`${environment.apiUrl}/suppliers/sup-1/activate`);
    expect(actReq.request.method).toBe("PATCH");
    actReq.flush(null);

    service.deactivate("sup-1").subscribe();
    const deactReq = httpTesting.expectOne(`${environment.apiUrl}/suppliers/sup-1/deactivate`);
    expect(deactReq.request.method).toBe("PATCH");
    deactReq.flush(null);
  });
});
