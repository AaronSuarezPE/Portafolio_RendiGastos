import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';

export type TipoAviso = 'exito' | 'error' | 'info' | 'pregunta';

export interface DatosAviso {
  tipo: TipoAviso;
  titulo: string;
  mensaje: string;
  /** Si viene, el cuadro es una pregunta con dos botones: cancelar y este. */
  textoConfirmar?: string;
  peligroso?: boolean;
}

const ICONOS: Record<TipoAviso, string> = { exito: '✓', error: '!', info: 'i', pregunta: '?' };

/** Cuadro centrado para avisos y confirmaciones. Es el único tipo de aviso de la aplicación. */
@Component({
  selector: 'app-dialogo-aviso',
  imports: [MatDialogModule, MatButtonModule],
  template: `
    <div class="aviso-central">
      <span class="icono" [class]="'icono icono--' + datos.tipo" aria-hidden="true">{{ icono }}</span>
      <h2 mat-dialog-title>{{ datos.titulo }}</h2>
      <mat-dialog-content>{{ datos.mensaje }}</mat-dialog-content>
      <mat-dialog-actions align="center">
        @if (datos.textoConfirmar) {
          <button mat-button type="button" [mat-dialog-close]="false">Cancelar</button>
          <button
            mat-flat-button
            type="button"
            cdkFocusInitial
            [mat-dialog-close]="true"
            [class.boton-peligro]="datos.peligroso"
          >
            {{ datos.textoConfirmar }}
          </button>
        } @else {
          <button mat-flat-button type="button" cdkFocusInitial [mat-dialog-close]="true">Entendido</button>
        }
      </mat-dialog-actions>
    </div>
  `,
  styles: `
    .aviso-central {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 28px 12px 8px;
      text-align: center;
    }

    h2 {
      padding: 0;
      margin: 0 0 8px;
    }

    h2::before {
      display: none;
    }

    mat-dialog-content {
      max-width: 380px;
      color: var(--texto-suave);
      line-height: 1.5;
      white-space: pre-line;
    }

    mat-dialog-actions {
      gap: 8px;
      padding-top: 20px;
    }

    .icono {
      display: grid;
      width: 56px;
      height: 56px;
      margin-bottom: 14px;
      border-radius: 50%;
      font-size: 1.8rem;
      font-weight: 700;
      place-items: center;
    }

    .icono--exito { background: var(--ok-fondo); color: var(--ok); }
    .icono--error { background: var(--error-fondo); color: var(--error); }
    .icono--info { background: #e4eefc; color: #0b57d0; }
    .icono--pregunta { background: var(--aviso-fondo); color: var(--aviso); }

    .boton-peligro {
      --mdc-filled-button-container-color: var(--error);
      --mat-button-filled-container-color: var(--error);
      --mdc-filled-button-label-text-color: #fff;
      --mat-button-filled-label-text-color: #fff;
    }
  `,
})
export class DialogoAvisoComponent {
  protected readonly datos = inject<DatosAviso>(MAT_DIALOG_DATA);
  protected readonly icono = ICONOS[this.datos.tipo];
}
