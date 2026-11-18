import { Injector, afterNextRender } from '@angular/core';

/** Lleva el foco al primer campo con error, una vez que la pantalla ya pintó los mensajes. */
export function enfocarPrimerError(injector: Injector, raiz: HTMLElement): void {
  afterNextRender(
    () => {
      const campo = raiz.querySelector<HTMLElement>(
        '.mat-form-field-invalid input, .mat-form-field-invalid textarea',
      );
      campo?.focus();
      campo?.scrollIntoView({ block: 'center', behavior: 'smooth' });
    },
    { injector },
  );
}
