import { Platform } from 'react-native';
import * as SecureStore from 'expo-secure-store';
import { STORAGE_KEYS } from '../config/env';

/**
 * Interface representing the authenticated user profile cached locally on device.
 */
export interface CachedUserProfile {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  organizationId: string;
  organizationName: string;
  roles: string[];
  permissions: string[];
}

/**
 * Safe storage driver:
 * On native mobile (iOS / Android), delegates to hardware-backed SecureStore (Keychain / Keystore).
 * On web browser preview, falls back to localStorage or memory store without throwing.
 */
const isWeb = Platform.OS === 'web';
const memoryStore = new Map<string, string>();

const safeStorage = {
  async getItem(key: string): Promise<string | null> {
    if (isWeb) {
      if (typeof window !== 'undefined' && window.localStorage) {
        return window.localStorage.getItem(key);
      }
      return memoryStore.get(key) ?? null;
    }
    try {
      return await SecureStore.getItemAsync(key);
    } catch {
      return null;
    }
  },

  async setItem(key: string, value: string): Promise<void> {
    if (isWeb) {
      if (typeof window !== 'undefined' && window.localStorage) {
        window.localStorage.setItem(key, value);
      } else {
        memoryStore.set(key, value);
      }
      return;
    }
    await SecureStore.setItemAsync(key, value);
  },

  async deleteItem(key: string): Promise<void> {
    if (isWeb) {
      if (typeof window !== 'undefined' && window.localStorage) {
        window.localStorage.removeItem(key);
      } else {
        memoryStore.delete(key);
      }
      return;
    }
    try {
      await SecureStore.deleteItemAsync(key);
    } catch {
      // Ignored if key does not exist
    }
  },
};

/**
 * Pure TypeScript implementation of SHA-256 for secure offline PIN hashing.
 * Operates reliably in all JS runtimes (Hermes, JSC, Web) without requiring native crypto modules.
 */
export function hashPin(pin: string): string {
  function rightRotate(value: number, amount: number) {
    return (value >>> amount) | (value << (32 - amount));
  }

  const mathPow = Math.pow;
  const maxWord = mathPow(2, 32);
  const asciiBitLength = pin.length * 8;

  const words: number[] = [];
  let hash: number[] = [];
  const k: number[] = [];
  let primeCounter = 0;

  const isComposite: Record<number, number> = {};
  for (let candidate = 2; primeCounter < 64; candidate++) {
    if (!isComposite[candidate]) {
      for (let step = 0; step < 313; step += candidate) {
        isComposite[step] = candidate;
      }
      hash[primeCounter] = (mathPow(candidate, 0.5) * maxWord) | 0;
      k[primeCounter++] = (mathPow(candidate, 1 / 3) * maxWord) | 0;
    }
  }

  hash = hash.slice(0, 8);

  for (let charIndex = 0; charIndex < pin.length; charIndex++) {
    const charCode = pin.charCodeAt(charIndex);
    words[charIndex >> 2] = (words[charIndex >> 2] || 0) | (charCode << (((3 - charIndex) % 4) * 8));
  }

  const pinLen = pin.length;
  words[pinLen >> 2] = (words[pinLen >> 2] || 0) | (0x80 << (((3 - pinLen) % 4) * 8));
  words[(((pinLen + 8) >> 6) << 4) + 15] = asciiBitLength;

  for (let blockIndex = 0; blockIndex < words.length; blockIndex += 16) {
    const w = words.slice(blockIndex, blockIndex + 16);
    for (let fillIdx = w.length; fillIdx < 16; fillIdx++) {
      w[fillIdx] = 0;
    }
    const oldHash = hash.slice(0);

    for (let round = 0; round < 64; round++) {
      const s0 = round < 16 ? w[round] : (w[round] = (
        ((w[round - 16] || 0) +
          (rightRotate(w[round - 15] || 0, 7) ^ rightRotate(w[round - 15] || 0, 18) ^ ((w[round - 15] || 0) >>> 3)) +
          (w[round - 7] || 0) +
          (rightRotate(w[round - 2] || 0, 17) ^ rightRotate(w[round - 2] || 0, 19) ^ ((w[round - 2] || 0) >>> 10))) | 0
      ));

      const ch = (hash[4] & hash[5]) ^ (~hash[4] & hash[6]);
      const temp1 = (hash[7] + (rightRotate(hash[4], 6) ^ rightRotate(hash[4], 11) ^ rightRotate(hash[4], 25)) + ch + k[round] + s0) | 0;
      const maj = (hash[0] & hash[1]) ^ (hash[0] & hash[2]) ^ (hash[1] & hash[2]);
      const temp2 = ((rightRotate(hash[0], 2) ^ rightRotate(hash[0], 13) ^ rightRotate(hash[0], 22)) + maj) | 0;

      hash = [(temp1 + temp2) | 0, hash[0], hash[1], hash[2], (hash[3] + temp1) | 0, hash[4], hash[5], hash[6]];
    }

    for (let hashIdx = 0; hashIdx < 8; hashIdx++) {
      hash[hashIdx] = (hash[hashIdx] + oldHash[hashIdx]) | 0;
    }
  }

  let result = '';
  for (let hashIdx = 0; hashIdx < 8; hashIdx++) {
    for (let byteShift = 3; byteShift >= 0; byteShift--) {
      const b = (hash[hashIdx] >> (8 * byteShift)) & 255;
      result += (b < 16 ? '0' : '') + b.toString(16);
    }
  }

  return result;
}

/**
 * Token and credential manager wrapping hardware-backed SecureStore.
 * Ensures access tokens, refresh tokens, and offline PIN hashes are safely encrypted on device.
 */
export const TokenStorage = {
  /**
   * Persists access and refresh tokens returned from authentication.
   */
  async saveTokens(accessToken: string, refreshToken: string): Promise<void> {
    await safeStorage.setItem(STORAGE_KEYS.ACCESS_TOKEN, accessToken);
    await safeStorage.setItem(STORAGE_KEYS.REFRESH_TOKEN, refreshToken);
  },

  /**
   * Retrieves the active access token for API requests.
   */
  async getAccessToken(): Promise<string | null> {
    return await safeStorage.getItem(STORAGE_KEYS.ACCESS_TOKEN);
  },

  /**
   * Retrieves the active refresh token for token rotation.
   */
  async getRefreshToken(): Promise<string | null> {
    return await safeStorage.getItem(STORAGE_KEYS.REFRESH_TOKEN);
  },

  /**
   * Saves user profile metadata.
   */
  async saveUser(user: CachedUserProfile): Promise<void> {
    await safeStorage.setItem(STORAGE_KEYS.USER_PROFILE, JSON.stringify(user));
  },

  /**
   * Retrieves the cached user profile.
   */
  async getUser(): Promise<CachedUserProfile | null> {
    const raw = await safeStorage.getItem(STORAGE_KEYS.USER_PROFILE);
    if (!raw) return null;
    try {
      return JSON.parse(raw);
    } catch {
      return null;
    }
  },

  /**
   * Hashes and stores the 4-digit PIN for offline field unlock.
   */
  async savePin(pin: string): Promise<void> {
    const hashed = hashPin(pin);
    await safeStorage.setItem(STORAGE_KEYS.OFFLINE_PIN_HASH, hashed);
  },

  /**
   * Checks whether the user has configured an offline PIN.
   */
  async hasPin(): Promise<boolean> {
    const hash = await safeStorage.getItem(STORAGE_KEYS.OFFLINE_PIN_HASH);
    return Boolean(hash);
  },

  /**
   * Verifies an entered PIN against the stored SHA-256 hash.
   */
  async verifyPin(enteredPin: string): Promise<boolean> {
    const storedHash = await safeStorage.getItem(STORAGE_KEYS.OFFLINE_PIN_HASH);
    if (!storedHash) return false;
    return hashPin(enteredPin) === storedHash;
  },

  /**
   * Gets custom server URL if overridden by user.
   */
  async getServerUrl(): Promise<string | null> {
    return await safeStorage.getItem(STORAGE_KEYS.CUSTOM_SERVER_URL);
  },

  /**
   * Sets custom server URL.
   */
  async saveServerUrl(url: string): Promise<void> {
    await safeStorage.setItem(STORAGE_KEYS.CUSTOM_SERVER_URL, url.trim());
  },

  /**
   * Clears tokens and session when logging out.
   */
  async clearSession(): Promise<void> {
    await safeStorage.deleteItem(STORAGE_KEYS.ACCESS_TOKEN);
    await safeStorage.deleteItem(STORAGE_KEYS.REFRESH_TOKEN);
    await safeStorage.deleteItem(STORAGE_KEYS.USER_PROFILE);
    await safeStorage.deleteItem(STORAGE_KEYS.OFFLINE_PIN_HASH);
  },
};
