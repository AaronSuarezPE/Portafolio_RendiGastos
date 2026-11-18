import { expect, Page, test } from '@playwright/test';

const PDF = Buffer.from('%PDF-1.4\n1 0 obj\n<< /Type /Catalog >>\nendobj\ntrailer\n<< /Root 1 0 R >>\n%%EOF\n');

async function entrarDemo(page: Page, rol: 'Empleado' | 'Aprobador' | 'Administrador') {
  await page.goto('/acceso');
  await page.getByRole('button', { name: new RegExp(`^${rol}`) }).click();
  await expect(page.getByRole('link', { name: 'Mis rendiciones' })).toBeVisible();
}

// El campo de fecha es nativo (yyyy-mm-dd) y no admite fechas futuras: se usa la de hoy en hora local.
function hoyIso(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

async function cerrarAviso(page: Page, titulo: string) {
  const dialogo = page.locator('mat-dialog-container');
  await expect(dialogo).toContainText(titulo);
  await dialogo.getByRole('button').last().click();
  await expect(dialogo).toHaveCount(0);
}

test.describe('Acceso y permisos', () => {
  test('con el formulario vacío se muestran los errores y no se llama a la API', async ({ page }) => {
    const llamadas: string[] = [];
    page.on('request', r => { if (r.url().includes('Autenticacion')) llamadas.push(r.url()); });

    await page.goto('/acceso');
    await page.getByRole('button', { name: 'Ingresar', exact: true }).click();

    await expect(page.locator('mat-error').first()).toBeVisible();
    expect(llamadas).toEqual([]);
  });

  test('una clave equivocada muestra un mensaje que no revela qué falló', async ({ page }) => {
    await page.goto('/acceso');
    await page.getByLabel('Correo').first().fill('empleado@demo.rendigastos.app');
    await page.getByLabel('Clave').first().fill('ClaveIncorrecta1');
    await page.getByRole('button', { name: 'Ingresar', exact: true }).click();

    await expect(page.getByRole('alert')).toContainText('Correo o clave incorrectos');
  });

  test('el empleado no ve las pantallas de aprobación ni de administración', async ({ page }) => {
    await entrarDemo(page, 'Empleado');
    await expect(page.getByRole('link', { name: 'Por aprobar' })).toHaveCount(0);
    await expect(page.getByRole('link', { name: 'Usuarios' })).toHaveCount(0);

    await page.goto('/usuarios');
    await expect(page.getByRole('heading', { name: /Usuarios/ })).toHaveCount(0);
  });

  test('sin sesión las pantallas internas llevan al acceso', async ({ page }) => {
    await page.goto('/rendiciones');
    await expect(page).toHaveURL(/acceso/);
  });
});

test.describe('Rendición de gastos', () => {
  test('el empleado arma una rendición, el aprobador la rechaza, se corrige y se aprueba', async ({ browser }) => {
    const sufijo = Date.now().toString(36);
    const titulo = `E2E ${sufijo}`;
    const descripcion = `Almuerzo con el cliente ${sufijo}`; // la aplicación rechaza un gasto idéntico a uno ya registrado

    // 1. El empleado crea la rendición (con validación antes de llamar a la API)
    const empleado = await (await browser.newContext()).newPage();
    await entrarDemo(empleado, 'Empleado');
    await empleado.getByRole('button', { name: 'Nueva rendición' }).click();
    await empleado.getByRole('button', { name: 'Crear rendición' }).click();
    await expect(empleado.locator('mat-error').first()).toBeVisible();
    await empleado.locator('app-panel-texto input').fill(titulo);
    await empleado.getByRole('button', { name: 'Crear rendición' }).click();
    await expect(empleado.getByRole('heading', { name: titulo })).toBeVisible();

    // 2. Un gasto inválido no se envía; uno válido se guarda
    await empleado.getByRole('button', { name: 'Agregar gasto' }).click();
    await empleado.getByRole('button', { name: 'Guardar gasto' }).click();
    await expect(empleado.locator('mat-error').first()).toBeVisible();

    await empleado.getByLabel('Categoría').click();
    await empleado.getByRole('option', { name: 'Alimentación' }).click();
    await empleado.locator('input[type=date]').fill(hoyIso());
    await empleado.getByPlaceholder('Ej.: Taxi al aeropuerto').fill(descripcion);
    await empleado.getByPlaceholder('Ej.: 45.50').fill('85.50');

    // 3. Solo se aceptan PDF e imágenes reales como comprobante
    await empleado.locator('input[type=file]').setInputFiles({ name: 'boleta.xlsx', mimeType: 'application/vnd.ms-excel', buffer: Buffer.from('PK\u0003\u0004 x') });
    await expect(empleado.getByRole('alert').first()).toBeVisible();
    await empleado.locator('input[type=file]').setInputFiles({ name: 'boleta.pdf', mimeType: 'application/pdf', buffer: PDF });
    await expect(empleado.getByText('boleta.pdf')).toBeVisible();
    await empleado.getByRole('button', { name: 'Guardar gasto' }).click();
    await cerrarAviso(empleado, 'Gasto agregado');
    await expect(empleado.getByText(descripcion)).toBeVisible();

    // 4. Se envía a aprobación
    await empleado.getByRole('button', { name: 'Enviar para aprobación' }).click();
    await expect(empleado.locator('mat-dialog-container')).toContainText('¿Enviar la rendición?');
    await empleado.locator('mat-dialog-container').getByRole('button', { name: 'Enviar' }).click();
    await cerrarAviso(empleado, 'Rendición enviada');
    await expect(empleado.getByRole('button', { name: 'Agregar gasto' })).toHaveCount(0);

    // 5. El aprobador la rechaza: el motivo corto se valida antes de llamar a la API
    const aprobador = await (await browser.newContext()).newPage();
    await entrarDemo(aprobador, 'Aprobador');
    await aprobador.getByRole('link', { name: 'Por aprobar' }).click();
    await aprobador.getByText(titulo).click();
    await aprobador.getByRole('button', { name: 'Rechazar' }).first().click();
    await aprobador.locator('app-panel-texto textarea').fill('corto');
    await aprobador.locator('app-panel-texto').getByRole('button', { name: 'Rechazar' }).click();
    await expect(aprobador.locator('app-panel-texto mat-error')).toBeVisible();
    await aprobador.locator('app-panel-texto textarea').fill('Falta el detalle de la visita al cliente');
    await aprobador.locator('app-panel-texto').getByRole('button', { name: 'Rechazar' }).click();
    await cerrarAviso(aprobador, 'Rendición rechazada');
    await expect(aprobador.getByText('Falta el detalle de la visita al cliente').first()).toBeVisible();

    // 6. El empleado la reabre, corrige y vuelve a enviar
    await empleado.reload();
    await expect(empleado.getByText('Falta el detalle de la visita al cliente').first()).toBeVisible();
    await empleado.getByRole('button', { name: 'Corregir y volver a enviar' }).click();
    await empleado.locator('mat-dialog-container').getByRole('button', { name: 'Reabrir' }).click();
    await cerrarAviso(empleado, 'Rendición reabierta');
    await empleado.getByRole('button', { name: 'Enviar para aprobación' }).click();
    await empleado.locator('mat-dialog-container').getByRole('button', { name: 'Enviar' }).click();
    await cerrarAviso(empleado, 'Rendición enviada');

    // 7. El aprobador la aprueba con un comentario
    await aprobador.reload();
    await aprobador.getByRole('button', { name: 'Aprobar' }).first().click();
    await aprobador.locator('app-panel-texto textarea').fill('Todo en orden');
    await aprobador.locator('app-panel-texto').getByRole('button', { name: 'Aprobar' }).click();
    await cerrarAviso(aprobador, 'Rendición aprobada');
    await expect(aprobador.getByText('Aprobada').first()).toBeVisible();

    // 8. El empleado recibe los avisos de las dos decisiones
    await empleado.getByRole('link', { name: /Avisos/ }).click();
    await expect(empleado.getByText('Rendición rechazada').first()).toBeVisible();
    await expect(empleado.getByText('Rendición aprobada').first()).toBeVisible();
  });
});
