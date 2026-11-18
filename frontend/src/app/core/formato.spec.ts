import { aFechaApi, aIso, claseEstado, dinero, haceDiasIso, hoyIso } from './formato';
import { claveSegura, dosDecimales, minimoUtil } from './validadores';
import { FormControl } from '@angular/forms';

describe('formato y fechas', () => {
  it('convierte entre dd/mm/aaaa y aaaa-mm-dd', () => {
    expect(aIso('05/11/2026')).toBe('2026-11-05');
    expect(aFechaApi('2026-11-05')).toBe('05/11/2026');
    expect(aIso('')).toBe('');
    expect(aFechaApi('texto')).toBe('');
  });

  it('hoy y hace N días tienen formato ISO y están en orden', () => {
    expect(hoyIso()).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    expect(haceDiasIso(90) < hoyIso()).toBeTrue();
  });

  it('da formato de dinero con dos decimales', () => {
    expect(dinero(12345.5)).toContain('12.345,50');
    expect(dinero(0)).toContain('0,00');
  });

  it('asigna una clase por estado', () => {
    expect(claseEstado(1)).toContain('borrador');
    expect(claseEstado(2)).toContain('enviada');
    expect(claseEstado(3)).toContain('aprobada');
    expect(claseEstado(4)).toContain('rechazada');
  });
});

describe('validadores de RendiGastos', () => {
  it('claveSegura exige letras y números', () => {
    expect(claveSegura(new FormControl('SoloLetras'))).toEqual({ claveDebil: true });
    expect(claveSegura(new FormControl('12345678'))).toEqual({ claveDebil: true });
    expect(claveSegura(new FormControl('Clave1234'))).toBeNull();
    expect(claveSegura(new FormControl(''))).toBeNull();
  });

  it('minimoUtil ignora los espacios sobrantes', () => {
    const validar = minimoUtil(3);
    expect(validar(new FormControl('  a '))).not.toBeNull();
    expect(validar(new FormControl(' abc '))).toBeNull();
    expect(validar(new FormControl(''))).toBeNull();
  });

  it('dosDecimales acepta hasta dos decimales', () => {
    expect(dosDecimales(new FormControl(10.25))).toBeNull();
    expect(dosDecimales(new FormControl(10.255))).toEqual({ decimales: true });
    expect(dosDecimales(new FormControl(null))).toBeNull();
  });
});
