import { Platform } from 'react-native';
import { create } from 'zustand';
import { TokenStorage, CachedUserProfile } from './tokenStorage';
import { apiClient, setApiClientBaseUrl } from '../api/client';
import { DEFAULT_API_BASE_URL } from '../config/env';
import { initializeDatabase } from '../db/database';

/**
 * Authentication and session state interface managed by Zustand.
 */
export interface AuthState {
  isLoading: boolean;
  isAuthenticated: boolean;
  isOfflineUnlocked: boolean;
  hasPin: boolean;
  user: CachedUserProfile | null;
  serverUrl: string;

  // Actions
  init: () => Promise<void>;
  login: (email: string, password: string, serverUrl?: string) => Promise<{ success: boolean; error?: string }>;
  logout: () => Promise<void>;
  setPin: (pin: string) => Promise<void>;
  unlockWithPin: (pin: string) => Promise<boolean>;
  setServerUrl: (url: string) => Promise<void>;
}

/**
 * Global authentication store providing state and actions for login,
 * offline PIN unlock, server host switching, and session management.
 */
export const useAuthStore = create<AuthState>((set, get) => ({
  isLoading: true,
  isAuthenticated: false,
  isOfflineUnlocked: false,
  hasPin: false,
  user: null,
  serverUrl: DEFAULT_API_BASE_URL || 'http://localhost:5108',

  /**
   * Initializes auth state on application startup.
   * Checks for stored credentials, verifies PIN configuration, and initializes SQLite.
   */
  init: async () => {
    try {
      // 1. Initialize local SQLite database
      initializeDatabase();

      // 2. Load custom server URL if configured
      let customUrl = await TokenStorage.getServerUrl();
      if (
        customUrl &&
        (customUrl.includes('10.0.2.2') || customUrl.includes('localhost') || customUrl.includes('127.0.0.1')) &&
        Platform.OS !== 'web' &&
        DEFAULT_API_BASE_URL.includes('192.168')
      ) {
        customUrl = null;
        await TokenStorage.saveServerUrl(DEFAULT_API_BASE_URL);
      }
      const activeUrl = customUrl || DEFAULT_API_BASE_URL || 'http://localhost:5108';
      setApiClientBaseUrl(activeUrl);

      // 3. Check for existing session and PIN configuration
      const accessToken = await TokenStorage.getAccessToken();
      const user = await TokenStorage.getUser();
      const hasPin = await TokenStorage.hasPin();

      set({
        isLoading: false,
        isAuthenticated: Boolean(accessToken && user),
        user,
        hasPin,
        serverUrl: activeUrl,
      });
    } catch (err) {
      set({ isLoading: false });
    }
  },

  /**
   * Submits login credentials to the ASP.NET Core backend.
   * Stores tokens and user profile securely upon success.
   */
  login: async (email: string, password: string, customUrl?: string) => {
    set({ isLoading: true });
    try {
      const activeUrl = customUrl ? customUrl.trim().replace(/\/+$/, '') : get().serverUrl;
      setApiClientBaseUrl(activeUrl);
      await TokenStorage.saveServerUrl(activeUrl);

      // 1. Submit login request to API
      const response = await apiClient.post<{
        accessToken: string;
        expiresIn: number;
        user: CachedUserProfile;
        refreshToken?: string;
      }>('/api/auth/login', { email, password });

      const { accessToken, refreshToken, user } = response.data;

      // 2. Securely store tokens and profile
      if (refreshToken) {
        await TokenStorage.saveTokens(accessToken, refreshToken);
      }
      await TokenStorage.saveUser(user);

      const hasPin = await TokenStorage.hasPin();

      set({
        isLoading: false,
        isAuthenticated: true,
        isOfflineUnlocked: true,
        user,
        hasPin,
        serverUrl: activeUrl,
      });

      return { success: true };
    } catch (err: any) {
      console.error('[AuthStore] Login failed:', err);
      const errorMsg =
        err.response?.data?.message ||
        err.response?.data?.detail ||
        (err.response?.status === 401 ? 'Invalid email or password.' : null) ||
        err.message ||
        'Unable to connect to server. Please check your network and server URL.';
      set({ isLoading: false });
      return { success: false, error: errorMsg };
    }
  },

  /**
   * Configures a new 4-digit PIN for offline field unlock.
   */
  setPin: async (pin: string) => {
    await TokenStorage.savePin(pin);
    set({ hasPin: true, isOfflineUnlocked: true });
  },

  /**
   * Verifies an entered PIN to unlock an offline field session.
   */
  unlockWithPin: async (pin: string) => {
    const isValid = await TokenStorage.verifyPin(pin);
    if (isValid) {
      set({ isOfflineUnlocked: true });
      return true;
    }
    return false;
  },

  /**
   * Updates and persists the backend API host URL.
   */
  setServerUrl: async (url: string) => {
    const cleaned = url.trim().replace(/\/+$/, '');
    setApiClientBaseUrl(cleaned);
    await TokenStorage.saveServerUrl(cleaned);
    set({ serverUrl: cleaned });
  },

  /**
   * Revokes the session and clears all stored tokens.
   */
  logout: async () => {
    try {
      const refreshToken = await TokenStorage.getRefreshToken();
      if (refreshToken) {
        // Attempt clean logout with backend if reachable
        await apiClient.post('/api/auth/logout', { refreshToken }).catch(() => {});
      }
    } finally {
      await TokenStorage.clearSession();
      set({
        isAuthenticated: false,
        isOfflineUnlocked: false,
        user: null,
      });
    }
  },
}));
