import { useEffect, useState } from 'react';
import NetInfo, { NetInfoState } from '@react-native-community/netinfo';

/**
 * Hook providing real-time network connectivity telemetry across the mobile app.
 * Automatically updates when the device transitions into or out of cellular/Wi-Fi coverage.
 */
export function useNetworkStatus() {
  const [networkState, setNetworkState] = useState<{
    isConnected: boolean;
    isInternetReachable: boolean | null;
    connectionType: string;
  }>({
    isConnected: true,
    isInternetReachable: true,
    connectionType: 'unknown',
  });

  useEffect(() => {
    // 1. Initial network check
    NetInfo.fetch().then((state: NetInfoState) => {
      setNetworkState({
        isConnected: Boolean(state.isConnected),
        isInternetReachable: state.isInternetReachable,
        connectionType: state.type,
      });
    });

    // 2. Subscribe to network state transitions (e.g. entering airplane mode)
    const unsubscribe = NetInfo.addEventListener((state: NetInfoState) => {
      setNetworkState({
        isConnected: Boolean(state.isConnected),
        isInternetReachable: state.isInternetReachable,
        connectionType: state.type,
      });
    });

    return () => unsubscribe();
  }, []);

  return networkState;
}
