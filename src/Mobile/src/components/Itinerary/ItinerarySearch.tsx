import { useCallback, useEffect, useRef, useState } from 'react';
import {
  ActivityIndicator, FlatList, Keyboard, KeyboardAvoidingView, Platform, Pressable,
  StyleSheet, Text, TextInput, View,
} from 'react-native';
import type {
  BusquedaItinerarioRequest, ItinerarioDto, ParadaDto, PoliticaAsiento, TramoItinerarioDto,
} from '../../contracts/itinerary';
import type { IItinerarySearchService } from '../../services/Itinerary/IItinerarySearchService';

type StopsState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; stops: ParadaDto[] };

type SearchState =
  | { status: 'idle' }
  | { status: 'searching' }
  | { status: 'error'; message: string }
  | { status: 'empty' }
  | { status: 'success'; results: ItinerarioDto[] };

const seatText: Record<PoliticaAsiento, string> = {
  sinAsiento: 'sin asiento asignado',
  asignacionAutomatica: 'asignación automática',
};

function todayLocal(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}

function validateDate(value: string): string | undefined {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value.trim());
  if (!match) return 'Ingresá la fecha con el formato AAAA-MM-DD.';
  const [year, month, day] = [Number(match[1]), Number(match[2]), Number(match[3])];
  const date = new Date(year, month - 1, day);
  if (date.getFullYear() !== year || date.getMonth() !== month - 1 || date.getDate() !== day) {
    return 'La fecha no existe en el calendario.';
  }
  return undefined;
}

// The API sends local wall-clock times; read HH:mm from the text to avoid time-zone shifts.
function hour(value: string): string {
  const match = /T(\d{2}):(\d{2})/.exec(value);
  return match ? `${match[1]}:${match[2]}` : '--:--';
}

function money(value: number): string {
  const [whole, cents] = value.toFixed(2).split('.');
  return `$ ${whole.replace(/\B(?=(\d{3})+(?!\d))/g, '.')},${cents}`;
}

export function ItinerarySearch({ service }: { service: IItinerarySearchService }) {
  const [stops, setStops] = useState<StopsState>({ status: 'loading' });
  const [originId, setOriginId] = useState<number | undefined>();
  const [destinationId, setDestinationId] = useState<number | undefined>();
  const [date, setDate] = useState(todayLocal);
  const [submitted, setSubmitted] = useState(false);
  const [search, setSearch] = useState<SearchState>({ status: 'idle' });
  const lastRequest = useRef<BusquedaItinerarioRequest | undefined>(undefined);
  const active = useRef<AbortController | undefined>(undefined);
  const searchCounter = useRef(0);

  const loadStops = useCallback(() => {
    const controller = new AbortController();
    setStops({ status: 'loading' });
    void service.getStops(controller.signal).then(result => {
      if (controller.signal.aborted) return;
      setStops(result.ok ? { status: 'ready', stops: result.data } : { status: 'error', message: result.message });
    });
    return controller;
  }, [service]);

  useEffect(() => {
    const controller = loadStops();
    return () => {
      controller.abort();
      active.current?.abort();
    };
  }, [loadStops]);

  const sameStops = originId !== undefined && originId === destinationId;
  const dateError = validateDate(date);
  const searching = search.status === 'searching';

  function run(request: BusquedaItinerarioRequest) {
    active.current?.abort();
    const controller = new AbortController();
    active.current = controller;
    const id = ++searchCounter.current;
    lastRequest.current = request;
    setSearch({ status: 'searching' });
    Keyboard.dismiss();
    void service.search(request, controller.signal).then(result => {
      if (controller.signal.aborted || id !== searchCounter.current) return;
      if (!result.ok) setSearch({ status: 'error', message: result.message });
      else setSearch(result.data.length === 0 ? { status: 'empty' } : { status: 'success', results: result.data });
    });
  }

  function submit() {
    if (searching) return;
    setSubmitted(true);
    if (originId === undefined || destinationId === undefined || sameStops || dateError) return;
    run({ origenId: originId, destinoId: destinationId, fecha: date.trim() });
  }

  function retry() {
    if (!searching && lastRequest.current) run({ ...lastRequest.current });
  }

  const header = (
    <View style={styles.form}>
      <Text style={styles.title} accessibilityRole="header">Buscar viaje</Text>
      {stops.status === 'loading' && (
        <View style={styles.row} accessibilityLiveRegion="polite">
          <ActivityIndicator accessibilityLabel="Cargando paradas" color="#222" />
          <Text style={styles.text}>Cargando paradas…</Text>
        </View>
      )}
      {stops.status === 'error' && (
        <View style={styles.box} accessibilityLiveRegion="polite">
          <Text style={styles.error}>{stops.message}</Text>
          <Action label="Reintentar" secondary onPress={() => { loadStops(); }} />
        </View>
      )}
      {stops.status === 'ready' && (
        <>
          <StopChooser label="Origen" stops={stops.stops} selected={originId} onSelect={setOriginId} />
          {submitted && originId === undefined && <Text style={styles.error}>Elegí una parada de origen.</Text>}
          <StopChooser label="Destino" stops={stops.stops} selected={destinationId} onSelect={setDestinationId} />
          {submitted && destinationId === undefined && <Text style={styles.error}>Elegí una parada de destino.</Text>}
          {sameStops && <Text style={styles.error} accessibilityLiveRegion="polite">El origen y el destino deben ser distintos.</Text>}
          <Text style={styles.label} nativeID="fechaLabel">Fecha (AAAA-MM-DD)</Text>
          <TextInput
            style={styles.input} value={date} onChangeText={setDate}
            accessibilityLabel="Fecha (AAAA-MM-DD)" accessibilityLabelledBy="fechaLabel"
            keyboardType="numbers-and-punctuation" autoCapitalize="none" autoCorrect={false}
            maxLength={10} editable={!searching} returnKeyType="search" onSubmitEditing={submit}
          />
          {(submitted || date.length === 10) && dateError && (
            <Text style={styles.error} accessibilityLiveRegion="polite">{dateError}</Text>
          )}
          <Action label={searching ? 'Buscando…' : 'Buscar'} disabled={searching} onPress={submit} />
        </>
      )}
      {searching && (
        <View style={styles.row} accessibilityLiveRegion="polite">
          <ActivityIndicator accessibilityLabel="Buscando viajes" color="#222" />
          <Text style={styles.text}>Buscando viajes…</Text>
        </View>
      )}
      {search.status === 'error' && (
        <View style={styles.box} accessibilityLiveRegion="polite">
          <Text style={styles.error}>{search.message}</Text>
          <Action label="Reintentar" secondary onPress={retry} />
        </View>
      )}
      {search.status === 'empty' && (
        <Text style={styles.text} accessibilityLiveRegion="polite">
          No hay viajes para esa fecha y esas paradas. Probá con otra fecha u otras paradas.
        </Text>
      )}
    </View>
  );

  const results = search.status === 'success' ? search.results : [];
  return (
    <KeyboardAvoidingView style={styles.flex} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
      <FlatList
        data={results}
        keyExtractor={(item, index) => `${item.salida}-${item.tramos.map(leg => leg.servicioId).join('-')}-${index}`}
        renderItem={({ item }) => <ItineraryCard itinerary={item} />}
        ListHeaderComponent={header}
        contentContainerStyle={styles.container}
        keyboardShouldPersistTaps="handled"
      />
    </KeyboardAvoidingView>
  );
}

function StopChooser({ label, stops, selected, onSelect }: {
  label: string; stops: ParadaDto[]; selected: number | undefined; onSelect: (id: number) => void;
}) {
  return (
    <View style={styles.group} accessibilityRole="radiogroup" accessibilityLabel={label}>
      <Text style={styles.label}>{label}</Text>
      <View style={styles.chips}>
        {stops.map(stop => {
          const isSelected = stop.id === selected;
          return (
            <Pressable
              key={stop.id} accessibilityRole="radio" accessibilityLabel={`${label}: ${stop.nombre}`}
              accessibilityState={{ selected: isSelected, checked: isSelected }}
              onPress={() => onSelect(stop.id)}
              style={({ pressed }) => [styles.chip, isSelected && styles.chipSelected, pressed && styles.dimmed]}
            >
              <Text style={[styles.chipLabel, isSelected && styles.chipLabelSelected]}>
                {isSelected ? `✓ ${stop.nombre}` : stop.nombre}
              </Text>
            </Pressable>
          );
        })}
      </View>
    </View>
  );
}

function ItineraryCard({ itinerary }: { itinerary: ItinerarioDto }) {
  const legs = [...itinerary.tramos].sort((a, b) => a.orden - b.orden);
  const kind = legs.length === 1 ? 'Directo' : `${legs.length} tramos`;
  return (
    <View style={styles.card}>
      <View style={styles.cardHeader}>
        <Text style={styles.cardTitle}>{hour(itinerary.salida)} → {hour(itinerary.llegada)}</Text>
        <Text style={styles.cardTotal}>{money(itinerary.precioTotal)}</Text>
      </View>
      <Text style={styles.text}>{kind}</Text>
      {legs.map((leg, index) => (
        <View key={`${leg.servicioId}-${leg.tramoId}-${leg.orden}`}>
          {index > 0 && itinerary.transbordo && (
            <Text style={styles.transfer}>
              Transbordo en {itinerary.transbordo.parada.nombre}: {itinerary.transbordo.minutos} min de espera
            </Text>
          )}
          <Leg leg={leg} />
        </View>
      ))}
    </View>
  );
}

function Leg({ leg }: { leg: TramoItinerarioDto }) {
  return (
    <View style={styles.leg}>
      <Text style={styles.legRoute}>{leg.orden}. {leg.origen.nombre} → {leg.destino.nombre}</Text>
      <Text style={styles.text}>Sale {hour(leg.salida)} · Llega {hour(leg.llegada)} · {money(leg.importe)}</Text>
      <Text style={styles.text}>Asiento: {seatText[leg.politicaAsiento]}</Text>
    </View>
  );
}

function Action({ label, disabled = false, secondary = false, onPress }: {
  label: string; disabled?: boolean; secondary?: boolean; onPress: () => void;
}) {
  return (
    <Pressable accessibilityRole="button" accessibilityState={{ disabled }} disabled={disabled} onPress={onPress}
      style={({ pressed }) => [styles.button, secondary && styles.secondary, (disabled || pressed) && styles.dimmed]}>
      <Text style={secondary ? styles.secondaryLabel : styles.buttonLabel}>{label}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  container: { padding: 24, gap: 16 },
  form: { gap: 8, marginBottom: 8 },
  title: { color: '#111', fontSize: 24, fontWeight: '600' },
  group: { gap: 4 },
  label: { color: '#111', fontWeight: '600', marginTop: 8 },
  text: { color: '#222' },
  error: { color: '#8a1c1c', fontWeight: '600' },
  row: { flexDirection: 'row', alignItems: 'center', gap: 8, minHeight: 48 },
  box: { gap: 8 },
  chips: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  chip: { minHeight: 48, paddingHorizontal: 16, justifyContent: 'center', borderWidth: 1, borderColor: '#666', borderRadius: 24, backgroundColor: '#fff' },
  chipSelected: { borderWidth: 3, borderColor: '#111', backgroundColor: '#e6e6e6' },
  chipLabel: { color: '#111' },
  chipLabelSelected: { fontWeight: '700' },
  input: { minHeight: 48, borderWidth: 1, borderColor: '#666', borderRadius: 4, padding: 12, color: '#111', backgroundColor: '#fff' },
  button: { minHeight: 48, padding: 12, borderRadius: 4, justifyContent: 'center', alignItems: 'center', backgroundColor: '#222', marginTop: 8 },
  secondary: { backgroundColor: '#eee', borderColor: '#666', borderWidth: 1 },
  buttonLabel: { color: '#fff', fontWeight: '600' },
  secondaryLabel: { color: '#111', fontWeight: '600' },
  dimmed: { opacity: 0.6 },
  card: { borderWidth: 1, borderColor: '#999', borderRadius: 8, padding: 16, gap: 8, backgroundColor: '#fafafa' },
  cardHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', gap: 8 },
  cardTitle: { color: '#111', fontSize: 20, fontWeight: '700' },
  cardTotal: { color: '#111', fontSize: 18, fontWeight: '700' },
  leg: { gap: 2, paddingVertical: 4 },
  legRoute: { color: '#111', fontWeight: '600' },
  transfer: { color: '#333', fontStyle: 'italic', paddingVertical: 4 },
});
