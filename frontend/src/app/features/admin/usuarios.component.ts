import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ApiService } from '../../core/api.service';
import { mensajeDeError } from '../../core/errores';
import { ROL_ADMINISTRADOR, ROL_APROBADOR, ROL_EMPLEADO, Usuario } from '../../core/modelos';
import { claveSegura, correoValido, minimoUtil, obligatorioSinEspacios } from '../../core/validadores';
import { AvisosService } from '../../shared/avisos.service';
import { PanelLateralComponent } from '../../shared/panel-lateral.component';

@Component({
  selector: 'app-usuarios',
  imports: [
    PanelLateralComponent, ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule,
  ],
  template: `
    <section>
      <header class="cabecera">
        <div>
          <h1>Usuarios</h1>
          <p class="sub">Crea cuentas, asigna el rol y el aprobador de cada persona.</p>
        </div>
        <button mat-flat-button type="button" (click)="abrirNuevo()">Nuevo usuario</button>
      </header>

      @if (error(); as mensaje) {
        <p class="aviso aviso--error" role="alert">{{ mensaje }} <button mat-button type="button" (click)="cargar()">Reintentar</button></p>
      }

      @if (cargando()) {
        <p class="vacio" role="status">Cargando…</p>
      } @else {
        <ul class="lista">
          @for (u of usuarios(); track u.codUsuario) {
            <li class="tarjeta fila" [class.inactivo]="!u.activo">
              <div class="fila__principal">
                <strong>{{ u.nombre }} @if (!u.activo) { <span class="etiqueta">Inactivo</span> }</strong>
                <span class="detalle">{{ u.email }}</span>
                <span class="detalle">{{ u.rol }}@if (u.aprobador) { · aprobador: {{ u.aprobador }} }</span>
              </div>
              <button mat-stroked-button type="button" (click)="abrirEdicion(u)">Editar</button>
            </li>
          }
        </ul>
      }

      @if (panelAbierto()) {
        <app-panel-lateral [titulo]="enEdicion() ? 'Editar usuario' : 'Nuevo usuario'" (cerrar)="cerrar()">
          <form [formGroup]="form" (ngSubmit)="guardar()" novalidate id="form-usuario">
            <p class="leyenda"><span class="obligatorio">*</span> Campo obligatorio</p>

            <mat-form-field appearance="outline">
              <mat-label>Nombre <span class="obligatorio" aria-hidden="true">*</span></mat-label>
              <input matInput formControlName="nombre" maxlength="100" placeholder="Ej.: Ana Torres" />
              @if (form.controls.nombre.touched && form.controls.nombre.invalid) {
                <mat-error>Escribe el nombre (entre 3 y 100 caracteres)</mat-error>
              }
            </mat-form-field>

            @if (!enEdicion()) {
              <mat-form-field appearance="outline">
                <mat-label>Correo <span class="obligatorio" aria-hidden="true">*</span></mat-label>
                <input matInput type="email" formControlName="email" maxlength="150" placeholder="Ej.: ana@empresa.com" />
                @if (form.controls.email.touched && form.controls.email.invalid) {
                  <mat-error>Escribe un correo válido, por ejemplo ana&#64;empresa.com</mat-error>
                }
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Clave inicial <span class="obligatorio" aria-hidden="true">*</span></mat-label>
                <input matInput type="text" formControlName="clave" maxlength="100" placeholder="Mínimo 8, con letras y números"
                       autocomplete="off" />
                <mat-hint>La persona podrá usarla para ingresar; compártesela por un canal seguro.</mat-hint>
                @if (form.controls.clave.touched && form.controls.clave.invalid) {
                  <mat-error>La clave debe tener al menos 8 caracteres, con letras y números</mat-error>
                }
              </mat-form-field>
            }

            <mat-form-field appearance="outline">
              <mat-label>Rol <span class="obligatorio" aria-hidden="true">*</span></mat-label>
              <mat-select formControlName="codRol" placeholder="Elige un rol">
                <mat-option [value]="1">Empleado — crea y envía rendiciones</mat-option>
                <mat-option [value]="2">Aprobador — además aprueba o rechaza</mat-option>
                <mat-option [value]="3">Administrador — además gestiona usuarios y categorías</mat-option>
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Aprobador asignado</mat-label>
              <mat-select formControlName="codAprobador" placeholder="Sin aprobador">
                <mat-option [value]="null">Sin aprobador</mat-option>
                @for (a of aprobadores(); track a.codUsuario) {
                  @if (a.codUsuario !== enEdicion()?.codUsuario) {
                    <mat-option [value]="a.codUsuario">{{ a.nombre }} ({{ a.rol }})</mat-option>
                  }
                }
              </mat-select>
              <mat-hint>Sin aprobador, la persona no podrá enviar rendiciones.</mat-hint>
            </mat-form-field>

            @if (enEdicion()) {
              <mat-form-field appearance="outline">
                <mat-label>Estado</mat-label>
                <mat-select formControlName="activo">
                  <mat-option [value]="true">Activo</mat-option>
                  <mat-option [value]="false">Inactivo — no puede ingresar</mat-option>
                </mat-select>
              </mat-form-field>
            }
          </form>
          <ng-container pie>
            <button mat-button type="button" (click)="cerrar()" [disabled]="guardando()">Cancelar</button>
            <button mat-flat-button type="submit" form="form-usuario" [disabled]="guardando()">
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
export class UsuariosComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly avisos = inject(AvisosService);
  private readonly fb = inject(FormBuilder).nonNullable;

  protected readonly cargando = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly usuarios = signal<Usuario[]>([]);
  protected readonly aprobadores = signal<Usuario[]>([]);
  protected readonly panelAbierto = signal(false);
  protected readonly enEdicion = signal<Usuario | null>(null);
  protected readonly guardando = signal(false);

  protected readonly form = this.fb.group({
    nombre: ['', [obligatorioSinEspacios, minimoUtil(3), Validators.maxLength(100)]],
    email: ['', [obligatorioSinEspacios, correoValido, Validators.maxLength(150)]],
    clave: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100), claveSegura]],
    codRol: [ROL_EMPLEADO as number, [Validators.required]],
    codAprobador: [null as number | null],
    activo: [true],
  });

  ngOnInit(): void {
    this.cargar();
  }

  protected cargar(): void {
    this.cargando.set(true);
    this.api.listarUsuarios().subscribe({
      next: (u) => {
        this.usuarios.set(u);
        this.error.set(null);
        this.cargando.set(false);
      },
      error: (e: unknown) => {
        this.error.set(mensajeDeError(e));
        this.cargando.set(false);
      },
    });
    this.api.listarAprobadores().subscribe({ next: (a) => this.aprobadores.set(a), error: () => undefined });
  }

  protected abrirNuevo(): void {
    this.enEdicion.set(null);
    this.form.reset({ nombre: '', email: '', clave: '', codRol: ROL_EMPLEADO, codAprobador: null, activo: true });
    this.form.controls.email.enable();
    this.form.controls.clave.enable();
    this.panelAbierto.set(true);
  }

  protected abrirEdicion(u: Usuario): void {
    this.enEdicion.set(u);
    this.form.reset({ nombre: u.nombre, email: u.email, clave: '', codRol: u.codRol, codAprobador: u.codAprobador, activo: u.activo });
    // En una edición no se cambian el correo ni la clave.
    this.form.controls.email.disable();
    this.form.controls.clave.disable();
    this.panelAbierto.set(true);
  }

  protected cerrar(): void {
    if (!this.guardando()) this.panelAbierto.set(false);
  }

  protected guardar(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.guardando()) return;

    const v = this.form.getRawValue();
    const edicion = this.enEdicion();
    const peticion = edicion
      ? this.api.editarUsuario(edicion.codUsuario, {
          nombre: v.nombre.trim(), codRol: v.codRol, codAprobador: v.codAprobador, activo: v.activo,
        })
      : this.api.crearUsuario({
          nombre: v.nombre.trim(), email: v.email.trim(), clave: v.clave, codRol: v.codRol, codAprobador: v.codAprobador,
        });

    this.guardando.set(true);
    peticion.subscribe({
      next: () => {
        this.guardando.set(false);
        this.panelAbierto.set(false);
        this.cargar();
        this.avisos.exito(edicion ? 'Usuario actualizado' : 'Usuario creado', 'Los cambios quedaron guardados.');
      },
      error: (e: unknown) => {
        this.guardando.set(false);
        this.avisos.error('No se pudo guardar el usuario', mensajeDeError(e));
      },
    });
  }

  protected readonly roles = { ROL_EMPLEADO, ROL_APROBADOR, ROL_ADMINISTRADOR };
}
