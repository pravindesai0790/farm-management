/**
 * SQLite Local Database Schema & Migration Definitions
 * 
 * Defines the 15 master cache tables and 2 operational/outbox tables.
 * Every table strictly mirrors the exact column names, data types, and nullability
 * of the production backend domain models in backend/src/FarmManagement.Domain/Entities/.
 */

/**
 * SQL statements to create all 15 master cache tables, operational outbox tables,
 * and indexing for rapid local lookups in offline field environments.
 */
export const CREATE_TABLES_SQL = `
-- 1. Farms (Mirrors Farm.cs)
CREATE TABLE IF NOT EXISTS cached_farms (
    id TEXT PRIMARY KEY,
    organization_id TEXT NOT NULL,
    name TEXT NOT NULL,
    description TEXT,
    ownership_type_id TEXT NOT NULL,
    total_area NUMERIC,
    area_unit_id TEXT,
    address_line1 TEXT,
    address_line2 TEXT,
    city TEXT,
    district TEXT,
    state TEXT,
    country TEXT,
    postal_code TEXT,
    latitude NUMERIC,
    longitude NUMERIC,
    is_active INTEGER NOT NULL
);

-- 2. Farm Areas / Land Parcelation (Mirrors FarmArea.cs)
CREATE TABLE IF NOT EXISTS cached_farm_areas (
    id TEXT PRIMARY KEY,
    organization_id TEXT NOT NULL,
    farm_id TEXT NOT NULL,
    parent_farm_area_id TEXT,
    name TEXT NOT NULL,
    description TEXT,
    total_area NUMERIC NOT NULL,
    area_unit_id TEXT NOT NULL,
    is_active INTEGER NOT NULL
);

-- 3. Crop Plantations (Mirrors CropPlantation.cs)
CREATE TABLE IF NOT EXISTS cached_crop_plantations (
    id TEXT PRIMARY KEY,
    organization_id TEXT NOT NULL,
    farm_id TEXT NOT NULL,
    farm_area_id TEXT NOT NULL,
    crop_id TEXT NOT NULL,
    variety_id TEXT,
    lifecycle_template_id TEXT,
    plantation_name TEXT NOT NULL,
    allocated_area NUMERIC NOT NULL,
    area_unit_id TEXT NOT NULL,
    planting_date TEXT NOT NULL,
    expected_end_date TEXT,
    actual_end_date TEXT,
    status INTEGER NOT NULL,
    end_reason_id TEXT,
    end_notes TEXT,
    is_active INTEGER NOT NULL
);

-- 4. Crop Cycles (Mirrors CropCycle.cs)
CREATE TABLE IF NOT EXISTS cached_crop_cycles (
    id TEXT PRIMARY KEY,
    organization_id TEXT NOT NULL,
    plantation_id TEXT NOT NULL,
    lifecycle_template_id TEXT,
    cycle_name TEXT NOT NULL,
    season_year INTEGER NOT NULL,
    season_name TEXT,
    planned_start_date TEXT NOT NULL,
    actual_start_date TEXT,
    expected_end_date TEXT,
    actual_end_date TEXT,
    status INTEGER NOT NULL,
    cancellation_reason_id TEXT,
    cancellation_notes TEXT
);

-- 5. Crop Cycle Stages (Mirrors CropCycleStage.cs)
CREATE TABLE IF NOT EXISTS cached_crop_cycle_stages (
    id TEXT PRIMARY KEY,
    crop_cycle_id TEXT NOT NULL,
    lifecycle_template_stage_id TEXT NOT NULL,
    stage_name TEXT NOT NULL,
    sequence_number INTEGER NOT NULL,
    expected_duration_days INTEGER,
    planned_start_date TEXT,
    planned_end_date TEXT,
    actual_start_date TEXT,
    actual_end_date TEXT,
    status INTEGER NOT NULL,
    notes TEXT
);

-- 6. Workers (Mirrors Worker.cs)
CREATE TABLE IF NOT EXISTS cached_workers (
    id TEXT PRIMARY KEY,
    organization_id TEXT NOT NULL,
    first_name TEXT NOT NULL,
    last_name TEXT,
    display_name TEXT NOT NULL,
    gender INTEGER NOT NULL,
    mobile_number TEXT,
    alternate_mobile_number TEXT,
    labor_category_id TEXT,
    employment_type INTEGER NOT NULL,
    contractor_id TEXT,
    joining_date TEXT,
    leaving_date TEXT,
    notes TEXT,
    is_active INTEGER NOT NULL,
    is_locally_created INTEGER NOT NULL DEFAULT 0
);

-- 7. Worker Farm Assignments (Mirrors WorkerFarmAssignment.cs)
CREATE TABLE IF NOT EXISTS cached_worker_farm_assignments (
    id TEXT PRIMARY KEY,
    organization_id TEXT NOT NULL,
    worker_id TEXT NOT NULL,
    farm_id TEXT NOT NULL,
    assigned_from TEXT NOT NULL,
    assigned_to TEXT,
    is_active INTEGER NOT NULL,
    notes TEXT
);

-- 8. Labor Categories (Mirrors LaborCategory.cs)
CREATE TABLE IF NOT EXISTS cached_labor_categories (
    id TEXT PRIMARY KEY,
    organization_id TEXT,
    name TEXT NOT NULL,
    description TEXT,
    is_system INTEGER NOT NULL,
    is_active INTEGER NOT NULL
);

-- 9. Contractors (Mirrors Contractor.cs)
CREATE TABLE IF NOT EXISTS cached_contractors (
    id TEXT PRIMARY KEY,
    organization_id TEXT NOT NULL,
    name TEXT NOT NULL,
    contact_person TEXT,
    phone_number TEXT,
    email TEXT,
    address TEXT,
    notes TEXT,
    is_active INTEGER NOT NULL
);

-- 10. Labor Wage Rates (Mirrors LaborWageRate.cs)
CREATE TABLE IF NOT EXISTS cached_labor_wage_rates (
    id TEXT PRIMARY KEY,
    organization_id TEXT NOT NULL,
    gender INTEGER NOT NULL,
    wage_type INTEGER NOT NULL,
    wage_rate NUMERIC NOT NULL,
    currency_id TEXT NOT NULL,
    effective_from TEXT NOT NULL,
    effective_to TEXT,
    notes TEXT,
    is_active INTEGER NOT NULL
);

-- 11. Units (Mirrors Unit.cs)
CREATE TABLE IF NOT EXISTS cached_units (
    id TEXT PRIMARY KEY,
    organization_id TEXT,
    code TEXT NOT NULL,
    name TEXT NOT NULL,
    symbol TEXT NOT NULL,
    unit_category INTEGER NOT NULL,
    base_unit_code TEXT,
    conversion_factor NUMERIC,
    is_system INTEGER NOT NULL,
    is_active INTEGER NOT NULL,
    display_order INTEGER NOT NULL
);

-- 12. Storage Locations (Mirrors StorageLocation.cs)
CREATE TABLE IF NOT EXISTS cached_storage_locations (
    id TEXT PRIMARY KEY,
    organization_id TEXT NOT NULL,
    farm_id TEXT NOT NULL,
    name TEXT NOT NULL,
    description TEXT,
    is_active INTEGER NOT NULL
);

-- 13. Inventory Items (Mirrors InventoryItem.cs)
CREATE TABLE IF NOT EXISTS cached_inventory_items (
    id TEXT PRIMARY KEY,
    organization_id TEXT NOT NULL,
    name TEXT NOT NULL,
    sku TEXT,
    description TEXT,
    category_id TEXT,
    stock_unit_id TEXT NOT NULL,
    is_active INTEGER NOT NULL
);

-- 14. Plant Protection Products (Mirrors PlantProtectionProduct.cs)
CREATE TABLE IF NOT EXISTS cached_plant_protection_products (
    id TEXT PRIMARY KEY,
    organization_id TEXT NOT NULL,
    inventory_item_id TEXT NOT NULL,
    product_type_id TEXT NOT NULL,
    active_ingredient TEXT,
    manufacturer TEXT,
    description TEXT,
    is_active INTEGER NOT NULL
);

-- 15. Irrigation Methods (Mirrors IrrigationMethod.cs)
CREATE TABLE IF NOT EXISTS cached_irrigation_methods (
    id TEXT PRIMARY KEY,
    organization_id TEXT,
    code TEXT NOT NULL,
    name TEXT NOT NULL,
    description TEXT,
    is_system INTEGER NOT NULL,
    is_active INTEGER NOT NULL,
    display_order INTEGER NOT NULL
);

-- 16. Operational: Local Labor Attendance (Mirrors LaborAttendance.cs)
CREATE TABLE IF NOT EXISTS local_labor_attendances (
    id TEXT PRIMARY KEY,
    organization_id TEXT NOT NULL,
    farm_id TEXT NOT NULL,
    worker_id TEXT NOT NULL,
    attendance_date TEXT NOT NULL,
    attendance_type INTEGER NOT NULL,
    working_hours NUMERIC,
    calculated_rate NUMERIC,
    calculated_amount NUMERIC,
    currency_id TEXT,
    status INTEGER NOT NULL,
    notes TEXT,
    finalized_at TEXT,
    finalized_by TEXT,
    created_at TEXT NOT NULL,
    created_by TEXT NOT NULL,
    updated_at TEXT,
    updated_by TEXT,
    sync_status TEXT NOT NULL DEFAULT 'LOCAL_DRAFT'
);

-- 17. Operational: Outbox Mutations Queue (Atomic offline synchronization)
CREATE TABLE IF NOT EXISTS outbox_mutations (
    id TEXT PRIMARY KEY,
    operation_type TEXT NOT NULL,
    endpoint TEXT NOT NULL,
    http_method TEXT NOT NULL,
    idempotency_key TEXT NOT NULL UNIQUE,
    payload_json TEXT NOT NULL,
    dependency_mutation_id TEXT,
    status TEXT NOT NULL DEFAULT 'PENDING',
    retry_count INTEGER NOT NULL DEFAULT 0,
    last_error TEXT,
    created_at TEXT NOT NULL,
    synced_at TEXT
);

-- 18. Metadata: Local Sync History per Entity
CREATE TABLE IF NOT EXISTS local_sync_history (
    entity_name TEXT PRIMARY KEY,
    last_synced_at TEXT NOT NULL,
    records_pulled INTEGER NOT NULL DEFAULT 0,
    status TEXT NOT NULL DEFAULT 'IDLE'
);

-- High-performance query indexes
CREATE INDEX IF NOT EXISTS idx_farm_areas_farm ON cached_farm_areas(farm_id);
CREATE INDEX IF NOT EXISTS idx_plantations_farm ON cached_crop_plantations(farm_id);
CREATE INDEX IF NOT EXISTS idx_crop_cycles_plantation ON cached_crop_cycles(plantation_id);
CREATE INDEX IF NOT EXISTS idx_cycle_stages_cycle ON cached_crop_cycle_stages(crop_cycle_id);
CREATE INDEX IF NOT EXISTS idx_worker_assignments_farm ON cached_worker_farm_assignments(farm_id, worker_id);
CREATE INDEX IF NOT EXISTS idx_storage_locations_farm ON cached_storage_locations(farm_id);
CREATE INDEX IF NOT EXISTS idx_attendances_farm_date ON local_labor_attendances(farm_id, attendance_date);
CREATE INDEX IF NOT EXISTS idx_outbox_status ON outbox_mutations(status, created_at);
`;

/**
 * Array of all 15 master cache table names for bulk statistics and inspection.
 */
export const MASTER_TABLE_NAMES = [
  'cached_farms',
  'cached_farm_areas',
  'cached_crop_plantations',
  'cached_crop_cycles',
  'cached_crop_cycle_stages',
  'cached_workers',
  'cached_worker_farm_assignments',
  'cached_labor_categories',
  'cached_contractors',
  'cached_labor_wage_rates',
  'cached_units',
  'cached_storage_locations',
  'cached_inventory_items',
  'cached_plant_protection_products',
  'cached_irrigation_methods',
] as const;

export type MasterTableName = typeof MASTER_TABLE_NAMES[number];

/**
 * Human-friendly metadata descriptor for displaying table cards on UI.
 */
export interface MasterTableMeta {
  tableName: MasterTableName;
  displayName: string;
  category: 'Land & Crops' | 'Labor & Payroll' | 'Inventory & Supplies' | 'Settings';
  icon: string;
}

export const MASTER_TABLES_META: MasterTableMeta[] = [
  { tableName: 'cached_farms', displayName: 'Farms', category: 'Land & Crops', icon: '🏡' },
  { tableName: 'cached_farm_areas', displayName: 'Farm Areas', category: 'Land & Crops', icon: '🗺️' },
  { tableName: 'cached_crop_plantations', displayName: 'Plantations', category: 'Land & Crops', icon: '🌱' },
  { tableName: 'cached_crop_cycles', displayName: 'Crop Cycles', category: 'Land & Crops', icon: '🔄' },
  { tableName: 'cached_crop_cycle_stages', displayName: 'Cycle Stages', category: 'Land & Crops', icon: '🪜' },
  { tableName: 'cached_workers', displayName: 'Workers', category: 'Labor & Payroll', icon: '👷' },
  { tableName: 'cached_worker_farm_assignments', displayName: 'Worker Assignments', category: 'Labor & Payroll', icon: '📋' },
  { tableName: 'cached_labor_categories', displayName: 'Labor Categories', category: 'Labor & Payroll', icon: '🏷️' },
  { tableName: 'cached_contractors', displayName: 'Contractors', category: 'Labor & Payroll', icon: '🤝' },
  { tableName: 'cached_labor_wage_rates', displayName: 'Wage Rates', category: 'Labor & Payroll', icon: '💵' },
  { tableName: 'cached_units', displayName: 'Units of Measure', category: 'Settings', icon: '⚖️' },
  { tableName: 'cached_storage_locations', displayName: 'Storage Locations', category: 'Inventory & Supplies', icon: '📦' },
  { tableName: 'cached_inventory_items', displayName: 'Inventory Items', category: 'Inventory & Supplies', icon: '🗄️' },
  { tableName: 'cached_plant_protection_products', displayName: 'Plant Protection', category: 'Inventory & Supplies', icon: '🛡️' },
  { tableName: 'cached_irrigation_methods', displayName: 'Irrigation Methods', category: 'Settings', icon: '💧' },
];
