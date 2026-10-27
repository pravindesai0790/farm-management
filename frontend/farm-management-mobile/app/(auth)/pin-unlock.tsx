import React, { useState } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  ActivityIndicator,
  Image,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useRouter } from 'expo-router';
import { useAuthStore } from '../../src/core/auth/useAuthStore';
import { useNetworkStatus } from '../../src/core/network/useNetworkStatus';

/**
 * Offline PIN Unlock Screen:
 * Presented when reopening the mobile application or waking from background.
 * Validates the entered 4-digit PIN against the encrypted SHA-256 hash in SecureStore,
 * enabling immediate field access without network connectivity.
 */
export default function PinUnlockScreen() {
  const router = useRouter();
  const { unlockWithPin, logout, user } = useAuthStore();
  const { isConnected } = useNetworkStatus();

  const [enteredPin, setEnteredPin] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isVerifying, setIsVerifying] = useState(false);

  /**
   * Handles numeric keypad tap:
   * Appends the digit, and upon reaching 4 digits, validates against the stored SHA-256 hash.
   * Uses safe try/catch/finally to guarantee that verification loading never stalls the UI.
   */
  const handleKeyPress = async (num: string) => {
    if (isVerifying || enteredPin.length >= 4) return;

    setErrorMessage(null);
    const next = enteredPin + num;
    setEnteredPin(next);

    if (next.length === 4) {
      setIsVerifying(true);
      setTimeout(async () => {
        try {
          const success = await unlockWithPin(next);
          if (success) {
            // Offline authentication verified: navigate to main tabs
            router.replace('/(tabs)');
          } else {
            setErrorMessage('Incorrect PIN. Please try again.');
            setEnteredPin('');
          }
        } catch (err: any) {
          console.error('[PinUnlockScreen] Unexpected offline validation exception:', err);
          setErrorMessage('Unable to verify PIN offline. Please try again.');
          setEnteredPin('');
        } finally {
          setIsVerifying(false);
        }
      }, 100);
    }
  };

  const handleDelete = () => {
    setErrorMessage(null);
    setEnteredPin((prev) => prev.slice(0, -1));
  };

  const handleSignOut = async () => {
    await logout();
    router.replace('/(auth)/login');
  };

  return (
    <SafeAreaView style={styles.container}>
      {/* Top Header */}
      <View style={styles.header}>
        <View style={styles.statusPill}>
          <Text style={styles.statusPillText}>
            {isConnected ? '🟢 Online' : '⚠️ Offline Mode'}
          </Text>
        </View>

        <View style={styles.logoCard}>
          <Image
            source={require('../../assets/farmnxt-logo.png')}
            style={styles.logoImg}
            resizeMode="contain"
          />
        </View>
        <Text style={styles.title}>Unlock FarmNxt</Text>
        <Text style={styles.userName}>{user?.firstName ? `${user.firstName} ${user.lastName || ''}` : 'Supervisor'}</Text>
        <Text style={styles.subtitle}>{user?.organizationName || 'Farm Operations'}</Text>
      </View>

      {/* 4 Digit Indicators */}
      <View style={styles.pinIndicatorContainer}>
        {[0, 1, 2, 3].map((index) => (
          <View
            key={index}
            style={[
              styles.pinDot,
              enteredPin.length > index && styles.pinDotFilled,
            ]}
          />
        ))}
      </View>

      {/* Error Message */}
      {errorMessage && (
        <View style={styles.errorBox}>
          <Text style={styles.errorText}>{errorMessage}</Text>
        </View>
      )}

      {isVerifying && (
        <View style={styles.verifyingBox}>
          <ActivityIndicator color="#2E7D32" />
          <Text style={styles.verifyingText}>Verifying PIN...</Text>
        </View>
      )}

      {/* Numeric Keypad */}
      <View style={styles.keypad}>
        {['1', '2', '3', '4', '5', '6', '7', '8', '9', '', '0', '⌫'].map((item, idx) => {
          if (item === '') {
            return <View key={idx} style={styles.keyEmpty} />;
          }
          if (item === '⌫') {
            return (
              <TouchableOpacity
                key={idx}
                style={[styles.key, styles.keyAction]}
                onPress={handleDelete}
                activeOpacity={0.7}
              >
                <Text style={styles.keyTextAction}>⌫</Text>
              </TouchableOpacity>
            );
          }
          return (
            <TouchableOpacity
              key={idx}
              style={styles.key}
              onPress={() => handleKeyPress(item)}
              activeOpacity={0.7}
            >
              <Text style={styles.keyText}>{item}</Text>
            </TouchableOpacity>
          );
        })}
      </View>

      {/* Sign Out Link */}
      <TouchableOpacity style={styles.signOutBtn} onPress={handleSignOut}>
        <Text style={styles.signOutText}>Switch Account or Sign Out</Text>
      </TouchableOpacity>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#F9FAFB',
    padding: 24,
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  header: {
    alignItems: 'center',
    marginTop: 10,
  },
  statusPill: {
    backgroundColor: '#E5E7EB',
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 12,
    marginBottom: 10,
  },
  statusPillText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#374151',
  },
  logoCard: {
    backgroundColor: '#FFFFFF',
    paddingVertical: 8,
    paddingHorizontal: 16,
    borderRadius: 12,
    marginBottom: 8,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.08,
    shadowRadius: 4,
    elevation: 2,
    borderWidth: 1,
    borderColor: '#E5E7EB',
  },
  logoImg: {
    width: 180,
    height: 52,
  },
  title: {
    fontSize: 22,
    fontWeight: '800',
    color: '#111827',
  },
  userName: {
    fontSize: 16,
    fontWeight: '700',
    color: '#2E7D32',
    marginTop: 4,
  },
  subtitle: {
    fontSize: 13,
    color: '#6B7280',
    marginTop: 2,
  },
  pinIndicatorContainer: {
    flexDirection: 'row',
    justifyContent: 'center',
    gap: 16,
    marginVertical: 16,
  },
  pinDot: {
    width: 20,
    height: 20,
    borderRadius: 10,
    borderWidth: 2,
    borderColor: '#9CA3AF',
    backgroundColor: 'transparent',
  },
  pinDotFilled: {
    backgroundColor: '#2E7D32',
    borderColor: '#2E7D32',
  },
  errorBox: {
    backgroundColor: '#FEE2E2',
    paddingVertical: 8,
    paddingHorizontal: 16,
    borderRadius: 8,
  },
  errorText: {
    color: '#B91C1C',
    fontSize: 14,
    fontWeight: '600',
  },
  verifyingBox: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  verifyingText: {
    color: '#2E7D32',
    fontSize: 14,
    fontWeight: '600',
  },
  keypad: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'center',
    gap: 16,
    maxWidth: 280,
  },
  key: {
    width: 76,
    height: 76,
    borderRadius: 38,
    backgroundColor: '#FFFFFF',
    justifyContent: 'center',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#E5E7EB',
    elevation: 2,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 3,
  },
  keyEmpty: {
    width: 76,
    height: 76,
  },
  keyAction: {
    backgroundColor: '#F3F4F6',
  },
  keyText: {
    fontSize: 26,
    fontWeight: '700',
    color: '#111827',
  },
  keyTextAction: {
    fontSize: 22,
    fontWeight: '700',
    color: '#374151',
  },
  signOutBtn: {
    padding: 12,
  },
  signOutText: {
    color: '#4B5563',
    fontSize: 13,
    fontWeight: '600',
  },
});
