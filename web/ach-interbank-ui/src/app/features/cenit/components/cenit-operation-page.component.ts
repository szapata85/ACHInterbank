import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { CenitOperationsApiService } from '../services/cenit-operations-api.service';
import { CenitNetPositionRow, CenitOptimizationDecisionRow, CenitQueueRow, CenitTraceabilityRow } from '../models/cenit.models';
import { CycleReportRow } from '../../reports/services/reports-api.service';

type OperationView = 'ciclos' | 'cola' | 'neteo' | 'optimizacion' | 'devoluciones' | 'trazabilidad';

@Component({
  selector: 'app-cenit-operation-page',
  templateUrl: './cenit-operation-page.component.html',
  styleUrls: ['./cenit-operation-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CenitOperationPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(CenitOperationsApiService);

  view: OperationView = 'ciclos';
  titulo = '';
  subtitulo = '';
  loading = false;
  error = '';
  filtro = '';

  rows: Array<Record<string, string>> = [];

  ngOnInit(): void {
    this.view = (this.route.snapshot.data['view'] as OperationView) ?? 'ciclos';
    this.resolveHeader();
    this.load();
  }

  get filteredRows(): Array<Record<string, string>> {
    const lower = this.filtro.trim().toLowerCase();
    if (!lower) {
      return this.rows;
    }

    return this.rows.filter((row) => Object.values(row).some((value) => value.toLowerCase().includes(lower)));
  }

  get hasRows(): boolean {
    return this.filteredRows.length > 0;
  }

  get headers(): string[] {
    return this.filteredRows[0] ? Object.keys(this.filteredRows[0]) : [];
  }

  private load(): void {
    this.loading = true;
    this.error = '';

    switch (this.view) {
      case 'ciclos':
        this.api
          .getCycles({ page: 1, pageSize: 50 })
          .pipe(finalize(() => (this.loading = false)))
          .subscribe({
            next: (items) => (this.rows = items.map((x) => this.mapCycle(x))),
            error: () => (this.error = 'No fue posible consultar ciclos operativos.')
          });
        return;
      case 'cola':
        this.api
          .getQueueTransactions('', 1, 50)
          .pipe(finalize(() => (this.loading = false)))
          .subscribe({
            next: (response) => (this.rows = (response.items ?? []).map((x) => this.mapQueue(x))),
            error: () => (this.error = 'No fue posible consultar la cola operativa.')
          });
        return;
      case 'neteo':
        this.api
          .getNetPositions()
          .pipe(finalize(() => (this.loading = false)))
          .subscribe({
            next: (response) => (this.rows = (response.items ?? []).map((x) => this.mapNet(x))),
            error: () => (this.error = 'No fue posible consultar posiciones netas.')
          });
        return;
      case 'optimizacion':
        this.api
          .getOptimizationDecisions()
          .pipe(finalize(() => (this.loading = false)))
          .subscribe({
            next: (response) => (this.rows = (response.items ?? []).map((x) => this.mapOptimization(x))),
            error: () => (this.error = 'No fue posible consultar decisiones de optimización.')
          });
        return;
      case 'devoluciones':
        this.api
          .getDeferredTransactions(1, 50)
          .pipe(finalize(() => (this.loading = false)))
          .subscribe({
            next: (response) => (this.rows = (response.items ?? []).map((x) => this.mapQueue(x))),
            error: () => (this.error = 'No fue posible consultar devoluciones/diferidas.')
          });
        return;
      case 'trazabilidad':
        this.api
          .getTraceability({ page: 1, pageSize: 50 })
          .pipe(finalize(() => (this.loading = false)))
          .subscribe({
            next: (items) => (this.rows = items.map((x) => this.mapTrace(x))),
            error: () => (this.error = 'No fue posible consultar trazabilidad/devoluciones.')
          });
        return;
    }
  }

  private mapCycle(row: CycleReportRow): Record<string, string> {
    return {
      Ciclo: row.cycleName,
      'ID ciclo': row.cycleId,
      Fecha: row.processingDate,
      Estado: row.status,
      Cámara: row.clearingHouseName,
      Transacciones: String(row.totalTransactions),
      Monto: this.formatAmount(row.totalAmount)
    };
  }

  private mapTrace(row: CenitTraceabilityRow): Record<string, string> {
    return {
      Transacción: String(row.transactionId),
      'ID externo': row.transactionExternalId || '-',
      Referencia: row.reference || '-',
      Ciclo: row.achCycleName,
      'ID ciclo': row.achCycleId,
      Cámara: row.clearingHouseName,
      Estado: row.state,
      Causal: `${row.causalCode} ${row.causalDescription}`.trim(),
      'Fecha valor': row.effectiveEntryDate,
      Monto: this.formatAmount(row.amount)
    };
  }

  private mapNet(row: CenitNetPositionRow): Record<string, string> {
    return {
      Entidad: row.financialInstitutionName || String(row.financialInstitutionId ?? '-'),
      Posición: this.formatAmount(row.netAmount ?? 0),
      Liquidez: this.formatAmount(row.availableLiquidity ?? 0),
      Tipo: row.positionType || '-'
    };
  }

  private mapQueue(row: CenitQueueRow): Record<string, string> {
    return {
      'ID cola': String(row.id),
      Estado: row.status,
      Motivo: row.queueReason,
      Transacción: String(row.transactionId),
      'ID externo': row.transactionExternalId || '-',
      Referencia: row.reference || '-',
      Tipo: row.transactionType,
      'Estado transacción': row.transactionState,
      'Ciclo original': row.originalAchCycleId || '-',
      'Ciclo destino': `${row.targetCycleName} (${row.targetAchCycleId})`,
      Encolado: row.enqueuedAtUtc,
      Desencolado: row.dequeuedAtUtc || '-'
    };
  }

  private mapOptimization(row: CenitOptimizationDecisionRow): Record<string, string> {
    return {
      Transacción: String(row.achTransactionId),
      Decisión: row.decisionType,
      Motivo: row.decisionReason,
      Prioridad: String(row.priority),
      'Ciclo origen': row.fromCycleId,
      'Ciclo destino': row.toCycleId || '-',
      'Fecha decisión': row.decidedAtUtc
    };
  }

  private formatAmount(value: number): string {
    return new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP', maximumFractionDigits: 2 }).format(value);
  }

  private resolveHeader(): void {
    const map: Record<OperationView, { titulo: string; subtitulo: string }> = {
      ciclos: { titulo: 'Ciclos del día', subtitulo: 'Monitoreo de ejecución y volumen operativo.' },
      cola: { titulo: 'Cola y transacciones diferidas', subtitulo: 'Visibilidad de operaciones pendientes por causal/estado.' },
      neteo: { titulo: 'Posiciones netas por entidad', subtitulo: 'Consolidado operativo para compensación.' },
      optimizacion: { titulo: 'Decisiones de optimización', subtitulo: 'Trazabilidad de reglas de liquidez y priorización.' },
      devoluciones: { titulo: 'Transacciones diferidas', subtitulo: 'Operaciones en cola con diferimiento por regla operativa.' },
      trazabilidad: { titulo: 'Trazabilidad CENIT/ACH', subtitulo: 'Ciclo, lote, archivo, causal y decisión operativa.' }
    };

    this.titulo = map[this.view].titulo;
    this.subtitulo = map[this.view].subtitulo;
  }
}
