import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { EMPTY, catchError, finalize, tap } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { SharedModule } from '../../shared/shared.module';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [SharedModule, RouterModule]
})
export class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]]
  });

  isSubmitting = false;
  successMessage: string | null = null;
  errorMessage: string | null = null;

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = null;
    this.successMessage = null;

    const email = this.form.getRawValue().email as string;

    this.authService
      .forgotPassword(email)
      .pipe(
        tap(() =>
          (this.successMessage =
            'Si el correo existe, hemos enviado un enlace de recuperación a tu bandeja de entrada.')
        ),
        catchError(() => {
          this.errorMessage = 'No pudimos procesar tu solicitud en este momento. Inténtalo más tarde.';
          return EMPTY;
        }),
        finalize(() => (this.isSubmitting = false))
      )
      .subscribe();
  }

  backToLogin(): void {
    this.router.navigate(['/auth/login']);
  }
}
