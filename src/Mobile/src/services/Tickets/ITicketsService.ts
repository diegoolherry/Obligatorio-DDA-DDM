import type { ListaTicketsPasajeroDto, TicketsResult } from '../../contracts/tickets';

export interface ITicketsService {
  /** Pasajero: the signed-in passenger's tickets, already grouped by the API into vigentes and anteriores. */
  listTickets(): Promise<TicketsResult<ListaTicketsPasajeroDto>>;
}
