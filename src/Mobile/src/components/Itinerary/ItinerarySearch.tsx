import { useCallback, useEffect, useRef, useState } from 'react';
import {
  ActivityIndicator, FlatList, Keyboard, KeyboardAvoidingView, Platform, Pressable, Text, View,
} from 'react-native';
import type {
  BusquedaItinerarioRequest, ItinerarioDto, ParadaDto, PoliticaAsiento, TramoItinerarioDto,
} from '../../contracts/itinerary';
import type { IItinerarySearchService } from '../../services/Itinerary/IItinerarySearchService';
import { Banner } from '../ui/Banner';
import { Button } from '../ui/Button';
import { StatusBadge } from '../ui/StatusBadge';
import { TextField } from '../ui/TextField';
import type { IAuthenticationService } from '../../services/Authentication/IAuthenticationService';
import type { IntentoPendienteResponseDto } from '../../contracts/purchase';
import type { PurchaseFinalization, PurchaseService } from '../../services/Purchase/PurchaseService';
import { PurchaseFlow } from '../Purchase/PurchaseFlow';

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

export function purchaseFlowInstanceKey(sessionEpoch: number, selectionRevision: number): string {
  return `${sessionEpoch}:${selectionRevision}`;
}

export function ItinerarySearch({ service, authentication, purchase, sessionEpoch }: {
  service: IItinerarySearchService; authentication: IAuthenticationService; purchase: PurchaseService; sessionEpoch: number;
}) {
  const [stops, setStops] = useState<StopsState>({ status: 'loading' });
  const [originId, setOriginId] = useState<number | undefined>();
  const [destinationId, setDestinationId] = useState<number | undefined>();
  const [date, setDate] = useState(todayLocal);
  const [submitted, setSubmitted] = useState(false);
  const [search, setSearch] = useState<SearchState>({ status: 'idle' });
  const [purchaseItinerary, setPurchaseItinerary] = useState<ItinerarioDto>();
  const [purchaseSessionEpoch, setPurchaseSessionEpoch] = useState(sessionEpoch);
  const [purchaseRevision, setPurchaseRevision] = useState(0);
  const [purchaseVisible, setPurchaseVisible] = useState(true);
  const [pendingPurchase, setPendingPurchase] = useState<{ epoch: number; data: IntentoPendienteResponseDto }>();
  const [pendingMessage, setPendingMessage] = useState('');
  const pendingLookup = useRef(0);
  const lastRequest = useRef<BusquedaItinerarioRequest | undefined>(undefined);
  const active = useRef<AbortController | undefined>(undefined);
  const searchCounter = useRef(0);

  useEffect(() => {
    setPurchaseItinerary(undefined);
    setPurchaseSessionEpoch(sessionEpoch);
    setPurchaseVisible(true);
  }, [sessionEpoch]);

  // Asks the API for the current passenger's pending attempt. Only the current session is used and
  // an answer that arrives after the session changed is ignored, so one passenger never sees another's attempt.
  const lookupPending = useCallback(() => {
    const lookup = ++pendingLookup.current;
    setPendingPurchase(undefined);
    setPendingMessage('');
    if (!authentication.getState().authenticated) return;
    void purchase.getPendingAttempt().then(result => {
      if (lookup !== pendingLookup.current) return;
      if (!result.ok) { setPendingMessage(result.message); return; }
      if (result.data) setPendingPurchase({ epoch: sessionEpoch, data: result.data });
    });
  }, [authentication, purchase, sessionEpoch]);

  useEffect(() => {
    lookupPending();
    return () => { pendingLookup.current += 1; };
  }, [lookupPending]);

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
    <View className="mb-2 gap-2">
      <Text className="text-2xl font-bold text-ink-900" accessibilityRole="header">Buscar viaje</Text>
      {stops.status === 'loading' && (
        <View className="min-h-12 flex-row items-center gap-2" accessibilityLiveRegion="polite">
          <ActivityIndicator accessibilityLabel="Cargando paradas" color="#141414" />
          <Text className="text-base text-ink-900">Cargando paradas…</Text>
        </View>
      )}
      {stops.status === 'error' && (
        <Banner kind="error" actionLabel="Reintentar" onAction={() => { loadStops(); }}>{stops.message}</Banner>
      )}
      {stops.status === 'ready' && (
        <>
          <StopChooser label="Origen" stops={stops.stops} selected={originId} onSelect={setOriginId} />
          {submitted && originId === undefined && <FieldError>Elegí una parada de origen.</FieldError>}
          <StopChooser label="Destino" stops={stops.stops} selected={destinationId} onSelect={setDestinationId} />
          {submitted && destinationId === undefined && <FieldError>Elegí una parada de destino.</FieldError>}
          {sameStops && <FieldError>El origen y el destino deben ser distintos.</FieldError>}
          <TextField
            label="Fecha (AAAA-MM-DD)" labelId="fechaLabel" value={date} onChangeText={setDate}
            keyboardType="numbers-and-punctuation" autoCapitalize="none" autoCorrect={false}
            maxLength={10} disabled={searching} returnKeyType="search" onSubmitEditing={submit}
            error={(submitted || date.length === 10) && dateError ? dateError : undefined}
          />
          <Button label={searching ? 'Buscando…' : 'Buscar'} loading={searching} onPress={submit} />
        </>
      )}
      {searching && (
        <View className="min-h-12 flex-row items-center gap-2" accessibilityLiveRegion="polite">
          <ActivityIndicator accessibilityLabel="Buscando viajes" color="#141414" />
          <Text className="text-base text-ink-900">Buscando viajes…</Text>
        </View>
      )}
      {search.status === 'error' && (
        <Banner kind="error" actionLabel="Reintentar" onAction={retry}>{search.message}</Banner>
      )}
      {search.status === 'empty' && (
        <Text className="text-base text-ink-900" accessibilityLiveRegion="polite">
          No hay viajes para esa fecha y esas paradas. Probá con otra fecha u otras paradas.
        </Text>
      )}
    </View>
  );

  const results = search.status === 'success' ? search.results : [];
  const activePurchaseItinerary = purchaseSessionEpoch === sessionEpoch ? purchaseItinerary : undefined;
  const activePending = pendingPurchase?.epoch === sessionEpoch ? pendingPurchase.data : undefined;
  const pendingHeader = activePending
    ? (
      <View className="gap-3">
        <View className={purchaseVisible ? '' : 'hidden'}>
          <PurchaseFlow key={`pendiente:${sessionEpoch}:${activePending.intento.intentoId}`} itinerary={activePending.intento.itinerario}
            authentication={authentication} service={purchase} pending={activePending}
            onClose={() => setPurchaseVisible(false)} onFinalize={(_reason: PurchaseFinalization) => {
              setPendingPurchase(undefined);
              setPurchaseVisible(false);
            }} />
        </View>
        {!purchaseVisible && (
          <View className="gap-2">
            <Text className="text-base text-ink-900" accessibilityLiveRegion="polite">Hay una compra pendiente de verificación.</Text>
            <Button label="Volver a la compra pendiente" onPress={() => setPurchaseVisible(true)} />
          </View>
        )}
      </View>
    ) : undefined;
  const purchaseHeader = pendingHeader ?? (activePurchaseItinerary
    ? (
      <View className="gap-3">
        <View className={purchaseVisible ? '' : 'hidden'}>
          <PurchaseFlow key={purchaseFlowInstanceKey(sessionEpoch, purchaseRevision)} itinerary={activePurchaseItinerary} authentication={authentication} service={purchase}
            onClose={() => setPurchaseVisible(false)} onFinalize={(_reason: PurchaseFinalization) => {
              setPurchaseItinerary(undefined);
              setPurchaseVisible(false);
            }} onDiscard={() => {
              // The create may have succeeded without an answer: look the attempt up instead of creating another one.
              setPurchaseItinerary(undefined);
              setPurchaseVisible(true);
              lookupPending();
            }} />
        </View>
        {!purchaseVisible && (
          <View className="gap-2">
            <Text className="text-base text-ink-900" accessibilityLiveRegion="polite">Hay una compra en curso en esta aplicación.</Text>
            <Button label="Volver a la compra en curso" onPress={() => setPurchaseVisible(true)} />
          </View>
        )}
      </View>
    ) : (
      <View className="gap-2">
        {pendingMessage !== '' && <Banner kind="warning" actionLabel="Reintentar" onAction={lookupPending}>{pendingMessage}</Banner>}
        {header}
      </View>
    ));
  return (
    <KeyboardAvoidingView className="flex-1" behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
      <FlatList
          data={activePurchaseItinerary || activePending ? [] : results}
        keyExtractor={(item, index) => `${item.salida}-${item.tramos.map(leg => leg.servicioId).join('-')}-${index}`}
        renderItem={({ item }) => <ItineraryCard itinerary={item} onPurchase={() => {
          if (activePurchaseItinerary) return;
          setPurchaseRevision(revision => revision + 1);
          setPurchaseItinerary(item);
          setPurchaseSessionEpoch(sessionEpoch);
          setPurchaseVisible(true);
        }} />}
        ListHeaderComponent={purchaseHeader}
        contentContainerClassName="gap-4 p-6"
        keyboardShouldPersistTaps="handled"
      />
    </KeyboardAvoidingView>
  );
}

function FieldError({ children }: { children: string }) {
  return (
    <View className="rounded-sm bg-error-100 px-3 py-2" accessibilityLiveRegion="polite">
      <Text className="text-sm text-error-700">{children}</Text>
    </View>
  );
}

function StopChooser({ label, stops, selected, onSelect }: {
  label: string; stops: ParadaDto[]; selected: number | undefined; onSelect: (id: number) => void;
}) {
  return (
    <View className="gap-1" accessibilityRole="radiogroup" accessibilityLabel={label}>
      <Text className="mt-2 text-base font-semibold text-ink-900">{label}</Text>
      <View className="flex-row flex-wrap gap-2">
        {stops.map(stop => {
          const isSelected = stop.id === selected;
          return (
            <Pressable
              key={stop.id} accessibilityRole="radio" accessibilityLabel={`${label}: ${stop.nombre}`}
              accessibilityState={{ selected: isSelected, checked: isSelected }}
              onPress={() => onSelect(stop.id)}
              className={isSelected
                ? 'min-h-12 justify-center rounded-full border-2 border-ink-900 bg-ink-900 px-4 active:opacity-80'
                : 'min-h-12 justify-center rounded-full border-2 border-ink-300 bg-surface-0 px-4 active:bg-surface-100'}
            >
              <Text className={isSelected ? 'text-base font-bold text-surface-0' : 'text-base text-ink-900'}>
                {isSelected ? `✓ ${stop.nombre}` : stop.nombre}
              </Text>
            </Pressable>
          );
        })}
      </View>
    </View>
  );
}

function ItineraryCard({ itinerary, onPurchase }: { itinerary: ItinerarioDto; onPurchase: () => void }) {
  const legs = [...itinerary.tramos].sort((a, b) => a.orden - b.orden);
  const direct = legs.length === 1;
  return (
    <View className="gap-2 rounded-lg border border-border-200 bg-surface-0 p-4">
      <View className="flex-row items-center justify-between gap-2">
        <StatusBadge kind={direct ? 'success' : 'accent'} label={direct ? 'Directo' : `${legs.length} tramos`} />
        <Text className="text-lg font-bold text-ink-900">{money(itinerary.precioTotal)}</Text>
      </View>
      <Text className="text-xl font-bold text-ink-900">{hour(itinerary.salida)} → {hour(itinerary.llegada)}</Text>
      {legs.map((leg, index) => (
        <View key={`${leg.servicioId}-${leg.tramoId}-${leg.orden}`}>
          {index > 0 && itinerary.transbordo && (
            <Text className="my-1 rounded-md bg-surface-50 px-3 py-2 text-sm text-ink-900">
              Transbordo en {itinerary.transbordo.parada.nombre}: {itinerary.transbordo.minutos} min de espera
            </Text>
          )}
          <Leg leg={leg} />
        </View>
      ))}
      <Button label="Comprar este viaje" onPress={onPurchase} />
    </View>
  );
}

function Leg({ leg }: { leg: TramoItinerarioDto }) {
  return (
    <View className="gap-1 py-1">
      <Text className="text-base font-semibold text-ink-900">{leg.orden}. {leg.origen.nombre} → {leg.destino.nombre}</Text>
      <Text className="text-base text-ink-700">Sale {hour(leg.salida)} · Llega {hour(leg.llegada)} · {money(leg.importe)}</Text>
      <Text className="text-base text-ink-700">Asiento: {seatText[leg.politicaAsiento]}</Text>
    </View>
  );
}
