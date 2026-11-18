# RendiGastos, frontend

Aplicación Angular 19 con Angular Material, instalable como PWA. La guía completa está en el [README principal](../README.md).

```powershell
npm install
npm start          # http://localhost:4200 (necesita la API en http://localhost:5240)
npm test           # pruebas unitarias (Karma y Jasmine)
npm run lint       # ESLint
npx playwright test   # pruebas de punta a punta
ng build           # compilación de producción en dist/
```

La dirección de la API se define en `src/environments`.
