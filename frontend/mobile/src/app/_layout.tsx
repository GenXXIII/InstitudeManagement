import { Stack } from 'expo-router';
import * as SplashScreen from 'expo-splash-screen';
import { useEffect } from 'react';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { StatusBar } from 'expo-status-bar';
import { AuthProvider, useAuth } from '@/features/auth/auth-context';

void SplashScreen.preventAutoHideAsync();

function AppNavigator() {
  const { ready, session } = useAuth();
  useEffect(() => { if (ready) void SplashScreen.hideAsync(); }, [ready]);
  if (!ready) return null;

  return <>
    <StatusBar style="dark"/>
    <Stack screenOptions={{ headerShown: false, animation: 'fade' }}>
      <Stack.Protected guard={!session}><Stack.Screen name="sign-in"/></Stack.Protected>
      <Stack.Protected guard={session?.role === 'teacher'}><Stack.Screen name="(teacher)"/></Stack.Protected>
      <Stack.Protected guard={session?.role === 'student'}><Stack.Screen name="(student)"/></Stack.Protected>
    </Stack>
  </>;
}

export default function RootLayout() {
  return <GestureHandlerRootView style={{ flex: 1 }}><SafeAreaProvider><AuthProvider><AppNavigator/></AuthProvider></SafeAreaProvider></GestureHandlerRootView>;
}
