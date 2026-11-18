import { AbstractControl, ValidationErrors } from '@angular/forms';

export const EXTENSIONES_PERMITIDAS = ['pdf', 'png', 'jpg', 'jpeg'];
export const TAMANO_MAXIMO_MB = 5;

const PATRON_CORREO = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;

/** Rechaza textos vacíos o formados solo por espacios. */
export function obligatorioSinEspacios(control: AbstractControl): ValidationErrors | null {
  const valor = control.value;
  return typeof valor === 'string' && valor.trim().length === 0 ? { obligatorio: true } : null;
}

export function correoValido(control: AbstractControl): ValidationErrors | null {
  const valor = (control.value ?? '').toString().trim();
  return valor.length === 0 || PATRON_CORREO.test(valor) ? null : { correo: true };
}

/** La clave pide letras y números, igual que el servidor. */
export function claveSegura(control: AbstractControl): ValidationErrors | null {
  const valor = (control.value ?? '').toString();
  if (valor.length === 0) return null;
  return /[A-Za-z]/.test(valor) && /\d/.test(valor) ? null : { claveDebil: true };
}

/** Texto con un mínimo de caracteres útiles (sin contar espacios sobrantes). */
export function minimoUtil(minimo: number) {
  return (control: AbstractControl): ValidationErrors | null => {
    const valor = (control.value ?? '').toString().trim();
    return valor.length === 0 || valor.length >= minimo ? null : { minimoUtil: { minimo } };
  };
}

/** Número con como máximo 2 decimales. */
export function dosDecimales(control: AbstractControl): ValidationErrors | null {
  const valor = control.value;
  if (valor === null || valor === '' || valor === undefined) return null;
  return Math.round(Number(valor) * 100) === Number(valor) * 100 ? null : { decimales: true };
}

export function formatearTamano(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

/**
 * Valida un archivo al elegirlo. Devuelve el mensaje de error o null si es válido.
 * El servidor repite estas comprobaciones: aquí solo se evita una subida inútil.
 */
export function validarArchivo(archivo: File): string | null {
  if (archivo.size === 0) {
    return 'El archivo está vacío. Elige otro comprobante.';
  }

  const extension = archivo.name.includes('.') ? (archivo.name.split('.').pop()?.toLowerCase() ?? '') : '';
  if (!EXTENSIONES_PERMITIDAS.includes(extension)) {
    return mensajeFormatoNoPermitido(archivo.name, extension);
  }

  if (archivo.size > TAMANO_MAXIMO_MB * 1024 * 1024) {
    return `El archivo pesa ${formatearTamano(archivo.size)} y el máximo es ${TAMANO_MAXIMO_MB} MB.`;
  }

  return null;
}

/** Texto fijo que explica qué se puede subir; se reutiliza en la ayuda de la pantalla. */
export const TEXTO_FORMATOS =
  'Solo se aceptan comprobantes en PDF e imágenes PNG o JPG. Si tu comprobante está en otro formato, guárdalo como PDF o tómale una foto.';

const NO_ACEPTADOS: Record<string, string> = {
  xls: 'Excel', xlsx: 'Excel', csv: 'Excel',
  doc: 'Word', docx: 'Word',
  ppt: 'PowerPoint', pptx: 'PowerPoint',
  txt: 'texto', zip: 'comprimidos', rar: 'comprimidos',
};

function mensajeFormatoNoPermitido(nombre: string, extension: string): string {
  const tipo = NO_ACEPTADOS[extension];
  const que = tipo
    ? `Los archivos de ${tipo} (.${extension}) no se pueden usar como comprobante.`
    : `«${nombre}» no tiene un formato permitido.`;
  return `${que} ${TEXTO_FORMATOS}`;
}

const FIRMAS: { extensiones: string[]; bytes: number[] }[] = [
  { extensiones: ['pdf'], bytes: [0x25, 0x50, 0x44, 0x46] },
  { extensiones: ['png'], bytes: [0x89, 0x50, 0x4e, 0x47] },
  { extensiones: ['jpg', 'jpeg'], bytes: [0xff, 0xd8, 0xff] },
];

/**
 * Revisa los primeros bytes del archivo para detectar uno que solo cambió de extensión
 * (por ejemplo un Excel renombrado a .pdf). Devuelve el mensaje de error o null.
 */
export async function validarContenido(archivo: File): Promise<string | null> {
  const extension = archivo.name.split('.').pop()?.toLowerCase() ?? '';
  const firma = FIRMAS.find((f) => f.extensiones.includes(extension));
  if (!firma) return null;

  const inicio = new Uint8Array(await archivo.slice(0, firma.bytes.length).arrayBuffer());
  const coincide = firma.bytes.every((b, i) => inicio[i] === b);

  return coincide
    ? null
    : `El contenido de «${archivo.name}» no corresponde a un archivo .${extension} real. ${TEXTO_FORMATOS}`;
}
