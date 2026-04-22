import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import { NotificationService } from '../../../core/services/notification.service';
import { NachaSecurityOperationsApiService } from '../services/nacha-security-operations-api.service';
import { NachaSecurityOperationResponse } from '../models/nacha-security-operation.model';
import { sanitizeDownloadFileName } from '../utils/download-file-name.util';
import { AuthService } from '../../../core/services/auth.service';
import { NACHA_SECURITY_PERMISSIONS } from '../nacha-security-permissions';

@Component({
  selector: 'app-manual-decrypt-operation',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, SharedModule],
  templateUrl: './manual-decrypt-operation.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ManualDecryptOperationComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(NachaSecurityOperationsApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly auth = inject(AuthService);

  loading = false;
  authorizing = false;
  operation?: NachaSecurityOperationResponse;

  readonly form = this.fb.group({
    file: [null as File | null, Validators.required]
  });

  get canDownload(): boolean {
    if (!this.operation || !this.operation.artifact.downloadAvailable || this.authorizing) {
      return false;
    }

    if (this.operation.error?.code === 'SIGNATURE_VALIDATION_FAILED') {
      return false;
    }

    const requiredPermission = this.requiresPlainDownloadPermission(this.operation)
      ? NACHA_SECURITY_PERMISSIONS.canDownloadPlainNacha
      : NACHA_SECURITY_PERMISSIONS.canDownloadEnvelope;

    return this.auth.hasPermission([requiredPermission, NACHA_SECURITY_PERMISSIONS.canManageAch, NACHA_SECURITY_PERMISSIONS.canReadAch]);
  }

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
    this.service.manualDecrypt(file)
      .pipe(finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (response) => {
          this.operation = response;
          this.cdr.markForCheck();
        },
        error: () => this.notifications.error('No fue posible descifrar el archivo.')
      });
  }

  authorizeAndDownload(): void {
    if (!this.operation || !this.canDownload) {
      return;
    }

    this.authorizing = true;
    this.service.authorizeDownload(this.operation.operationId)
      .pipe(finalize(() => {
        this.authorizing = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: () => {
          this.service.downloadArtifact(this.operation!.operationId).subscribe({
            next: (response) => {
              const blob = response.body ?? new Blob();
              const url = window.URL.createObjectURL(blob);
              const a = document.createElement('a');
              a.href = url;

              const fallback = `${this.operation?.operationId ?? 'operacion'}.txt`;
              a.download = sanitizeDownloadFileName(this.operation?.artifact.externalFileName, fallback);
              a.click();
              window.URL.revokeObjectURL(url);
            },
            error: () => this.notifications.error('La descarga no fue autorizada por backend.')
          });
        },
        error: () => this.notifications.error('No fue posible autorizar la descarga.')
      });
  }

  private requiresPlainDownloadPermission(operation: NachaSecurityOperationResponse): boolean {
    return operation.operationType === 'ManualEnvelopeDecrypt'
      || operation.operationType === 'NachaGeneratePlain'
      || (operation.artifact.contentType ?? '').toLowerCase() === 'text/plain';
  }
}
