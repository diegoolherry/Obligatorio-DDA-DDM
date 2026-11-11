import { useCallback, useEffect } from 'react';
import { AppState, Linking } from 'react-native';
import { useForegroundPermissions } from 'expo-location';

/**
 * checking: the permission object is still null (loading branch).
 * askable: not granted yet and the system dialog can still be shown.
 * blocked: not granted and the system will not ask again; only Ajustes can change it.
 */
export type LocationPermissionState = 'checking' | 'granted' | 'askable' | 'blocked';

export interface ForegroundLocationPermission {
  state: LocationPermissionState;
  /** Shows the system dialog. Call it only from a user tap, never when the app opens. */
  request: () => Promise<void>;
  /** Reads the current permission without asking; true when it is granted. */
  recheck: () => Promise<boolean>;
  openSettings: () => void;
}

// Foreground ("When In Use") location only: there is no background permission here.
export function useForegroundLocationPermission(): ForegroundLocationPermission {
  const [permission, requestPermission, refreshPermission] = useForegroundPermissions();

  // The user may change the permission in Ajustes; read it again on return to the app.
  useEffect(() => {
    const subscription = AppState.addEventListener('change', next => {
      if (next === 'active') refreshPermission().catch(() => undefined);
    });
    return () => subscription.remove();
  }, [refreshPermission]);

  const request = useCallback(async () => {
    try { await requestPermission(); } catch { /* the state stays as it was; the screen keeps its explanation */ }
  }, [requestPermission]);

  const recheck = useCallback(async () => {
    try { return (await refreshPermission()).granted; } catch { return false; }
  }, [refreshPermission]);

  const openSettings = useCallback(() => {
    Linking.openSettings().catch(() => undefined);
  }, []);

  const state: LocationPermissionState = permission === null ? 'checking'
    : permission.granted ? 'granted'
      : permission.canAskAgain ? 'askable' : 'blocked';
  return { state, request, recheck, openSettings };
}
