import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { CenitOperationsApiService } from '../services/cenit-operations-api.service';
import { CenitChamberResponseRow, CenitNetPositionRow, CenitOptimizationDecisionRow, CenitQueueRow, CenitTraceabilityRow } from '../models/cenit.models';
import { CycleReportRow } from '../../reports/services/reports-api.service';
import { SharedModule } from '../../../shared/shared.module';
import { ColDef } from 'ag-grid-community';

type OperationView = 'ciclos' | 'cola' | 'neteo' | 'optimizacion' | 'devoluciones' | 'trazabilidad' | 'respuestas-camara';

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
  private readonly cdr = inject(ChangeDetectorRef);

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
  columnasTabla: ColDef<Record<string, string>>[] = [];

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
    const etiqueta = this.view === 'trazabilidad' || this.view === 'respuestas-camara' ? 'eventos' : 'registros';

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

  get mensajeVacio(): string {
    const map: Record<OperationView, string> = {
      ciclos: 'No hay ciclos CENIT para los filtros aplicados.',
      cola: 'No hay transacciones en cola CENIT para los filtros aplicados.',
      neteo: 'No hay posiciones netas CENIT registradas para la ejecución consultada.',
      optimizacion: 'No hay decisiones de optimización CENIT registradas para los filtros aplicados.',
      devoluciones: 'No hay devoluciones operativas CENIT para los filtros aplicados.',
      trazabilidad: 'No hay eventos de trazabilidad CENIT/ACH para los filtros aplicados.',
      'respuestas-camara': 'No hay archivos ordinarios pendientes ni respuestas de cámara CENIT registradas.'
    };

    return map[this.view];
  }

  limpiarFiltros(): void {
    this.filtroControl.setValue('');
    this.cdr.markForCheck();
  }

  private load(): void {
    this.loading = true;
    this.error = '';
    this.cdr.markForCheck();

    const done = () => {
      this.loading = false;
      this.cdr.markForCheck();
    };

    switch (this.view) {
      case 'ciclos':
        this.api
          .getCycles({ page: 1, pageSize: 50 })
          .pipe(finalize(done))
          .subscribe({
            next: (items) => this.setRows(items.map((x) => this.mapCycle(x))),
            error: () => this.setError('No fue posible consultar ciclos operativos.')
          });
        return;
      case 'cola':
        this.api
          .getQueueTransactions('', 1, 50)
          .pipe(finalize(done))
          .subscribe({
            next: (response) => this.setRows((response.items ?? []).map((x) => this.mapQueue(x))),
            error: () => this.setError('No fue posible consultar la cola operativa.')
          });
        return;
      case 'neteo':
        this.api
          .getNetPositions()
          .pipe(finalize(done))
          .subscribe({
            next: (response) => this.setRows((response.items ?? []).map((x) => this.mapNet(x))),
            error: () => this.setError('No fue posible consultar posiciones netas.')
          });
        return;
      case 'optimizacion':
        this.api
          .getOptimizationDecisions()
          .pipe(finalize(done))
          .subscribe({
            next: (response) => this.setRows((response.items ?? []).map((x) => this.mapOptimization(x))),
            error: () => this.setError('No fue posible consultar decisiones de optimización.')
          });
        return;
      case 'devoluciones':
        this.api
          .getReturns({ page: 1, pageSize: 50 })
          .pipe(finalize(done))
          .subscribe({
            next: (items) => this.setRows(items.map((x) => this.mapTrace(x))),
            error: () => this.setError('No fue posible consultar devoluciones.')
          });
        return;
      case 'trazabilidad':
        this.api
          .getOperationalTraceability(1, 50)
          .pipe(finalize(done))
          .subscribe({
            next: (response) => this.setRows((response.items ?? []).map((x) => this.mapTrace(x))),
            error: () => this.setError('No fue posible consultar trazabilidad operativa.')
          });
        return;
      case 'respuestas-camara':
        this.api
          .getChamberResponses(1, 50)
          .pipe(finalize(done))
          .subscribe({
            next: (response) => this.setRows((response.items ?? []).map((x) => this.mapChamberResponse(x))),
            error: () => this.setError('No fue posible consultar las respuestas de cámara CENIT.')
          });
        return;
    }
  }

  private setRows(rows: Array<Record<string, string>>): void {
    this.rows = rows;
    this.columnasTabla = this.buildColumns(rows);
    this.cdr.markForCheck();
  }

  private setError(message: string): void {
    this.error = message;
    this.setRows([]);
  }

  private buildColumns(rows: Array<Record<string, string>>): ColDef<Record<string, string>>[] {
    const headers = rows[0] ? Object.keys(rows[0]) : [];
    return headers.map((header) => ({
      field: header,
      headerName: header,
      sortable: true,
      filter: 'agTextColumnFilter',
      cellClass: header === 'Estado cámara'
        ? (params) => `cenit-chamber-state cenit-chamber-state--${String(params.value ?? '').toLowerCase().replace(/\s+/g, '-')}`
        : undefined,
      tooltipValueGetter: (params) => (params.value == null ? '' : String(params.value))
    }));
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
      'Posición neta': this.formatAmount(row.netAmount ?? 0),
      'Liquidez simulada': this.formatAmount(row.availableLiquidity ?? 0),
      'Fuente de liquidez (referencia)': this.mapLiquiditySourceType(row.liquiditySourceType)
    };
  }

  private mapQueue(row: CenitQueueRow): Record<string, string> {
    return {
      Id: String(row.id),
      Estado: row.status,
      Motivo: row.queueReason,
      'Encolado': row.enqueuedAtUtc,
      'Desencolado': row.dequeuedAtUtc ?? '-',
      'Ciclo destino': row.targetCycleName,
      'ID ciclo destino': row.targetAchCycleId,
      'Ciclo origen': row.originalAchCycleId ?? '-',
      'Transacción': String(row.transactionId),
      'Id externo': row.transactionExternalId ?? '-',
      Referencia: row.reference ?? '-',
      Monto: this.formatAmount(row.amount),
      Tipo: row.transactionType,
      'Estado transacción': row.transactionState,
      'Fecha valor': row.effectiveEntryDate,
      'Ejecución CENIT': row.cenitCycleExecutionId?.toString() ?? '-'
    };
  }

  private mapOptimization(row: CenitOptimizationDecisionRow): Record<string, string> {
    return {
      'Transacción ACH': String(row.achTransactionId),
      Decisión: this.mapDecisionType(row.decisionType),
      'Motivo operativo': this.mapDecisionReason(row.decisionReason),
      Prioridad: String(row.priority),
      'Ciclo origen': row.fromCycleId,
      'Ciclo destino': row.toCycleId || '-',
      'Fecha decisión': row.decidedAtUtc
    };
  }

  private mapChamberResponse(row: CenitChamberResponseRow): Record<string, string> {
    return {
      'Estado cámara': this.mapChamberState(row.state),
      'Tipo respuesta': this.mapChamberType(row.responseType),
      Correlación: this.mapCorrelation(row.correlationOutcome, row.problemCode),
      'Respuesta origen': row.sourceResponseId || '-',
      'Archivo respuesta': row.sourceFileName || '-',
      Archivo: row.relatedFileName || '-',
      Ciclo: row.achCycleId || '-',
      'Grupo XML': row.messageGroupId || '-',
      'Estado XML': row.messageStatus || '-',
      'Referencia relacionada': row.relatedReference || '-',
      'Ítem respuesta': `${row.itemSequence || 1}/${row.itemCount || 1}`,
      Transacción: row.relatedTransactionId?.toString() || '-',
      Traza: row.transactionTraceNumber || '-',
      Código: row.reasonCode || '-',
      Descripción: row.description || '-',
      Recibida: row.receivedAtUtc,
      Procesada: row.processedAtUtc || '-'
    };
  }

  private mapChamberState(value: CenitChamberResponseRow['state']): string {
    return ({
      Pending: 'Pendiente',
      Accepted: 'ACK aceptado',
      Rejected: 'NACK rechazado',
      OperatorRejected: 'Rechazo definitivo del operador',
      Reconciliation: 'Reconciliación',
      NoActivity: 'Sin actividad'
    } as const)[value];
  }

  private mapChamberType(value: CenitChamberResponseRow['responseType']): string {
    return ({
      Unknown: 'Pendiente / no reconocido',
      Ack: 'ACK',
      Nack: 'NACK',
      OperatorRejected: 'Rechazo del operador',
      Reconciliation: 'Reconciliación',
      NoActivity: 'Sin actividad'
    } as const)[value];
  }

  private mapCorrelation(value: CenitChamberResponseRow['correlationOutcome'], problemCode?: string | null): string {
    if (problemCode) return `${value} · ${problemCode}`;
    return ({
      Pending: 'Pendiente',
      Matched: 'Correlacionada',
      NotFound: 'Archivo no encontrado',
      Ambiguous: 'Correlación ambigua',
      TransactionNotFound: 'Transacción no encontrada',
      TransactionAmbiguous: 'Transacción ambigua',
      Invalid: 'Respuesta no reconocida',
      InvalidTransition: 'Transición incompatible'
    } as const)[value];
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
        subtitulo: 'Consolidado de posición neta y liquidez operativa para compensación.',
        mensaje: 'Liquidez simulada para evaluación interna. No representa saldo real CUD ni liquidación firme.'
      },
      optimizacion: {
        titulo: 'Decisiones de optimización',
        subtitulo: 'Trazabilidad de reglas de liquidez, prioridad y diferimiento.',
        mensaje: 'Analice decisiones internas de liquidez. DXX-LIQ es causal interna y no representa rechazo oficial CUD.'
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
      },
      'respuestas-camara': {
        titulo: 'Respuestas de cámara CENIT',
        subtitulo: 'Estado de ACK, NACK, rechazo del operador y salidas al cierre de sesión.',
        mensaje: 'Los rechazos definitivos bloquean la retransmisión; las correlaciones no resueltas requieren revisión operativa.'
      }
    };

    this.titulo = map[this.view].titulo;
    this.subtitulo = map[this.view].subtitulo;
    this.mensajeOperacion = map[this.view].mensaje;
  }

  private mapLiquiditySourceType(value?: string): string {
    if (!value) return '-';
    const lower = value.toLowerCase();
    if (lower.includes('simulated')) return 'Liquidez simulada (no equivale a saldo real CUD)';
    if (lower.includes('external')) return 'Liquidez externa reportada (referencia operacional)';
    return value;
  }

  private mapDecisionType(value?: string): string {
    if (!value) return '-';
    const lower = value.toLowerCase();
    if (lower.includes('processed')) return 'Procesado internamente';
    if (lower.includes('deferred')) return 'Diferido por liquidez';
    if (lower.includes('rejected')) return 'Rechazado internamente por liquidez';
    return value;
  }

  private mapDecisionReason(value?: string): string {
    if (!value) return '-';
    if (value.toUpperCase().includes('DXX-LIQ')) return 'Causal interna DXX-LIQ (no representa rechazo oficial CUD)';
    return value;
  }
}
