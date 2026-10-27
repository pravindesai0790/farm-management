import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  ActivityIndicator,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  Image,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useAuthStore } from '../../src/core/auth/useAuthStore';
import { useNetworkStatus } from '../../src/core/network/useNetworkStatus';

/**
 * Mobile Login Screen:
 * Designed for outdoor sunlight readability with high-contrast surfaces and minimum 48px touch targets.
 * Features an expandable Server Configuration drawer to allow testing on emulators or local Wi-Fi.
 */
export default function LoginScreen() {
  const { login, serverUrl, setServerUrl } = useAuthStore();
  const { isConnected } = useNetworkStatus();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [targetServerUrl, setTargetServerUrl] = useState(serverUrl);
  const [showServerSettings, setShowServerSettings] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Synchronize targetServerUrl if auth store loads a configured server URL
  useEffect(() => {
    if (serverUrl) {
      setTargetServerUrl(serverUrl);
    }
  }, [serverUrl]);

  const applyPort = (port: string) => {
    let base = targetServerUrl.trim();
    if (base.includes(':')) {
      const parts = base.split(':');
      parts[parts.length - 1] = port;
      base = parts.join(':');
    } else {
      base = `${base}:${port}`;
    }
    setTargetServerUrl(base);
    setServerUrl(base);
  };

  const handleLogin = async () => {
    if (!email.trim() || !password) {
      setErrorMessage('Please enter both email and password.');
      return;
    }

    setErrorMessage(null);
    setIsSubmitting(true);

    try {
      console.log('[LoginScreen] Submitting login request for:', email.trim(), 'to server:', targetServerUrl);
      const result = await login(email.trim(), password, targetServerUrl);
      console.log('[LoginScreen] Login outcome:', result);
      if (!result.success) {
        setErrorMessage(result.error || 'Authentication failed. Please verify credentials.');
      }
    } catch (err: any) {
      console.error('[LoginScreen] Unhandled login exception:', err);
      setErrorMessage(err.message || 'An unexpected error occurred during login.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <SafeAreaView style={styles.safeArea}>
      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        style={styles.keyboardView}
      >
        <ScrollView contentContainerStyle={styles.scrollContent} keyboardShouldPersistTaps="handled">
          {/* Header & Brand */}
          <View style={styles.brandContainer}>
            <View style={styles.logoCard}>
              <Image
                source={require('../../assets/farmnxt-logo.png')}
                style={styles.brandLogo}
                resizeMode="contain"
              />
            </View>
            <Text style={styles.subtitle}>Field Operations & Workforce</Text>

            {/* Offline Alert if device has no connection */}
            {!isConnected && (
              <View style={styles.offlineBanner}>
                <Text style={styles.offlineText}>⚠️ No Internet Connection (Offline Mode)</Text>
              </View>
            )}
          </View>

          {/* Login Card */}
          <View style={styles.card}>
            <Text style={styles.cardHeader}>Sign In</Text>

            {/* Error Message */}
            {errorMessage && (
              <View style={styles.errorBox}>
                <Text style={styles.errorText}>{errorMessage}</Text>
              </View>
            )}

            {/* Email Field */}
            <View style={styles.inputGroup}>
              <Text style={styles.label}>Email Address</Text>
              <TextInput
                style={styles.input}
                value={email}
                onChangeText={setEmail}
                placeholder="user@farm.local"
                placeholderTextColor="#9CA3AF"
                autoCapitalize="none"
                keyboardType="email-address"
                autoCorrect={false}
                returnKeyType="next"
              />
            </View>

            {/* Password Field */}
            <View style={styles.inputGroup}>
              <Text style={styles.label}>Password</Text>
              <TextInput
                style={styles.input}
                value={password}
                onChangeText={setPassword}
                placeholder="Enter password"
                placeholderTextColor="#9CA3AF"
                secureTextEntry
                returnKeyType="go"
                onSubmitEditing={handleLogin}
              />
            </View>

            {/* Submit Button */}
            <TouchableOpacity
              style={[styles.loginButton, isSubmitting && styles.buttonDisabled]}
              onPress={handleLogin}
              disabled={isSubmitting}
              activeOpacity={0.8}
            >
              {isSubmitting ? (
                <ActivityIndicator color="#FFFFFF" />
              ) : (
                <Text style={styles.loginButtonText}>Sign In</Text>
              )}
            </TouchableOpacity>

            {/* Expandable Server Configuration */}
            <TouchableOpacity
              style={styles.serverToggle}
              onPress={() => setShowServerSettings(!showServerSettings)}
            >
              <Text style={styles.serverToggleText}>
                {showServerSettings ? '▲ Hide Server Settings' : `⚙️ Server: ${targetServerUrl || 'Configuring...'}`}
              </Text>
            </TouchableOpacity>

            {showServerSettings && (
              <View style={styles.serverSettingsBox}>
                <Text style={styles.serverLabel}>Quick Environment Selection:</Text>
                <View style={styles.quickPortRow}>
                  <TouchableOpacity
                    style={[styles.portChip, targetServerUrl.includes(':8080') && styles.portChipActive]}
                    onPress={() => applyPort('8080')}
                  >
                    <Text style={[styles.portChipText, targetServerUrl.includes(':8080') && styles.portChipTextActive]}>
                      🐳 Docker API (8080)
                    </Text>
                  </TouchableOpacity>
                  <TouchableOpacity
                    style={[styles.portChip, targetServerUrl.includes(':5108') && styles.portChipActive]}
                    onPress={() => applyPort('5108')}
                  >
                    <Text style={[styles.portChipText, targetServerUrl.includes(':5108') && styles.portChipTextActive]}>
                      ⚡ VS Code API (5108)
                    </Text>
                  </TouchableOpacity>
                </View>

                <Text style={[styles.serverLabel, { marginTop: 12 }]}>Backend API Host URL:</Text>
                <TextInput
                  style={styles.serverInput}
                  value={targetServerUrl}
                  onChangeText={(text) => {
                    setTargetServerUrl(text);
                    setServerUrl(text);
                  }}
                  placeholder="http://192.168.29.147:8080"
                  placeholderTextColor="#9CA3AF"
                  autoCapitalize="none"
                  autoCorrect={false}
                />
                <Text style={styles.serverHelp}>
                  • Docker API: http://192.168.29.147:8080{'\n'}
                  • VS Code dotnet run: http://192.168.29.147:5108
                </Text>
              </View>
            )}
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: '#F3F4F6',
  },
  keyboardView: {
    flex: 1,
  },
  scrollContent: {
    flexGrow: 1,
    justifyContent: 'center',
    padding: 20,
  },
  brandContainer: {
    alignItems: 'center',
    marginBottom: 24,
  },
  logoCard: {
    backgroundColor: '#FFFFFF',
    paddingVertical: 10,
    paddingHorizontal: 22,
    borderRadius: 14,
    marginBottom: 8,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.08,
    shadowRadius: 6,
    elevation: 3,
    borderWidth: 1,
    borderColor: '#E5E7EB',
  },
  brandLogo: {
    width: 220,
    height: 64,
  },
  subtitle: {
    fontSize: 14,
    color: '#4B5563',
    marginTop: 4,
    fontWeight: '500',
  },
  offlineBanner: {
    marginTop: 12,
    backgroundColor: '#FEF3C7',
    paddingVertical: 6,
    paddingHorizontal: 12,
    borderRadius: 6,
    borderWidth: 1,
    borderColor: '#FDE68A',
  },
  offlineText: {
    color: '#92400E',
    fontWeight: '600',
    fontSize: 12,
  },
  card: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    padding: 24,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.08,
    shadowRadius: 6,
    elevation: 3,
  },
  cardHeader: {
    fontSize: 20,
    fontWeight: '700',
    color: '#111827',
    marginBottom: 20,
  },
  errorBox: {
    backgroundColor: '#FEE2E2',
    borderWidth: 1,
    borderColor: '#FCA5A5',
    padding: 12,
    borderRadius: 8,
    marginBottom: 16,
  },
  errorText: {
    color: '#B91C1C',
    fontSize: 14,
    fontWeight: '500',
  },
  inputGroup: {
    marginBottom: 16,
  },
  label: {
    fontSize: 14,
    fontWeight: '600',
    color: '#374151',
    marginBottom: 6,
  },
  input: {
    height: 52, // Large minimum hit target for outdoor/gloved hands
    backgroundColor: '#F9FAFB',
    borderWidth: 1,
    borderColor: '#D1D5DB',
    borderRadius: 8,
    paddingHorizontal: 14,
    fontSize: 16,
    color: '#111827',
  },
  loginButton: {
    height: 52, // Large hit target
    backgroundColor: '#2E7D32',
    borderRadius: 8,
    justifyContent: 'center',
    alignItems: 'center',
    marginTop: 8,
  },
  buttonDisabled: {
    opacity: 0.6,
  },
  loginButtonText: {
    color: '#FFFFFF',
    fontSize: 17,
    fontWeight: '700',
  },
  serverToggle: {
    marginTop: 20,
    alignItems: 'center',
    padding: 8,
  },
  serverToggleText: {
    color: '#4B5563',
    fontSize: 13,
    fontWeight: '600',
  },
  serverSettingsBox: {
    marginTop: 10,
    padding: 14,
    backgroundColor: '#F9FAFB',
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#E5E7EB',
  },
  quickPortRow: {
    flexDirection: 'row',
    gap: 8,
    marginTop: 4,
  },
  portChip: {
    flex: 1,
    backgroundColor: '#E5E7EB',
    paddingVertical: 8,
    paddingHorizontal: 10,
    borderRadius: 6,
    alignItems: 'center',
  },
  portChipActive: {
    backgroundColor: '#2E7D32',
  },
  portChipText: {
    fontSize: 12,
    fontWeight: '700',
    color: '#374151',
  },
  portChipTextActive: {
    color: '#FFFFFF',
  },
  serverLabel: {
    fontSize: 13,
    fontWeight: '600',
    color: '#374151',
    marginBottom: 4,
  },
  serverInput: {
    height: 44,
    backgroundColor: '#FFFFFF',
    borderWidth: 1,
    borderColor: '#D1D5DB',
    borderRadius: 6,
    paddingHorizontal: 10,
    fontSize: 14,
    color: '#111827',
  },
  serverHelp: {
    marginTop: 8,
    fontSize: 11,
    color: '#6B7280',
    lineHeight: 16,
  },
});
