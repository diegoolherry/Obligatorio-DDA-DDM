import type { IAuthenticationService } from './IAuthenticationService';
import type { SolicitudIngresoDto } from '../../contracts/authentication';
import type { AuthenticationResult, SessionState } from '../../contracts/session';

type Operation = 'ingreso' | 'salida' | 'prueba-pasajero';
const requiredEmail = 'El correo es obligatorio.';
const requiredPassword = 'La contraseña es obligatoria.';
const knownValidation = [requiredEmail, 'El correo no es válido.', requiredPassword];
const unauthenticated: AuthenticationResult = {
  outcome: 'unauthenticated', message: 'La sesión no es válida o venció. Ingresá nuevamente.',
};
const cancelled: AuthenticationResult = { outcome: 'cancelled', message: 'Solicitud descartada; sin confirmación remota.' };
const invalid: AuthenticationResult = { outcome: 'invalidResponse', message: 'Respuesta inválida del servidor. Reintentá.' };

function record(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}
function validIdentity(value: unknown): value is Record<string, unknown> {
  return record(value) && typeof value.identificadorUsuario === 'string'
    && /^[A-Za-z0-9_-]{43}$/.test(value.identificadorUsuario)
    && Array.isArray(value.roles)
    && value.roles.every(role => ['pasajero', 'cobrador', 'administrador'].includes(role));
}
function expiry(value: unknown): number {
  if (typeof value !== 'string' || !/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{6}Z$/.test(value)) return NaN;
  const milliseconds = Date.parse(value);
  // Date.parse accepts impossible calendar days by rolling forward; reject them.
  if (!Number.isFinite(milliseconds) || new Date(milliseconds).toISOString() !== value.slice(0, 23) + 'Z') return NaN;
  return milliseconds;
}
function apiRoot(value?: string): string | undefined {
  try {
    if (!value || !/^https:\/\/[^/@\s\\?#]+\/?$/.test(value)) return undefined;
    const url = new URL(value);
    if (url.protocol !== 'https:' || !url.hostname || url.username || url.password) return undefined;
    // Configuration describes the API origin, not a route prefix.
    if (url.pathname !== '/') return undefined;
    return url.origin;
  } catch { return undefined; }
}

export class AuthenticationService implements IAuthenticationService {
  private root: string | undefined;
  private transport: typeof fetch;
  private clock: () => number;
  private timeoutMs: number;
  private token: string | undefined;
  private expiresAt = 0;
  private logoutPending = false;
  private active: AbortController | undefined;
  private generation = 0;

  constructor(url?: string, transport: typeof fetch = fetch, clock: () => number = Date.now, timeoutMs = 12000) {
    this.root = apiRoot(url);
    this.transport = transport;
    this.clock = clock;
    this.timeoutMs = timeoutMs;
  }

  getState(): SessionState {
    if (this.token && this.clock() >= this.expiresAt) this.discard();
    return { authenticated: this.token !== undefined && !this.logoutPending, logoutPending: this.logoutPending };
  }

  discard(): void {
    this.generation++;
    this.token = undefined;
    this.expiresAt = 0;
    this.logoutPending = false;
    this.active?.abort();
    this.active = undefined;
  }

  async login(request: SolicitudIngresoDto): Promise<AuthenticationResult> {
    return this.perform('ingreso', request);
  }
  async logout(): Promise<AuthenticationResult> {
    return this.perform('salida');
  }
  async checkPassenger(): Promise<AuthenticationResult> {
    return this.perform('prueba-pasajero');
  }

  private async perform(operation: Operation, request?: SolicitudIngresoDto): Promise<AuthenticationResult> {
    if (this.active) return { outcome: 'busy', message: 'Hay una solicitud en curso.' };
    if (!this.root) return { outcome: 'configuration', message: 'Configurá EXPO_PUBLIC_API_URL con la raíz HTTPS de la API, sin credenciales, query ni fragmento.' };
    const state = this.getState();
    if (operation === 'ingreso') {
      if (state.authenticated || state.logoutPending) return { outcome: 'denied', message: 'Cerrá la sesión actual antes de ingresar.' };
      const messages: string[] = [];
      if (!request?.correo.trim()) messages.push(requiredEmail);
      if (!request?.contrasena.trim()) messages.push(requiredPassword);
      if (messages.length) return { outcome: 'validation', message: messages.join(' ') };
    } else if (!this.token || (operation !== 'salida' && this.logoutPending)) return { ...unauthenticated };

    const controller = new AbortController();
    this.active = controller;
    const generation = this.generation;
    const headers: Record<string, string> = operation === 'ingreso'
      ? { 'Content-Type': 'application/json' } : { Authorization: `Bearer ${this.token}` };
    if (operation === 'salida') this.logoutPending = true;
    let timer: ReturnType<typeof setTimeout> | undefined;
    let removeAbort = () => {};
    try {
      // Bound both fetch and body consumption even when a transport ignores abort.
      const interrupted = new Promise<never>((_, reject) => {
        const abort = () => reject(new Error('interrupted'));
        controller.signal.addEventListener('abort', abort, { once: true });
        removeAbort = () => controller.signal.removeEventListener('abort', abort);
        timer = setTimeout(() => controller.abort(), this.timeoutMs);
      });
      const exchange = async (): Promise<AuthenticationResult> => {
        const response = await this.transport(`${this.root}/api/autenticacion/${operation}`, {
          method: operation === 'prueba-pasajero' ? 'GET' : 'POST',
          headers, ...(operation === 'ingreso' ? { body: JSON.stringify(request) } : {}),
          credentials: 'omit', redirect: 'error', signal: controller.signal,
        });
        if (generation !== this.generation || controller.signal.aborted) return { ...cancelled };
        const status = response.status;
        if (response.redirected || (status >= 300 && status < 400)) return { ...invalid, status };
        if (status === 401) {
          if (operation === 'ingreso') return {
            outcome: 'rejected', message: 'No se pudo ingresar con las credenciales proporcionadas.', status,
          };
          // Do not abort our own completed exchange.
          this.token = undefined;
          this.logoutPending = false;
          return { ...unauthenticated, status };
        }
        if (status === 403) return { outcome: 'denied', message: 'Acceso denegado por la API.', status };
        if (status >= 500) return { outcome: 'server', message: 'La API no está disponible. Reintentá.', status };
        if (operation === 'salida') {
          if (status !== 204) return { ...invalid, status };
          this.token = undefined;
          this.logoutPending = false;
          return { outcome: 'success', message: 'Cierre de sesión confirmado.', status };
        }
        if (status !== 200 && status !== 400) return { ...invalid, status };
        let body: unknown;
        try { body = await response.json(); } catch { return { ...invalid, status }; }
        if (generation !== this.generation || controller.signal.aborted) return { ...cancelled };
        if (status === 400) {
          const errors = record(body) && record(body.errors) ? body.errors : {};
          const messages = [...new Set(Object.values(errors).flatMap(value =>
            Array.isArray(value) ? value.filter((message): message is string =>
              typeof message === 'string' && knownValidation.includes(message)) : []))];
          return messages.length ? { outcome: 'validation', message: messages.join(' '), status } : { ...invalid, status };
        }
        if (!validIdentity(body)) return { ...invalid, status };
        if (operation === 'ingreso') {
          const expiresAt = expiry(body.venceEnUtc);
          // A 32-byte canonical Base64url token has 43 chars and two zero pad bits.
          if (typeof body.token !== 'string' || !/^[A-Za-z0-9_-]{42}[AEIMQUYcgkosw048]$/.test(body.token)
            || !Number.isFinite(expiresAt) || expiresAt <= this.clock()) return { ...invalid, status };
          this.token = body.token;
          this.expiresAt = expiresAt;
        }
        return { outcome: 'success', message: operation === 'ingreso' ? 'Sesión confirmada.' : 'Acceso de pasajero confirmado por la API.', status };
      };
      const result = await Promise.race([exchange(), interrupted]);
      if (generation !== this.generation) return { ...cancelled };
      if (operation === 'salida' && result.outcome !== 'success') return {
        ...result, message: `No se pudo confirmar la revocación. ${this.token ? 'Reintentá cerrar sesión.' : 'La sesión local ya no es válida; ingresá nuevamente.'}`,
      };
      return result;
    } catch {
      if (generation !== this.generation) return { ...cancelled };
      return { outcome: 'network', message: operation === 'salida'
        ? 'No se pudo confirmar la revocación. Revisá la conexión y reintentá cerrar sesión.'
        : 'No se pudo conectar con la API o se agotó el tiempo. Revisá la conexión y reintentá.' };
    } finally {
      clearTimeout(timer);
      removeAbort();
      if (this.active === controller) this.active = undefined;
    }
  }
}
