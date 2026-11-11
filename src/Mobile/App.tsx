import { useEffect, useState } from 'react';
import { StatusBar } from 'expo-status-bar';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { SafeAreaProvider, SafeAreaView } from 'react-native-safe-area-context';
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
      <SafeAreaView style={styles.safeArea}>
        <View style={styles.tabs} accessibilityRole="tablist">
          <Tab label="Buscar viaje" selected={screen === 'buscar'} onPress={() => setScreen('buscar')} />
          <Tab label="Ingreso" selected={screen === 'ingreso'} onPress={() => setScreen('ingreso')} />
          <Tab label="Compartir ubicación" selected={screen === 'compartir'} onPress={() => setScreen('compartir')} />
          <Tab label="Ubicación del servicio" selected={screen === 'ubicacion'} onPress={() => setScreen('ubicacion')} />
        </View>
        {screen === 'buscar' && <ItinerarySearch service={itineraries} />}
        {screen === 'ingreso' && <PassengerLogin service={authentication} />}
        {/* Stays mounted while hidden so the collector keeps sharing from any tab (foreground only). */}
        <View style={screen === 'compartir' ? styles.screen : styles.hidden}>
          <LocationSharing session={authentication} service={location} />
        </View>
        {screen === 'ubicacion' && <ServiceLocation session={authentication} service={location} />}
        <StatusBar style="dark" />
      </SafeAreaView>
    </SafeAreaProvider>
  );
}

function Tab({ label, selected, onPress }: { label: string; selected: boolean; onPress: () => void }) {
  return (
    <Pressable
      accessibilityRole="tab" accessibilityLabel={label} accessibilityState={{ selected }} onPress={onPress}
      style={({ pressed }) => [styles.tab, selected && styles.tabSelected, pressed && styles.tabPressed]}
    >
      <Text style={[styles.tabLabel, selected && styles.tabLabelSelected]}>{label}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  safeArea: { flex: 1, backgroundColor: '#fff' },
  screen: { flex: 1 },
  hidden: { display: 'none' },
  tabs: { flexDirection: 'row', borderBottomWidth: 1, borderBottomColor: '#999' },
  tab: { flex: 1, minHeight: 48, paddingHorizontal: 2, justifyContent: 'center', alignItems: 'center', borderBottomWidth: 3, borderBottomColor: 'transparent' },
  tabSelected: { borderBottomColor: '#111' },
  tabPressed: { opacity: 0.6 },
  tabLabel: { color: '#333', fontSize: 12, textAlign: 'center' },
  tabLabelSelected: { color: '#111', fontWeight: '700' },
});
