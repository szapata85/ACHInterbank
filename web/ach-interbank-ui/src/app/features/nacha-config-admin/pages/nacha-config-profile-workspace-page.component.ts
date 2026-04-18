import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, Validators } from '@angular/forms';
import { ColDef } from 'ag-grid-community';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import {
  NachaConfigApiError,
  NachaConfigHistoryItem,
  NachaConfigLayoutField,
  NachaConfigLayoutVariant,
  NachaConfigProfileDetail,
  NachaConfigProfileRecord,
  NachaConfigResolverPreviewResult,
  NachaConfigSnapshotItem,
  NachaConfigValidationResult
} from '../models/nacha-config-admin.models';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';
import { NachaConfigStateService } from '../services/nacha-config-state.service';

type TabKey = 'detalle' | 'secuencia' | 'variantes' | 'fields' | 'rules' | 'validacion' | 'historial' | 'preview';

@Component({
  selector: 'app-nacha-config-profile-workspace-page',
  templateUrl: './nacha-config-profile-workspace-page.component.html',
  styleUrls: ['./nacha-config-profile-workspace-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaConfigProfileWorkspacePageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly query = inject(NachaConfigQueryService);
  private readonly command = inject(NachaConfigCommandService);
  private readonly state = inject(NachaConfigStateService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  perfilId = 0;
  perfil: NachaConfigProfileDetail | null = null;
  historial: NachaConfigHistoryItem[] = [];
  snapshots: NachaConfigSnapshotItem[] = [];
  validacion: NachaConfigValidationResult | null = null;
  preview: NachaConfigResolverPreviewResult | null = null;

  cargando = false;
  procesando = false;
  tab: TabKey = 'detalle';
  alerta: { tipo: 'info' | 'exito' | 'advertencia' | 'error'; mensaje: string } | null = null;

  readonly tabs: Array<{ key: TabKey; label: string }> = [
    { key: 'detalle', label: 'Detalle y edición' },
    { key: 'secuencia', label: 'Secuencia records' },
    { key: 'variantes', label: 'Variantes' },
    { key: 'fields', label: 'Fields' },
    { key: 'rules', label: 'Rules' },
    { key: 'validacion', label: 'Validación y publicación' },
    { key: 'historial', label: 'Historial y snapshots' },
    { key: 'preview', label: 'Preview resolver' }
  ];

  readonly editarForm = this.fb.group({
    nombreEs: ['', [Validators.required, Validators.maxLength(120)]],
    descripcion: [''],
    contextPriority: [100, [Validators.required]],
    effectiveFrom: ['', Validators.required],
    effectiveTo: ['']
  });

  readonly clonarForm = this.fb.group({
    nuevoProfileCode: ['', Validators.required],
    nuevoNombreEs: ['', Validators.required],
    effectiveFrom: ['', Validators.required]
  });

  readonly varianteForm = this.fb.group({
    variantId: [0, Validators.required],
    nombreEs: ['', Validators.required],
    descripcion: [''],
    priority: [100, Validators.required],
    isDefaultForRecord: [false],
    effectiveFrom: ['', Validators.required],
    effectiveTo: ['']
  });

  readonly fieldForm = this.fb.group({
    fieldId: [0, Validators.required],
    fieldNameEs: ['', Validators.required],
    startPosition: [1, Validators.required],
    length: [1, Validators.required],
    propertyPath: [''],
    isEnabled: [true]
  });

  readonly ruleForm = this.fb.group({
    ruleId: [0, Validators.required],
    errorCode: ['', Validators.required],
    errorMessageEs: ['', Validators.required],
    severity: ['ERROR', Validators.required],
    isEnabled: [true]
  });

  readonly previewForm = this.fb.group({
    camaraCode: ['ACH', Validators.required],
    flujoCode: ['ORIGINAL', Validators.required],
    direccionCode: ['SALIDA', Validators.required],
    servicioCode: ['PPD'],
    processDateUtc: [this.today(), Validators.required],
    recordCodesCsv: ['1,5,6,8,9', Validators.required]
  });

  get rowsRecords(): NachaConfigProfileRecord[] {
    return this.perfil?.records ?? [];
  }

  get rowsVariantes(): NachaConfigLayoutVariant[] {
    return this.perfil?.variantes ?? [];
  }

  get rowsFields(): NachaConfigLayoutField[] {
    return (this.perfil?.variantes ?? []).flatMap((v) => v.fields.map((f) => ({ ...f, variantCode: v.variantCode } as NachaConfigLayoutField & { variantCode: string })));
  }

  readonly columnasRecords: ColDef<NachaConfigProfileRecord>[] = [
    { field: 'recordCode', headerName: 'Record', minWidth: 100 },
    { field: 'sequence', headerName: 'Secuencia', minWidth: 120, editable: true },
    { field: 'isEnabled', headerName: 'Habilitado', minWidth: 120 },
    { field: 'minOccurs', headerName: 'Min', minWidth: 80 },
    { field: 'maxOccurs', headerName: 'Max', minWidth: 80 },
    { field: 'sourceStrategy', headerName: 'Estrategia', minWidth: 160 }
  ];

  readonly columnasVariantes: ColDef<NachaConfigLayoutVariant>[] = [
    { field: 'recordCode', headerName: 'Record', minWidth: 90 },
    { field: 'variantCode', headerName: 'Código variante', minWidth: 140 },
    { field: 'nombreEs', headerName: 'Nombre', minWidth: 180 },
    { field: 'priority', headerName: 'Prioridad', minWidth: 100 },
    { field: 'isDefaultForRecord', headerName: 'Default', minWidth: 100 },
    { field: 'totalLength', headerName: 'Longitud', minWidth: 100 }
  ];

  readonly columnasFields: ColDef<any>[] = [
    { field: 'variantCode', headerName: 'Variante', minWidth: 120 },
    { field: 'fieldCode', headerName: 'Field', minWidth: 120 },
    { field: 'fieldNameEs', headerName: 'Nombre', minWidth: 190 },
    { field: 'startPosition', headerName: 'Posición', minWidth: 100 },
    { field: 'length', headerName: 'Longitud', minWidth: 90 },
    { field: 'propertyPath', headerName: 'Source', minWidth: 220 }
  ];

  readonly columnasHistorial: ColDef<NachaConfigHistoryItem>[] = [
    { field: 'changedAtUtc', headerName: 'Fecha', minWidth: 170 },
    { field: 'changedBy', headerName: 'Usuario', minWidth: 140 },
    { field: 'changeType', headerName: 'Cambio', minWidth: 140 },
    { field: 'entityName', headerName: 'Entidad', minWidth: 140 },
    { field: 'correlationId', headerName: 'Correlación', minWidth: 260 }
  ];

  readonly columnasSnapshots: ColDef<NachaConfigSnapshotItem>[] = [
    { field: 'createdAtUtc', headerName: 'Fecha', minWidth: 170 },
    { field: 'createdBy', headerName: 'Usuario', minWidth: 140 },
    { field: 'snapshotType', headerName: 'Tipo', minWidth: 130 },
    { headerName: 'Versión', minWidth: 120, valueGetter: (p) => `v${p.data?.versionMajor}.${p.data?.versionMinor}` }
  ];

  ngOnInit(): void {
    this.perfilId = Number(this.route.snapshot.paramMap.get('id'));
    if (!this.perfilId) {
      this.router.navigate(['/nacha-config-admin/perfiles']);
      return;
    }

    this.cargarTodo();
  }

  seleccionarTab(tab: TabKey): void {
    this.tab = tab;
  }

  cargarTodo(): void {
    this.cargando = true;
    this.alerta = null;

    this.query.detalle(this.perfilId).pipe(finalize(() => {
      this.cargando = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: (perfil) => {
        this.perfil = perfil;
        this.state.setPerfil(perfil);
        this.editarForm.patchValue({
          nombreEs: perfil.nombreEs,
          descripcion: perfil.descripcion ?? '',
          contextPriority: perfil.contextPriority,
          effectiveFrom: (perfil.effectiveFrom ?? '').slice(0, 10),
          effectiveTo: (perfil.effectiveTo ?? '').slice(0, 10)
        });
        this.query.historial(this.perfilId).subscribe((rows) => {
          this.historial = rows;
          this.cdr.markForCheck();
        });
        this.query.snapshots(this.perfilId).subscribe((rows) => {
          this.snapshots = rows;
          this.cdr.markForCheck();
        });
      },
      error: () => {
        this.alerta = { tipo: 'error', mensaje: 'No fue posible cargar el perfil seleccionado.' };
      }
    });
  }

  guardarEdicion(): void {
    if (!this.perfil || this.procesando) return;

    this.procesando = true;
    this.command.editarBorrador(this.perfil.id, {
      ...this.editarForm.getRawValue(),
      expectedRowVersion: this.state.rowVersionActual()
    }).pipe(finalize(() => {
      this.procesando = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: (perfil) => {
        this.onSuccess('Perfil actualizado correctamente.');
        this.perfil = perfil;
        this.state.setPerfil(perfil);
      },
      error: (error: NachaConfigApiError) => this.onBackendError(error)
    });
  }

  clonarPerfil(): void {
    if (!this.perfil || this.procesando) return;
    this.procesando = true;

    this.command.clonar(this.perfil.id, {
      ...this.clonarForm.getRawValue(),
      expectedRowVersion: this.state.rowVersionActual()
    }).pipe(finalize(() => {
      this.procesando = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: (clone) => {
        this.notifications.success('Perfil clonado correctamente.');
        this.router.navigate(['/nacha-config-admin/perfiles', clone.id]);
      },
      error: (error: NachaConfigApiError) => this.onBackendError(error)
    });
  }

  guardarSecuencia(): void {
    if (!this.perfil || this.procesando) return;
    const records = this.rowsRecords.map((r) => ({ profileRecordId: r.id, sequence: r.sequence }));
    this.procesando = true;
    this.command.actualizarSecuencia(this.perfil.id, {
      expectedRowVersion: this.state.rowVersionActual(),
      records
    }).pipe(finalize(() => {
      this.procesando = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: () => {
        this.onSuccess('Secuencia actualizada correctamente.');
        this.cargarTodo();
      },
      error: (error: NachaConfigApiError) => this.onBackendError(error)
    });
  }

  guardarVariante(): void {
    if (!this.perfil || this.procesando) return;
    const payload = { ...this.varianteForm.getRawValue(), expectedRowVersion: this.state.rowVersionActual() };
    this.procesando = true;
    this.command.actualizarVariante(this.perfil.id, Number(payload.variantId), payload as Record<string, unknown>)
      .pipe(finalize(() => {
        this.procesando = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: () => {
          this.onSuccess('Variante actualizada correctamente.');
          this.cargarTodo();
        },
        error: (error: NachaConfigApiError) => this.onBackendError(error)
      });
  }

  guardarField(): void {
    if (!this.perfil || this.procesando) return;
    const payload = { ...this.fieldForm.getRawValue(), expectedRowVersion: this.state.rowVersionActual() };
    this.procesando = true;
    this.command.actualizarField(this.perfil.id, Number(payload.fieldId), payload as Record<string, unknown>)
      .pipe(finalize(() => {
        this.procesando = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: () => {
          this.onSuccess('Field actualizado correctamente.');
          this.cargarTodo();
        },
        error: (error: NachaConfigApiError) => this.onBackendError(error)
      });
  }

  guardarRule(): void {
    if (!this.perfil || this.procesando) return;
    const payload = { ...this.ruleForm.getRawValue(), expectedRowVersion: this.state.rowVersionActual() };
    this.procesando = true;
    this.command.actualizarRule(this.perfil.id, Number(payload.ruleId), payload as Record<string, unknown>)
      .pipe(finalize(() => {
        this.procesando = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: () => this.onSuccess('Rule actualizada correctamente.'),
        error: (error: NachaConfigApiError) => this.onBackendError(error)
      });
  }

  validar(): void {
    if (!this.perfil || this.procesando) return;
    this.procesando = true;
    this.command.validar(this.perfil.id).pipe(finalize(() => {
      this.procesando = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: (result) => {
        this.validacion = result;
        this.state.setValidacion(result);
        this.alerta = { tipo: result.isValid ? 'exito' : 'advertencia', mensaje: result.resumen };
      },
      error: (error: NachaConfigApiError) => this.onBackendError(error)
    });
  }

  publicar(): void {
    if (!this.perfil || this.procesando) return;
    this.procesando = true;
    this.command.publicar(this.perfil.id, this.state.rowVersionActual()).pipe(finalize(() => {
      this.procesando = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: () => {
        this.onSuccess('Perfil publicado correctamente.');
        this.cargarTodo();
      },
      error: (error: NachaConfigApiError) => this.onBackendError(error)
    });
  }

  inactivar(): void {
    this.cambiarEstado('inactivar');
  }

  archivar(): void {
    this.cambiarEstado('archivar');
  }

  ejecutarPreview(): void {
    if (this.procesando) return;
    this.procesando = true;
    const raw = this.previewForm.getRawValue();
    const payload = {
      camaraCode: raw.camaraCode,
      flujoCode: raw.flujoCode,
      direccionCode: raw.direccionCode,
      servicioCode: raw.servicioCode,
      processDateUtc: new Date(raw.processDateUtc || this.today()).toISOString(),
      recordCodes: (raw.recordCodesCsv ?? '').split(',').map((x) => x.trim()).filter(Boolean)
    };

    this.command.preview(payload).pipe(finalize(() => {
      this.procesando = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: (result) => {
        this.preview = result;
        this.alerta = { tipo: 'info', mensaje: result.success ? 'Preview resuelto correctamente.' : 'No se resolvió un perfil para el contexto solicitado.' };
      },
      error: (error: NachaConfigApiError) => this.onBackendError(error)
    });
  }

  private cambiarEstado(accion: 'inactivar' | 'archivar'): void {
    if (!this.perfil || this.procesando) return;
    this.procesando = true;
    const stream = accion === 'inactivar'
      ? this.command.inactivar(this.perfil.id, this.state.rowVersionActual())
      : this.command.archivar(this.perfil.id, this.state.rowVersionActual());

    stream.pipe(finalize(() => {
      this.procesando = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: () => {
        this.onSuccess(`Perfil ${accion === 'inactivar' ? 'inactivado' : 'archivado'} correctamente.`);
        this.cargarTodo();
      },
      error: (error: NachaConfigApiError) => this.onBackendError(error)
    });
  }

  private onSuccess(message: string): void {
    this.alerta = { tipo: 'exito', mensaje: message };
    this.notifications.success(message);
  }

  private onBackendError(error: NachaConfigApiError): void {
    if (error.errorCode === 'CONCURRENCY_CONFLICT' || error.errorCode === 'CONCURRENCY_TOKEN_REQUIRED' || error.errorCode === 'INVALID_CONCURRENCY_TOKEN') {
      this.alerta = { tipo: 'error', mensaje: `${error.message}${error.currentRowVersion ? ` (Versión actual: ${error.currentRowVersion})` : ''}` };
      this.notifications.warning('Se detectó conflicto de concurrencia. Recargue el perfil antes de continuar.');
      return;
    }

    if (error.errorCode === 'PUBLISH_BLOCKED') {
      this.alerta = { tipo: 'advertencia', mensaje: 'La publicación fue bloqueada por validaciones pendientes.' };
      return;
    }

    this.alerta = { tipo: 'error', mensaje: error.message };
    this.notifications.error(error.message);
  }

  private today(): string {
    return new Date().toISOString().slice(0, 10);
  }
}
