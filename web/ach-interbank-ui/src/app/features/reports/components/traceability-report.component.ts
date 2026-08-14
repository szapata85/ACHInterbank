import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../../shared/shared.module';
import { NotificationService } from '../../../core/services/notification.service';
import { ReportsApiService } from '../services/reports-api.service';
import { AchCyclesApiService } from '../../ach-cycles/services/ach-cycles-api.service';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { REPORT_STATE_OPTIONS, extractReportFileName, validatePdfBlob } from '../report-presentation';

@Component({
  selector: 'app-traceability-report',
  standalone: true,
  imports: [SharedModule, RouterModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressBarModule, MatSelectModule],
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
  exportError: string | null = null;

  readonly states = REPORT_STATE_OPTIONS;

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
    this.exportError = null;
    this.cdr.markForCheck();

    const payload = {
      fromUtc: this.form.value.fromUtc ? `${this.form.value.fromUtc}T00:00:00Z` : undefined,
      toUtc: this.form.value.toUtc ? `${this.form.value.toUtc}T23:59:59Z` : undefined,
      state: this.form.value.state ?? '',
      achCycleId: this.getDistinctAchCycleIds()
    };

    this.api.downloadTraceabilityPdf(payload).subscribe({
      next: async (response) => {
        const blob = response.body ?? new Blob();
        const contentType = (response.headers.get('content-type') ?? blob.type ?? '').toLowerCase();
        const fileName = extractReportFileName(response.headers.get('content-disposition'), `trazabilidad-ach-${this.buildTimestamp()}.pdf`);

        const serverErrorMessage = await this.tryExtractServerErrorMessage(blob, contentType);
        if (serverErrorMessage) {
          this.exportError = serverErrorMessage;
          this.notifications.error(serverErrorMessage);
          this.loading = false;
          this.cdr.markForCheck();
          return;
        }

        const invalidPdfMessage = await validatePdfBlob(blob, contentType);
        if (invalidPdfMessage) {
          this.exportError = invalidPdfMessage;
          this.notifications.error(invalidPdfMessage);
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
        this.exportError = null;
        this.notifications.success('El reporte de trazabilidad se descargó correctamente.');
        this.cdr.markForCheck();
      },
      error: (error) => {
        const message = 'No pudimos generar el reporte de trazabilidad en este momento. Intenta nuevamente.';
        this.exportError = message;
        this.notifications.error(message);
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

  private getDistinctAchCycleIds(): string[] {
    return Array.from(new Set((this.form.value.achCycleId ?? []).filter((value) => !!value)));
  }

  private isInvalidDateRange(): boolean {
    const { fromUtc, toUtc } = this.form.value;
    return Boolean(fromUtc && toUtc && new Date(fromUtc) > new Date(toUtc));
  }

  private buildTimestamp(): string {
    const now = new Date();
    const pad = (value: number) => value.toString().padStart(2, '0');
    return `${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}_${pad(now.getHours())}${pad(now.getMinutes())}${pad(now.getSeconds())}`;
  }
}

