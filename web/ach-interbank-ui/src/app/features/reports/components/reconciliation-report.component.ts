import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../../shared/shared.module';
import { ReportsApiService, ReconciliationReportResponse } from '../services/reports-api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { extractReportFileName, validatePdfBlob } from '../report-presentation';

@Component({
  selector: 'app-reconciliation-report',
  standalone: true,
  imports: [SharedModule, RouterModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressBarModule],
  templateUrl: './reconciliation-report.component.html',
  styleUrls: ['./reconciliation-report.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReconciliationReportComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ReportsApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  loading = false;
  exporting = false;
  data: ReconciliationReportResponse | null = null;
  error: string | null = null;
  exportMessage: string | null = null;

  readonly form = this.fb.group({
    date: [''],
    clearingHouseId: [null as number | null],
    achCycleId: ['']
  });

  search(): void {
    const v = this.form.value;
    this.loading = true;
    this.error = null;
    this.exportMessage = null;
    this.cdr.markForCheck();

    this.api.getReconciliation({
      date: v.date || undefined,
      clearingHouseId: v.clearingHouseId ?? undefined,
      achCycleId: v.achCycleId || undefined
    }).subscribe({
      next: (res) => {
        this.data = res;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.error = 'No pudimos consultar la conciliación en este momento. Intenta nuevamente.';
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  exportPdf(): void {
    if (this.exporting) {
      return;
    }

    if (!this.hasExportableData()) {
      this.exportMessage = 'Consulta primero información con resultados para descargar el PDF.';
      this.notifications.error(this.exportMessage);
      this.cdr.markForCheck();
      return;
    }

    const v = this.form.value;
    this.exporting = true;
    this.cdr.markForCheck();
    this.api.downloadReconciliationPdf({
      date: v.date || undefined,
      clearingHouseId: v.clearingHouseId ?? undefined,
      achCycleId: v.achCycleId || undefined
    }).subscribe({
      next: async (response) => {
        const blob = response.body ?? new Blob();
        const contentType = response.headers.get('content-type') ?? blob.type;
        const invalidMessage = await validatePdfBlob(blob, contentType);
        if (invalidMessage) {
          this.exportMessage = invalidMessage;
          this.notifications.error(invalidMessage);
          this.exporting = false;
          this.cdr.markForCheck();
          return;
        }

        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = extractReportFileName(response.headers.get('content-disposition'), 'conciliacion-ach.pdf');
        link.click();
        window.URL.revokeObjectURL(url);
        this.exportMessage = null;
        this.exporting = false;
        this.notifications.success('El reporte de conciliación se descargó correctamente.');
        this.cdr.markForCheck();
      },
      error: () => {
        this.exporting = false;
        this.exportMessage = 'No pudimos generar el PDF de conciliación en este momento. Intenta nuevamente.';
        this.notifications.error(this.exportMessage);
        this.cdr.markForCheck();
      }
    });
  }

  clearFilters(): void {
    this.form.reset({ date: '', clearingHouseId: null, achCycleId: '' });
    this.data = null;
    this.error = null;
    this.exportMessage = null;
  }

  hasExportableData(): boolean {
    if (!this.data) {
      return false;
    }

    const totals = this.data.totals;
    const totalCounts = (totals.sentCount ?? 0) + (totals.receivedCount ?? 0) + (totals.returnedCount ?? 0);
    const totalAmounts = Math.abs(totals.sentAmount ?? 0) + Math.abs(totals.receivedAmount ?? 0) + Math.abs(totals.returnedAmount ?? 0);
    return totalCounts > 0 || totalAmounts > 0 || (this.data.inconsistencies?.length ?? 0) > 0;
  }

}
