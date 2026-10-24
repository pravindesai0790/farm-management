import React, { useEffect } from 'react';
import { Stack, useRouter, useSegments } from 'expo-router';
import { View, ActivityIndicator, StyleSheet, Text } from 'react-native';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { StatusBar } from 'expo-status-bar';
import { useAuthStore } from '../src/core/auth/useAuthStore';

/**
 * Root Application Layout:
 * Initializes SQLite, checks encrypted tokens in SecureStore, and controls navigation routing
 * based on whether the user is online, offline, or needs to enter their 4-digit PIN.
 */
export default function RootLayout() {
  const { isLoading, isAuthenticated, isOfflineUnlocked, hasPin, init } = useAuthStore();
  const segments = useSegments();
  const router = useRouter();

  useEffect(() => {
    // Initialize database and load stored tokens
    init();
  }, [init]);

  useEffect(() => {
    if (isLoading) return;

    const inAuthGroup = segments[0] === '(auth)';

    if (!isAuthenticated) {
      // User is not signed in -> redirect to login
      if (!inAuthGroup) {
        router.replace('/(auth)/login');
      }
    } else if (!hasPin) {
      // User is signed in but has not yet configured their offline PIN -> setup PIN
      router.replace('/(auth)/set-pin');
    } else if (!isOfflineUnlocked) {
      // User has a session but needs to unlock via PIN -> show PIN unlock pad
      router.replace('/(auth)/pin-unlock');
    } else if (inAuthGroup) {
      // Fully authenticated and unlocked -> navigate to main tabs
      router.replace('/(tabs)');
    }
  }, [isLoading, isAuthenticated, isOfflineUnlocked, hasPin, segments, router]);

  if (isLoading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#2E7D32" />
        <Text style={styles.loadingText}>Initializing Farm Management...</Text>
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
