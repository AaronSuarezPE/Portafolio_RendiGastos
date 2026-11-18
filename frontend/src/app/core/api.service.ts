import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  Aviso, Categoria, ClavePush, DatosGasto, RendicionDetalle, RendicionResumen, Usuario,
} from './modelos';

/** Todas las llamadas a la API de RendiGastos en un solo lugar. */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  // ---- Rendiciones ----
  listarPropias(codEstado?: number | null): Observable<RendicionResumen[]> {
    return this.http.get<RendicionResumen[]>(`${this.base}/Rendiciones/GetList`, { params: this.estado(codEstado) });
  }

  listarPorAprobar(codEstado?: number | null): Observable<RendicionResumen[]> {
    return this.http.get<RendicionResumen[]>(`${this.base}/Rendiciones/GetListPorAprobar`, {
      params: this.estado(codEstado),
    });
  }

  obtener(codRendicion: number): Observable<RendicionDetalle> {
    return this.http.get<RendicionDetalle>(`${this.base}/Rendiciones/Get/${codRendicion}`);
  }

  crearRendicion(titulo: string): Observable<{ codRendicion: number }> {
    return this.http.post<{ codRendicion: number }>(`${this.base}/Rendiciones/PostAgregar`, { titulo });
  }

  editarRendicion(codRendicion: number, titulo: string): Observable<void> {
    return this.http.put<void>(`${this.base}/Rendiciones/PutActualizar/${codRendicion}`, { titulo });
  }

  eliminarRendicion(codRendicion: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/Rendiciones/DeleteEliminar/${codRendicion}`);
  }

  // ---- Gastos y comprobantes ----
  crearGasto(codRendicion: number, datos: DatosGasto): Observable<{ codGasto: number }> {
    return this.http.post<{ codGasto: number }>(`${this.base}/Rendiciones/PostAgregarGasto/${codRendicion}`, datos);
  }

  editarGasto(codGasto: number, datos: DatosGasto): Observable<void> {
    return this.http.put<void>(`${this.base}/Rendiciones/PutActualizarGasto/${codGasto}`, datos);
  }

  eliminarGasto(codGasto: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/Rendiciones/DeleteEliminarGasto/${codGasto}`);
  }

  subirComprobante(codGasto: number, archivo: File): Observable<void> {
    const formulario = new FormData();
    formulario.append('archivo', archivo, archivo.name);
    return this.http.post<void>(`${this.base}/Rendiciones/PostSubirComprobante/${codGasto}`, formulario);
  }

  quitarComprobante(codGasto: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/Rendiciones/DeleteQuitarComprobante/${codGasto}`);
  }

  descargarComprobante(codGasto: number): Observable<Blob> {
    return this.http.get(`${this.base}/Rendiciones/GetComprobante/${codGasto}`, { responseType: 'blob' });
  }

  // ---- Flujo ----
  enviar(codRendicion: number): Observable<void> {
    return this.http.post<void>(`${this.base}/Rendiciones/PostEnviar/${codRendicion}`, {});
  }

  aprobar(codRendicion: number, comentario: string): Observable<void> {
    return this.http.post<void>(`${this.base}/Rendiciones/PostAprobar/${codRendicion}`, { comentario });
  }

  rechazar(codRendicion: number, motivo: string): Observable<void> {
    return this.http.post<void>(`${this.base}/Rendiciones/PostRechazar/${codRendicion}`, { motivo });
  }

  reabrir(codRendicion: number): Observable<void> {
    return this.http.post<void>(`${this.base}/Rendiciones/PostReabrir/${codRendicion}`, {});
  }

  // ---- Categorías ----
  listarCategorias(soloActivas: boolean): Observable<Categoria[]> {
    return this.http.get<Categoria[]>(`${this.base}/Categorias/GetList`, {
      params: new HttpParams().set('soloActivas', soloActivas),
    });
  }

  crearCategoria(datos: Omit<Categoria, 'codCategoria'>): Observable<Categoria[]> {
    return this.http.post<Categoria[]>(`${this.base}/Categorias/PostAgregar`, datos);
  }

  editarCategoria(codCategoria: number, datos: Omit<Categoria, 'codCategoria'>): Observable<Categoria[]> {
    return this.http.put<Categoria[]>(`${this.base}/Categorias/PutActualizar/${codCategoria}`, datos);
  }

  // ---- Usuarios ----
  listarUsuarios(): Observable<Usuario[]> {
    return this.http.get<Usuario[]>(`${this.base}/Usuarios/GetList`);
  }

  listarAprobadores(): Observable<Usuario[]> {
    return this.http.get<Usuario[]>(`${this.base}/Usuarios/GetComboAprobadores`);
  }

  crearUsuario(datos: { nombre: string; email: string; clave: string; codRol: number; codAprobador: number | null }): Observable<Usuario> {
    return this.http.post<Usuario>(`${this.base}/Usuarios/PostAgregar`, datos);
  }

  editarUsuario(codUsuario: number, datos: { nombre: string; codRol: number; codAprobador: number | null; activo: boolean }): Observable<Usuario> {
    return this.http.put<Usuario>(`${this.base}/Usuarios/PutActualizar/${codUsuario}`, datos);
  }

  // ---- Avisos ----
  listarAvisos(): Observable<Aviso[]> {
    return this.http.get<Aviso[]>(`${this.base}/Avisos/GetList`);
  }

  contarAvisos(): Observable<{ noLeidos: number }> {
    return this.http.get<{ noLeidos: number }>(`${this.base}/Avisos/GetContador`);
  }

  marcarLeido(codAviso: number): Observable<void> {
    return this.http.put<void>(`${this.base}/Avisos/PutMarcarLeido/${codAviso}`, {});
  }

  marcarTodosLeidos(): Observable<void> {
    return this.http.put<void>(`${this.base}/Avisos/PutMarcarTodosLeidos`, {});
  }

  clavePush(): Observable<ClavePush> {
    return this.http.get<ClavePush>(`${this.base}/Avisos/GetClavePush`);
  }

  suscribirPush(url: string, claveP256: string, claveAuth: string): Observable<void> {
    return this.http.post<void>(`${this.base}/Avisos/PostSuscribirPush`, { url, claveP256, claveAuth });
  }

  desuscribirPush(url: string): Observable<void> {
    return this.http.post<void>(`${this.base}/Avisos/PostDesuscribirPush`, { url });
  }

  private estado(codEstado?: number | null): HttpParams {
    return codEstado ? new HttpParams().set('codEstado', codEstado) : new HttpParams();
  }
}
