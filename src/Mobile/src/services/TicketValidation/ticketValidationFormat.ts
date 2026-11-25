// Pure display helpers for the validation results. No React Native imports.

const weekdays = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'] as const;

/** «07:00 · Vie 13/11/2026» from a local time without zone (the API already sends Uruguay time). */
export function formatServiceDay(local: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/.exec(local);
  if (!match) return 'Fecha no disponible';
  const [, year, month, day, hour, minute] = match;
  // Weekday of the calendar date only: UTC arithmetic avoids any device time-zone shift.
  const weekday = weekdays[new Date(Date.UTC(Number(year), Number(month) - 1, Number(day))).getUTCDay()];
  return `${hour}:${minute} · ${weekday} ${day}/${month}/${year}`;
}

/** «13/11 07:02» from «dd/MM/yyyy HH:mm:ss» (see formatUruguayDateTime). */
export function shortDateTime(formatted: string | undefined): string {
  const match = formatted === undefined ? null : /^(\d{2}\/\d{2})\/\d{4} (\d{2}:\d{2})/.exec(formatted);
  return match ? `${match[1]} ${match[2]}` : 'no disponible';
}

// ---- Result presentation (pure; the components only render this) ---------------------------

export type ResultTone = 'success' | 'error' | 'warning';

export interface ResultRow { label: string; value: string }

export interface ResultView {
  tone: ResultTone;
  /** Text glyph that repeats the meaning of the title (color is never the only signal). */
  glyph: string;
  title: string;
  message: string;
  rows: ResultRow[];
}

/** The tramo (service) the collector chose; shown as context in every result. */
export interface ChosenService { origen: string; destino: string; salida: string }

/** Minimal shape of a validation answer (structurally the contract DTO; kept local so this file has no imports). */
export interface ResultInput {
  resultado: 'valido' | 'yaUtilizado' | 'vencido' | 'tramoIncorrecto' | 'codigoInexistente';
  ticket: { origenNombre: string; destinoNombre: string; salida: string } | null;
  usadoUtc: string | null;
}

/** Formats a UTC instant as «dd/MM/yyyy HH:mm:ss» in Uruguay time (formatUruguayDateTime). */
export type InstantFormatter = (utc: string) => string | undefined;

function chosenRows(chosen: ChosenService): ResultRow[] {
  return [
    { label: 'Tramo', value: `${chosen.origen} a ${chosen.destino}` },
    { label: 'Servicio', value: formatServiceDay(chosen.salida) },
  ];
}

export function describeResult(result: ResultInput, chosen: ChosenService, formatInstant: InstantFormatter): ResultView {
  const { ticket, usadoUtc } = result;
  const ticketRoute = ticket ? `${ticket.origenNombre} a ${ticket.destinoNombre}` : 'no disponible';
  const ticketDay = ticket ? formatServiceDay(ticket.salida) : 'no disponible';
  const when = shortDateTime(usadoUtc === null ? undefined : formatInstant(usadoUtc));
  switch (result.resultado) {
    case 'valido':
      return {
        tone: 'success', glyph: '✓', title: 'Ticket válido', message: 'Confirmado por el servidor. Puede viajar.',
        rows: [{ label: 'Tramo', value: ticketRoute }, { label: 'Servicio', value: ticketDay }, { label: 'Validado', value: when }],
      };
    case 'yaUtilizado':
      return {
        tone: 'error', glyph: '✕', title: 'Ya utilizado', message: 'Este ticket ya se validó. No es válido para viajar.',
        rows: [{ label: 'Tramo', value: ticketRoute }, { label: 'Servicio', value: ticketDay }, { label: 'Primer uso', value: when }],
      };
    case 'vencido':
      return {
        tone: 'error', glyph: '▦', title: 'Vencido', message: 'El ticket es de un servicio que ya pasó. No es válido para viajar.',
        rows: [...chosenRows(chosen), { label: 'Servicio del ticket', value: ticketDay }],
      };
    case 'tramoIncorrecto':
      return {
        tone: 'error', glyph: '⇄', title: 'Tramo incorrecto', message: 'Este ticket es de otro tramo. No es válido en este tramo.',
        rows: [...chosenRows(chosen), { label: 'Tramo del ticket', value: ticketRoute }, { label: 'Ticket', value: 'No se consumió · sigue vigente' }],
      };
    case 'codigoInexistente':
      return {
        tone: 'error', glyph: '?', title: 'Código inexistente', message: 'El código no corresponde a ningún ticket. No es válido para viajar.',
        rows: [...chosenRows(chosen), { label: 'Qué hacer', value: 'Pedí al pasajero que abra el ticket en su app' }],
      };
  }
}

/** «No se pudo confirmar»: nothing was confirmed, so this is never presented as a result. */
export function describeUnconfirmed(message: string, chosen: ChosenService): ResultView {
  return {
    tone: 'warning', glyph: '!', title: 'No se pudo confirmar', message,
    rows: [...chosenRows(chosen), { label: 'Estado', value: 'Sin confirmar' }],
  };
}

/** Failures where sending the same code again can help; otherwise the collector has to act first. */
export function canRetryFailure(failure: string): boolean {
  return failure === 'network' || failure === 'server' || failure === 'invalidResponse' || failure === 'cancelled';
}
