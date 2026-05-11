import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ColDef } from 'ag-grid-community';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { AchResponseStatusMappingResponse } from '../models/ach-responses.models';
import { AchResponsesApiService } from '../services/ach-responses-api.service';

type AchStatusMappingRow = AchResponseStatusMappingResponse & {
  activoText: string;
  requiereCausalText: string;
  permiteNotificacionText: string;
  fechaInicioVigenciaText: string;
  fechaFinVigenciaText: string;
};

@Component({
  selector: 'app-ach-response-status-mappings-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, SharedModule],
  templateUrl: './ach-response-status-mappings-page.component.html',
  styleUrls: ['./ach-response-status-mappings-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchResponseStatusMappingsPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(AchResponsesApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly filtrosForm = this.fb.group({
    codigoCamaraCompensacion: [''],
    tipoRespuesta: [''],
    activo: ['']
  });

  readonly tiposRespuesta: Array<'' | 'Prenota' | 'Transaccion'> = ['', 'Prenota', 'Transaccion'];
  readonly activos: Array<'' | 'true' | 'false'> = ['', 'true', 'false'];

  readonly columnas: ColDef<AchStatusMappingRow>[] = [
    { field: 'codigoCamaraCompensacion', headerName: 'Cámara', minWidth: 120 },
    { field: 'tipoRespuesta', headerName: 'Tipo respuesta', minWidth: 130 },
    { field: 'codigoEstadoExterno', headerName: 'Estado externo', minWidth: 130 },
    { field: 'codigoCausalExterna', headerName: 'Causal externa', minWidth: 140 },
    { field: 'idEstadoInterno', headerName: 'Id estado interno', minWidth: 140 },
    { field: 'idEstadoServicioExterno', headerName: 'Id estado servicio externo', minWidth: 170 },
    { field: 'estadoInternoNombre', headerName: 'Estado interno', minWidth: 140 },
    { field: 'causalNormalizada', headerName: 'Causal normalizada', minWidth: 150 },
    { field: 'descripcionCausalNormalizada', headerName: 'Descripción causal', minWidth: 180 },
    {
      field: 'requiereCausalText',
      headerName: 'Requiere causal',
      minWidth: 130,
      cellRenderer: (params: any) => this.renderBooleanBadge(params.data?.requiereCausal, 'Sí', 'No', 'requiereCausal')
    },
    {
      field: 'permiteNotificacionText',
      headerName: 'Permite notificación',
      minWidth: 160,
      cellRenderer: (params: any) => this.renderBooleanBadge(params.data?.permiteNotificacion, 'Sí', 'No', 'permiteNotificacion')
    },
    {
      field: 'activoText',
      headerName: 'Activo',
      minWidth: 110,
      cellRenderer: (params: any) => this.renderBooleanBadge(params.data?.activo, 'Sí', 'No', 'activo')
    },
    { field: 'fechaInicioVigenciaText', headerName: 'Inicio vigencia', minWidth: 160 },
    { field: 'fechaFinVigenciaText', headerName: 'Fin vigencia', minWidth: 160 }
  ];

  rows: AchStatusMappingRow[] = [];
  loading = false;
  error = false;

  ngOnInit(): void {
    this.loadMappings();
  }

  applyFilters(): void {
    this.loadMappings();
  }

  clearFilters(): void {
    this.filtrosForm.setValue({
      codigoCamaraCompensacion: '',
      tipoRespuesta: '',
      activo: ''
    });
    this.loadMappings();
  }

  formatValue(value: unknown): string {
    if (value === null || value === undefined || value === '') return '-';
    if (typeof value === 'boolean') return value ? 'Sí' : 'No';
    return String(value);
  }

  formatBoolean(value: boolean | null | undefined): string {
    if (value === true) return 'Sí';
    if (value === false) return 'No';
    return '-';
  }

  formatDate(value: string | null | undefined): string {
    if (!value) return '-';
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? value : date.toLocaleString('es-CO');
  }

  parseActivoFilter(value: string | null | undefined): boolean | undefined {
    if (value === 'true') return true;
    if (value === 'false') return false;
    return undefined;
  }

  normalize(value: string | null | undefined): string | undefined {
    const trimmed = value?.trim();
    return trimmed ? trimmed : undefined;
  }

  private loadMappings(): void {
    this.loading = true;
    this.error = false;
    this.cdr.markForCheck();

    const raw = this.filtrosForm.getRawValue();
    const filters = {
      codigoCamaraCompensacion: this.normalize(raw.codigoCamaraCompensacion),
      tipoRespuesta: this.normalize(raw.tipoRespuesta) as 'Prenota' | 'Transaccion' | undefined,
      activo: this.parseActivoFilter(raw.activo)
    };

    this.api.getStatusMappings(filters).subscribe({
      next: (response) => {
        this.rows = (response ?? []).map((item) => this.mapRow(item));
      },
      error: () => {
        this.error = true;
        this.rows = [];
        this.notifications.error('No fue posible cargar las homologaciones ACH');
      },
      complete: () => {
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  private mapRow(item: AchResponseStatusMappingResponse): AchStatusMappingRow {
    return {
      ...item,
      activoText: this.formatBoolean(item.activo),
      requiereCausalText: this.formatBoolean(item.requiereCausal),
      permiteNotificacionText: this.formatBoolean(item.permiteNotificacion),
      fechaInicioVigenciaText: this.formatDate(item.fechaInicioVigencia),
      fechaFinVigenciaText: this.formatDate(item.fechaFinVigencia)
    };
  }

  private renderBooleanBadge(
    value: boolean | null | undefined,
    trueText = 'Sí',
    falseText = 'No',
    context: 'activo' | 'requiereCausal' | 'permiteNotificacion' = 'activo'
  ): HTMLElement {
    const span = document.createElement('span');
    span.classList.add('estado-pill');

    if (value === null || value === undefined) {
      span.classList.add('estado-advertencia');
      span.textContent = '-';
      return span;
    }

    span.textContent = value ? trueText : falseText;

    if (context === 'activo') {
      span.classList.add(value ? 'estado-exitoso' : 'estado-neutro');
      return span;
    }

    if (context === 'requiereCausal') {
      span.classList.add(value ? 'estado-advertencia' : 'estado-neutro');
      return span;
    }

    span.classList.add(value ? 'estado-exitoso' : 'estado-advertencia');
    return span;
  }
}
