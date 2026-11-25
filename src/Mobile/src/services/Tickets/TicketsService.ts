import type { ITicketsService } from './ITicketsService';
import type {
  EstadoTicket, ListaTicketsPasajeroDto, TicketPasajeroDto, TicketsFailure, TicketsResult,
} from '../../contracts/tickets';
import type { AuthorizedRequest, AuthorizedResult } from '../../contracts/session';

/** The only capability this service needs from the session: authenticated requests. */
export interface AuthorizedRequester {
  authorizedRequest(request: AuthorizedRequest): Promise<AuthorizedResult>;
}

const messages: Record<TicketsFailure, string> = {
  unauthenticated: 'La sesión no es válida o venció. Ingresá nuevamente.',
  forbidden: 'Tu cuenta no tiene permiso para ver tickets. Ingresá con una cuenta de pasajero.',
  network: 'No se pudo conectar con la API o se agotó el tiempo. Revisá la conexión y reintentá.',
  server: 'La API no está disponible. Reintentá en unos minutos.',
  configuration: 'Configurá EXPO_PUBLIC_API_URL con la raíz HTTPS de la API, sin credenciales, query ni fragmento.',
  invalidResponse: 'Respuesta inválida del servidor. Reintentá.',
  cancelled: 'Solicitud descartada; sin confirmación remota.',
};

function fail<T>(failure: TicketsFailure, status?: number): TicketsResult<T> {
  return status === undefined ? { ok: false, failure, message: messages[failure] }
    : { ok: false, failure, message: messages[failure], status };
}

function record(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}
function text(value: unknown): value is string {
  return typeof value === 'string' && value.length > 0;
}
function isState(value: unknown): value is EstadoTicket {
  return value === 'sinUsar' || value === 'utilizado' || value === 'vencido';
}

function toTicket(value: unknown): TicketPasajeroDto | undefined {
  if (!record(value) || !text(value.ticketId) || !isState(value.estado) || !text(value.servicioId)
    || !text(value.tramoId) || !text(value.origenNombre) || !text(value.destinoNombre)
    || !text(value.salida) || !text(value.llegada) || !text(value.qrOpaco)
    || (value.politicaAsiento !== 'sinAsiento' && value.politicaAsiento !== 'asignacionAutomatica')
    || !(value.numeroAsiento === null || value.numeroAsiento === undefined || Number.isInteger(value.numeroAsiento))
    || !(value.usadoUtc === null || value.usadoUtc === undefined || typeof value.usadoUtc === 'string')) return undefined;
  return {
    ticketId: value.ticketId, estado: value.estado, servicioId: value.servicioId, tramoId: value.tramoId,
    origenNombre: value.origenNombre, destinoNombre: value.destinoNombre, salida: value.salida, llegada: value.llegada,
    politicaAsiento: value.politicaAsiento, numeroAsiento: (value.numeroAsiento as number | null | undefined) ?? null,
    qrOpaco: value.qrOpaco, usadoUtc: (value.usadoUtc as string | null | undefined) ?? null,
  };
}
function toTickets(value: unknown): TicketPasajeroDto[] | undefined {
  if (!Array.isArray(value)) return undefined;
  const list = value.map(toTicket);
  return list.every((item): item is TicketPasajeroDto => item !== undefined) ? list : undefined;
}
function toList(value: unknown): ListaTicketsPasajeroDto | undefined {
  if (!record(value)) return undefined;
  const vigentes = toTickets(value.vigentes);
  const anteriores = toTickets(value.anteriores);
  return vigentes && anteriores ? { vigentes, anteriores } : undefined;
}

export class TicketsService implements ITicketsService {
  private readonly session: AuthorizedRequester;

  constructor(session: AuthorizedRequester) {
    this.session = session;
  }

  async listTickets(): Promise<TicketsResult<ListaTicketsPasajeroDto>> {
    const result = await this.session.authorizedRequest({ method: 'GET', path: '/api/tickets' });
    switch (result.outcome) {
      case 'unauthenticated': return fail('unauthenticated', result.status);
      case 'configuration': return fail('configuration');
      case 'network': return fail('network');
      case 'cancelled': return fail('cancelled');
      case 'invalidRequest': return fail('invalidResponse');
      case 'invalidResponse': return fail('invalidResponse', result.status);
      case 'response': break;
    }
    const status = result.status;
    if (status === undefined) return fail('invalidResponse');
    if (status === 200) {
      const data = toList(result.body);
      return data === undefined ? fail('invalidResponse', status) : { ok: true, data };
    }
    if (status === 401) return fail('unauthenticated', status);
    if (status === 403) return fail('forbidden', status);
    if (status >= 500) return fail('server', status);
    return fail('invalidResponse', status);
  }
}
