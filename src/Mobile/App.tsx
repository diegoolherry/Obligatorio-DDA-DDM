import { useState } from 'react';
import { StatusBar } from 'expo-status-bar';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { SafeAreaProvider, SafeAreaView } from 'react-native-safe-area-context';
import { PassengerLogin } from './src/components/Authentication/PassengerLogin';
import { ItinerarySearch } from './src/components/Itinerary/ItinerarySearch';
import { AuthenticationService } from './src/services/Authentication/AuthenticationService';
import type { IAuthenticationService } from './src/services/Authentication/IAuthenticationService';
import { ItinerarySearchService } from './src/services/Itinerary/ItinerarySearchService';
import type { IItinerarySearchService } from './src/services/Itinerary/IItinerarySearchService';

type Screen = 'buscar' | 'ingreso';

export default function App() {
  const [authentication] = useState<IAuthenticationService>(
    () => new AuthenticationService(process.env.EXPO_PUBLIC_API_URL),
  );
  const [itineraries] = useState<IItinerarySearchService>(
    () => new ItinerarySearchService(process.env.EXPO_PUBLIC_API_URL),
  );
  const [screen, setScreen] = useState<Screen>('buscar');
  return (
    <SafeAreaProvider>
      <SafeAreaView style={styles.safeArea}>
        <View style={styles.tabs} accessibilityRole="tablist">
          <Tab label="Buscar viaje" selected={screen === 'buscar'} onPress={() => setScreen('buscar')} />
          <Tab label="Ingreso" selected={screen === 'ingreso'} onPress={() => setScreen('ingreso')} />
        </View>
        {screen === 'buscar' ? <ItinerarySearch service={itineraries} /> : <PassengerLogin service={authentication} />}
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
  tabs: { flexDirection: 'row', borderBottomWidth: 1, borderBottomColor: '#999' },
  tab: { flex: 1, minHeight: 48, justifyContent: 'center', alignItems: 'center', borderBottomWidth: 3, borderBottomColor: 'transparent' },
  tabSelected: { borderBottomColor: '#111' },
  tabPressed: { opacity: 0.6 },
  tabLabel: { color: '#333' },
  tabLabelSelected: { color: '#111', fontWeight: '700' },
});
