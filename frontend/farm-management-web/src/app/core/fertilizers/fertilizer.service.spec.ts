import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import {
  CreateFertilizerDraftRequest,
  FertilizerApplicationDetailsResponse,
  FertilizerApplicationListQuery,
  FertilizerProductLookup,
  FertilizerStorageLocationLookup,
} from './fertilizer.models';
import { FertilizerService } from './fertilizer.service';

describe('FertilizerService', () => {
  let service: FertilizerService;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiUrl}/fertilizer-applications`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        FertilizerService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(FertilizerService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('list should send correct query parameters including dateType and paging', () => {
    const query: FertilizerApplicationListQuery = {
      farmId: 'farm-123',
      farmAreaId: 'area-456',
      plantationId: 'plant-789',
      cropCycleId: 'cycle-101',
      cropCycleStageId: 'stage-202',
      inventoryItemId: 'item-303',
      status: 'Scheduled',
      dateType: 'PlannedDate',
      fromDate: '2026-06-01',
      toDate: '2026-06-30',
      search: 'Urea boost',
      pageNumber: 2,
      pageSize: 25,
      sortBy: 'PlannedDate',
      sortDirection: 'asc',
    };

    service.list(query).subscribe((res) => {
      expect(res.items.length).toBe(1);
      expect(res.totalCount).toBe(1);
    });

    const req = httpMock.expectOne((r) => r.url === baseUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('farmId')).toBe('farm-123');
    expect(req.request.params.get('farmAreaId')).toBe('area-456');
    expect(req.request.params.get('plantationId')).toBe('plant-789');
    expect(req.request.params.get('cropCycleId')).toBe('cycle-101');
    expect(req.request.params.get('cropCycleStageId')).toBe('stage-202');
    expect(req.request.params.get('inventoryItemId')).toBe('item-303');
    expect(req.request.params.get('status')).toBe('Scheduled');
    expect(req.request.params.get('dateType')).toBe('PlannedDate');
    expect(req.request.params.get('fromDate')).toBe('2026-06-01');
    expect(req.request.params.get('toDate')).toBe('2026-06-30');
    expect(req.request.params.get('search')).toBe('Urea boost');
    expect(req.request.params.get('pageNumber')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('25');
    expect(req.request.params.get('sortBy')).toBe('PlannedDate');
    expect(req.request.params.get('sortDirection')).toBe('asc');

    req.flush({
      items: [{ id: 'app-1', referenceNumber: 'FA-2026-000001', status: 'Scheduled' }],
      totalCount: 1,
      pageNumber: 2,
      pageSize: 25,
    });
  });

  it('get should fetch single application details by id', () => {
    const appId = 'app-123';
    service.get(appId).subscribe((res) => {
      expect(res.id).toBe(appId);
      expect(res.referenceNumber).toBe('FA-2026-000001');
    });

    const req = httpMock.expectOne(`${baseUrl}/${appId}`);
    expect(req.request.method).toBe('GET');
    req.flush({
      id: appId,
      referenceNumber: 'FA-2026-000001',
      status: 'Draft',
      products: [],
    } as unknown as FertilizerApplicationDetailsResponse);
  });

  it('exportCsv should request CSV blob with format=csv parameter', () => {
    const query: FertilizerApplicationListQuery = {
      status: 'Completed',
      search: 'orchard',
    };

    service.exportCsv(query).subscribe((blob) => {
      expect(blob).toBeTruthy();
      expect(blob.type).toBe('text/csv');
    });

    const req = httpMock.expectOne((r) => r.url === `${baseUrl}/export`);
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    expect(req.request.params.get('format')).toBe('csv');
    expect(req.request.params.get('status')).toBe('Completed');
    expect(req.request.params.get('search')).toBe('orchard');

    const mockBlob = new Blob(['col1,col2\nval1,val2'], { type: 'text/csv' });
    req.flush(mockBlob);
  });

  it('lookups should query proper lookup endpoints', () => {
    service.getProductsLookup().subscribe((products) => {
      expect(products.length).toBe(1);
      expect(products[0].itemName).toBe('Urea');
    });

    const req1 = httpMock.expectOne(`${baseUrl}/lookups/products`);
    expect(req1.request.method).toBe('GET');
    req1.flush([{ inventoryItemId: 'item-1', itemName: 'Urea' } as FertilizerProductLookup]);

    service.getStorageLocationsLookup('farm-1', 'item-1').subscribe((locs) => {
      expect(locs.length).toBe(1);
    });

    const req2 = httpMock.expectOne(`${baseUrl}/lookups/storage-locations?farmId=farm-1&inventoryItemId=item-1`);
    expect(req2.request.method).toBe('GET');
    req2.flush([{ storageLocationId: 'loc-1', storageLocationName: 'Shed' } as FertilizerStorageLocationLookup]);
  });

  it('getCropCycleStages should fetch stages for a cycle', () => {
    service.getCropCycleStages('cycle-1').subscribe((stages) => {
      expect(stages.length).toBe(1);
      expect(stages[0].stageName).toBe('Vegetative');
    });
    const reqStages = httpMock.expectOne(`${environment.apiUrl}/crop-cycles/cycle-1/stages`);
    expect(reqStages.request.method).toBe('GET');
    reqStages.flush([{ id: 'stage-1', stageName: 'Vegetative' }]);
  });

  it('mutations should call respective endpoints', () => {
    const draftReq: CreateFertilizerDraftRequest = {
      farmId: 'farm-1',
      purposeReason: 'Spring feeding',
    };

    service.createDraft(draftReq).subscribe((res) => {
      expect(res.id).toBe('app-new');
    });

    const reqCreate = httpMock.expectOne(baseUrl);
    expect(reqCreate.request.method).toBe('POST');
    expect(reqCreate.request.body).toEqual(draftReq);
    reqCreate.flush({ id: 'app-new', status: 'Draft' });

    service.copyAsNewDraft('app-1').subscribe((res) => {
      expect(res.id).toBe('app-copied');
    });

    const reqCopy = httpMock.expectOne(`${baseUrl}/app-1/copy`);
    expect(reqCopy.request.method).toBe('POST');
    reqCopy.flush({ id: 'app-copied', status: 'Draft' });

    service.deleteDraft('app-1').subscribe();
    const reqDel = httpMock.expectOne(`${baseUrl}/app-1`);
    expect(reqDel.request.method).toBe('DELETE');
    reqDel.flush(null);
  });
});
