import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ColDef } from 'ag-grid-community';
import { finalize } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import { AchReconciliationDetail, AchReconciliationItem } from '../models/nacha-operational.models';
import { AchReconciliationConsoleData, AchReconciliationService } from '../services/ach-reconciliation.service';

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

  readonly columnas: ColDef<AchReconciliationItem>[] = [
    { field: 'reconciliationId', headerName: 'ID', minWidth: 170 },
    { field: 'correlationId', headerName: 'CorrelationId', minWidth: 180 },
    { field: 'fileName', headerName: 'Archivo', minWidth: 180 },
    { field: 'clearingHouseCode', headerName: 'Camara', minWidth: 110 },
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

  badgeClass(value: string | boolean | undefined): string {
    const normalized = String(value ?? '').toLowerCase();
    if (normalized.includes('inconsistente') || normalized.includes('manual') || normalized === 'true') return 'badge-danger';
    if (normalized.includes('conciliado') || normalized.includes('no monetario')) return 'badge-success';
    if (normalized.includes('pendiente') || normalized.includes('candidate')) return 'badge-warning';
    return 'badge-neutral';
  }

  keys(value?: Record<string, unknown> | null): string[] {
    return value ? Object.keys(value) : [];
  }
}
