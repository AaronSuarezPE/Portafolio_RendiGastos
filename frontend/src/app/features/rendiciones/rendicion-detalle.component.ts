import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { mensajeDeError } from '../../core/errores';
import { claseEstado, dinero } from '../../core/formato';
import {
  Categoria, ESTADO_BORRADOR, ESTADO_ENVIADA, ESTADO_RECHAZADA, Gasto, RendicionDetalle,
} from '../../core/modelos';
import { formatearTamano } from '../../core/validadores';
import { AvisosService } from '../../shared/avisos.service';
import { PanelTextoComponent } from '../../shared/panel-texto.component';
import { PanelGastoComponent, ResultadoGasto } from './panel-gasto.component';

type PanelAbierto = 'ninguno' | 'titulo' | 'gasto' | 'rechazo' | 'aprobacion';

@Component({
  selector: 'app-rendicion-detalle',
  imports: [RouterLink, MatButtonModule, PanelTextoComponent, PanelGastoComponent],
  templateUrl: './rendicion-detalle.component.html',
  styleUrl: './rendicion-detalle.component.scss',
})
export class RendicionDetalleComponent implements OnInit {
  readonly id = input.required<string>();

  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly avisos = inject(AvisosService);
  private readonly router = inject(Router);

  protected readonly dinero = dinero;
  protected readonly claseEstado = claseEstado;
  protected readonly formatearTamano = formatearTamano;

  protected readonly cargando = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly detalle = signal<RendicionDetalle | null>(null);
  protected readonly categorias = signal<Categoria[]>([]);
  protected readonly panel = signal<PanelAbierto>('ninguno');
  protected readonly gastoEnEdicion = signal<Gasto | null>(null);
  protected readonly trabajando = signal(false);

  protected readonly esDueno = computed(() => this.detalle()?.codUsuario === this.auth.sesion()?.codUsuario);
  protected readonly esBorrador = computed(() => this.detalle()?.codEstado === ESTADO_BORRADOR);
  protected readonly puedeEditar = computed(() => this.esDueno() && this.esBorrador());
  protected readonly puedeReabrir = computed(() => this.esDueno() && this.detalle()?.codEstado === ESTADO_RECHAZADA);
  protected readonly puedeDecidir = computed(
    () => !this.esDueno() && this.auth.esAprobador() && this.detalle()?.codEstado === ESTADO_ENVIADA,
  );

  /** Gastos de categorías que exigen comprobante y todavía no lo tienen. */
  protected readonly sinComprobante = computed(
    () => (this.detalle()?.gastos ?? []).filter((g) => g.comprobanteObligatorio && !g.comprobante).length,
  );

  ngOnInit(): void {
    this.cargar();
    this.api.listarCategorias(true).subscribe({
      next: (c) => this.categorias.set(c),
      error: (e: unknown) => this.avisos.error('No se pudieron cargar las categorías', mensajeDeError(e)),
    });
  }

  protected cargar(): void {
    this.api.obtener(Number(this.id())).subscribe({
      next: (d) => {
        this.detalle.set(d);
        this.error.set(null);
        this.cargando.set(false);
      },
      error: (e: unknown) => {
        this.error.set(mensajeDeError(e));
        this.cargando.set(false);
      },
    });
  }

  protected cerrarPanel(): void {
    if (this.trabajando()) return;
    this.panel.set('ninguno');
    this.gastoEnEdicion.set(null);
  }

  protected nuevoGasto(): void {
    this.gastoEnEdicion.set(null);
    this.panel.set('gasto');
  }

  protected corregirGasto(gasto: Gasto): void {
    this.gastoEnEdicion.set(gasto);
    this.panel.set('gasto');
  }

  // ---- Título ----
  protected guardarTitulo(titulo: string): void {
    this.ejecutar(this.api.editarRendicion(this.codigo(), titulo), 'No se pudo cambiar el título', () => {
      this.cerrarPanelForzado();
      this.cargar();
      this.avisos.exito('Título actualizado', 'El nuevo título de la rendición quedó guardado.');
    });
  }

  // ---- Gastos ----
  protected guardarGasto(r: ResultadoGasto): void {
    const existente = this.gastoEnEdicion();
    this.trabajando.set(true);

    const guardado: Observable<unknown> = existente
      ? this.api.editarGasto(existente.codGasto, r.datos)
      : this.api.crearGasto(this.codigo(), r.datos);

    guardado.subscribe({
      next: (res) => {
        const codGasto = existente?.codGasto ?? (res as { codGasto: number }).codGasto;
        this.despuesDeGuardarGasto(codGasto, r, existente !== null);
      },
      error: (e: unknown) => {
        this.trabajando.set(false);
        this.avisos.error('No se pudo guardar el gasto', mensajeDeError(e));
      },
    });
  }

  private despuesDeGuardarGasto(codGasto: number, r: ResultadoGasto, edicion: boolean): void {
    const terminar = (mensaje: string) => {
      this.trabajando.set(false);
      this.cerrarPanelForzado();
      this.cargar();
      this.avisos.exito(edicion ? 'Gasto actualizado' : 'Gasto agregado', mensaje);
    };

    if (r.archivo) {
      this.api.subirComprobante(codGasto, r.archivo).subscribe({
        next: () => terminar('El gasto y su comprobante quedaron guardados.'),
        error: (e: unknown) => {
          // El gasto ya existe; solo falló el archivo. Se deja el panel abierto para reintentar.
          this.trabajando.set(false);
          this.cargar();
          this.gastoEnEdicion.set(null);
          this.cerrarPanelForzado();
          this.avisos.error(
            'El gasto se guardó, pero el comprobante no',
            `${mensajeDeError(e)} Abre el gasto con «Corregir» para volver a adjuntarlo.`,
          );
        },
      });
    } else if (r.quitarComprobante) {
      this.api.quitarComprobante(codGasto).subscribe({
        next: () => terminar('El gasto quedó guardado sin comprobante.'),
        error: (e: unknown) => {
          this.trabajando.set(false);
          this.avisos.error('No se pudo quitar el comprobante', mensajeDeError(e));
        },
      });
    } else {
      terminar('Los datos del gasto quedaron guardados.');
    }
  }

  protected eliminarGasto(gasto: Gasto): void {
    this.avisos
      .confirmar('¿Eliminar este gasto?', `Se eliminará «${gasto.descripcion}» y su comprobante. No se puede deshacer.`, 'Eliminar', true)
      .subscribe((ok) => {
        if (!ok) return;
        this.ejecutar(this.api.eliminarGasto(gasto.codGasto), 'No se pudo eliminar el gasto', () => {
          this.cargar();
          this.avisos.exito('Gasto eliminado', 'El gasto ya no forma parte de la rendición.');
        });
      });
  }

  protected verComprobante(gasto: Gasto): void {
    this.api.descargarComprobante(gasto.codGasto).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        window.open(url, '_blank', 'noopener');
        setTimeout(() => URL.revokeObjectURL(url), 60_000);
      },
      error: (e: unknown) => this.avisos.error('No se pudo abrir el comprobante', mensajeDeError(e)),
    });
  }

  // ---- Flujo ----
  protected enviar(): void {
    const d = this.detalle();
    if (!d) return;

    if (d.gastos.length === 0) {
      this.avisos.info('Agrega al menos un gasto', 'Una rendición vacía no se puede enviar. Agrega tus gastos y vuelve a intentarlo.');
      return;
    }
    if (this.sinComprobante() > 0) {
      this.avisos.info(
        'Faltan comprobantes',
        `Hay ${this.sinComprobante()} gasto(s) de categorías que exigen comprobante. Corrígelos y adjunta el archivo antes de enviar.`,
      );
      return;
    }

    this.avisos
      .confirmar(
        '¿Enviar la rendición?',
        `Se enviará «${d.titulo}» por ${dinero(d.total)} a ${d.aprobador ?? 'tu aprobador'}. Mientras esté en revisión no podrás editarla.`,
        'Enviar',
      )
      .subscribe((ok) => {
        if (!ok) return;
        this.ejecutar(this.api.enviar(this.codigo()), 'No se pudo enviar la rendición', () => {
          this.cargar();
          this.avisos.exito('Rendición enviada', 'Tu aprobador recibió un aviso. Te avisaremos cuando la revise.');
        });
      });
  }

  protected eliminar(): void {
    const d = this.detalle();
    if (!d) return;
    this.avisos
      .confirmar('¿Eliminar la rendición?', `Se eliminará «${d.titulo}» con todos sus gastos y comprobantes. No se puede deshacer.`, 'Eliminar', true)
      .subscribe((ok) => {
        if (!ok) return;
        this.ejecutar(this.api.eliminarRendicion(this.codigo()), 'No se pudo eliminar la rendición', () => {
          void this.router.navigate(['/rendiciones']);
          this.avisos.exito('Rendición eliminada', 'La rendición se eliminó correctamente.');
        });
      });
  }

  protected reabrir(): void {
    this.avisos
      .confirmar('¿Corregir y volver a enviar?', 'La rendición volverá a borrador para que corrijas lo que se observó.', 'Reabrir')
      .subscribe((ok) => {
        if (!ok) return;
        this.ejecutar(this.api.reabrir(this.codigo()), 'No se pudo reabrir la rendición', () => {
          this.cargar();
          this.avisos.exito('Rendición reabierta', 'Ya puedes corregirla y enviarla otra vez.');
        });
      });
  }

  protected aprobar(comentario: string): void {
    this.ejecutar(this.api.aprobar(this.codigo(), comentario), 'No se pudo aprobar', () => {
      this.cerrarPanelForzado();
      this.cargar();
      this.avisos.exito('Rendición aprobada', 'La persona recibirá un aviso con tu decisión.');
    });
  }

  protected rechazar(motivo: string): void {
    this.ejecutar(this.api.rechazar(this.codigo(), motivo), 'No se pudo rechazar', () => {
      this.cerrarPanelForzado();
      this.cargar();
      this.avisos.exito('Rendición rechazada', 'La persona recibirá un aviso con tu motivo para que la corrija.');
    });
  }

  private codigo(): number {
    return Number(this.id());
  }

  private cerrarPanelForzado(): void {
    this.panel.set('ninguno');
    this.gastoEnEdicion.set(null);
  }

  private ejecutar(peticion: Observable<unknown>, titulo: string, alExito: () => void): void {
    if (this.trabajando()) return;
    this.trabajando.set(true);
    peticion.subscribe({
      next: () => {
        this.trabajando.set(false);
        alExito();
      },
      error: (e: unknown) => {
        this.trabajando.set(false);
        this.avisos.error(titulo, mensajeDeError(e));
      },
    });
  }
}
