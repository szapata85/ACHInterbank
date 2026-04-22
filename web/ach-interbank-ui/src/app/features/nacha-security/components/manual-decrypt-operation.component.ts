import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import { NotificationService } from '../../../core/services/notification.service';
import { NachaSecurityOperationsApiService } from '../services/nacha-security-operations-api.service';
import { NachaSecurityOperationResponse } from '../models/nacha-security-operation.model';

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

  loading = false;
  authorizing = false;
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
    if (!this.operation) {
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
              a.download = this.operation?.artifact.externalFileName || `${this.operation?.operationId}.txt`;
              a.click();
              window.URL.revokeObjectURL(url);
            },
            error: () => this.notifications.error('La descarga no fue autorizada por backend.')
          });
        },
        error: () => this.notifications.error('No fue posible autorizar la descarga.')
      });
  }
}
