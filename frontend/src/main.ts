import { isDevMode } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';

bootstrapApplication(AppComponent, appConfig)
  .then(() => {
    // En desarrollo no se registra: la copia guardada ocultaría los cambios recientes.
    if (!isDevMode() && 'serviceWorker' in navigator) {
      navigator.serviceWorker.register('sw.js').catch((e) => console.warn('No se pudo registrar el service worker', e));
    }
  })
  .catch((err) => console.error(err));
