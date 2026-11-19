export type RolIngreso = 'pasajero' | 'cobrador' | 'administrador';

export interface SolicitudIngresoDto {
  correo: string;
  contrasena: string;
}

export interface RespuestaIngresoDto {
  identificadorUsuario: string;
  roles: RolIngreso[];
  /** JWT compacto (header.payload.firma); el cliente solo valida su forma, no decodifica el payload. */
  token: string;
  venceEnUtc: string;
}

export interface RespuestaErrorIngresoDto {
  codigo: string;
  mensaje: string;
}
