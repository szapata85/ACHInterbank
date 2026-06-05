import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ColDef } from 'ag-grid-community';
import { finalize } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import { NachaSoapUatAudit, NachaSoapUatCandidate } from '../models/nacha-operational.models';
import { NachaSoapUatConsoleData, NachaSoapUatConsoleService } from '../services/nacha-soap-uat-console.service';

@Component({
  selector: 'app-nacha-soap-uat-console',
  standalone: true,
  imports: [CommonModule, RouterModule, SharedModule],
  templateUrl: './nacha-soap-uat-console.component.html',
  styleUrls: ['./nacha-operational-dashboard.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaSoapUatConsoleComponent implements OnInit {
  private readonly service = inject(NachaSoapUatConsoleService);
  private readonly cdr = inject(ChangeDetectorRef);

  data?: NachaSoapUatConsoleData;
  cargando = false;
  error = '';
  selectedCandidate?: NachaSoapUatCandidate;

  readonly columnasCandidatos: ColDef<NachaSoapUatCandidate>[] = [
    { field: 'correlationId', headerName: 'CorrelationId', minWidth: 180 },
    { field: 'fileName', headerName: 'Archivo', minWidth: 180 },
    { field: 'entryTraceNumber', headerName: 'Trace', minWidth: 130 },
    { field: 'decisionType', headerName: 'Decision', minWidth: 180 },
    { field: 'operationCandidate', headerName: 'Operacion', minWidth: 190 },
    { field: 'requiresMonetaryMovement', headerName: 'Monetario', minWidth: 120 },
    { field: 'readinessStatus', headerName: 'Preparación', minWidth: 140 },
    { field: 'simulationStatus', headerName: 'Simulación', minWidth: 130 },
    { field: 'resilienceStatus', headerName: 'Resiliencia', minWidth: 130 },
    { field: 'idempotencyStatus', headerName: 'Idempotencia', minWidth: 150 },
    { field: 'attemptCount', headerName: 'Intentos', minWidth: 110 }
  ];

  readonly columnasAuditoria: ColDef<NachaSoapUatAudit>[] = [
    { field: 'correlationId', headerName: 'CorrelationId', minWidth: 190 },
    { field: 'phase', headerName: 'Fase', minWidth: 100 },
    { field: 'eventType', headerName: 'Evento', minWidth: 220 },
    { field: 'severity', headerName: 'Severidad', minWidth: 130 },
    { field: 'message', headerName: 'Mensaje', minWidth: 280 },
    { field: 'isBlocked', headerName: 'Bloqueado', minWidth: 120 },
    { headerName: 'Marca temporal', minWidth: 180, valueGetter: (p) => this.formatDate(p.data?.timestamp) },
    { headerName: 'Detalles sanitizados', minWidth: 300, valueGetter: (p) => this.formatDetails(p.data?.sanitizedDetails) }
  ];

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando = true;
    this.error = '';
    this.service.getConsoleData().pipe(finalize(() => {
      this.cargando = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: (data) => {
        this.data = data;
        this.selectedCandidate = data.candidates[0];
      },
      error: (err) => {
        this.error = err?.message ?? 'No fue posible cargar la consola SOAP/UAT.';
      }
    });
  }

  selectCandidate(candidate: NachaSoapUatCandidate): void {
    this.selectedCandidate = candidate;
  }

  badgeClass(value: string | boolean | undefined): string {
    const normalized = String(value ?? '').toLowerCase();
    if (normalized.includes('blocked') || normalized.includes('no-go') || normalized.includes('failed') || normalized === 'true') {
      return 'badge-danger';
    }
    if (normalized.includes('ready') || normalized.includes('passed') || normalized.includes('simulated')) {
      return 'badge-success';
    }
    if (normalized.includes('manual') || normalized.includes('warning') || normalized.includes('duplicate')) {
      return 'badge-warning';
    }
    return 'badge-neutral';
  }

  formatDate(value?: string | null): string {
    return value ? new Date(value).toLocaleString('es-CO') : '-';
  }

  private formatDetails(details?: Record<string, string>): string {
    if (!details) {
      return '';
    }

    return Object.entries(details).map(([key, value]) => `${key}: ${value}`).join(' | ');
  }
}
