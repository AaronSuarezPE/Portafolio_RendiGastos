import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ROL_ADMINISTRADOR, ROL_APROBADOR, RespuestaSesion, Sesion } from './modelos';

const CLAVE_ALMACEN = 'rendigastos.sesion';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly base = `${environment.apiUrl}/Autenticacion`;

  private readonly _sesion = signal<Sesion | null>(this.leerGuardada());

  readonly sesion = this._sesion.asReadonly();
  readonly autenticado = computed(() => this._sesion() !== null);
  readonly esAprobador = computed(() => {
    const rol = this._sesion()?.codRol;
    return rol === ROL_APROBADOR || rol === ROL_ADMINISTRADOR;
  });
  readonly esAdministrador = computed(() => this._sesion()?.codRol === ROL_ADMINISTRADOR);

  login(email: string, clave: string): Observable<RespuestaSesion> {
    return this.http
      .post<RespuestaSesion>(`${this.base}/PostIngresar`, { email: email.trim(), clave })
      .pipe(tap((r) => this.guardar(r)));
  }

  registrar(nombre: string, email: string, clave: string): Observable<RespuestaSesion> {
    return this.http
      .post<RespuestaSesion>(`${this.base}/PostRegistrar`, { nombre: nombre.trim(), email: email.trim(), clave })
      .pipe(tap((r) => this.guardar(r)));
  }

  token(): string | null {
    return this._sesion()?.token ?? null;
  }

  cerrarSesion(): void {
    this._sesion.set(null);
    this.escribir(null);
    void this.router.navigate(['/acceso']);
  }

  private guardar(respuesta: RespuestaSesion): void {
    const sesion: Sesion = {
      token: respuesta.token,
      codUsuario: respuesta.codUsuario,
      nombre: respuesta.nombre,
      rol: respuesta.rol,
      codRol: respuesta.codRol,
      expira: Date.now() + respuesta.minutosVigencia * 60_000,
    };
    this._sesion.set(sesion);
    this.escribir(sesion);
  }

  private leerGuardada(): Sesion | null {
    try {
      const texto = sessionStorage.getItem(CLAVE_ALMACEN);
      if (!texto) return null;

      const sesion = JSON.parse(texto) as Sesion;
      return sesion.expira > Date.now() ? sesion : null;
    } catch {
      return null;
    }
  }

  private escribir(sesion: Sesion | null): void {
    try {
      if (sesion) sessionStorage.setItem(CLAVE_ALMACEN, JSON.stringify(sesion));
      else sessionStorage.removeItem(CLAVE_ALMACEN);
    } catch {
      // Sin almacenamiento disponible: la sesión sigue funcionando mientras no se recargue la página.
    }
  }
}
