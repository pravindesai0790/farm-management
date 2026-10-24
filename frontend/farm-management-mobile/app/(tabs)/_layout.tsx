import React from 'react';
import { Tabs } from 'expo-router';
import { Text } from 'react-native';

/**
 * Tab Navigation Shell:
 * Houses the primary dashboard and diagnostics screen for Step 1.
 * Subsequent steps will add Attendance, Operations, Scouting, and Sync tabs.
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
          title: 'Diagnostics',
          tabBarIcon: ({ color }) => <Text style={{ fontSize: 20 }}>📊</Text>,
        }}
      />
    </Tabs>
  );
}
