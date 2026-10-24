import { Stack } from 'expo-router';

/**
 * Auth Stack Layout:
 * Houses Login, PIN Setup, and PIN Unlock screens.
 */
export default function AuthLayout() {
  return (
    <Stack screenOptions={{ headerShown: false }}>
      <Stack.Screen name="login" />
      <Stack.Screen name="set-pin" />
      <Stack.Screen name="pin-unlock" />
    </Stack>
  );
}
