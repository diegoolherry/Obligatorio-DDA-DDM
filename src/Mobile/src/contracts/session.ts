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
