import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ColDef } from 'ag-grid-community';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { AchResponseMappingWriteRequest, AchResponseStatusMappingResponse } from '../models/ach-responses.models';
import { AchResponsesApiService } from '../services/ach-responses-api.service';
import { formatAchBoolean, formatAchDate, formatAchValue, normalizeAchFilter } from '../utils/ach-response-formatters';
import { createAchBooleanBadgeElement, createAchButtonElement } from '../utils/ach-response-renderers';

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

  readonly mappingForm = this.fb.group({
    clearingHouseId: [0, [Validators.required, Validators.min(1)]],
    responseType: ['Transaccion', Validators.required],
    externalCode: ['', Validators.required],
    externalCause: [''],
    internalStatusId: [0, [Validators.required, Validators.min(1)]],
    externalServiceStatusId: [0, [Validators.required, Validators.min(1)]],
    internalStatusName: ['', Validators.required],
    normalizedCause: [''],
    normalizedDescription: [''],
    requiresCause: [false],
    allowsNotification: [false],
    priority: [0, Validators.required],
    effectiveFrom: ['', Validators.required],
    effectiveTo: [''],
    isActive: [true],
    expectedVersion: [''],
    reason: ['', [Validators.required, Validators.minLength(5)]]
  });

  readonly tiposRespuesta: Array<'' | 'Prenota' | 'Transaccion'> = ['', 'Prenota', 'Transaccion'];
  readonly activos: Array<'' | 'true' | 'false'> = ['', 'true', 'false'];

  readonly columnas: ColDef<AchStatusMappingRow>[] = [
    { field: 'codigoCamaraCompensacion', headerName: 'Cámara', minWidth: 120 },
    { field: 'tipoRespuesta', headerName: 'Tipo respuesta', minWidth: 130 },
    { field: 'codigoEstadoExterno', headerName: 'Estado externo', minWidth: 130 },
    { field: 'codigoCausalExterna', headerName: 'Causal externa', minWidth: 140 },
    { field: 'priority', headerName: 'Prioridad', minWidth: 110 },
    { field: 'idEstadoInterno', headerName: 'Id estado interno', minWidth: 140 },
    { field: 'idEstadoServicioExterno', headerName: 'Id estado servicio externo', minWidth: 170 },
    { field: 'estadoInternoNombre', headerName: 'Estado interno', minWidth: 140 },
    { field: 'causalNormalizada', headerName: 'Causal normalizada', minWidth: 150 },
    { field: 'descripcionCausalNormalizada', headerName: 'Descripción causal', minWidth: 180 },
    {
      field: 'requiereCausalText',
      headerName: 'Requiere causal',
      minWidth: 130,
      cellRenderer: (params: any) => createAchBooleanBadgeElement(params.data?.requiereCausal, 'requiereCausal')
    },
    {
      field: 'permiteNotificacionText',
      headerName: 'Permite notificación',
      minWidth: 160,
      cellRenderer: (params: any) => createAchBooleanBadgeElement(params.data?.permiteNotificacion, 'permiteNotificacion')
    },
    {
      field: 'activoText',
      headerName: 'Activo',
      minWidth: 110,
      cellRenderer: (params: any) => createAchBooleanBadgeElement(params.data?.activo, 'activo')
    },
    { field: 'fechaInicioVigenciaText', headerName: 'Inicio vigencia', minWidth: 160 },
    { field: 'fechaFinVigenciaText', headerName: 'Fin vigencia', minWidth: 160 },
    {
      headerName: 'Acciones', minWidth: 120, sortable: false, filter: false,
      cellRenderer: () => createAchButtonElement('Editar', 'editar'),
      onCellClicked: (params) => {
        const action = (params.event?.target as HTMLElement | null)?.getAttribute('data-action');
        if (action === 'editar' && params.data) this.editMapping(params.data);
      }
    }
  ];

  rows: AchStatusMappingRow[] = [];
  loading = false;
  error = false;
  editorVisible = false;
  saving = false;
  editingId: number | null = null;

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

  formatValue(value: unknown): string { return formatAchValue(value); }

  formatBoolean(value: boolean | null | undefined): string { return formatAchBoolean(value); }

  formatDate(value: string | null | undefined): string { return formatAchDate(value); }

  parseActivoFilter(value: string | null | undefined): boolean | undefined {
    if (value === 'true') return true;
    if (value === 'false') return false;
    return undefined;
  }

  normalize(value: string | null | undefined): string | undefined { return normalizeAchFilter(value); }

  newMapping(): void {
    this.editingId = null;
    this.editorVisible = true;
    this.mappingForm.reset({
      clearingHouseId: 0, responseType: 'Transaccion', externalCode: '', externalCause: '',
      internalStatusId: 0, externalServiceStatusId: 0, internalStatusName: '', normalizedCause: '',
      normalizedDescription: '', requiresCause: false, allowsNotification: false, priority: 0,
      effectiveFrom: '', effectiveTo: '', isActive: true, expectedVersion: '', reason: ''
    });
  }

  editMapping(row: AchResponseStatusMappingResponse): void {
    this.editingId = row.id;
    this.editorVisible = true;
    this.mappingForm.setValue({
      clearingHouseId: row.clearingHouseId ?? 0,
      responseType: row.tipoRespuesta === 'Prenota' ? 'Prenota' : 'Transaccion',
      externalCode: row.codigoEstadoExterno ?? '', externalCause: row.codigoCausalExterna ?? '',
      internalStatusId: row.idEstadoInterno, externalServiceStatusId: row.idEstadoServicioExterno,
      internalStatusName: row.estadoInternoNombre ?? '', normalizedCause: row.causalNormalizada ?? '',
      normalizedDescription: row.descripcionCausalNormalizada ?? '', requiresCause: row.requiereCausal,
      allowsNotification: row.permiteNotificacion, priority: row.priority ?? 0,
      effectiveFrom: this.toDateInput(row.fechaInicioVigencia), effectiveTo: this.toDateInput(row.fechaFinVigencia),
      isActive: row.activo, expectedVersion: row.version ?? '', reason: ''
    });
    this.cdr.markForCheck();
  }

  cancelEditor(): void {
    this.editorVisible = false;
    this.editingId = null;
    this.mappingForm.reset();
  }

  saveMapping(): void {
    if (this.mappingForm.invalid) {
      this.mappingForm.markAllAsTouched();
      this.notifications.error('Complete los campos obligatorios y la justificación.');
      return;
    }
    const raw = this.mappingForm.getRawValue();
    if (raw.effectiveTo && raw.effectiveFrom && raw.effectiveTo < raw.effectiveFrom) {
      this.notifications.error('La fecha fin no puede ser anterior a la fecha inicio.');
      return;
    }
    const request: AchResponseMappingWriteRequest = {
      clearingHouseId: raw.clearingHouseId ?? 0,
      responseType: raw.responseType === 'Prenota' ? 'Prenota' : 'Transaccion',
      externalCode: raw.externalCode?.trim() ?? '', externalCause: this.normalize(raw.externalCause) ?? null,
      internalStatusId: raw.internalStatusId ?? 0, externalServiceStatusId: raw.externalServiceStatusId ?? 0,
      internalStatusName: raw.internalStatusName?.trim() ?? '', normalizedCause: this.normalize(raw.normalizedCause) ?? null,
      normalizedDescription: this.normalize(raw.normalizedDescription) ?? null,
      requiresCause: !!raw.requiresCause, allowsNotification: !!raw.allowsNotification,
      priority: raw.priority ?? 0, effectiveFrom: this.asUtc(raw.effectiveFrom ?? ''),
      effectiveTo: raw.effectiveTo ? this.asUtc(raw.effectiveTo) : null, isActive: !!raw.isActive,
      expectedVersion: this.normalize(raw.expectedVersion) ?? null, reason: raw.reason?.trim() ?? ''
    };
    this.saving = true;
    const call = this.editingId == null
      ? this.api.createStatusMapping(request)
      : this.api.updateStatusMapping(this.editingId, request);
    call.pipe(finalize(() => { this.saving = false; this.cdr.markForCheck(); })).subscribe({
      next: () => {
        this.notifications.success('Mapping guardado correctamente.');
        this.cancelEditor();
        this.loadMappings();
      },
      error: (error: HttpErrorResponse) => this.handleWriteError(error)
    });
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

    this.api.getStatusMappings(filters).pipe(
      finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      })
    ).subscribe({
      next: (response) => {
        this.rows = (response ?? []).map((item) => this.mapRow(item));
      },
      error: () => {
        this.error = true;
        this.rows = [];
        this.notifications.error('No fue posible cargar las homologaciones ACH');
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

  private handleWriteError(error: HttpErrorResponse): void {
    if (error.status === 409) {
      this.notifications.warning('El mapping cambió mientras lo editaba. Se recargó la versión vigente.');
      this.loadMappings();
      if (this.editingId != null) {
        const id = this.editingId;
        this.api.getStatusMapping(id).subscribe({ next: (current) => {
          const row = this.rows.find((item) => item.id === id);
          if (row) this.editMapping({ ...row, version: current.version, priority: current.priority,
            clearingHouseId: current.clearingHouseId });
        }});
      }
      return;
    }
    const detail = typeof error.error?.detail === 'string' ? error.error.detail : 'No fue posible guardar el mapping.';
    this.notifications.error(detail);
  }

  private toDateInput(value: string | null | undefined): string {
    return value ? value.slice(0, 10) : '';
  }

  private asUtc(value: string): string { return `${value}T00:00:00.000Z`; }

}
