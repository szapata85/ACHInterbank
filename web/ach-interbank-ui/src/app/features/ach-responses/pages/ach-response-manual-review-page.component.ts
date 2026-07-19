import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ColDef } from 'ag-grid-community';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { AchResponseListItemResponse, AchResponseSearchRequest } from '../models/ach-responses.models';
import { AchResponsesApiService } from '../services/ach-responses-api.service';
import { formatAchDate, formatAchValue, normalizeAchFilter } from '../utils/ach-response-formatters';
import { createAchBadgeElement, createAchButtonElement } from '../utils/ach-response-renderers';
import { getAchManualReviewPriority, getAchPriorityClass, getAchProcessingStatusClass } from '../utils/ach-response-status.utils';

type AchManualReviewRow = AchResponseListItemResponse & {
  fechaRecepcionText: string;
  fechaCreacionText: string;
  estadoCriticoText: string;
  prioridadText: string;
  permiteNotificacionText: string;
};

@Component({
  selector: 'app-ach-response-manual-review-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, SharedModule],
  templateUrl: './ach-response-manual-review-page.component.html',
  styleUrls: ['./ach-response-manual-review-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchResponseManualReviewPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly api = inject(AchResponsesApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly criticalStates = ['NoHomologada', 'RequiereRevisionManual', 'ErrorFuncional', 'PendienteReintento'];
  readonly tiposRespuesta: Array<'' | 'Prenota' | 'Transaccion'> = ['', 'Prenota', 'Transaccion'];

  readonly filtrosForm = this.fb.group({
    fechaDesde: [''],
    fechaHasta: [''],
    estadoProcesamiento: ['NoHomologada'],
    tipoRespuesta: [''],
    idTransaccion: [''],
    codigoCamaraCompensacion: [''],
    codigoEntidadOrigen: [''],
    codigoEntidadDestino: [''],
    pageNumber: [1],
    pageSize: [20]
  });

  readonly columnas: ColDef<AchManualReviewRow>[] = [
    {
      field: 'prioridadText',
      headerName: 'Prioridad',
      minWidth: 120,
      cellRenderer: (params: any) => createAchBadgeElement(this.formatValue(params.value), this.getPriorityClass(params.value))
    },
    { field: 'fechaRecepcionText', headerName: 'Fecha recepción', minWidth: 170 },
    { field: 'fechaCreacionText', headerName: 'Fecha creación', minWidth: 170 },
    { field: 'tipoRespuesta', headerName: 'Tipo', minWidth: 120 },
    { field: 'idTransaccion', headerName: 'Transacción', minWidth: 180 },
    { field: 'codigoCamaraCompensacion', headerName: 'Cámara', minWidth: 120 },
    { field: 'codigoEntidadOrigen', headerName: 'Entidad origen', minWidth: 140 },
    { field: 'codigoEntidadDestino', headerName: 'Entidad destino', minWidth: 140 },
    { field: 'codigoEstadoExterno', headerName: 'Estado externo', minWidth: 130 },
    { field: 'codigoCausalExterna', headerName: 'Causal externa', minWidth: 140 },
    { field: 'estadoInternoNombre', headerName: 'Estado interno', minWidth: 140 },
    {
      field: 'estadoProcesamiento',
      headerName: 'Estado crítico',
      minWidth: 170,
      cellRenderer: (params: any) => createAchBadgeElement(this.formatProcessingStatus(params.value), this.getProcessingStatusClass(params.value))
    },
    { field: 'permiteNotificacionText', headerName: 'Notificable', minWidth: 120 },
    { field: 'correlationId', headerName: 'CorrelationId', minWidth: 180 },
    {
      headerName: 'Acciones',
      minWidth: 130,
      sortable: false,
      filter: false,
      cellRenderer: () => createAchButtonElement('Ver detalle', 'detalle'),
      onCellClicked: (params) => {
        const action = (params.event?.target as HTMLElement | null)?.getAttribute('data-action');
        if (action === 'detalle' && params.data) this.openDetail(params.data);
      }
    }
  ];

  rows: AchManualReviewRow[] = [];
  totalCount = 0;
  totalPages = 0;
  loading = false;
  error = false;

  ngOnInit(): void {
    this.loadManualReviewCases();
  }

  applyFilters(): void {
    this.filtrosForm.patchValue({ pageNumber: 1 });
    this.loadManualReviewCases();
  }

  clearFilters(): void {
    this.filtrosForm.setValue({
      fechaDesde: '',
      fechaHasta: '',
      estadoProcesamiento: 'NoHomologada',
      tipoRespuesta: '',
      idTransaccion: '',
      codigoCamaraCompensacion: '',
      codigoEntidadOrigen: '',
      codigoEntidadDestino: '',
      pageNumber: 1,
      pageSize: 20
    });
    this.loadManualReviewCases();
  }

  previousPage(): void {
    const current = this.filtrosForm.controls.pageNumber.value ?? 1;
    if (current <= 1) return;
    this.filtrosForm.patchValue({ pageNumber: current - 1 });
    this.loadManualReviewCases();
  }

  nextPage(): void {
    const current = this.filtrosForm.controls.pageNumber.value ?? 1;
    if (this.totalPages > 0 && current >= this.totalPages) return;
    this.filtrosForm.patchValue({ pageNumber: current + 1 });
    this.loadManualReviewCases();
  }

  setPageSize(value: number): void {
    this.filtrosForm.patchValue({ pageSize: value, pageNumber: 1 });
    this.loadManualReviewCases();
  }

  openDetail(row: AchManualReviewRow): void {
    this.router.navigate(['/ach-responses', row.id]);
  }

  getPriority(status: string | null | undefined): string {
    if (status === 'NoHomologada' || status === 'ErrorFuncional') return 'Alta';
    if (status === 'RequiereRevisionManual' || status === 'PendienteReintento') return 'Media';
    return 'Baja';
  }

  getPriorityClass(priority: string): string {
    return getAchPriorityClass(priority);
  }

  formatProcessingStatus(status: string | null | undefined): string {
    return status?.trim() ? status : '-';
  }

  getProcessingStatusClass(status: string | null | undefined): string {
    if (status === 'NoHomologada' || status === 'RequiereRevisionManual' || status === 'PendienteReintento') return 'estado-advertencia';
    if (status === 'ErrorFuncional') return 'estado-error';
    return 'estado-neutro';
  }

  formatValue(value: unknown): string {
    return formatAchValue(value);
  }

  private loadManualReviewCases(): void {
    this.loading = true;
    this.error = false;
    this.cdr.markForCheck();

    const request = this.buildSearchRequest();

    this.api.search(request).pipe(
      finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      })
    ).subscribe({
      next: (response) => {
        this.rows = (response.items ?? []).map((item) => this.mapRow(item));
        this.totalCount = response.totalCount ?? 0;
        this.totalPages = response.totalPages ?? 0;
      },
      error: () => {
        this.error = true;
        this.rows = [];
        this.totalCount = 0;
        this.totalPages = 0;
        this.notifications.error('No fue posible cargar los casos de revisión manual ACH');
      }
    });
  }

  private buildSearchRequest(): AchResponseSearchRequest {
    const raw = this.filtrosForm.getRawValue();
    return {
      fechaDesde: this.normalize(raw.fechaDesde),
      fechaHasta: this.normalize(raw.fechaHasta),
      estadoProcesamiento: this.normalize(raw.estadoProcesamiento),
      tipoRespuesta: this.normalize(raw.tipoRespuesta) as 'Prenota' | 'Transaccion' | undefined,
      idTransaccion: this.normalize(raw.idTransaccion),
      codigoCamaraCompensacion: this.normalize(raw.codigoCamaraCompensacion),
      codigoEntidadOrigen: this.normalize(raw.codigoEntidadOrigen),
      codigoEntidadDestino: this.normalize(raw.codigoEntidadDestino),
      pageNumber: raw.pageNumber ?? 1,
      pageSize: raw.pageSize ?? 20
    };
  }

  private mapRow(item: AchResponseListItemResponse): AchManualReviewRow {
    return {
      ...item,
      fechaRecepcionText: this.formatDate(item.fechaRecepcion),
      fechaCreacionText: this.formatDate(item.fechaCreacion),
      estadoCriticoText: this.formatProcessingStatus(item.estadoProcesamiento),
      prioridadText: this.getPriority(item.estadoProcesamiento),
      permiteNotificacionText: item.permiteNotificacion ? 'Sí' : 'No'
    };
  }

  private formatDate(value: string | null | undefined): string {
    if (!value) return '-';
    return formatAchDate(value);
  }

  private normalize(value: string | null | undefined): string | undefined {
    return normalizeAchFilter(value);
  }
}
