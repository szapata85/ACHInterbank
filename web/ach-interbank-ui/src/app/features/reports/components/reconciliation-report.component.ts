import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../../shared/shared.module';
import { ReportsApiService, ReconciliationReportResponse } from '../services/reports-api.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-reconciliation-report',
  standalone: true,
  imports: [SharedModule, RouterModule],
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
        this.error = 'No fue posible cargar la conciliacion.';
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  exportPdf(): void {
    if (!this.hasExportableData()) {
      this.exportMessage = 'No hay informacion para exportar.';
      this.notifications.error(this.exportMessage);
      this.cdr.markForCheck();
      return;
    }

    const v = this.form.value;
    this.api.downloadReconciliationPdf({
      date: v.date || undefined,
      clearingHouseId: v.clearingHouseId ?? undefined,
      achCycleId: v.achCycleId || undefined
    }).subscribe({
      next: async (response) => {
        const blob = response.body ?? new Blob();
        const invalidMessage = await this.getInvalidPdfMessage(blob);
        if (invalidMessage) {
          this.exportMessage = invalidMessage;
          this.notifications.error(invalidMessage);
          this.cdr.markForCheck();
          return;
        }

        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = 'reconciliation.pdf';
        link.click();
        window.URL.revokeObjectURL(url);
        this.exportMessage = null;
        this.notifications.success('PDF exportado correctamente.');
        this.cdr.markForCheck();
      },
      error: () => {
        this.exportMessage = 'No fue posible exportar el PDF de conciliacion.';
        this.notifications.error(this.exportMessage);
        this.cdr.markForCheck();
      }
    });
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

  private async getInvalidPdfMessage(blob: Blob): Promise<string | null> {
    if (blob.size === 0) {
      return 'No hay informacion para exportar.';
    }

    if (blob.size < 512) {
      return 'El PDF generado no contiene informacion suficiente para descargar.';
    }

    const header = await blob.slice(0, 5).text().catch(() => '');
    return header === '%PDF-' ? null : 'El archivo generado no es un PDF valido.';
  }
}
