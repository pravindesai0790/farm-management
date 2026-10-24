import { Platform } from 'react-native';
import Constants from 'expo-constants';

/**
 * Resolves the active development API host:
 * 1. If running in Expo Go (physical phone over Wi-Fi), extract the Metro host IP (e.g. 192.168.x.x)
 *    so the phone automatically connects to the workstation without manual configuration.
 * 2. If running on an Android Emulator, use the Android loopback bridge (10.0.2.2).
 * 3. Fallback to localhost for iOS Simulator and Web preview.
 */
function resolveDefaultApiBaseUrl(): string {
  // If Expo Go is connected to Metro, hostUri is "192.168.x.x:8081"
  const hostUri = Constants.expoConfig?.hostUri;
  if (hostUri) {
    const hostIp = hostUri.split(':')[0];
    if (hostIp && hostIp !== 'localhost' && hostIp !== '127.0.0.1') {
      return `http://${hostIp}:5108`;
    }
  }

  return Platform.select({
    android: 'http://10.0.2.2:5108',
    default: 'http://localhost:5108',
  }) as string;
}

export const DEFAULT_API_BASE_URL = resolveDefaultApiBaseUrl();

/**
 * Storage key constants used throughout the mobile application for SecureStore keys.
 */
export const STORAGE_KEYS = {
  ACCESS_TOKEN: 'fm_access_token',
  REFRESH_TOKEN: 'fm_refresh_token',
  USER_PROFILE: 'fm_user_profile',
  OFFLINE_PIN_HASH: 'fm_offline_pin_hash',
  CUSTOM_SERVER_URL: 'fm_custom_server_url',
} as const;

/**
 * HTTP Header used by native mobile applications to signal the ASP.NET Core backend
 * to deliver tokens directly in JSON payloads rather than setting browser HttpOnly cookies.
 */
export const MOBILE_CLIENT_HEADER = {
  'X-Client-Platform': 'Mobile',
} as const;
