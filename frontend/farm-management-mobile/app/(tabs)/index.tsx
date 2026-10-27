import React, { useState, useEffect, useMemo } from 'react';
import {
  View,
  Text,
  ScrollView,
  TouchableOpacity,
  StyleSheet,
  ActivityIndicator,
  Modal,
  FlatList,
  Image,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useAuthStore } from '../../src/core/auth/useAuthStore';
import { useNetworkStatus } from '../../src/core/network/useNetworkStatus';
import { useSyncStore } from '../../src/core/sync/syncManager';
import { MASTER_TABLES_META, MasterTableName, MasterTableMeta } from '../../src/core/db/schema';
import { getTableSampleRows } from '../../src/core/db/database';
import { getCachedFarms, CachedFarm } from '../../src/core/sync/dataQueries';

/**
 * Master Data & Delta Sync Hub:
 * Primary operational dashboard for Step 2.
 * Allows farm managers and supervisors to:
 * 1. Select the current operational Farm.
 * 2. Monitor real-time local SQLite table record counts across all 15 master tables.
 * 3. Trigger smart delta synchronization or full bootstrap caching.
 * 4. Inspect local SQLite records offline to verify zero-latency field availability.
 */
export default function MasterDataSyncScreen() {
  const { user } = useAuthStore();
  const { isConnected } = useNetworkStatus();
  const {
    isSyncing,
    lastSyncedAt,
    syncError,
    selectedFarmId,
    tableCounts,
    lastIngestedCount,
    initSyncState,
    setSelectedFarmId,
    bootstrapSync,
    syncNow,
    refreshTableCounts,
  } = useSyncStore();

  const [farms, setFarms] = useState<CachedFarm[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<string>('All');
  const [inspectingTable, setInspectingTable] = useState<MasterTableMeta | null>(null);
  const [inspectorRows, setInspectorRows] = useState<any[]>([]);

  // Initialize sync state on mount
  useEffect(() => {
    initSyncState();
    const cachedFarms = getCachedFarms();
    setFarms(cachedFarms);
  }, []);

  // Re-read cached farms whenever table counts change
  useEffect(() => {
    const cachedFarms = getCachedFarms();
    setFarms(cachedFarms);
  }, [tableCounts]);

  // Network listener: automatically trigger sync if user reconnects
  useEffect(() => {
    if (isConnected && lastSyncedAt) {
      // Auto-trigger delta sync on network restoration
      syncNow();
    }
  }, [isConnected]);

  // Filtered tables based on selected category tab
  const categories = ['All', 'Land & Crops', 'Labor & Payroll', 'Inventory & Supplies', 'Settings'];

  const filteredTables = useMemo(() => {
    if (selectedCategory === 'All') return MASTER_TABLES_META;
    return MASTER_TABLES_META.filter((t) => t.category === selectedCategory);
  }, [selectedCategory]);

  const activeFarm = farms.find((f) => f.id === selectedFarmId);

  // Formats UTC date into readable local string
  const formatSyncTime = (isoString: string | null) => {
    if (!isoString) return 'Never synced';
    const date = new Date(isoString);
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' }) +
      ' (' + date.toLocaleDateString() + ')';
  };

  // Opens data inspector modal for a specific SQLite table
  const handleOpenInspector = (meta: MasterTableMeta) => {
    const rows = getTableSampleRows(meta.tableName, 25);
    setInspectorRows(rows);
    setInspectingTable(meta);
  };

  return (
    <SafeAreaView style={styles.safeArea}>
      <ScrollView contentContainerStyle={styles.scrollContent}>
        {/* Screen Header */}
        <View style={styles.header}>
          <View style={styles.headerTopRow}>
            <Image
              source={require('../../assets/farmnxt-logo.png')}
              style={styles.headerBrandLogo}
              resizeMode="contain"
            />
          </View>
          <Text style={styles.headerTitle}>Master Data & Sync</Text>
          <Text style={styles.headerSubtitle}>
            {user?.organizationName ? `Organization: ${user.organizationName}` : 'Offline Field Cache'}
          </Text>
        </View>

        {/* Farm Selector Header */}
        <View style={styles.farmSelectorCard}>
          <Text style={styles.farmSelectorLabel}>📍 Active Farm Context</Text>
          {farms.length === 0 ? (
            <View style={styles.noFarmsBox}>
              <Text style={styles.noFarmsText}>No farms cached locally yet.</Text>
              <Text style={styles.noFarmsHint}>Tap "Full Bootstrap" below to download your farms.</Text>
            </View>
          ) : (
            <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.farmPillsScroll}>
              {farms.map((farm) => {
                const isSelected = farm.id === selectedFarmId;
                return (
                  <TouchableOpacity
                    key={farm.id}
                    style={[styles.farmPill, isSelected && styles.farmPillActive]}
                    onPress={() => setSelectedFarmId(farm.id)}
                    activeOpacity={0.7}
                  >
                    <Text style={[styles.farmPillText, isSelected && styles.farmPillTextActive]}>
                      {farm.name}
                    </Text>
                    {farm.city && (
                      <Text style={[styles.farmPillSub, isSelected && styles.farmPillSubActive]}>
                        {farm.city}
                      </Text>
                    )}
                  </TouchableOpacity>
                );
              })}
            </ScrollView>
          )}
        </View>

        {/* Sync Status & Action Control Card */}
        <View style={styles.syncCard}>
          <View style={styles.syncCardHeader}>
            <View>
              <Text style={styles.syncCardTitle}>⚡ Bi-Directional Delta Engine</Text>
              <Text style={styles.syncCardSubtitle}>
                Last Synced: <Text style={styles.syncTimeText}>{formatSyncTime(lastSyncedAt)}</Text>
              </Text>
            </View>
            <View style={[styles.netBadge, isConnected ? styles.netBadgeOnline : styles.netBadgeOffline]}>
              <Text style={styles.netBadgeText}>{isConnected ? 'ONLINE' : 'OFFLINE'}</Text>
            </View>
          </View>

          {lastIngestedCount > 0 && (
            <View style={styles.ingestedBadgeRow}>
              <Text style={styles.ingestedBadgeText}>
                ✓ Ingested {lastIngestedCount} records into local SQLite
              </Text>
            </View>
          )}

          {syncError && (
            <View style={styles.errorBox}>
              <Text style={styles.errorText}>⚠️ Sync Error: {syncError}</Text>
            </View>
          )}

          {/* Sync Trigger Buttons */}
          <View style={styles.syncActionsRow}>
            <TouchableOpacity
              style={[styles.syncButtonPrimary, isSyncing && styles.btnDisabled]}
              onPress={() => syncNow(selectedFarmId)}
              disabled={isSyncing}
              activeOpacity={0.8}
            >
              {isSyncing ? (
                <ActivityIndicator color="#FFFFFF" size="small" />
              ) : (
                <Text style={styles.syncButtonPrimaryText}>🔄 Sync Now</Text>
              )}
            </TouchableOpacity>

            <TouchableOpacity
              style={[styles.syncButtonSecondary, isSyncing && styles.btnDisabled]}
              onPress={() => bootstrapSync(selectedFarmId)}
              disabled={isSyncing}
              activeOpacity={0.8}
            >
              <Text style={styles.syncButtonSecondaryText}>📥 Full Bootstrap</Text>
            </TouchableOpacity>
          </View>
        </View>

        {/* Category Filter Chips */}
        <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.categoryScroll}>
          {categories.map((cat) => {
            const isSelected = selectedCategory === cat;
            return (
              <TouchableOpacity
                key={cat}
                style={[styles.categoryChip, isSelected && styles.categoryChipActive]}
                onPress={() => setSelectedCategory(cat)}
              >
                <Text style={[styles.categoryChipText, isSelected && styles.categoryChipTextActive]}>
                  {cat}
                </Text>
              </TouchableOpacity>
            );
          })}
        </ScrollView>

        {/* 15 Master Tables Record Grid */}
        <View style={styles.gridContainer}>
          {filteredTables.map((table) => {
            const count = tableCounts[table.tableName] ?? 0;
            return (
              <View key={table.tableName} style={styles.tableCard}>
                <View style={styles.tableCardHeader}>
                  <Text style={styles.tableIcon}>{table.icon}</Text>
                  <View style={[styles.countBadge, count > 0 ? styles.countBadgeActive : styles.countBadgeZero]}>
                    <Text style={[styles.countBadgeText, count > 0 ? styles.countBadgeTextActive : styles.countBadgeTextZero]}>
                      {count} rows
                    </Text>
                  </View>
                </View>

                <Text style={styles.tableDisplayName}>{table.displayName}</Text>
                <Text style={styles.tableDbName}>{table.tableName}</Text>

                <TouchableOpacity
                  style={styles.inspectButton}
                  onPress={() => handleOpenInspector(table)}
                  activeOpacity={0.7}
                >
                  <Text style={styles.inspectButtonText}>🔍 Inspect Data</Text>
                </TouchableOpacity>
              </View>
            );
          })}
        </View>
      </ScrollView>

      {/* Offline Data Inspector Modal */}
      <Modal
        visible={inspectingTable !== null}
        animationType="slide"
        presentationStyle="pageSheet"
        onRequestClose={() => setInspectingTable(null)}
      >
        <SafeAreaView style={styles.modalContainer}>
          <View style={styles.modalHeader}>
            <View>
              <Text style={styles.modalTitle}>
                {inspectingTable?.icon} {inspectingTable?.displayName}
              </Text>
              <Text style={styles.modalSubtitle}>
                SQLite: {inspectingTable?.tableName} ({inspectorRows.length} cached rows)
              </Text>
            </View>
            <TouchableOpacity style={styles.modalCloseBtn} onPress={() => setInspectingTable(null)}>
              <Text style={styles.modalCloseBtnText}>Done</Text>
            </TouchableOpacity>
          </View>

          {inspectorRows.length === 0 ? (
            <View style={styles.modalEmptyBox}>
              <Text style={styles.modalEmptyIcon}>📭</Text>
              <Text style={styles.modalEmptyText}>No cached records found in local SQLite table.</Text>
              <Text style={styles.modalEmptyHint}>Tap "Sync Now" or "Full Bootstrap" to populate data.</Text>
            </View>
          ) : (
            <FlatList
              data={inspectorRows}
              keyExtractor={(item, index) => item.id || String(index)}
              contentContainerStyle={styles.modalListContent}
              renderItem={({ item, index }) => (
                <View style={styles.inspectorRowCard}>
                  <View style={styles.inspectorRowHeader}>
                    <Text style={styles.inspectorRowIndex}>#{index + 1}</Text>
                    <Text style={styles.inspectorRowId} numberOfLines={1}>
                      {item.id}
                    </Text>
                  </View>

                  {/* Highlight key properties */}
                  {Object.entries(item).map(([key, val]) => {
                    if (key === 'id') return null;
                    return (
                      <View key={key} style={styles.propRow}>
                        <Text style={styles.propKey}>{key}:</Text>
                        <Text style={styles.propVal} numberOfLines={2}>
                          {val === null || val === undefined ? 'null' : String(val)}
                        </Text>
                      </View>
                    );
                  })}
                </View>
              )}
            />
          )}
        </SafeAreaView>
      </Modal>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: '#F3F4F6',
  },
  scrollContent: {
    padding: 16,
    paddingBottom: 40,
  },
  header: {
    marginBottom: 14,
  },
  headerTopRow: {
    marginBottom: 8,
  },
  headerBrandLogo: {
    width: 130,
    height: 36,
  },
  headerTitle: {
    fontSize: 24,
    fontWeight: '800',
    color: '#111827',
  },
  headerSubtitle: {
    fontSize: 14,
    color: '#6B7280',
    marginTop: 2,
    fontWeight: '500',
  },
  farmSelectorCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    padding: 14,
    marginBottom: 14,
    borderWidth: 1,
    borderColor: '#E5E7EB',
  },
  farmSelectorLabel: {
    fontSize: 13,
    fontWeight: '700',
    color: '#374151',
    marginBottom: 8,
    textTransform: 'uppercase',
    letterSpacing: 0.5,
  },
  noFarmsBox: {
    paddingVertical: 10,
  },
  noFarmsText: {
    fontSize: 13,
    color: '#6B7280',
    fontWeight: '500',
  },
  noFarmsHint: {
    fontSize: 12,
    color: '#2563EB',
    fontWeight: '600',
    marginTop: 2,
  },
  farmPillsScroll: {
    flexDirection: 'row',
  },
  farmPill: {
    backgroundColor: '#F3F4F6',
    paddingVertical: 8,
    paddingHorizontal: 14,
    borderRadius: 8,
    marginRight: 8,
    borderWidth: 1,
    borderColor: '#E5E7EB',
  },
  farmPillActive: {
    backgroundColor: '#2E7D32',
    borderColor: '#1B5E20',
  },
  farmPillText: {
    fontSize: 14,
    fontWeight: '700',
    color: '#374151',
  },
  farmPillTextActive: {
    color: '#FFFFFF',
  },
  farmPillSub: {
    fontSize: 11,
    color: '#6B7280',
    marginTop: 2,
  },
  farmPillSubActive: {
    color: '#E8F5E9',
  },
  syncCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    padding: 16,
    marginBottom: 16,
    elevation: 2,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 4,
    borderWidth: 1,
    borderColor: '#E5E7EB',
  },
  syncCardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: 10,
  },
  syncCardTitle: {
    fontSize: 16,
    fontWeight: '800',
    color: '#111827',
  },
  syncCardSubtitle: {
    fontSize: 12,
    color: '#6B7280',
    marginTop: 3,
  },
  syncTimeText: {
    fontWeight: '700',
    color: '#1F2937',
  },
  netBadge: {
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 6,
  },
  netBadgeOnline: {
    backgroundColor: '#DEF7EC',
  },
  netBadgeOffline: {
    backgroundColor: '#FDE8E8',
  },
  netBadgeText: {
    fontSize: 11,
    fontWeight: '800',
    color: '#03543F',
  },
  ingestedBadgeRow: {
    backgroundColor: '#F0FDF4',
    padding: 8,
    borderRadius: 6,
    marginBottom: 12,
    borderWidth: 1,
    borderColor: '#BBF7D0',
  },
  ingestedBadgeText: {
    fontSize: 12,
    fontWeight: '700',
    color: '#16A34A',
  },
  errorBox: {
    backgroundColor: '#FEF2F2',
    padding: 10,
    borderRadius: 6,
    marginBottom: 12,
    borderWidth: 1,
    borderColor: '#FECACA',
  },
  errorText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#DC2626',
  },
  syncActionsRow: {
    flexDirection: 'row',
    gap: 10,
  },
  syncButtonPrimary: {
    flex: 1,
    height: 48,
    backgroundColor: '#2E7D32',
    borderRadius: 8,
    justifyContent: 'center',
    alignItems: 'center',
  },
  syncButtonPrimaryText: {
    color: '#FFFFFF',
    fontWeight: '700',
    fontSize: 15,
  },
  syncButtonSecondary: {
    flex: 1,
    height: 48,
    backgroundColor: '#E8F5E9',
    borderRadius: 8,
    justifyContent: 'center',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#A5D6A7',
  },
  syncButtonSecondaryText: {
    color: '#1B5E20',
    fontWeight: '700',
    fontSize: 14,
  },
  btnDisabled: {
    opacity: 0.6,
  },
  categoryScroll: {
    flexDirection: 'row',
    marginBottom: 14,
  },
  categoryChip: {
    backgroundColor: '#E5E7EB',
    paddingVertical: 6,
    paddingHorizontal: 12,
    borderRadius: 20,
    marginRight: 8,
  },
  categoryChipActive: {
    backgroundColor: '#111827',
  },
  categoryChipText: {
    fontSize: 13,
    fontWeight: '600',
    color: '#4B5563',
  },
  categoryChipTextActive: {
    color: '#FFFFFF',
  },
  gridContainer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 12,
  },
  tableCard: {
    width: '48%',
    backgroundColor: '#FFFFFF',
    borderRadius: 10,
    padding: 12,
    borderWidth: 1,
    borderColor: '#E5E7EB',
    justifyContent: 'space-between',
  },
  tableCardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
  },
  tableIcon: {
    fontSize: 22,
  },
  countBadge: {
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 10,
  },
  countBadgeActive: {
    backgroundColor: '#DCFCE7',
  },
  countBadgeZero: {
    backgroundColor: '#F3F4F6',
  },
  countBadgeText: {
    fontSize: 11,
    fontWeight: '700',
  },
  countBadgeTextActive: {
    color: '#15803D',
  },
  countBadgeTextZero: {
    color: '#9CA3AF',
  },
  tableDisplayName: {
    fontSize: 14,
    fontWeight: '700',
    color: '#1F2937',
  },
  tableDbName: {
    fontSize: 10,
    color: '#6B7280',
    fontFamily: 'monospace',
    marginTop: 2,
    marginBottom: 10,
  },
  inspectButton: {
    backgroundColor: '#F9FAFB',
    paddingVertical: 6,
    borderRadius: 6,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#D1D5DB',
  },
  inspectButtonText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#374151',
  },
  modalContainer: {
    flex: 1,
    backgroundColor: '#F9FAFB',
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 16,
    backgroundColor: '#FFFFFF',
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
  },
  modalTitle: {
    fontSize: 18,
    fontWeight: '800',
    color: '#111827',
  },
  modalSubtitle: {
    fontSize: 12,
    color: '#6B7280',
    marginTop: 2,
    fontFamily: 'monospace',
  },
  modalCloseBtn: {
    paddingHorizontal: 14,
    paddingVertical: 8,
    backgroundColor: '#2E7D32',
    borderRadius: 8,
  },
  modalCloseBtnText: {
    color: '#FFFFFF',
    fontWeight: '700',
    fontSize: 13,
  },
  modalEmptyBox: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 24,
  },
  modalEmptyIcon: {
    fontSize: 48,
    marginBottom: 12,
  },
  modalEmptyText: {
    fontSize: 15,
    fontWeight: '700',
    color: '#374151',
    textAlign: 'center',
  },
  modalEmptyHint: {
    fontSize: 13,
    color: '#6B7280',
    marginTop: 6,
    textAlign: 'center',
  },
  modalListContent: {
    padding: 16,
    gap: 12,
  },
  inspectorRowCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 8,
    padding: 12,
    borderWidth: 1,
    borderColor: '#E5E7EB',
  },
  inspectorRowHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 8,
    paddingBottom: 6,
    borderBottomWidth: 1,
    borderBottomColor: '#F3F4F6',
  },
  inspectorRowIndex: {
    fontSize: 12,
    fontWeight: '800',
    color: '#2E7D32',
  },
  inspectorRowId: {
    fontSize: 11,
    fontFamily: 'monospace',
    color: '#6B7280',
    maxWidth: 240,
  },
  propRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: 2,
  },
  propKey: {
    fontSize: 11,
    fontWeight: '600',
    color: '#4B5563',
    fontFamily: 'monospace',
  },
  propVal: {
    fontSize: 11,
    color: '#111827',
    fontWeight: '500',
    maxWidth: 200,
    textAlign: 'right',
  },
});
