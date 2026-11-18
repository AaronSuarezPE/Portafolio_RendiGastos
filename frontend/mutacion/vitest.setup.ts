import '@angular/compiler';
import { expect } from 'vitest';

// Las pruebas de Karma/Jasmine usan toBeTrue/toBeFalse; Vitest no los trae.
expect.extend({
  toBeTrue(recibido: unknown) {
    return { pass: recibido === true, message: () => `se esperaba true y llegó ${String(recibido)}` };
  },
  toBeFalse(recibido: unknown) {
    return { pass: recibido === false, message: () => `se esperaba false y llegó ${String(recibido)}` };
  },
});

declare module 'vitest' {
  interface Assertion {
    toBeTrue(): void;
    toBeFalse(): void;
  }
}
