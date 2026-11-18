import { Component, ElementRef, Injector, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/auth.service';
import { mensajeDeError } from '../../core/errores';
import { enfocarPrimerError } from '../../core/foco';
import { RespuestaSesion } from '../../core/modelos';
import { claveSegura, correoValido, obligatorioSinEspacios } from '../../core/validadores';

@Component({
  selector: 'app-acceso',
  imports: [
    ReactiveFormsModule,
    MatTabsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './acceso.component.html',
  styleUrl: './acceso.component.scss',
})
export class AccesoComponent {
  private readonly fb = inject(FormBuilder).nonNullable;
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly elemento = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);

  protected readonly trabajando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly verClave = signal(false);

  protected readonly formLogin = this.fb.group({
    email: ['', [obligatorioSinEspacios, correoValido, Validators.maxLength(150)]],
    clave: ['', [Validators.required, Validators.maxLength(100)]],
  });

  protected readonly formRegistro = this.fb.group({
    nombre: ['', [obligatorioSinEspacios, Validators.minLength(2), Validators.maxLength(100)]],
    email: ['', [obligatorioSinEspacios, correoValido, Validators.maxLength(150)]],
    clave: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100), claveSegura]],
    repetirClave: ['', [Validators.required]],
  });

  protected ingresar(): void {
    const f = this.formLogin;
    if (f.invalid) return this.marcarErrores(f);

    const { email, clave } = f.getRawValue();
    this.ejecutar(this.auth.login(email, clave));
  }

  protected registrar(): void {
    const f = this.formRegistro;
    const { nombre, email, clave, repetirClave } = f.getRawValue();

    if (repetirClave.length > 0 && clave !== repetirClave) {
      f.controls.repetirClave.setErrors({ distinta: true });
    }
    if (f.invalid) return this.marcarErrores(f);

    this.ejecutar(this.auth.registrar(nombre, email, clave));
  }

  protected readonly cuentasDemo = environment.cuentasDemo;
  protected readonly demos = [
    { rol: 'Empleado', email: 'empleado@demo.rendigastos.app', ayuda: 'Crea y envía rendiciones' },
    { rol: 'Aprobador', email: 'aprobador@demo.rendigastos.app', ayuda: 'Aprueba o rechaza las de su equipo' },
    { rol: 'Administrador', email: 'admin@demo.rendigastos.app', ayuda: 'Gestiona usuarios y categorías' },
  ];

  protected entrarDemo(email: string): void {
    this.ejecutar(this.auth.login(email, 'Demo2026!'));
  }

  protected revalidarRepeticion(): void {
    const { clave, repetirClave } = this.formRegistro.getRawValue();
    const control = this.formRegistro.controls.repetirClave;
    if (repetirClave.length === 0) return;

    if (clave !== repetirClave) control.setErrors({ distinta: true });
    else control.updateValueAndValidity({ emitEvent: false });
  }

  private ejecutar(peticion: Observable<RespuestaSesion>): void {
    if (this.trabajando()) return;
    this.trabajando.set(true);
    this.error.set(null);

    peticion.subscribe({
      next: () => void this.router.navigate(['/rendiciones']),
      error: (e: unknown) => {
        this.error.set(mensajeDeError(e));
        this.trabajando.set(false);
      },
    });
  }

  private marcarErrores(formulario: { markAllAsTouched(): void }): void {
    formulario.markAllAsTouched();
    this.error.set(null);
    enfocarPrimerError(this.injector, this.elemento.nativeElement);
  }
}
