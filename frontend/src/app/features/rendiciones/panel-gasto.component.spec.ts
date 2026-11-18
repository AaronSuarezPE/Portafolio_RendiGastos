import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Categoria } from '../../core/modelos';
import { haceDiasIso, hoyIso } from '../../core/formato';
import { PanelGastoComponent, ResultadoGasto } from './panel-gasto.component';

const CATEGORIAS: Categoria[] = [
  { codCategoria: 1, nombre: 'Transporte', descripcion: '', limite: 200, comprobanteObligatorio: false, activa: true },
  { codCategoria: 2, nombre: 'Alimentación', descripcion: '', limite: 150, comprobanteObligatorio: true, activa: true },
];

describe('PanelGastoComponent', () => {
  let fixture: ComponentFixture<PanelGastoComponent>;
  let panel: any;
  let emitido: ResultadoGasto[];

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [PanelGastoComponent], providers: [provideNoopAnimations()] }).compileComponents();
    fixture = TestBed.createComponent(PanelGastoComponent);
    fixture.componentRef.setInput('categorias', CATEGORIAS);
    panel = fixture.componentInstance;
    emitido = [];
    panel.confirmar.subscribe((r: ResultadoGasto) => emitido.push(r));
    fixture.detectChanges();
  });

  function llenar(valores: Partial<{ codCategoria: number; fecha: string; descripcion: string; monto: number | null }>): void {
    panel.form.patchValue({ codCategoria: 1, fecha: haceDiasIso(1), descripcion: 'Taxi centro', monto: 50, ...valores });
  }

  it('no envía un formulario vacío y marca los campos', () => {
    panel.guardar();
    expect(emitido).toEqual([]);
    expect(panel.form.controls.codCategoria.touched).toBeTrue();
  });

  it('envía un gasto válido con la fecha en formato dd/mm/aaaa', () => {
    llenar({});
    panel.guardar();

    expect(emitido.length).toBe(1);
    expect(emitido[0].datos.fecha).toMatch(/^\d{2}\/\d{2}\/\d{4}$/);
    expect(emitido[0].datos).toEqual(jasmine.objectContaining({ codCategoria: 1, descripcion: 'Taxi centro', monto: 50 }));
    expect(emitido[0].archivo).toBeNull();
  });

  it('rechaza un monto sobre el límite de la categoría y lo vuelve a evaluar al cambiar de categoría', () => {
    llenar({ codCategoria: 2, monto: 160 });
    expect(panel.form.controls.monto.hasError('limite')).toBeTrue();
    panel.guardar();
    expect(emitido).toEqual([]);

    panel.form.controls.codCategoria.setValue(1);
    expect(panel.form.controls.monto.hasError('limite')).toBeFalse();
  });

  it('rechaza fechas futuras y de hace más de 90 días', () => {
    llenar({ fecha: haceDiasIso(120) });
    expect(panel.form.controls.fecha.hasError('rango')).toBeTrue();

    const manana = new Date();
    manana.setDate(manana.getDate() + 1);
    llenar({ fecha: manana.toISOString().slice(0, 10) });
    expect(panel.form.controls.fecha.hasError('rango')).toBeTrue();

    llenar({ fecha: hoyIso() });
    expect(panel.form.controls.fecha.hasError('rango')).toBeFalse();
  });

  it('rechaza monto cero, negativo y con más de dos decimales', () => {
    for (const monto of [0, -5, 10.123]) {
      llenar({ monto });
      expect(panel.form.controls.monto.invalid).withContext(`monto ${monto}`).toBeTrue();
    }
  });

  it('rechaza descripción vacía, de espacios o demasiado corta', () => {
    for (const descripcion of ['', '   ', 'ab']) {
      llenar({ descripcion });
      expect(panel.form.controls.descripcion.invalid).withContext(`«${descripcion}»`).toBeTrue();
    }
  });

  it('si la categoría exige comprobante no envía sin archivo y sí con uno', () => {
    llenar({ codCategoria: 2, monto: 100 });
    panel.guardar();
    expect(emitido).toEqual([]);
    expect(panel.faltaComprobante).toBeTrue();

    panel.archivo.set(new File([new Uint8Array([0x25, 0x50, 0x44, 0x46])], 'boleta.pdf'));
    panel.guardar();
    expect(emitido.length).toBe(1);
    expect(emitido[0].archivo?.name).toBe('boleta.pdf');
  });

  it('rechaza un archivo de Excel con un mensaje que explica qué se acepta', async () => {
    const entrada = document.createElement('input');
    entrada.type = 'file';
    const dt = new DataTransfer();
    dt.items.add(new File([new Uint8Array([1, 2, 3])], 'gastos.xlsx'));
    entrada.files = dt.files;

    await panel.elegirArchivo({ target: entrada } as unknown as Event);

    expect(panel.archivo()).toBeNull();
    expect(panel.errorArchivo()).toContain('Excel');
    expect(panel.errorArchivo()).toContain('PDF');
  });

  it('rechaza un archivo con extensión PDF pero contenido falso', async () => {
    const entrada = document.createElement('input');
    entrada.type = 'file';
    const dt = new DataTransfer();
    dt.items.add(new File([new TextEncoder().encode('texto')], 'falso.pdf'));
    entrada.files = dt.files;

    await panel.elegirArchivo({ target: entrada } as unknown as Event);

    expect(panel.archivo()).toBeNull();
    expect(panel.errorArchivo()).toContain('no corresponde');
  });

  it('acepta un PNG real y limpia el error anterior', async () => {
    panel.errorArchivo.set('error previo');
    const entrada = document.createElement('input');
    entrada.type = 'file';
    const dt = new DataTransfer();
    dt.items.add(new File([new Uint8Array([0x89, 0x50, 0x4e, 0x47, 1, 2, 3, 4])], 'boleta.png'));
    entrada.files = dt.files;

    await panel.elegirArchivo({ target: entrada } as unknown as Event);

    expect(panel.archivo()?.name).toBe('boleta.png');
    expect(panel.errorArchivo()).toBeNull();
  });
});
