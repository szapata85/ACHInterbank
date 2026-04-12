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

  readonly form = this.fb.group({
    date: [''],
    clearingHouseId: [null as number | null],
    achCycleId: ['']
  });

  search(): void {
    const v = this.form.value;
    this.loading = true;
    this.error = null;
    this.cdr.markForCheck();

    this.api.getReconciliation({
      date: v.date || undefined,
      clearingHouseId: v.clearingHouseId ?? undefined,
      achCycleId: v.achCycleId || undefined
    }).subscribe({
      next: (res) => { this.data = res; this.loading = false; this.cdr.markForCheck(); },
      error: () => { this.error = 'No fue posible cargar la conciliación.'; this.loading = false; this.cdr.markForCheck(); }
    });
  }

  exportPdf(): void {
    const v = this.form.value;
    this.api.downloadReconciliationPdf({
      date: v.date || undefined,
      clearingHouseId: v.clearingHouseId ?? undefined,
      achCycleId: v.achCycleId || undefined
    }).subscribe({
      next: (response) => {
        const blob = response.body ?? new Blob();
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = 'reconciliation.pdf';
        link.click();
        window.URL.revokeObjectURL(url);
        this.notifications.success('PDF exportado correctamente.');
      },
      error: () => this.notifications.error('No fue posible exportar el PDF de conciliación.')
    });
  }
}
