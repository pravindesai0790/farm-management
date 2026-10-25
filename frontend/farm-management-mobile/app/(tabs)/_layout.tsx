import React from 'react';
import { Tabs } from 'expo-router';
import { Text } from 'react-native';

/**
 * Tab Navigation Shell:
 * Configures the two primary operational hubs for Step 2:
 * 1. Master Data & Sync (index.tsx) — Real-time table telemetry, farm selection, and delta sync.
 * 2. Diagnostics (diagnostics.tsx) — Step 1 test suite, WAL health, offline simulation, and auth validation.
 */
export default function TabLayout() {
  return (
    <Tabs
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: '#2E7D32',
        tabBarInactiveTintColor: '#6B7280',
        tabBarStyle: {
          backgroundColor: '#FFFFFF',
          borderTopColor: '#E5E7EB',
          height: 60,
          paddingBottom: 8,
          paddingTop: 8,
        },
      }}
    >
      <Tabs.Screen
        name="index"
        options={{
          title: 'Master Data',
          tabBarIcon: () => <Text style={{ fontSize: 20 }}>🔄</Text>,
        }}
      />
      <Tabs.Screen
        name="diagnostics"
        options={{
          title: 'Diagnostics',
          tabBarIcon: () => <Text style={{ fontSize: 20 }}>📊</Text>,
        }}
      />
    </Tabs>
  );
}
