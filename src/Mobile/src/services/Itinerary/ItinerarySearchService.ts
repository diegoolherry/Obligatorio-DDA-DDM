import type { IItinerarySearchService } from './IItinerarySearchService';
import type {
  BusquedaItinerarioRequest, ItinerarioDto, ItineraryResult, ParadaDto,
} from '../../contracts/itinerary';
import { apiRoot } from '../Http/apiRoot';

const networkMessage = 'No se pudo conectar con la API. Revisá tu conexión e intentá de nuevo.';
const badRequestFallback = 'Revisá los datos de la búsqueda.';
const notFoundMessage = 'Una de las paradas elegidas ya no existe. Actualizá la lista de paradas.';
const otherMessage = 'La API no pudo completar la búsqueda. Intentá de nuevo en unos minutos.';
const configMessage = 'Configurá EXPO_PUBLIC_API_URL con la raíz HTTPS de la API, sin credenciales, query ni fragmento.';

function fail<T>(message: string): ItineraryResult<T> {
  return { ok: false, message };
}

// Public, anonymous endpoints: never send Authorization and never include cookies.
export class ItinerarySearchService implements IItinerarySearchService {
  private root: string | undefined;
  private transport: typeof fetch;
  private timeoutMs: number;

  constructor(url?: string, transport: typeof fetch = fetch, timeoutMs = 12000) {
    this.root = apiRoot(url);
    this.transport = transport;
    this.timeoutMs = timeoutMs;
  }

  getStops(signal?: AbortSignal): Promise<ItineraryResult<ParadaDto[]>> {
    return this.get<ParadaDto[]>('/api/paradas', signal);
  }

  search(request: BusquedaItinerarioRequest, signal?: AbortSignal): Promise<ItineraryResult<ItinerarioDto[]>> {
    const query = `origenId=${encodeURIComponent(String(request.origenId))}`
      + `&destinoId=${encodeURIComponent(String(request.destinoId))}`
      + `&fecha=${encodeURIComponent(request.fecha)}`;
    return this.get<ItinerarioDto[]>(`/api/itinerarios?${query}`, signal);
  }

  private async get<T>(path: string, external?: AbortSignal): Promise<ItineraryResult<T>> {
    if (!this.root) return fail(configMessage);
    const controller = new AbortController();
    const forward = () => controller.abort();
    external?.addEventListener('abort', forward, { once: true });
    if (external?.aborted) controller.abort();
    let timer: ReturnType<typeof setTimeout> | undefined;
    try {
      // Bound both fetch and body consumption even when a transport ignores abort.
      const interrupted = new Promise<never>((_, reject) => {
        controller.signal.addEventListener('abort', () => reject(new Error('interrupted')), { once: true });
        timer = setTimeout(() => controller.abort(), this.timeoutMs);
      });
      const exchange = async (): Promise<ItineraryResult<T>> => {
        const response = await this.transport(`${this.root}${path}`, {
          method: 'GET', headers: { Accept: 'application/json' },
          credentials: 'omit', redirect: 'error', signal: controller.signal,
        });
        if (response.status === 200) {
          let body: unknown;
          try { body = await response.json(); } catch { return fail(otherMessage); }
          return Array.isArray(body) ? { ok: true, data: body as T } : fail(otherMessage);
        }
        if (response.status === 400) {
          let text = '';
          try { text = (await response.text()).trim(); } catch { /* use fallback */ }
          return fail(text && text.length <= 200 && !text.startsWith('<') && !text.startsWith('{') ? text : badRequestFallback);
        }
        if (response.status === 404) return fail(notFoundMessage);
        return fail(otherMessage);
      };
      return await Promise.race([exchange(), interrupted]);
    } catch {
      return fail(networkMessage);
    } finally {
      clearTimeout(timer);
      external?.removeEventListener('abort', forward);
    }
  }
}
