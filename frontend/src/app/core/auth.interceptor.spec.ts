import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { OverlayContainer } from '@angular/cdk/overlay';
import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let controlador: HttpTestingController;
  let auth: { token: jasmine.Spy; cerrarSesion: jasmine.Spy };

  beforeEach(() => {
    auth = { token: jasmine.createSpy('token'), cerrarSesion: jasmine.createSpy('cerrarSesion') };

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth },
      ],
    });

    http = TestBed.inject(HttpClient);
    controlador = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controlador.verify());

  it('envía el token a la API', () => {
    auth.token.and.returnValue('abc123');

    http.get(`${environment.apiUrl}/Documentos/GetList`).subscribe();

    const peticion = controlador.expectOne(`${environment.apiUrl}/Documentos/GetList`);
    expect(peticion.request.headers.get('Authorization')).toBe('Bearer abc123');
    peticion.flush([]);
  });

  it('no envía el token a otros sitios', () => {
    auth.token.and.returnValue('abc123');

    http.get('https://otro-sitio.com/datos').subscribe();

    const peticion = controlador.expectOne('https://otro-sitio.com/datos');
    expect(peticion.request.headers.has('Authorization')).toBeFalse();
    peticion.flush({});
  });

  it('cierra la sesión cuando la API responde 401 con una sesión abierta', () => {
    auth.token.and.returnValue('abc123');

    http.get(`${environment.apiUrl}/Documentos/GetList`).subscribe({ error: () => undefined });

    controlador
      .expectOne(`${environment.apiUrl}/Documentos/GetList`)
      .flush({ mensaje: 'x' }, { status: 401, statusText: 'Unauthorized' });
    expect(auth.cerrarSesion).toHaveBeenCalled();
    TestBed.inject(ApplicationRef).tick();
    expect(TestBed.inject(OverlayContainer).getContainerElement().textContent).toContain('Tu sesión venció');
  });

  it('no cierra la sesión si el 401 viene de un intento de login', () => {
    auth.token.and.returnValue('abc123');

    http.post(`${environment.apiUrl}/Autenticacion/PostLogin`, {}).subscribe({ error: () => undefined });

    controlador
      .expectOne(`${environment.apiUrl}/Autenticacion/PostLogin`)
      .flush({ mensaje: 'x' }, { status: 401, statusText: 'Unauthorized' });
    expect(auth.cerrarSesion).not.toHaveBeenCalled();
  });
});
