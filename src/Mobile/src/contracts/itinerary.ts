export type PoliticaAsiento = 'sinAsiento' | 'asignacionAutomatica';

export interface ParadaDto {
  id: number;
  nombre: string;
}

export interface TramoItinerarioDto {
  orden: number;
  servicioId: number;
  tramoId: number;
  origen: ParadaDto;
  destino: ParadaDto;
  salida: string;
  llegada: string;
  importe: number;
  politicaAsiento: PoliticaAsiento;
}

export interface TransbordoDto {
  parada: ParadaDto;
  minutos: number;
}

export interface ItinerarioDto {
  salida: string;
  llegada: string;
  precioTotal: number;
  transbordo: TransbordoDto | null;
  tramos: TramoItinerarioDto[];
}

export interface BusquedaItinerarioRequest {
  origenId: number;
  destinoId: number;
  /** yyyy-MM-dd */
  fecha: string;
}

export type ItineraryResult<T> = { ok: true; data: T } | { ok: false; message: string };
