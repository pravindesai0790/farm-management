import { Platform } from 'react-native';
import { apiClient } from '../api/client';
import { getDatabase, setWebPreviewData } from '../db/database';
import { MasterTableName } from '../db/schema';

/**
 * TypeScript contract mirroring the backend MobileSyncPayloadResponse.
 */
export interface MobileSyncPayloadResponse {
  syncedAtUtc: string;
  farmId: string | null;
  farms: any[];
  farmAreas: any[];
  cropPlantations: any[];
  cropCycles: any[];
  cropCycleStages: any[];
  workers: any[];
  workerFarmAssignments: any[];
  laborCategories: any[];
  contractors: any[];
  laborWageRates: any[];
  units: any[];
  storageLocations: any[];
  inventoryItems: any[];
  plantProtectionProducts: any[];
  irrigationMethods: any[];
}

/**
 * Result structure returned after an atomic pull operation.
 */
export interface SyncPullResult {
  success: boolean;
  syncedAtUtc: string;
  totalRecordsIngested: number;
  recordsPerTable: Record<MasterTableName, number>;
  errorMessage?: string;
}

/**
 * Executes a full bootstrap pull against /api/mobile/sync/bootstrap.
 * Ingests all active master records into local SQLite within an atomic transaction.
 */
export async function executeBootstrapPull(farmId?: string | null): Promise<SyncPullResult> {
  try {
    const url = farmId ? `/api/mobile/sync/bootstrap?farmId=${encodeURIComponent(farmId)}` : '/api/mobile/sync/bootstrap';
    const response = await apiClient.get<MobileSyncPayloadResponse>(url);
    const payload = response.data;

    return await ingestSyncPayload(payload);
  } catch (err: any) {
    console.error('[deltaPuller] executeBootstrapPull failed:', err);
    const errorDetail =
      err.response?.data?.detail ||
      err.response?.data?.message ||
      err.response?.data?.title ||
      err.message ||
      'Bootstrap pull failed';
    return {
      success: false,
      syncedAtUtc: new Date().toISOString(),
      totalRecordsIngested: 0,
      recordsPerTable: {} as any,
      errorMessage: errorDetail,
    };
  }
}

/**
 * Executes an incremental delta pull against /api/mobile/sync/delta.
 * Ingests only modified records since sinceUtc into local SQLite.
 */
export async function executeDeltaPull(sinceUtc: string, farmId?: string | null): Promise<SyncPullResult> {
  try {
    let url = `/api/mobile/sync/delta?sinceUtc=${encodeURIComponent(sinceUtc)}`;
    if (farmId) {
      url += `&farmId=${encodeURIComponent(farmId)}`;
    }

    const response = await apiClient.get<MobileSyncPayloadResponse>(url);
    const payload = response.data;

    return await ingestSyncPayload(payload);
  } catch (err: any) {
    console.error('[deltaPuller] executeDeltaPull failed:', err);
    const errorDetail =
      err.response?.data?.detail ||
      err.response?.data?.message ||
      err.response?.data?.title ||
      err.message ||
      'Delta pull failed';
    return {
      success: false,
      syncedAtUtc: sinceUtc,
      totalRecordsIngested: 0,
      recordsPerTable: {} as any,
      errorMessage: errorDetail,
    };
  }
}

/**
 * Ingests the received MobileSyncPayloadResponse into local SQLite using prepared batch statements
 * wrapped in an atomic transaction.
 */
async function ingestSyncPayload(payload: MobileSyncPayloadResponse): Promise<SyncPullResult> {
  const recordsPerTable: Partial<Record<MasterTableName, number>> = {};
  let totalRecords = 0;

  if (Platform.OS === 'web') {
    // In Web Preview sandbox, update memory stores
    setWebPreviewData('cached_farms', payload.farms || []);
    setWebPreviewData('cached_farm_areas', payload.farmAreas || []);
    setWebPreviewData('cached_crop_plantations', payload.cropPlantations || []);
    setWebPreviewData('cached_crop_cycles', payload.cropCycles || []);
    setWebPreviewData('cached_crop_cycle_stages', payload.cropCycleStages || []);
    setWebPreviewData('cached_workers', payload.workers || []);
    setWebPreviewData('cached_worker_farm_assignments', payload.workerFarmAssignments || []);
    setWebPreviewData('cached_labor_categories', payload.laborCategories || []);
    setWebPreviewData('cached_contractors', payload.contractors || []);
    setWebPreviewData('cached_labor_wage_rates', payload.laborWageRates || []);
    setWebPreviewData('cached_units', payload.units || []);
    setWebPreviewData('cached_storage_locations', payload.storageLocations || []);
    setWebPreviewData('cached_inventory_items', payload.inventoryItems || []);
    setWebPreviewData('cached_plant_protection_products', payload.plantProtectionProducts || []);
    setWebPreviewData('cached_irrigation_methods', payload.irrigationMethods || []);

    totalRecords =
      (payload.farms?.length || 0) +
      (payload.farmAreas?.length || 0) +
      (payload.cropPlantations?.length || 0) +
      (payload.cropCycles?.length || 0) +
      (payload.cropCycleStages?.length || 0) +
      (payload.workers?.length || 0) +
      (payload.workerFarmAssignments?.length || 0) +
      (payload.laborCategories?.length || 0) +
      (payload.contractors?.length || 0) +
      (payload.laborWageRates?.length || 0) +
      (payload.units?.length || 0) +
      (payload.storageLocations?.length || 0) +
      (payload.inventoryItems?.length || 0) +
      (payload.plantProtectionProducts?.length || 0) +
      (payload.irrigationMethods?.length || 0);

    return {
      success: true,
      syncedAtUtc: payload.syncedAtUtc,
      totalRecordsIngested: totalRecords,
      recordsPerTable: {
        cached_farms: payload.farms?.length || 0,
        cached_farm_areas: payload.farmAreas?.length || 0,
        cached_crop_plantations: payload.cropPlantations?.length || 0,
        cached_crop_cycles: payload.cropCycles?.length || 0,
        cached_crop_cycle_stages: payload.cropCycleStages?.length || 0,
        cached_workers: payload.workers?.length || 0,
        cached_worker_farm_assignments: payload.workerFarmAssignments?.length || 0,
        cached_labor_categories: payload.laborCategories?.length || 0,
        cached_contractors: payload.contractors?.length || 0,
        cached_labor_wage_rates: payload.laborWageRates?.length || 0,
        cached_units: payload.units?.length || 0,
        cached_storage_locations: payload.storageLocations?.length || 0,
        cached_inventory_items: payload.inventoryItems?.length || 0,
        cached_plant_protection_products: payload.plantProtectionProducts?.length || 0,
        cached_irrigation_methods: payload.irrigationMethods?.length || 0,
      },
    };
  }

  const db = getDatabase();
  if (!db) {
    throw new Error('Local SQLite database instance is not available.');
  }

  // Execute all table upserts within a single atomic SQLite transaction
  db.withTransactionSync(() => {
    // 1. Farms
    if (payload.farms?.length) {
      const stmt = db.prepareSync(`
        INSERT OR REPLACE INTO cached_farms (
          id, organization_id, name, description, ownership_type_id, total_area,
          area_unit_id, address_line1, address_line2, city, district, state,
          country, postal_code, latitude, longitude, is_active
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
      `);
      for (const f of payload.farms) {
        stmt.executeSync([
          f.id, f.organizationId, f.name, f.description ?? null, f.ownershipTypeId,
          f.totalArea ?? null, f.areaUnitId ?? null, f.addressLine1 ?? null, f.addressLine2 ?? null,
          f.city ?? null, f.district ?? null, f.state ?? null, f.country ?? null,
          f.postalCode ?? null, f.latitude ?? null, f.longitude ?? null, f.isActive ? 1 : 0
        ]);
      }
      stmt.finalizeSync();
    }
    recordsPerTable.cached_farms = payload.farms?.length || 0;

    // 2. Farm Areas
    if (payload.farmAreas?.length) {
      const stmt = db.prepareSync(`
        INSERT OR REPLACE INTO cached_farm_areas (
          id, organization_id, farm_id, parent_farm_area_id, name, description,
          total_area, area_unit_id, is_active
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?);
      `);
      for (const fa of payload.farmAreas) {
        stmt.executeSync([
          fa.id, fa.organizationId, fa.farmId, fa.parentFarmAreaId ?? null,
          fa.name, fa.description ?? null, fa.totalArea, fa.areaUnitId, fa.isActive ? 1 : 0
        ]);
      }
      stmt.finalizeSync();
    }
    recordsPerTable.cached_farm_areas = payload.farmAreas?.length || 0;

    // 3. Crop Plantations
    if (payload.cropPlantations?.length) {
      const stmt = db.prepareSync(`
        INSERT OR REPLACE INTO cached_crop_plantations (
          id, organization_id, farm_id, farm_area_id, crop_id, variety_id,
          lifecycle_template_id, plantation_name, allocated_area, area_unit_id,
          planting_date, expected_end_date, actual_end_date, status, end_reason_id,
          end_notes, is_active
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
      `);
      for (const p of payload.cropPlantations) {
        stmt.executeSync([
          p.id, p.organizationId, p.farmId, p.farmAreaId, p.cropId, p.varietyId ?? null,
          p.lifecycleTemplateId ?? null, p.plantationName, p.allocatedArea, p.areaUnitId,
          p.plantingDate, p.expectedEndDate ?? null, p.actualEndDate ?? null, p.status,
          p.endReasonId ?? null, p.endNotes ?? null, p.isActive ? 1 : 0
        ]);
      }
      stmt.finalizeSync();
    }
    recordsPerTable.cached_crop_plantations = payload.cropPlantations?.length || 0;

    // 4. Crop Cycles
    if (payload.cropCycles?.length) {
      const stmt = db.prepareSync(`
        INSERT OR REPLACE INTO cached_crop_cycles (
          id, organization_id, plantation_id, lifecycle_template_id, cycle_name,
          season_year, season_name, planned_start_date, actual_start_date,
          expected_end_date, actual_end_date, status, cancellation_reason_id,
          cancellation_notes
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
      `);
      for (const c of payload.cropCycles) {
        stmt.executeSync([
          c.id, c.organizationId, c.plantationId, c.lifecycleTemplateId ?? null,
          c.cycleName, c.seasonYear, c.seasonName ?? null, c.plannedStartDate,
          c.actualStartDate ?? null, c.expectedEndDate ?? null, c.actualEndDate ?? null,
          c.status, c.cancellationReasonId ?? null, c.cancellationNotes ?? null
        ]);
      }
      stmt.finalizeSync();
    }
    recordsPerTable.cached_crop_cycles = payload.cropCycles?.length || 0;

    // 5. Crop Cycle Stages
    if (payload.cropCycleStages?.length) {
      const stmt = db.prepareSync(`
        INSERT OR REPLACE INTO cached_crop_cycle_stages (
          id, crop_cycle_id, lifecycle_template_stage_id, stage_name, sequence_number,
          expected_duration_days, planned_start_date, planned_end_date, actual_start_date,
          actual_end_date, status, notes
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
      `);
      for (const s of payload.cropCycleStages) {
        stmt.executeSync([
          s.id, s.cropCycleId, s.lifecycleTemplateStageId, s.stageName, s.sequenceNumber,
          s.expectedDurationDays ?? null, s.plannedStartDate ?? null, s.plannedEndDate ?? null,
          s.actualStartDate ?? null, s.actualEndDate ?? null, s.status, s.notes ?? null
        ]);
      }
      stmt.finalizeSync();
    }
    recordsPerTable.cached_crop_cycle_stages = payload.cropCycleStages?.length || 0;

    // 6. Workers
    if (payload.workers?.length) {
      const stmt = db.prepareSync(`
        INSERT OR REPLACE INTO cached_workers (
          id, organization_id, first_name, last_name, display_name, gender,
          mobile_number, alternate_mobile_number, labor_category_id, employment_type,
          contractor_id, joining_date, leaving_date, notes, is_active, is_locally_created
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
      `);
      for (const w of payload.workers) {
        stmt.executeSync([
          w.id, w.organizationId, w.firstName, w.lastName ?? null, w.displayName,
          w.gender, w.mobileNumber ?? null, w.alternateMobileNumber ?? null,
          w.laborCategoryId ?? null, w.employmentType, w.contractorId ?? null,
          w.joiningDate ?? null, w.leavingDate ?? null, w.notes ?? null,
          w.isActive ? 1 : 0, w.isLocallyCreated ?? 0
        ]);
      }
      stmt.finalizeSync();
    }
    recordsPerTable.cached_workers = payload.workers?.length || 0;

    // 7. Worker Farm Assignments
    if (payload.workerFarmAssignments?.length) {
      const stmt = db.prepareSync(`
        INSERT OR REPLACE INTO cached_worker_farm_assignments (
          id, organization_id, worker_id, farm_id, assigned_from, assigned_to,
          is_active, notes
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?);
      `);
      for (const a of payload.workerFarmAssignments) {
        stmt.executeSync([
          a.id, a.organizationId, a.workerId, a.farmId, a.assignedFrom,
          a.assignedTo ?? null, a.isActive ? 1 : 0, a.notes ?? null
        ]);
      }
      stmt.finalizeSync();
    }
    recordsPerTable.cached_worker_farm_assignments = payload.workerFarmAssignments?.length || 0;

    // 8. Labor Categories
    if (payload.laborCategories?.length) {
      const stmt = db.prepareSync(`
        INSERT OR REPLACE INTO cached_labor_categories (
          id, organization_id, name, description, is_system, is_active
        ) VALUES (?, ?, ?, ?, ?, ?);
      `);
      for (const lc of payload.laborCategories) {
        stmt.executeSync([
          lc.id, lc.organizationId ?? null, lc.name, lc.description ?? null,
          lc.isSystem ? 1 : 0, lc.isActive ? 1 : 0
        ]);
      }
      stmt.finalizeSync();
    }
    recordsPerTable.cached_labor_categories = payload.laborCategories?.length || 0;

    // 9. Contractors
    if (payload.contractors?.length) {
      const stmt = db.prepareSync(`
        INSERT OR REPLACE INTO cached_contractors (
          id, organization_id, name, contact_person, phone_number, email, address,
          notes, is_active
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?);
      `);
      for (const c of payload.contractors) {
        stmt.executeSync([
          c.id, c.organizationId, c.name, c.contactPerson ?? null, c.phoneNumber ?? null,
          c.email ?? null, c.address ?? null, c.notes ?? null, c.isActive ? 1 : 0
        ]);
      }
      stmt.finalizeSync();
    }
    recordsPerTable.cached_contractors = payload.contractors?.length || 0;

    // 10. Labor Wage Rates
    if (payload.laborWageRates?.length) {
      const stmt = db.prepareSync(`
        INSERT OR REPLACE INTO cached_labor_wage_rates (
          id, organization_id, gender, wage_type, wage_rate, currency_id,
          effective_from, effective_to, notes, is_active
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
      `);
      for (const r of payload.laborWageRates) {
        stmt.executeSync([
          r.id, r.organizationId, r.gender, r.wageType, r.wageRate, r.currencyId,
          r.effectiveFrom, r.effectiveTo ?? null, r.notes ?? null, r.isActive ? 1 : 0
        ]);
      }
      stmt.finalizeSync();
    }
    recordsPerTable.cached_labor_wage_rates = payload.laborWageRates?.length || 0;

    // 11. Units
    if (payload.units?.length) {
      const stmt = db.prepareSync(`
        INSERT OR REPLACE INTO cached_units (
          id, organization_id, code, name, symbol, unit_category, base_unit_code,
          conversion_factor, is_system, is_active, display_order
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
      `);
      for (const u of payload.units) {
        stmt.executeSync([
          u.id, u.organizationId ?? null, u.code, u.name, u.symbol, u.unitCategory,
          u.baseUnitCode ?? null, u.conversionFactor ?? null, u.isSystem ? 1 : 0,
          u.isActive ? 1 : 0, u.displayOrder
        ]);
      }
      stmt.finalizeSync();
    }
    recordsPerTable.cached_units = payload.units?.length || 0;

    // 12. Storage Locations
    if (payload.storageLocations?.length) {
      const stmt = db.prepareSync(`
        INSERT OR REPLACE INTO cached_storage_locations (
          id, organization_id, farm_id, name, description, is_active
        ) VALUES (?, ?, ?, ?, ?, ?);
      `);
      for (const sl of payload.storageLocations) {
        stmt.executeSync([
          sl.id, sl.organizationId, sl.farmId, sl.name, sl.description ?? null,
          sl.isActive ? 1 : 0
        ]);
      }
      stmt.finalizeSync();
    }
    recordsPerTable.cached_storage_locations = payload.storageLocations?.length || 0;

    // 13. Inventory Items
    if (payload.inventoryItems?.length) {
      const stmt = db.prepareSync(`
        INSERT OR REPLACE INTO cached_inventory_items (
          id, organization_id, name, sku, description, category_id, stock_unit_id,
          is_active
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?);
      `);
      for (const ii of payload.inventoryItems) {
        stmt.executeSync([
          ii.id, ii.organizationId, ii.name, ii.sku ?? null, ii.description ?? null,
          ii.categoryId ?? null, ii.stockUnitId, ii.isActive ? 1 : 0
        ]);
      }
      stmt.finalizeSync();
    }
    recordsPerTable.cached_inventory_items = payload.inventoryItems?.length || 0;

    // 14. Plant Protection Products
    if (payload.plantProtectionProducts?.length) {
      const stmt = db.prepareSync(`
        INSERT OR REPLACE INTO cached_plant_protection_products (
          id, organization_id, inventory_item_id, product_type_id, active_ingredient,
          manufacturer, description, is_active
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?);
      `);
      for (const ppp of payload.plantProtectionProducts) {
        stmt.executeSync([
          ppp.id, ppp.organizationId, ppp.inventoryItemId, ppp.productTypeId,
          ppp.activeIngredient ?? null, ppp.manufacturer ?? null,
          ppp.description ?? null, ppp.isActive ? 1 : 0
        ]);
      }
      stmt.finalizeSync();
    }
    recordsPerTable.cached_plant_protection_products = payload.plantProtectionProducts?.length || 0;

    // 15. Irrigation Methods
    if (payload.irrigationMethods?.length) {
      const stmt = db.prepareSync(`
        INSERT OR REPLACE INTO cached_irrigation_methods (
          id, organization_id, code, name, description, is_system, is_active, display_order
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?);
      `);
      for (const im of payload.irrigationMethods) {
        stmt.executeSync([
          im.id, im.organizationId ?? null, im.code, im.name, im.description ?? null,
          im.isSystem ? 1 : 0, im.isActive ? 1 : 0, im.displayOrder
        ]);
      }
      stmt.finalizeSync();
    }
    recordsPerTable.cached_irrigation_methods = payload.irrigationMethods?.length || 0;

    // Update sync checkpoint in system metadata
    db.runSync(
      `INSERT INTO local_system_info (key, value, updated_at) 
       VALUES ('last_synced_at', ?, ?) 
       ON CONFLICT(key) DO UPDATE SET value = ?, updated_at = ?;`,
      [payload.syncedAtUtc, payload.syncedAtUtc, payload.syncedAtUtc, payload.syncedAtUtc]
    );
  });

  totalRecords = Object.values(recordsPerTable).reduce((acc, curr) => (acc || 0) + (curr || 0), 0) || 0;

  return {
    success: true,
    syncedAtUtc: payload.syncedAtUtc,
    totalRecordsIngested: totalRecords,
    recordsPerTable: recordsPerTable as Record<MasterTableName, number>,
  };
}
