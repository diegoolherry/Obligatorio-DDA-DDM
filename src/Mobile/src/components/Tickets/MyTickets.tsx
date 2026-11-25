import { useCallback, useEffect, useRef, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, Text, View } from 'react-native';
import type { TicketsFilter, TicketsViewState } from '../../contracts/tickets';
import type { IAuthenticationService } from '../../services/Authentication/IAuthenticationService';
import type { ITicketsService } from '../../services/Tickets/ITicketsService';
import { countFor, filterLabel, isEmptyFilter, visibleTickets } from '../../services/Tickets/ticketFormat';
import { Banner } from '../ui/Banner';
import { Button } from '../ui/Button';
import { TicketCard } from './TicketCard';
import { TicketDetail } from './TicketDetail';

const filters: TicketsFilter[] = ['vigentes', 'anteriores'];

export function MyTickets({ session, service, onSearchTrip }: {
  session: Pick<IAuthenticationService, 'getState' | 'getRoles'>;
  service: ITicketsService;
  /** Empty state action: takes the passenger to the trip search. */
  onSearchTrip: () => void;
}) {
  const authenticated = session.getState().authenticated;
  const isPassenger = session.getRoles().includes('pasajero');
  const allowed = authenticated && isPassenger;
  const [state, setState] = useState<TicketsViewState>({ status: 'loading' });
  const [filter, setFilter] = useState<TicketsFilter>('vigentes');
  const [openId, setOpenId] = useState<string | undefined>();
  const mounted = useRef(true);
  const counter = useRef(0);

  useEffect(() => {
    mounted.current = true;
    return () => { mounted.current = false; };
  }, []);

  const load = useCallback(() => {
    const id = ++counter.current;
    setState({ status: 'loading' });
    void service.listTickets().then(result => {
      // A newer load wins over a late response.
      if (!mounted.current || id !== counter.current) return;
      setState(result.ok ? { status: 'ready', data: result.data } : { status: 'error', message: result.message });
    });
  }, [service]);

  useEffect(() => {
    if (!allowed) return undefined;
    load();
    return () => { counter.current++; };
  }, [allowed, load]);

  if (!allowed) {
    return (
      <View className="gap-3 p-4">
        <Text accessibilityRole="header" className="text-2xl font-bold text-ink-900">Mis tickets</Text>
        <Banner kind="warning">
          {!authenticated ? 'Ingresá como pasajero en la pestaña «Ingreso» para ver tus tickets.'
            : 'Esta pantalla es para cuentas de pasajero.'}
        </Banner>
      </View>
    );
  }

  const open = state.status === 'ready' && openId !== undefined
    ? [...state.data.vigentes, ...state.data.anteriores].find(ticket => ticket.ticketId === openId)
    : undefined;
  if (open) return <TicketDetail ticket={open} onBack={() => setOpenId(undefined)} />;

  const tickets = visibleTickets(state, filter);
  return (
    <ScrollView contentContainerClassName="gap-4 p-4">
      <View>
        <Text accessibilityRole="header" className="text-3xl font-bold text-ink-900">Mis tickets</Text>
        <Text className="text-base text-ink-500">Un ticket por cada tramo.</Text>
      </View>

      <View className="flex-row rounded-xl bg-surface-100 p-1" accessibilityRole="tablist">
        {filters.map(item => {
          const selected = item === filter;
          const label = filterLabel(item, countFor(state, item));
          return (
            <Pressable key={item} accessibilityRole="tab" accessibilityLabel={label} accessibilityState={{ selected }}
              onPress={() => setFilter(item)}
              className={selected
                ? 'min-h-12 flex-1 items-center justify-center rounded-lg bg-surface-0 px-2'
                : 'min-h-12 flex-1 items-center justify-center rounded-lg px-2 active:bg-surface-0'}>
              <Text className={selected ? 'text-base font-bold text-ink-900' : 'text-base font-semibold text-ink-700'}>{label}</Text>
            </Pressable>
          );
        })}
      </View>

      {state.status === 'loading' && (
        <View className="gap-4" accessibilityLiveRegion="polite">
          <View className="flex-row items-center gap-2">
            <ActivityIndicator accessibilityLabel="Cargando tickets" color="#141414" />
            <Text className="text-base text-ink-700">Cargando tus tickets…</Text>
          </View>
          {[0, 1].map(index => <View key={index} className="h-40 rounded-xl bg-surface-100" />)}
        </View>
      )}

      {state.status === 'error' && (
        <Banner kind="error" title="No pudimos cargar tus tickets" actionLabel="Reintentar" onAction={load}>
          {`${state.message} Tus tickets no se perdieron.`}
        </Banner>
      )}

      {isEmptyFilter(state, filter) && (
        <View className="items-center gap-3 py-8">
          <Text accessibilityRole="header" className="text-center text-xl font-bold text-ink-900">
            {filter === 'vigentes' ? 'No tenés tickets vigentes' : 'No tenés tickets anteriores'}
          </Text>
          <Text className="text-center text-base text-ink-500">
            {filter === 'vigentes'
              ? 'Cuando compres un viaje, vas a ver acá un ticket por cada tramo.'
              : 'Acá vas a ver los tickets que ya usaste o que vencieron.'}
          </Text>
          {filter === 'vigentes' && <Button label="Buscar viaje" onPress={onSearchTrip} />}
        </View>
      )}

      {tickets.map(ticket => <TicketCard key={ticket.ticketId} ticket={ticket} onOpen={setOpenId} />)}
    </ScrollView>
  );
}
