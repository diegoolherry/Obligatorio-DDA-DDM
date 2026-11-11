import type {
  ConsultaUbicacionResponse, LocationResult, PublicacionPosicionResponse, ServicioUbicacionDto,
} from '../../contracts/location';

export interface ILocationService {
  /** Cobrador: services assigned to the signed-in collector. */
  listCollectorServices(): Promise<LocationResult<ServicioUbicacionDto[]>>;
  /** Cobrador: publishes one position; coordinates are rounded to 6 decimals. */
  publishPosition(serviceId: string, latitude: number, longitude: number): Promise<LocationResult<PublicacionPosicionResponse>>;
  /** Pasajero: demo services. */
  listPassengerServices(): Promise<LocationResult<ServicioUbicacionDto[]>>;
  /** Pasajero: last position, delay state and academic parameters of a service. */
  getServiceLocation(serviceId: string): Promise<LocationResult<ConsultaUbicacionResponse>>;
}
