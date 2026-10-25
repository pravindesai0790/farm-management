import { create } from 'zustand';
import { executeBootstrapPull, executeDeltaPull } from './deltaPuller';
import { getTableCounts, getDatabase } from '../db/database';
import { MasterTableName } from '../db/schema';
import { getCachedFarms } from './dataQueries';
import { Platform } from 'react-native';

/**
 * Synchronization state and manager interface for offline master data caching.
 */
export interface SyncState {
  isSyncing: boolean;
  lastSyncedAt: string | null;
  syncError: string | null;
  selectedFarmId: string | null;
  tableCounts: Record<MasterTableName, number>;
  lastIngestedCount: number;

  // Actions
  initSyncState: () => void;
  setSelectedFarmId: (farmId: string | null) => void;
  refreshTableCounts: () => void;
  bootstrapSync: (farmId?: string | null) => Promise<boolean>;
  deltaSync: (farmId?: string | null) => Promise<boolean>;
  syncNow: (farmId?: string | null) => Promise<boolean>;
}

/**
 * Global Zustand store managing background delta sync, manual bootstrap triggering,
 * table count telemetry, and active farm filtering.
 */
export const useSyncStore = create<SyncState>((set, get) => ({
  isSyncing: false,
  lastSyncedAt: null,
  syncError: null,
  selectedFarmId: null,
  tableCounts: getTableCounts(),
  lastIngestedCount: 0,

  /**
   * Reads initial sync checkpoint and table records from local SQLite.
   */
  initSyncState: () => {
    let lastSyncedAt: string | null = null;

    if (Platform.OS !== 'web') {
      const db = getDatabase();
      if (db) {
        try {
          const res = db.getAllSync<{ value: string }>(
            "SELECT value FROM local_system_info WHERE key = 'last_synced_at';"
          );
          if (res.length > 0 && res[0].value) {
            lastSyncedAt = res[0].value;
          }
        } catch (err) {
          console.warn('[useSyncStore] Failed to read last_synced_at:', err);
        }
      }
    }

    const counts = getTableCounts();
    const farms = getCachedFarms();
    const initialFarmId = get().selectedFarmId || (farms.length > 0 ? farms[0].id : null);

    set({
      lastSyncedAt,
      tableCounts: counts,
      selectedFarmId: initialFarmId,
    });
  },

  /**
   * Sets the active farm scope for parcelation and operational records.
   */
  setSelectedFarmId: (farmId: string | null) => {
    set({ selectedFarmId: farmId });
  },

  /**
   * Refreshes real-time row counts across all 15 master tables.
   */
  refreshTableCounts: () => {
    const counts = getTableCounts();
    set({ tableCounts: counts });
  },

  /**
   * Triggers a complete initial bootstrap sync from the backend API.
   */
  bootstrapSync: async (farmId?: string | null) => {
    const targetFarmId = farmId !== undefined ? farmId : get().selectedFarmId;
    set({ isSyncing: true, syncError: null });

    try {
      const result = await executeBootstrapPull(targetFarmId);
      if (result.success) {
        const counts = getTableCounts();
        const farms = getCachedFarms();
        const resolvedFarmId = targetFarmId || (farms.length > 0 ? farms[0].id : null);

        set({
          isSyncing: false,
          lastSyncedAt: result.syncedAtUtc,
          tableCounts: counts,
          lastIngestedCount: result.totalRecordsIngested,
          selectedFarmId: resolvedFarmId,
          syncError: null,
        });
        return true;
      } else {
        set({
          isSyncing: false,
          syncError: result.errorMessage || 'Bootstrap sync failed',
        });
        return false;
      }
    } catch (err: any) {
      set({
        isSyncing: false,
        syncError: err.message || 'Unexpected sync error',
      });
      return false;
    }
  },

  /**
   * Triggers an incremental delta sync querying changes since the previous sync checkpoint.
   */
  deltaSync: async (farmId?: string | null) => {
    const { lastSyncedAt, selectedFarmId } = get();
    if (!lastSyncedAt) {
      // If never synced before, fall back to full bootstrap
      return get().bootstrapSync(farmId);
    }

    const targetFarmId = farmId !== undefined ? farmId : selectedFarmId;
    set({ isSyncing: true, syncError: null });

    try {
      const result = await executeDeltaPull(lastSyncedAt, targetFarmId);
      if (result.success) {
        const counts = getTableCounts();
        set({
          isSyncing: false,
          lastSyncedAt: result.syncedAtUtc,
          tableCounts: counts,
          lastIngestedCount: result.totalRecordsIngested,
          syncError: null,
        });
        return true;
      } else {
        set({
          isSyncing: false,
          syncError: result.errorMessage || 'Delta sync failed',
        });
        return false;
      }
    } catch (err: any) {
      set({
        isSyncing: false,
        syncError: err.message || 'Unexpected sync error',
      });
      return false;
    }
  },

  /**
   * Smart synchronization action: performs delta pull if checkpoint exists, or full bootstrap if clean.
   */
  syncNow: async (farmId?: string | null) => {
    const { lastSyncedAt } = get();
    if (!lastSyncedAt) {
      return get().bootstrapSync(farmId);
    }
    return get().deltaSync(farmId);
  },
}));
