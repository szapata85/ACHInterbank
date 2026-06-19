import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import {
  IntegrationMappingAdminService,
  IntegrationMappingRule,
  IntegrationMappingSet,
  IntegrationMethod,
  IntegrationMethodParameter,
  IntegrationSourceCatalogField,
  IntegrationTransformationCatalog,
  MappingSetHistoryItem
} from '../../../core/services/integration-mapping-admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';

type MatrixStatus = 'Mapeado' | 'Sin mapear' | 'Inactivo';
type MappingModalMode = 'detail' | 'edit' | 'draft' | 'history' | null;

interface ServiceDescriptor {
  operationKey: string;
  description: string;
}

interface MappingMatrixRow {
  serviceName: string;
  parameterId: number;
  parameterSoap: string;
  parameterDescription: string;
  tableOrigin: string;
  fieldOrigin: string;
  conversionRule: string;
  required: boolean;
  status: MatrixStatus;
  lastUpdated: string;
  mappingSet: IntegrationMappingSet | null;
  rule: IntegrationMappingRule | null;
  sourceField: IntegrationSourceCatalogField | null;
  technicalNote: string;
}

@Component({
  selector: 'app-mapping-sets-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, SharedModule],
  templateUrl: './mapping-sets-page.component.html',
  styleUrls: ['./mapping-sets-page.component.scss']
})
export class MappingSetsPageComponent implements OnInit {
  private readonly api = inject(IntegrationMappingAdminService);
  private readonly notifications = inject(NotificationService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  loading = false;
  savingRule = false;
  creatingDraft = false;
  historyLoading = false;
  methods: IntegrationMethod[] = [];
  mappingSets: IntegrationMappingSet[] = [];
  sourceCatalog: IntegrationSourceCatalogField[] = [];
  targetFields: IntegrationMethodParameter[] = [];
  transformations: IntegrationTransformationCatalog[] = [];
  historyItems: MappingSetHistoryItem[] = [];
  catalogLoadState: 'idle' | 'loading' | 'ready' | 'error' = 'idle';
  modalMode: MappingModalMode = null;
  selectedRow: MappingMatrixRow | null = null;
  selectedMapping: IntegrationMappingSet | null = null;

  readonly canManage = this.auth.hasPermission('CanManageAch');

  readonly migas = [
    { etiqueta: 'Inicio', ruta: '/' },
    { etiqueta: 'Integraciones', ruta: '/integraciones' },
    { etiqueta: 'Matriz de campos SOAP' }
  ];

  readonly serviceDescriptions: ServiceDescriptor[] = [
    {
      operationKey: 'Proc_Transacciones',
      description: 'Servicio utilizado para procesar creditos monetarios recibidos desde otra entidad financiera hacia CFA.'
    },
    {
      operationKey: 'Proc_Contrapartidas',
      description: 'Servicio utilizado para procesar debitos monetarios originados por CFA hacia otra entidad financiera.'
    },
    {
      operationKey: 'RegistrarRespuestaTransaccion',
      description: 'Servicio utilizado para registrar respuestas, rechazos o notificaciones diferenciales. No realiza movimiento monetario.'
    }
  ];

  readonly createDraftForm = this.fb.group({
    methodId: [null as number | null, Validators.required],
    name: ['', [Validators.required, Validators.maxLength(120)]],
    notes: ['']
  });

  readonly editRelationForm = this.fb.group({
    sourceCatalogFieldId: [null as number | null],
    transformationCode: [''],
    requiredOverride: [false],
    enabled: [true]
  });

  ngOnInit(): void {
    this.loadMethods();
    this.loadTransformations();
  }

  get selectedMethodId(): number | null {
    return this.createDraftForm.controls.methodId.value;
  }

  get selectedMethod(): IntegrationMethod | undefined {
    return this.methods.find((x) => x.id === this.selectedMethodId);
  }

  get selectedServiceDescription(): string {
    const operationKey = this.selectedMethod?.operationKey;
    return this.getServiceDescription(operationKey);
  }

  get activeMappingSet(): IntegrationMappingSet | null {
    if (!this.mappingSets.length) {
      return null;
    }

    const draft = this.canManage
      ? this.mappingSets.find((set) => this.normalizeStatus(set.status) === 'Draft')
      : null;

    return draft
      ?? this.mappingSets.find((set) => this.normalizeStatus(set.status) === 'Published' && set.isActive)
      ?? this.mappingSets.find((set) => this.normalizeStatus(set.status) === 'Published')
      ?? this.mappingSets[0];
  }

  get matrixRows(): MappingMatrixRow[] {
    const mappingSet = this.activeMappingSet;
    const rulesByParameter = new Map<number, IntegrationMappingRule>();
    for (const rule of mappingSet?.rules ?? []) {
      const current = rulesByParameter.get(rule.parameterId);
      if (!current || rule.priority < current.priority) {
        rulesByParameter.set(rule.parameterId, rule);
      }
    }

    return [...this.targetFields]
      .filter((parameter) => parameter.isActive)
      .sort((a, b) => a.sortOrder - b.sortOrder)
      .map((parameter) => this.buildMatrixRow(parameter, mappingSet, rulesByParameter.get(parameter.id) ?? null));
  }

  get allowedSourceFields(): IntegrationSourceCatalogField[] {
    return this.sourceCatalog
      .filter((field) => field.isActive && this.isAllowedNachaSource(field.sourceKind))
      .sort((a, b) => this.getSourceKindLabel(a.sourceKind).localeCompare(this.getSourceKindLabel(b.sourceKind)) || a.sortOrder - b.sortOrder);
  }

  get matrixStats() {
    const rows = this.matrixRows;
    return {
      mapped: rows.filter((row) => row.status === 'Mapeado').length,
      unmapped: rows.filter((row) => row.status === 'Sin mapear').length,
      inactive: rows.filter((row) => row.status === 'Inactivo').length
    };
  }

  loadMethods(): void {
    this.loading = true;
    this.api.getMethods().pipe(finalize(() => (this.loading = false))).subscribe({
      next: (items) => {
        this.methods = (items ?? []).filter((method) => this.serviceDescriptions.some((item) => item.operationKey === method.operationKey));
        const requestedMethodCode = this.route.snapshot.queryParamMap.get('method');
        const requestedMethod = requestedMethodCode
          ? this.methods.find((method) => method.code === requestedMethodCode || method.operationKey === requestedMethodCode)
          : undefined;
        const initialMethod = requestedMethod ?? this.methods[0];
        if (initialMethod) {
          this.createDraftForm.patchValue({ methodId: initialMethod.id });
        }
        this.loadCatalogForSelectedMethod();
        this.loadMappingSets();
      },
      error: () => this.notifications.error('No fue posible cargar los servicios SOAP.')
    });
  }

  loadMappingSets(): void {
    const methodId = this.selectedMethodId;
    this.api.getMappingSets(methodId ?? undefined).subscribe({
      next: (items) => (this.mappingSets = items ?? []),
      error: () => this.notifications.error('No fue posible cargar la relacion de campos.')
    });
  }

  onMethodChange(): void {
    this.closeModal();
    this.loadCatalogForSelectedMethod();
    this.loadMappingSets();
  }

  openDraftModal(): void {
    if (!this.canManage) {
      return;
    }

    const method = this.selectedMethod;
    this.createDraftForm.patchValue({
      methodId: method?.id ?? null,
      name: method ? `Matriz ${method.operationKey} - borrador` : '',
      notes: 'Ajuste controlado desde matriz de campos SOAP.'
    });
    this.modalMode = 'draft';
  }

  openDetail(row: MappingMatrixRow): void {
    this.selectedRow = row;
    this.selectedMapping = row.mappingSet;
    this.modalMode = 'detail';
  }

  openHistory(row?: MappingMatrixRow): void {
    const mappingSet = row?.mappingSet ?? this.activeMappingSet;
    if (!mappingSet) {
      this.notifications.error('No hay auditoria disponible para este servicio.');
      return;
    }

    this.selectedRow = row ?? null;
    this.selectedMapping = mappingSet;
    this.historyItems = [];
    this.historyLoading = true;
    this.modalMode = 'history';
    this.api.getHistory(mappingSet.id).pipe(finalize(() => (this.historyLoading = false))).subscribe({
      next: (items) => (this.historyItems = items ?? []),
      error: () => this.notifications.error('No fue posible cargar la auditoria.')
    });
  }

  editRelation(row: MappingMatrixRow): void {
    if (!this.canManage || this.savingRule) {
      return;
    }

    this.selectedRow = row;
    const current = this.activeMappingSet;
    if (current && this.normalizeStatus(current.status) === 'Draft') {
      this.openEditForDraft(current, row.parameterId);
      return;
    }

    if (current) {
      this.savingRule = true;
      this.api.clone(current.id, `${current.name} - ajuste matriz`, 'ui-admin')
        .pipe(finalize(() => (this.savingRule = false)))
        .subscribe({
          next: (draft) => {
            this.mappingSets = [draft, ...this.mappingSets];
            this.notifications.success('Se creo un borrador para editar la relacion.');
            this.openEditForDraft(draft, row.parameterId);
          },
          error: () => this.notifications.error('No fue posible crear un borrador editable.')
        });
      return;
    }

    this.createDraftAndOpen(row.parameterId);
  }

  saveRelation(): void {
    if (!this.canManage || !this.selectedRow || !this.selectedMapping || this.savingRule) {
      return;
    }

    if (this.editRelationForm.controls.enabled.value && !this.editRelationForm.controls.sourceCatalogFieldId.value) {
      this.notifications.error('Seleccione una tabla y campo origen para activar la relacion.');
      return;
    }

    const payload = this.buildUpsertPayload(this.selectedMapping, this.selectedRow);
    this.savingRule = true;
    this.api.upsertRules(this.selectedMapping.id, 'ui-admin', payload)
      .pipe(finalize(() => (this.savingRule = false)))
      .subscribe({
        next: (updated) => {
          this.mappingSets = [updated, ...this.mappingSets.filter((set) => set.id !== updated.id)];
          this.notifications.success('Relacion de campos actualizada.');
          this.closeModal();
        },
        error: () => this.notifications.error('No fue posible guardar la relacion de campos.')
      });
  }

  closeModal(): void {
    this.modalMode = null;
    this.selectedRow = null;
    this.selectedMapping = null;
    this.historyItems = [];
  }

  createDraft(): void {
    if (!this.canManage) {
      return;
    }

    if (this.createDraftForm.invalid) {
      this.createDraftForm.markAllAsTouched();
      return;
    }

    const methodId = this.createDraftForm.controls.methodId.value!;
    const name = this.createDraftForm.controls.name.value!.trim();
    const notes = this.createDraftForm.controls.notes.value?.trim() ?? '';
    this.creatingDraft = true;
    this.api.createDraft(methodId, name, notes, 'ui-admin')
      .pipe(finalize(() => (this.creatingDraft = false)))
      .subscribe({
        next: (created) => {
          this.mappingSets = [created, ...this.mappingSets];
          this.notifications.success('Borrador de relacion de campos creado.');
          this.closeModal();
        },
        error: () => this.notifications.error('No fue posible crear el borrador.')
      });
  }

  openAdvancedEditor(): void {
    const mappingSet = this.selectedMapping ?? this.activeMappingSet;
    if (!mappingSet) {
      return;
    }

    this.router.navigate(['/integraciones/mappings', mappingSet.methodCode, mappingSet.id]);
  }

  getStatusClass(status: MatrixStatus): string {
    if (status === 'Mapeado') return 'mapped';
    if (status === 'Inactivo') return 'inactive';
    return 'unmapped';
  }

  getMappingSetStatusLabel(mappingSet: IntegrationMappingSet | null): string {
    if (!mappingSet) {
      return 'Sin version de trabajo';
    }

    const normalized = this.normalizeStatus(mappingSet.status);
    if (normalized === 'Draft') return 'Borrador';
    if (normalized === 'Published') return 'Publicado';
    if (normalized === 'Archived') return 'Archivado';
    return normalized || 'Sin estado';
  }

  getSourceKindLabel(kind: string | number | null | undefined): string {
    switch (this.normalizeSourceKind(kind).toLowerCase()) {
      case 'nachaheader': return 'NachaHeaders';
      case 'batchheader': return 'BatchHeaders';
      case 'entrydetail': return 'EntryDetails';
      case 'addendarecord': return 'AddendaRecords';
      case 'batchcontrol': return 'BatchControls';
      case 'filecontrol': return 'FileControls';
      default: return 'Sin mapear';
    }
  }

  getServiceDescription(operationKey: string | null | undefined): string {
    return this.serviceDescriptions.find((item) => item.operationKey === operationKey)?.description
      ?? 'Servicio SOAP controlado para integracion ACH.';
  }

  getTransformationLabel(code: string | null | undefined): string {
    if (!code) {
      return 'Sin regla';
    }

    const known = this.transformations.find((item) => item.code === code);
    const label = known?.displayName ?? code;
    const dictionary: Record<string, string> = {
      Trim: 'Limpiar espacios',
      Uppercase: 'Mayusculas',
      Lowercase: 'Minusculas',
      PadLeft: 'Rellenar a la izquierda',
      PadRight: 'Rellenar a la derecha',
      Substring: 'Extraer segmento',
      Concat: 'Concatenar',
      DateFormat: 'Formato de fecha',
      NumericFormat: 'Formato numerico',
      NullIfEmpty: 'Nulo si vacio',
      DefaultIfNull: 'Valor por defecto'
    };
    return dictionary[label] ?? dictionary[code] ?? label;
  }

  trackRow(_: number, row: MappingMatrixRow): number {
    return row.parameterId;
  }

  trackField(_: number, field: IntegrationSourceCatalogField): number {
    return field.id;
  }

  private loadTransformations(): void {
    this.api.getTransformations().subscribe({
      next: (items) => (this.transformations = items ?? []),
      error: () => {
        this.transformations = [];
      }
    });
  }

  private loadCatalogForSelectedMethod(): void {
    const methodId = this.selectedMethodId;
    this.sourceCatalog = [];
    this.targetFields = [];
    if (!methodId) {
      this.catalogLoadState = 'idle';
      return;
    }

    this.catalogLoadState = 'loading';
    this.api.getSourceCatalog(methodId).subscribe({
      next: (items) => {
        this.sourceCatalog = items ?? [];
        this.catalogLoadState = 'ready';
      },
      error: () => {
        this.catalogLoadState = 'error';
        this.notifications.error('No fue posible cargar las tablas origen permitidas.');
      }
    });

    this.api.getMethodParameters(methodId).subscribe({
      next: (items) => (this.targetFields = items ?? []),
      error: () => this.notifications.error('No fue posible cargar los parametros SOAP.')
    });
  }

  private buildMatrixRow(
    parameter: IntegrationMethodParameter,
    mappingSet: IntegrationMappingSet | null,
    rule: IntegrationMappingRule | null
  ): MappingMatrixRow {
    const sourceField = this.resolveSourceField(rule);
    const hasAllowedSource = !!sourceField && this.isAllowedNachaSource(sourceField.sourceKind);
    const isInactive = !!mappingSet && (!mappingSet.isActive || this.normalizeStatus(mappingSet.status) === 'Archived' || rule?.enabled === false);
    const status: MatrixStatus = isInactive ? 'Inactivo' : hasAllowedSource ? 'Mapeado' : 'Sin mapear';

    return {
      serviceName: this.selectedMethod?.operationKey ?? parameter.parameterPath.split('.')[0] ?? 'Servicio SOAP',
      parameterId: parameter.id,
      parameterSoap: parameter.displayName || parameter.parameterPath,
      parameterDescription: parameter.descriptionEs || parameter.uiHelpText || parameter.parameterPath,
      tableOrigin: hasAllowedSource ? this.getSourceKindLabel(sourceField.sourceKind) : 'Sin mapear',
      fieldOrigin: hasAllowedSource ? sourceField.displayName : 'Sin mapear',
      conversionRule: this.getTransformationLabel(rule?.transformationCode),
      required: rule?.requiredOverride ?? parameter.required,
      status,
      lastUpdated: this.getLastUpdatedLabel(mappingSet),
      mappingSet,
      rule,
      sourceField: hasAllowedSource ? sourceField : null,
      technicalNote: rule && !hasAllowedSource
        ? 'La regla existe, pero no usa una de las tablas origen permitidas para la matriz funcional.'
        : 'Relacion construida desde catalogo controlado.'
    };
  }

  private resolveSourceField(rule: IntegrationMappingRule | null): IntegrationSourceCatalogField | null {
    if (!rule) {
      return null;
    }

    if (rule.sourceCatalogFieldId) {
      const byId = this.sourceCatalog.find((field) => field.id === rule.sourceCatalogFieldId);
      if (byId) {
        return byId;
      }
    }

    const normalizedPath = (rule.sourceFieldPath ?? '').trim().toLowerCase();
    if (!normalizedPath) {
      return null;
    }

    return this.sourceCatalog.find((field) => field.fieldPath.trim().toLowerCase() === normalizedPath) ?? null;
  }

  private getLastUpdatedLabel(mappingSet: IntegrationMappingSet | null): string {
    if (!mappingSet) {
      return 'Sin version';
    }

    if (mappingSet.publishedAtUtc) {
      return new Date(mappingSet.publishedAtUtc).toLocaleString();
    }

    return this.normalizeStatus(mappingSet.status) === 'Draft' ? 'Borrador sin publicar' : 'No disponible';
  }

  private openEditForDraft(draft: IntegrationMappingSet, parameterId: number): void {
    const row = this.buildMatrixRow(
      this.targetFields.find((parameter) => parameter.id === parameterId)!,
      draft,
      draft.rules.find((rule) => rule.parameterId === parameterId) ?? null
    );
    const selectedFieldId = row.sourceField?.id ?? null;
    this.selectedMapping = draft;
    this.selectedRow = row;
    this.editRelationForm.reset({
      sourceCatalogFieldId: selectedFieldId,
      transformationCode: row.rule?.transformationCode ?? '',
      requiredOverride: row.required,
      enabled: row.rule?.enabled ?? true
    });
    this.modalMode = 'edit';
  }

  private createDraftAndOpen(parameterId: number): void {
    const method = this.selectedMethod;
    if (!method) {
      return;
    }

    this.savingRule = true;
    this.api.createDraft(method.id, `Matriz ${method.operationKey} - borrador`, 'Ajuste controlado desde matriz de campos SOAP.', 'ui-admin')
      .pipe(finalize(() => (this.savingRule = false)))
      .subscribe({
        next: (draft) => {
          this.mappingSets = [draft, ...this.mappingSets];
          this.notifications.success('Se creo un borrador para editar la relacion.');
          this.openEditForDraft(draft, parameterId);
        },
        error: () => this.notifications.error('No fue posible crear un borrador editable.')
      });
  }

  private buildUpsertPayload(mappingSet: IntegrationMappingSet, editedRow: MappingMatrixRow): Array<Record<string, unknown>> {
    const editedParameterId = editedRow.parameterId;
    const existingRules = mappingSet.rules.filter((rule) => rule.parameterId !== editedParameterId);
    const selectedField = this.allowedSourceFields.find((field) => field.id === this.editRelationForm.controls.sourceCatalogFieldId.value);
    const existingRule = mappingSet.rules.find((rule) => rule.parameterId === editedParameterId) ?? editedRow.rule;

    return [
      ...existingRules.map((rule) => this.mapRuleForUpsert(rule)),
      {
        id: existingRule?.id ?? null,
        methodId: mappingSet.methodId,
        parameterId: editedParameterId,
        sourceKind: selectedField?.sourceKind ?? existingRule?.sourceKind ?? 'NachaHeader',
        sourceCatalogFieldId: selectedField?.id ?? null,
        sourceFieldPath: selectedField?.fieldPath ?? '',
        fixedValue: existingRule?.fixedValue ?? null,
        defaultValue: existingRule?.defaultValue ?? null,
        transformationCode: this.editRelationForm.controls.transformationCode.value || null,
        formatMask: existingRule?.formatMask ?? null,
        priority: existingRule?.priority ?? 1,
        requiredOverride: this.editRelationForm.controls.requiredOverride.value,
        enabled: !!this.editRelationForm.controls.enabled.value,
        conditionExpression: null
      }
    ];
  }

  private mapRuleForUpsert(rule: IntegrationMappingRule): Record<string, unknown> {
    return {
      id: rule.id,
      methodId: rule.methodId,
      parameterId: rule.parameterId,
      sourceKind: rule.sourceKind,
      sourceCatalogFieldId: rule.sourceCatalogFieldId ?? null,
      sourceFieldPath: rule.sourceFieldPath,
      fixedValue: rule.fixedValue ?? null,
      defaultValue: rule.defaultValue ?? null,
      transformationCode: rule.transformationCode ?? null,
      formatMask: rule.formatMask ?? null,
      priority: rule.priority,
      requiredOverride: rule.requiredOverride ?? null,
      enabled: rule.enabled,
      conditionExpression: null
    };
  }

  private isAllowedNachaSource(kind: string | number | null | undefined): boolean {
    return ['nachaheader', 'batchheader', 'entrydetail', 'addendarecord', 'batchcontrol', 'filecontrol']
      .includes(this.normalizeSourceKind(kind).toLowerCase());
  }

  private normalizeSourceKind(kind: string | number | null | undefined): string {
    if (kind === null || kind === undefined) return '';
    if (typeof kind === 'number') {
      if (kind === 8) return 'NachaHeader';
      if (kind === 9) return 'BatchHeader';
      if (kind === 10) return 'EntryDetail';
      if (kind === 11) return 'AddendaRecord';
      if (kind === 12) return 'BatchControl';
      if (kind === 13) return 'FileControl';
      return String(kind);
    }

    const raw = String(kind).trim();
    if (!raw) return '';
    if (/^\d+$/.test(raw)) return this.normalizeSourceKind(Number(raw));
    const lowered = raw.toLowerCase();
    if (lowered === 'nachaheaders') return 'NachaHeader';
    if (lowered === 'batchheaders') return 'BatchHeader';
    if (lowered === 'entrydetails') return 'EntryDetail';
    if (lowered === 'addendarecords') return 'AddendaRecord';
    if (lowered === 'batchcontrols') return 'BatchControl';
    if (lowered === 'filecontrols') return 'FileControl';
    return raw;
  }

  private normalizeStatus(status: IntegrationMappingSet['status'] | number | string | null | undefined): string {
    if (status === null || status === undefined) return '';
    if (typeof status === 'number') {
      if (status === 0) return 'Draft';
      if (status === 1) return 'Published';
      if (status === 2) return 'Archived';
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
}
