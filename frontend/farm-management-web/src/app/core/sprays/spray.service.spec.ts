import { provideHttpClient } from "@angular/common/http";
import {
  HttpTestingController,
  provideHttpClientTesting,
} from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";

import { environment } from "../../../environments/environment";
import { PagedResponse } from "../models/paged-response.model";
import {
  ProductTypeResponse,
  RescheduleSprayRequest,
  ScheduleSprayRequest,
  SprayListItem,
  SprayListQuery,
  TargetResponse,
} from "./spray.models";
import { SprayService } from "./spray.service";

describe("SprayService", () => {
  let service: SprayService;
  let httpTesting: HttpTestingController;

  const sampleSprayListItem: SprayListItem = {
    id: "sp-1",
    referenceNumber: "SP-2026-0001",
    farmId: "farm-1",
    farmName: "Sunrise Vineyard",
    farmAreaId: "area-1",
    farmAreaName: "North Block",
    plantationId: "plant-1",
    plantationName: "Cabernet Block",
    cropCycleId: "cycle-1",
    cropCycleName: "2026 Main Season",
    cropCycleStageId: "stage-1",
    cropCycleStageName: "Flowering",
    status: "Scheduled",
    statusName: "Scheduled",
    isOverdue: false,
    plannedDate: "2026-10-15",
    scheduledDateTime: "2026-10-15T08:00:00Z",
    actualApplicationDateTime: null,
    targetId: "target-1",
    targetName: "Powdery Mildew",
    applicationMethodId: "app-1",
    applicationMethodName: "Air Blast",
    productCount: 2,
    createdAt: "2026-10-09T08:00:00Z",
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        SprayService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(SprayService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it("lists sprays with query parameters", () => {
    const query: SprayListQuery = {
      farmId: "farm-1",
      farmAreaId: "area-1",
      plantationId: "plant-1",
      cropCycleId: "cycle-1",
      cropCycleStageId: "stage-1",
      status: "Scheduled",
      targetId: "target-1",
      fromDate: "2026-10-01",
      toDate: "2026-10-31",
      search: "Powdery",
      includeOverdue: true,
      pageNumber: 1,
      pageSize: 20,
    };

    const mockResponse: PagedResponse<SprayListItem> = {
      items: [sampleSprayListItem],
      totalCount: 1,
      pageNumber: 1,
      pageSize: 20,
      totalPages: 1,
    };

    service.listSprays(query).subscribe((res) => {
      expect(res).toEqual(mockResponse);
      expect(res.items.length).toBe(1);
      expect(res.items[0].referenceNumber).toBe("SP-2026-0001");
    });

    const req = httpTesting.expectOne((request) => {
      const p = request.params;
      return (
        request.url === `${environment.apiUrl}/sprays` &&
        p.get("farmId") === "farm-1" &&
        p.get("farmAreaId") === "area-1" &&
        p.get("plantationId") === "plant-1" &&
        p.get("cropCycleId") === "cycle-1" &&
        p.get("cropCycleStageId") === "stage-1" &&
        p.get("status") === "Scheduled" &&
        p.get("targetId") === "target-1" &&
        p.get("fromDate") === "2026-10-01" &&
        p.get("toDate") === "2026-10-31" &&
        p.get("search") === "Powdery" &&
        p.get("includeOverdue") === "true" &&
        p.get("pageNumber") === "1" &&
        p.get("pageSize") === "20"
      );
    });

    expect(req.request.method).toBe("GET");
    req.flush(mockResponse);
  });

  it("cancels a spray with reason", () => {
    service.cancelSpray("sp-1", "Inclement heavy rain and wind").subscribe();

    const req = httpTesting.expectOne(`${environment.apiUrl}/sprays/sp-1/cancel`);
    expect(req.request.method).toBe("POST");
    expect(req.request.body).toEqual({ cancellationReason: "Inclement heavy rain and wind" });
    req.flush(null, { status: 204, statusText: "No Content" });
  });

  it("schedules a draft spray", () => {
    const schedulePayload: ScheduleSprayRequest = {
      scheduledDateTime: "2026-10-16T07:30:00Z",
    };

    service.scheduleSpray("sp-1", schedulePayload).subscribe();

    const req = httpTesting.expectOne(`${environment.apiUrl}/sprays/sp-1/schedule`);
    expect(req.request.method).toBe("POST");
    expect(req.request.body).toEqual(schedulePayload);
    req.flush(null, { status: 204, statusText: "No Content" });
  });

  it("reschedules a scheduled spray", () => {
    const reschedulePayload: RescheduleSprayRequest = {
      scheduledDateTime: "2026-10-18T07:30:00Z",
    };

    service.rescheduleSpray("sp-1", reschedulePayload).subscribe();

    const req = httpTesting.expectOne(`${environment.apiUrl}/sprays/sp-1/reschedule`);
    expect(req.request.method).toBe("POST");
    expect(req.request.body).toEqual(reschedulePayload);
    req.flush(null, { status: 204, statusText: "No Content" });
  });

  it("fetches target lookup list", () => {
    const mockTargets: TargetResponse[] = [
      {
        id: "t-1",
        code: "APHID",
        name: "Aphids",
        targetType: "Pest",
        description: null,
        displayOrder: 1,
        isSystem: true,
        isActive: true,
      },
    ];

    service.getTargets("Pest").subscribe((targets) => {
      expect(targets).toEqual(mockTargets);
    });

    const req = httpTesting.expectOne((request) => {
      return (
        request.url === `${environment.apiUrl}/sprays/targets` &&
        request.params.get("targetType") === "Pest"
      );
    });
    expect(req.request.method).toBe("GET");
    req.flush(mockTargets);
  });

  it("fetches product types", () => {
    const mockProductTypes: ProductTypeResponse[] = [
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

    service.getProductTypes().subscribe((res) => {
      expect(res).toEqual(mockProductTypes);
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/sprays/product-types`);
    expect(req.request.method).toBe("GET");
    req.flush(mockProductTypes);
  });
});
