import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
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

type RegulatoryView =
  | 'causales-devolucion'
  | 'causales-rechazo'
  | 'politicas-transaccion'
  | 'politicas-devolucion'
  | 'politicas-prenotificacion';

@Component({
  selector: 'app-cenit-regulatory-page',
  templateUrl: './cenit-regulatory-page.component.html',
  styleUrls: ['./cenit-regulatory-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CenitRegulatoryPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(CenitRegulatoryApiService);

  loading = false;
  error = '';
  filtro = '';

  view: RegulatoryView = 'causales-devolucion';
  titulo = '';
  subtitulo = '';

  rows: Array<Record<string, string>> = [];

  ngOnInit(): void {
    this.view = (this.route.snapshot.data['view'] as RegulatoryView) ?? 'causales-devolucion';
    this.resolveHeader();
    this.load();
  }

  get hasRows(): boolean {
    return this.filteredRows.length > 0;
  }

  get filteredRows(): Array<Record<string, string>> {
    const lower = this.filtro.trim().toLowerCase();
    if (!lower) {
      return this.rows;
    }

    return this.rows.filter((row) => Object.values(row).some((value) => value.toLowerCase().includes(lower)));
  }

  get headers(): string[] {
    return this.filteredRows[0] ? Object.keys(this.filteredRows[0]) : [];
  }

  private load(): void {
    this.loading = true;
    this.error = '';

    const done = () => (this.loading = false);

    switch (this.view) {
      case 'causales-rechazo':
        this.api
          .getFileRejectionCodes()
          .pipe(finalize(done))
          .subscribe({
            next: (items) => (this.rows = items.map((item) => this.mapRejectionRow(item))),
            error: () => (this.error = 'No fue posible consultar causales de rechazo.')
          });
        return;
      case 'politicas-transaccion':
        this.api
          .getTransactionTypePolicies()
          .pipe(finalize(done))
          .subscribe({
            next: (items) => (this.rows = items.map((item) => this.mapTxPolicyRow(item))),
            error: () => (this.error = 'No fue posible consultar políticas de transacción.')
          });
        return;
      case 'politicas-devolucion':
        this.api
          .getReturnPolicies()
          .pipe(finalize(done))
          .subscribe({
            next: (items) => {
              this.rows = items.map((item) => this.mapReturnPolicyRow(item));
              this.loadReturnOfReturnPolicies();
            },
            error: () => (this.error = 'No fue posible consultar políticas de devolución.')
          });
        return;
      case 'politicas-prenotificacion':
        this.api
          .getPrenotificationPolicies()
          .pipe(finalize(done))
          .subscribe({
            next: (items) => (this.rows = items.map((item) => this.mapPrenotePolicyRow(item))),
            error: () => (this.error = 'No fue posible consultar políticas de prenotificación.')
          });
        return;
      default:
        this.api
          .getReturnCodes()
          .pipe(finalize(done))
          .subscribe({
            next: (items) => (this.rows = items.map((item) => this.mapReturnCodeRow(item))),
            error: () => (this.error = 'No fue posible consultar causales de devolución.')
          });
    }
  }

  private loadReturnOfReturnPolicies(): void {
    this.api.getReturnOfReturnPolicies().subscribe({
      next: (items) => {
        const mapped = items.map((item) => this.mapReturnOfReturnPolicyRow(item));
        this.rows = [...this.rows, ...mapped];
      }
    });
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
      Tipo: `ReturnOfReturn (${row.originalReturnCode})`,
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
    const map: Record<RegulatoryView, { titulo: string; subtitulo: string }> = {
      'causales-devolucion': {
        titulo: 'Causales de devolución (Rxx)',
        subtitulo: 'Consulta de causal, aplicabilidad y vigencia normativa.'
      },
      'causales-rechazo': {
        titulo: 'Causales de rechazo (Dxx)',
        subtitulo: 'Consulta por severidad, etapa y reintento permitido.'
      },
      'politicas-transaccion': {
        titulo: 'Políticas de tipo de transacción',
        subtitulo: 'Prioridad operativa, naturaleza monetaria y capacidad de devolución.'
      },
      'politicas-devolucion': {
        titulo: 'Políticas de devolución y devolución de devolución',
        subtitulo: 'Reglas de causal, plazo y estado origen para auditoría regulatoria.'
      },
      'politicas-prenotificacion': {
        titulo: 'Políticas de prenotificación',
        subtitulo: 'Reglas de obligatoriedad, addenda y bloqueo operativo.'
      }
    };

    this.titulo = map[this.view].titulo;
    this.subtitulo = map[this.view].subtitulo;
  }
}
