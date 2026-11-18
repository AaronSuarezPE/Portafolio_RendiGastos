import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.autenticado() ? true : inject(Router).createUrlTree(['/acceso']);
};

/** Quien ya tiene sesión no necesita ver la pantalla de acceso. */
export const sinSesionGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.autenticado() ? inject(Router).createUrlTree(['/rendiciones']) : true;
};

/** Pantallas de aprobación: aprobadores y administradores. El servidor vuelve a verificarlo. */
export const aprobadorGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.esAprobador() ? true : inject(Router).createUrlTree(['/rendiciones']);
};

export const administradorGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.esAdministrador() ? true : inject(Router).createUrlTree(['/rendiciones']);
};
