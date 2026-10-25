import React from 'react';
import { Tabs } from 'expo-router';
import { Text, StyleSheet } from 'react-native';

/**
 * Tab Navigation Shell:
 * Configures the mobile operational hubs:
 * 1. Daily Attendance (attendance.tsx) — Mode A & B attendance roster, hours stepper, quick add.
 * 2. Master Data & Sync (index.tsx) — Real-time master table telemetry, farm selection, and delta sync.
 * 3. Diagnostics (diagnostics.tsx) — Step 1 test suite, WAL health, offline simulation, and auth validation.
 * 
 * Strict compliance: zero inline styles (all styles defined via StyleSheet).
 */
export default function TabLayout() {
  return (
    <Tabs
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: '#2E7D32',
        tabBarInactiveTintColor: '#6B7280',
        tabBarStyle: styles.tabBar,
      }}
    >
      <Tabs.Screen
        name="attendance"
        options={{
          title: 'Attendance',
          tabBarIcon: () => <Text style={styles.tabIcon}>📋</Text>,
        }}
      />
      <Tabs.Screen
        name="index"
        options={{
          title: 'Master Data',
          tabBarIcon: () => <Text style={styles.tabIcon}>🔄</Text>,
        }}
      />
      <Tabs.Screen
        name="diagnostics"
        options={{
          title: 'Diagnostics',
          tabBarIcon: () => <Text style={styles.tabIcon}>📊</Text>,
        }}
      />
    </Tabs>
  );
}

const styles = StyleSheet.create({
  tabBar: {
    backgroundColor: '#FFFFFF',
    borderTopColor: '#E5E7EB',
    height: 60,
    paddingBottom: 8,
    paddingTop: 8,
  },
  tabIcon: {
    fontSize: 20,
  },
});
