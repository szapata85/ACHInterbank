import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { EMPTY, catchError, finalize, tap } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { LoginRequestModel } from '../../core/models/auth.models';
import { SharedModule } from '../../shared/shared.module';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [SharedModule, RouterModule]
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly form = this.fb.group({
    username: ['', [Validators.required, Validators.minLength(3)]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  errorMessage: string | null = null;
  isSubmitting = false;

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = null;

    const credentials = this.form.getRawValue() as LoginRequestModel;

    this.authService
      .login(credentials)
      .pipe(
        tap(() => this.router.navigate(['/'])),
        catchError((error: Error) => {
          this.errorMessage = error.message ?? 'No fue posible iniciar sesión';
          return EMPTY;
        }),
        finalize(() => (this.isSubmitting = false))
      )
      .subscribe();
  }
}
