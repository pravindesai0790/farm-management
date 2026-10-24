import React, { useState } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  ActivityIndicator,
  Alert,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useRouter } from 'expo-router';
import { useAuthStore } from '../../src/core/auth/useAuthStore';

/**
 * Offline PIN Setup Screen:
 * Allows supervisors to configure a 4-digit numeric PIN on first login.
 * This PIN enables instantaneous field unlock when offline without needing cellular reception.
 */
export default function SetPinScreen() {
  const router = useRouter();
  const { setPin, user } = useAuthStore();

  const [pin, setLocalPin] = useState('');
  const [confirmPin, setConfirmPin] = useState('');
  const [step, setStep] = useState<'enter' | 'confirm'>('enter');
  const [isSaving, setIsSaving] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const handleKeyPress = (num: string) => {
    setErrorMsg(null);
    if (step === 'enter') {
      if (pin.length < 4) {
        const next = pin + num;
        setLocalPin(next);
        if (next.length === 4) {
          setTimeout(() => setStep('confirm'), 200);
        }
      }
    } else {
      if (confirmPin.length < 4) {
        const next = confirmPin + num;
        setConfirmPin(next);
        if (next.length === 4) {
          handleSavePin(next);
        }
      }
    }
  };

  const handleDelete = () => {
    setErrorMsg(null);
    if (step === 'enter') {
      setLocalPin((prev) => prev.slice(0, -1));
    } else {
      setConfirmPin((prev) => prev.slice(0, -1));
    }
  };

  const handleSavePin = async (finalConfirm: string) => {
    if (pin !== finalConfirm) {
      setErrorMsg('PINs do not match. Please try again.');
      setConfirmPin('');
      setStep('enter');
      setLocalPin('');
      return;
    }

    setIsSaving(true);
    try {
      await setPin(pin);
      router.replace('/(tabs)');
    } catch {
      setErrorMsg('Failed to save PIN. Please try again.');
    } finally {
      setIsSaving(false);
    }
  };

  const currentDisplayPin = step === 'enter' ? pin : confirmPin;

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.icon}>🔒</Text>
        <Text style={styles.title}>
          {step === 'enter' ? 'Set Offline Field PIN' : 'Confirm Your 4-Digit PIN'}
        </Text>
        <Text style={styles.subtitle}>
          Welcome, {user?.firstName || 'Supervisor'}! Create a 4-digit PIN for quick field access when offline.
        </Text>
      </View>

      {/* 4 Digit Indicators */}
      <View style={styles.pinIndicatorContainer}>
        {[0, 1, 2, 3].map((index) => (
          <View
            key={index}
            style={[
              styles.pinDot,
              currentDisplayPin.length > index && styles.pinDotFilled,
            ]}
          />
        ))}
      </View>

      {errorMsg && (
        <View style={styles.errorBox}>
          <Text style={styles.errorText}>{errorMsg}</Text>
        </View>
      )}

      {isSaving && (
        <View style={styles.savingBox}>
          <ActivityIndicator color="#2E7D32" />
          <Text style={styles.savingText}>Securing offline PIN...</Text>
        </View>
      )}

      {/* Large Touch Numeric Pad */}
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
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#F9FAFB',
    padding: 24,
    justifyContent: 'space-between',
  },
  header: {
    alignItems: 'center',
    marginTop: 20,
  },
  icon: {
    fontSize: 48,
    marginBottom: 12,
  },
  title: {
    fontSize: 22,
    fontWeight: '800',
    color: '#111827',
    textAlign: 'center',
  },
  subtitle: {
    fontSize: 14,
    color: '#4B5563',
    textAlign: 'center',
    marginTop: 8,
    paddingHorizontal: 20,
    lineHeight: 20,
  },
  pinIndicatorContainer: {
    flexDirection: 'row',
    justifyContent: 'center',
    gap: 16,
    marginVertical: 20,
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
    padding: 10,
    borderRadius: 8,
    marginHorizontal: 20,
    alignItems: 'center',
  },
  errorText: {
    color: '#B91C1C',
    fontSize: 14,
    fontWeight: '600',
  },
  savingBox: {
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    gap: 10,
  },
  savingText: {
    color: '#2E7D32',
    fontSize: 14,
    fontWeight: '600',
  },
  keypad: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'center',
    gap: 16,
    marginBottom: 30,
  },
  key: {
    width: 76,
    height: 76, // Large tactile hit target
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
});
