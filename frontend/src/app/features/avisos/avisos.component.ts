import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { ApiService } from '../../core/api.service';
import { mensajeDeError } from '../../core/errores';
import { Aviso } from '../../core/modelos';
import { NotificacionesService } from '../../core/notificaciones.service';
import { AvisosService } from '../../shared/avisos.service';

@Component({
  selector: 'app-avisos',
  imports: [RouterLink, MatButtonModule],
  template: `
    <section>
      <header class="cabecera">
        <div>
          <h1>Avisos</h1>
          <p class="sub">Novedades sobre tus rendiciones y las que debes revisar.</p>
        </div>
        @if (hayNoLeidos()) {
          <button mat-stroked-button type="button" (click)="marcarTodos()">Marcar todos como leídos</button>
        }
      </header>

      <div class="tarjeta push">
        <div>
          <strong>Avisos en este dispositivo</strong>
          <p class="sub">
            @switch (notificaciones.estadoPush()) {
              @case ('activo') { Activados: te avisaremos aunque tengas la aplicación cerrada. }
              @case ('inactivo') { Actívalos para enterarte al instante de aprobaciones, rechazos y envíos. }
              @case ('bloqueado') { El navegador los tiene bloqueados. Puedes permitirlos desde la configuración del sitio. }
              @case ('sin-servidor') { El servidor aún no tiene activados los avisos en el dispositivo; verás los avisos aquí dentro. }
              @default { Este navegador no admite avisos en el dispositivo; verás los avisos aquí dentro. }
            }
          </p>
        </div>
        @if (notificaciones.estadoPush() === 'inactivo') {
          <button mat-flat-button type="button" (click)="activar()" [disabled]="trabajando()">Activar avisos</button>
        } @else if (notificaciones.estadoPush() === 'activo') {
          <button mat-stroked-button type="button" (click)="desactivar()" [disabled]="trabajando()">Desactivar</button>
        }
      </div>

      @if (error(); as mensaje) {
        <p class="aviso aviso--error" role="alert">{{ mensaje }} <button mat-button type="button" (click)="cargar()">Reintentar</button></p>
      }

      @if (cargando()) {
        <p class="vacio" role="status">Cargando…</p>
      } @else if (lista().length === 0 && !error()) {
        <div class="tarjeta vacio"><p>No tienes avisos todavía.</p></div>
      } @else {
        <ul class="lista">
          @for (a of lista(); track a.codAviso) {
            <li class="tarjeta aviso-item" [class.sin-leer]="!a.leido">
              <div>
                <strong>{{ a.titulo }}</strong>
                <p>{{ a.mensaje }}</p>
                <span class="fecha">{{ a.fecha }} {{ a.hora }}</span>
              </div>
              <div class="acciones">
                @if (a.codRendicion) {
                  <a mat-stroked-button [routerLink]="['/rendiciones', a.codRendicion]" (click)="leer(a)">Ver rendición</a>
                }
                @if (!a.leido) {
                  <button mat-button type="button" (click)="leer(a)">Marcar leído</button>
                }
              </div>
            </li>
          }
        </ul>
      }
    </section>
  `,
  styles: `
    .cabecera { display: flex; align-items: flex-start; justify-content: space-between; gap: 16px; margin-bottom: 16px; }
    h1 { font-size: 1.6rem; }
    .sub { margin: 4px 0 0; font-size: 0.9rem; color: var(--texto-suave); }
    .push { display: flex; align-items: center; justify-content: space-between; gap: 16px; margin-bottom: 16px; }
    .lista { display: flex; flex-direction: column; gap: 10px; margin: 0; padding: 0; list-style: none; }
    .aviso-item { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
    .aviso-item p { margin: 4px 0; overflow-wrap: anywhere; }
    .sin-leer { border-left: 4px solid var(--mat-sys-primary); }
    .fecha { font-size: 0.8rem; color: var(--texto-suave); }
    .acciones { display: flex; flex-direction: column; gap: 4px; flex-shrink: 0; }
    .vacio { padding: 24px; text-align: center; color: var(--texto-suave); }
    @media (max-width: 600px) {
      .cabecera, .push, .aviso-item { flex-direction: column; align-items: stretch; }
      .acciones { flex-direction: row; flex-wrap: wrap; }
    }
  `,
})
export class AvisosComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly avisos = inject(AvisosService);
  protected readonly notificaciones = inject(NotificacionesService);

  protected readonly cargando = signal(true);
  protected readonly trabajando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly lista = signal<Aviso[]>([]);

  ngOnInit(): void {
    this.cargar();
    void this.notificaciones.evaluarPush();
  }

  protected hayNoLeidos(): boolean {
    return this.lista().some((a) => !a.leido);
  }

  protected cargar(): void {
    this.cargando.set(true);
    this.api.listarAvisos().subscribe({
      next: (datos) => {
        this.lista.set(datos);
        this.error.set(null);
        this.cargando.set(false);
      },
      error: (e: unknown) => {
        this.error.set(mensajeDeError(e));
        this.cargando.set(false);
      },
    });
  }

  protected leer(aviso: Aviso): void {
    if (aviso.leido) return;
    this.api.marcarLeido(aviso.codAviso).subscribe({
      next: () => {
        this.lista.update((l) => l.map((a) => (a.codAviso === aviso.codAviso ? { ...a, leido: true } : a)));
        this.notificaciones.actualizar();
      },
      error: (e: unknown) => this.avisos.error('No se pudo marcar el aviso', mensajeDeError(e)),
    });
  }

  protected marcarTodos(): void {
    this.api.marcarTodosLeidos().subscribe({
      next: () => {
        this.lista.update((l) => l.map((a) => ({ ...a, leido: true })));
        this.notificaciones.actualizar();
      },
      error: (e: unknown) => this.avisos.error('No se pudieron marcar los avisos', mensajeDeError(e)),
    });
  }

  protected async activar(): Promise<void> {
    this.trabajando.set(true);
    try {
      await this.notificaciones.activarPush();
      this.avisos.exito('Avisos activados', 'Este dispositivo recibirá un aviso cuando haya novedades.');
    } catch (e) {
      this.avisos.error('No se pudieron activar los avisos', e instanceof Error ? e.message : mensajeDeError(e));
    } finally {
      this.trabajando.set(false);
    }
  }

  protected async desactivar(): Promise<void> {
    this.trabajando.set(true);
    try {
      await this.notificaciones.desactivarPush();
      this.avisos.exito('Avisos desactivados', 'Este dispositivo ya no recibirá avisos. Seguirás viéndolos aquí.');
    } catch (e) {
      this.avisos.error('No se pudieron desactivar los avisos', e instanceof Error ? e.message : mensajeDeError(e));
    } finally {
      this.trabajando.set(false);
    }
  }
}
