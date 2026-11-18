import { Routes } from '@angular/router';
import { administradorGuard, aprobadorGuard, authGuard, sinSesionGuard } from './core/auth.guard';

export const routes: Routes = [
  {
    path: 'acceso',
    canActivate: [sinSesionGuard],
    title: 'Ingresar · RendiGastos',
    loadComponent: () => import('./features/acceso/acceso.component').then((m) => m.AccesoComponent),
  },
  {
    path: 'rendiciones',
    canActivate: [authGuard],
    title: 'Mis rendiciones · RendiGastos',
    data: { modo: 'propias' },
    loadComponent: () => import('./features/rendiciones/rendiciones.component').then((m) => m.RendicionesComponent),
  },
  {
    path: 'rendiciones/:id',
    canActivate: [authGuard],
    title: 'Rendición · RendiGastos',
    loadComponent: () =>
      import('./features/rendiciones/rendicion-detalle.component').then((m) => m.RendicionDetalleComponent),
  },
  {
    path: 'por-aprobar',
    canActivate: [authGuard, aprobadorGuard],
    title: 'Por aprobar · RendiGastos',
    data: { modo: 'aprobar' },
    loadComponent: () => import('./features/rendiciones/rendiciones.component').then((m) => m.RendicionesComponent),
  },
  {
    path: 'avisos',
    canActivate: [authGuard],
    title: 'Avisos · RendiGastos',
    loadComponent: () => import('./features/avisos/avisos.component').then((m) => m.AvisosComponent),
  },
  {
    path: 'usuarios',
    canActivate: [authGuard, administradorGuard],
    title: 'Usuarios · RendiGastos',
    loadComponent: () => import('./features/admin/usuarios.component').then((m) => m.UsuariosComponent),
  },
  {
    path: 'categorias',
    canActivate: [authGuard, administradorGuard],
    title: 'Categorías · RendiGastos',
    loadComponent: () => import('./features/admin/categorias.component').then((m) => m.CategoriasComponent),
  },
  { path: '', pathMatch: 'full', redirectTo: 'rendiciones' },
  { path: '**', redirectTo: 'rendiciones' },
];
