import { provideHttpClient } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { environment } from "../../../environments/environment";
import {
  IrrigationListItem,
  IrrigationMethodDto,
  IrrigationSummaryCounts,
} from "./irrigation.models";
import { IrrigationService } from "./irrigation.service";

describe("IrrigationService", () => {
  let service: IrrigationService;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiUrl}/irrigations`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        IrrigationService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(IrrigationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it("should be created", () => {
    expect(service).toBeTruthy();
  });

  it("should request irrigations with default pagination query params", () => {
    const mockResponse = {
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
      totalPages: 0,
    };

    service.listIrrigations().subscribe((res) => {
      expect(res.items).toEqual([]);
      expect(res.totalCount).toBe(0);
    });

    const req = httpMock.expectOne((r) => r.url === baseUrl);
    expect(req.request.method).toBe("GET");
    expect(req.request.params.get("pageNumber")).toBe("1");
    expect(req.request.params.get("pageSize")).toBe("20");
    req.flush(mockResponse);
  });

  it("should request irrigations with full filter query parameters", () => {
    service
      .listIrrigations({
        farmId: "farm-1",
        farmAreaId: "area-1",
        plantationId: "plant-1",
        cropCycleId: "cycle-1",
        cropCycleStageId: "stage-1",
        irrigationMethodId: "method-1",
        status: "Scheduled",
        fromDate: "2026-10-01",
        toDate: "2026-10-31",
        search: "North field",
        includeOverdue: true,
        pageNumber: 2,
        pageSize: 50,
      })
      .subscribe();

    const req = httpMock.expectOne((r) => r.url === baseUrl);
    expect(req.request.method).toBe("GET");
    expect(req.request.params.get("farmId")).toBe("farm-1");
    expect(req.request.params.get("farmAreaId")).toBe("area-1");
    expect(req.request.params.get("plantationId")).toBe("plant-1");
    expect(req.request.params.get("cropCycleId")).toBe("cycle-1");
    expect(req.request.params.get("cropCycleStageId")).toBe("stage-1");
    expect(req.request.params.get("irrigationMethodId")).toBe("method-1");
    expect(req.request.params.get("status")).toBe("Scheduled");
    expect(req.request.params.get("fromDate")).toBe("2026-10-01");
    expect(req.request.params.get("toDate")).toBe("2026-10-31");
    expect(req.request.params.get("search")).toBe("North field");
    expect(req.request.params.get("includeOverdue")).toBe("true");
    expect(req.request.params.get("pageNumber")).toBe("2");
    expect(req.request.params.get("pageSize")).toBe("50");
    req.flush({ items: [], totalCount: 0, page: 2, pageSize: 50, totalPages: 0 });
  });

  it("should request summary counts with optional farmId", () => {
    const mockSummary: IrrigationSummaryCounts = {
      totalCount: 10,
      draftCount: 2,
      scheduledCount: 4,
      inProgressCount: 1,
      completedCount: 2,
      cancelledCount: 1,
      overdueCount: 2,
    };

    service.getSummaryCounts("farm-123").subscribe((res) => {
      expect(res.totalCount).toBe(10);
      expect(res.scheduledCount).toBe(4);
      expect(res.overdueCount).toBe(2);
    });

    const req = httpMock.expectOne((r) => r.url === `${baseUrl}/summary`);
    expect(req.request.method).toBe("GET");
    expect(req.request.params.get("farmId")).toBe("farm-123");
    req.flush(mockSummary);
  });

  it("should request active methods", () => {
    const mockMethods: IrrigationMethodDto[] = [
      {
        id: "m-1",
        organizationId: null,
        code: "DRIP",
        name: "Drip Irrigation",
        description: null,
        displayOrder: 1,
        isSystem: true,
        isActive: true,
      },
    ];

    service.getMethods(true).subscribe((res) => {
      expect(res.length).toBe(1);
      expect(res[0].code).toBe("DRIP");
    });

    const req = httpMock.expectOne((r) => r.url === `${baseUrl}/methods`);
    expect(req.request.method).toBe("GET");
    expect(req.request.params.get("activeOnly")).toBe("true");
    req.flush(mockMethods);
  });

  it("should request irrigation by id", () => {
    const id = "irr-999";
    service.getIrrigation(id).subscribe((res) => {
      expect(res.id).toBe(id);
    });

    const req = httpMock.expectOne(`${baseUrl}/${id}`);
    expect(req.request.method).toBe("GET");
    req.flush({ id } as any);
  });

  it("should post create draft to /irrigations", () => {
    const payload = {
      farmId: "farm-1",
      farmAreaId: "area-1",
      plantationId: "plant-1",
      cropCycleId: "cycle-1",
      cropCycleStageId: "stage-1",
      irrigationMethodId: "method-1",
      plannedAt: "2026-10-20T10:00:00Z",
      plannedWaterQuantity: 500,
      plannedWaterUnitId: "unit-l",
      notes: "Test draft",
    };

    service.createDraft(payload).subscribe((res) => {
      expect(res.id).toBe("irr-new");
    });

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe("POST");
    expect(req.request.body).toEqual(payload);
    req.flush({ id: "irr-new", ...payload } as any);
  });

  it("should put update draft to /irrigations/:id", () => {
    const id = "irr-123";
    const payload = {
      farmId: "farm-1",
      farmAreaId: "area-1",
      plantationId: null,
      cropCycleId: null,
      cropCycleStageId: null,
      irrigationMethodId: null,
      plannedAt: null,
      plannedWaterQuantity: 300,
      plannedWaterUnitId: "unit-l",
      notes: "Updated note",
      concurrencyToken: "ct-123",
    };

    service.updateDraft(id, payload).subscribe((res) => {
      expect(res.id).toBe(id);
    });

    const req = httpMock.expectOne(`${baseUrl}/${id}`);
    expect(req.request.method).toBe("PUT");
    expect(req.request.body).toEqual(payload);
    req.flush({ id, ...payload } as any);
  });
});
