/** Funciones de formato y de fechas compartidas. Las fechas del servidor viajan como dd/mm/aaaa. */

export function dinero(valor: number): string {
  return `$ ${valor.toLocaleString('es', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

/** De dd/mm/aaaa a aaaa-mm-dd (lo que usa el campo de fecha del navegador). */
export function aIso(fecha: string): string {
  const [d, m, a] = fecha.split('/');
  return d && m && a ? `${a}-${m}-${d}` : '';
}

/** De aaaa-mm-dd a dd/mm/aaaa (lo que espera la API). */
export function aFechaApi(iso: string): string {
  const [a, m, d] = iso.split('-');
  return d && m && a ? `${d}/${m}/${a}` : '';
}

export function hoyIso(): string {
  return fechaIso(new Date());
}

export function haceDiasIso(dias: number): string {
  const f = new Date();
  f.setDate(f.getDate() - dias);
  return fechaIso(f);
}

function fechaIso(f: Date): string {
  const mes = String(f.getMonth() + 1).padStart(2, '0');
  const dia = String(f.getDate()).padStart(2, '0');
  return `${f.getFullYear()}-${mes}-${dia}`;
}

/** Clase de color para el estado de una rendición. */
export function claseEstado(codEstado: number): string {
  switch (codEstado) {
    case 2: return 'estado estado--enviada';
    case 3: return 'estado estado--aprobada';
    case 4: return 'estado estado--rechazada';
    default: return 'estado estado--borrador';
  }
}

export const DIAS_MAXIMOS_ANTIGUEDAD = 90;
