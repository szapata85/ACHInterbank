import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ColDef } from 'ag-grid-community';
import { finalize } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import { AchReconciliationDetail, AchReconciliationItem } from '../models/nacha-operational.models';
import { AchReconciliationConsoleData, AchReconciliationException, AchReconciliationService } from '../services/ach-reconciliation.service';

@Component({
  selector: 'app-ach-reconciliation-console',
  standalone: true,
  imports: [CommonModule, FormsModule, SharedModule],
  templateUrl: './ach-reconciliation-console.component.html',
  styleUrls: ['./nacha-operational-dashboard.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchReconciliationConsoleComponent implements OnInit {
  private readonly service = inject(AchReconciliationService);
  private readonly cdr = inject(ChangeDetectorRef);

  data?: AchReconciliationConsoleData;
  detail?: AchReconciliationDetail;
  cargando = false;
  error = '';
  estado = '';
  camara = '';
  tipo = '';
  soloRevision = false;
  selectedException?: AchReconciliationException;
  resolution = '';
  resolutionReason = '';
  resolving = false;

  readonly columnas: ColDef<AchReconciliationItem>[] = [
    { field: 'reconciliationId', headerName: 'ID', minWidth: 170 },
    { field: 'correlationId', headerName: 'CorrelationId', minWidth: 180 },
    { field: 'fileName', headerName: 'Archivo', minWidth: 180 },
    { field: 'clearingHouseCode', headerName: 'Cámara', minWidth: 110 },
    { field: 'flowType', headerName: 'Flujo', minWidth: 170 },
    { field: 'responseType', headerName: 'Respuesta', minWidth: 180 },
    { field: 'reasonCode', headerName: 'Causal', minWidth: 110 },
    { field: 'traceNumberMasked', headerName: 'Trace', minWidth: 130 },
    { field: 'internalStatus', headerName: 'Estado interno', minWidth: 170 },
    { field: 'reconciliationStatus', headerName: 'Conciliación', minWidth: 150 },
    { field: 'soapOperationCandidate', headerName: 'SOAP candidato', minWidth: 190 }
  ];

  ngOnInit(): void {
    this.cargar();
  }

  get filteredItems(): AchReconciliationItem[] {
    const items = this.data?.items ?? [];
    return items.filter((item) =>
      (!this.estado || item.reconciliationStatus === this.estado)
      && (!this.camara || item.clearingHouseCode === this.camara)
      && (!this.tipo || item.responseType === this.tipo || item.flowType === this.tipo)
      && (!this.soloRevision || item.requiresManualReview)
    );
  }

  get clearingHouseOptions(): string[] {
    return [...new Set((this.data?.items ?? []).map((item) => item.clearingHouseCode).filter(Boolean))]
      .sort((left, right) => left.localeCompare(right));
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
        if (data.items[0]) {
          this.verDetalle(data.items[0]);
        }
      },
      error: (err) => {
        this.error = err?.message ?? 'No fue posible cargar conciliación ACH.';
      }
    });
  }

  verDetalle(item: AchReconciliationItem): void {
    this.service.getItem(item.reconciliationId).subscribe({
      next: (detail) => {
        this.detail = detail;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.error = err?.message ?? 'No fue posible cargar el detalle.';
        this.cdr.markForCheck();
      }
    });
  }

  selectException(item: AchReconciliationException): void {
    this.selectedException = item;
    this.resolution = '';
    this.resolutionReason = '';
  }

  resolveSelectedException(): void {
    const item = this.selectedException;
    if (!item || this.resolution.trim().length < 3 || this.resolutionReason.trim().length < 5) {
      this.error = 'La resolución y la justificación son obligatorias.';
      return;
    }
    this.resolving = true;
    this.error = '';
    this.service.resolveException(item.id, item.version, this.resolution.trim(), this.resolutionReason.trim())
      .pipe(finalize(() => { this.resolving = false; this.cdr.markForCheck(); }))
      .subscribe({
        next: (updated) => {
          const index = this.data?.exceptions.findIndex(x => x.id === updated.id) ?? -1;
          if (this.data && index >= 0) this.data.exceptions[index] = updated;
          this.selectedException = updated;
        },
        error: (err) => {
          this.error = err?.status === 409
            ? 'La excepción cambió; recargue la versión vigente.'
            : err?.message ?? 'No fue posible resolver la excepción.';
        }
      });
  }

  badgeClass(value: string | boolean | undefined): string {
    const normalized = String(value ?? '').toLowerCase();
    if (normalized.includes('inconsistente') || normalized.includes('manual') || normalized === 'true') return 'badge-danger';
    if (normalized.includes('conciliado') || normalized.includes('no monetario')) return 'badge-success';
    if (normalized.includes('pendiente') || normalized.includes('candidate')) return 'badge-warning';
    return 'badge-neutral';
  }

  dataSourceLabel(value?: string | null): string {
    const normalized = String(value ?? '').toLowerCase();
    if (normalized.includes('demo')) {
      return 'demo seguro';
    }
    if (normalized.includes('parcial')) {
      return 'parcial';
    }
    return 'backend solo lectura';
  }

  keys(value?: Record<string, unknown> | null): string[] {
    return value ? Object.keys(value) : [];
  }
}
