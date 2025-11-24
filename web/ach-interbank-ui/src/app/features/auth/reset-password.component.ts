import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { EMPTY, catchError, finalize, tap } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';

function passwordsMatch(control: AbstractControl): ValidationErrors | null {
  const newPassword = control.get('newPassword')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;
  return newPassword && confirmPassword && newPassword !== confirmPassword
    ? { passwordMismatch: true }
    : null;
}

@Component({
  selector: 'app-reset-password',
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ResetPasswordComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  token = '';
  isSubmitting = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  readonly form = this.fb.group(
    {
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required, Validators.minLength(6)]]
    },
    { validators: passwordsMatch }
  );

  ngOnInit(): void {
    this.token = this.route.snapshot.paramMap.get('token') ?? '';
  }

  submit(): void {
    if (this.form.invalid || !this.token) {
      this.form.markAllAsTouched();
      if (!this.token) {
        this.errorMessage = 'El enlace de recuperación no es válido.';
      }
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = null;
    this.successMessage = null;

    const { newPassword, confirmPassword } = this.form.getRawValue();

    this.authService
      .resetPassword(this.token, newPassword!, confirmPassword!)
      .pipe(
        tap(() => {
          this.successMessage = 'Tu contraseña se actualizó correctamente. Ahora puedes iniciar sesión.';
          setTimeout(() => this.router.navigate(['/auth/login']), 1200);
        }),
        catchError(() => {
          this.errorMessage = 'El enlace es inválido o expiró. Solicita uno nuevo y vuelve a intentarlo.';
          return EMPTY;
        }),
        finalize(() => (this.isSubmitting = false))
      )
      .subscribe();
  }

  goToForgot(): void {
    this.router.navigate(['/auth/forgot-password']);
  }
}
