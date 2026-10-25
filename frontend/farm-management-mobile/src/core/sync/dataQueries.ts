import { Platform } from 'react-native';
import { getDatabase, getTableSampleRows } from '../db/database';

/**
 * Type-safe query accessors for locally cached SQLite records.
 * Ensures offline features (e.g. Attendance, Spraying, Diagnostics) can read data
 * with zero network latency.
 */

export interface CachedFarm {
  id: string;
  organization_id: string;
  name: string;
  description?: string | null;
  ownership_type_id: string;
  total_area?: number | null;
  area_unit_id?: string | null;
  city?: string | null;
  state?: string | null;
  country?: string | null;
  is_active: number;
}

export interface CachedWorker {
  id: string;
  organization_id: string;
  first_name: string;
  last_name?: string | null;
  display_name: string;
  gender: number;
  mobile_number?: string | null;
  alternate_mobile_number?: string | null;
  labor_category_id?: string | null;
  employment_type: number;
  contractor_id?: string | null;
  is_active: number;
  is_locally_created: number;
}

export interface CachedFarmArea {
  id: string;
  organization_id: string;
  farm_id: string;
  parent_farm_area_id?: string | null;
  name: string;
  description?: string | null;
  total_area: number;
  area_unit_id: string;
  is_active: number;
}

export interface CachedPlantation {
  id: string;
  organization_id: string;
  farm_id: string;
  farm_area_id: string;
  crop_id: string;
  plantation_name: string;
  allocated_area: number;
  area_unit_id: string;
  planting_date: string;
  status: number;
  is_active: number;
}

export interface CachedCropCycle {
  id: string;
  organization_id: string;
  plantation_id: string;
  cycle_name: string;
  season_year: number;
  season_name?: string | null;
  planned_start_date: string;
  status: number;
}

export interface CachedWageRate {
  id: string;
  organization_id: string;
  gender: number;
  wage_type: number;
  wage_rate: number;
  currency_id: string;
  effective_from: string;
  effective_to?: string | null;
  is_active: number;
}

export interface CachedLaborCategory {
  id: string;
  organization_id?: string | null;
  name: string;
  description?: string | null;
  is_system: number;
  is_active: number;
}

/**
 * Retrieves all active farms cached in local SQLite.
 */
export function getCachedFarms(): CachedFarm[] {
  if (Platform.OS === 'web') {
    return getTableSampleRows('cached_farms', 100);
  }
  const db = getDatabase();
  if (!db) return [];
  try {
    return db.getAllSync<CachedFarm>('SELECT * FROM cached_farms WHERE is_active = 1 ORDER BY name ASC;');
  } catch (err) {
    console.warn('[dataQueries] getCachedFarms error:', err);
    return [];
  }
}

/**
 * Retrieves all workers assigned to a specific farm or all tenant workers.
 */
export function getCachedWorkers(farmId?: string | null): CachedWorker[] {
  if (Platform.OS === 'web') {
    return getTableSampleRows('cached_workers', 100);
  }
  const db = getDatabase();
  if (!db) return [];
  try {
    if (farmId) {
      return db.getAllSync<CachedWorker>(
        `SELECT DISTINCT w.* FROM cached_workers w
         LEFT JOIN cached_worker_farm_assignments a ON a.worker_id = w.id
         WHERE (a.farm_id = ? OR a.farm_id IS NULL) AND w.is_active = 1
         ORDER BY w.display_name ASC;`,
        [farmId]
      );
    }
    return db.getAllSync<CachedWorker>('SELECT * FROM cached_workers WHERE is_active = 1 ORDER BY display_name ASC;');
  } catch (err) {
    console.warn('[dataQueries] getCachedWorkers error:', err);
    return [];
  }
}

/**
 * Retrieves all farm parcelation areas for a specific farm.
 */
export function getCachedFarmAreas(farmId: string): CachedFarmArea[] {
  if (Platform.OS === 'web') {
    return getTableSampleRows('cached_farm_areas', 100);
  }
  const db = getDatabase();
  if (!db) return [];
  try {
    return db.getAllSync<CachedFarmArea>(
      'SELECT * FROM cached_farm_areas WHERE farm_id = ? AND is_active = 1 ORDER BY name ASC;',
      [farmId]
    );
  } catch (err) {
    console.warn('[dataQueries] getCachedFarmAreas error:', err);
    return [];
  }
}

/**
 * Retrieves all active crop plantations for a specific farm.
 */
export function getCachedPlantations(farmId: string): CachedPlantation[] {
  if (Platform.OS === 'web') {
    return getTableSampleRows('cached_crop_plantations', 100);
  }
  const db = getDatabase();
  if (!db) return [];
  try {
    return db.getAllSync<CachedPlantation>(
      'SELECT * FROM cached_crop_plantations WHERE farm_id = ? AND is_active = 1 ORDER BY plantation_name ASC;',
      [farmId]
    );
  } catch (err) {
    console.warn('[dataQueries] getCachedPlantations error:', err);
    return [];
  }
}

/**
 * Retrieves active wage rates defined for the tenant.
 */
export function getCachedWageRates(): CachedWageRate[] {
  if (Platform.OS === 'web') {
    return getTableSampleRows('cached_labor_wage_rates', 100);
  }
  const db = getDatabase();
  if (!db) return [];
  try {
    return db.getAllSync<CachedWageRate>('SELECT * FROM cached_labor_wage_rates WHERE is_active = 1 ORDER BY wage_type ASC;');
  } catch (err) {
    console.warn('[dataQueries] getCachedWageRates error:', err);
    return [];
  }
}

/**
 * Retrieves active labor categories defined for the tenant.
 */
export function getCachedLaborCategories(): CachedLaborCategory[] {
  if (Platform.OS === 'web') {
    return getTableSampleRows('cached_labor_categories', 100);
  }
  const db = getDatabase();
  if (!db) return [];
  try {
    return db.getAllSync<CachedLaborCategory>(
      'SELECT * FROM cached_labor_categories WHERE is_active = 1 ORDER BY name ASC;'
    );
  } catch (err) {
    console.warn('[dataQueries] getCachedLaborCategories error:', err);
    return [];
  }
}
