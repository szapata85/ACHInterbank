import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import { NotificationService } from '../../../core/services/notification.service';
import { NachaSecurityOperationsApiService } from '../services/nacha-security-operations-api.service';
import { NachaSecurityOperationResponse } from '../models/nacha-security-operation.model';

@Component({
  selector: 'app-nacha-generate-encrypted-operation',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, SharedModule],
  templateUrl: './nacha-generate-encrypted-operation.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaGenerateEncryptedOperationComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(NachaSecurityOperationsApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  loading = false;
  operation?: NachaSecurityOperationResponse;

  readonly form = this.fb.group({
    cycleId: ['', Validators.required]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.operation = undefined;

    this.service.generateEncrypted({ cycleId: this.form.value.cycleId! })
      .pipe(finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (response) => {
          this.operation = response;
          this.cdr.markForCheck();
        },
        error: () => this.notifications.error('No fue posible generar NACHA-M cifrado.')
      });
  }
}
