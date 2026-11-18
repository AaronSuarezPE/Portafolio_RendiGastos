import { HttpErrorResponse } from '@angular/common/http';
import { mensajeDeError } from './errores';

describe('mensajeDeError', () => {
  it('muestra el mensaje que envía el servidor', () => {
    const error = new HttpErrorResponse({ status: 400, error: { mensaje: 'El correo ya está registrado' } });

    expect(mensajeDeError(error)).toBe('El correo ya está registrado');
  });

  it('avisa cuando no hay conexión', () => {
    expect(mensajeDeError(new HttpErrorResponse({ status: 0 }))).toContain('No se pudo conectar');
  });

  it('usa un texto propio según el código cuando el servidor no manda mensaje', () => {
    expect(mensajeDeError(new HttpErrorResponse({ status: 429 }))).toContain('demasiadas solicitudes');
    expect(mensajeDeError(new HttpErrorResponse({ status: 500 }))).toContain('El servidor tuvo un problema');
  });

  it('nunca devuelve un texto vacío ni técnico para errores desconocidos', () => {
    expect(mensajeDeError(new Error('boom'))).toContain('error inesperado');
  });
});
