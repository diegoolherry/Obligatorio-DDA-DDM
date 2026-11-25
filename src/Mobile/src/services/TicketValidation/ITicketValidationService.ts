import type { ResultadoValidacionTicketDto, TicketValidationResult } from '../../contracts/ticketValidation';

export interface ITicketValidationService {
  /**
   * Cobrador: asks the API to validate (and consume) a ticket for the chosen service.
   * A result is only returned when the server answered; any other outcome is a failure.
   */
  validate(qrOpaco: string, servicioId: string): Promise<TicketValidationResult<ResultadoValidacionTicketDto>>;
}
