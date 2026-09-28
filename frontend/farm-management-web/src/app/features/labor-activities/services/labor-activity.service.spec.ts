import { provideHttpClient } from "@angular/common/http";
import {
  HttpTestingController,
  provideHttpClientTesting,
} from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";

import { environment } from "../../../../environments/environment";
import {
  CreateLaborActivityRequest,
  LaborActivity,
  LaborActivityFilter,
  LaborActivityList,
  NamedReference,
  UpdateLaborActivityRequest,
} from "../models/labor-activity.models";
import { LaborActivityService } from "./labor-activity.service";

describe("LaborActivityService", () => {
  let service: LaborActivityService;
  let httpTesting: HttpTestingController;

  const sampleFarm: NamedReference = { id: "farm-1", name: "Green Acres" };
  const sampleArea: NamedReference = { id: "area-1", name: "North Block" };
  const sampleType: NamedReference = { id: "type-1", name: "Pruning" };

  const sampleActivity: LaborActivity = {
    id: "act-1",
    activityDate: "2026-09-28",
    farm: sampleFarm,
    farmArea: sampleArea,
    plantation: null,
    cropCycle: null,
    cropCycleStage: { id: "stage-1", name: "Budbreak", sequenceNumber: 1 },
    activityType: sampleType,
    status: "COMPLETED",
    description: "Manual canopy trimming",
    createdAt: "2026-09-28T10:00:00Z",
    updatedAt: null,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        LaborActivityService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(LaborActivityService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it("lists labor activities with all filter params including cropCycleStageId", () => {
    const filter: LaborActivityFilter = {
      farmId: "farm-1",
      farmAreaId: "area-1",
      plantationId: "plant-1",
      cropCycleId: "cycle-1",
      cropCycleStageId: "stage-1",
      activityTypeId: "type-1",
      fromDate: "2026-09-01",
      toDate: "2026-09-30",
      status: "COMPLETED",
      page: 1,
      pageSize: 20,
    };

    const mockResponse: LaborActivityList = {
      items: [sampleActivity],
      totalCount: 1,
      page: 1,
      pageSize: 20,
      totalPages: 1,
    };

    service.list(filter).subscribe((res) => {
      expect(res).toEqual(mockResponse);
      expect(res.items[0].cropCycleStage).toBeDefined();
      expect(res.items[0].cropCycleStage?.name).toBe("Budbreak");
    });

    const req = httpTesting.expectOne((request) => {
      const p = request.params;
      return (
        request.url === `${environment.apiUrl}/labor-activities` &&
        p.get("page") === "1" &&
        p.get("pageSize") === "20" &&
        p.get("farmId") === "farm-1" &&
        p.get("farmAreaId") === "area-1" &&
        p.get("plantationId") === "plant-1" &&
        p.get("cropCycleId") === "cycle-1" &&
        p.get("cropCycleStageId") === "stage-1" &&
        p.get("activityTypeId") === "type-1" &&
        p.get("fromDate") === "2026-09-01" &&
        p.get("toDate") === "2026-09-30" &&
        p.get("status") === "COMPLETED"
      );
    });

    expect(req.request.method).toBe("GET");
    req.flush(mockResponse);
  });

  it("fetches single activity by ID", () => {
    service.get("act-1").subscribe((act) => {
      expect(act).toEqual(sampleActivity);
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/labor-activities/act-1`);
    expect(req.request.method).toBe("GET");
    req.flush(sampleActivity);
  });

  it("creates a labor activity without obsolete fields and with optional stage ID", () => {
    const requestPayload: CreateLaborActivityRequest = {
      activityDate: "2026-09-28",
      farmId: "farm-1",
      farmAreaId: "area-1",
      plantationId: null,
      cropCycleId: null,
      cropCycleStageId: "stage-1",
      laborActivityTypeId: "type-1",
      description: "Sample work",
      status: "COMPLETED",
    };

    service.create(requestPayload).subscribe((created) => {
      expect(created).toEqual(sampleActivity);
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/labor-activities`);
    expect(req.request.method).toBe("POST");
    expect(req.request.body).toEqual(requestPayload);
    expect((req.request.body as Record<string, unknown>)["workerCount"]).toBeUndefined();
    expect((req.request.body as Record<string, unknown>)["totalWorkingHours"]).toBeUndefined();
    expect((req.request.body as Record<string, unknown>)["costAmount"]).toBeUndefined();
    req.flush(sampleActivity);
  });

  it("updates a labor activity without obsolete fields", () => {
    const updatePayload: UpdateLaborActivityRequest = {
      activityDate: "2026-09-28",
      farmId: "farm-1",
      cropCycleStageId: "stage-1",
      laborActivityTypeId: "type-1",
      status: "COMPLETED",
    };

    service.update("act-1", updatePayload).subscribe((updated) => {
      expect(updated).toEqual(sampleActivity);
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/labor-activities/act-1`);
    expect(req.request.method).toBe("PUT");
    expect(req.request.body).toEqual(updatePayload);
    req.flush(sampleActivity);
  });

  it("cancels a labor activity with reason payload", () => {
    service.cancel("act-1", "Weather delay").subscribe();

    const req = httpTesting.expectOne(`${environment.apiUrl}/labor-activities/act-1/cancel`);
    expect(req.request.method).toBe("POST");
    expect(req.request.body).toEqual({ reason: "Weather delay" });
    req.flush(null, { status: 204, statusText: "No Content" });
  });

  it("fetches labor activity master types", () => {
    const types: readonly NamedReference[] = [sampleType];

    service.getTypes().subscribe((res) => {
      expect(res).toEqual(types);
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/labor-activities/types`);
    expect(req.request.method).toBe("GET");
    req.flush(types);
  });
});
