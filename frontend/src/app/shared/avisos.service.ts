import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable, map } from 'rxjs';
import { DatosAviso, DialogoAvisoComponent } from './dialogo-aviso.component';

/** Avisos y confirmaciones de la aplicación: siempre un cuadro centrado, nunca alert ni confirm del navegador. */
@Injectable({ providedIn: 'root' })
export class AvisosService {
  private readonly dialogo = inject(MatDialog);

  exito(titulo: string, mensaje: string): void {
    this.abrir({ tipo: 'exito', titulo, mensaje });
  }

  error(titulo: string, mensaje: string): void {
    this.abrir({ tipo: 'error', titulo, mensaje });
  }

  info(titulo: string, mensaje: string): void {
    this.abrir({ tipo: 'info', titulo, mensaje });
  }

  /** Emite true si la persona confirma y false si cancela o cierra el cuadro. */
  confirmar(titulo: string, mensaje: string, textoConfirmar: string, peligroso = false): Observable<boolean> {
    return this.abrir({ tipo: 'pregunta', titulo, mensaje, textoConfirmar, peligroso }).pipe(map((r) => r === true));
  }

  private abrir(datos: DatosAviso): Observable<boolean | undefined> {
    return this.dialogo
      .open<DialogoAvisoComponent, DatosAviso, boolean>(DialogoAvisoComponent, {
        data: datos,
        width: '440px',
        maxWidth: '92vw',
        autoFocus: 'first-tabbable',
      })
      .afterClosed();
  }
}
