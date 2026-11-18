import { FormControl } from '@angular/forms';
import { correoValido, formatearTamano, obligatorioSinEspacios, validarArchivo, validarContenido } from './validadores';

const archivo = (nombre: string, bytes: number) => new File([new Uint8Array(bytes)], nombre);

describe('validarArchivo', () => {
  it('acepta PDF, PNG y JPG dentro del tamaño permitido', () => {
    expect(validarArchivo(archivo('factura.pdf', 100))).toBeNull();
    expect(validarArchivo(archivo('foto.PNG', 100))).toBeNull();
    expect(validarArchivo(archivo('scan.jpeg', 100))).toBeNull();
  });

  it('rechaza una extensión no permitida con un mensaje claro', () => {
    expect(validarArchivo(archivo('programa.exe', 100))).toContain('PNG o JPG');
    expect(validarArchivo(archivo('sin_extension', 100))).toContain('PNG o JPG');
  });

  it('rechaza un archivo vacío', () => {
    expect(validarArchivo(archivo('vacio.pdf', 0))).toContain('vacío');
  });

  it('rechaza un archivo que supera el máximo y dice cuánto pesa', () => {
    const mensaje = validarArchivo(archivo('grande.pdf', 6 * 1024 * 1024));

    expect(mensaje).toContain('6.0 MB');
    expect(mensaje).toContain('5 MB');
  });
});

describe('validadores de formulario', () => {
  it('obligatorioSinEspacios rechaza textos vacíos o con solo espacios', () => {
    expect(obligatorioSinEspacios(new FormControl(''))).toEqual({ obligatorio: true });
    expect(obligatorioSinEspacios(new FormControl('   '))).toEqual({ obligatorio: true });
    expect(obligatorioSinEspacios(new FormControl('Ana'))).toBeNull();
  });

  it('correoValido acepta correos bien formados y rechaza los demás', () => {
    expect(correoValido(new FormControl('ana@correo.com'))).toBeNull();
    expect(correoValido(new FormControl('ana@correo'))).toEqual({ correo: true });
    expect(correoValido(new FormControl('no es correo'))).toEqual({ correo: true });
  });

  it('formatearTamano usa la unidad adecuada', () => {
    expect(formatearTamano(512)).toBe('512 B');
    expect(formatearTamano(2048)).toBe('2.0 KB');
    expect(formatearTamano(3 * 1024 * 1024)).toBe('3.0 MB');
  });
});

describe('validarContenido', () => {
  const con = (texto: number[], nombre: string) => new File([new Uint8Array(texto)], nombre);

  it('acepta archivos cuya firma coincide con su extensión', async () => {
    expect(await validarContenido(con([0x25, 0x50, 0x44, 0x46, 0x2d], 'a.pdf'))).toBeNull();
    expect(await validarContenido(con([0x89, 0x50, 0x4e, 0x47, 0, 0, 0, 0], 'a.png'))).toBeNull();
    expect(await validarContenido(con([0xff, 0xd8, 0xff, 0xe0], 'a.jpg'))).toBeNull();
    expect(await validarContenido(con([0xff, 0xd8, 0xff, 0xe0], 'a.JPEG'))).toBeNull();
  });

  it('rechaza un archivo que solo cambió de extensión', async () => {
    const mensaje = await validarContenido(con([0x50, 0x4b, 0x03, 0x04], 'ventas.pdf'));

    expect(mensaje).toContain('no corresponde a un archivo .pdf real');
  });

  it('rechaza un PNG que en realidad es JPG', async () => {
    expect(await validarContenido(con([0xff, 0xd8, 0xff], 'foto.png'))).not.toBeNull();
  });
});

describe('mensajes de formato no permitido', () => {
  it('nombra el tipo de archivo y dice cómo resolverlo', () => {
    for (const [nombre, tipo] of [['a.xls', 'Excel'], ['a.docx', 'Word'], ['a.pptx', 'PowerPoint'], ['a.zip', 'comprimidos']]) {
      const mensaje = validarArchivo(archivo(nombre, 10));
      expect(mensaje).toContain(tipo);
      expect(mensaje).toContain('guárdalo como PDF');
    }
  });
});
