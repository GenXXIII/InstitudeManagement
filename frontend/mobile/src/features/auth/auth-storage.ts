import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';

const sessionKey = 'ink-mobile-session';

export async function readStoredSession() {
  if (Platform.OS === 'web') {
    return typeof window === 'undefined' ? null : window.localStorage.getItem(sessionKey);
  }

  return SecureStore.getItemAsync(sessionKey);
}

export async function writeStoredSession(value: string) {
  if (Platform.OS === 'web') {
    if (typeof window !== 'undefined') window.localStorage.setItem(sessionKey, value);
    return;
  }

  await SecureStore.setItemAsync(sessionKey, value);
}

export async function clearStoredSession() {
  if (Platform.OS === 'web') {
    if (typeof window !== 'undefined') window.localStorage.removeItem(sessionKey);
    return;
  }

  await SecureStore.deleteItemAsync(sessionKey);
}
