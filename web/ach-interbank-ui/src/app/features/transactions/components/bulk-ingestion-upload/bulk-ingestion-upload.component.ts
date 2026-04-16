import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { finalize, take } from 'rxjs';
import { NotificationService } from '../../../../core/services/notification.service';
import { SharedModule } from '../../../../shared/shared.module';
import { BulkIngestionTrackingApiService } from '../../services/bulk-ingestion-tracking-api.service';

@Component({
  selector: 'app-bulk-ingestion-upload',
  standalone: true,
  imports: [SharedModule, RouterModule],
  templateUrl: './bulk-ingestion-upload.component.html',
  styleUrls: ['./bulk-ingestion-upload.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BulkIngestionUploadComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(BulkIngestionTrackingApiService);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);

  readonly acceptedFormats = '.json,.csv,.xlsx,.xls';

  readonly selectedFile = signal<File | null>(null);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successBatchId = signal<string | null>(null);
  readonly metadataForm = this.fb.group({
    batchReference: [''],
    clientRequestId: ['']
  });

  readonly fileExtension = computed(() => {
    const fileName = this.selectedFile()?.name ?? '';
    return fileName.includes('.') ? fileName.split('.').pop()?.toLowerCase() ?? '' : '';
  });

  readonly isValidFile = computed(() => {
    const file = this.selectedFile();
    if (!file) {
      return false;
    }

    const extension = this.fileExtension();
    const validType = ['json', 'csv', 'xlsx', 'xls'].includes(extension);
    const maxSizeBytes = 20 * 1024 * 1024;
    return validType && file.size > 0 && file.size <= maxSizeBytes;
  });

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.item(0) ?? null;
    this.selectedFile.set(file);
    this.errorMessage.set(null);
    this.successBatchId.set(null);
  }

  upload(): void {
    if (this.isSubmitting()) {
      return;
    }
    const file = this.selectedFile();
    if (!file) {
      this.errorMessage.set('Debe seleccionar un archivo para cargar el lote.');
      return;
    }

    if (!this.isValidFile()) {
      this.errorMessage.set('Archivo inválido. Formatos permitidos: JSON, CSV y Excel (.xlsx/.xls), tamaño máximo 20MB.');
      return;
    }

    this.errorMessage.set(null);
    this.isSubmitting.set(true);

    const values = this.metadataForm.getRawValue();
    this.api.upload(file, values.batchReference ?? '', values.clientRequestId ?? '')
      .pipe(
        take(1),
        finalize(() => this.isSubmitting.set(false))
      )
      .subscribe({
        next: (response) => {
          this.successBatchId.set(response.batchId);
          const raw = localStorage.getItem('ach.bulk.recentBatchIds');
          const list = raw ? (JSON.parse(raw) as string[]) : [];
          localStorage.setItem('ach.bulk.recentBatchIds', JSON.stringify([response.batchId, ...list.filter((id) => id !== response.batchId)].slice(0, 25)));
          this.notifications.success(`Lote ${response.batchReference} registrado. Estado inicial: ${response.status}.`);
        },
        error: (error: Error) => {
          this.errorMessage.set(error.message);
          this.notifications.error(error.message);
        }
      });
  }

  goToTracking(): void {
    this.router.navigate(['/transactions/bulk-ingestion/tracking']);
  }

  goToDetail(): void {
    const batchId = this.successBatchId();
    if (batchId) {
      this.router.navigate(['/transactions/bulk-ingestion', batchId]);
    }
  }
}
