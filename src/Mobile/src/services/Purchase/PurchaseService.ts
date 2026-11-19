import type { IAuthenticationService } from '../Authentication/IAuthenticationService';
import type { ItinerarioDto, TramoItinerarioDto } from '../../contracts/itinerary';
import type {
  CotizarCompraResponseDto, IniciarIntentoCompraResponseDto, IntentoPendienteResponseDto, PagoResponseDto,
  PurchaseResult, PurchaseStartResult, SeleccionTramoCompraDto, SincronizarPagoResponseDto,
} from '../../contracts/purchase';

const genericMessage = 'La API no pudo completar la compra. Revise la conexión e intente verificar el estado.';
const unauthenticatedMessage = 'Ingrese con tu cuenta de pasajero para continuar.';
const ambiguousCreateMessage = 'No se pudo confirmar la creación del intento. No vuelva a crearlo automáticamente.';

const pendingMessage = 'No se pudo consultar si tenés una compra pendiente. Revisá la conexión e intentá de nuevo.';

type ApiReply = { outcome: string; status?: number; body?: unknown };
const isRecord = (value: unknown): value is Record<string, unknown> =>
  typeof value === 'object' && value !== null && !Array.isArray(value);
const nonEmpty = (value: unknown): value is string => typeof value === 'string' && value.length > 0;
const isDateString = (value: unknown): value is string => nonEmpty(value) && Number.isFinite(Date.parse(value));
const isPlace = (value: unknown): boolean => isRecord(value) && nonEmpty(value.nombre);
const isLeg = (value: unknown): value is TramoItinerarioDto => isRecord(value)
  && Number.isInteger(value.orden) && Number(value.orden) > 0
  && nonEmpty(value.servicioId) && nonEmpty(value.tramoId)
  && isPlace(value.origen) && isPlace(value.destino)
  && isDateString(value.salida) && isDateString(value.llegada)
  && typeof value.importe === 'number' && Number.isFinite(value.importe)
  && (value.politicaAsiento === 'sinAsiento' || value.politicaAsiento === 'asignacionAutomatica');
const isTransfer = (value: unknown): boolean => isRecord(value) && isPlace(value.parada)
  && Number.isInteger(value.minutos);
const isItinerary = (value: unknown): value is ItinerarioDto => isRecord(value)
  && isDateString(value.salida) && isDateString(value.llegada)
  && typeof value.precioTotal === 'number' && Number.isFinite(value.precioTotal)
  && Array.isArray(value.tramos) && value.tramos.length > 0 && value.tramos.every(isLeg)
  && new Set(value.tramos.map(tramo => tramo.orden)).size === value.tramos.length
  && (value.transbordo === null || isTransfer(value.transbordo));
const isAttempt = (value: unknown): value is IniciarIntentoCompraResponseDto => isRecord(value)
  && nonEmpty(value.intentoId) && nonEmpty(value.holdVenceEn) && isItinerary(value.itinerario);
const isQuote = (value: unknown): value is CotizarCompraResponseDto => isRecord(value)
  && nonEmpty(value.intentoId) && nonEmpty(value.holdVenceEn) && nonEmpty(value.huellaCotizacion)
  && isItinerary(value.cotizacionActual) && isItinerary(value.cotizacionOriginal) && typeof value.cambioPrecio === 'boolean';
const states = ['pendiente', 'desconocido', 'aprobado', 'rechazado', 'devolucionPendiente', 'devuelto'];
const isPayment = (value: unknown): value is PagoResponseDto => isRecord(value)
  && nonEmpty(value.pagoId) && nonEmpty(value.intentoId) && typeof value.estado === 'string'
  && states.includes(value.estado) && typeof value.importe === 'number' && nonEmpty(value.moneda) && nonEmpty(value.holdVenceEn)
  && (value.checkoutUrl === null || typeof value.checkoutUrl === 'string');
const isTicket = (value: unknown): boolean => isRecord(value) && nonEmpty(value.ticketId)
  && Number.isInteger(value.orden) && nonEmpty(value.servicioId) && nonEmpty(value.tramoId)
  && nonEmpty(value.origenNombre) && nonEmpty(value.destinoNombre) && typeof value.importe === 'number'
  && (value.numeroAsiento === null || Number.isInteger(value.numeroAsiento)) && nonEmpty(value.qrOpaco);
const isPurchase = (value: unknown): boolean => isRecord(value) && nonEmpty(value.compraId)
  && nonEmpty(value.intentoId) && nonEmpty(value.pagoId) && typeof value.importeTotal === 'number'
  && Array.isArray(value.tickets) && value.tickets.every(isTicket);
const isSync = (value: unknown): value is SincronizarPagoResponseDto => isRecord(value)
  && isPayment(value.pago) && (value.compra === null || isPurchase(value.compra));
const isPending = (value: unknown): value is IntentoPendienteResponseDto => isRecord(value)
  && isAttempt(value.intento) && (value.pago === null || (isPayment(value.pago) && value.pago.intentoId === value.intento.intentoId));
const attemptPath = (id: string): string | undefined => /^[A-Za-z0-9-]+$/.test(id)
  ? `/api/intentos-compra/${id}` : undefined;

export type PurchaseFinalization = 'confirmed' | 'refunded' | 'expired';

export function canFinishPurchase(state: string, holdUntil: string | undefined, paymentState: string | undefined, now: number): boolean {
  if (state === 'confirmed' || state === 'refunded') return true;
  if (state !== 'expired' && state !== 'rejected' && state !== 'error') return false;
  if (state === 'rejected' && paymentState !== 'rechazado') return false;
  if (paymentState !== undefined && paymentState !== 'rechazado') return false;
  if (!holdUntil) return false;
  const deadline = Date.parse(holdUntil);
  return Number.isFinite(deadline) && deadline <= now;
}

export class PurchaseService {
  private readonly authentication: IAuthenticationService;

  constructor(authentication: IAuthenticationService) { this.authentication = authentication; }

  async createAttempt(tramos: SeleccionTramoCompraDto[]): Promise<PurchaseResult<IniciarIntentoCompraResponseDto>> {
    if (tramos.length < 1 || tramos.some(item => !Number.isInteger(item.orden) || item.orden < 1 || !item.servicioId)) {
      return { ok: false, kind: 'error', message: 'El itinerario no contiene una selección válida.' };
    }
    const reply = await this.authentication.authorizedRequest({ method: 'POST', path: '/api/intentos-compra', body: { tramos } });
    if (reply.outcome === 'network' || reply.outcome === 'cancelled' || reply.outcome === 'invalidResponse') {
      return { ok: false, kind: 'ambiguous', message: ambiguousCreateMessage, status: reply.status };
    }
    if (reply.outcome !== 'response') return { ok: false, kind: 'error', message: reply.outcome === 'unauthenticated' ? unauthenticatedMessage : genericMessage, status: reply.status };
    if (reply.status === undefined || reply.status >= 500) return { ok: false, kind: 'ambiguous', message: ambiguousCreateMessage, status: reply.status };
    if (reply.status >= 200 && reply.status < 300) return isAttempt(reply.body)
      ? { ok: true, data: reply.body, status: reply.status }
      : { ok: false, kind: 'ambiguous', message: ambiguousCreateMessage, status: reply.status };
    return { ok: false, kind: 'error', message: genericMessage, status: reply.status };
  }

  // 200 -> pending attempt, 204 -> nothing pending (data null). Runs with the current session only.
  async getPendingAttempt(): Promise<PurchaseResult<IntentoPendienteResponseDto | null>> {
    const reply = await this.authentication.authorizedRequest({ method: 'GET', path: '/api/intentos-compra/pendiente' });
    if (reply.outcome === 'network' || reply.outcome === 'cancelled' || reply.outcome === 'invalidResponse') return { ok: false, kind: 'unknown', message: pendingMessage, status: reply.status };
    if (reply.outcome !== 'response') return { ok: false, kind: 'error', message: reply.outcome === 'unauthenticated' ? unauthenticatedMessage : genericMessage, status: reply.status };
    if (reply.status === 204) return { ok: true, data: null, status: 204 };
    if (reply.status === 200 && isPending(reply.body)) return { ok: true, data: reply.body, status: 200 };
    return { ok: false, kind: reply.status === undefined || reply.status >= 500 || reply.status === 200 ? 'unknown' : 'error', message: reply.status === 200 || (reply.status ?? 500) >= 500 ? pendingMessage : genericMessage, status: reply.status };
  }

  async getQuote(id: string): Promise<PurchaseResult<CotizarCompraResponseDto>> {
    const base = attemptPath(id);
    if (!base) return { ok: false, kind: 'error', message: genericMessage };
    const reply = await this.authentication.authorizedRequest({ method: 'GET', path: `${base}/cotizacion` });
    if (reply.outcome === 'network' || reply.outcome === 'cancelled') return { ok: false, kind: 'error', message: genericMessage };
    if (reply.outcome !== 'response') return { ok: false, kind: 'error', message: reply.outcome === 'unauthenticated' ? unauthenticatedMessage : genericMessage, status: reply.status };
    if (reply.status === 409) return { ok: false, kind: 'error', message: 'El intento venció o la selección ya no está disponible. El hold original no se extiende.', status: 409 };
    return reply.status === 200 && isQuote(reply.body)
      ? { ok: true, data: reply.body, status: reply.status }
      : { ok: false, kind: 'error', message: genericMessage, status: reply.status };
  }

  async startPayment(id: string, fingerprint: string): Promise<PurchaseStartResult> {
    const base = attemptPath(id);
    if (!base || !/^[a-fA-F0-9]{64}$/.test(fingerprint)) return { kind: 'error', message: 'La cotización aceptada no es válida.' };
    const reply: ApiReply = await this.authentication.authorizedRequest({
      method: 'POST', path: `${base}/pagos`, body: { huellaCotizacionAceptada: fingerprint },
    });
    if (reply.outcome === 'network' || reply.outcome === 'cancelled' || reply.outcome === 'invalidResponse') return { kind: 'unknown', message: 'No se pudo confirmar el inicio del pago. Verifique su estado antes de intentar otra acción.', status: reply.status };
    if (reply.outcome !== 'response') return { kind: 'error', message: reply.outcome === 'unauthenticated' ? unauthenticatedMessage : genericMessage, status: reply.status };
    if (reply.status === 409) {
      const current = await this.getQuote(id);
      return current.ok ? { kind: 'quoteChanged', quote: current.data } : { kind: 'error', message: current.message, status: current.status };
    }
    if (reply.status === undefined || reply.status >= 500) return { kind: 'unknown', message: 'No se pudo confirmar el inicio del pago. Verifique su estado antes de intentar otra acción.', status: reply.status };
    if (reply.status === 202) return { kind: 'unknown', message: 'El inicio del pago quedó pendiente. Verifique el estado de este mismo intento.', status: 202 };
    if (reply.status >= 200 && reply.status < 300) return isPayment(reply.body)
      ? { kind: 'started', payment: reply.body, status: reply.status }
      : { kind: 'unknown', message: 'No se pudo leer la respuesta de inicio. Verifique el estado del mismo intento.', status: reply.status };
    return { kind: 'error', message: genericMessage, status: reply.status };
  }

  async syncPayment(id: string): Promise<PurchaseResult<SincronizarPagoResponseDto>> {
    const base = attemptPath(id);
    if (!base) return { ok: false, kind: 'error', message: genericMessage };
    const reply = await this.authentication.authorizedRequest({ method: 'POST', path: `${base}/pagos/sincronizar` });
    if (reply.outcome === 'network' || reply.outcome === 'cancelled' || reply.outcome === 'invalidResponse') return { ok: false, kind: 'unknown', message: 'No se pudo verificar el pago. Conserve este intento y vuelva a intentar la verificación.', status: reply.status };
    if (reply.outcome !== 'response') return { ok: false, kind: 'error', message: reply.outcome === 'unauthenticated' ? unauthenticatedMessage : genericMessage, status: reply.status };
    if (reply.status === undefined || reply.status >= 500) return { ok: false, kind: 'unknown', message: 'No se pudo verificar el pago. Conserve este intento y reintente la verificación.', status: reply.status };
    if (reply.status === 202) return { ok: false, kind: 'unknown', message: 'El pago sigue pendiente. Vuelva a verificar el mismo intento.', status: 202 };
    return reply.status === 200 && isSync(reply.body)
      ? { ok: true, data: reply.body, status: reply.status }
      : { ok: false, kind: 'error', message: genericMessage, status: reply.status };
  }
}
