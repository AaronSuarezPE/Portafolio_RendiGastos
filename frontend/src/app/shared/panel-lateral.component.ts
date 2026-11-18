import { Component, ElementRef, HostListener, OnDestroy, afterNextRender, inject, input, output } from '@angular/core';

/**
 * Panel que entra por la derecha. Título y botones quedan fijos; solo el cuerpo se desplaza.
 * Se cierra con la cruz, la tecla Escape o un clic en el fondo.
 */
@Component({
  selector: 'app-panel-lateral',
  template: `
    <div class="fondo" (click)="cerrar.emit()" aria-hidden="true"></div>
    <section class="panel" role="dialog" aria-modal="true" [attr.aria-label]="titulo()">
      <header class="panel__cabecera">
        <h2>{{ titulo() }}</h2>
        <button type="button" class="panel__cerrar" aria-label="Cerrar" (click)="cerrar.emit()">×</button>
      </header>
      <div class="panel__cuerpo"><ng-content /></div>
      <footer class="panel__pie"><ng-content select="[pie]" /></footer>
    </section>
  `,
  styleUrl: './panel-lateral.component.scss',
})
export class PanelLateralComponent implements OnDestroy {
  readonly titulo = input.required<string>();
  readonly cerrar = output<void>();

  private readonly elemento = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly anterior = document.activeElement as HTMLElement | null;

  constructor() {
    document.body.style.overflow = 'hidden';

    afterNextRender(() => {
      const primero = this.elemento.nativeElement.querySelector<HTMLElement>(
        'input, textarea, select, button:not(.panel__cerrar)',
      );
      (primero ?? this.elemento.nativeElement.querySelector<HTMLElement>('.panel__cerrar'))?.focus();
    });
  }

  @HostListener('document:keydown.escape')
  protected alEscape(): void {
    this.cerrar.emit();
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
    this.anterior?.focus();
  }
}
