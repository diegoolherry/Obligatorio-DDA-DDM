import './global.css';
import { useEffect, useState } from 'react';
import { StatusBar } from 'expo-status-bar';
import { Pressable, Text, View } from 'react-native';
import { SafeAreaProvider, SafeAreaView } from 'react-native-safe-area-context';
import { AppHeader } from './src/components/ui/AppHeader';
import { PassengerLogin } from './src/components/Authentication/PassengerLogin';
import { ItinerarySearch } from './src/components/Itinerary/ItinerarySearch';
import { LocationSharing } from './src/components/Location/LocationSharing';
import { ServiceLocation } from './src/components/Location/ServiceLocation';
import { AuthenticationService } from './src/services/Authentication/AuthenticationService';
import type { IAuthenticationService } from './src/services/Authentication/IAuthenticationService';
import { ItinerarySearchService } from './src/services/Itinerary/ItinerarySearchService';
import type { IItinerarySearchService } from './src/services/Itinerary/IItinerarySearchService';
import type { ILocationService } from './src/services/Location/ILocationService';
import { LocationService } from './src/services/Location/LocationService';

type Screen = 'buscar' | 'ingreso' | 'compartir' | 'ubicacion';

export default function App() {
  const [authentication] = useState<IAuthenticationService>(
    () => new AuthenticationService(process.env.EXPO_PUBLIC_API_URL),
  );
  const [itineraries] = useState<IItinerarySearchService>(
    () => new ItinerarySearchService(process.env.EXPO_PUBLIC_API_URL),
  );
  // The location service asks the session to send its requests: the token stays inside it.
  const [location] = useState<ILocationService>(() => new LocationService(authentication));
  const [screen, setScreen] = useState<Screen>('buscar');
  // The in-memory session outlives tab switches and is discarded only when the app root unmounts.
  useEffect(() => () => authentication.discard(), [authentication]);
  return (
    <SafeAreaProvider>
      <View className="flex-1 bg-surface-50">
        <AppHeader />
        <SafeAreaView className="flex-1 bg-surface-50" edges={['left', 'right', 'bottom']}>
          <View className="flex-row border-b border-border-200 bg-surface-0 p-1" accessibilityRole="tablist">
            <Tab label="Buscar viaje" selected={screen === 'buscar'} onPress={() => setScreen('buscar')} />
            <Tab label="Ingreso" selected={screen === 'ingreso'} onPress={() => setScreen('ingreso')} />
            <Tab label="Compartir ubicación" selected={screen === 'compartir'} onPress={() => setScreen('compartir')} />
            <Tab label="Ubicación del servicio" selected={screen === 'ubicacion'} onPress={() => setScreen('ubicacion')} />
          </View>
          {screen === 'buscar' && <ItinerarySearch service={itineraries} />}
          {screen === 'ingreso' && <PassengerLogin service={authentication} />}
          {/* Stays mounted while hidden so the collector keeps sharing from any tab (foreground only). */}
          <View className={screen === 'compartir' ? 'flex-1' : 'hidden'}>
            <LocationSharing session={authentication} service={location} />
          </View>
          {screen === 'ubicacion' && <ServiceLocation session={authentication} service={location} />}
        </SafeAreaView>
        <StatusBar style="light" />
      </View>
    </SafeAreaProvider>
  );
}

function Tab({ label, selected, onPress }: { label: string; selected: boolean; onPress: () => void }) {
  return (
    <Pressable
      accessibilityRole="tab" accessibilityLabel={label} accessibilityState={{ selected }} onPress={onPress}
      className={selected
        ? 'min-h-12 flex-1 items-center justify-center rounded-full bg-accent-500 px-1'
        : 'min-h-12 flex-1 items-center justify-center rounded-full bg-surface-0 px-1 active:bg-surface-100'}
    >
      <Text className={selected ? 'text-center text-xs font-bold text-ink-900' : 'text-center text-xs font-semibold text-ink-700'}>{label}</Text>
    </Pressable>
  );
}
