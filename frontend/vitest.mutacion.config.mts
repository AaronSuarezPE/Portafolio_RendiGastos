import { defineConfig } from 'vitest/config';

// Solo para el mutation testing: corre en Node las pruebas de lógica pura de core/.
export default defineConfig({
  test: {
    globals: true,
    environment: 'node',
    setupFiles: ['mutacion/vitest.setup.ts'],
    include: ['src/app/core/{errores,formato,validadores}.spec.ts'],
  },
});
