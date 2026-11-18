import { HttpErrorResponse } from '@angular/common/http';

/** Convierte cualquier error de una llamada a la API en un mensaje en español para mostrar a la persona. */
export function mensajeDeError(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'Ocurrió un error inesperado. Inténtalo de nuevo.';
  }

  if (error.status === 0) {
    return 'No se pudo conectar con el servidor. Revisa tu conexión o inténtalo en unos minutos.';
  }

  const mensajeServidor = (error.error as { mensaje?: unknown } | null)?.mensaje;
  if (typeof mensajeServidor === 'string' && mensajeServidor.length > 0) {
    return mensajeServidor;
  }

  switch (error.status) {
    case 401:
      return 'Tu sesión terminó. Vuelve a ingresar.';
    case 413:
      return 'El archivo es demasiado grande.';
    case 429:
      return 'Hiciste demasiadas solicitudes. Espera un minuto e inténtalo de nuevo.';
    default:
      return error.status >= 500
        ? 'El servidor tuvo un problema. Inténtalo de nuevo en unos minutos.'
        : 'No se pudo completar la acción.';
  }
}
