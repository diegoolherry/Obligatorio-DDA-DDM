export type RolIngreso = 'pasajero' | 'cobrador' | 'administrador';

export interface SolicitudIngresoDto {
  correo: string;
  contrasena: string;
}

export interface RespuestaIngresoDto {
  identificadorUsuario: string;
  roles: RolIngreso[];
  token: string;
  venceEnUtc: string;
}

export interface RespuestaErrorIngresoDto {
  codigo: string;
  mensaje: string;
}
