import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { SharedModule } from '../../../shared/shared.module';
import { NotificationService } from '../../../core/services/notification.service';
import { LoginLockoutSettingsService } from '../../../core/services/login-lockout-settings.service';
import { RouterModule } from '@angular/router';
import { take } from 'rxjs';

@Component({
  selector: 'app-login-lockout-settings',
  standalone: true,
  imports: [SharedModule, RouterModule],
  templateUrl: './login-lockout-settings.component.html',
  styleUrls: ['./login-lockout-settings.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginLockoutSettingsComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(LoginLockoutSettingsService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly form = this.fb.group({
    maxFailedAttempts: [5, [Validators.required, Validators.min(1), Validators.max(20)]],
    lockoutMinutes: [5, [Validators.required, Validators.min(1), Validators.max(60)]]
  });

  constructor() {
    this.service.settings$.pipe(take(1)).subscribe((settings) => {
      this.form.patchValue(settings, { emitEvent: false });
      this.cdr.markForCheck();
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notifications.error('Revisa los valores de bloqueo.');
      return;
    }

    const value = this.form.getRawValue();
    this.service
      .updateSettings({
        maxFailedAttempts: value.maxFailedAttempts ?? 5,
        lockoutMinutes: value.lockoutMinutes ?? 5
      })
      .subscribe(() => {
        this.form.markAsPristine();
        this.notifications.success('Configuración de bloqueo guardada.');
        this.cdr.markForCheck();
      });
  }
}
