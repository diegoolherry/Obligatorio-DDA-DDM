import type { IAuthenticationService } from '../Authentication/IAuthenticationService';
import type {
  CotizarCompraResponseDto, IniciarIntentoCompraResponseDto, PagoResponseDto,
  PurchaseResult, PurchaseStartResult, SeleccionTramoCompraDto, SincronizarPagoResponseDto,
} from '../../contracts/purchase';

const genericMessage = 'La API no pudo completar la compra. Revise la conexión e intente verificar el estado.';
const unauthenticatedMessage = 'Ingrese con tu cuenta de pasajero para continuar.';
const ambiguousCreateMessage = 'No se pudo confirmar la creación del intento. No vuelva a crearlo automáticamente.';

type ApiReply = { outcome: string; status?: number; body?: unknown };
const isRecord = (value: unknown): value is Record<string, unknown> =>
  typeof value === 'object' && value !== null && !Array.isArray(value);
const nonEmpty = (value: unknown): value is string => typeof value === 'string' && value.length > 0;
const isItinerary = (value: unknown): value is { tramos: unknown[]; precioTotal: number } => isRecord(value)
  && Array.isArray(value.tramos) && typeof value.precioTotal === 'number';
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
const attemptPath = (id: string): string | undefined => /^[A-Za-z0-9-]+$/.test(id)
  ? `/api/intentos-compra/${id}` : undefined;

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
