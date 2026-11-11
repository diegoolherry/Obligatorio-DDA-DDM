// Mirrors the DTOs of the API (DTOs/Location). JSON is camelCase.

export type EstadoDemora = 'sinDatos' | 'enHorario' | 'conDemora';

export interface ServicioUbicacionDto {
  id: string;
  origen: string;
  destino: string;
  /** Uruguay local time without zone, e.g. 2026-11-11T01:30:00. */
  salida: string;
  /** Uruguay local time without zone. */
  llegada: string;
}

export interface PublicarPosicionRequest {
  /** Rounded to 6 decimals before sending. */
  latitud: number;
  longitud: number;
}

export interface PublicacionPosicionResponse {
  id: string;
  servicioId: string;
  latitud: number;
  longitud: number;
  /** UTC with trailing Z. */
  recibidaUtc: string;
  intervaloSegundos: number | null;
  periodicidadSegundos: number;
  cumplePeriodicidad: boolean | null;
}

export interface UltimaPosicionDto {
  latitud: number;
  longitud: number;
  /** UTC with trailing Z. */
  recibidaUtc: string;
  antiguedadSegundos: number;
}

export interface ParametrosUbicacionDto {
  periodicidadSegundos: number;
  umbralDesactualizacionSegundos: number;
  velocidadPromedioKmH: number;
  toleranciaDemoraMinutos: number;
}

export interface ConsultaUbicacionResponse {
  servicio: ServicioUbicacionDto;
  ultimaPosicion: UltimaPosicionDto | null;
  desactualizada: boolean;
  /** Uruguay local time without zone; null without fresh data or a configured destination. */
  llegadaAproximada: string | null;
  estadoDemora: EstadoDemora;
  motivo: string | null;
  parametros: ParametrosUbicacionDto;
  datosFicticios: boolean;
}

export type LocationFailure = 'unauthenticated' | 'forbidden' | 'notFound' | 'invalid'
  | 'network' | 'server' | 'configuration' | 'invalidResponse' | 'cancelled';

export type LocationResult<T> =
  | { ok: true; data: T }
  | { ok: false; failure: LocationFailure; message: string; status?: number };
