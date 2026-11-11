import type { SolicitudIngresoDto } from '../../contracts/authentication';
import type { RolIngreso } from '../../contracts/authentication';
import type {
  AuthenticationResult, AuthorizedRequest, AuthorizedResult, SessionState,
} from '../../contracts/session';

export interface IAuthenticationService {
  getState(): SessionState;
  /** Roles of the confirmed session; they are not secret. Empty without a session. */
  getRoles(): readonly RolIngreso[];
  login(request: SolicitudIngresoDto): Promise<AuthenticationResult>;
  logout(): Promise<AuthenticationResult>;
  checkPassenger(): Promise<AuthenticationResult>;
  checkCollector(): Promise<AuthenticationResult>;
  /** Sends a request with the session credential; the token never leaves the service. */
  authorizedRequest(request: AuthorizedRequest): Promise<AuthorizedResult>;
  discard(): void;
}
