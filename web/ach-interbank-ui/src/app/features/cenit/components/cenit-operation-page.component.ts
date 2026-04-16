import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { CenitOperationsApiService } from '../services/cenit-operations-api.service';
import { CenitNetPositionRow, CenitOptimizationDecisionRow, CenitQueueRow, CenitTraceabilityRow } from '../models/cenit.models';
import { CycleReportRow } from '../../reports/services/reports-api.service';
import { SharedModule } from '../../../shared/shared.module';
import { ColDef } from 'ag-grid-community';

type OperationView = 'ciclos' | 'cola' | 'neteo' | 'optimizacion' | 'devoluciones' | 'trazabilidad';

interface IndicadorOperacion {
  etiqueta: string;
  valor: string;
  estado: 'activo' | 'pendiente' | 'exitoso';
}

@Component({
  selector: 'app-cenit-operation-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, SharedModule],
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
  mensajeOperacion = '';
  loading = false;
  error = '';

  readonly filtroControl = new FormControl<string>('', { nonNullable: true });

  readonly migas = [
    { etiqueta: 'Inicio', ruta: '/' },
    { etiqueta: 'CENIT', ruta: '/cenit' },
    { etiqueta: 'Operación' }
  ];

  rows: Array<Record<string, string>> = [];

  ngOnInit(): void {
    this.view = (this.route.snapshot.data['view'] as OperationView) ?? 'ciclos';
    this.resolveHeader();
    this.load();
  }

  get filteredRows(): Array<Record<string, string>> {
    const lower = this.filtroControl.value.trim().toLowerCase();
    if (!lower) {
      return this.rows;
    }

    return this.rows.filter((row) => Object.values(row).some((value) => value.toLowerCase().includes(lower)));
  }

  get hasRows(): boolean {
    return this.filteredRows.length > 0;
  }

  get resumenVista(): IndicadorOperacion[] {
    const cantidad = this.filteredRows.length;
    const etiqueta = this.view === 'trazabilidad' ? 'eventos' : 'registros';

    return [
      {
        etiqueta: 'Vista activa',
        valor: this.titulo,
        estado: 'activo'
      },
      {
        etiqueta: `Total de ${etiqueta}`,
        valor: cantidad.toLocaleString('es-CO'),
        estado: cantidad > 0 ? 'exitoso' : 'pendiente'
      },
      {
        etiqueta: 'Última actualización',
        valor: new Date().toLocaleString('es-CO'),
        estado: 'activo'
      }
    ];
  }

  get columnasTabla(): ColDef<Record<string, string>>[] {
    const headers = this.filteredRows[0] ? Object.keys(this.filteredRows[0]) : [];

    return headers.map((header) => ({
      field: header,
      headerName: header,
      sortable: true,
      filter: 'agTextColumnFilter',
      tooltipValueGetter: (params) => (params.value == null ? '' : String(params.value))
    }));
  }

  limpiarFiltros(): void {
    this.filtroControl.setValue('');
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
          .getReturns({ page: 1, pageSize: 50 })
          .pipe(finalize(() => (this.loading = false)))
          .subscribe({
            next: (items) => (this.rows = items.map((x) => this.mapTrace(x))),
            error: () => (this.error = 'No fue posible consultar devoluciones.')
          });
        return;
      case 'trazabilidad':
        this.api
          .getOperationalTraceability(1, 50)
          .pipe(finalize(() => (this.loading = false)))
          .subscribe({
            next: (response) => (this.rows = (response.items ?? []).map((x) => this.mapTrace(x))),
            error: () => (this.error = 'No fue posible consultar trazabilidad operativa.')
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
      'Tipo causal': row.causalKind || '-',
      Decisión: row.decisionType || '-',
      Causal: `${row.causalCode} ${row.causalDescription}`.trim(),
      'Fecha valor': row.effectiveEntryDate,
      Lote: row.batchSequenceNumber ? `${row.batchSequenceNumber} (#${row.batchId ?? '-'})` : '-',
      Archivo: row.sourceFileReference || '-',
      Monto: this.formatAmount(row.amount)
    };
  }

  private mapNet(row: CenitNetPositionRow): Record<string, string> {
    return {
      Entidad: row.financialInstitutionName || String(row.financialInstitutionId ?? '-'),
      Posición: this.formatAmount(row.netAmount ?? 0),
      Liquidez: this.formatAmount(row.availableLiquidity ?? 0),
      Tipo: row.liquiditySourceType || '-'
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
    const map: Record<OperationView, { titulo: string; subtitulo: string; mensaje: string }> = {
      ciclos: {
        titulo: 'Ciclos del día',
        subtitulo: 'Monitoreo de ejecución, ventanas de cámara y volumen operativo.',
        mensaje: 'Supervise el avance del día operacional y detecte desbalances de forma temprana.'
      },
      cola: {
        titulo: 'Cola y transacciones diferidas',
        subtitulo: 'Visibilidad de pendientes por causal, estado y ciclo destino.',
        mensaje: 'Priorice transacciones en riesgo y reduzca acumulación operacional.'
      },
      neteo: {
        titulo: 'Posiciones netas por entidad',
        subtitulo: 'Consolidado de posición y liquidez para compensación.',
        mensaje: 'Identifique presión de liquidez y soporte decisiones de contingencia.'
      },
      optimizacion: {
        titulo: 'Decisiones de optimización',
        subtitulo: 'Trazabilidad de reglas de liquidez, prioridad y diferimiento.',
        mensaje: 'Analice por qué una transacción fue aprobada, diferida o rechazada.'
      },
      devoluciones: {
        titulo: 'Devoluciones operativas',
        subtitulo: 'Consulta de causales, ciclo y estado para gestión diaria.',
        mensaje: 'Detecte patrones de devolución y reduzca reprocesos.'
      },
      trazabilidad: {
        titulo: 'Trazabilidad operativa CENIT/ACH',
        subtitulo: 'Vista integral de causal, decisión, lote, archivo, cámara y fecha valor.',
        mensaje: 'Evidencia detallada para auditoría operativa y regulatoria.'
      }
    };

    this.titulo = map[this.view].titulo;
    this.subtitulo = map[this.view].subtitulo;
    this.mensajeOperacion = map[this.view].mensaje;
  }
}
