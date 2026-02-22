import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../../shared/shared.module';
import { NotificationService } from '../../../core/services/notification.service';
import { ReportsApiService } from '../services/reports-api.service';
import { AchCyclesApiService } from '../../ach-cycles/services/ach-cycles-api.service';

@Component({
  selector: 'app-traceability-report',
  standalone: true,
  imports: [SharedModule, RouterModule],
  templateUrl: './traceability-report.component.html',
  styleUrls: ['./traceability-report.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TraceabilityReportComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ReportsApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly achCyclesApi = inject(AchCyclesApiService);

  loading = false;

  readonly states: Array<{ value: '' | 'Pending' | 'ReturnedByOperator' | 'ReturnedByEpr' | 'AppliedTacitly' | 'Certified'; label: string }> = [
    { value: '', label: 'Todos los estados' },
    { value: 'Pending', label: 'Pendiente' },
    { value: 'ReturnedByOperator', label: 'Devuelto por operador' },
    { value: 'ReturnedByEpr', label: 'Devuelto por EPR' },
    { value: 'AppliedTacitly', label: 'Aplicado tácitamente' },
    { value: 'Certified', label: 'Certificado' }
  ];

  achCycleOptions: Array<{ id: string; cycleName: string; clearingHouseName: string }> = [];

  readonly form = this.fb.group({
    fromUtc: [''],
    toUtc: [''],
    state: ['' as '' | 'Pending' | 'ReturnedByOperator' | 'ReturnedByEpr' | 'AppliedTacitly' | 'Certified'],
    achCycleId: this.fb.control<string[]>([], { nonNullable: true })
  });

  ngOnInit(): void {
    this.achCyclesApi.search({ page: 1, pageSize: 200 }).subscribe({
      next: (response) => {
        this.achCycleOptions = (response?.items ?? [])
          .filter((item) => !!item.id)
          .map((item) => ({
            id: item.id,
            cycleName: item.cycleName || item.id,
            clearingHouseName: item.clearingHouseName || 'Cámara compensadora no definida'
          }))
          .sort((a, b) => a.clearingHouseName.localeCompare(b.clearingHouseName) || a.cycleName.localeCompare(b.cycleName));
        this.cdr.markForCheck();
      },
      error: () => {
        this.achCycleOptions = [];
        this.cdr.markForCheck();
      }
    });
  }

  generatePdf(): void {
    if (this.isInvalidDateRange()) {
      this.notifications.error('La fecha inicial no puede ser posterior a la fecha final');
      return;
    }

    this.loading = true;
    this.cdr.markForCheck();

    const payload = {
      fromUtc: this.form.value.fromUtc ? `${this.form.value.fromUtc}T00:00:00Z` : undefined,
      toUtc: this.form.value.toUtc ? `${this.form.value.toUtc}T23:59:59Z` : undefined,
      state: this.form.value.state ?? '',
      achCycleId: (this.form.value.achCycleId ?? []).filter((value) => !!value)
    };

    this.api.downloadTraceabilityPdf(payload).subscribe({
      next: async (response) => {
        const blob = response.body ?? new Blob();
        const contentType = (response.headers.get('content-type') ?? blob.type ?? '').toLowerCase();
        const fileName = this.extractFileName(response.headers.get('content-disposition')) ?? `ACH_Traceability_${this.buildTimestamp()}.pdf`;

        const serverErrorMessage = await this.tryExtractServerErrorMessage(blob, contentType);
        if (serverErrorMessage) {
          this.notifications.error(serverErrorMessage);
          this.loading = false;
          this.cdr.markForCheck();
          return;
        }

        const url = window.URL.createObjectURL(blob);

        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        link.click();

        window.URL.revokeObjectURL(url);
        this.loading = false;
        this.notifications.success('Reporte generado correctamente');
        this.cdr.markForCheck();
      },
      error: (error) => {
        const message = error?.error?.message ?? 'No fue posible generar el reporte de trazabilidad.';
        this.notifications.error(String(message));
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  private async tryExtractServerErrorMessage(blob: Blob, contentType: string): Promise<string | null> {
    const isTextLikeError = contentType.includes('application/json') || contentType.includes('text/plain') || contentType.includes('text/html');
    if (!isTextLikeError) {
      return null;
    }

    try {
      const raw = await blob.text();
      if (!raw) {
        return 'No fue posible generar el reporte de trazabilidad.';
      }

      if (contentType.includes('application/json')) {
        const parsed = JSON.parse(raw) as { message?: string };
        return parsed?.message?.trim() || 'No fue posible generar el reporte de trazabilidad.';
      }

      return 'No fue posible generar el reporte de trazabilidad. Verifica filtros/permisos e intenta nuevamente.';
    } catch {
      return 'No fue posible generar el reporte de trazabilidad.';
    }
  }

  private isInvalidDateRange(): boolean {
    const { fromUtc, toUtc } = this.form.value;
    return Boolean(fromUtc && toUtc && new Date(fromUtc) > new Date(toUtc));
  }

  private extractFileName(contentDisposition: string | null): string | null {
    if (!contentDisposition) {
      return null;
    }

    const match = /filename\*=UTF-8''([^;]+)|filename="?([^";]+)"?/i.exec(contentDisposition);
    const fileName = match?.[1] ?? match?.[2];

    return fileName ? decodeURIComponent(fileName) : null;
  }

  private buildTimestamp(): string {
    const now = new Date();
    const pad = (value: number) => value.toString().padStart(2, '0');
    return `${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}_${pad(now.getHours())}${pad(now.getMinutes())}${pad(now.getSeconds())}`;
  }
}

