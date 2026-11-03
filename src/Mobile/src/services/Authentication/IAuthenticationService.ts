import type { SolicitudIngresoDto } from '../../contracts/authentication';
import type { AuthenticationResult, SessionState } from '../../contracts/session';

export interface IAuthenticationService {
  getState(): SessionState;
  login(request: SolicitudIngresoDto): Promise<AuthenticationResult>;
  logout(): Promise<AuthenticationResult>;
  checkPassenger(): Promise<AuthenticationResult>;
  checkCollector(): Promise<AuthenticationResult>;
  discard(): void;
}
