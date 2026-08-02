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
import { supportStatusLabel } from '../presentation/incoming-nacha-support-presentation';

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
    { headerName: 'Estado', minWidth: 190, valueGetter: (p) => supportStatusLabel(p.data?.key) },
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
    { field: 'blockedItems', headerName: 'Bloqueados', minWidth: 110 },
    { field: 'retryPendingItems', headerName: 'Pendientes de reintento', minWidth: 180 },
    { field: 'waitingWindowItems', headerName: 'En espera de ventana', minWidth: 170 },
    { field: 'failedFinalItems', headerName: 'Errores definitivos', minWidth: 160 },
    { field: 'confirmedItems', headerName: 'Procesados', minWidth: 120 }
  ];

  readonly columnasTimeline: ColDef<IncomingNachaTimelinePoint>[] = [
    { headerName: 'Hora UTC', minWidth: 170, valueGetter: (p) => this.formatDate(p.data?.bucketAtUtc) },
    { field: 'totalEvents', headerName: 'Eventos', minWidth: 100 },
    { field: 'manualApplied', headerName: 'Acciones aplicadas', minWidth: 150 },
    { field: 'manualRejected', headerName: 'Acciones no aplicadas', minWidth: 170 },
    { field: 'retryPendingTransitions', headerName: 'Pendientes de reintento', minWidth: 180 },
    { field: 'failedFinalTransitions', headerName: 'Errores definitivos', minWidth: 160 }
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
        this.error = err?.error?.message ?? 'No fue posible consultar los indicadores operativos.';
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
