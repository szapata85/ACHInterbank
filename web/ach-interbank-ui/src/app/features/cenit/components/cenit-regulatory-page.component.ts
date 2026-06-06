import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { CenitRegulatoryApiService } from '../services/cenit-regulatory-api.service';
import {
  CenitFileRejectionCode,
  CenitPrenotificationPolicy,
  CenitReturnCode,
  CenitReturnOfReturnPolicy,
  CenitReturnPolicy,
  CenitTransactionTypePolicy
} from '../models/cenit.models';
import { SharedModule } from '../../../shared/shared.module';
import { ColDef } from 'ag-grid-community';

type RegulatoryView =
  | 'causales-devolucion'
  | 'causales-rechazo'
  | 'politicas-transaccion'
  | 'politicas-devolucion'
  | 'politicas-prenotificacion';

@Component({
  selector: 'app-cenit-regulatory-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, SharedModule],
  templateUrl: './cenit-regulatory-page.component.html',
  styleUrls: ['./cenit-regulatory-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CenitRegulatoryPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(CenitRegulatoryApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  loading = false;
  error = '';
  mensajeRegulatorio = '';

  readonly filtroControl = new FormControl<string>('', { nonNullable: true });

  readonly migas = [
    { etiqueta: 'Inicio', ruta: '/' },
    { etiqueta: 'CENIT', ruta: '/cenit' },
    { etiqueta: 'Regulatorio' }
  ];

  view: RegulatoryView = 'causales-devolucion';
  titulo = '';
  subtitulo = '';

  rows: Array<Record<string, string>> = [];
  columnasTabla: ColDef<Record<string, string>>[] = [];

  ngOnInit(): void {
    this.view = (this.route.snapshot.data['view'] as RegulatoryView) ?? 'causales-devolucion';
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

  get indicadoresRegulatorios(): Array<{ etiqueta: string; valor: string; estado: 'activo' | 'pendiente' | 'exitoso' }> {
    return [
      { etiqueta: 'Panel', valor: this.titulo, estado: 'activo' },
      { etiqueta: 'Registros visibles', valor: this.filteredRows.length.toLocaleString('es-CO'), estado: this.filteredRows.length ? 'exitoso' : 'pendiente' },
      { etiqueta: 'Estado de consulta', valor: this.error ? 'Con novedad' : 'En línea', estado: this.error ? 'pendiente' : 'activo' }
    ];
  }

  get mensajeVacio(): string {
    const map: Record<RegulatoryView, string> = {
      'causales-devolucion': 'No hay causales de devolución CENIT disponibles para los filtros aplicados.',
      'causales-rechazo': 'No hay causales de rechazo CENIT disponibles para los filtros aplicados.',
      'politicas-transaccion': 'No hay políticas de transacción CENIT disponibles para los filtros aplicados.',
      'politicas-devolucion': 'No hay políticas de devolución CENIT disponibles para los filtros aplicados.',
      'politicas-prenotificacion': 'No hay políticas de prenotificación CENIT disponibles para los filtros aplicados.'
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
      case 'causales-rechazo':
        this.api
          .getFileRejectionCodes()
          .pipe(finalize(done))
          .subscribe({
            next: (items) => this.setRows(items.map((item) => this.mapRejectionRow(item))),
            error: () => this.setError('No fue posible consultar causales de rechazo.')
          });
        return;
      case 'politicas-transaccion':
        this.api
          .getTransactionTypePolicies()
          .pipe(finalize(done))
          .subscribe({
            next: (items) => this.setRows(items.map((item) => this.mapTxPolicyRow(item))),
            error: () => this.setError('No fue posible consultar políticas de transacción.')
          });
        return;
      case 'politicas-devolucion':
        this.api
          .getReturnPolicies()
          .pipe(finalize(done))
          .subscribe({
            next: (items) => {
              this.setRows(items.map((item) => this.mapReturnPolicyRow(item)));
              this.loadReturnOfReturnPolicies();
            },
            error: () => this.setError('No fue posible consultar políticas de devolución.')
          });
        return;
      case 'politicas-prenotificacion':
        this.api
          .getPrenotificationPolicies()
          .pipe(finalize(done))
          .subscribe({
            next: (items) => this.setRows(items.map((item) => this.mapPrenotePolicyRow(item))),
            error: () => this.setError('No fue posible consultar políticas de prenotificación.')
          });
        return;
      default:
        this.api
          .getReturnCodes()
          .pipe(finalize(done))
          .subscribe({
            next: (items) => this.setRows(items.map((item) => this.mapReturnCodeRow(item))),
            error: () => this.setError('No fue posible consultar causales de devolución.')
          });
    }
  }

  private loadReturnOfReturnPolicies(): void {
    this.api.getReturnOfReturnPolicies().subscribe({
      next: (items) => {
        const mapped = items.map((item) => this.mapReturnOfReturnPolicyRow(item));
        this.setRows([...this.rows, ...mapped]);
      }
    });
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
      tooltipValueGetter: (params) => (params.value == null ? '' : String(params.value))
    }));
  }

  private mapReturnCodeRow(row: CenitReturnCode): Record<string, string> {
    return {
      Código: row.code,
      Descripción: row.description,
      'Aplica débito': row.appliesToDebit ? 'Sí' : 'No',
      'Aplica crédito': row.appliesToCredit ? 'Sí' : 'No',
      'Aplica prenotificación': row.appliesToPrenotification ? 'Sí' : 'No',
      'Aplica retorno': row.appliesToReturn ? 'Sí' : 'No',
      'Días máximos': row.maxDaysAllowed?.toString() ?? '-',
      'Requiere addenda': row.requiresAddenda ? 'Sí' : 'No',
      Estado: row.isActive ? 'Activo' : 'Inactivo'
    };
  }

  private mapRejectionRow(row: CenitFileRejectionCode): Record<string, string> {
    return {
      Código: row.code,
      Descripción: row.description,
      Severidad: row.severity,
      Etapa: row.appliesToStage,
      Reintento: row.isRetryable ? 'Sí' : 'No',
      Estado: row.isActive ? 'Activo' : 'Inactivo'
    };
  }

  private mapTxPolicyRow(row: CenitTransactionTypePolicy): Record<string, string> {
    return {
      Tipo: row.transactionType,
      Prioridad: String(row.priorityOrder),
      Monetaria: row.isMonetary ? 'Sí' : 'No',
      'Requiere prenotificación': row.requiresPrenotification ? 'Sí' : 'No',
      'Permite devolución': row.canBeReturned ? 'Sí' : 'No',
      'Permite devolución de devolución': row.canBeReturnedAgain ? 'Sí' : 'No',
      Estado: row.isActive ? 'Activo' : 'Inactivo'
    };
  }

  private mapReturnPolicyRow(row: CenitReturnPolicy): Record<string, string> {
    return {
      Tipo: row.transactionType,
      Causales: row.allowedReturnCodesCsv,
      'Días máximos': String(row.maxDays),
      'Estado origen': row.requiredOriginalTransactionState,
      'Permite devolución de devolución': row.allowsReturnOfReturn ? 'Sí' : 'No',
      'Requiere addenda': row.requiresAddenda ? 'Sí' : 'No',
      Estado: row.isActive ? 'Activo' : 'Inactivo'
    };
  }

  private mapReturnOfReturnPolicyRow(row: CenitReturnOfReturnPolicy): Record<string, string> {
    return {
      Tipo: `Devolución de devolución (${row.originalReturnCode})`,
      Causales: row.allowedNewReturnCodesCsv,
      'Días máximos': String(row.maxDays),
      'Estado origen': row.requiredOriginalState,
      'Permite devolución de devolución': 'Sí',
      'Requiere addenda': 'Sí',
      Estado: row.isActive ? 'Activo' : 'Inactivo'
    };
  }

  private mapPrenotePolicyRow(row: CenitPrenotificationPolicy): Record<string, string> {
    return {
      Tipo: row.transactionType,
      Obligatoria: row.isRequired ? 'Sí' : 'No',
      'Requiere addenda': row.requiresAddenda ? 'Sí' : 'No',
      'Bloquea monetaria si falta': row.blocksMonetaryTransactionIfMissing ? 'Sí' : 'No',
      Estado: row.isActive ? 'Activo' : 'Inactivo'
    };
  }

  private resolveHeader(): void {
    const map: Record<RegulatoryView, { titulo: string; subtitulo: string; mensaje: string }> = {
      'causales-devolucion': {
        titulo: 'Causales de devolución (Rxx)',
        subtitulo: 'Consulta de causal, aplicabilidad y vigencia normativa.',
        mensaje: 'Use esta vista para validar cumplimiento regulatorio de devoluciones antes de cierres de ciclo.'
      },
      'causales-rechazo': {
        titulo: 'Causales de rechazo (Dxx)',
        subtitulo: 'Consulta por severidad, etapa y reintento permitido.',
        mensaje: 'Permite identificar rechazos críticos y definir acciones de remediación operacional.'
      },
      'politicas-transaccion': {
        titulo: 'Políticas de tipo de transacción',
        subtitulo: 'Prioridad operativa, naturaleza monetaria y capacidad de devolución.',
        mensaje: 'Alinee la configuración de productos con las reglas vigentes de CENIT.'
      },
      'politicas-devolucion': {
        titulo: 'Políticas de devolución y de devolución de devolución',
        subtitulo: 'Reglas de causal, plazo y estado origen para auditoría regulatoria.',
        mensaje: 'Consolida reglas críticas para evitar devoluciones fuera de política.'
      },
      'politicas-prenotificacion': {
        titulo: 'Políticas de prenotificación',
        subtitulo: 'Reglas de obligatoriedad, addenda y bloqueo operativo.',
        mensaje: 'Controla el cumplimiento previo a transacciones monetarias sensibles.'
      }
    };

    this.titulo = map[this.view].titulo;
    this.subtitulo = map[this.view].subtitulo;
    this.mensajeRegulatorio = map[this.view].mensaje;
  }
}
