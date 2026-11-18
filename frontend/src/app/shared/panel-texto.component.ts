import { Component, OnInit, input, output } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { minimoUtil, obligatorioSinEspacios } from '../core/validadores';
import { PanelLateralComponent } from './panel-lateral.component';

/**
 * Panel lateral con un único campo de texto: título de una rendición, motivo de un rechazo,
 * comentario de una aprobación. El campo se valida antes de entregar el valor.
 */
@Component({
  selector: 'app-panel-texto',
  imports: [PanelLateralComponent, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <app-panel-lateral [titulo]="titulo()" (cerrar)="cerrar.emit()">
      <form (submit)="$event.preventDefault(); guardar()" novalidate id="form-texto">
        @if (obligatorio()) {
          <p class="leyenda"><span class="obligatorio">*</span> Campo obligatorio</p>
        }
        @if (ayuda()) {
          <p class="ayuda-panel">{{ ayuda() }}</p>
        }
        <mat-form-field appearance="outline" class="completo">
          <mat-label>
            {{ etiqueta() }}
            @if (obligatorio()) { <span class="obligatorio" aria-hidden="true">*</span> }
          </mat-label>
          @if (multilinea()) {
            <textarea matInput [formControl]="campo" [placeholder]="placeholder()" rows="4"
                      [attr.maxlength]="maximo()"></textarea>
          } @else {
            <input matInput [formControl]="campo" [placeholder]="placeholder()" [attr.maxlength]="maximo()" />
          }
          <mat-hint align="end">{{ campo.value.length }} / {{ maximo() }}</mat-hint>
          @if (campo.touched && campo.invalid) {
            <mat-error>
              @if (campo.hasError('obligatorio') || campo.hasError('required')) { Este dato es obligatorio }
              @else if (campo.hasError('minimoUtil')) { Escribe al menos {{ minimo() }} caracteres }
              @else { Revisa el texto escrito }
            </mat-error>
          }
        </mat-form-field>
      </form>

      <ng-container pie>
        <button mat-button type="button" (click)="cerrar.emit()" [disabled]="trabajando()">Cancelar</button>
        <button mat-flat-button type="submit" form="form-texto"
                [class.boton-peligro]="peligroso()" [disabled]="trabajando()">
          {{ trabajando() ? 'Guardando…' : textoBoton() }}
        </button>
      </ng-container>
    </app-panel-lateral>
  `,
  styles: `
    .completo { width: 100%; }
    .ayuda-panel { margin: 0 0 16px; font-size: 0.9rem; line-height: 1.4; color: var(--texto-suave); }
  `,
})
export class PanelTextoComponent implements OnInit {
  readonly titulo = input.required<string>();
  readonly etiqueta = input.required<string>();
  readonly placeholder = input('');
  readonly ayuda = input('');
  readonly textoBoton = input('Guardar');
  readonly valorInicial = input('');
  readonly obligatorio = input(true);
  readonly multilinea = input(false);
  readonly minimo = input(1);
  readonly maximo = input(100);
  readonly peligroso = input(false);
  readonly trabajando = input(false);

  readonly confirmar = output<string>();
  readonly cerrar = output<void>();

  protected readonly campo = new FormControl('', { nonNullable: true });

  ngOnInit(): void {
    const validadores = [Validators.maxLength(this.maximo())];
    if (this.obligatorio()) validadores.push(obligatorioSinEspacios, Validators.required);
    if (this.minimo() > 1) validadores.push(minimoUtil(this.minimo()));
    this.campo.setValidators(validadores);
    this.campo.setValue(this.valorInicial());
  }

  protected guardar(): void {
    this.campo.markAsTouched();
    this.campo.updateValueAndValidity();
    if (this.campo.invalid || this.trabajando()) return;
    this.confirmar.emit(this.campo.value.trim());
  }
}
