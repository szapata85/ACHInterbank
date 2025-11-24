import { CommonModule } from '@angular/common';
import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { EMPTY, catchError, finalize, tap } from 'rxjs';
import { AuthService } from '../core/services/auth.service';
import { LoginRequestModel } from '../core/models/auth.models';
import { ErrorMessageComponent } from '../shared/error-message.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, ErrorMessageComponent],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
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
