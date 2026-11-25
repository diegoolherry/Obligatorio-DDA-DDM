import '../../global.css';
import { Tabs } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { View } from 'react-native';
import { SafeAreaProvider, SafeAreaView } from 'react-native-safe-area-context';
import { AppHeader } from '../components/ui/AppHeader';
import { TabBar } from '../components/ui/TabBar';
import { AppServicesProvider } from '../context/AppServicesContext';

export default function RootLayout() {
  return (
    <SafeAreaProvider>
      <AppServicesProvider>
        <View className="flex-1 bg-surface-50">
          <AppHeader />
          {/* style, not className: NativeWind did not apply className to this third-party SafeAreaView on device (empty tabs). */}
          <SafeAreaView style={{ flex: 1 }} edges={['left', 'right', 'bottom']}>
            <Tabs tabBar={props => <TabBar {...props} />} screenOptions={{ headerShown: false, tabBarPosition: 'top' }}>
              <Tabs.Screen name="index" options={{ title: 'Buscar viaje' }} />
              <Tabs.Screen name="ingreso" options={{ title: 'Ingreso' }} />
              <Tabs.Screen name="tickets" options={{ title: 'Tickets' }} />
              {/* Mounted with the app so a validation in flight is not lost while the tab is hidden. */}
              <Tabs.Screen name="validar" options={{ title: 'Validar', lazy: false }} />
              {/* Mounted with the app and never unmounted, so the collector keeps sharing from any tab (foreground only). */}
              <Tabs.Screen name="compartir" options={{ title: 'Compartir ubicación', lazy: false }} />
              <Tabs.Screen name="ubicacion" options={{ title: 'Ubicación del servicio' }} />
            </Tabs>
          </SafeAreaView>
          <StatusBar style="light" />
        </View>
      </AppServicesProvider>
    </SafeAreaProvider>
  );
}
