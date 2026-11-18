import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { AvisosService } from '../shared/avisos.service';
import { AuthService } from './auth.service';

/** Envía el token en cada llamada a la API y cierra la sesión si el servidor la rechaza. */
export const authInterceptor: HttpInterceptorFn = (peticion, siguiente) => {
  const auth = inject(AuthService);
  const avisos = inject(AvisosService);
  const esApi = peticion.url.startsWith(environment.apiUrl);
  const token = auth.token();

  const conToken = esApi && token
    ? peticion.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : peticion;

  return siguiente(conToken).pipe(
    catchError((error: unknown) => {
      const sesionVencida =
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        token !== null &&
        !peticion.url.includes('/Autenticacion/');

      if (sesionVencida) {
        auth.cerrarSesion();
        avisos.info('Tu sesión venció', 'Por seguridad la sesión se cerró. Ingresa de nuevo para continuar.');
      }
      return throwError(() => error);
    }),
  );
};
