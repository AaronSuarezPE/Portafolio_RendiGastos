import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { mensajeDeError } from '../../core/errores';
import { claseEstado, dinero } from '../../core/formato';
import { RendicionResumen } from '../../core/modelos';
import { AvisosService } from '../../shared/avisos.service';
import { PanelTextoComponent } from '../../shared/panel-texto.component';

/** Lista de rendiciones: las propias (empleado) o las que esperan decisión (aprobador/administrador). */
@Component({
  selector: 'app-rendiciones',
  imports: [RouterLink, FormsModule, MatButtonModule, MatFormFieldModule, MatSelectModule, PanelTextoComponent],
  templateUrl: './rendiciones.component.html',
  styleUrl: './rendiciones.component.scss',
})
export class RendicionesComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly avisos = inject(AvisosService);
  private readonly router = inject(Router);
  protected readonly modoAprobar = inject(ActivatedRoute).snapshot.data['modo'] === 'aprobar';

  protected readonly dinero = dinero;
  protected readonly claseEstado = claseEstado;

  protected readonly cargando = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly lista = signal<RendicionResumen[]>([]);
  protected readonly panelNueva = signal(false);
  protected readonly guardando = signal(false);
  protected estado: number | null = null;

  protected readonly estados = [
    { codigo: null, nombre: 'Todos los estados' },
    { codigo: 1, nombre: 'Borrador' },
    { codigo: 2, nombre: 'Enviada' },
    { codigo: 3, nombre: 'Aprobada' },
    { codigo: 4, nombre: 'Rechazada' },
  ];

  ngOnInit(): void {
    // En la bandeja del aprobador se muestran primero las que esperan decisión.
    if (this.modoAprobar) this.estado = 2;
    this.cargar();
  }

  protected cargar(): void {
    this.cargando.set(true);
    this.error.set(null);
    const peticion = this.modoAprobar ? this.api.listarPorAprobar(this.estado) : this.api.listarPropias(this.estado);
    peticion.subscribe({
      next: (datos) => {
        this.lista.set(datos);
        this.cargando.set(false);
      },
      error: (e: unknown) => {
        this.error.set(mensajeDeError(e));
        this.cargando.set(false);
      },
    });
  }

  protected crear(titulo: string): void {
    this.guardando.set(true);
    this.api.crearRendicion(titulo).subscribe({
      next: (r) => {
        this.guardando.set(false);
        this.panelNueva.set(false);
        void this.router.navigate(['/rendiciones', r.codRendicion]);
      },
      error: (e: unknown) => {
        this.guardando.set(false);
        this.avisos.error('No se pudo crear la rendición', mensajeDeError(e));
      },
    });
  }
}
