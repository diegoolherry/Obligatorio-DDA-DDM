import type { ITicketValidationService } from './ITicketValidationService';
import type {
  ResultadoValidacion, ResultadoValidacionTicketDto, TicketValidadoDto, TicketValidationFailure, TicketValidationResult,
} from '../../contracts/ticketValidation';
import type { AuthorizedRequest, AuthorizedResult } from '../../contracts/session';

/** The only capability this service needs from the session: authenticated requests. */
export interface AuthorizedRequester {
  authorizedRequest(request: AuthorizedRequest): Promise<AuthorizedResult>;
}

const messages: Record<TicketValidationFailure, string> = {
  unauthenticated: 'La sesión no es válida o venció. Ingresá nuevamente como cobrador.',
  forbidden: 'No podés validar en este tramo: el servicio no está asignado a tu cuenta de cobrador. El ticket no se validó.',
  invalid: 'La API rechazó la validación: falta el código o el tramo.',
  network: 'No hubo respuesta del servidor. Todavía no sabemos si el ticket se validó.',
  server: 'La API no está disponible. Todavía no sabemos si el ticket se validó.',
  configuration: 'Configurá EXPO_PUBLIC_API_URL con la raíz HTTPS de la API, sin credenciales, query ni fragmento.',
  invalidResponse: 'Respuesta inválida del servidor. Todavía no sabemos si el ticket se validó.',
  cancelled: 'Solicitud descartada; sin confirmación remota.',
};

const results: readonly ResultadoValidacion[] = ['valido', 'yaUtilizado', 'vencido', 'tramoIncorrecto', 'codigoInexistente'];

function fail<T>(failure: TicketValidationFailure, status?: number): TicketValidationResult<T> {
  return status === undefined ? { ok: false, failure, message: messages[failure] }
    : { ok: false, failure, message: messages[failure], status };
}
function record(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}
function text(value: unknown): value is string {
  return typeof value === 'string' && value.length > 0;
}

function toTicket(value: unknown): TicketValidadoDto | undefined {
  if (!record(value) || !text(value.ticketId) || !text(value.servicioId) || !text(value.tramoId)
    || !text(value.origenNombre) || !text(value.destinoNombre) || !text(value.salida) || !text(value.llegada)) return undefined;
  return {
    ticketId: value.ticketId, servicioId: value.servicioId, tramoId: value.tramoId,
    origenNombre: value.origenNombre, destinoNombre: value.destinoNombre, salida: value.salida, llegada: value.llegada,
  };
}

function toValidation(value: unknown): ResultadoValidacionTicketDto | undefined {
  if (!record(value)) return undefined;
  const resultado = results.find(item => item === value.resultado);
  if (resultado === undefined) return undefined;
  const used = value.usadoUtc;
  if (!(used === undefined || used === null || text(used))) return undefined;
  const hasTicket = value.ticket !== undefined && value.ticket !== null;
  const ticket = hasTicket ? toTicket(value.ticket) : null;
  if (ticket === undefined) return undefined;
  // Only an unknown code may come without a ticket; a ticket-less success is never trusted.
  if (ticket === null && resultado !== 'codigoInexistente') return undefined;
  return { resultado, ticket, usadoUtc: used ?? null };
}

export class TicketValidationService implements ITicketValidationService {
  private readonly session: AuthorizedRequester;

  constructor(session: AuthorizedRequester) {
    this.session = session;
  }

  async validate(qrOpaco: string, servicioId: string): Promise<TicketValidationResult<ResultadoValidacionTicketDto>> {
    if (!qrOpaco || !servicioId) return fail('invalid');
    const result = await this.session.authorizedRequest({
      method: 'POST', path: '/api/validaciones-ticket', body: { qrOpaco, servicioId },
    });
    switch (result.outcome) {
      case 'unauthenticated': return fail('unauthenticated', result.status);
      case 'configuration': return fail('configuration');
      case 'network': return fail('network');
      case 'cancelled': return fail('cancelled');
      case 'invalidRequest': return fail('invalid');
      case 'invalidResponse': return fail('invalidResponse', result.status);
      case 'response': break;
    }
    const status = result.status;
    if (status === undefined) return fail('invalidResponse');
    if (status === 200) {
      const data = toValidation(result.body);
      return data === undefined ? fail('invalidResponse', status) : { ok: true, data };
    }
    if (status === 400) return fail('invalid', status);
    if (status === 401) return fail('unauthenticated', status);
    if (status === 403) return fail('forbidden', status);
    if (status >= 500) return fail('server', status);
    return fail('invalidResponse', status);
  }
}
