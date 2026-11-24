// Pure display and view-state helpers for «Mis tickets». No React Native imports.
import type {
  EstadoTicket, TicketPasajeroDto, TicketsFilter, TicketsViewState,
} from '../../contracts/tickets';

const filterNames: Record<TicketsFilter, string> = { vigentes: 'Vigentes', anteriores: 'Anteriores' };

/** «Vigentes (2)»; no count while loading or after an error. */
export function filterLabel(filter: TicketsFilter, count: number | undefined): string {
  return count === undefined ? filterNames[filter] : `${filterNames[filter]} (${count})`;
}

export function countFor(state: TicketsViewState, filter: TicketsFilter): number | undefined {
  return state.status === 'ready' ? state.data[filter].length : undefined;
}

export function visibleTickets(state: TicketsViewState, filter: TicketsFilter): TicketPasajeroDto[] {
  return state.status === 'ready' ? state.data[filter] : [];
}

/** True only when the list loaded and the selected group has no tickets. */
export function isEmptyFilter(state: TicketsViewState, filter: TicketsFilter): boolean {
  return state.status === 'ready' && state.data[filter].length === 0;
}

export type TicketBadgeKind = 'success' | 'neutral' | 'error';

/** Label plus icon, so the state never depends on color alone. */
export function statusBadge(state: EstadoTicket): { label: string; icon: string; kind: TicketBadgeKind } {
  switch (state) {
    case 'sinUsar': return { label: 'Sin usar', icon: '✓', kind: 'success' };
    case 'utilizado': return { label: 'Utilizado', icon: '✓✓', kind: 'neutral' };
    case 'vencido': return { label: 'Vencido', icon: '✕', kind: 'error' };
  }
}

const weekdays = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'];

/** HH:mm from the Uruguay local value (the API already sends it without zone). */
export function formatTime(local: string): string {
  const match = /T(\d{2}):(\d{2})/.exec(local);
  return match ? `${match[1]}:${match[2]}` : '--:--';
}

/** «Vie 13/11/2026» from the local date part; no time-zone shifting. */
export function formatDate(local: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(local);
  if (!match) return 'Fecha no disponible';
  const [year, month, day] = [Number(match[1]), Number(match[2]), Number(match[3])];
  const weekday = weekdays[new Date(Date.UTC(year, month - 1, day)).getUTCDay()];
  return `${weekday} ${match[3]}/${match[2]}/${match[1]}`;
}

const notUsable = 'Ya no sirve para viajar.';
// Uruguay has no daylight saving time, so a fixed UTC-3 offset is exact.
const uruguayOffsetMs = -3 * 60 * 60 * 1000;

/** «Se validó el 11/11 a las 09:17. …» in Uruguay time from the UTC validation instant. */
export function usedAtMessage(usadoUtc: string | null): string {
  if (usadoUtc !== null) {
    // The API sends UTC; tolerate a missing zone suffix instead of reading it as device-local time.
    const instant = /(Z|[+-]\d{2}:\d{2})$/.test(usadoUtc) ? usadoUtc : `${usadoUtc}Z`;
    const milliseconds = Date.parse(instant);
    if (Number.isFinite(milliseconds)) {
      const iso = new Date(milliseconds + uruguayOffsetMs).toISOString();
      return `Se validó el ${iso.slice(8, 10)}/${iso.slice(5, 7)} a las ${iso.slice(11, 16)}. ${notUsable}`;
    }
  }
  return `Este ticket ya se validó. ${notUsable}`;
}

export type QrMode = 'active' | 'dimmed';

/** Only a sin usar ticket shows a usable QR; the others show it dimmed with the reason. */
export function detailPresentation(ticket: TicketPasajeroDto): { qr: QrMode; notice: string } {
  switch (ticket.estado) {
    case 'sinUsar': return { qr: 'active', notice: 'Presentá este código al cobrador' };
    case 'utilizado': return { qr: 'dimmed', notice: usedAtMessage(ticket.usadoUtc) };
    case 'vencido': return { qr: 'dimmed', notice: 'Este ticket venció y ya no es válido para viajar.' };
  }
}

export function routeTitle(ticket: TicketPasajeroDto): string {
  return `${ticket.origenNombre} a ${ticket.destinoNombre}`;
}

export function cardAccessibilityLabel(ticket: TicketPasajeroDto): string {
  return `${routeTitle(ticket)}, ${statusBadge(ticket.estado).label}, ${formatTime(ticket.salida)}, ${formatDate(ticket.salida)}`;
}
