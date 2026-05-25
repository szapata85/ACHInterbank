import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ColDef } from 'ag-grid-community';
import { finalize } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import {
  NachaOperationalAudit,
  NachaOperationalDashboardData,
  NachaOperationalDecision,
  NachaOperationalFile,
  NachaSoapReadiness
} from '../models/nacha-operational.models';
import { NachaOperationalReadinessService } from '../services/nacha-operational-readiness.service';

@Component({
  selector: 'app-nacha-operational-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, SharedModule],
  templateUrl: './nacha-operational-dashboard.component.html',
  styleUrls: ['./nacha-operational-dashboard.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaOperationalDashboardComponent implements OnInit {
  private readonly service = inject(NachaOperationalReadinessService);
  private readonly cdr = inject(ChangeDetectorRef);

  data?: NachaOperationalDashboardData;
  cargando = false;
  error = '';

  readonly columnasArchivos: ColDef<NachaOperationalFile>[] = [
    { field: 'fileName', headerName: 'Archivo', minWidth: 210 },
    { field: 'clearingHouseCode', headerName: 'Camara', minWidth: 110 },
    { field: 'profileCode', headerName: 'Perfil', minWidth: 260 },
    { field: 'flowType', headerName: 'Flujo', minWidth: 220 },
    { field: 'isReturnFile', headerName: '.RET', minWidth: 90 },
    { field: 'validationPassed', headerName: 'Validado', minWidth: 110 },
    { field: 'batchCount', headerName: 'Batches', minWidth: 110 },
    { field: 'entryCount', headerName: 'Entries', minWidth: 110 },
    { field: 'addendaCount', headerName: 'Addenda', minWidth: 110 },
    { field: 'processingStatus', headerName: 'Estado', minWidth: 170 },
    { headerName: 'Recibido', minWidth: 180, valueGetter: (p) => this.formatDate(p.data?.receivedAt) }
  ];

  readonly columnasDecisiones: ColDef<NachaOperationalDecision>[] = [
    { field: 'correlationId', headerName: 'CorrelationId', minWidth: 210 },
    { field: 'decisionType', headerName: 'DecisionType', minWidth: 210 },
    { field: 'soapOperationCandidate', headerName: 'Candidato SOAP', minWidth: 210 },
    { field: 'requiresMonetaryMovement', headerName: 'Movimiento', minWidth: 130 },
    { field: 'reasonCode', headerName: 'Causal', minWidth: 110 },
    { field: 'reasonDescription', headerName: 'Descripcion', minWidth: 260 },
    { field: 'newInternalStatus', headerName: 'Nuevo estado', minWidth: 170 },
    { field: 'manualReviewRequired', headerName: 'Manual review', minWidth: 150 }
  ];

  readonly columnasReadiness: ColDef<NachaSoapReadiness>[] = [
    { field: 'correlationId', headerName: 'CorrelationId', minWidth: 210 },
    { field: 'isReadyForUat', headerName: 'Ready UAT', minWidth: 130 },
    { field: 'isBlocked', headerName: 'Blocked', minWidth: 120 },
    { headerName: 'Bloqueos', minWidth: 320, valueGetter: (p) => p.data?.blockReasons.join(' | ') ?? '' },
    { field: 'operationalGatePassed', headerName: 'Gate', minWidth: 110 },
    { field: 'readinessCheckPassed', headerName: 'Readiness', minWidth: 130 },
    { field: 'simulationPassed', headerName: 'Simulacion', minWidth: 130 },
    { field: 'resiliencePassed', headerName: 'Resiliencia', minWidth: 130 },
    { field: 'productiveExecution', headerName: 'Productivo', minWidth: 130 },
    { field: 'wouldInvokeRealSoap', headerName: 'SOAP real', minWidth: 130 }
  ];

  readonly columnasAuditoria: ColDef<NachaOperationalAudit>[] = [
    { field: 'correlationId', headerName: 'CorrelationId', minWidth: 210 },
    { field: 'phase', headerName: 'Phase', minWidth: 100 },
    { field: 'eventType', headerName: 'Evento', minWidth: 220 },
    { field: 'severity', headerName: 'Severidad', minWidth: 130 },
    { field: 'message', headerName: 'Mensaje', minWidth: 300 },
    { field: 'isBlocked', headerName: 'Blocked', minWidth: 120 },
    { headerName: 'Timestamp', minWidth: 180, valueGetter: (p) => this.formatDate(p.data?.timestamp) },
    { headerName: 'Detalles sanitizados', minWidth: 300, valueGetter: (p) => this.formatDetails(p.data?.sanitizedDetails) }
  ];

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando = true;
    this.error = '';

    this.service.getDashboardData().pipe(finalize(() => {
      this.cargando = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: (data) => {
        this.data = data;
      },
      error: (err) => {
        this.error = err?.message ?? 'No fue posible cargar la consulta operativa NACHA-M.';
      }
    });
  }

  badgeClass(value: string | boolean | undefined): string {
    const normalized = String(value ?? '').toLowerCase();
    if (normalized.includes('no-go') || normalized.includes('blocked') || normalized === 'true') {
      return 'badge-danger';
    }

    if (normalized.includes('ready') || normalized.includes('simulated') || normalized.includes('dryrun')) {
      return 'badge-success';
    }

    if (normalized.includes('manual')) {
      return 'badge-warning';
    }

    return 'badge-neutral';
  }

  formatDate(value?: string | null): string {
    return value ? new Date(value).toLocaleString('es-CO') : '-';
  }

  dataSourceLabel(data: NachaOperationalDashboardData): string {
    if (data.isDemoData || data.summary.isDemoData) {
      return 'Fuente: demo seguro';
    }

    if (data.isPartialData || data.summary.isPartialData) {
      return 'Fuente: parcial';
    }

    return 'Fuente: backend read-only';
  }

  private formatDetails(details?: Record<string, string>): string {
    if (!details) {
      return '';
    }

    return Object.entries(details).map(([key, value]) => `${key}: ${value}`).join(' | ');
  }
}
