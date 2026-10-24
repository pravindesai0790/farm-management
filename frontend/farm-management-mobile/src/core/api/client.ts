import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { DEFAULT_API_BASE_URL, MOBILE_CLIENT_HEADER } from '../config/env';
import { TokenStorage } from '../auth/tokenStorage';

/**
 * Creates and configures the centralized Axios HTTP client for the mobile app.
 * Automatically injects the Mobile platform header and active Bearer token,
 * and handles silent 401 token refresh.
 */
export const apiClient = axios.create({
  baseURL: DEFAULT_API_BASE_URL,
  timeout: 15000,
  headers: {
    'Content-Type': 'application/json',
    ...MOBILE_CLIENT_HEADER,
  },
});

/**
 * Updates the client's baseURL dynamically when the user switches host address.
 */
export function setApiClientBaseUrl(url: string) {
  apiClient.defaults.baseURL = url.replace(/\/+$/, '');
}

/**
 * Request Interceptor:
 * Retrieves the access token from SecureStore and injects it into the Authorization header.
 */
apiClient.interceptors.request.use(
  async (config: InternalAxiosRequestConfig) => {
    // Dynamically ensure latest server URL is applied
    const customUrl = await TokenStorage.getServerUrl();
    if (customUrl && config.baseURL !== customUrl) {
      config.baseURL = customUrl;
    }

    const token = await TokenStorage.getAccessToken();
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Flag to prevent multiple simultaneous refresh calls
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value?: unknown) => void;
  reject: (reason?: any) => void;
}> = [];

const processQueue = (error: any = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve();
    }
  });
  failedQueue = [];
};

/**
 * Response Interceptor:
 * Intercepts 401 Unauthorized responses to perform a silent refresh using the stored refresh token.
 */
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    // Do not intercept login or refresh requests - they handle their own errors
    if (
      originalRequest?.url?.includes('/api/auth/login') ||
      originalRequest?.url?.includes('/api/auth/refresh')
    ) {
      return Promise.reject(error);
    }

    // If 401 and not already retried
    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {

      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then(() => apiClient(originalRequest))
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        const storedRefreshToken = await TokenStorage.getRefreshToken();
        if (!storedRefreshToken) {
          throw new Error('No refresh token available');
        }

        // Call backend refresh endpoint with body token
        const refreshResponse = await axios.post<{ accessToken: string; expiresIn: number; refreshToken?: string }>(
          `${originalRequest.baseURL || DEFAULT_API_BASE_URL}/api/auth/refresh`,
          { refreshToken: storedRefreshToken },
          { headers: { ...MOBILE_CLIENT_HEADER } }
        );

        const newAccessToken = refreshResponse.data.accessToken;
        const newRefreshToken = refreshResponse.data.refreshToken || storedRefreshToken;

        // Persist rotated tokens
        await TokenStorage.saveTokens(newAccessToken, newRefreshToken);

        // Update Authorization header for original request
        originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;

        processQueue(null);
        return apiClient(originalRequest);
      } catch (refreshErr) {
        processQueue(refreshErr);
        // If refresh fails, clear session to force re-login
        await TokenStorage.clearSession();
        return Promise.reject(refreshErr);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);
