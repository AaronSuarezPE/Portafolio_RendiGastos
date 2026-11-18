import { OverlayContainer } from '@angular/cdk/overlay';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { AvisosService } from './avisos.service';

describe('AvisosService', () => {
  let servicio: AvisosService;
  let capa: HTMLElement;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideNoopAnimations()] });
    servicio = TestBed.inject(AvisosService);
    capa = TestBed.inject(OverlayContainer).getContainerElement();
  });

  afterEach(() => TestBed.inject(MatDialog).closeAll());

  const esperar = () => new Promise<void>((r) => setTimeout(r, 50));
  const pintar = () => TestBed.inject(ApplicationRef).tick();
  const texto = () => capa.textContent ?? '';
  const botones = () => Array.from(capa.querySelectorAll('button'));

  it('muestra un aviso de éxito con un solo botón Entendido', () => {
    servicio.exito('Datos confirmados', 'Quedaron guardados.');
    pintar();

    expect(texto()).toContain('Datos confirmados');
    expect(texto()).toContain('Quedaron guardados.');
    expect(botones().map((b) => b.textContent?.trim())).toEqual(['Entendido']);
  });

  it('muestra un aviso de error con su mensaje', () => {
    servicio.error('No se pudo guardar', 'Inténtalo de nuevo.');
    pintar();

    expect(texto()).toContain('No se pudo guardar');
    expect(capa.querySelector('.icono--error')).not.toBeNull();
  });

  it('la confirmación tiene Cancelar y la acción, y emite true al confirmar', async () => {
    let resultado: boolean | undefined;
    servicio.confirmar('¿Eliminar?', 'No se puede deshacer.', 'Eliminar', true).subscribe((r) => (resultado = r));
    pintar();

    const etiquetas = botones().map((b) => b.textContent?.trim());
    expect(etiquetas).toEqual(['Cancelar', 'Eliminar']);

    botones()[1].click();
    pintar();
    await esperar();
    expect(resultado).toBeTrue();
  });

  it('la confirmación emite false al cancelar', async () => {
    let resultado: boolean | undefined;
    servicio.confirmar('¿Eliminar?', 'x', 'Eliminar').subscribe((r) => (resultado = r));
    pintar();

    botones()[0].click();
    pintar();
    await esperar();

    expect(resultado).toBeFalse();
  });
});
