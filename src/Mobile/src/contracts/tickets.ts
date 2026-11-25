// Mirrors the DTOs of the API (DTOs/Operational/TicketPasajeroDto). JSON is camelCase.

export type EstadoTicket = 'sinUsar' | 'utilizado' | 'vencido';

export type PoliticaAsientoTicket = 'sinAsiento' | 'asignacionAutomatica';

export interface TicketPasajeroDto {
  ticketId: string;
  estado: EstadoTicket;
  servicioId: string;
  tramoId: string;
  origenNombre: string;
  destinoNombre: string;
  /** Uruguay local time without zone, e.g. 2026-11-13T07:00:00. */
  salida: string;
  /** Uruguay local time without zone. */
  llegada: string;
  politicaAsiento: PoliticaAsientoTicket;
  numeroAsiento: number | null;
  /** Opaque code to render as a QR. Never log it or show it as text. */
  qrOpaco: string;
  /** UTC instant of the validation; only with estado «utilizado». */
  usadoUtc: string | null;
}

/** GET /api/tickets (rol Pasajero). */
export interface ListaTicketsPasajeroDto {
  /** Sin usar y no vencidos, por salida ascendente. */
  vigentes: TicketPasajeroDto[];
  /** Utilizados y vencidos, el más reciente primero. */
  anteriores: TicketPasajeroDto[];
}

export type TicketsFailure = 'unauthenticated' | 'forbidden' | 'network' | 'server'
  | 'configuration' | 'invalidResponse' | 'cancelled';

export type TicketsResult<T> =
  | { ok: true; data: T }
  | { ok: false; failure: TicketsFailure; message: string; status?: number };

export type TicketsFilter = 'vigentes' | 'anteriores';

export type TicketsViewState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; data: ListaTicketsPasajeroDto };
