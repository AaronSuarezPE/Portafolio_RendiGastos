import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ApiService } from '../../core/api.service';
import { mensajeDeError } from '../../core/errores';
import { dinero } from '../../core/formato';
import { Categoria } from '../../core/modelos';
import { dosDecimales, minimoUtil, obligatorioSinEspacios } from '../../core/validadores';
import { AvisosService } from '../../shared/avisos.service';
import { PanelLateralComponent } from '../../shared/panel-lateral.component';

@Component({
  selector: 'app-categorias',
  imports: [
    PanelLateralComponent, ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule,
  ],
  template: `
    <section>
      <header class="cabecera">
        <div>
          <h1>Categorías de gasto</h1>
          <p class="sub">Define el límite por gasto y si la categoría exige comprobante.</p>
        </div>
        <button mat-flat-button type="button" (click)="abrirNueva()">Nueva categoría</button>
      </header>

      @if (error(); as mensaje) {
        <p class="aviso aviso--error" role="alert">{{ mensaje }} <button mat-button type="button" (click)="cargar()">Reintentar</button></p>
      }

      @if (cargando()) {
        <p class="vacio" role="status">Cargando…</p>
      } @else {
        <ul class="lista">
          @for (c of categorias(); track c.codCategoria) {
            <li class="tarjeta fila" [class.inactivo]="!c.activa">
              <div class="fila__principal">
                <strong>{{ c.nombre }} @if (!c.activa) { <span class="etiqueta">Inactiva</span> }</strong>
                @if (c.descripcion) { <span class="detalle">{{ c.descripcion }}</span> }
                <span class="detalle">
                  Límite {{ dinero(c.limite) }} · {{ c.comprobanteObligatorio ? 'comprobante obligatorio' : 'comprobante opcional' }}
                </span>
              </div>
              <button mat-stroked-button type="button" (click)="abrirEdicion(c)">Editar</button>
            </li>
          }
        </ul>
      }

      @if (panelAbierto()) {
        <app-panel-lateral [titulo]="enEdicion() ? 'Editar categoría' : 'Nueva categoría'" (cerrar)="cerrar()">
          <form [formGroup]="form" (ngSubmit)="guardar()" novalidate id="form-categoria">
            <p class="leyenda"><span class="obligatorio">*</span> Campo obligatorio</p>

            <mat-form-field appearance="outline">
              <mat-label>Nombre <span class="obligatorio" aria-hidden="true">*</span></mat-label>
              <input matInput formControlName="nombre" maxlength="60" placeholder="Ej.: Movilidad local" />
              @if (form.controls.nombre.touched && form.controls.nombre.invalid) {
                <mat-error>Escribe el nombre (entre 3 y 60 caracteres)</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Descripción</mat-label>
              <input matInput formControlName="descripcion" maxlength="200" placeholder="Ej.: Taxis y transporte público" />
              @if (form.controls.descripcion.invalid) { <mat-error>Máximo 200 caracteres</mat-error> }
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Límite por gasto <span class="obligatorio" aria-hidden="true">*</span></mat-label>
              <input matInput type="number" inputmode="decimal" step="0.01" min="0.01" formControlName="limite"
                     placeholder="Ej.: 150.00" />
              <mat-hint>Un gasto de esta categoría no puede superar este monto.</mat-hint>
              @if (form.controls.limite.touched && form.controls.limite.invalid) {
                <mat-error>Escribe un monto mayor que 0 y hasta 1,000,000 (máximo 2 decimales)</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Comprobante</mat-label>
              <mat-select formControlName="comprobanteObligatorio">
                <mat-option [value]="true">Obligatorio — no se envía sin él</mat-option>
                <mat-option [value]="false">Opcional</mat-option>
              </mat-select>
            </mat-form-field>

            @if (enEdicion()) {
              <mat-form-field appearance="outline">
                <mat-label>Estado</mat-label>
                <mat-select formControlName="activa">
                  <mat-option [value]="true">Activa</mat-option>
                  <mat-option [value]="false">Inactiva — no se ofrece en gastos nuevos</mat-option>
                </mat-select>
              </mat-form-field>
            }
          </form>
          <ng-container pie>
            <button mat-button type="button" (click)="cerrar()" [disabled]="guardando()">Cancelar</button>
            <button mat-flat-button type="submit" form="form-categoria" [disabled]="guardando()">
              {{ guardando() ? 'Guardando…' : 'Guardar' }}
            </button>
          </ng-container>
        </app-panel-lateral>
      }
    </section>
  `,
  styles: `
    .cabecera { display: flex; align-items: flex-start; justify-content: space-between; gap: 16px; margin-bottom: 16px; }
    h1 { font-size: 1.6rem; }
    .sub { margin: 4px 0 0; color: var(--texto-suave); }
    .lista { display: flex; flex-direction: column; gap: 10px; margin: 0; padding: 0; list-style: none; }
    .fila { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
    .fila__principal { display: flex; flex-direction: column; gap: 2px; min-width: 0; overflow-wrap: anywhere; }
    .detalle { font-size: 0.85rem; color: var(--texto-suave); }
    .inactivo { opacity: 0.65; }
    .etiqueta { margin-left: 6px; padding: 2px 8px; border-radius: 999px; background: var(--borde); font-size: 0.75rem; font-weight: 500; }
    form { display: flex; flex-direction: column; gap: 4px; }
    mat-form-field { width: 100%; }
    .vacio { padding: 24px; text-align: center; color: var(--texto-suave); }
    @media (max-width: 600px) { .cabecera { flex-direction: column; } }
  `,
})
export class CategoriasComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly avisos = inject(AvisosService);
  private readonly fb = inject(FormBuilder).nonNullable;

  protected readonly dinero = dinero;
  protected readonly cargando = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly categorias = signal<Categoria[]>([]);
  protected readonly panelAbierto = signal(false);
  protected readonly enEdicion = signal<Categoria | null>(null);
  protected readonly guardando = signal(false);

  protected readonly form = this.fb.group({
    nombre: ['', [obligatorioSinEspacios, minimoUtil(3), Validators.maxLength(60)]],
    descripcion: ['', [Validators.maxLength(200)]],
    limite: [null as number | null, [Validators.required, Validators.min(0.01), Validators.max(1000000), dosDecimales]],
    comprobanteObligatorio: [true],
    activa: [true],
  });

  ngOnInit(): void {
    this.cargar();
  }

  protected cargar(): void {
    this.cargando.set(true);
    this.api.listarCategorias(false).subscribe({
      next: (c) => {
        this.categorias.set(c);
        this.error.set(null);
        this.cargando.set(false);
      },
      error: (e: unknown) => {
        this.error.set(mensajeDeError(e));
        this.cargando.set(false);
      },
    });
  }

  protected abrirNueva(): void {
    this.enEdicion.set(null);
    this.form.reset({ nombre: '', descripcion: '', limite: null, comprobanteObligatorio: true, activa: true });
    this.panelAbierto.set(true);
  }

  protected abrirEdicion(c: Categoria): void {
    this.enEdicion.set(c);
    this.form.reset({
      nombre: c.nombre, descripcion: c.descripcion, limite: c.limite,
      comprobanteObligatorio: c.comprobanteObligatorio, activa: c.activa,
    });
    this.panelAbierto.set(true);
  }

  protected cerrar(): void {
    if (!this.guardando()) this.panelAbierto.set(false);
  }

  protected guardar(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.guardando()) return;

    const v = this.form.getRawValue();
    const datos = {
      nombre: v.nombre.trim(), descripcion: v.descripcion.trim(), limite: Number(v.limite),
      comprobanteObligatorio: v.comprobanteObligatorio, activa: v.activa,
    };
    const edicion = this.enEdicion();
    const peticion = edicion ? this.api.editarCategoria(edicion.codCategoria, datos) : this.api.crearCategoria(datos);

    this.guardando.set(true);
    peticion.subscribe({
      next: (lista) => {
        this.guardando.set(false);
        this.panelAbierto.set(false);
        this.categorias.set(lista);
        this.avisos.exito(edicion ? 'Categoría actualizada' : 'Categoría creada', 'Los cambios quedaron guardados.');
      },
      error: (e: unknown) => {
        this.guardando.set(false);
        this.avisos.error('No se pudo guardar la categoría', mensajeDeError(e));
      },
    });
  }
}
