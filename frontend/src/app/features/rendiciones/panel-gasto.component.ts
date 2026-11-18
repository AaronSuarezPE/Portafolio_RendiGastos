import { Component, ElementRef, OnInit, inject, input, output, signal, viewChild } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { aFechaApi, aIso, dinero, haceDiasIso, hoyIso, DIAS_MAXIMOS_ANTIGUEDAD } from '../../core/formato';
import { Categoria, DatosGasto, Gasto } from '../../core/modelos';
import {
  TAMANO_MAXIMO_MB, TEXTO_FORMATOS, dosDecimales, formatearTamano, minimoUtil, obligatorioSinEspacios,
  validarArchivo, validarContenido,
} from '../../core/validadores';
import { PanelLateralComponent } from '../../shared/panel-lateral.component';

export interface ResultadoGasto {
  datos: DatosGasto;
  archivo: File | null;
  quitarComprobante: boolean;
}

/** Formulario lateral para agregar o corregir un gasto, con su comprobante opcional u obligatorio según la categoría. */
@Component({
  selector: 'app-panel-gasto',
  imports: [
    PanelLateralComponent, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule,
  ],
  templateUrl: './panel-gasto.component.html',
  styleUrl: './panel-gasto.component.scss',
})
export class PanelGastoComponent implements OnInit {
  readonly categorias = input.required<Categoria[]>();
  readonly gasto = input<Gasto | null>(null);
  readonly trabajando = input(false);

  readonly confirmar = output<ResultadoGasto>();
  readonly cerrar = output<void>();

  private readonly fb = inject(FormBuilder).nonNullable;
  protected readonly entrada = viewChild<ElementRef<HTMLInputElement>>('entradaArchivo');

  protected readonly minimaFecha = haceDiasIso(DIAS_MAXIMOS_ANTIGUEDAD);
  protected readonly maximaFecha = hoyIso();
  protected readonly textoFormatos = TEXTO_FORMATOS;
  protected readonly tamanoMaximo = TAMANO_MAXIMO_MB;
  protected readonly dinero = dinero;
  protected readonly formatearTamano = formatearTamano;

  protected readonly archivo = signal<File | null>(null);
  protected readonly errorArchivo = signal<string | null>(null);
  protected readonly quitarActual = signal(false);
  protected readonly intentado = signal(false);

  protected readonly form = this.fb.group({
    codCategoria: [0, [Validators.min(1)]],
    fecha: ['', [Validators.required, (c: AbstractControl) => this.validarRango(c)]],
    descripcion: ['', [obligatorioSinEspacios, minimoUtil(3), Validators.maxLength(200)]],
    monto: [null as number | null, [Validators.required, Validators.min(0.01), Validators.max(1000000), dosDecimales, (c: AbstractControl) => this.validarLimite(c)]],
  });

  ngOnInit(): void {
    // Al cambiar de categoría cambia el límite aplicable al monto.
    this.form.controls.codCategoria.valueChanges.subscribe(() => this.form.controls.monto.updateValueAndValidity());
    const g = this.gasto();
    if (g) {
      this.form.setValue({
        codCategoria: g.codCategoria,
        fecha: aIso(g.fecha),
        descripcion: g.descripcion,
        monto: g.monto,
      });
    }
  }

  protected get titulo(): string {
    return this.gasto() ? 'Corregir gasto' : 'Agregar gasto';
  }

  protected get categoriaElegida(): Categoria | undefined {
    return this.categorias().find((c) => c.codCategoria === this.form.controls.codCategoria.value);
  }

  /** El comprobante es obligatorio si la categoría lo exige y no hay uno ya guardado que se conserve. */
  protected get comprobanteObligatorio(): boolean {
    return !!this.categoriaElegida?.comprobanteObligatorio;
  }

  protected get tieneComprobanteGuardado(): boolean {
    return !!this.gasto()?.comprobante && !this.quitarActual();
  }

  protected get faltaComprobante(): boolean {
    return this.comprobanteObligatorio && !this.archivo() && !this.tieneComprobanteGuardado;
  }

  private validarLimite(control: AbstractControl): ValidationErrors | null {
    // Al construir el formulario todavía no existen ni el grupo ni la lista de categorías.
    if (!this.form) return null;
    const limite = this.categorias().find((c) => c.codCategoria === this.form.controls.codCategoria.value)?.limite;
    const monto = control.value;
    return limite !== undefined && monto !== null && monto !== '' && Number(monto) > limite ? { limite: { limite } } : null;
  }

  private validarRango(control: AbstractControl): ValidationErrors | null {
    const fecha = control.value as string;
    return fecha && (fecha < this.minimaFecha || fecha > this.maximaFecha) ? { rango: true } : null;
  }

  protected async elegirArchivo(evento: Event): Promise<void> {
    const entrada = evento.target as HTMLInputElement;
    // Se copia la lista antes de limpiar el campo: al vaciarlo el navegador vacía también la lista original.
    const elegidos = Array.from(entrada.files ?? []);
    entrada.value = '';
    if (elegidos.length === 0) return;

    const candidato = elegidos[0];
    const error = validarArchivo(candidato) ?? (await validarContenido(candidato));
    if (error) {
      this.archivo.set(null);
      this.errorArchivo.set(error);
      return;
    }

    this.errorArchivo.set(null);
    this.archivo.set(candidato);
  }

  protected quitarArchivoElegido(): void {
    this.archivo.set(null);
    this.errorArchivo.set(null);
  }

  protected quitarGuardado(): void {
    this.quitarActual.set(true);
  }

  protected guardar(): void {
    this.intentado.set(true);
    this.form.markAllAsTouched();
    if (this.form.invalid || this.faltaComprobante || this.trabajando()) return;

    const v = this.form.getRawValue();
    this.confirmar.emit({
      datos: {
        codCategoria: v.codCategoria,
        fecha: aFechaApi(v.fecha),
        descripcion: v.descripcion.trim(),
        monto: Number(v.monto),
      },
      archivo: this.archivo(),
      quitarComprobante: this.quitarActual() && !this.archivo(),
    });
  }
}
