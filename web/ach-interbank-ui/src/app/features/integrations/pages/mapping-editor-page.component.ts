import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Router } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { Observable, Subject, forkJoin, of, throwError } from 'rxjs';
import { catchError, distinctUntilChanged, finalize, map, switchMap, takeUntil, timeout } from 'rxjs/operators';
import {
  IntegrationMappingAdminService,
  IntegrationMappingRule,
  IntegrationMappingSet,
  IntegrationMethodParameter,
  IntegrationSourceCatalogField,
  IntegrationTransformationCatalog,
  ParameterValidationStatus,
  PreviewResult,
  ValidationResult,
  MappingSetHistoryItem
} from '../../../core/services/integration-mapping-admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';

@Component({
  selector: 'app-mapping-editor-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, SharedModule],
  templateUrl: './mapping-editor-page.component.html',
  styleUrls: ['./mapping-editor-page.component.scss']
})
export class MappingEditorPageComponent implements OnInit, OnDestroy {
  private readonly api = inject(IntegrationMappingAdminService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroy$ = new Subject<void>();

  mappingSetId = '';
  methodCode = '';

  readonly migas = [
    { etiqueta: 'Inicio', ruta: '/' },
    { etiqueta: 'Integraciones', ruta: '/integraciones' },
    { etiqueta: 'Configuración funcional', ruta: '/integraciones/mappings' },
    { etiqueta: 'Editor de configuración' }
  ];

  loading = false;
  viewState: 'loading' | 'error' | 'ready' = 'loading';
  errorMessage = '';
  mappingSet?: IntegrationMappingSet;
  parameters: IntegrationMethodParameter[] = [];
  sourceCatalog: IntegrationSourceCatalogField[] = [];
  sourceKindOptions: Array<{ value: string; label: string }> = [{ value: 'Constant', label: 'Valor fijo' }];
  transformations: IntegrationTransformationCatalog[] = [];

  selectedParameterId?: number;
  validationResult?: ValidationResult;
  previewResult?: PreviewResult;
  historyItems: MappingSetHistoryItem[] = [];

  savingRule = false;
  validating = false;
  previewing = false;
  publishing = false;
  cloning = false;


  readonly previewForm = this.fb.group({
    usarMuestraControlada: [true],
    idTransaccionMuestra: [null as number | null],
    idCicloMuestra: ['']
  });

  readonly ruleForm = this.fb.group({
    id: [null as number | null],
    sourceKind: ['Transaction'],
    sourceCatalogFieldId: [null as number | null],
    sourceFieldPath: [''],
    fixedValue: [''],
    defaultValue: [''],
    transformationCode: [''],
    formatMask: [''],
    priority: [1],
    requiredOverride: [null as boolean | null],
    enabled: [true],
    conditionExpression: ['']
  });

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        map((params) => ({
          mappingSetId: params.get('mappingSetId') ?? '',
          methodCode: params.get('methodCode') ?? ''
        })),
        distinctUntilChanged((a, b) => a.mappingSetId === b.mappingSetId && a.methodCode === b.methodCode),
        takeUntil(this.destroy$)
      )
      .subscribe(({ mappingSetId, methodCode }) => {
        this.mappingSetId = mappingSetId;
        this.methodCode = methodCode;

        if (!this.mappingSetId || !this.methodCode) {
          this.loading = false;
          this.viewState = 'error';
          this.errorMessage = 'No se recibieron los datos requeridos para abrir el editor.';
          return;
        }

        this.loadAll();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get currentRules(): IntegrationMappingRule[] {
    const paramId = this.selectedParameterId;
    if (!this.mappingSet || !paramId) return [];
    return this.mappingSet.rules
      .filter((x) => x.parameterId === paramId)
      .sort((a, b) => a.priority - b.priority);
  }

  get selectedParameter(): IntegrationMethodParameter | undefined {
    return this.parameters.find((x) => x.id === this.selectedParameterId);
  }

  get coverage() {
    const fallback = {
      totalParameters: this.parameters.length,
      validParameters: 0,
      incompleteParameters: this.parameters.length,
      invalidParameters: 0,
      inactiveParameters: 0,
      coveredByDefaultOrFixed: 0,
      coveredBySourceField: 0
    };

    return this.validationResult?.coverage ?? fallback;
  }

  get selectedParameterHints(): string[] {
    const status = this.getValidationStatus(this.selectedParameterId);
    return status?.hints ?? ['Selecciona una estrategia de origen y ejecuta validación para ver asistencia guiada.'];
  }

  get groupedPreview() {
    const grouped = {
      'ciclo-camara': [] as NonNullable<PreviewResult['items']>,
      transaccion: [] as NonNullable<PreviewResult['items']>,
      lote: [] as NonNullable<PreviewResult['items']>,
      addenda: [] as NonNullable<PreviewResult['items']>,
      configuracion: [] as NonNullable<PreviewResult['items']>
    };

    for (const item of this.previewResult?.items ?? []) {
      const key = (item.sourceSection || 'configuracion') as keyof typeof grouped;
      if (grouped[key]) grouped[key].push(item);
      else grouped.configuracion.push(item);
    }

    return grouped;
  }

  loadAll(): void {
    if (!this.mappingSetId || !this.methodCode) {
      this.loading = false;
      this.viewState = 'error';
      this.errorMessage = 'No se recibieron los datos requeridos para abrir el editor.';
      return;
    }

    console.debug('[mapping-editor] loadAll:start', { mappingSetId: this.mappingSetId, methodCode: this.methodCode });
    this.loading = true;
    this.viewState = 'loading';
    this.errorMessage = '';
    this.previewResult = undefined;
    this.validationResult = undefined;
    this.cdr.detectChanges();

    this.api
      .getMappingSetById(this.mappingSetId)
      .pipe(
        timeout(15000),
        catchError((error) => this.failEditorLoad<IntegrationMappingSet>('No existe o no se pudo cargar el mapping set solicitado.', error)),
        switchMap((set) => {
          const routeMethodCode = decodeURIComponent(this.methodCode).trim();
          if (routeMethodCode && set.methodCode !== routeMethodCode) {
            return this.failEditorLoad(
              `El mapping set cargado pertenece a ${set.methodCode}, pero la ruta solicita ${routeMethodCode}.`
            );
          }

          return forkJoin({
            parameters: this.api.getMethodParameters(set.methodId).pipe(
              timeout(15000),
              catchError((error) => this.failEditorLoad('No fue posible cargar los campos destino SOAP/XML.', error))
            ),
            sourceCatalog: this.api.getSourceCatalog(set.methodId).pipe(
              timeout(15000),
              catchError((error) => this.failEditorLoad('No fue posible cargar el catálogo controlado de campos origen.', error))
            ),
            transformations: this.api.getTransformations().pipe(
              timeout(15000),
              catchError((error) => this.failEditorLoad('No fue posible cargar el catálogo de transformaciones.', error))
            ),
            historyItems: this.api.getHistory(set.id).pipe(
              timeout(10000),
              catchError((historyError) => {
                console.warn('[mapping-editor] history load failed, continuing without history', historyError);
                return of([] as MappingSetHistoryItem[]);
              })
            )
          }).pipe(map((catalogs) => ({ set, ...catalogs })));
        }),
        takeUntil(this.destroy$),
        finalize(() => (this.loading = false))
      )
      .subscribe({
        next: ({ set, parameters, sourceCatalog, transformations, historyItems }) => {
          console.debug('[mapping-editor] loadAll:success', {
            mappingSetId: set.id,
            parameters: parameters?.length ?? 0,
            sourceCatalog: sourceCatalog?.length ?? 0,
            transformations: transformations?.length ?? 0,
            historyItems: historyItems?.length ?? 0
          });
          try {
            this.mappingSet = set;
            this.parameters = parameters ?? [];
            this.sourceCatalog = sourceCatalog ?? [];
            this.refreshSourceKindOptions();
            this.transformations = transformations ?? [];
            this.historyItems = historyItems ?? [];

            const parameterStillExists = this.parameters.some((x) => x.id === this.selectedParameterId);
            this.selectedParameterId = parameterStillExists ? this.selectedParameterId : this.parameters[0]?.id;
            this.populateFormFromSelectedRule();
            this.viewState = 'ready';
            this.cdr.detectChanges();
          } catch (renderError) {
            this.handleEditorLoadFailure(renderError);
          }
        },
        error: (error) => {
          this.handleEditorLoadFailure(error);
        }
      });
  }

  selectParameter(parameterId: number): void {
    this.selectedParameterId = parameterId;
    this.populateFormFromSelectedRule();
  }

  populateFormFromSelectedRule(): void {
    const first = this.currentRules[0];
    if (!first) {
      this.ruleForm.reset({
        id: null,
        sourceKind: 'Transaction',
        sourceCatalogFieldId: null,
        sourceFieldPath: '',
        fixedValue: '',
        defaultValue: '',
        transformationCode: '',
        formatMask: '',
        priority: 1,
        requiredOverride: null,
        enabled: true,
        conditionExpression: ''
      });
      return;
    }

    this.ruleForm.patchValue({
      id: first.id,
      sourceKind: first.sourceKind,
      sourceCatalogFieldId: first.sourceCatalogFieldId ?? null,
      sourceFieldPath: first.sourceFieldPath,
      fixedValue: first.fixedValue ?? '',
      defaultValue: first.defaultValue ?? '',
      transformationCode: first.transformationCode ?? '',
      formatMask: first.formatMask ?? '',
      priority: first.priority,
      requiredOverride: first.requiredOverride ?? null,
      enabled: first.enabled,
      conditionExpression: first.conditionExpression ?? ''
    });
  }

  onSourceCatalogFieldChange(fieldId: number | null): void {
    const field = this.sourceCatalog.find((item) => item.id === fieldId);
    this.ruleForm.patchValue({
      sourceFieldPath: field?.fieldPath ?? ''
    });
  }

  saveRule(): void {
    if (!this.mappingSet || !this.selectedParameterId || this.savingRule) return;

    const payload = {
      id: this.ruleForm.value.id,
      methodId: this.mappingSet.methodId,
      parameterId: this.selectedParameterId,
      sourceKind: this.ruleForm.value.sourceKind,
      sourceCatalogFieldId: this.ruleForm.value.sourceCatalogFieldId,
      sourceFieldPath: this.resolveControlledSourceFieldPath(),
      fixedValue: this.ruleForm.value.fixedValue,
      defaultValue: this.ruleForm.value.defaultValue,
      transformationCode: this.ruleForm.value.transformationCode,
      formatMask: this.ruleForm.value.formatMask,
      priority: Number(this.ruleForm.value.priority ?? 1),
      requiredOverride: this.ruleForm.value.requiredOverride,
      enabled: Boolean(this.ruleForm.value.enabled),
      conditionExpression: this.ruleForm.value.conditionExpression
    };

    this.savingRule = true;
    this.api.upsertRules(this.mappingSet.id, 'ui-admin', [payload]).subscribe({
      next: (updated) => {
        this.mappingSet = updated;
        this.notifications.success('Regla guardada. Ejecuta validación para confirmar consistencia.');
        this.populateFormFromSelectedRule();
      },
      error: () => this.notifications.error('No fue posible guardar la regla.'),
      complete: () => (this.savingRule = false)
    });
  }

  runValidation(onDone?: (isValid: boolean) => void): void {
    if (!this.mappingSet || this.validating) return;

    this.validating = true;
    this.api.validate(this.mappingSet.id).subscribe({
      next: (result) => {
        this.validationResult = result;
        const message = result.isValid
          ? 'Configuración válida y completa. Puedes publicar.'
          : 'Se encontraron observaciones estructurales/funcionales. Corrígelas antes de publicar.';
        this.notifications.success(message);
        onDone?.(result.isValid);
      },
      error: () => {
        this.notifications.error('No fue posible validar la configuración.');
        onDone?.(false);
      },
      complete: () => (this.validating = false)
    });
  }

  runPreview(): void {
    if (!this.mappingSet || this.previewing) return;

    this.previewing = true;
    this.api
      .preview(this.mappingSet.id, {
        sampleTransactionId: this.previewForm.controls.idTransaccionMuestra.value ?? undefined,
        sampleCycleId: this.previewForm.controls.idCicloMuestra.value?.trim() || undefined,
        useControlledSample: Boolean(this.previewForm.controls.usarMuestraControlada.value),
        maxItems: 200
      })
      .subscribe({
        next: (result) => {
          this.previewResult = result;
          this.notifications.success(`Simulación generada usando contexto: ${result.contextMode}.`);
        },
        error: () => this.notifications.error('No fue posible generar la simulación.'),
        complete: () => (this.previewing = false)
      });
  }

  publish(): void {
    if (!this.mappingSet || this.publishing) return;

    this.runValidation((isValid) => {
      if (!isValid) {
        this.notifications.error('Publicación bloqueada: configuración inválida o incompleta.');
        return;
      }

      this.publishing = true;
      this.api.publish(this.mappingSet!.id, 'ui-admin', 'Publicado desde SPA').subscribe({
        next: (updated) => {
          this.mappingSet = updated;
          this.notifications.success('Configuración publicada correctamente.');
        },
        error: () => this.notifications.error('No se pudo publicar. Revisa validación y cobertura.'),
        complete: () => (this.publishing = false)
      });
    });
  }

  clone(): void {
    if (!this.mappingSet || this.cloning) return;
    this.cloning = true;
    this.api.clone(this.mappingSet.id, `${this.mappingSet.name} (copia)`, 'ui-admin').subscribe({
      next: (created) => {
        this.notifications.success('Configuración clonada.');
        this.mappingSet = created;
        this.mappingSetId = created.id;
        this.router.navigate(['/integraciones/mappings', created.methodCode, created.id]);
      },
      error: () => this.notifications.error('No fue posible clonar la configuración.'),
      complete: () => (this.cloning = false)
    });
  }

  loadHistory(): void {
    if (!this.mappingSet) return;
    this.api.getHistory(this.mappingSet.id).subscribe({
      next: (items) => (this.historyItems = items ?? []),
      error: () => (this.historyItems = [])
    });
  }

  getValidationStatus(parameterId?: number): ParameterValidationStatus | undefined {
    if (!parameterId) return undefined;
    return this.validationResult?.parameters.find((x) => x.parameterId === parameterId);
  }

  getParameterStatus(parameterId: number): 'valid' | 'incomplete' | 'invalid' | 'inactive' | 'unknown' {
    return (this.getValidationStatus(parameterId)?.status as any) ?? 'unknown';
  }

  getStatusLabel(parameterId: number): string {
    const status = this.getParameterStatus(parameterId);
    const resolution = this.getValidationStatus(parameterId)?.resolutionKind;

    if (status === 'valid' && resolution === 'default-fixed') return 'Cubierto por valor fijo';
    if (status === 'valid' && resolution === 'source-field') return 'Resuelto por origen';
    if (status === 'valid') return 'Válido';
    if (status === 'incomplete') return 'Incompleto';
    if (status === 'invalid') return 'Inválido';
    if (status === 'inactive') return 'Inactivo';
    return 'Sin validar';
  }

  getStatusClass(parameterId: number): string {
    return `status-${this.getParameterStatus(parameterId)}`;
  }

  getMappingSetStatusLabel(status: IntegrationMappingSet['status']): string {
    const normalized = this.normalizeMappingSetStatus(status);
    switch (normalized) {
      case 'Draft': return 'Borrador';
      case 'Published': return 'Publicado';
      case 'Archived': return 'Archivado';
      default: return normalized || 'Sin estado';
    }
  }

  getMappingSetStatusClass(status: IntegrationMappingSet['status'] | number | string | null | undefined): string {
    return this.normalizeMappingSetStatus(status).toLowerCase();
  }

  isDraftStatus(status: IntegrationMappingSet['status'] | number | string | null | undefined): boolean {
    return this.normalizeMappingSetStatus(status) === 'Draft';
  }

  private normalizeMappingSetStatus(status: IntegrationMappingSet['status'] | number | string | null | undefined): string {
    if (status === null || status === undefined) return '';
    if (typeof status === 'number') {
      if (status === 1) return 'Draft';
      if (status === 2) return 'Published';
      if (status === 3) return 'Archived';
      if (status === 0) return 'Draft';
      return String(status);
    }

    const raw = String(status).trim();
    if (!raw) return '';
    const lowered = raw.toLowerCase();
    if (lowered === 'draft') return 'Draft';
    if (lowered === 'published') return 'Published';
    if (lowered === 'archived') return 'Archived';
    return raw;
  }

  getParameterIssues(parameterPath: string): ValidationResult['issues'] {
    return (this.validationResult?.issues ?? []).filter((x) => x.path === parameterPath);
  }

  getSourceOptionsByKind(kind: string | null | undefined): IntegrationSourceCatalogField[] {
    if (!kind) return [];
    const normalizedKind = this.normalizeSourceKind(kind);
    return this.sourceCatalog.filter((x) => this.normalizeSourceKind(x.sourceKind as any) === normalizedKind);
  }

  getSelectedSourceField(): IntegrationSourceCatalogField | undefined {
    const fieldId = this.ruleForm.controls.sourceCatalogFieldId.value;
    return this.sourceCatalog.find((field) => field.id === fieldId);
  }

  private resolveControlledSourceFieldPath(): string {
    const kind = this.normalizeSourceKind(this.ruleForm.value.sourceKind);
    if (kind === 'Constant') return '';
    return this.getSelectedSourceField()?.fieldPath ?? '';
  }

  private refreshSourceKindOptions(): void {
    const kinds = new Set(this.sourceCatalog.map((field) => this.normalizeSourceKind(field.sourceKind as any)).filter(Boolean));
    kinds.add('Constant');
    this.sourceKindOptions = Array.from(kinds)
      .map((value) => ({ value, label: this.getSourceKindLabel(value) }))
      .sort((a, b) => a.label.localeCompare(b.label, 'es'));
  }

  getSourceKindLabel(kind: string | null | undefined): string {
    switch (this.normalizeSourceKind(kind)) {
      case 'Transaction': return 'Transaccion';
      case 'Batch': return 'Lote operativo';
      case 'Cycle': return 'Ciclo';
      case 'ClearingHouse': return 'Camara';
      case 'Constant': return 'Constante';
      case 'Addenda': return 'Addenda NACHA';
      case 'NachaHeader': return 'Archivo NACHA';
      case 'BatchHeader': return 'Lote NACHA';
      case 'EntryDetail': return 'Detalle NACHA';
      case 'AddendaRecord': return 'Addenda NACHA';
      case 'BatchControl': return 'Control lote NACHA';
      case 'FileControl': return 'Control archivo NACHA';
      case 'FinancialInstitution': return 'Entidad financiera';
      case 'Prenotification': return 'Prenotificacion';
      case 'DifferentialResponse': return 'Respuesta diferencial';
      default: return 'No definido';
    }
  }

  private normalizeSourceKind(kind: string | number | null | undefined): string {
    if (kind === null || kind === undefined) return '';
    if (typeof kind === 'number') {
      if (kind === 1) return 'Transaction';
      if (kind === 2) return 'Addenda';
      if (kind === 3) return 'Batch';
      if (kind === 4) return 'Cycle';
      if (kind === 5) return 'ClearingHouse';
      if (kind === 6) return 'Constant';
      if (kind === 7) return 'Expression';
      if (kind === 8) return 'NachaHeader';
      if (kind === 9) return 'BatchHeader';
      if (kind === 10) return 'EntryDetail';
      if (kind === 11) return 'AddendaRecord';
      if (kind === 12) return 'BatchControl';
      if (kind === 13) return 'FileControl';
      if (kind === 14) return 'Prenotification';
      if (kind === 15) return 'DifferentialResponse';
      return String(kind);
    }

    const raw = String(kind).trim();
    if (!raw) return '';
    if (/^\d+$/.test(raw)) return this.normalizeSourceKind(Number(raw));
    const lowered = raw.toLowerCase();
    if (lowered === 'transaction') return 'Transaction';
    if (lowered === 'addenda') return 'Addenda';
    if (lowered === 'batch') return 'Batch';
    if (lowered === 'cycle') return 'Cycle';
    if (lowered === 'clearinghouse') return 'ClearingHouse';
    if (lowered === 'constant') return 'Constant';
    if (lowered === 'expression') return 'Expression';
    if (lowered === 'nachaheader' || lowered === 'nachaheaders') return 'NachaHeader';
    if (lowered === 'batchheader' || lowered === 'batchheaders') return 'BatchHeader';
    if (lowered === 'entrydetail' || lowered === 'entrydetails') return 'EntryDetail';
    if (lowered === 'addendarecord' || lowered === 'addendarecords') return 'AddendaRecord';
    if (lowered === 'batchcontrol' || lowered === 'batchcontrols') return 'BatchControl';
    if (lowered === 'filecontrol' || lowered === 'filecontrols') return 'FileControl';
    if (lowered === 'financialinstitution') return 'FinancialInstitution';
    if (lowered === 'prenotification') return 'Prenotification';
    if (lowered === 'differentialresponse') return 'DifferentialResponse';
    return raw;
  }

  private failEditorLoad<T>(message: string, error?: unknown): Observable<T> {
    return throwError(() => new Error(this.buildEditorLoadErrorMessage(message, error)));
  }

  private buildEditorLoadErrorMessage(message: string, error?: unknown): string {
    const status = this.extractHttpStatus(error);

    if (status === 401 || status === 403) {
      return `${message} La sesión no tiene permisos suficientes o expiró.`;
    }

    if (status === 404) {
      return `${message} Verifica que el identificador exista y que pertenezca a la operación solicitada.`;
    }

    if (status && status >= 500) {
      return `${message} El API respondió con error ${status}.`;
    }

    if (this.isTimeoutError(error)) {
      return `${message} Se agotó el tiempo de espera del API.`;
    }

    return message;
  }

  private getEditorLoadErrorMessage(error: unknown): string {
    if (error instanceof Error && error.message) {
      return error.message;
    }

    return 'No fue posible cargar el editor de configuración. Intenta nuevamente.';
  }

  private handleEditorLoadFailure(error: unknown): void {
    const message = this.getEditorLoadErrorMessage(error);
    console.error('[mapping-editor] loadAll:error', {
      mappingSetId: this.mappingSetId,
      methodCode: this.methodCode,
      message
    });
    this.loading = false;
    this.mappingSet = undefined;
    this.parameters = [];
    this.sourceCatalog = [];
    this.refreshSourceKindOptions();
    this.transformations = [];
    this.historyItems = [];
    this.viewState = 'error';
    this.errorMessage = message;
    this.notifications.error(this.errorMessage);
    this.cdr.detectChanges();
  }

  private extractHttpStatus(error: unknown): number | undefined {
    if (!error || typeof error !== 'object') return undefined;
    const candidate = error as { status?: unknown };
    return typeof candidate.status === 'number' ? candidate.status : undefined;
  }

  private isTimeoutError(error: unknown): boolean {
    if (!error || typeof error !== 'object') return false;
    const candidate = error as { name?: unknown };
    return candidate.name === 'TimeoutError';
  }


  get usarMuestraControlada(): boolean {
    return Boolean(this.previewForm.controls.usarMuestraControlada.value);
  }


  get totalErroresValidacion(): number {
    return (this.validationResult?.issues ?? []).filter((x) => (x.severity || '').toLowerCase() === 'error').length;
  }

  get totalAdvertenciasValidacion(): number {
    return (this.validationResult?.issues ?? []).filter((x) => (x.severity || '').toLowerCase() !== 'error').length;
  }

  get estadoPublicacion(): string {
    return this.getMappingSetStatusLabel(this.mappingSet?.status as any);
  }
  trackByParameterId(_: number, parameter: IntegrationMethodParameter): number {
    return parameter.id;
  }

  goToMappingList(): void {
    this.router.navigate(['/integraciones/mappings']);
  }
}
