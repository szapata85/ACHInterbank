import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { SharedModule } from '../../../shared/shared.module';
import { NotificationService } from '../../../core/services/notification.service';
import { PasswordRulesService } from '../../../core/services/password-rules.service';
import { RouterModule } from '@angular/router';
import { take } from 'rxjs';

@Component({
  selector: 'app-password-rules-settings',
  standalone: true,
  imports: [SharedModule, RouterModule],
  templateUrl: './password-rules-settings.component.html',
  styleUrls: ['./password-rules-settings.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PasswordRulesSettingsComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(PasswordRulesService);
  private readonly notifications = inject(NotificationService);

  readonly form = this.fb.group({
    minLength: [6, [Validators.required, Validators.min(1)]],
    minUppercase: [1, [Validators.required, Validators.min(0)]],
    minNumbers: [1, [Validators.required, Validators.min(0)]],
    minSpecial: [1, [Validators.required, Validators.min(0)]],
    maxSpecial: [4, [Validators.required, Validators.min(0)]]
  });

  constructor() {
    this.service.rules$.pipe(take(1)).subscribe((rules) => {
      this.form.patchValue(rules, { emitEvent: false });
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notifications.error('Revisa los valores de las reglas.');
      return;
    }

    const value = this.form.getRawValue();
    this.service
      .updateRules({
        minLength: value.minLength ?? 6,
        minUppercase: value.minUppercase ?? 0,
        minNumbers: value.minNumbers ?? 0,
        minSpecial: value.minSpecial ?? 0,
        maxSpecial: value.maxSpecial ?? 0
      })
      .subscribe(() => {
        this.form.markAsPristine();
        this.notifications.success('Reglas de contraseña guardadas.');
      });
  }
}
