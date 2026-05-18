import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { AccountingReviewExportFormat, AccountingReviewExportRequest } from '../models/accounting-review-export.model';
import { ReportsApiService } from '../services/reports-api.service';

@Component({
  selector: 'app-accounting-review-export',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './accounting-review-export.component.html',
  styleUrls: ['./accounting-review-export.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AccountingReviewExportComponent {
  private readonly fb = inject(FormBuilder);
  private readonly reportsApi = inject(ReportsApiService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);

  readonly formats: { value: AccountingReviewExportFormat; label: string }[] = [
    { value: 'pdf', label: 'PDF' },
    { value: 'csv', label: 'CSV' },
    { value: 'xlsx', label: 'Excel' }
  ];

  downloading = false;

  readonly form = this.fb.group({
    format: this.fb.nonNullable.control<AccountingReviewExportFormat>('pdf', [Validators.required]),
    csvDelimiter: this.fb.nonNullable.control(','),
    dateFrom: this.fb.control<string>(''),
    dateTo: this.fb.control<string>(''),
    clearingHouseCode: this.fb.control<string>(''),
    cycleCode: this.fb.control<string>(''),
    fileId: this.fb.control<string>(''),
    fileHash: this.fb.control<string>(''),
    transactionId: this.fb.control<string>(''),
    status: this.fb.control<string>(''),
    causeCode: this.fb.control<string>(''),
    includeOutbound: this.fb.nonNullable.control(true),
    includeIncoming: this.fb.nonNullable.control(true),
    includeReturns: this.fb.nonNullable.control(true),
    includeReturnOfReturn: this.fb.nonNullable.control(true),
    includeManualAuditOnly: this.fb.nonNullable.control(true),
    includeNetting: this.fb.nonNullable.control(true),
    includeLiquidity: this.fb.nonNullable.control(true),
    includeCudEvidence: this.fb.nonNullable.control(true)
  });

  get isCsv(): boolean {
    return this.form.controls.format.value === 'csv';
  }

  downloadReport(): void {
    if (this.form.invalid || this.downloading) {
      return;
    }

    const raw = this.form.getRawValue();
    const request: AccountingReviewExportRequest = {
      format: raw.format,
      csvDelimiter: raw.format === 'csv' ? (raw.csvDelimiter?.trim() || ',') : undefined,
      requestedBy: this.auth.getCurrentUser()?.username || 'spa-operator',
      correlationId: `SPA-ACCOUNTING-REVIEW-${Date.now()}`,
      dateFrom: raw.dateFrom || undefined,
      dateTo: raw.dateTo || undefined,
      clearingHouseCode: raw.clearingHouseCode || undefined,
      cycleCode: raw.cycleCode || undefined,
      fileId: raw.fileId || undefined,
      fileHash: raw.fileHash || undefined,
      transactionId: raw.transactionId || undefined,
      status: raw.status || undefined,
      causeCode: raw.causeCode || undefined,
      includeOutbound: raw.includeOutbound,
      includeIncoming: raw.includeIncoming,
      includeReturns: raw.includeReturns,
      includeReturnOfReturn: raw.includeReturnOfReturn,
      includeManualAuditOnly: raw.includeManualAuditOnly,
      includeNetting: raw.includeNetting,
      includeLiquidity: raw.includeLiquidity,
      includeCudEvidence: raw.includeCudEvidence
    };

    this.downloading = true;
    this.reportsApi.exportAccountingReview(request).subscribe({
      next: (response) => {
        this.triggerDownload(response.body ?? new Blob(), response.headers.get('content-disposition'), raw.format, response.headers.get('content-type'));
        this.notifications.success('Archivo generado para descarga.');
        this.downloading = false;
      },
      error: () => {
        this.notifications.error('No fue posible generar el reporte operativo de revisión en este momento.');
        this.downloading = false;
      }
    });
  }

  private triggerDownload(blob: Blob, contentDisposition: string | null, format: AccountingReviewExportFormat, contentType: string | null): void {
    const extension = format === 'csv' ? 'csv' : format === 'pdf' ? 'pdf' : 'xlsx';
    const fileName = this.extractFileName(contentDisposition) ?? `accounting-review-operativo.${extension}`;
    const fileBlob = new Blob([blob], { type: contentType ?? blob.type ?? 'application/octet-stream' });
    const url = window.URL.createObjectURL(fileBlob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    window.URL.revokeObjectURL(url);
  }

  private extractFileName(contentDisposition: string | null): string | null {
    if (!contentDisposition) return null;
    const match = /filename\*=UTF-8''([^;]+)|filename="?([^";]+)"?/i.exec(contentDisposition);
    const fileName = match?.[1] ?? match?.[2];
    return fileName ? decodeURIComponent(fileName) : null;
  }
}
