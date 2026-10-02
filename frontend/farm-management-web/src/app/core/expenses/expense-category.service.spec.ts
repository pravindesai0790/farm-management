import { provideHttpClient } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";

import { environment } from "../../../environments/environment";
import {
  CreateExpenseCategoryRequest,
  ExpenseCategory,
  ExpenseCategoryList,
  UpdateExpenseCategoryRequest,
} from "./expense-category.models";
import { ExpenseCategoryService } from "./expense-category.service";

describe("ExpenseCategoryService", () => {
  let service: ExpenseCategoryService;
  let httpTesting: HttpTestingController;

  const sampleCategory: ExpenseCategory = {
    id: "cat-1",
    organizationId: "org-1",
    name: "Specialized Seeds",
    code: null,
    description: "Hybrid and organic seeds",
    isSystemDefault: false,
    isActive: true,
    createdAt: "2026-10-01T00:00:00Z",
    createdBy: "usr-1",
    updatedAt: null,
    updatedBy: null,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ExpenseCategoryService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(ExpenseCategoryService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it("lists expense categories with query parameters", () => {
    const mockResponse: ExpenseCategoryList = {
      items: [sampleCategory],
      page: 1,
      pageSize: 50,
      totalCount: 1,
    };

    service.list(1, 50, "Seeds", true).subscribe((res) => {
      expect(res.items.length).toBe(1);
      expect(res.items[0].name).toBe("Specialized Seeds");
    });

    const req = httpTesting.expectOne((r) =>
      r.url === `${environment.apiUrl}/expense-categories` &&
      r.params.get("page") === "1" &&
      r.params.get("pageSize") === "50" &&
      r.params.get("search") === "Seeds" &&
      r.params.get("isActive") === "true"
    );
    expect(req.request.method).toBe("GET");
    req.flush(mockResponse);
  });

  it("creates a custom expense category", () => {
    const request: CreateExpenseCategoryRequest = {
      name: "New Category",
      description: "Category description",
    };

    service.create(request).subscribe((res) => {
      expect(res.name).toBe("New Category");
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/expense-categories`);
    expect(req.request.method).toBe("POST");
    expect(req.request.body).toEqual(request);
    req.flush({ ...sampleCategory, name: "New Category" });
  });

  it("updates a custom expense category", () => {
    const request: UpdateExpenseCategoryRequest = {
      name: "Updated Category",
      description: "Updated description",
    };

    service.update("cat-1", request).subscribe((res) => {
      expect(res.name).toBe("Updated Category");
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/expense-categories/cat-1`);
    expect(req.request.method).toBe("PUT");
    expect(req.request.body).toEqual(request);
    req.flush({ ...sampleCategory, name: "Updated Category" });
  });

  it("activates and deactivates an expense category", () => {
    service.activate("cat-1").subscribe();
    const actReq = httpTesting.expectOne(`${environment.apiUrl}/expense-categories/cat-1/activate`);
    expect(actReq.request.method).toBe("PATCH");
    actReq.flush(null);

    service.deactivate("cat-1").subscribe();
    const deactReq = httpTesting.expectOne(`${environment.apiUrl}/expense-categories/cat-1/deactivate`);
    expect(deactReq.request.method).toBe("PATCH");
    deactReq.flush(null);
  });
});
