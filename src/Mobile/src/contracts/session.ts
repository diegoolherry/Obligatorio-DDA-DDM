export type AuthenticationOutcome = 'success' | 'validation' | 'rejected' | 'unauthenticated'
  | 'denied' | 'configuration' | 'server' | 'network' | 'invalidResponse' | 'busy' | 'cancelled';

export interface AuthenticationResult {
  outcome: AuthenticationOutcome;
  message: string;
  status?: number;
}

// Deliberately contains neither token nor public identifier.
export interface SessionState {
  authenticated: boolean;
  logoutPending: boolean;
}

/** Request sent with the session credential, which the service never hands out. */
export interface AuthorizedRequest {
  method: 'GET' | 'POST';
  /** Path below the API origin, for example /api/ubicacion/servicios. */
  path: string;
  body?: unknown;
}

export type AuthorizedOutcome = 'response' | 'unauthenticated' | 'configuration' | 'network'
  | 'invalidResponse' | 'invalidRequest' | 'cancelled';

export interface AuthorizedResult {
  outcome: AuthorizedOutcome;
  /** HTTP status, present when the API answered. */
  status?: number;
  /** Parsed JSON body, only for 200 and 201 responses. */
  body?: unknown;
}
