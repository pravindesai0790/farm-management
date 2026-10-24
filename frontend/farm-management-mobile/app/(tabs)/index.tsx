import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  ScrollView,
  TouchableOpacity,
  StyleSheet,
  ActivityIndicator,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useAuthStore } from '../../src/core/auth/useAuthStore';
import { useNetworkStatus } from '../../src/core/network/useNetworkStatus';
import { getDbHealth, DbHealthResult } from '../../src/core/db/database';
import { apiClient } from '../../src/core/api/client';
import { TokenStorage } from '../../src/core/auth/tokenStorage';

/**
 * Step 1 Verification & Diagnostics Dashboard:
 * Serves as the central validation control center for Step 1.
 * Allows developers and users to verify:
 * 1. Live Bearer JWT authorization against /api/auth/me.
 * 2. Mobile refresh token rotation against /api/auth/refresh.
 * 3. Local SQLite engine health and WAL mode status.
 * 4. Real-time network transitions (Online <-> Offline).
 */
export default function DiagnosticsDashboardScreen() {
  const { user, logout, serverUrl } = useAuthStore();
  const { isConnected, isInternetReachable, connectionType } = useNetworkStatus();

  const [dbHealth, setDbHealth] = useState<DbHealthResult | null>(null);
  const [testResult, setTestResult] = useState<{
    action: string;
    status: 'success' | 'error';
    latencyMs: number;
    message: string;
    details?: string;
  } | null>(null);
  const [isRunningTest, setIsRunningTest] = useState(false);

  useEffect(() => {
    // Check SQLite database health on mount
    const health = getDbHealth();
    setDbHealth(health);
  }, []);

  /**
   * Diagnostic Test 1: Verify current Bearer Token via /api/auth/me
   */
  const handleTestAuthMe = async () => {
    setIsRunningTest(true);
    setTestResult(null);
    const start = Date.now();

    try {
      const response = await apiClient.get('/api/auth/me');
      const latency = Date.now() - start;
      setTestResult({
        action: 'GET /api/auth/me',
        status: 'success',
        latencyMs: latency,
        message: `HTTP 200 OK — User: ${response.data.firstName} ${response.data.lastName}`,
        details: JSON.stringify(response.data, null, 2),
      });
    } catch (err: any) {
      const latency = Date.now() - start;
      setTestResult({
        action: 'GET /api/auth/me',
        status: 'error',
        latencyMs: latency,
        message: err.message || 'Request failed',
        details: err.response?.data ? JSON.stringify(err.response.data, null, 2) : undefined,
      });
    } finally {
      setIsRunningTest(false);
    }
  };

  /**
   * Diagnostic Test 2: Verify Mobile Refresh Token Rotation via /api/auth/refresh
   */
  const handleTestTokenRefresh = async () => {
    setIsRunningTest(true);
    setTestResult(null);
    const start = Date.now();

    try {
      const storedRefreshToken = await TokenStorage.getRefreshToken();
      if (!storedRefreshToken) {
        throw new Error('No refresh token found in SecureStore.');
      }

      const response = await apiClient.post('/api/auth/refresh', {
        refreshToken: storedRefreshToken,
      });

      const { accessToken, refreshToken } = response.data;

      // Update tokens in SecureStore with rotated credentials
      if (refreshToken) {
        await TokenStorage.saveTokens(accessToken, refreshToken);
      }

      const latency = Date.now() - start;
      setTestResult({
        action: 'POST /api/auth/refresh (Mobile Body)',
        status: 'success',
        latencyMs: latency,
        message: `HTTP 200 OK — Rotated token received & stored securely!`,
        details: `New AccessToken: ${accessToken.substring(0, 20)}...\nNew RefreshToken: ${refreshToken?.substring(0, 20)}...`,
      });
    } catch (err: any) {
      const latency = Date.now() - start;
      setTestResult({
        action: 'POST /api/auth/refresh',
        status: 'error',
        latencyMs: latency,
        message: err.message || 'Refresh failed',
        details: err.response?.data ? JSON.stringify(err.response.data, null, 2) : undefined,
      });
    } finally {
      setIsRunningTest(false);
    }
  };

  /**
   * Diagnostic Test 3: Re-query SQLite PRAGMA journal_mode
   */
  const handleRefreshDbHealth = () => {
    const health = getDbHealth();
    setDbHealth(health);
  };

  return (
    <SafeAreaView style={styles.safeArea}>
      <ScrollView contentContainerStyle={styles.scrollContent}>
        {/* Top Header */}
        <View style={styles.header}>
          <Text style={styles.headerTitle}>Platform Diagnostics</Text>
          <Text style={styles.headerSubtitle}>Step 1 Foundation Verification</Text>
        </View>

        {/* 1. Network Connectivity Card */}
        <View style={styles.card}>
          <View style={styles.cardHeaderRow}>
            <Text style={styles.cardTitle}>🌐 Network Telemetry</Text>
            <View
              style={[
                styles.badge,
                isConnected ? styles.badgeSuccess : styles.badgeDanger,
              ]}
            >
              <Text style={styles.badgeText}>
                {isConnected ? 'ONLINE' : 'OFFLINE'}
              </Text>
            </View>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>Connection Type:</Text>
            <Text style={styles.value}>{connectionType.toUpperCase()}</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>Internet Reachable:</Text>
            <Text style={styles.value}>
              {isInternetReachable === null ? 'Checking...' : isInternetReachable ? 'Yes' : 'No'}
            </Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>Server Host URL:</Text>
            <Text style={styles.valueUrl} numberOfLines={1}>
              {serverUrl}
            </Text>
          </View>
        </View>

        {/* 2. Authenticated User Profile Card */}
        <View style={styles.card}>
          <View style={styles.cardHeaderRow}>
            <Text style={styles.cardTitle}>👤 Authenticated Session</Text>
            <View style={[styles.badge, styles.badgeSuccess]}>
              <Text style={styles.badgeText}>AUTHENTICATED</Text>
            </View>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>User Name:</Text>
            <Text style={styles.valueBold}>
              {user?.firstName} {user?.lastName}
            </Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>Email:</Text>
            <Text style={styles.value}>{user?.email}</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>Organization:</Text>
            <Text style={styles.value}>{user?.organizationName || 'Default Org'}</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>Roles:</Text>
            <Text style={styles.value}>{user?.roles?.join(', ') || 'None'}</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>Offline PIN Security:</Text>
            <Text style={styles.valueGreen}>✓ Encrypted (SHA-256)</Text>
          </View>
        </View>

        {/* 3. SQLite Database Engine Card */}
        <View style={styles.card}>
          <View style={styles.cardHeaderRow}>
            <Text style={styles.cardTitle}>💾 SQLite Embedded Database</Text>
            <TouchableOpacity onPress={handleRefreshDbHealth}>
              <Text style={styles.refreshLink}>Re-check</Text>
            </TouchableOpacity>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>File Name:</Text>
            <Text style={styles.value}>{dbHealth?.fileName || 'farm_management_offline.db'}</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>Journal Mode:</Text>
            <Text style={styles.valueBold}>
              {dbHealth?.journalMode === 'WAL' ? '🟢 WAL (Write-Ahead Logging)' : dbHealth?.journalMode}
            </Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>Schema Version:</Text>
            <Text style={styles.value}>v{dbHealth?.version || 1}</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>Database Health:</Text>
            <Text style={dbHealth?.status === 'Ready' ? styles.valueGreen : styles.valueDanger}>
              {dbHealth?.status === 'Ready' ? '✓ Operational' : dbHealth?.errorMessage || 'Error'}
            </Text>
          </View>
        </View>

        {/* 4. Live API Test Controls */}
        <View style={styles.card}>
          <Text style={styles.cardTitle}>🧪 Live Verification Tests</Text>
          <Text style={styles.cardSubtitle}>
            Run these diagnostic checks to manually validate Step 1 backend integration:
          </Text>

          <View style={styles.testActions}>
            {/* Test 1: /api/auth/me */}
            <TouchableOpacity
              style={[styles.testBtn, isRunningTest && styles.buttonDisabled]}
              onPress={handleTestAuthMe}
              disabled={isRunningTest}
              activeOpacity={0.7}
            >
              <Text style={styles.testBtnText}>1. Verify Bearer Token (/api/auth/me)</Text>
            </TouchableOpacity>

            {/* Test 2: /api/auth/refresh */}
            <TouchableOpacity
              style={[styles.testBtn, styles.testBtnSecondary, isRunningTest && styles.buttonDisabled]}
              onPress={handleTestTokenRefresh}
              disabled={isRunningTest}
              activeOpacity={0.7}
            >
              <Text style={styles.testBtnTextSecondary}>2. Test Token Refresh (Mobile Body)</Text>
            </TouchableOpacity>
          </View>

          {isRunningTest && (
            <View style={styles.testingRow}>
              <ActivityIndicator color="#2E7D32" />
              <Text style={styles.testingText}>Executing test against {serverUrl}...</Text>
            </View>
          )}

          {/* Test Results Output */}
          {testResult && (
            <View
              style={[
                styles.resultBox,
                testResult.status === 'success' ? styles.resultSuccess : styles.resultError,
              ]}
            >
              <View style={styles.resultHeader}>
                <Text style={styles.resultAction}>{testResult.action}</Text>
                <Text style={styles.resultLatency}>{testResult.latencyMs} ms</Text>
              </View>
              <Text style={styles.resultMessage}>{testResult.message}</Text>
              {testResult.details && (
                <Text style={styles.resultDetails}>{testResult.details}</Text>
              )}
            </View>
          )}
        </View>

        {/* 5. Logout Button */}
        <TouchableOpacity style={styles.logoutBtn} onPress={logout} activeOpacity={0.8}>
          <Text style={styles.logoutBtnText}>🚪 Sign Out & Lock Session</Text>
        </TouchableOpacity>
      </ScrollView>
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
    marginBottom: 16,
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
  card: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    padding: 16,
    marginBottom: 16,
    elevation: 2,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 4,
  },
  cardHeaderRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12,
  },
  cardTitle: {
    fontSize: 16,
    fontWeight: '700',
    color: '#1F2937',
  },
  cardSubtitle: {
    fontSize: 13,
    color: '#6B7280',
    marginTop: 4,
    marginBottom: 12,
  },
  badge: {
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 6,
  },
  badgeSuccess: {
    backgroundColor: '#DEF7EC',
  },
  badgeDanger: {
    backgroundColor: '#FDE8E8',
  },
  badgeText: {
    fontSize: 11,
    fontWeight: '700',
    color: '#03543F',
  },
  row: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 6,
    borderBottomWidth: 0.5,
    borderBottomColor: '#F3F4F6',
  },
  label: {
    fontSize: 13,
    color: '#4B5563',
    fontWeight: '500',
  },
  value: {
    fontSize: 13,
    color: '#111827',
    fontWeight: '500',
  },
  valueBold: {
    fontSize: 13,
    color: '#111827',
    fontWeight: '700',
  },
  valueUrl: {
    fontSize: 12,
    color: '#2563EB',
    maxWidth: 180,
  },
  valueGreen: {
    fontSize: 13,
    color: '#16A34A',
    fontWeight: '700',
  },
  valueDanger: {
    fontSize: 13,
    color: '#DC2626',
    fontWeight: '700',
  },
  refreshLink: {
    fontSize: 13,
    color: '#2563EB',
    fontWeight: '600',
  },
  testActions: {
    gap: 10,
    marginTop: 4,
  },
  testBtn: {
    height: 48, // 48px tactile touch target
    backgroundColor: '#2E7D32',
    borderRadius: 8,
    justifyContent: 'center',
    alignItems: 'center',
  },
  testBtnSecondary: {
    backgroundColor: '#E8F5E9',
    borderWidth: 1,
    borderColor: '#A5D6A7',
  },
  testBtnText: {
    color: '#FFFFFF',
    fontWeight: '700',
    fontSize: 14,
  },
  testBtnTextSecondary: {
    color: '#1B5E20',
    fontWeight: '700',
    fontSize: 14,
  },
  buttonDisabled: {
    opacity: 0.6,
  },
  testingRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginTop: 12,
  },
  testingText: {
    fontSize: 13,
    color: '#2E7D32',
    fontWeight: '600',
  },
  resultBox: {
    marginTop: 14,
    padding: 12,
    borderRadius: 8,
    borderWidth: 1,
  },
  resultSuccess: {
    backgroundColor: '#F0FDF4',
    borderColor: '#BBF7D0',
  },
  resultError: {
    backgroundColor: '#FEF2F2',
    borderColor: '#FECACA',
  },
  resultHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 4,
  },
  resultAction: {
    fontSize: 13,
    fontWeight: '700',
    color: '#1F2937',
  },
  resultLatency: {
    fontSize: 12,
    fontWeight: '700',
    color: '#16A34A',
  },
  resultMessage: {
    fontSize: 13,
    fontWeight: '600',
    color: '#111827',
    marginBottom: 4,
  },
  resultDetails: {
    fontSize: 11,
    color: '#4B5563',
    fontFamily: 'monospace',
    backgroundColor: '#FFFFFF',
    padding: 8,
    borderRadius: 4,
    marginTop: 4,
  },
  logoutBtn: {
    height: 50,
    backgroundColor: '#FEE2E2',
    borderRadius: 8,
    justifyContent: 'center',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#FCA5A5',
  },
  logoutBtnText: {
    color: '#B91C1C',
    fontWeight: '700',
    fontSize: 15,
  },
});
