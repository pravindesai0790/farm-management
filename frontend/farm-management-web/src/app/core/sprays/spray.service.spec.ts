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
      page: 1,
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
        request.url === `${environment.apiUrl}/master-data/targets` &&
        request.params.get("type") === "Pest"
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

    const req = httpTesting.expectOne(`${environment.apiUrl}/master-data/product-types`);
    expect(req.request.method).toBe("GET");
    req.flush(mockProductTypes);
  });

  it("fetches single spray details by ID", () => {
    const mockDetails: any = {
      id: "sp-1",
      farmId: "farm-1",
      farmName: "Sunrise Vineyard",
      status: "Draft",
      products: [],
    };

    service.getSpray("sp-1").subscribe((res) => {
      expect(res.id).toBe("sp-1");
      expect(res.status).toBe("Draft");
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/sprays/sp-1`);
    expect(req.request.method).toBe("GET");
    req.flush(mockDetails);
  });

  it("creates spray draft via POST /api/sprays", () => {
    const createPayload = {
      farmId: "farm-1",
      plannedDate: "2026-10-20",
      products: [
        { inventoryItemId: "item-1", plannedQuantity: 2.5, dosage: "1 L/ha" },
      ],
    };

    const mockCreated: any = {
      id: "sp-new",
      farmId: "farm-1",
      status: "Draft",
      products: [],
    };

    service.createDraft(createPayload).subscribe((res) => {
      expect(res.id).toBe("sp-new");
      expect(res.status).toBe("Draft");
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/sprays`);
    expect(req.request.method).toBe("POST");
    expect(req.request.body).toEqual(createPayload);
    req.flush(mockCreated);
  });

  it("updates spray draft via PUT /api/sprays/:id", () => {
    const updatePayload = {
      farmId: "farm-1",
      plannedDate: "2026-10-25",
      products: [
        { inventoryItemId: "item-2", plannedQuantity: 3.0, dosage: "2 L/ha" },
      ],
    };

    const mockUpdated: any = {
      id: "sp-1",
      farmId: "farm-1",
      status: "Draft",
      products: [],
    };

    service.updateDraft("sp-1", updatePayload).subscribe((res) => {
      expect(res.id).toBe("sp-1");
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/sprays/sp-1`);
    expect(req.request.method).toBe("PUT");
    expect(req.request.body).toEqual(updatePayload);
    req.flush(mockUpdated);
  });

  it("starts spray via POST /api/sprays/:id/start", () => {
    const startPayload = {
      actualApplicationDateTime: "2026-10-16T08:00:00Z",
      products: [
        {
          inventoryItemId: "item-1",
          storageLocationId: "loc-1",
          actualQuantity: 2.0,
          dosage: "1 L/ha",
        },
      ],
    };

    const mockResponse: any = {
      id: "sp-1",
      status: "InProgress",
    };

    service.startSpray("sp-1", startPayload).subscribe((res) => {
      expect(res.id).toBe("sp-1");
      expect(res.status).toBe("InProgress");
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/sprays/sp-1/start`);
    expect(req.request.method).toBe("POST");
    expect(req.request.body).toEqual(startPayload);
    req.flush(mockResponse);
  });

  it("saves spray execution progress via PUT /api/sprays/:id/execution", () => {
    const executionPayload = {
      actualApplicationDateTime: "2026-10-16T08:30:00Z",
      actualTreatedArea: 4.5,
      actualTreatedAreaUnitId: "u-ha",
      waterQuantity: 450,
      waterUnitId: "u-l",
      purposeReason: "Ongoing powdery mildew coverage",
      products: [
        { inventoryItemId: "item-1", actualQuantity: 2.2, dosage: "1.1 L/ha" },
      ],
    };

    const mockResponse: any = {
      id: "sp-1",
      status: "InProgress",
    };

    service.saveExecution("sp-1", executionPayload).subscribe((res) => {
      expect(res.id).toBe("sp-1");
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/sprays/sp-1/execution`);
    expect(req.request.method).toBe("PUT");
    expect(req.request.body).toEqual(executionPayload);
    req.flush(mockResponse);
  });

  it("completes spray via POST /api/sprays/:id/complete", () => {
    const completePayload = {
      actualApplicationDateTime: "2026-10-16T09:00:00Z",
      actualTreatedArea: 5.0,
      products: [
        { inventoryItemId: "item-1", actualQuantity: 2.5 },
      ],
    };

    const mockResponse: any = {
      id: "sp-1",
      status: "Completed",
    };

    service.completeSpray("sp-1", completePayload).subscribe((res) => {
      expect(res.id).toBe("sp-1");
      expect(res.status).toBe("Completed");
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/sprays/sp-1/complete`);
    expect(req.request.method).toBe("POST");
    expect(req.request.body).toEqual(completePayload);
    req.flush(mockResponse);
  });
});

