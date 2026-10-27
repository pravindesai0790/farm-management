import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
  SafeAreaView,
  View,
  Text,
  TextInput,
  TouchableOpacity,
  FlatList,
  StyleSheet,
  ActivityIndicator,
  Alert,
  Modal,
  Platform,
  StatusBar,
  ScrollView,
  RefreshControl,
} from 'react-native';
import { useNetworkStatus } from '../../src/core/network/useNetworkStatus';
import { useSyncStore } from '../../src/core/sync/syncManager';
import { getCachedFarms, CachedFarm } from '../../src/core/sync/dataQueries';
import {
  loadDailyRoster,
  syncDailyAttendanceFromServer,
  saveOfflineDraft,
  syncDraftOnline,
  finalizeAttendance,
  AttendanceType,
  AttendanceStatus,
  RosterWorkerItem,
  DailyRosterSummary,
  getDefaultHoursForType,
  calculateEstimatedWage,
  RawWageRate,
} from '../../src/core/attendance/attendanceService';
import { QuickAddWorkerModal } from '../../src/core/attendance/QuickAddWorkerModal';
import {
  getPendingOutboxCount,
  processOutboxQueue,
  clearFailedMutations,
} from '../../src/core/sync/outboxProcessor';

/**
 * Main Daily Field Attendance Screen (Mode A: Gate Check-in).
 * 
 * Implements a structured 3-Step Lifecycle:
 * - Step 1: Save Draft (Offline) -> local SQLite storage, allows edits & re-saving.
 * - Step 2: Save Draft (Online) -> server synchronization, freezes mobile screen.
 * - Step 3: Finalize Attendance -> irreversible financial ledger creation, permanent lock.
 * 
 * Strict architectural guidelines:
 * - Zero inline styles (all styles centralized in StyleSheet).
 * - Full TypeScript typing without any types.
 * - Comprehensive comments explaining all behaviors.
 */
export default function AttendanceScreen() {
  const network = useNetworkStatus();
  const selectedFarmIdFromStore = useSyncStore((s) => s.selectedFarmId);
  const setSelectedFarmIdInStore = useSyncStore((s) => s.setSelectedFarmId);

  // Active Farm & Date States
  const [farms, setFarms] = useState<CachedFarm[]>([]);
  const [selectedFarmId, setSelectedFarmId] = useState<string>('');
  const [farmPickerVisible, setFarmPickerVisible] = useState(false);
  const [attendanceDate, setAttendanceDate] = useState<string>(
    () => new Date().toISOString().split('T')[0]
  );

  // Operational Roster States
  const [rosterItems, setRosterItems] = useState<RosterWorkerItem[]>([]);
  const [wageRates, setWageRates] = useState<RawWageRate[]>([]);
  const [summary, setSummary] = useState<DailyRosterSummary>({
    totalAssigned: 0,
    presentCount: 0,
    absentCount: 0,
    estimatedEarnings: 0,
    isFinalized: false,
    isLockedOnline: false,
    hasLocalDraft: false,
  });

  // Track if changes have been made in memory since the last offline SQLite save
  const [hasOfflineDraftChanges, setHasOfflineDraftChanges] = useState(false);

  // UI Interactive States
  const [searchQuery, setSearchQuery] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isSavingOffline, setIsSavingOffline] = useState(false);
  const [isSyncingOnline, setIsSyncingOnline] = useState(false);
  const [isFinalizing, setIsFinalizing] = useState(false);
  const [isSyncingOutbox, setIsSyncingOutbox] = useState(false);
  const [pendingOutboxCount, setPendingOutboxCount] = useState(0);

  // Modals
  const [quickAddVisible, setQuickAddVisible] = useState(false);
  const [step1ModalVisible, setStep1ModalVisible] = useState(false);
  const [step2ModalVisible, setStep2ModalVisible] = useState(false);
  const [step3ModalVisible, setStep3ModalVisible] = useState(false);

  // Derived lock state: screen is read-only if synced online or finalized
  const isScreenLocked = summary.isFinalized || summary.isLockedOnline;

  // 1. Initial Master Data Load
  useEffect(() => {
    const cachedFarms = getCachedFarms();
    setFarms(cachedFarms);
    if (cachedFarms.length > 0) {
      const initialFarmId = selectedFarmIdFromStore || cachedFarms[0].id;
      setSelectedFarmId(initialFarmId);
    }
    updateOutboxCount();
  }, []);

  // Updates pending outbox telemetry count
  const updateOutboxCount = useCallback(() => {
    const count = getPendingOutboxCount();
    setPendingOutboxCount(count);
  }, []);

  // 2. Load Daily Roster when Farm or Date Changes
  const reloadRoster = useCallback(
    async (options?: { skipServerSync?: boolean }) => {
      if (!selectedFarmId) return;
      setIsLoading(true);
      try {
        if (!options?.skipServerSync && network.isConnected) {
          await syncDailyAttendanceFromServer(selectedFarmId, attendanceDate);
        }
        const { items, summary: s, wageRates: rates } = await loadDailyRoster(selectedFarmId, attendanceDate);
        setRosterItems(items);
        setWageRates(rates);
        setSummary(s);
        setHasOfflineDraftChanges(false);
        updateOutboxCount();
      } catch (err: unknown) {
        console.warn('[AttendanceScreen] Error loading roster:', err);
      } finally {
        setIsLoading(false);
      }
    },
    [selectedFarmId, attendanceDate, network.isConnected, updateOutboxCount]
  );

  // Pull-to-Refresh handler to fetch latest server state
  const handleRefresh = useCallback(async () => {
    if (!selectedFarmId) return;

    if (hasOfflineDraftChanges) {
      Alert.alert(
        'Unsaved Local Changes',
        'You have unsaved offline attendance changes for today. Refreshing will reload server records. Do you wish to continue?',
        [
          { text: 'Cancel', style: 'cancel' },
          {
            text: 'Discard & Refresh',
            style: 'destructive',
            onPress: async () => {
              setIsRefreshing(true);
              try {
                if (network.isConnected) {
                  await syncDailyAttendanceFromServer(selectedFarmId, attendanceDate);
                }
                const { items, summary: s, wageRates: rates } = await loadDailyRoster(selectedFarmId, attendanceDate);
                setRosterItems(items);
                setWageRates(rates);
                setSummary(s);
                setHasOfflineDraftChanges(false);
                updateOutboxCount();
              } catch (err: unknown) {
                console.warn('[AttendanceScreen] Error during pull-to-refresh:', err);
              } finally {
                setIsRefreshing(false);
              }
            },
          },
        ]
      );
      return;
    }

    setIsRefreshing(true);
    try {
      if (network.isConnected) {
        await syncDailyAttendanceFromServer(selectedFarmId, attendanceDate);
      }
      const { items, summary: s, wageRates: rates } = await loadDailyRoster(selectedFarmId, attendanceDate);
      setRosterItems(items);
      setWageRates(rates);
      setSummary(s);
      setHasOfflineDraftChanges(false);
      updateOutboxCount();
    } catch (err: unknown) {
      console.warn('[AttendanceScreen] Error during pull-to-refresh:', err);
    } finally {
      setIsRefreshing(false);
    }
  }, [selectedFarmId, attendanceDate, network.isConnected, hasOfflineDraftChanges, updateOutboxCount]);

  useEffect(() => {
    reloadRoster();
  }, [reloadRoster]);

  // Handle active farm switch
  const handleSelectFarm = (farmId: string) => {
    setSelectedFarmId(farmId);
    setSelectedFarmIdInStore(farmId);
    setFarmPickerVisible(false);
  };

  // Date Navigation Handlers
  const handleDateOffset = (days: number) => {
    if (hasOfflineDraftChanges) {
      Alert.alert(
        'Unsaved Offline Changes',
        'You have unsaved attendance changes for this date. Navigating away will discard them.',
        [
          { text: 'Stay Here', style: 'cancel' },
          {
            text: 'Discard & Change Date',
            style: 'destructive',
            onPress: () => {
              const current = new Date(attendanceDate);
              current.setDate(current.getDate() + days);
              setAttendanceDate(current.toISOString().split('T')[0]);
            },
          },
        ]
      );
      return;
    }

    const current = new Date(attendanceDate);
    current.setDate(current.getDate() + days);
    setAttendanceDate(current.toISOString().split('T')[0]);
  };

  // Re-calculates summary metrics whenever roster items change
  const recalculateSummary = (items: RosterWorkerItem[]): DailyRosterSummary => {
    let presentCount = 0;
    let absentCount = 0;
    let estimatedEarnings = 0;

    for (const item of items) {
      if (item.isCheckedIn) {
        presentCount++;
        estimatedEarnings += item.calculatedAmount;
      } else {
        absentCount++;
      }
    }

    return {
      ...summary,
      totalAssigned: items.length,
      presentCount,
      absentCount,
      estimatedEarnings: Math.round(estimatedEarnings * 100) / 100,
    };
  };

  // 3. Single Attendance Toggle Button Handler (Mark Present ⇄ Present)
  const handleTogglePresent = (workerId: string) => {
    if (isScreenLocked) return;

    setRosterItems((prev) => {
      const updated = prev.map((item) => {
        if (item.workerId !== workerId) return item;

        const nextCheckedIn = !item.isCheckedIn;
        const nextType = nextCheckedIn ? AttendanceType.FullDay : AttendanceType.NotWorked;
        const nextHours = nextCheckedIn ? 8.0 : 0.0;
        const wage = calculateEstimatedWage(item.gender, nextType, nextHours, wageRates, attendanceDate);

        return {
          ...item,
          isCheckedIn: nextCheckedIn,
          attendanceType: nextType,
          workingHours: nextHours,
          calculatedRate: wage.rate,
          calculatedAmount: wage.amount,
          currencyId: wage.currencyId,
          syncStatus: 'LOCAL_DRAFT' as const,
        };
      });

      setSummary(recalculateSummary(updated));
      return updated;
    });

    setHasOfflineDraftChanges(true);
  };

  // 4. Attendance Type Selector Handler (FullDay | HalfDay | Hourly)
  const handleTypeChange = (workerId: string, newType: AttendanceType) => {
    if (isScreenLocked) return;

    setRosterItems((prev) => {
      const updated = prev.map((item) => {
        if (item.workerId !== workerId) return item;

        // When switching to Hourly, preserve or initialize working hours to at least 1.0h
        let defaultHours = getDefaultHoursForType(newType);
        if (newType === AttendanceType.Hourly) {
          defaultHours = item.workingHours > 0 ? item.workingHours : 1.0;
        }

        const wage = calculateEstimatedWage(item.gender, newType, defaultHours, wageRates, attendanceDate);

        return {
          ...item,
          attendanceType: newType,
          workingHours: defaultHours,
          calculatedRate: wage.rate,
          calculatedAmount: wage.amount,
          currencyId: wage.currencyId,
          isCheckedIn: true,
          syncStatus: 'LOCAL_DRAFT' as const,
        };
      });

      setSummary(recalculateSummary(updated));
      return updated;
    });

    setHasOfflineDraftChanges(true);
  };

  // 5. Working Hours Stepper Handler (Only active for Hourly attendance)
  const handleHoursStep = (workerId: string, delta: number) => {
    if (isScreenLocked) return;

    setRosterItems((prev) => {
      const updated = prev.map((item) => {
        if (item.workerId !== workerId) return item;
        if (item.attendanceType !== AttendanceType.Hourly) return item;

        const current = item.workingHours;
        const next = Math.max(0.5, Math.min(24, Math.round((current + delta) * 10) / 10));

        const wage = calculateEstimatedWage(item.gender, AttendanceType.Hourly, next, wageRates, attendanceDate);

        return {
          ...item,
          workingHours: next,
          calculatedRate: wage.rate,
          calculatedAmount: wage.amount,
          currencyId: wage.currencyId,
          isCheckedIn: true,
          syncStatus: 'LOCAL_DRAFT' as const,
        };
      });

      setSummary(recalculateSummary(updated));
      return updated;
    });

    setHasOfflineDraftChanges(true);
  };

  // 6. "Clear All Attendance" Action (Active during Step 1)
  const handleClearAllAttendance = () => {
    if (isScreenLocked || summary.presentCount === 0) return;

    Alert.alert(
      'Clear All Attendance?',
      'Are you sure you want to reset all marked laborers for today? All cards will revert to "Mark Present".',
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Clear All',
          style: 'destructive',
          onPress: () => {
            setRosterItems((prev) => {
              const updated = prev.map((item) => ({
                ...item,
                isCheckedIn: false,
                attendanceType: AttendanceType.NotWorked,
                workingHours: 0.0,
                calculatedAmount: 0.0,
                syncStatus: 'LOCAL_DRAFT' as const,
              }));
              setSummary(recalculateSummary(updated));
              return updated;
            });
            setHasOfflineDraftChanges(true);
          },
        },
      ]
    );
  };

  // 7. Remove Worker Action from within Confirmation Modals
  const handleRemoveWorkerFromModal = (workerId: string) => {
    setRosterItems((prev) => {
      const updated = prev.map((item) => {
        if (item.workerId !== workerId) return item;
        return {
          ...item,
          isCheckedIn: false,
          attendanceType: AttendanceType.NotWorked,
          workingHours: 0.0,
          calculatedAmount: 0.0,
          syncStatus: 'LOCAL_DRAFT' as const,
        };
      });
      setSummary(recalculateSummary(updated));
      return updated;
    });

    setHasOfflineDraftChanges(true);
  };

  // 8. Step 1: Confirm Save Draft (Offline)
  const handleConfirmSaveOfflineDraft = async () => {
    setStep1ModalVisible(false);
    setIsSavingOffline(true);
    try {
      const res = await saveOfflineDraft(selectedFarmId, attendanceDate, rosterItems);
      if (res.success) {
        setHasOfflineDraftChanges(false);
        await reloadRoster({ skipServerSync: true });
        Alert.alert(
          'Draft Saved Locally',
          `Successfully saved ${res.savedCount} marked laborer(s) to SQLite storage. You can continue updating or proceed to sync online.`
        );
      } else {
        Alert.alert('Save Failed', res.error || 'Failed to save offline draft to device.');
      }
    } finally {
      setIsSavingOffline(false);
    }
  };

  // 9. Step 2: Confirm Save Draft (Online)
  const handleConfirmSyncOnlineDraft = async () => {
    setStep2ModalVisible(false);
    setIsSyncingOnline(true);
    try {
      const res = await syncDraftOnline(selectedFarmId, attendanceDate, rosterItems);
      if (res.success) {
        setHasOfflineDraftChanges(false);
        await reloadRoster();
        Alert.alert(
          'Draft Synced Online',
          'Attendance draft has been synchronized with the server. Mobile attendance for this date is now locked. Further additions or removals prior to finalization can only be made on the Web Application.'
        );
      } else {
        Alert.alert(
          'Sync Failed',
          res.error || 'Unable to synchronize draft with server. Attendance remains saved locally on your device.'
        );
      }
    } finally {
      setIsSyncingOnline(false);
    }
  };

  // 10. Step 3: Confirm Finalize Attendance
  const handleConfirmFinalize = async () => {
    setStep3ModalVisible(false);
    setIsFinalizing(true);
    try {
      const res = await finalizeAttendance(selectedFarmId, attendanceDate);
      if (res.success) {
        await reloadRoster();
        Alert.alert(
          'Attendance Finalized',
          'Attendance has been permanently locked and financial worker earnings ledgers have been posted.'
        );
      } else {
        Alert.alert('Finalization Failed', res.error || 'Failed to finalize attendance on server.');
      }
    } finally {
      setIsFinalizing(false);
    }
  };

  // 11. Handler when a Quick Add worker is created
  const handleWorkerQuickAdded = (newWorker: RosterWorkerItem) => {
    setRosterItems((prev) => {
      const updated = [newWorker, ...prev];
      setSummary(recalculateSummary(updated));
      return updated;
    });
    setHasOfflineDraftChanges(true);
    updateOutboxCount();
    Alert.alert('Worker Added', `${newWorker.displayName} has been added to today's roster and marked Present.`);
  };

  // 12. Manual Outbox Sync Trigger
  const handleSyncOutbox = async () => {
    if (!network.isConnected) {
      Alert.alert('Offline Mode', 'Device is offline. Changes will synchronize automatically when connection is restored.');
      return;
    }

    setIsSyncingOutbox(true);
    try {
      const res = await processOutboxQueue();
      if (res.successCount > 0) {
        Alert.alert('Sync Complete', `Synchronized ${res.successCount} queued mutation(s) to server.`);
        await reloadRoster();
      } else if (res.errors.length > 0) {
        Alert.alert(
          'Sync Incomplete',
          res.errors.join('\n\n'),
          [
            { text: 'OK', style: 'cancel' },
            {
              text: 'Clear Stuck Outbox',
              style: 'destructive',
              onPress: () => {
                const cleared = clearFailedMutations();
                updateOutboxCount();
                Alert.alert('Outbox Reset', `Cleared ${cleared} stuck mutation(s).`);
              },
            },
          ]
        );
      } else {
        Alert.alert('All Synced', 'Outbox is clean. No pending mutations.');
      }
    } finally {
      setIsSyncingOutbox(false);
      updateOutboxCount();
    }
  };

  const activeFarm = farms.find((f) => f.id === selectedFarmId);

  // Fast filtered items (< 50ms search)
  const filteredRoster = useMemo(() => {
    if (!searchQuery.trim()) return rosterItems;
    const q = searchQuery.toLowerCase();
    return rosterItems.filter(
      (w) =>
        w.displayName.toLowerCase().includes(q) ||
        (w.firstName && w.firstName.toLowerCase().includes(q)) ||
        (w.lastName && w.lastName.toLowerCase().includes(q))
    );
  }, [rosterItems, searchQuery]);

  // Marked laborers for confirmation modals
  const markedLaborers = useMemo(() => {
    return rosterItems.filter((i) => i.isCheckedIn);
  }, [rosterItems]);

  return (
    <SafeAreaView style={styles.safeArea}>
      {/* Top Header */}
      <View style={styles.header}>
        <View style={styles.headerRow}>
          <TouchableOpacity
            style={styles.farmSelectorButton}
            onPress={() => setFarmPickerVisible(true)}
            disabled={isScreenLocked}
          >
            <Text style={styles.farmIcon}>🏡</Text>
            <Text style={styles.farmName} numberOfLines={1}>
              {activeFarm ? activeFarm.name : 'Select Farm'}
            </Text>
            <Text style={styles.dropdownArrow}>▼</Text>
          </TouchableOpacity>

          {/* Network Status Pill */}
          <View
            style={[
              styles.networkBadge,
              !network.isConnected && styles.networkBadgeOffline,
            ]}
          >
            <Text style={styles.networkBadgeText}>
              {!network.isConnected ? '📡 Offline' : '🟢 Online'}
            </Text>
          </View>
        </View>

        {/* Date Selector Row */}
        <View style={styles.dateRow}>
          <TouchableOpacity
            style={styles.dateNavButton}
            onPress={() => handleDateOffset(-1)}
          >
            <Text style={styles.dateNavText}>◀ Prev</Text>
          </TouchableOpacity>

          <View style={styles.dateDisplay}>
            <Text style={styles.dateText}>📅 {attendanceDate}</Text>
            {summary.isFinalized && (
              <View style={styles.finalizedBadge}>
                <Text style={styles.finalizedBadgeText}>🔒 FINALIZED</Text>
              </View>
            )}
            {summary.isLockedOnline && !summary.isFinalized && (
              <View style={styles.lockedOnlineBadge}>
                <Text style={styles.lockedOnlineBadgeText}>🔒 LOCKED ONLINE</Text>
              </View>
            )}
          </View>

          <TouchableOpacity
            style={styles.dateNavButton}
            onPress={() => handleDateOffset(1)}
          >
            <Text style={styles.dateNavText}>Next ▶</Text>
          </TouchableOpacity>
        </View>
      </View>

      {/* KPI Telemetry Strip (Assigned, Present, Est. Wages) */}
      <View style={styles.kpiContainer}>
        <View style={styles.kpiBox}>
          <Text style={styles.kpiValue}>{summary.totalAssigned}</Text>
          <Text style={styles.kpiLabel}>Assigned</Text>
        </View>
        <View style={[styles.kpiBox, styles.kpiBorderLeft]}>
          <Text style={[styles.kpiValue, styles.textGreen]}>{summary.presentCount}</Text>
          <Text style={styles.kpiLabel}>Present</Text>
        </View>
        <View style={[styles.kpiBox, styles.kpiBorderLeft]}>
          <Text style={[styles.kpiValue, styles.textBlue]}>
            {summary.estimatedEarnings > 0 ? `₹${summary.estimatedEarnings}` : '—'}
          </Text>
          <Text style={styles.kpiLabel}>Est. Wages</Text>
        </View>
      </View>

      {/* Screen Lock Warning Banners */}
      {summary.isFinalized ? (
        <View style={styles.finalizedBanner}>
          <Text style={styles.bannerIcon}>🔒</Text>
          <View style={styles.bannerTextCol}>
            <Text style={styles.finalizedBannerTitle}>Attendance Finalized & Locked</Text>
            <Text style={styles.finalizedBannerSubtitle}>
              Attendance records for this date have been permanently posted to Financial and Payment ledgers.
            </Text>
          </View>
        </View>
      ) : summary.isLockedOnline ? (
        <View style={styles.lockedBanner}>
          <Text style={styles.bannerIcon}>🔒</Text>
          <View style={styles.bannerTextCol}>
            <Text style={styles.lockedBannerTitle}>Draft Synced Online — Mobile Screen Locked</Text>
            <Text style={styles.lockedBannerSubtitle}>
              Further additions or removals prior to finalization can only be made via the Web Application.
            </Text>
          </View>
        </View>
      ) : null}

      {/* Controls Bar: Search + Clear All + Quick Add */}
      <View style={styles.controlsBar}>
        <View style={styles.searchContainer}>
          <TextInput
            style={styles.searchInput}
            placeholder="🔍 Search worker by name..."
            placeholderTextColor="#9CA3AF"
            value={searchQuery}
            onChangeText={setSearchQuery}
            autoCapitalize="none"
            clearButtonMode="while-editing"
          />
        </View>

        {!isScreenLocked && summary.presentCount > 0 && (
          <TouchableOpacity
            style={styles.clearAllButton}
            onPress={handleClearAllAttendance}
          >
            <Text style={styles.clearAllButtonText}>Clear All</Text>
          </TouchableOpacity>
        )}

        <TouchableOpacity
          style={[styles.quickAddButton, isScreenLocked && styles.buttonDisabled]}
          onPress={() => setQuickAddVisible(true)}
          disabled={isScreenLocked}
        >
          <Text style={styles.quickAddButtonText}>+ Quick Add</Text>
        </TouchableOpacity>
      </View>

      {/* Roster List View */}
      {isLoading ? (
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color="#2E7D32" />
          <Text style={styles.loadingText}>Loading attendance roster from SQLite...</Text>
        </View>
      ) : filteredRoster.length === 0 ? (
        <ScrollView
          contentContainerStyle={styles.emptyContainer}
          refreshControl={
            <RefreshControl
              refreshing={isRefreshing}
              onRefresh={handleRefresh}
              colors={['#2E7D32']}
              tintColor="#2E7D32"
            />
          }
        >
          <Text style={styles.emptyIcon}>👷</Text>
          <Text style={styles.emptyTitle}>No Workers Found</Text>
          <Text style={styles.emptySubtitle}>
            {searchQuery
              ? 'No workers match your search query.'
              : 'No workers assigned to this farm yet. Use Quick Add to register laborers.'}
          </Text>
          {!isScreenLocked && (
            <TouchableOpacity
              style={styles.emptyActionButton}
              onPress={() => setQuickAddVisible(true)}
            >
              <Text style={styles.emptyActionButtonText}>+ Quick Add Worker</Text>
            </TouchableOpacity>
          )}
        </ScrollView>
      ) : (
        <FlatList
          data={filteredRoster}
          keyExtractor={(item) => item.workerId}
          refreshControl={
            <RefreshControl
              refreshing={isRefreshing}
              onRefresh={handleRefresh}
              colors={['#2E7D32']}
              tintColor="#2E7D32"
            />
          }
          renderItem={({ item }) => {
            const isHourly = item.attendanceType === AttendanceType.Hourly;

            return (
              <View style={styles.workerCard}>
                {/* Worker Identity Row */}
                <View style={styles.workerHeaderRow}>
                  <View style={styles.workerIdentity}>
                    <Text style={styles.workerName}>{item.displayName}</Text>
                    <View style={styles.workerBadgesRow}>
                      <Text style={styles.genderBadge}>
                        {item.gender === 1 ? '👨 M' : item.gender === 2 ? '👩 F' : '🧑 O'}
                      </Text>

                      {/* Card Persistence Status Badges */}
                      {summary.isFinalized ? (
                        <View style={styles.badgeFinalized}>
                          <Text style={styles.badgeFinalizedText}>🔒 Finalized</Text>
                        </View>
                      ) : summary.isLockedOnline ? (
                        <View style={styles.badgeOnlineDraft}>
                          <Text style={styles.badgeOnlineDraftText}>🔒 Draft (Online)</Text>
                        </View>
                      ) : item.isCheckedIn && item.syncStatus === 'LOCAL_DRAFT' ? (
                        <View style={styles.badgeOfflineDraft}>
                          <Text style={styles.badgeOfflineDraftText}>Draft (Offline)</Text>
                        </View>
                      ) : null}

                      {item.isLocallyCreated && (
                        <View style={styles.locallyCreatedBadge}>
                          <Text style={styles.locallyCreatedBadgeText}>New Worker</Text>
                        </View>
                      )}
                    </View>
                  </View>

                  {/* Single Toggle Button: "Mark Present" ⇄ "✓ Present" */}
                  <TouchableOpacity
                    style={[
                      item.isCheckedIn ? styles.toggleBtnPresent : styles.toggleBtnUnmarked,
                      isScreenLocked && styles.buttonDisabled,
                    ]}
                    onPress={() => handleTogglePresent(item.workerId)}
                    disabled={isScreenLocked}
                    accessibilityRole="button"
                    accessibilityLabel={
                      item.isCheckedIn ? 'Worker Present, tap to unmark' : 'Tap to mark worker present'
                    }
                  >
                    <Text
                      style={[
                        item.isCheckedIn ? styles.toggleBtnTextPresent : styles.toggleBtnTextUnmarked,
                      ]}
                    >
                      {item.isCheckedIn ? '✓ Present' : 'Mark Present'}
                    </Text>
                  </TouchableOpacity>
                </View>

                {/* Expanded Details Section: Only shown when worker is Present */}
                {item.isCheckedIn && (
                  <View style={styles.cardDetailsSection}>
                    {/* Attendance Type Selector (Full Day | Half Day | Hourly) */}
                    <View style={styles.typeSelectorRow}>
                      <TouchableOpacity
                        style={[
                          styles.typeChip,
                          item.attendanceType === AttendanceType.FullDay && styles.typeChipFullDay,
                          isScreenLocked && styles.chipDisabled,
                        ]}
                        onPress={() => handleTypeChange(item.workerId, AttendanceType.FullDay)}
                        disabled={isScreenLocked}
                      >
                        <Text
                          style={[
                            styles.typeChipText,
                            item.attendanceType === AttendanceType.FullDay && styles.typeChipTextActive,
                          ]}
                        >
                          Full Day (8h)
                        </Text>
                      </TouchableOpacity>

                      <TouchableOpacity
                        style={[
                          styles.typeChip,
                          item.attendanceType === AttendanceType.HalfDay && styles.typeChipHalfDay,
                          isScreenLocked && styles.chipDisabled,
                        ]}
                        onPress={() => handleTypeChange(item.workerId, AttendanceType.HalfDay)}
                        disabled={isScreenLocked}
                      >
                        <Text
                          style={[
                            styles.typeChipText,
                            item.attendanceType === AttendanceType.HalfDay && styles.typeChipTextActive,
                          ]}
                        >
                          Half Day (4h)
                        </Text>
                      </TouchableOpacity>

                      <TouchableOpacity
                        style={[
                          styles.typeChip,
                          item.attendanceType === AttendanceType.Hourly && styles.typeChipHourly,
                          isScreenLocked && styles.chipDisabled,
                        ]}
                        onPress={() => handleTypeChange(item.workerId, AttendanceType.Hourly)}
                        disabled={isScreenLocked}
                      >
                        <Text
                          style={[
                            styles.typeChipText,
                            item.attendanceType === AttendanceType.Hourly && styles.typeChipTextActive,
                          ]}
                        >
                          Hourly
                        </Text>
                      </TouchableOpacity>
                    </View>

                    {/* Wage & Working Hours Row */}
                    <View style={styles.wageHoursRow}>
                      {isHourly ? (
                        /* Stepper Controls: Only visible for Hourly attendance */
                        <View style={styles.stepperContainer}>
                          <Text style={styles.hoursLabel}>Working Hours:</Text>
                          <TouchableOpacity
                            style={[
                              styles.stepButton,
                              (isScreenLocked || item.workingHours <= 0.5) && styles.stepButtonDisabled,
                            ]}
                            onPress={() => handleHoursStep(item.workerId, -0.5)}
                            disabled={isScreenLocked || item.workingHours <= 0.5}
                          >
                            <Text style={styles.stepButtonText}>−</Text>
                          </TouchableOpacity>

                          <View style={styles.hoursValueBox}>
                            <Text style={styles.hoursValueText}>{item.workingHours.toFixed(1)}h</Text>
                          </View>

                          <TouchableOpacity
                            style={[
                              styles.stepButton,
                              (isScreenLocked || item.workingHours >= 24) && styles.stepButtonDisabled,
                            ]}
                            onPress={() => handleHoursStep(item.workerId, 0.5)}
                            disabled={isScreenLocked || item.workingHours >= 24}
                          >
                            <Text style={styles.stepButtonText}>+</Text>
                          </TouchableOpacity>
                        </View>
                      ) : (
                        /* Standard Shift indicator for Full Day and Half Day */
                        <View style={styles.fixedShiftContainer}>
                          <Text style={styles.fixedShiftLabel}>
                            {item.attendanceType === AttendanceType.FullDay
                              ? 'Standard Shift: 8.0h'
                              : 'Half-Day Shift: 4.0h'}
                          </Text>
                        </View>
                      )}

                      {/* Calculated Wage Preview */}
                      <View style={styles.wagePreview}>
                        <Text style={styles.wageAmountText}>
                          ₹{item.calculatedAmount.toFixed(2)}
                        </Text>
                        {isHourly && item.calculatedRate > 0 && (
                          <Text style={styles.wageSubtext}>
                            @ ₹{item.calculatedRate.toFixed(2)}/hr
                          </Text>
                        )}
                      </View>
                    </View>
                  </View>
                )}
              </View>
            );
          }}
          contentContainerStyle={styles.listContent}
        />
      )}

      {/* Floating Sync Banner for Outbox Mutations */}
      {(pendingOutboxCount > 0 || !network.isConnected) && (
        <View style={styles.floatingSyncBannerContainer}>
          <TouchableOpacity
            style={[
              styles.floatingSyncBanner,
              !network.isConnected && styles.floatingSyncBannerOffline,
              isSyncingOutbox && styles.floatingSyncBannerSyncing,
            ]}
            onPress={handleSyncOutbox}
            disabled={isSyncingOutbox}
          >
            {isSyncingOutbox ? (
              <View style={styles.syncBannerContent}>
                <ActivityIndicator size="small" color="#FFFFFF" />
                <Text style={styles.floatingSyncBannerText}>
                  Syncing {pendingOutboxCount} mutation(s) to server...
                </Text>
              </View>
            ) : !network.isConnected ? (
              <View style={styles.syncBannerContent}>
                <Text style={styles.floatingSyncBannerText}>
                  📡 Device Offline — {pendingOutboxCount} change(s) stored locally
                </Text>
              </View>
            ) : (
              <View style={styles.syncBannerContent}>
                <Text style={styles.floatingSyncBannerText}>
                  🔄 {pendingOutboxCount} Change(s) Saved Locally — Tap to Sync Now
                </Text>
              </View>
            )}
          </TouchableOpacity>
        </View>
      )}

      {/* Bottom Sticky Action Bar: 3-Step State Engine */}
      <View style={styles.bottomActionBar}>
        {summary.isFinalized ? (
          /* State 3: Finalized (Permanently Locked) */
          <View style={styles.finalizedBarButton}>
            <Text style={styles.finalizedBarButtonText}>🔒 Finalized & Locked</Text>
          </View>
        ) : summary.isLockedOnline ? (
          /* State 2: Locked Online -> Ready for Step 3 Finalization */
          <TouchableOpacity
            style={[styles.finalizeButton, isFinalizing && styles.buttonDisabled]}
            onPress={() => setStep3ModalVisible(true)}
            disabled={isFinalizing}
          >
            {isFinalizing ? (
              <ActivityIndicator size="small" color="#FFFFFF" />
            ) : (
              <Text style={styles.finalizeButtonText}>🔒 Finalize Attendance</Text>
            )}
          </TouchableOpacity>
        ) : (
          /* State 1: Active Draft (Step 1 Offline + Step 2 Online Sync) */
          <View style={styles.stepOneTwoRow}>
            {/* Step 1: Save / Update Offline Draft */}
            <TouchableOpacity
              style={[
                styles.saveOfflineBtn,
                hasOfflineDraftChanges && styles.updateOfflineBtn,
                (summary.presentCount === 0 || isSavingOffline) && styles.buttonDisabled,
              ]}
              onPress={() => {
                if (summary.presentCount === 0) {
                  Alert.alert('No Workers Marked', 'Please mark at least one laborer present before saving draft.');
                  return;
                }
                setStep1ModalVisible(true);
              }}
              disabled={summary.presentCount === 0 || isSavingOffline}
            >
              {isSavingOffline ? (
                <ActivityIndicator size="small" color="#1B5E20" />
              ) : (
                <Text
                  style={[
                    styles.saveOfflineBtnText,
                    hasOfflineDraftChanges && styles.updateOfflineBtnText,
                  ]}
                >
                  {summary.hasLocalDraft
                    ? hasOfflineDraftChanges
                      ? '💾 Update Offline Draft'
                      : '✓ Offline Saved'
                    : '💾 Save Draft (Offline)'}
                </Text>
              )}
            </TouchableOpacity>

            {/* Step 2: Save Draft (Online) */}
            <TouchableOpacity
              style={[
                styles.syncOnlineBtn,
                (summary.presentCount === 0 || isSyncingOnline) && styles.buttonDisabled,
              ]}
              onPress={() => {
                if (summary.presentCount === 0) {
                  Alert.alert('No Workers Marked', 'Please mark at least one laborer present before syncing online.');
                  return;
                }
                if (hasOfflineDraftChanges) {
                  Alert.alert(
                    'Unsaved Local Changes',
                    'You have modified laborers since your last offline save. Please save the offline draft before syncing online.',
                    [
                      { text: 'Review Offline Draft', onPress: () => setStep1ModalVisible(true) },
                      { text: 'Cancel', style: 'cancel' },
                    ]
                  );
                  return;
                }
                setStep2ModalVisible(true);
              }}
              disabled={summary.presentCount === 0 || isSyncingOnline}
            >
              {isSyncingOnline ? (
                <ActivityIndicator size="small" color="#FFFFFF" />
              ) : (
                <Text style={styles.syncOnlineBtnText}>🌐 Save Draft (Online)</Text>
              )}
            </TouchableOpacity>
          </View>
        )}
      </View>

      {/* Farm Picker Dropdown Modal */}
      <Modal visible={farmPickerVisible} transparent animationType="fade">
        <View style={styles.modalOverlay}>
          <View style={styles.pickerCard}>
            <Text style={styles.pickerTitle}>Select Operational Farm</Text>
            {farms.map((f) => (
              <TouchableOpacity
                key={f.id}
                style={[styles.pickerItem, f.id === selectedFarmId && styles.pickerItemSelected]}
                onPress={() => handleSelectFarm(f.id)}
              >
                <Text style={[styles.pickerItemText, f.id === selectedFarmId && styles.pickerItemTextSelected]}>
                  {f.name}
                </Text>
              </TouchableOpacity>
            ))}
            <TouchableOpacity
              style={styles.pickerCloseButton}
              onPress={() => setFarmPickerVisible(false)}
            >
              <Text style={styles.pickerCloseButtonText}>Cancel</Text>
            </TouchableOpacity>
          </View>
        </View>
      </Modal>

      {/* Quick Add Worker Modal Sheet */}
      <QuickAddWorkerModal
        visible={quickAddVisible}
        farmId={selectedFarmId}
        wageRates={wageRates}
        onClose={() => setQuickAddVisible(false)}
        onWorkerAdded={handleWorkerQuickAdded}
      />

      {/* STEP 1 MODAL: Save Draft (Offline) Confirmation */}
      <Modal visible={step1ModalVisible} transparent animationType="slide">
        <View style={styles.modalOverlay}>
          <View style={styles.workflowModalContainer}>
            <View style={styles.workflowModalHeader}>
              <Text style={styles.workflowModalTitle}>Review Offline Draft</Text>
              <TouchableOpacity onPress={() => setStep1ModalVisible(false)}>
                <Text style={styles.workflowModalCloseText}>✕</Text>
              </TouchableOpacity>
            </View>

            <Text style={styles.workflowModalDescription}>
              Review laborers currently marked Present. Confirming will save these records locally to device SQLite storage.
            </Text>

            {/* Marked Laborers List */}
            <View style={styles.tableHeaderRow}>
              <Text style={[styles.tableColHeader, styles.colFlexName]}>Laborer Name</Text>
              <Text style={[styles.tableColHeader, styles.colFlexType]}>Attendance Type</Text>
              <Text style={[styles.tableColHeader, styles.colFlexAction]}>Action</Text>
            </View>

            <ScrollView style={styles.modalScrollArea}>
              {markedLaborers.length === 0 ? (
                <View style={styles.modalEmptyState}>
                  <Text style={styles.modalEmptyStateText}>No laborers currently marked present.</Text>
                </View>
              ) : (
                markedLaborers.map((worker) => (
                  <View key={worker.workerId} style={styles.tableDataRow}>
                    <View style={styles.colFlexName}>
                      <Text style={styles.tableLaborerName}>{worker.displayName}</Text>
                    </View>
                    <View style={styles.colFlexType}>
                      <View style={styles.tableTypeBadge}>
                        <Text style={styles.tableTypeBadgeText}>
                          {worker.attendanceType === AttendanceType.FullDay
                            ? 'Full Day (8h)'
                            : worker.attendanceType === AttendanceType.HalfDay
                            ? 'Half Day (4h)'
                            : `Hourly (${worker.workingHours}h)`}
                        </Text>
                      </View>
                    </View>
                    <View style={[styles.colFlexAction, styles.alignRight]}>
                      <TouchableOpacity
                        style={styles.tableRemoveButton}
                        onPress={() => handleRemoveWorkerFromModal(worker.workerId)}
                      >
                        <Text style={styles.tableRemoveButtonText}>Remove</Text>
                      </TouchableOpacity>
                    </View>
                  </View>
                ))
              )}
            </ScrollView>

            {/* Modal Footer Actions */}
            <View style={styles.workflowModalFooter}>
              <TouchableOpacity
                style={styles.modalCloseButton}
                onPress={() => setStep1ModalVisible(false)}
              >
                <Text style={styles.modalCloseButtonText}>Close</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={[styles.modalSubmitButton, markedLaborers.length === 0 && styles.buttonDisabled]}
                onPress={handleConfirmSaveOfflineDraft}
                disabled={markedLaborers.length === 0}
              >
                <Text style={styles.modalSubmitButtonText}>💾 Save Draft (Offline)</Text>
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>

      {/* STEP 2 MODAL: Save Draft (Online) Confirmation */}
      <Modal visible={step2ModalVisible} transparent animationType="slide">
        <View style={styles.modalOverlay}>
          <View style={styles.workflowModalContainer}>
            <View style={styles.workflowModalHeader}>
              <Text style={styles.workflowModalTitle}>Sync Draft to Server</Text>
              <TouchableOpacity onPress={() => setStep2ModalVisible(false)}>
                <Text style={styles.workflowModalCloseText}>✕</Text>
              </TouchableOpacity>
            </View>

            {/* Warning Banner */}
            <View style={styles.warningAlertBox}>
              <Text style={styles.warningAlertIcon}>⚠️</Text>
              <Text style={styles.warningAlertText}>
                Warning: Saving online will lock attendance in the mobile app. Further additions or removals can only be made via the Web Application prior to finalization.
              </Text>
            </View>

            {/* Marked Laborers List */}
            <View style={styles.tableHeaderRow}>
              <Text style={[styles.tableColHeader, styles.colFlexName]}>Laborer Name</Text>
              <Text style={[styles.tableColHeader, styles.colFlexType]}>Attendance Type</Text>
              <Text style={[styles.tableColHeader, styles.colFlexAction]}>Action</Text>
            </View>

            <ScrollView style={styles.modalScrollArea}>
              {markedLaborers.length === 0 ? (
                <View style={styles.modalEmptyState}>
                  <Text style={styles.modalEmptyStateText}>No laborers currently marked present.</Text>
                </View>
              ) : (
                markedLaborers.map((worker) => (
                  <View key={worker.workerId} style={styles.tableDataRow}>
                    <View style={styles.colFlexName}>
                      <Text style={styles.tableLaborerName}>{worker.displayName}</Text>
                    </View>
                    <View style={styles.colFlexType}>
                      <View style={styles.tableTypeBadge}>
                        <Text style={styles.tableTypeBadgeText}>
                          {worker.attendanceType === AttendanceType.FullDay
                            ? 'Full Day (8h)'
                            : worker.attendanceType === AttendanceType.HalfDay
                            ? 'Half Day (4h)'
                            : `Hourly (${worker.workingHours}h)`}
                        </Text>
                      </View>
                    </View>
                    <View style={[styles.colFlexAction, styles.alignRight]}>
                      <TouchableOpacity
                        style={styles.tableRemoveButton}
                        onPress={() => handleRemoveWorkerFromModal(worker.workerId)}
                      >
                        <Text style={styles.tableRemoveButtonText}>Remove</Text>
                      </TouchableOpacity>
                    </View>
                  </View>
                ))
              )}
            </ScrollView>

            {/* Modal Footer Actions */}
            <View style={styles.workflowModalFooter}>
              <TouchableOpacity
                style={styles.modalCloseButton}
                onPress={() => setStep2ModalVisible(false)}
              >
                <Text style={styles.modalCloseButtonText}>Close</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={[styles.modalSubmitButtonOnline, markedLaborers.length === 0 && styles.buttonDisabled]}
                onPress={handleConfirmSyncOnlineDraft}
                disabled={markedLaborers.length === 0}
              >
                <Text style={styles.modalSubmitButtonOnlineText}>🌐 Save Draft (Online)</Text>
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>

      {/* STEP 3 MODAL: Finalize Attendance Review */}
      <Modal visible={step3ModalVisible} transparent animationType="slide">
        <View style={styles.modalOverlay}>
          <View style={styles.workflowModalContainer}>
            <View style={styles.workflowModalHeader}>
              <Text style={styles.workflowModalTitle}>Finalize Attendance Roster</Text>
              <TouchableOpacity onPress={() => setStep3ModalVisible(false)}>
                <Text style={styles.workflowModalCloseText}>✕</Text>
              </TouchableOpacity>
            </View>

            {/* Critical Financial Entry Warning Banner */}
            <View style={styles.dangerAlertBox}>
              <Text style={styles.dangerAlertIcon}>⚠️</Text>
              <Text style={styles.dangerAlertText}>
                Finalizing attendance will lock all records and automatically generate Financial and Payment entries. This action cannot be undone.
              </Text>
            </View>

            {/* Read-Only Laborer List (NO Remove action) */}
            <View style={styles.tableHeaderRow}>
              <Text style={[styles.tableColHeader, styles.colFlexName]}>Laborer Name</Text>
              <Text style={[styles.tableColHeader, styles.colFlexType]}>Attendance Type</Text>
              <Text style={[styles.tableColHeader, styles.colFlexAction]}>Est. Wage</Text>
            </View>

            <ScrollView style={styles.modalScrollArea}>
              {markedLaborers.map((worker) => (
                <View key={worker.workerId} style={styles.tableDataRow}>
                  <View style={styles.colFlexName}>
                    <Text style={styles.tableLaborerName}>{worker.displayName}</Text>
                  </View>
                  <View style={styles.colFlexType}>
                    <View style={styles.tableTypeBadge}>
                      <Text style={styles.tableTypeBadgeText}>
                        {worker.attendanceType === AttendanceType.FullDay
                          ? 'Full Day (8h)'
                          : worker.attendanceType === AttendanceType.HalfDay
                          ? 'Half Day (4h)'
                          : `Hourly (${worker.workingHours}h)`}
                      </Text>
                    </View>
                  </View>
                  <View style={[styles.colFlexAction, styles.alignRight]}>
                    <Text style={styles.tableWageAmount}>
                      ₹{worker.calculatedAmount.toFixed(2)}
                    </Text>
                  </View>
                </View>
              ))}
            </ScrollView>

            {/* Summary Strip */}
            <View style={styles.finalizeSummaryBox}>
              <View style={styles.summaryItem}>
                <Text style={styles.summaryItemLabel}>Present Workers</Text>
                <Text style={styles.summaryItemValue}>{summary.presentCount}</Text>
              </View>
              <View style={styles.summaryItem}>
                <Text style={styles.summaryItemLabel}>Total Gross Wages</Text>
                <Text style={[styles.summaryItemValue, styles.textBlue]}>₹{summary.estimatedEarnings}</Text>
              </View>
            </View>

            {/* Modal Footer Actions */}
            <View style={styles.workflowModalFooter}>
              <TouchableOpacity
                style={styles.modalCloseButton}
                onPress={() => setStep3ModalVisible(false)}
              >
                <Text style={styles.modalCloseButtonText}>Close</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={styles.modalFinalizeSubmitButton}
                onPress={handleConfirmFinalize}
              >
                <Text style={styles.modalFinalizeSubmitButtonText}>🔒 Finalize</Text>
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: '#F3F4F6',
  },
  header: {
    backgroundColor: '#FFFFFF',
    paddingHorizontal: 16,
    paddingTop: Platform.OS === 'android' ? (StatusBar.currentHeight ? StatusBar.currentHeight + 8 : 28) : 12,
    paddingBottom: 10,
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
  },
  headerRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: 10,
  },
  farmSelectorButton: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#F3F4F6',
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderRadius: 8,
    marginRight: 10,
  },
  farmIcon: {
    fontSize: 16,
    marginRight: 6,
  },
  farmName: {
    flex: 1,
    fontSize: 14,
    fontWeight: '700',
    color: '#111827',
  },
  dropdownArrow: {
    fontSize: 10,
    color: '#6B7280',
    marginLeft: 4,
  },
  networkBadge: {
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 8,
    backgroundColor: '#2E7D32',
    alignItems: 'center',
    justifyContent: 'center',
  },
  networkBadgeOffline: {
    backgroundColor: '#D97706',
  },
  networkBadgeText: {
    fontSize: 11,
    fontWeight: '700',
    color: '#FFFFFF',
  },
  dateRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  dateNavButton: {
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 6,
    backgroundColor: '#F3F4F6',
  },
  dateNavText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#4B5563',
  },
  dateDisplay: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  dateText: {
    fontSize: 14,
    fontWeight: '700',
    color: '#111827',
  },
  finalizedBadge: {
    backgroundColor: '#FEE2E2',
    paddingHorizontal: 8,
    paddingVertical: 2,
    borderRadius: 4,
  },
  finalizedBadgeText: {
    fontSize: 11,
    fontWeight: '700',
    color: '#B91C1C',
  },
  lockedOnlineBadge: {
    backgroundColor: '#E0F2FE',
    paddingHorizontal: 8,
    paddingVertical: 2,
    borderRadius: 4,
  },
  lockedOnlineBadgeText: {
    fontSize: 11,
    fontWeight: '700',
    color: '#0369A1',
  },

  /* KPI Strip */
  kpiContainer: {
    flexDirection: 'row',
    backgroundColor: '#FFFFFF',
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
  },
  kpiBox: {
    flex: 1,
    alignItems: 'center',
  },
  kpiBorderLeft: {
    borderLeftWidth: 1,
    borderLeftColor: '#E5E7EB',
  },
  kpiValue: {
    fontSize: 18,
    fontWeight: '800',
    color: '#111827',
  },
  kpiLabel: {
    fontSize: 11,
    fontWeight: '600',
    color: '#6B7280',
    marginTop: 2,
  },
  textGreen: {
    color: '#2E7D32',
  },
  textBlue: {
    color: '#1D4ED8',
  },

  /* Banners */
  lockedBanner: {
    flexDirection: 'row',
    backgroundColor: '#FEF3C7',
    borderBottomWidth: 1,
    borderBottomColor: '#FCD34D',
    paddingHorizontal: 16,
    paddingVertical: 10,
    alignItems: 'center',
  },
  finalizedBanner: {
    flexDirection: 'row',
    backgroundColor: '#FEE2E2',
    borderBottomWidth: 1,
    borderBottomColor: '#FCA5A5',
    paddingHorizontal: 16,
    paddingVertical: 10,
    alignItems: 'center',
  },
  bannerIcon: {
    fontSize: 20,
    marginRight: 10,
  },
  bannerTextCol: {
    flex: 1,
  },
  lockedBannerTitle: {
    fontSize: 13,
    fontWeight: '700',
    color: '#92400E',
  },
  lockedBannerSubtitle: {
    fontSize: 11,
    color: '#B45309',
    marginTop: 2,
  },
  finalizedBannerTitle: {
    fontSize: 13,
    fontWeight: '700',
    color: '#991B1B',
  },
  finalizedBannerSubtitle: {
    fontSize: 11,
    color: '#B91C1C',
    marginTop: 2,
  },

  /* Controls Bar */
  controlsBar: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 10,
    backgroundColor: '#FFFFFF',
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
    gap: 10,
  },
  searchContainer: {
    flex: 1,
  },
  searchInput: {
    backgroundColor: '#F3F4F6',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 8,
    fontSize: 13,
    color: '#111827',
  },
  clearAllButton: {
    backgroundColor: '#FEE2E2',
    paddingHorizontal: 12,
    paddingVertical: 9,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
  },
  clearAllButtonText: {
    fontSize: 12,
    fontWeight: '700',
    color: '#B91C1C',
  },
  quickAddButton: {
    backgroundColor: '#2E7D32',
    paddingHorizontal: 12,
    paddingVertical: 9,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
  },
  quickAddButtonText: {
    fontSize: 12,
    fontWeight: '700',
    color: '#FFFFFF',
  },

  /* Worker Card */
  listContent: {
    padding: 16,
    paddingBottom: 110,
  },
  workerCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    padding: 14,
    marginBottom: 10,
    borderWidth: 1,
    borderColor: '#E5E7EB',
    shadowColor: '#000000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 2,
    elevation: 1,
  },
  workerHeaderRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  workerIdentity: {
    flex: 1,
    marginRight: 10,
  },
  workerName: {
    fontSize: 15,
    fontWeight: '700',
    color: '#111827',
  },
  workerBadgesRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginTop: 4,
    gap: 6,
    flexWrap: 'wrap',
  },
  genderBadge: {
    fontSize: 12,
    color: '#4B5563',
  },
  badgeOfflineDraft: {
    backgroundColor: '#E0F2FE',
    paddingHorizontal: 7,
    paddingVertical: 2,
    borderRadius: 4,
  },
  badgeOfflineDraftText: {
    fontSize: 11,
    fontWeight: '700',
    color: '#0369A1',
  },
  badgeOnlineDraft: {
    backgroundColor: '#DCFCE7',
    paddingHorizontal: 7,
    paddingVertical: 2,
    borderRadius: 4,
  },
  badgeOnlineDraftText: {
    fontSize: 11,
    fontWeight: '700',
    color: '#15803D',
  },
  badgeFinalized: {
    backgroundColor: '#FEE2E2',
    paddingHorizontal: 7,
    paddingVertical: 2,
    borderRadius: 4,
  },
  badgeFinalizedText: {
    fontSize: 11,
    fontWeight: '700',
    color: '#B91C1C',
  },
  locallyCreatedBadge: {
    backgroundColor: '#FEF3C7',
    paddingHorizontal: 7,
    paddingVertical: 2,
    borderRadius: 4,
  },
  locallyCreatedBadgeText: {
    fontSize: 11,
    fontWeight: '700',
    color: '#92400E',
  },

  /* Single Toggle Button */
  toggleBtnUnmarked: {
    borderWidth: 1,
    borderColor: '#D1D5DB',
    backgroundColor: '#F9FAFB',
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
    minWidth: 105,
  },
  toggleBtnTextUnmarked: {
    fontSize: 13,
    fontWeight: '600',
    color: '#374151',
  },
  toggleBtnPresent: {
    backgroundColor: '#2E7D32',
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
    minWidth: 105,
  },
  toggleBtnTextPresent: {
    fontSize: 13,
    fontWeight: '700',
    color: '#FFFFFF',
  },

  /* Expanded Card Details */
  cardDetailsSection: {
    marginTop: 12,
    paddingTop: 10,
    borderTopWidth: 1,
    borderTopColor: '#F3F4F6',
  },
  typeSelectorRow: {
    flexDirection: 'row',
    gap: 8,
    marginBottom: 10,
  },
  typeChip: {
    flex: 1,
    paddingVertical: 7,
    paddingHorizontal: 6,
    borderRadius: 6,
    backgroundColor: '#F3F4F6',
    alignItems: 'center',
    justifyContent: 'center',
  },
  typeChipFullDay: {
    backgroundColor: '#DCFCE7',
  },
  typeChipHalfDay: {
    backgroundColor: '#FEF3C7',
  },
  typeChipHourly: {
    backgroundColor: '#E0F2FE',
  },
  typeChipText: {
    fontSize: 11,
    fontWeight: '600',
    color: '#6B7280',
  },
  typeChipTextActive: {
    color: '#111827',
    fontWeight: '700',
  },
  chipDisabled: {
    opacity: 0.7,
  },

  /* Wage & Stepper Row */
  wageHoursRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  stepperContainer: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  hoursLabel: {
    fontSize: 12,
    fontWeight: '600',
    color: '#4B5563',
    marginRight: 8,
  },
  stepButton: {
    width: 28,
    height: 28,
    borderRadius: 6,
    backgroundColor: '#E5E7EB',
    alignItems: 'center',
    justifyContent: 'center',
  },
  stepButtonDisabled: {
    opacity: 0.4,
  },
  stepButtonText: {
    fontSize: 16,
    fontWeight: '700',
    color: '#111827',
    lineHeight: 18,
  },
  hoursValueBox: {
    paddingHorizontal: 8,
    minWidth: 44,
    alignItems: 'center',
  },
  hoursValueText: {
    fontSize: 13,
    fontWeight: '700',
    color: '#111827',
  },
  fixedShiftContainer: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  fixedShiftLabel: {
    fontSize: 12,
    fontWeight: '600',
    color: '#4B5563',
  },
  wagePreview: {
    alignItems: 'flex-end',
  },
  wageAmountText: {
    fontSize: 15,
    fontWeight: '800',
    color: '#2E7D32',
  },
  wageSubtext: {
    fontSize: 10,
    color: '#6B7280',
    marginTop: 1,
  },

  /* Loading & Empty States */
  loadingContainer: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 32,
  },
  loadingText: {
    marginTop: 12,
    fontSize: 13,
    color: '#6B7280',
  },
  emptyContainer: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 32,
  },
  emptyIcon: {
    fontSize: 48,
    marginBottom: 12,
  },
  emptyTitle: {
    fontSize: 16,
    fontWeight: '700',
    color: '#111827',
    marginBottom: 6,
  },
  emptySubtitle: {
    fontSize: 13,
    color: '#6B7280',
    textAlign: 'center',
    marginBottom: 16,
  },
  emptyActionButton: {
    backgroundColor: '#2E7D32',
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderRadius: 8,
  },
  emptyActionButtonText: {
    fontSize: 13,
    fontWeight: '700',
    color: '#FFFFFF',
  },

  /* Floating Sync Banner */
  floatingSyncBannerContainer: {
    position: 'absolute',
    bottom: 74,
    left: 16,
    right: 16,
    zIndex: 99,
  },
  floatingSyncBanner: {
    backgroundColor: '#1E3A8A',
    borderRadius: 10,
    paddingVertical: 10,
    paddingHorizontal: 14,
    shadowColor: '#000000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.15,
    shadowRadius: 4,
    elevation: 4,
  },
  floatingSyncBannerOffline: {
    backgroundColor: '#B45309',
  },
  floatingSyncBannerSyncing: {
    backgroundColor: '#047857',
  },
  syncBannerContent: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
  },
  floatingSyncBannerText: {
    fontSize: 12,
    fontWeight: '700',
    color: '#FFFFFF',
  },

  /* Bottom Action Bar */
  bottomActionBar: {
    position: 'absolute',
    bottom: 0,
    left: 0,
    right: 0,
    backgroundColor: '#FFFFFF',
    borderTopWidth: 1,
    borderTopColor: '#E5E7EB',
    paddingHorizontal: 16,
    paddingVertical: 12,
    shadowColor: '#000000',
    shadowOffset: { width: 0, height: -2 },
    shadowOpacity: 0.05,
    shadowRadius: 3,
    elevation: 3,
  },
  stepOneTwoRow: {
    flexDirection: 'row',
    gap: 12,
  },
  saveOfflineBtn: {
    flex: 1,
    backgroundColor: '#F3F4F6',
    borderWidth: 1,
    borderColor: '#D1D5DB',
    paddingVertical: 12,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
  },
  saveOfflineBtnText: {
    fontSize: 13,
    fontWeight: '700',
    color: '#374151',
  },
  updateOfflineBtn: {
    backgroundColor: '#FEF3C7',
    borderColor: '#F59E0B',
  },
  updateOfflineBtnText: {
    color: '#92400E',
  },
  syncOnlineBtn: {
    flex: 1,
    backgroundColor: '#2E7D32',
    paddingVertical: 12,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
  },
  syncOnlineBtnText: {
    fontSize: 13,
    fontWeight: '700',
    color: '#FFFFFF',
  },
  finalizeButton: {
    backgroundColor: '#1D4ED8',
    paddingVertical: 12,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
  },
  finalizeButtonText: {
    fontSize: 14,
    fontWeight: '700',
    color: '#FFFFFF',
  },
  finalizedBarButton: {
    backgroundColor: '#F3F4F6',
    borderWidth: 1,
    borderColor: '#E5E7EB',
    paddingVertical: 12,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
  },
  finalizedBarButtonText: {
    fontSize: 14,
    fontWeight: '700',
    color: '#9CA3AF',
  },
  buttonDisabled: {
    opacity: 0.5,
  },

  /* Modals Overlay */
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'flex-end',
  },
  pickerCard: {
    backgroundColor: '#FFFFFF',
    borderTopLeftRadius: 16,
    borderTopRightRadius: 16,
    padding: 20,
    maxHeight: '60%',
  },
  pickerTitle: {
    fontSize: 16,
    fontWeight: '700',
    color: '#111827',
    marginBottom: 16,
  },
  pickerItem: {
    paddingVertical: 12,
    paddingHorizontal: 8,
    borderRadius: 8,
  },
  pickerItemSelected: {
    backgroundColor: '#F3F4F6',
  },
  pickerItemText: {
    fontSize: 14,
    color: '#374151',
  },
  pickerItemTextSelected: {
    fontWeight: '700',
    color: '#2E7D32',
  },
  pickerCloseButton: {
    marginTop: 16,
    paddingVertical: 12,
    backgroundColor: '#F3F4F6',
    borderRadius: 8,
    alignItems: 'center',
  },
  pickerCloseButtonText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#4B5563',
  },

  /* Step 1, 2, 3 Workflow Modals */
  workflowModalContainer: {
    backgroundColor: '#FFFFFF',
    borderTopLeftRadius: 20,
    borderTopRightRadius: 20,
    padding: 20,
    maxHeight: '85%',
  },
  workflowModalHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: 10,
  },
  workflowModalTitle: {
    fontSize: 18,
    fontWeight: '800',
    color: '#111827',
  },
  workflowModalCloseText: {
    fontSize: 18,
    color: '#9CA3AF',
    padding: 4,
  },
  workflowModalDescription: {
    fontSize: 13,
    color: '#6B7280',
    marginBottom: 14,
    lineHeight: 18,
  },

  /* Warning and Danger Banners */
  warningAlertBox: {
    flexDirection: 'row',
    backgroundColor: '#FEF3C7',
    borderWidth: 1,
    borderColor: '#FCD34D',
    borderRadius: 8,
    padding: 12,
    marginBottom: 14,
    alignItems: 'flex-start',
  },
  warningAlertIcon: {
    fontSize: 18,
    marginRight: 8,
    marginTop: 1,
  },
  warningAlertText: {
    flex: 1,
    fontSize: 12,
    fontWeight: '600',
    color: '#92400E',
    lineHeight: 17,
  },
  dangerAlertBox: {
    flexDirection: 'row',
    backgroundColor: '#FEE2E2',
    borderWidth: 1,
    borderColor: '#FCA5A5',
    borderRadius: 8,
    padding: 12,
    marginBottom: 14,
    alignItems: 'flex-start',
  },
  dangerAlertIcon: {
    fontSize: 18,
    marginRight: 8,
    marginTop: 1,
  },
  dangerAlertText: {
    flex: 1,
    fontSize: 12,
    fontWeight: '600',
    color: '#991B1B',
    lineHeight: 17,
  },

  /* Table styles inside workflow modals */
  tableHeaderRow: {
    flexDirection: 'row',
    backgroundColor: '#F9FAFB',
    paddingVertical: 8,
    paddingHorizontal: 8,
    borderRadius: 6,
    marginBottom: 6,
  },
  tableColHeader: {
    fontSize: 11,
    fontWeight: '700',
    color: '#6B7280',
    textTransform: 'uppercase',
  },
  colFlexName: {
    flex: 1.5,
  },
  colFlexType: {
    flex: 1.2,
  },
  colFlexAction: {
    flex: 0.9,
  },
  alignRight: {
    alignItems: 'flex-end',
  },
  modalScrollArea: {
    maxHeight: 240,
    marginBottom: 16,
  },
  tableDataRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 10,
    paddingHorizontal: 8,
    borderBottomWidth: 1,
    borderBottomColor: '#F3F4F6',
  },
  tableLaborerName: {
    fontSize: 13,
    fontWeight: '600',
    color: '#111827',
  },
  tableTypeBadge: {
    backgroundColor: '#F3F4F6',
    paddingHorizontal: 6,
    paddingVertical: 3,
    borderRadius: 4,
    alignSelf: 'flex-start',
  },
  tableTypeBadgeText: {
    fontSize: 11,
    fontWeight: '600',
    color: '#374151',
  },
  tableRemoveButton: {
    backgroundColor: '#FEE2E2',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 4,
  },
  tableRemoveButtonText: {
    fontSize: 11,
    fontWeight: '700',
    color: '#B91C1C',
  },
  tableWageAmount: {
    fontSize: 13,
    fontWeight: '700',
    color: '#2E7D32',
  },
  modalEmptyState: {
    paddingVertical: 24,
    alignItems: 'center',
  },
  modalEmptyStateText: {
    fontSize: 13,
    color: '#9CA3AF',
  },

  /* Step 3 Summary Box */
  finalizeSummaryBox: {
    flexDirection: 'row',
    backgroundColor: '#F9FAFB',
    borderRadius: 8,
    padding: 12,
    marginBottom: 16,
    borderWidth: 1,
    borderColor: '#E5E7EB',
  },
  summaryItem: {
    flex: 1,
    alignItems: 'center',
  },
  summaryItemLabel: {
    fontSize: 11,
    color: '#6B7280',
    marginBottom: 4,
  },
  summaryItemValue: {
    fontSize: 16,
    fontWeight: '800',
    color: '#111827',
  },

  /* Workflow Modal Footer Actions */
  workflowModalFooter: {
    flexDirection: 'row',
    gap: 12,
  },
  modalCloseButton: {
    flex: 1,
    backgroundColor: '#F3F4F6',
    paddingVertical: 12,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
  },
  modalCloseButtonText: {
    fontSize: 13,
    fontWeight: '700',
    color: '#4B5563',
  },
  modalSubmitButton: {
    flex: 1.5,
    backgroundColor: '#2E7D32',
    paddingVertical: 12,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
  },
  modalSubmitButtonText: {
    fontSize: 13,
    fontWeight: '700',
    color: '#FFFFFF',
  },
  modalSubmitButtonOnline: {
    flex: 1.5,
    backgroundColor: '#2E7D32',
    paddingVertical: 12,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
  },
  modalSubmitButtonOnlineText: {
    fontSize: 13,
    fontWeight: '700',
    color: '#FFFFFF',
  },
  modalFinalizeSubmitButton: {
    flex: 1.5,
    backgroundColor: '#1D4ED8',
    paddingVertical: 12,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
  },
  modalFinalizeSubmitButtonText: {
    fontSize: 13,
    fontWeight: '700',
    color: '#FFFFFF',
  },
});
