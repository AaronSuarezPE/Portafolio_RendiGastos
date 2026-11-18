import { defineConfig } from '@playwright/test';

// Pruebas de punta a punta: navegador, API y base de datos reales (LocalDB, cuentas de demostración).
// Requiere la base instalada: backend/database/instalar.ps1 -CrearBd
export default defineConfig({
  testDir: 'e2e',
  timeout: 60_000,
  expect: { timeout: 15_000 },
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'reports/e2e' }]],
  use: { baseURL: 'http://localhost:4200', channel: 'chrome', headless: true, locale: 'es' },
  webServer: [
    {
      command: 'dotnet run --project ../backend/src/RendiGastos.Api --no-launch-profile --urls http://localhost:5240',
      url: 'http://localhost:5240/api/v1/rendigastos/Salud/Get',
      env: { ASPNETCORE_ENVIRONMENT: 'Development', Demo__Habilitado: "true" },
      reuseExistingServer: true,
      timeout: 120_000,
    },
    {
      command: 'npx ng serve --port 4200',
      url: 'http://localhost:4200',
      reuseExistingServer: true,
      timeout: 180_000,
    },
  ],
});
