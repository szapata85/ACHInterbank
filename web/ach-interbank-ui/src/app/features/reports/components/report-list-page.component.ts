import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { NotificationService } from '../../../core/services/notification.service';
import { TableColumn } from '../../../shared/components/table.component';
import { SharedModule } from '../../../shared/shared.module';
import { ReportsApiService } from '../services/reports-api.service';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import {
  extractReportFileName,
  formatReportValue,
  REPORT_SOURCE_OPTIONS,
  REPORT_STATE_OPTIONS,
  validatePdfBlob
} from '../report-presentation';

type ReportKey = 'sent' | 'received' | 'returns' | 'rejections' | 'files' | 'cycles' | 'audit' | 'history';

interface ReportConfig {
  key: ReportKey;
  title: string;
  subtitle: string;
  columns: TableColumn[];
}

@Component({
  selector: 'app-report-list-page',
  standalone: true,
  imports: [SharedModule, RouterModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressBarModule, MatSelectModule],
  templateUrl: './report-list-page.component.html',
  styleUrls: ['./report-list-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReportListPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ReportsApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  loading = false;
  exporting = false;
  hasLoaded = false;
  loadError: string | null = null;
  rows: any[] = [];
  total = 0;
  page = 1;
  pageSize = 25;

  readonly states = REPORT_STATE_OPTIONS;
  readonly sources = REPORT_SOURCE_OPTIONS;

  config: ReportConfig = {
    key: 'sent',
    title: 'Reporte',
    subtitle: '',
    columns: []
  };

  readonly form = this.fb.group({
    date: [''],
    clearingHouseId: [null as number | null],
    achCycleId: [''],
    state: [''],
    reference: [''],
    causal: [''],
    name: [''],
    user: [''],
    action: [''],
    entity: [''],
    fromUtc: [''],
    toUtc: [''],
    transactionId: [''],
    toState: [''],
    source: ['']
  });

  ngOnInit(): void {
    const key = (this.route.snapshot.data['reportKey'] as ReportKey) || 'sent';
    this.config = this.buildConfig(key);
    this.search();
  }

  search(page = 1): void {
    this.page = page;
    this.loading = true;
    this.loadError = null;
    this.cdr.markForCheck();

    (this.requestData() as any).subscribe({
      next: (response: any) => {
        this.rows = (response?.items ?? []).map((row: Record<string, unknown>) => this.presentRow(row));
        this.total = response?.total ?? this.rows.length;
        this.loading = false;
        this.hasLoaded = true;
        this.cdr.markForCheck();
      },
      error: () => {
        this.rows = [];
        this.total = 0;
        this.loadError = 'No pudimos consultar este reporte en este momento. Intenta nuevamente.';
        this.loading = false;
        this.hasLoaded = true;
        this.cdr.markForCheck();
      }
    });
  }

  clear(): void {
    this.form.reset({
      date: '', clearingHouseId: null, achCycleId: '', state: '', reference: '', causal: '', name: '',
      user: '', action: '', entity: '', fromUtc: '', toUtc: '', transactionId: '', toState: '', source: ''
    });
    this.search(1);
  }

  exportPdf(): void {
    if (this.exporting) return;
    this.exporting = true;
    this.cdr.markForCheck();
    this.requestPdf().subscribe({
      next: async (response) => {
        const blob = response.body ?? new Blob();
        const invalidMessage = await validatePdfBlob(blob, response.headers.get('content-type'));
        if (invalidMessage) {
          this.notifications.error(invalidMessage);
          this.exporting = false;
          this.cdr.markForCheck();
          return;
        }
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = extractReportFileName(response.headers.get('content-disposition'), `${this.config.key}.pdf`);
        link.click();
        window.URL.revokeObjectURL(url);
        this.notifications.success('El reporte está listo y se descargó correctamente.');
        this.exporting = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.notifications.error('No pudimos generar el PDF en este momento. Intenta nuevamente.');
        this.exporting = false;
        this.cdr.markForCheck();
      }
    });
  }

  private requestData() {
    const v = this.form.value;
    switch (this.config.key) {
      case 'sent': return this.api.getSentTransactions({ date: v.date || undefined, clearingHouseId: this.toNumber(v.clearingHouseId), achCycleId: v.achCycleId || undefined, state: (v.state as any) || undefined, reference: v.reference || undefined, page: this.page, pageSize: this.pageSize });
      case 'received': return this.api.getReceivedTransactions({ date: v.date || undefined, clearingHouseId: this.toNumber(v.clearingHouseId), achCycleId: v.achCycleId || undefined, state: (v.state as any) || undefined, reference: v.reference || undefined, page: this.page, pageSize: this.pageSize });
      case 'returns': return this.api.getReturns({ date: v.date || undefined, clearingHouseId: this.toNumber(v.clearingHouseId), state: (v.state as any) || undefined, reference: v.reference || undefined, causal: v.causal || undefined, page: this.page, pageSize: this.pageSize });
      case 'rejections': return this.api.getRejections({ date: v.date || undefined, clearingHouseId: this.toNumber(v.clearingHouseId), state: (v.state as any) || undefined, reference: v.reference || undefined, causal: v.causal || undefined, page: this.page, pageSize: this.pageSize });
      case 'files': return this.api.getNachaFiles({ date: v.date || undefined, clearingHouseId: this.toNumber(v.clearingHouseId), page: this.page, pageSize: this.pageSize });
      case 'cycles': return this.api.getCyclesReport({ date: v.date || undefined, clearingHouseId: this.toNumber(v.clearingHouseId), name: v.name || undefined, page: this.page, pageSize: this.pageSize });
      case 'audit': return this.api.getAudit({ user: v.user || undefined, action: v.action || undefined, entity: v.entity || undefined, fromUtc: v.fromUtc ? `${v.fromUtc}T00:00:00Z` : undefined, toUtc: v.toUtc ? `${v.toUtc}T23:59:59Z` : undefined, page: this.page, pageSize: this.pageSize });
      case 'history': return this.api.getHistory({ fromUtc: v.fromUtc ? `${v.fromUtc}T00:00:00Z` : undefined, toUtc: v.toUtc ? `${v.toUtc}T23:59:59Z` : undefined, transactionId: this.toNumber(v.transactionId), toState: (v.toState as any) || undefined, source: (v.source as any) || undefined, page: this.page, pageSize: this.pageSize });
    }
  }

  private requestPdf() {
    const v = this.form.value;
    switch (this.config.key) {
      case 'sent': return this.api.downloadSentTransactionsPdf({ date: v.date || undefined, clearingHouseId: this.toNumber(v.clearingHouseId), achCycleId: v.achCycleId || undefined, state: (v.state as any) || undefined, reference: v.reference || undefined });
      case 'received': return this.api.downloadReceivedTransactionsPdf({ date: v.date || undefined, clearingHouseId: this.toNumber(v.clearingHouseId), achCycleId: v.achCycleId || undefined, state: (v.state as any) || undefined, reference: v.reference || undefined });
      case 'returns': return this.api.downloadReturnsPdf({ date: v.date || undefined, clearingHouseId: this.toNumber(v.clearingHouseId), state: (v.state as any) || undefined, reference: v.reference || undefined, causal: v.causal || undefined });
      case 'rejections': return this.api.downloadRejectionsPdf({ date: v.date || undefined, clearingHouseId: this.toNumber(v.clearingHouseId), state: (v.state as any) || undefined, reference: v.reference || undefined, causal: v.causal || undefined });
      case 'files': return this.api.downloadNachaFilesPdf({ date: v.date || undefined, clearingHouseId: this.toNumber(v.clearingHouseId) });
      case 'cycles': return this.api.downloadCyclesPdf({ date: v.date || undefined, clearingHouseId: this.toNumber(v.clearingHouseId), name: v.name || undefined });
      case 'audit': return this.api.downloadAuditPdf({ user: v.user || undefined, action: v.action || undefined, entity: v.entity || undefined, fromUtc: v.fromUtc ? `${v.fromUtc}T00:00:00Z` : undefined, toUtc: v.toUtc ? `${v.toUtc}T23:59:59Z` : undefined });
      case 'history': return this.api.downloadHistoryPdf({ fromUtc: v.fromUtc ? `${v.fromUtc}T00:00:00Z` : undefined, toUtc: v.toUtc ? `${v.toUtc}T23:59:59Z` : undefined, transactionId: this.toNumber(v.transactionId), toState: (v.toState as any) || undefined, source: (v.source as any) || undefined });
    }
  }

  private toNumber(value: unknown): number | undefined {
    if (value === null || value === undefined || value === '') return undefined;
    const num = Number(value);
    return Number.isFinite(num) ? num : undefined;
  }

  private presentRow(row: Record<string, unknown>): Record<string, unknown> {
    return Object.fromEntries(Object.entries(row).map(([key, value]) => [key, formatReportValue(key, value)]));
  }

  private buildConfig(key: ReportKey): ReportConfig {
    const base: Record<ReportKey, ReportConfig> = {
      sent: { key, title: 'Enviados', subtitle: 'Reporte de transacciones enviadas', columns: [
        { key: 'transactionId', label: 'Identificador' }, { key: 'effectiveEntryDate', label: 'Fecha' }, { key: 'transactionExternalId', label: 'Identificador de operación' }, { key: 'reference', label: 'Referencia anterior' }, { key: 'amount', label: 'Monto' }, { key: 'state', label: 'Estado' }, { key: 'sourceBankName', label: 'Banco de origen' }, { key: 'destinationBankName', label: 'Banco de destino' }
      ] },
      received: { key, title: 'Recibidos', subtitle: 'Reporte de transacciones recibidas', columns: [
        { key: 'transactionId', label: 'Identificador' }, { key: 'effectiveEntryDate', label: 'Fecha' }, { key: 'transactionExternalId', label: 'Identificador de operación' }, { key: 'reference', label: 'Referencia anterior' }, { key: 'amount', label: 'Monto' }, { key: 'state', label: 'Estado' }, { key: 'sourceBankName', label: 'Banco de origen' }, { key: 'destinationBankName', label: 'Banco de destino' }
      ] },
      returns: { key, title: 'Devoluciones', subtitle: 'Reporte de devoluciones ACH', columns: [
        { key: 'transactionId', label: 'Identificador' }, { key: 'effectiveEntryDate', label: 'Fecha' }, { key: 'transactionExternalId', label: 'Identificador de operación' }, { key: 'reference', label: 'Referencia anterior' }, { key: 'causalCode', label: 'Causal' }, { key: 'amount', label: 'Monto' }, { key: 'state', label: 'Estado' }
      ] },
      rejections: { key, title: 'Rechazos', subtitle: 'Reporte de rechazos ACH', columns: [
        { key: 'transactionId', label: 'Identificador' }, { key: 'effectiveEntryDate', label: 'Fecha' }, { key: 'transactionExternalId', label: 'Identificador de operación' }, { key: 'reference', label: 'Referencia anterior' }, { key: 'causalCode', label: 'Causal' }, { key: 'amount', label: 'Monto' }, { key: 'state', label: 'Estado' }
      ] },
      files: { key, title: 'Archivos', subtitle: 'Reporte de archivos NACHA', columns: [
        { key: 'fileName', label: 'Archivo' }, { key: 'generatedAtUtc', label: 'Fecha' }, { key: 'clearingHouseName', label: 'Cámara' }, { key: 'totalRecords', label: 'Registros', align: 'end' }, { key: 'totalTransactions', label: 'Transacciones', align: 'end' }
      ] },
      cycles: { key, title: 'Ciclos', subtitle: 'Reporte de ciclos ACH', columns: [
        { key: 'cycleName', label: 'Nombre' }, { key: 'schedule', label: 'Horario' }, { key: 'processingDate', label: 'Fecha' }, { key: 'status', label: 'Estado' }, { key: 'totalTransactions', label: 'Transacciones' }, { key: 'totalAmount', label: 'Monto' }
      ] },
      audit: { key, title: 'Auditoría', subtitle: 'Traza operativa por usuario/acción/entidad', columns: [
        { key: 'user', label: 'Usuario' }, { key: 'action', label: 'Acción' }, { key: 'entity', label: 'Entidad' }, { key: 'entityId', label: 'Identificador de entidad' }, { key: 'dateUtc', label: 'Fecha y hora' }
      ] },
      history: { key, title: 'Histórico', subtitle: 'Histórico de cambios por rango de fechas', columns: [
        { key: 'transactionId', label: 'Transacción' }, { key: 'fromState', label: 'Estado anterior' }, { key: 'toState', label: 'Estado nuevo' }, { key: 'source', label: 'Fuente' }, { key: 'reasonCode', label: 'Causal' }, { key: 'dateUtc', label: 'Fecha y hora' }
      ] }
    };

    return base[key];
  }

  get showDateFilter(): boolean { return ['sent','received','returns','rejections','files','cycles'].includes(this.config.key); }
  get showStateFilter(): boolean { return ['sent','received','returns','rejections'].includes(this.config.key); }
  get showReferenceFilter(): boolean { return ['sent','received','returns','rejections'].includes(this.config.key); }
  get showCausalFilter(): boolean { return ['returns','rejections'].includes(this.config.key); }
  get showNameFilter(): boolean { return this.config.key === 'cycles'; }
  get showAuditFilters(): boolean { return this.config.key === 'audit'; }
  get showHistoryFilters(): boolean { return this.config.key === 'history'; }
}
