// Mirrors the DTOs of POST /api/validaciones-ticket (docs/reference/api.md). JSON is camelCase.

/** The five business outcomes; all are answered with HTTP 200. */
export type ResultadoValidacion = 'valido' | 'yaUtilizado' | 'vencido' | 'tramoIncorrecto' | 'codigoInexistente';

export interface ValidarTicketRequestDto {
  /** Content read from the QR, sent unchanged. */
  qrOpaco: string;
  /** The service (tramo) the collector chose; ids come from GET /api/ubicacion/cobrador/servicios. */
  servicioId: string;
}

export interface TicketValidadoDto {
  ticketId: string;
  servicioId: string;
  tramoId: string;
  origenNombre: string;
  destinoNombre: string;
  /** Uruguay local time without zone, e.g. 2026-11-13T07:00:00. */
  salida: string;
  /** Uruguay local time without zone. */
  llegada: string;
}

export interface ResultadoValidacionTicketDto {
  resultado: ResultadoValidacion;
  /** Absent in the API only for codigoInexistente; normalised to null. */
  ticket: TicketValidadoDto | null;
  /** valido: when it was consumed; yaUtilizado: first use; UTC with trailing Z. Normalised to null when absent. */
  usadoUtc: string | null;
}

export type TicketValidationFailure = 'unauthenticated' | 'forbidden' | 'invalid' | 'network' | 'server'
  | 'configuration' | 'invalidResponse' | 'cancelled';

export type TicketValidationResult<T> =
  | { ok: true; data: T }
  | { ok: false; failure: TicketValidationFailure; message: string; status?: number };
