import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import { NotificationService } from '../../../core/services/notification.service';
import { NachaSecurityOperationsApiService } from '../services/nacha-security-operations-api.service';
import { NachaSecurityOperationResponse } from '../models/nacha-security-operation.model';

@Component({
  selector: 'app-manual-encrypt-operation',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, SharedModule],
  templateUrl: './manual-encrypt-operation.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ManualEncryptOperationComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(NachaSecurityOperationsApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  loading = false;
  operation?: NachaSecurityOperationResponse;

  readonly form = this.fb.group({
    file: [null as File | null, Validators.required]
  });

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.form.patchValue({ file: input.files?.[0] ?? null });
  }

  submit(): void {
    const file = this.form.value.file;
    if (!file) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.service.manualEncrypt(file)
      .pipe(finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (response) => this.operation = response,
        error: () => this.notifications.error('No fue posible cifrar el archivo.')
      });
  }
}
