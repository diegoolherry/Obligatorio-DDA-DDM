import { Linking, Text, View } from 'react-native';
import { useEffect, useState } from 'react';
import type { IAuthenticationService } from '../../services/Authentication/IAuthenticationService';
import { canFinishPurchase } from '../../services/Purchase/PurchaseService';
import type { PurchaseFinalization, PurchaseService } from '../../services/Purchase/PurchaseService';
import type {
  CompraConfirmadaResponseDto, CotizarCompraResponseDto, IniciarIntentoCompraResponseDto, IntentoPendienteResponseDto, PagoResponseDto,
} from '../../contracts/purchase';
import type { ItinerarioDto } from '../../contracts/itinerary';
import { Banner } from '../ui/Banner';
import { Button } from '../ui/Button';

type FlowState = 'idle' | 'creatingAttempt' | 'loadingQuote' | 'consent' | 'startingPayment' | 'pending'
  | 'unknown' | 'syncing' | 'rejected' | 'refundPending' | 'refunded' | 'confirmed' | 'expired'
  | 'uncertainCreate' | 'error';

const money = (value: number) => new Intl.NumberFormat('es-UY', { style: 'currency', currency: 'UYU' }).format(value);
const safeCheckoutUrl = (value: string | null | undefined): string | undefined => {
  try {
    if (!value) return undefined;
    const url = new URL(value);
    return url.protocol === 'https:' && !url.username && !url.password ? url.toString() : undefined;
  } catch { return undefined; }
};
const deadline = (value: string) => {
  const date = new Date(value);
  return Number.isFinite(date.getTime())
    ? new Intl.DateTimeFormat('es-UY', { dateStyle: 'short', timeStyle: 'short' }).format(date)
    : 'no disponible';
};

export function PurchaseFlow({ itinerary, authentication, service, onClose, onFinalize, pending, onDiscard }: {
  itinerary: ItinerarioDto; authentication: IAuthenticationService; service: PurchaseService; onClose: () => void;
  onFinalize: (reason: PurchaseFinalization) => void;
  // Attempt recovered from the API for the current session: the flow resumes it instead of creating another one.
  pending?: IntentoPendienteResponseDto;
  // Leaves a dead end (uncertain create or error without attempt) and goes back to the results list.
  onDiscard?: () => void;
}) {
  const [state, setState] = useState<FlowState>('idle');
  const [attempt, setAttempt] = useState<IniciarIntentoCompraResponseDto | undefined>(pending?.intento);
  const [quote, setQuote] = useState<CotizarCompraResponseDto>();
  const [payment, setPayment] = useState<PagoResponseDto | undefined>(pending?.pago ?? undefined);
  const [purchase, setPurchase] = useState<CompraConfirmadaResponseDto>();
  const [message, setMessage] = useState('');
  const busy = state === 'creatingAttempt' || state === 'loadingQuote' || state === 'startingPayment' || state === 'syncing';
  const holdUntil = attempt?.holdVenceEn ?? quote?.holdVenceEn;
  const expired = holdUntil !== undefined && new Date(holdUntil).getTime() <= Date.now();
  const link = safeCheckoutUrl(payment?.checkoutUrl);
  const canFinalize = canFinishPurchase(state, holdUntil, payment?.estado, Date.now());

  useEffect(() => {
    if (!pending) return;
    // Resume the recovered attempt: verify non-terminal payments, show refund follow-up, or ask the price again.
    const paid = pending.pago;
    if (!paid) { void loadQuote(pending.intento.intentoId); return; }
    if (paid.estado === 'rechazado') { setState('rejected'); return; }
    if (paid.estado === 'devuelto') { setState('refunded'); return; }
    void syncPayment();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function createAttempt() {
    if (busy || attempt || state === 'uncertainCreate') return;
    if (!authentication.getState().authenticated) { setState('error'); setMessage('Ingrese con tu cuenta de pasajero antes de iniciar la compra.'); return; }
    setMessage(''); setState('creatingAttempt');
    const tramos = [...itinerary.tramos].sort((a, b) => a.orden - b.orden).map(({ orden, servicioId }) => ({ orden, servicioId }));
    const result = await service.createAttempt(tramos);
    if (!result.ok) {
      setState(result.kind === 'ambiguous' ? 'uncertainCreate' : 'error'); setMessage(result.message); return;
    }
    setAttempt(result.data);
    setState('idle');
    await loadQuote(result.data.intentoId);
  }

  async function loadQuote(id = attempt?.intentoId) {
    if (!id || (busy && state !== 'creatingAttempt')) return;
    setMessage(''); setState('loadingQuote');
    const result = await service.getQuote(id);
    if (!result.ok) { setState(result.kind === 'unknown' ? 'unknown' : 'error'); setMessage(result.message); return; }
    setQuote(result.data);
    if (new Date(result.data.holdVenceEn).getTime() <= Date.now()) {
      setState('expired'); setMessage('El hold original venció y no se puede extender.');
    } else setState('consent');
  }

  async function acceptQuote() {
    if (!attempt || !quote || busy) return;
    if (new Date(quote.holdVenceEn).getTime() <= Date.now()) {
      setState('expired'); setMessage('El hold original venció y no se puede extender.'); return;
    }
    setMessage(''); setState('startingPayment');
    const result = await service.startPayment(attempt.intentoId, quote.huellaCotizacion);
    if (result.kind === 'quoteChanged') {
      setQuote(result.quote); setState('consent'); setMessage('El precio cambió. Revise y aceptá nuevamente la cotización vigente.'); return;
    }
    if (result.kind === 'unknown') { setState('unknown'); setMessage(result.message); return; }
    if (result.kind === 'error') { setState('error'); setMessage(result.message); return; }
    setPayment(result.payment);
    if (result.payment.estado === 'rechazado') setState('rejected');
    else setState('pending');
  }

  async function syncPayment() {
    if (!attempt || busy) return;
    setMessage(''); setState('syncing');
    const result = await service.syncPayment(attempt.intentoId);
    if (!result.ok) { setState(result.kind === 'unknown' ? 'unknown' : 'pending'); setMessage(result.message); return; }
    const paymentResult = result.data.pago;
    if (paymentResult.intentoId !== attempt.intentoId) {
      setState('unknown'); setMessage('La respuesta no corresponde al intento actual. Verifique el mismo intento.'); return;
    }
    setPayment(paymentResult);
    if (paymentResult.estado === 'aprobado') {
      const confirmed = result.data.compra;
      const tickets = confirmed?.tickets ?? [];
      const qrCount = new Set(tickets.map(ticket => ticket.qrOpaco)).size;
      if (confirmed && confirmed.intentoId === attempt.intentoId && confirmed.pagoId === paymentResult.pagoId
        && tickets.length === itinerary.tramos.length && qrCount === tickets.length) {
        setPurchase(confirmed); setState('confirmed'); return;
      }
      setPurchase(undefined); setState('unknown');
      setMessage('La API no devolvió una compra y todos sus tickets para confirmar el resultado. Verifique el mismo intento.'); return;
    }
    setPurchase(undefined);
    switch (paymentResult.estado) {
      case 'rechazado': setState('rejected'); break;
      case 'devolucionPendiente': setState('refundPending'); break;
      case 'devuelto': setState('refunded'); break;
      default: setState('pending');
    }
  }

  async function openCheckout(url: string) {
    const safe = safeCheckoutUrl(url);
    if (!safe) { setMessage('La API no devolvió una dirección HTTPS de checkout válida.'); return; }
    try { await Linking.openURL(safe); }
    catch { setMessage('No se pudo abrir el checkout externo. Vuelva a intentar abrirlo o verifique el mismo pago.'); }
  }

  const currentQuote = quote?.cotizacionActual;
  return (
    <View className="gap-3 rounded-lg border border-primary-300 bg-surface-0 p-4">
      <Text className="text-lg font-bold text-ink-900">Compra TEST</Text>
      <Text className="text-sm text-ink-700">Pago de prueba. No ingreses datos de tarjeta en esta aplicación.</Text>
      {attempt && <Text className="text-sm text-ink-700">Intento {attempt.intentoId}. Hold original hasta {deadline(attempt.holdVenceEn)}; consultar no lo extiende.</Text>}
      {((state === 'idle' && !pending) || (state === 'error' && !attempt)) && <Button label="Iniciar compra" disabled={busy} onPress={() => void createAttempt()} />}
      {state === 'creatingAttempt' && <Button label="Creando intento…" loading onPress={() => {}} />}
      {state === 'loadingQuote' && <Text accessibilityLiveRegion="polite" className="text-base text-ink-900">Consultando el precio vigente…</Text>}
      {currentQuote && (state === 'consent' || state === 'rejected' || state === 'expired') && (
        <View className="gap-2">
          <Text className="text-base font-semibold text-ink-900">Cotización vigente</Text>
          {currentQuote.tramos.slice().sort((a, b) => a.orden - b.orden).map(leg => (
            <Text key={`${leg.orden}-${leg.servicioId}`} className="text-base text-ink-900">
              Tramo {leg.orden}: {leg.origen.nombre} a {leg.destino.nombre} · {money(leg.importe)} · {leg.politicaAsiento === 'sinAsiento' ? 'sin asiento' : 'asiento automático'}
            </Text>
          ))}
          <Text className="text-lg font-bold text-ink-900">Total: {money(currentQuote.precioTotal)}</Text>
          {quote?.cambioPrecio && <Banner kind="warning">El precio cambió desde la reserva. Revise el valor vigente antes de continuar.</Banner>}
          {expired ? <Banner kind="error">El hold original venció. No se puede extender.</Banner>
            : state === 'rejected'
              ? <Button label="Revisar precio y reintentar" disabled={busy} onPress={() => void loadQuote()} />
              : <Button label="Acepto el precio y continuar" disabled={state !== 'consent'} onPress={() => void acceptQuote()} />}
        </View>
      )}
      {state === 'startingPayment' && <Text accessibilityLiveRegion="polite" className="text-base text-ink-900">Iniciando el pago…</Text>}
      {(state === 'pending' || state === 'unknown') && (
        <View className="gap-2">
          <Banner kind="warning" children={`${state === 'unknown' ? 'No se pudo confirmar el inicio o el estado del pago.' : 'El pago sigue pendiente de confirmación.'} Vuelva a esta aplicación y verifique el mismo intento; no se creará otro pago.`} />
          {link && <Button label="Abrir checkout TEST" variant="secondary" onPress={() => void openCheckout(link)} />}
          <Button label={busy ? 'Verificando…' : 'Verificar pago'} loading={busy} onPress={() => void syncPayment()} />
        </View>
      )}
      {state === 'syncing' && <Text accessibilityLiveRegion="polite" className="text-base text-ink-900">Verificando el pago con la API…</Text>}
      {state === 'rejected' && <View className="gap-2"><Banner kind="error">El pago fue rechazado; no se emitieron tickets. Puede reintentar solo dentro del hold original.</Banner><Button label="Revisar cotización para reintentar" disabled={expired || busy} onPress={() => void loadQuote()} /></View>}
      {state === 'refundPending' && <Banner kind="warning">La aprobación requiere una devolución pendiente; todavía no se confirmó la compra ni se emitieron tickets.</Banner>}
      {state === 'refundPending' && <Button label="Verificar devolución" disabled={busy} onPress={() => void syncPayment()} />}
      {state === 'refunded' && <Banner kind="info">La devolución fue confirmada por la API. No se emitieron tickets.</Banner>}
      {state === 'confirmed' && purchase && (
        <View className="gap-2" accessibilityLiveRegion="polite">
          <Banner kind="success" title="Compra confirmada" children={`Compra ${purchase.compraId} · ${purchase.tickets.length} tickets emitidos · ${money(purchase.importeTotal)}.`} />
          {purchase.tickets.slice().sort((a, b) => a.orden - b.orden).map(ticket => (
            <Text key={ticket.ticketId} className="text-sm text-ink-900">Tramo {ticket.orden}: {ticket.origenNombre} a {ticket.destinoNombre} · ticket {ticket.ticketId} · asiento {ticket.numeroAsiento ?? 'sin asiento'}</Text>
          ))}
        </View>
      )}
      {(state === 'uncertainCreate' || state === 'error') && <Banner kind="error">{message || 'No se pudo completar la operación.'}</Banner>}
      {state === 'uncertainCreate' && <Banner kind="warning">No vuelva a crear otro intento automáticamente.</Banner>}
      {message && state !== 'error' && state !== 'uncertainCreate' && <Text accessibilityLiveRegion="polite" className="text-sm text-error-700">{message}</Text>}
      {state === 'error' && attempt && <Button label="Reintentar consulta de precio" disabled={busy} onPress={() => void loadQuote()} />}
      {(state === 'uncertainCreate' || (state === 'error' && !attempt)) && onDiscard && (
        <Button label="Volver a los resultados" variant="secondary" onPress={onDiscard} />
      )}
      {canFinalize ? (
        <Button label={state === 'confirmed' ? 'Finalizar compra y buscar otro viaje' : state === 'refunded' ? 'Cerrar devolución y buscar otro viaje' : 'Cerrar intento y elegir otro viaje'} variant="secondary" disabled={busy} onPress={() => {
          if (!canFinishPurchase(state, holdUntil, payment?.estado, Date.now())) return;
          const reason: PurchaseFinalization = state === 'confirmed' ? 'confirmed' : state === 'refunded' ? 'refunded' : 'expired';
          onFinalize(reason);
        }} />
      ) : <Button label="Volver a itinerarios" variant="secondary" disabled={busy} onPress={onClose} />}
    </View>
  );
}
