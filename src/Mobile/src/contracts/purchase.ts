import type { ItinerarioDto } from './itinerary';

export type EstadoPago = 'pendiente' | 'desconocido' | 'aprobado' | 'rechazado' | 'devolucionPendiente' | 'devuelto';
export type PoliticaAsiento = 'sinAsiento' | 'asignacionAutomatica';

export interface SeleccionTramoCompraDto { orden: number; servicioId: string; }
export interface IniciarIntentoCompraRequestDto { tramos: SeleccionTramoCompraDto[]; }
export interface IniciarIntentoCompraResponseDto {
  intentoId: string;
  itinerario: ItinerarioDto;
  holdVenceEn: string;
}
export interface CotizarCompraResponseDto {
  intentoId: string;
  cotizacionOriginal: ItinerarioDto;
  cotizacionActual: ItinerarioDto;
  cambioPrecio: boolean;
  huellaCotizacion: string;
  holdVenceEn: string;
}
export interface IniciarPagoRequestDto { huellaCotizacionAceptada: string; }
export interface PagoResponseDto {
  pagoId: string;
  intentoId: string;
  estado: EstadoPago;
  importe: number;
  moneda: string;
  checkoutUrl: string | null;
  holdVenceEn: string;
}
export interface TicketEmitidoDto {
  ticketId: string;
  orden: number;
  servicioId: string;
  tramoId: string;
  origenNombre: string;
  destinoNombre: string;
  salida?: string;
  llegada?: string;
  importe: number;
  politicaAsiento: PoliticaAsiento;
  numeroAsiento: number | null;
  qrOpaco: string;
}
export interface CompraConfirmadaResponseDto {
  compraId: string;
  intentoId: string;
  pagoId: string;
  confirmadoUtc: string;
  importeTotal: number;
  tickets: TicketEmitidoDto[];
}
export interface IntentoPendienteResponseDto {
  intento: IniciarIntentoCompraResponseDto;
  pago: PagoResponseDto | null;
}
export interface SincronizarPagoResponseDto { pago: PagoResponseDto; compra: CompraConfirmadaResponseDto | null; }

export type PurchaseResult<T> = { ok: true; data: T; status: number } | {
  ok: false; kind: 'error' | 'ambiguous' | 'unknown'; message: string; status?: number;
};
export type PurchaseStartResult =
  | { kind: 'started'; payment: PagoResponseDto; status: number }
  | { kind: 'unknown'; message: string; status?: number }
  | { kind: 'quoteChanged'; quote: CotizarCompraResponseDto }
  | { kind: 'error'; message: string; status?: number };
