import React, { useEffect } from 'react';
import { Stack, useRouter, useSegments } from 'expo-router';
import { View, ActivityIndicator, StyleSheet, Text, AppState, AppStateStatus } from 'react-native';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { StatusBar } from 'expo-status-bar';
import { useAuthStore } from '../src/core/auth/useAuthStore';
import * as SQLite from 'expo-sqlite';
import { useDrizzleStudio } from 'expo-drizzle-studio-plugin';

const db = SQLite.openDatabaseSync('farm_management_offline.db');

/**
 * Root Application Layout:
 * Initializes SQLite, checks encrypted tokens in SecureStore, and controls navigation routing
 * based on whether the user is online, offline, or needs to enter their 4-digit PIN.
 * Also monitors AppState transitions to automatically lock the session when backgrounded.
 */
export default function RootLayout() {
  const { isLoading, isAuthenticated, isOfflineUnlocked, hasPin, init } = useAuthStore();
  const segments = useSegments();
  const router = useRouter();

  // Active only in development builds / Expo Go
  useDrizzleStudio(db);

  useEffect(() => {
    // Initialize database and load stored tokens on application mount
    init();
  }, [init]);

  /**
   * AppState Lifecycle Monitor:
   * When a field supervisor minimizes the app or turns off the screen,
   * automatically marks the session as locked so returning to the app prompts for the 4-digit PIN.
   */
  useEffect(() => {
    const handleAppStateChange = (nextState: AppStateStatus) => {
      if (nextState.match(/inactive|background/)) {
        const { isAuthenticated, hasPin, lockSession } = useAuthStore.getState();
        if (isAuthenticated && hasPin) {
          lockSession();
        }
      }
    };

    const subscription = AppState.addEventListener('change', handleAppStateChange);
    return () => subscription.remove();
  }, []);

  /**
   * Centralized Authentication & Offline PIN Route Guard:
   * Evaluates session state and deterministically navigates between Login, PIN Setup, PIN Unlock, and Main Tabs.
   */
  useEffect(() => {
    if (isLoading) return;

    const inAuthGroup = (segments as string[])[0] === '(auth)';
    const currentScreen = (segments as string[])[1];

    if (!isAuthenticated) {
      // User is not signed in -> redirect to login if not already there
      if (!inAuthGroup || currentScreen !== 'login') {
        router.replace('/(auth)/login');
      }
    } else if (!hasPin) {
      // User is signed in but has not yet configured their offline PIN -> setup PIN
      if (currentScreen !== 'set-pin') {
        router.replace('/(auth)/set-pin');
      }
    } else if (!isOfflineUnlocked) {
      // User has an active local session but needs to unlock via PIN -> show PIN unlock pad
      if (currentScreen !== 'pin-unlock') {
        router.replace('/(auth)/pin-unlock');
      }
    } else if (inAuthGroup) {
      // Fully authenticated and unlocked -> navigate to main tabs
      router.replace('/(tabs)');
    }
  }, [isLoading, isAuthenticated, isOfflineUnlocked, hasPin, segments, router]);

  if (isLoading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#2E7D32" />
        <Text style={styles.loadingText}>Initializing FarmNxt...</Text>
      </View>
    );
  }

  return (
    <SafeAreaProvider>
      <StatusBar style="dark" />
      <Stack screenOptions={{ headerShown: false }}>
        <Stack.Screen name="(auth)" options={{ headerShown: false }} />
        <Stack.Screen name="(tabs)" options={{ headerShown: false }} />
      </Stack>
    </SafeAreaProvider>
  );
}

const styles = StyleSheet.create({
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#F8F9FA',
  },
  loadingText: {
    marginTop: 16,
    fontSize: 16,
    color: '#374151',
    fontWeight: '600',
  },
});
