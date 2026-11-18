import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { environment } from '../../environments/environment';
import { administradorGuard, aprobadorGuard, authGuard, sinSesionGuard } from './auth.guard';
import { AuthService } from './auth.service';

function respuesta(codRol: number, rol: string) {
  return { token: 't', codUsuario: 7, nombre: 'Ana', rol, codRol, minutosVigencia: 60 };
}

describe('AuthService y guards', () => {
  let auth: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => sessionStorage.clear());

  function ingresarComo(codRol: number, rol: string): void {
    auth.login('ana@correo.com', 'Clave1234').subscribe();
    http.expectOne(`${environment.apiUrl}/Autenticacion/PostIngresar`).flush(respuesta(codRol, rol));
  }

  it('guarda la sesión con el rol y el código del usuario', () => {
    ingresarComo(2, 'Aprobador');

    expect(auth.autenticado()).toBeTrue();
    expect(auth.sesion()?.codUsuario).toBe(7);
    expect(auth.esAprobador()).toBeTrue();
    expect(auth.esAdministrador()).toBeFalse();
  });

  it('un empleado no es aprobador ni administrador; un administrador es ambas cosas', () => {
    ingresarComo(1, 'Empleado');
    expect(auth.esAprobador()).toBeFalse();
    expect(auth.esAdministrador()).toBeFalse();

    ingresarComo(3, 'Administrador');
    expect(auth.esAprobador()).toBeTrue();
    expect(auth.esAdministrador()).toBeTrue();
  });

  it('cerrar sesión borra todo y envía a la pantalla de acceso', () => {
    const navegar = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);
    ingresarComo(1, 'Empleado');

    auth.cerrarSesion();

    expect(auth.autenticado()).toBeFalse();
    expect(auth.token()).toBeNull();
    expect(sessionStorage.getItem('rendigastos.sesion')).toBeNull();
    expect(navegar).toHaveBeenCalledWith(['/acceso']);
  });

  it('una sesión guardada y vencida no se recupera', () => {
    sessionStorage.setItem('rendigastos.sesion', JSON.stringify({ token: 't', codUsuario: 1, nombre: 'A', rol: 'Empleado', codRol: 1, expira: Date.now() - 1000 }));
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] });

    expect(TestBed.inject(AuthService).autenticado()).toBeFalse();
  });

  it('los guards respetan la sesión y el rol', () => {
    const ejecutar = (guard: typeof authGuard) =>
      TestBed.runInInjectionContext(() => guard({} as never, {} as never));

    expect(ejecutar(authGuard)).not.toBe(true);
    expect(ejecutar(sinSesionGuard)).toBe(true);

    ingresarComo(1, 'Empleado');
    expect(ejecutar(authGuard)).toBe(true);
    expect(ejecutar(sinSesionGuard)).not.toBe(true);
    expect(ejecutar(aprobadorGuard)).not.toBe(true);
    expect(ejecutar(administradorGuard)).not.toBe(true);

    ingresarComo(2, 'Aprobador');
    expect(ejecutar(aprobadorGuard)).toBe(true);
    expect(ejecutar(administradorGuard)).not.toBe(true);

    ingresarComo(3, 'Administrador');
    expect(ejecutar(administradorGuard)).toBe(true);
  });
});
