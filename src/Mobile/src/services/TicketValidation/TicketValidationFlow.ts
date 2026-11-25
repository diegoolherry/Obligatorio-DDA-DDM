// Pure state machine of the collector's «Validar» flow. No React or Expo imports: the screen renders
// `state` and forwards events; this class owns the transitions and the single request per scan.
import type { ResultadoValidacionTicketDto, TicketValidationFailure } from '../../contracts/ticketValidation';
import type { ITicketValidationService } from './ITicketValidationService';

/** Subset of the expo-camera permission response that decides the flow. */
export interface CameraPermissionInfo {
  status: 'granted' | 'undetermined' | 'denied';
  canAskAgain: boolean;
}

export type ValidationFlowState =
  | { kind: 'selecting'; servicioId: string | undefined }
  | { kind: 'permission-explain'; servicioId: string }
  | { kind: 'permission-denied'; servicioId: string }
  | { kind: 'scanning'; servicioId: string }
  | { kind: 'validating'; servicioId: string; code: string }
  | { kind: 'result'; servicioId: string; result: ResultadoValidacionTicketDto }
  | { kind: 'network-error'; servicioId: string; code: string; failure: TicketValidationFailure; message: string };

type Listener = (state: ValidationFlowState) => void;

export class TicketValidationFlow {
  private current: ValidationFlowState = { kind: 'selecting', servicioId: undefined };
  private readonly listeners = new Set<Listener>();
  private readonly service: Pick<ITicketValidationService, 'validate'>;
  private disposed = false;

  constructor(service: Pick<ITicketValidationService, 'validate'>) {
    this.service = service;
  }

  get state(): ValidationFlowState {
    return this.current;
  }

  subscribe(listener: Listener): () => void {
    this.listeners.add(listener);
    listener(this.current);
    return () => { this.listeners.delete(listener); };
  }

  /** The tramo can only change while nothing is being validated or shown. */
  selectService(servicioId: string): void {
    if (this.current.kind !== 'selecting' || !servicioId) return;
    this.set({ kind: 'selecting', servicioId });
  }

  /** «Escanear» / «Volver a intentar»: the screen passes the camera permission it just read. */
  requestScan(permission: CameraPermissionInfo): void {
    const { current } = this;
    if (current.kind !== 'selecting' && current.kind !== 'permission-denied' && current.kind !== 'permission-explain') return;
    if (current.servicioId === undefined) return;
    this.set(this.afterPermission(current.servicioId, permission, true));
  }

  /** Answer of the system dialog (or of a re-read after Ajustes). */
  permissionAnswered(permission: CameraPermissionInfo): void {
    const { current } = this;
    if (current.kind !== 'permission-explain' && current.kind !== 'permission-denied') return;
    this.set(this.afterPermission(current.servicioId, permission, false));
  }

  /** «Cancelar» while scanning, or leaving the permission screens. */
  cancel(): void {
    const { current } = this;
    if (current.kind === 'scanning' || current.kind === 'permission-explain' || current.kind === 'permission-denied') {
      this.set({ kind: 'selecting', servicioId: current.servicioId });
    }
  }

  /**
   * A QR was read. Only the first read of a scan starts a request: while validating, after a result or
   * after an error the state is no longer `scanning`, so repeated barcode events fall through.
   */
  codeScanned(code: string): void {
    const { current } = this;
    if (current.kind !== 'scanning' || !code) return;
    this.validate(current.servicioId, code);
  }

  /** «Reintentar» after an error: resends the same code for the same service. */
  retry(): void {
    const { current } = this;
    if (current.kind !== 'network-error') return;
    this.validate(current.servicioId, current.code);
  }

  /** «Escanear otro»: back to the camera for the same tramo (the permission was already granted). */
  scanAnother(): void {
    const { current } = this;
    if (current.kind !== 'result' && current.kind !== 'network-error') return;
    this.set({ kind: 'scanning', servicioId: current.servicioId });
  }

  /** «Cambiar tramo» after a result or an error: back to the picker, keeping the chosen service. */
  backToSelection(): void {
    const { current } = this;
    if (current.kind !== 'result' && current.kind !== 'network-error') return;
    this.set({ kind: 'selecting', servicioId: current.servicioId });
  }

  /** The session ended: forget everything, including any answer still in flight. */
  reset(): void {
    if (this.current.kind === 'selecting' && this.current.servicioId === undefined) return;
    this.set({ kind: 'selecting', servicioId: undefined });
  }

  /** Drops any late response and stops notifying (screen unmounted or session changed). */
  dispose(): void {
    this.disposed = true;
    this.listeners.clear();
  }

  private afterPermission(servicioId: string, permission: CameraPermissionInfo, mayExplain: boolean): ValidationFlowState {
    if (permission.status === 'granted') return { kind: 'scanning', servicioId };
    if (permission.status === 'undetermined' && mayExplain) return { kind: 'permission-explain', servicioId };
    return { kind: 'permission-denied', servicioId };
  }

  private validate(servicioId: string, code: string): void {
    const attempt: ValidationFlowState = { kind: 'validating', servicioId, code };
    this.set(attempt);
    void this.service.validate(code, servicioId).then(result => {
      // Ignore the answer unless it belongs to the attempt still on screen.
      if (this.disposed || this.current !== attempt) return;
      this.set(result.ok
        ? { kind: 'result', servicioId, result: result.data }
        : { kind: 'network-error', servicioId, code, failure: result.failure, message: result.message });
    }, () => {
      if (this.disposed || this.current !== attempt) return;
      this.set({ kind: 'network-error', servicioId, code, failure: 'network', message: 'No hubo respuesta del servidor. Todavía no sabemos si el ticket se validó.' });
    });
  }

  private set(next: ValidationFlowState): void {
    this.current = next;
    for (const listener of [...this.listeners]) listener(next);
  }
}
