import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ColDef } from 'ag-grid-community';
import { finalize } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import {
  IncomingNachaClearingCycleKpi,
  IncomingNachaKpiCount,
  IncomingNachaObservabilitySummary,
  IncomingNachaTimelinePoint,
  IncomingNachaTopError
} from '../models/incoming-nacha-command-center.models';
import { IncomingNachaCommandCenterApiService } from '../services/incoming-nacha-command-center-api.service';

@Component({
  selector: 'app-incoming-nacha-observability-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, SharedModule],
  templateUrl: './incoming-nacha-observability-page.component.html',
  styleUrls: ['./incoming-nacha-observability-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class IncomingNachaObservabilityPageComponent implements OnInit {
  private readonly api = inject(IncomingNachaCommandCenterApiService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  cargando = false;
  error = '';
  summary?: IncomingNachaObservabilitySummary;

  readonly filtrosForm = this.fb.group({
    windowHours: [24]
  });

  readonly columnasEstatus: ColDef<IncomingNachaKpiCount>[] = [
    { field: 'key', headerName: 'Estado', minWidth: 170 },
    { field: 'count', headerName: 'Cantidad', minWidth: 120 }
  ];

  readonly columnasErrores: ColDef<IncomingNachaTopError>[] = [
    { field: 'errorCode', headerName: 'Código error', minWidth: 180 },
    { field: 'count', headerName: 'Frecuencia', minWidth: 120 },
    { headerName: 'Último visto', minWidth: 180, valueGetter: (p) => this.formatDate(p.data?.lastSeenAtUtc) }
  ];

  readonly columnasCiclo: ColDef<IncomingNachaClearingCycleKpi>[] = [
    { field: 'clearingHouseId', headerName: 'Cámara', minWidth: 100 },
    { field: 'achCycleId', headerName: 'Ciclo', minWidth: 130 },
    { field: 'totalItems', headerName: 'Total', minWidth: 100 },
    { field: 'blockedItems', headerName: 'Blocked', minWidth: 100 },
    { field: 'retryPendingItems', headerName: 'RetryPending', minWidth: 130 },
    { field: 'waitingWindowItems', headerName: 'WaitingWindow', minWidth: 130 },
    { field: 'failedFinalItems', headerName: 'FailedFinal', minWidth: 120 },
    { field: 'confirmedItems', headerName: 'Confirmed', minWidth: 110 }
  ];

  readonly columnasTimeline: ColDef<IncomingNachaTimelinePoint>[] = [
    { headerName: 'Hora UTC', minWidth: 170, valueGetter: (p) => this.formatDate(p.data?.bucketAtUtc) },
    { field: 'totalEvents', headerName: 'Eventos', minWidth: 100 },
    { field: 'manualApplied', headerName: 'Applied', minWidth: 100 },
    { field: 'manualRejected', headerName: 'Rejected', minWidth: 100 },
    { field: 'retryPendingTransitions', headerName: 'RetryPending', minWidth: 130 },
    { field: 'failedFinalTransitions', headerName: 'FailedFinal', minWidth: 120 }
  ];

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando = true;
    this.error = '';
    const windowHours = Math.max(1, Math.min(168, this.filtrosForm.controls.windowHours.value ?? 24));

    this.api.getObservabilitySummary(windowHours).pipe(finalize(() => {
      this.cargando = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: (summary) => {
        this.summary = summary;
      },
      error: (err) => {
        this.error = err?.error?.message ?? 'No fue posible cargar observabilidad inbound.';
      }
    });
  }

  irACola(): void {
    this.router.navigate(['/incoming-nacha-command-center/queue']);
  }

  irAIngestas(): void {
    this.router.navigate(['/incoming-nacha-command-center']);
  }

  formatNumber(value?: number): string {
    if (value === undefined || value === null) {
      return '0';
    }

    return new Intl.NumberFormat('es-CO', { maximumFractionDigits: 2 }).format(value);
  }

  alertaClass(value: number, threshold: number): string {
    return value >= threshold ? 'kpi-alerta' : '';
  }

  private formatDate(value?: string | null): string {
    return value ? new Date(value).toLocaleString('es-CO') : '—';
  }
}
