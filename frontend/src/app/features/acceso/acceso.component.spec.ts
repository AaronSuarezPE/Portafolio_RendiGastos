import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { AccesoComponent } from './acceso.component';

describe('AccesoComponent', () => {
  let fixture: ComponentFixture<AccesoComponent>;
  let componente: any;
  let auth: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['login', 'registrar']);
    auth.login.and.returnValue(of({ token: 't', codUsuario: 1, nombre: 'Ana', rol: 'Empleado', codRol: 1, minutosVigencia: 60 }));
    auth.registrar.and.returnValue(of({ token: 't', codUsuario: 1, nombre: 'Ana', rol: 'Empleado', codRol: 1, minutosVigencia: 60 }));

    await TestBed.configureTestingModule({
      imports: [AccesoComponent],
      providers: [provideRouter([]), provideNoopAnimations(), { provide: AuthService, useValue: auth }],
    }).compileComponents();

    fixture = TestBed.createComponent(AccesoComponent);
    componente = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('no llama al servidor si el login está vacío y marca los campos', () => {
    componente.ingresar();

    expect(auth.login).not.toHaveBeenCalled();
    expect(componente.formLogin.controls.email.touched).toBeTrue();
    expect(componente.formLogin.controls.clave.touched).toBeTrue();
  });

  it('no llama al servidor si el correo no es válido', () => {
    componente.formLogin.setValue({ email: 'no-es-correo', clave: 'ClaveSegura1' });

    componente.ingresar();

    expect(auth.login).not.toHaveBeenCalled();
    expect(componente.formLogin.controls.email.hasError('correo')).toBeTrue();
  });

  it('llama al servidor con los datos limpios cuando el login es válido', () => {
    componente.formLogin.setValue({ email: 'ana@correo.com', clave: 'ClaveSegura1' });

    componente.ingresar();

    expect(auth.login).toHaveBeenCalledWith('ana@correo.com', 'ClaveSegura1');
  });

  it('rechaza un registro con clave corta o claves distintas', () => {
    componente.formRegistro.setValue({
      nombre: 'Ana', email: 'ana@correo.com', clave: 'corta', repetirClave: 'corta',
    });
    componente.registrar();
    expect(auth.registrar).not.toHaveBeenCalled();
    expect(componente.formRegistro.controls.clave.hasError('minlength')).toBeTrue();

    componente.formRegistro.setValue({
      nombre: 'Ana', email: 'ana@correo.com', clave: 'ClaveSegura1', repetirClave: 'OtraClave123',
    });
    componente.registrar();
    expect(auth.registrar).not.toHaveBeenCalled();
    expect(componente.formRegistro.controls.repetirClave.hasError('distinta')).toBeTrue();
  });

  it('registra cuando todos los datos son válidos', () => {
    componente.formRegistro.setValue({
      nombre: 'Ana Torres', email: 'ana@correo.com', clave: 'ClaveSegura1', repetirClave: 'ClaveSegura1',
    });

    componente.registrar();

    expect(auth.registrar).toHaveBeenCalledWith('Ana Torres', 'ana@correo.com', 'ClaveSegura1');
  });

  it('muestra el mensaje del servidor cuando las credenciales son incorrectas y deja reintentar', () => {
    auth.login.and.returnValue(throwError(() => new HttpErrorResponse({ status: 401, error: { mensaje: 'Correo o clave incorrectos.' } })));
    componente.formLogin.setValue({ email: 'ana@correo.com', clave: 'ClaveSegura1' });

    componente.ingresar();
    fixture.detectChanges();

    expect(componente.error()).toBe('Correo o clave incorrectos.');
    expect(componente.trabajando()).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain('Correo o clave incorrectos.');
  });

  it('muestra el error cuando el correo ya está registrado', () => {
    auth.registrar.and.returnValue(throwError(() => new HttpErrorResponse({ status: 400, error: { mensaje: 'El correo ya está registrado.' } })));
    componente.formRegistro.setValue({
      nombre: 'Ana Torres', email: 'ana@correo.com', clave: 'ClaveSegura1', repetirClave: 'ClaveSegura1',
    });

    componente.registrar();

    expect(componente.error()).toBe('El correo ya está registrado.');
  });

  it('entra con una cuenta de demostración sin pedir datos', () => {
    componente.entrarDemo('empleado@demo.rendigastos.app');

    expect(auth.login).toHaveBeenCalledWith('empleado@demo.rendigastos.app', 'Demo2026!');
  });

  it('rechaza una clave sin números', () => {
    componente.formRegistro.setValue({
      nombre: 'Ana Torres', email: 'ana@correo.com', clave: 'SoloLetrasAqui', repetirClave: 'SoloLetrasAqui',
    });

    componente.registrar();

    expect(auth.registrar).not.toHaveBeenCalled();
    expect(componente.formRegistro.controls.clave.hasError('claveDebil')).toBeTrue();
  });

  it('rechaza nombre de uno o dos espacios y correo mal escrito en el registro', () => {
    componente.formRegistro.setValue({ nombre: ' ', email: 'ana@', clave: 'ClaveSegura1', repetirClave: 'ClaveSegura1' });

    componente.registrar();

    expect(auth.registrar).not.toHaveBeenCalled();
    expect(componente.formRegistro.controls.nombre.invalid).toBeTrue();
    expect(componente.formRegistro.controls.email.invalid).toBeTrue();
  });
});
