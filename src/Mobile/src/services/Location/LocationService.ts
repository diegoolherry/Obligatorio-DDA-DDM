import type { ILocationService } from './ILocationService';
import type {
  ConsultaUbicacionResponse, LocationFailure, LocationResult, ParametrosUbicacionDto,
  PublicacionPosicionResponse, ServicioUbicacionDto, UltimaPosicionDto,
} from '../../contracts/location';
import type { AuthorizedRequest, AuthorizedResult } from '../../contracts/session';

/** The only capability this service needs from the session: authenticated requests. */
export interface AuthorizedRequester {
  authorizedRequest(request: AuthorizedRequest): Promise<AuthorizedResult>;
}

const messages: Record<LocationFailure, string> = {
  unauthenticated: 'La sesión no es válida o venció. Ingresá nuevamente.',
  forbidden: 'Tu cuenta no tiene permiso para esta operación o no está asignada a este servicio.',
  notFound: 'El servicio ya no existe. Actualizá la lista.',
  invalid: 'La API rechazó los datos enviados. Revisá la posición e intentá de nuevo.',
  network: 'No se pudo conectar con la API o se agotó el tiempo. Revisá la conexión y reintentá.',
  server: 'La API no está disponible. Reintentá en unos minutos.',
  configuration: 'Configurá EXPO_PUBLIC_API_URL con la raíz HTTPS de la API, sin credenciales, query ni fragmento.',
  invalidResponse: 'Respuesta inválida del servidor. Reintentá.',
  cancelled: 'Solicitud descartada; sin confirmación remota.',
};

function fail<T>(failure: LocationFailure, status?: number): LocationResult<T> {
  return status === undefined ? { ok: false, failure, message: messages[failure] }
    : { ok: false, failure, message: messages[failure], status };
}

function record(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}
function text(value: unknown): value is string {
  return typeof value === 'string' && value.length > 0;
}
function num(value: unknown): value is number {
  return typeof value === 'number' && Number.isFinite(value);
}
function textOrNull(value: unknown): value is string | null {
  return value === null || typeof value === 'string';
}

function toService(value: unknown): ServicioUbicacionDto | undefined {
  if (!record(value) || !text(value.id) || !text(value.origen) || !text(value.destino)
    || !text(value.salida) || !text(value.llegada)) return undefined;
  return { id: value.id, origen: value.origen, destino: value.destino, salida: value.salida, llegada: value.llegada };
}
function toServices(value: unknown): ServicioUbicacionDto[] | undefined {
  if (!Array.isArray(value)) return undefined;
  const list = value.map(toService);
  return list.every((item): item is ServicioUbicacionDto => item !== undefined) ? list : undefined;
}
function toPublication(value: unknown): PublicacionPosicionResponse | undefined {
  if (!record(value) || !text(value.id) || !text(value.servicioId) || !num(value.latitud) || !num(value.longitud)
    || !text(value.recibidaUtc) || !num(value.periodicidadSegundos) || value.periodicidadSegundos <= 0
    || !(value.intervaloSegundos === null || num(value.intervaloSegundos))
    || !(value.cumplePeriodicidad === null || typeof value.cumplePeriodicidad === 'boolean')) return undefined;
  return {
    id: value.id, servicioId: value.servicioId, latitud: value.latitud, longitud: value.longitud,
    recibidaUtc: value.recibidaUtc, intervaloSegundos: value.intervaloSegundos,
    periodicidadSegundos: value.periodicidadSegundos, cumplePeriodicidad: value.cumplePeriodicidad,
  };
}
function toPosition(value: unknown): UltimaPosicionDto | null | undefined {
  if (value === null) return null;
  if (!record(value) || !num(value.latitud) || !num(value.longitud) || !text(value.recibidaUtc)
    || !num(value.antiguedadSegundos)) return undefined;
  return { latitud: value.latitud, longitud: value.longitud, recibidaUtc: value.recibidaUtc, antiguedadSegundos: value.antiguedadSegundos };
}
function toParameters(value: unknown): ParametrosUbicacionDto | undefined {
  if (!record(value) || !num(value.periodicidadSegundos) || !num(value.umbralDesactualizacionSegundos)
    || !num(value.velocidadPromedioKmH) || !num(value.toleranciaDemoraMinutos)) return undefined;
  return {
    periodicidadSegundos: value.periodicidadSegundos,
    umbralDesactualizacionSegundos: value.umbralDesactualizacionSegundos,
    velocidadPromedioKmH: value.velocidadPromedioKmH, toleranciaDemoraMinutos: value.toleranciaDemoraMinutos,
  };
}
function toQuery(value: unknown): ConsultaUbicacionResponse | undefined {
  if (!record(value)) return undefined;
  const service = toService(value.servicio);
  const position = toPosition(value.ultimaPosicion);
  const parameters = toParameters(value.parametros);
  const state = value.estadoDemora;
  if (!service || position === undefined || !parameters || typeof value.desactualizada !== 'boolean'
    || typeof value.datosFicticios !== 'boolean' || !textOrNull(value.llegadaAproximada) || !textOrNull(value.motivo)
    || (state !== 'sinDatos' && state !== 'enHorario' && state !== 'conDemora')) return undefined;
  return {
    servicio: service, ultimaPosicion: position, desactualizada: value.desactualizada,
    llegadaAproximada: value.llegadaAproximada, estadoDemora: state, motivo: value.motivo,
    parametros: parameters, datosFicticios: value.datosFicticios,
  };
}

/** API accepts up to 6 decimals; round so a GPS reading is never rejected for precision. */
export function roundCoordinate(value: number): number {
  return Math.round(value * 1e6) / 1e6;
}

export class LocationService implements ILocationService {
  private readonly session: AuthorizedRequester;

  constructor(session: AuthorizedRequester) {
    this.session = session;
  }

  listCollectorServices(): Promise<LocationResult<ServicioUbicacionDto[]>> {
    return this.send({ method: 'GET', path: '/api/ubicacion/cobrador/servicios' }, [200], toServices);
  }

  async publishPosition(serviceId: string, latitude: number, longitude: number): Promise<LocationResult<PublicacionPosicionResponse>> {
    if (!serviceId || !num(latitude) || !num(longitude)
      || Math.abs(latitude) > 90 || Math.abs(longitude) > 180) return fail('invalid');
    return this.send({
      method: 'POST', path: `/api/ubicacion/servicios/${encodeURIComponent(serviceId)}/posiciones`,
      body: { latitud: roundCoordinate(latitude), longitud: roundCoordinate(longitude) },
    }, [201], toPublication);
  }

  listPassengerServices(): Promise<LocationResult<ServicioUbicacionDto[]>> {
    return this.send({ method: 'GET', path: '/api/ubicacion/servicios' }, [200], toServices);
  }

  async getServiceLocation(serviceId: string): Promise<LocationResult<ConsultaUbicacionResponse>> {
    if (!serviceId) return fail('invalid');
    return this.send({ method: 'GET', path: `/api/ubicacion/servicios/${encodeURIComponent(serviceId)}` }, [200], toQuery);
  }

  private async send<T>(request: AuthorizedRequest, success: number[], parse: (body: unknown) => T | undefined): Promise<LocationResult<T>> {
    const result = await this.session.authorizedRequest(request);
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
    if (success.includes(status)) {
      const data = parse(result.body);
      return data === undefined ? fail('invalidResponse', status) : { ok: true, data };
    }
    if (status === 400) return fail('invalid', status);
    if (status === 401) return fail('unauthenticated', status);
    if (status === 403) return fail('forbidden', status);
    if (status === 404) return fail('notFound', status);
    if (status >= 500) return fail('server', status);
    return fail('invalidResponse', status);
  }
}
