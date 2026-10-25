import { Platform } from 'react-native';
import * as SQLite from 'expo-sqlite';
import { CREATE_TABLES_SQL, MASTER_TABLE_NAMES, MasterTableName } from './schema';

/**
 * Name of the local embedded SQLite database for offline operations.
 */
export const DB_NAME = 'farm_management_offline.db';

let dbInstance: SQLite.SQLiteDatabase | null = null;

/**
 * In-memory fallback dictionary for web preview when SharedArrayBuffer / native SQLite is unavailable.
 */
const webPreviewStore: Record<string, any[]> = {};

/**
 * Retrieves or initializes the SQLite database connection singleton.
 */
export function getDatabase(): SQLite.SQLiteDatabase | null {
  if (Platform.OS === 'web') {
    // In web preview, native SQLite WAL mode via SharedArrayBuffer is typically unsupported in default browser sandbox.
    return null;
  }
  if (!dbInstance) {
    dbInstance = SQLite.openDatabaseSync(DB_NAME);
  }
  return dbInstance;
}

/**
 * Result structure returned by the database health diagnostic check.
 */
export interface DbHealthResult {
  fileName: string;
  status: 'Ready' | 'Error';
  journalMode: string;
  version: number;
  tablesCount: number;
  lastInitializedAt: string;
  errorMessage?: string;
}

/**
 * Initializes the SQLite database on app startup:
 * 1. Enables Write-Ahead Logging (WAL) for high concurrency.
 * 2. Enables foreign key enforcement.
 * 3. Provisions all 15 master cache tables and 2 operational/outbox tables.
 * 4. Records schema version in metadata table.
 */
export function initializeDatabase(): DbHealthResult {
  try {
    if (Platform.OS === 'web') {
      // Initialize web mock tables if empty
      for (const t of MASTER_TABLE_NAMES) {
        if (!webPreviewStore[t]) {
          webPreviewStore[t] = [];
        }
      }
      return {
        fileName: DB_NAME,
        status: 'Ready',
        journalMode: 'Web-Preview',
        version: 1,
        tablesCount: MASTER_TABLE_NAMES.length + 3,
        lastInitializedAt: new Date().toISOString(),
      };
    }

    const db = getDatabase();
    if (!db) {
      throw new Error('Database instance unavailable');
    }

    // 1. Enable WAL Mode (Write-Ahead Logging provides maximum durability & read/write concurrency)
    db.execSync('PRAGMA journal_mode = WAL;');
    db.execSync('PRAGMA foreign_keys = ON;');

    // 2. Ensure system metadata table exists to track schema migrations and sync states
    db.execSync(`
      CREATE TABLE IF NOT EXISTS local_system_info (
        key TEXT PRIMARY KEY,
        value TEXT NOT NULL,
        updated_at TEXT NOT NULL
      );
    `);

    // 3. Provision all 15 master tables, 2 operational tables, and indexes from schema definition
    db.execSync(CREATE_TABLES_SQL);

    // 4. Record initialization metadata
    const now = new Date().toISOString();
    db.runSync(
      `INSERT INTO local_system_info (key, value, updated_at) 
       VALUES ('schema_version', '1', ?) 
       ON CONFLICT(key) DO UPDATE SET updated_at = ?`,
      [now, now]
    );

    return getDbHealth();
  } catch (err: any) {
    return {
      fileName: DB_NAME,
      status: 'Error',
      journalMode: 'Unknown',
      version: 0,
      tablesCount: 0,
      lastInitializedAt: new Date().toISOString(),
      errorMessage: err.message || String(err),
    };
  }
}

/**
 * Performs a live diagnostic query against SQLite to verify connection, WAL mode, and table count.
 */
export function getDbHealth(): DbHealthResult {
  try {
    if (Platform.OS === 'web') {
      return {
        fileName: DB_NAME,
        status: 'Ready',
        journalMode: 'Web-Preview',
        version: 1,
        tablesCount: MASTER_TABLE_NAMES.length + 3,
        lastInitializedAt: new Date().toISOString(),
      };
    }

    const db = getDatabase();
    if (!db) {
      throw new Error('Database instance unavailable');
    }

    // Query active journal mode
    const pragmaRes = db.getAllSync<{ journal_mode: string }>('PRAGMA journal_mode;');
    const journalMode = pragmaRes[0]?.journal_mode || 'unknown';

    // Count user tables in SQLite
    const tablesRes = db.getAllSync<{ count: number }>(
      "SELECT count(*) as count FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';"
    );
    const tablesCount = tablesRes[0]?.count ?? 0;

    // Read stored schema version
    const versionRes = db.getAllSync<{ value: string }>(
      "SELECT value FROM local_system_info WHERE key = 'schema_version';"
    );
    const version = versionRes[0] ? parseInt(versionRes[0].value, 10) : 1;

    return {
      fileName: DB_NAME,
      status: 'Ready',
      journalMode: journalMode.toUpperCase(),
      version,
      tablesCount,
      lastInitializedAt: new Date().toISOString(),
    };
  } catch (err: any) {
    return {
      fileName: DB_NAME,
      status: 'Error',
      journalMode: 'Error',
      version: 0,
      tablesCount: 0,
      lastInitializedAt: new Date().toISOString(),
      errorMessage: err.message || String(err),
    };
  }
}

/**
 * Queries the record counts for all 15 master cache tables in real-time.
 * Returns a mapping from table_name to row count.
 */
export function getTableCounts(): Record<MasterTableName, number> {
  const counts: Partial<Record<MasterTableName, number>> = {};

  if (Platform.OS === 'web') {
    for (const t of MASTER_TABLE_NAMES) {
      counts[t] = webPreviewStore[t]?.length ?? 0;
    }
    return counts as Record<MasterTableName, number>;
  }

  const db = getDatabase();
  if (!db) {
    for (const t of MASTER_TABLE_NAMES) {
      counts[t] = 0;
    }
    return counts as Record<MasterTableName, number>;
  }

  try {
    for (const tableName of MASTER_TABLE_NAMES) {
      const res = db.getAllSync<{ count: number }>(`SELECT COUNT(*) as count FROM ${tableName};`);
      counts[tableName] = res[0]?.count ?? 0;
    }
  } catch (err) {
    console.warn('[database] getTableCounts error:', err);
    for (const t of MASTER_TABLE_NAMES) {
      if (counts[t] === undefined) {
        counts[t] = 0;
      }
    }
  }

  return counts as Record<MasterTableName, number>;
}

/**
 * Fetches sample rows from a specific local SQLite table for inspector verification.
 */
export function getTableSampleRows(tableName: MasterTableName, limit = 20): any[] {
  if (Platform.OS === 'web') {
    return (webPreviewStore[tableName] || []).slice(0, limit);
  }

  const db = getDatabase();
  if (!db) return [];

  try {
    return db.getAllSync(`SELECT * FROM ${tableName} LIMIT ${limit};`);
  } catch (err) {
    console.warn(`[database] getTableSampleRows error for ${tableName}:`, err);
    return [];
  }
}

/**
 * Web mock storage setter to allow simulated offline testing in browser.
 */
export function setWebPreviewData(tableName: MasterTableName, records: any[]) {
  webPreviewStore[tableName] = records;
}
