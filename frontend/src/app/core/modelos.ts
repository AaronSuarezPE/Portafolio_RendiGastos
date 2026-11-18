export const ROL_EMPLEADO = 1;
export const ROL_APROBADOR = 2;
export const ROL_ADMINISTRADOR = 3;

export const ESTADO_BORRADOR = 1;
export const ESTADO_ENVIADA = 2;
export const ESTADO_APROBADA = 3;
export const ESTADO_RECHAZADA = 4;

export interface RespuestaSesion {
  token: string;
  codUsuario: number;
  nombre: string;
  rol: string;
  codRol: number;
  minutosVigencia: number;
}

export interface Sesion {
  token: string;
  codUsuario: number;
  nombre: string;
  rol: string;
  codRol: number;
  expira: number;
}

export interface Usuario {
  codUsuario: number;
  nombre: string;
  email: string;
  codRol: number;
  rol: string;
  codAprobador: number | null;
  aprobador: string | null;
  activo: boolean;
}

export interface Categoria {
  codCategoria: number;
  nombre: string;
  descripcion: string;
  limite: number;
  comprobanteObligatorio: boolean;
  activa: boolean;
}

export interface RendicionResumen {
  codRendicion: number;
  titulo: string;
  codUsuario: number;
  solicitante: string;
  codEstado: number;
  estado: string;
  total: number;
  cantidadGastos: number;
  fechaCreacion: string;
  fechaEnvio: string;
}

export interface Gasto {
  codGasto: number;
  codCategoria: number;
  categoria: string;
  comprobanteObligatorio: boolean;
  fecha: string;
  descripcion: string;
  monto: number;
  comprobante: string | null;
  tipoComprobante: string | null;
  tamanoComprobante: number;
}

export interface Historial {
  codHistorial: number;
  codAccion: number;
  accion: string;
  codUsuario: number;
  usuario: string;
  comentario: string;
  fecha: string;
  hora: string;
}

export interface RendicionDetalle {
  codRendicion: number;
  titulo: string;
  codUsuario: number;
  solicitante: string;
  aprobador: string | null;
  codEstado: number;
  estado: string;
  total: number;
  motivoRechazo: string;
  fechaCreacion: string;
  fechaEnvio: string;
  fechaResolucion: string;
  resolvio: string;
  gastos: Gasto[];
  historial: Historial[];
}

export interface Aviso {
  codAviso: number;
  codRendicion: number;
  titulo: string;
  mensaje: string;
  leido: boolean;
  fecha: string;
  hora: string;
}

export interface ClavePush {
  habilitado: boolean;
  clavePublica: string;
}

export interface DatosGasto {
  codCategoria: number;
  fecha: string;
  descripcion: string;
  monto: number;
}
