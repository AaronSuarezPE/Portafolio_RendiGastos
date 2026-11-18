import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { PanelTextoComponent } from './panel-texto.component';

describe('PanelTextoComponent', () => {
  let fixture: ComponentFixture<PanelTextoComponent>;
  let panel: PanelTextoComponent;
  let emitido: string[];

  function crear(entradas: Record<string, unknown> = {}): void {
    fixture = TestBed.createComponent(PanelTextoComponent);
    fixture.componentRef.setInput('titulo', 'Rechazar');
    fixture.componentRef.setInput('etiqueta', 'Motivo');
    for (const [clave, valor] of Object.entries(entradas)) fixture.componentRef.setInput(clave, valor);
    panel = fixture.componentInstance;
    emitido = [];
    panel.confirmar.subscribe((v) => emitido.push(v));
    fixture.detectChanges();
  }

  const campo = () => (panel as unknown as { campo: { setValue(v: string): void; value: string; touched: boolean } }).campo;
  const guardar = () => (panel as unknown as { guardar(): void }).guardar();

  beforeEach(() => TestBed.configureTestingModule({ imports: [PanelTextoComponent], providers: [provideNoopAnimations()] }));

  it('no entrega nada si el campo obligatorio está vacío o solo tiene espacios', () => {
    crear();
    guardar();
    expect(emitido).toEqual([]);
    expect(campo().touched).toBeTrue();

    campo().setValue('    ');
    guardar();
    expect(emitido).toEqual([]);
  });

  it('respeta el mínimo de caracteres', () => {
    crear({ minimo: 10 });
    campo().setValue('corto');
    guardar();
    expect(emitido).toEqual([]);

    campo().setValue('Falta el detalle de la visita');
    guardar();
    expect(emitido).toEqual(['Falta el detalle de la visita']);
  });

  it('entrega el texto sin espacios sobrantes', () => {
    crear();
    campo().setValue('  Hola mundo  ');
    guardar();
    expect(emitido).toEqual(['Hola mundo']);
  });

  it('un campo opcional permite enviar vacío', () => {
    crear({ obligatorio: false });
    guardar();
    expect(emitido).toEqual(['']);
  });

  it('no entrega mientras está trabajando', () => {
    crear({ trabajando: true });
    campo().setValue('Texto válido');
    guardar();
    expect(emitido).toEqual([]);
  });

  it('carga el valor inicial', () => {
    crear({ valorInicial: 'Viaje' });
    expect(campo().value).toBe('Viaje');
  });
});
