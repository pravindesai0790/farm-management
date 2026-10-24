import { Platform } from 'react-native';
import * as SQLite from 'expo-sqlite';

/**
 * Name of the local embedded SQLite database for offline operations.
 */
export const DB_NAME = 'farm_management_offline.db';

let dbInstance: SQLite.SQLiteDatabase | null = null;

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
 * 3. Provisions the internal `local_system_info` metadata table.
 */
export function initializeDatabase(): DbHealthResult {
  try {
    if (Platform.OS === 'web') {
      return {
        fileName: DB_NAME,
        status: 'Ready',
        journalMode: 'Web-Preview',
        version: 1,
        tablesCount: 1,
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

    // 3. Record initialization metadata
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
        tablesCount: 1,
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
