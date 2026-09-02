import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResponse } from '../administration/administration.models';
import { Crop, CropCycle, CropList, CropVariety, CycleList, Farm, FarmArea, FarmAreaAvailability, FarmList, FarmOwnershipType, LifecycleList, LifecycleTemplate, Organization, OrganizationListResponse, Plantation, PlantationEndReason, PlantationList, Unit, VarietyList } from './farm-management.models';

@Injectable({ providedIn: 'root' })
export class FarmManagementService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;
  listOrganizations(): Observable<OrganizationListResponse> { return this.http.get<OrganizationListResponse>(`${this.api}/organizations`); }
  getOrganization(): Observable<Organization> { return this.http.get<Organization>(`${this.api}/organization`); }
  createOrganization(request: { name: string; code: string }): Observable<Organization> { return this.http.post<Organization>(`${this.api}/organizations`, request); }
  updateOrganization(request: { name: string; code: string }): Observable<Organization> { return this.http.put<Organization>(`${this.api}/organization`, request); }
  activateOrganization(): Observable<void> { return this.http.patch<void>(`${this.api}/organization/activate`, null); }
  deactivateOrganization(): Observable<void> { return this.http.patch<void>(`${this.api}/organization/deactivate`, null); }

  listUnits(category = 'AREA'): Observable<readonly Unit[]> { return this.http.get<readonly Unit[]>(`${this.api}/master-data/units`, { params: { category } }); }
  listOwnershipTypes(): Observable<readonly FarmOwnershipType[]> { return this.http.get<readonly FarmOwnershipType[]>(`${this.api}/master-data/farm-ownership-types`); }
  listEndReasons(): Observable<readonly PlantationEndReason[]> { return this.http.get<readonly PlantationEndReason[]>(`${this.api}/master-data/plantation-end-reasons`); }

  listFarms(page: number, pageSize: number, search: string, isActive: boolean | null): Observable<FarmList> { return this.http.get<FarmList>(`${this.api}/farms`, { params: this.listParams(page, pageSize, search, isActive) }); }
  getFarm(id: string): Observable<Farm> { return this.http.get<Farm>(`${this.api}/farms/${id}`); }
  createFarm(request: object): Observable<Farm> { return this.http.post<Farm>(`${this.api}/farms`, request); }
  updateFarm(id: string, request: object): Observable<Farm> { return this.http.put<Farm>(`${this.api}/farms/${id}`, request); }
  activateFarm(id: string): Observable<void> { return this.http.patch<void>(`${this.api}/farms/${id}/activate`, null); }
  deactivateFarm(id: string): Observable<void> { return this.http.patch<void>(`${this.api}/farms/${id}/deactivate`, null); }
  listAreas(farmId: string, isActive: boolean | null = null): Observable<readonly FarmArea[]> { return this.http.get<readonly FarmArea[]>(`${this.api}/farms/${farmId}/areas`, { params: isActive === null ? {} : { isActive } }); }
  getArea(id: string): Observable<FarmArea> { return this.http.get<FarmArea>(`${this.api}/farm-areas/${id}`); }
  getAreaAvailability(id: string): Observable<FarmAreaAvailability> { return this.http.get<FarmAreaAvailability>(`${this.api}/farm-areas/${id}/availability`); }
  createArea(request: object): Observable<FarmArea> { return this.http.post<FarmArea>(`${this.api}/farm-areas`, request); }
  updateArea(id: string, request: object): Observable<FarmArea> { return this.http.put<FarmArea>(`${this.api}/farm-areas/${id}`, request); }
  activateArea(id: string): Observable<void> { return this.http.patch<void>(`${this.api}/farm-areas/${id}/activate`, null); }
  deactivateArea(id: string): Observable<void> { return this.http.patch<void>(`${this.api}/farm-areas/${id}/deactivate`, null); }

  listCrops(page: number, pageSize: number, search: string, isActive: boolean | null): Observable<CropList> { return this.http.get<CropList>(`${this.api}/crops`, { params: this.listParams(page, pageSize, search, isActive) }); }
  getCrop(id: string): Observable<Crop> { return this.http.get<Crop>(`${this.api}/crops/${id}`); }
  createCrop(request: object): Observable<Crop> { return this.http.post<Crop>(`${this.api}/crops`, request); }
  updateCrop(id: string, request: object): Observable<Crop> { return this.http.put<Crop>(`${this.api}/crops/${id}`, request); }
  activateCrop(id: string): Observable<void> { return this.http.patch<void>(`${this.api}/crops/${id}/activate`, null); }
  deactivateCrop(id: string): Observable<void> { return this.http.patch<void>(`${this.api}/crops/${id}/deactivate`, null); }
  listVarieties(cropId: string, page = 1, pageSize = 100): Observable<VarietyList> { return this.http.get<VarietyList>(`${this.api}/crops/${cropId}/varieties`, { params: { page, pageSize, isActive: true } }); }
  createVariety(request: object): Observable<CropVariety> { return this.http.post<CropVariety>(`${this.api}/crop-varieties`, request); }
  updateVariety(id: string, request: object): Observable<CropVariety> { return this.http.put<CropVariety>(`${this.api}/crop-varieties/${id}`, request); }
  activateVariety(id: string): Observable<void> { return this.http.patch<void>(`${this.api}/crop-varieties/${id}/activate`, null); }
  deactivateVariety(id: string): Observable<void> { return this.http.patch<void>(`${this.api}/crop-varieties/${id}/deactivate`, null); }

  listPlantations(farmId?: string, farmAreaId?: string, status?: string): Observable<PlantationList> { let params = new HttpParams(); if (farmId) params = params.set('farmId', farmId); if (farmAreaId) params = params.set('farmAreaId', farmAreaId); if (status) params = params.set('status', status); return this.http.get<PlantationList>(`${this.api}/plantations`, { params }); }
  getPlantation(id: string): Observable<Plantation> { return this.http.get<Plantation>(`${this.api}/plantations/${id}`); }
  createPlantation(request: object): Observable<Plantation> { return this.http.post<Plantation>(`${this.api}/plantations`, request); }
  updatePlantation(id: string, request: object): Observable<Plantation> { return this.http.put<Plantation>(`${this.api}/plantations/${id}`, request); }
  activatePlantation(id: string): Observable<void> { return this.http.post<void>(`${this.api}/plantations/${id}/activate`, null); }
  terminatePlantation(id: string, request: object): Observable<void> { return this.http.post<void>(`${this.api}/plantations/${id}/terminate`, request); }
  archivePlantation(id: string): Observable<void> { return this.http.post<void>(`${this.api}/plantations/${id}/archive`, null); }
  listCycles(plantationId?: string, status?: string, seasonYear?: number): Observable<CycleList> { let params = new HttpParams(); if (plantationId) params = params.set('plantationId', plantationId); if (status) params = params.set('status', status); if (seasonYear) params = params.set('seasonYear', seasonYear); return this.http.get<CycleList>(`${this.api}/crop-cycles`, { params }); }
  getCycle(id: string): Observable<CropCycle> { return this.http.get<CropCycle>(`${this.api}/crop-cycles/${id}`); }
  createCycle(request: object): Observable<CropCycle> { return this.http.post<CropCycle>(`${this.api}/crop-cycles`, request); }
  updateCycle(id: string, request: object): Observable<CropCycle> { return this.http.put<CropCycle>(`${this.api}/crop-cycles/${id}`, request); }
  startCycle(id: string, date: string): Observable<void> { return this.http.post<void>(`${this.api}/crop-cycles/${id}/start`, { startDate: date }); }
  harvestCycle(id: string, date: string): Observable<void> { return this.http.post<void>(`${this.api}/crop-cycles/${id}/harvest`, { harvestDate: date }); }
  completeCycle(id: string, date?: string): Observable<void> { return this.http.post<void>(`${this.api}/crop-cycles/${id}/complete`, date ? { completionDate: date } : null); }
  cancelCycle(id: string, request: object): Observable<void> { return this.http.post<void>(`${this.api}/crop-cycles/${id}/cancel`, request); }
  listLifecycleTemplates(page = 1, pageSize = 100): Observable<LifecycleList> { return this.http.get<LifecycleList>(`${this.api}/crop-lifecycle-templates`, { params: { page, pageSize, isActive: true } }); }

  private listParams(page: number, pageSize: number, search: string, isActive: boolean | null): HttpParams { let params = new HttpParams().set('page', page).set('pageSize', pageSize); if (search.trim()) params = params.set('search', search.trim()); if (isActive !== null) params = params.set('isActive', isActive); return params; }
}
