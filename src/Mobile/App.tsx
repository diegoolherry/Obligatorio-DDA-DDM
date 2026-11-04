import { useState } from 'react';
import { StatusBar } from 'expo-status-bar';
import { StyleSheet } from 'react-native';
import { SafeAreaProvider, SafeAreaView } from 'react-native-safe-area-context';
import { PassengerLogin } from './src/components/Authentication/PassengerLogin';
import { AuthenticationService } from './src/services/Authentication/AuthenticationService';
import type { IAuthenticationService } from './src/services/Authentication/IAuthenticationService';

export default function App() {
  const [authentication] = useState<IAuthenticationService>(
    () => new AuthenticationService(process.env.EXPO_PUBLIC_API_URL),
  );
  return (
    <SafeAreaProvider>
      <SafeAreaView style={styles.safeArea}>
        <PassengerLogin service={authentication} />
        <StatusBar style="dark" />
      </SafeAreaView>
    </SafeAreaProvider>
  );
}

const styles = StyleSheet.create({
  safeArea: { flex: 1, backgroundColor: '#fff' },
});
