import { Component, effect, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';
import { NotificacionesService } from './core/notificaciones.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatButtonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {
  protected readonly auth = inject(AuthService);
  protected readonly notificaciones = inject(NotificacionesService);

  constructor() {
    // El contador de avisos solo corre mientras hay una sesión abierta.
    effect(() => {
      if (this.auth.autenticado()) this.notificaciones.iniciar();
      else this.notificaciones.detener();
    });
  }
}
