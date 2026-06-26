import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
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

type MatrixStatus =
  | 'Mapeado NACHA'
  | 'Mapeado transaccional'
  | 'Mapeado por ciclo/camara'
  | 'Mapeado desde respuesta diferencial'
  | 'Constante tecnica'
  | 'Placeholder / pendiente funcional'
  | 'Opcional / reservado'
  | 'Sin mapear'
  | 'Inactivo';
type MatrixFilter = 'Todos' | 'Pendientes' | 'Bloqueantes' | 'Warnings' | 'Listos' | 'Opcionales/reservados';
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

interface SourceVisual {
  tableOrigin: string;
  fieldOrigin: string;
  sourceField: IntegrationSourceCatalogField | null;
  hasValidSource: boolean;
  sourceKind: string;
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
  private readonly cdr = inject(ChangeDetectorRef);

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
  selectedFilter: MatrixFilter = 'Todos';

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

  private readonly serviceOrder = this.serviceDescriptions.map((service) => service.operationKey);

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

    return this.mappingSets.find((set) => this.normalizeStatus(set.status) === 'Published' && set.isActive)
      ?? this.mappingSets.find((set) => this.normalizeStatus(set.status) === 'Published')
      ?? this.mappingSets.find((set) => this.normalizeStatus(set.status) === 'Draft')
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
      .filter((parameter) => this.isActiveFlag(parameter.isActive))
      .sort((a, b) => a.sortOrder - b.sortOrder)
      .map((parameter) => this.buildMatrixRow(parameter, mappingSet, rulesByParameter.get(parameter.id) ?? null));
  }

  get filteredMatrixRows(): MappingMatrixRow[] {
    return this.matrixRows.filter((row) => this.matchesSelectedFilter(row));
  }

  get allowedSourceFields(): IntegrationSourceCatalogField[] {
    return this.sourceCatalog
      .filter((field) => this.isActiveFlag(field.isActive) && this.isAllowedNachaSource(field.sourceKind))
      .sort((a, b) => this.getSourceKindLabel(a.sourceKind).localeCompare(this.getSourceKindLabel(b.sourceKind)) || a.sortOrder - b.sortOrder);
  }

  get matrixStats() {
    const rows = this.matrixRows;
    return {
      total: rows.length,
      ready: rows.filter((row) => this.isReadyRow(row)).length,
      pending: rows.filter((row) => this.isPendingRow(row)).length,
      blocking: rows.filter((row) => this.isBlockingRow(row)).length,
      warnings: rows.filter((row) => this.isWarningRow(row)).length,
      optionalReserved: rows.filter((row) => row.status === 'Opcional / reservado').length,
      mapped: rows.filter((row) => this.isMappedStatus(row.status)).length,
      unmapped: rows.filter((row) => row.status === 'Sin mapear').length,
      inactive: rows.filter((row) => row.status === 'Inactivo').length
    };
  }

  get filterOptions(): Array<{ key: MatrixFilter; label: string; count: number }> {
    const stats = this.matrixStats;
    return [
      { key: 'Todos', label: 'Todos', count: stats.total },
      { key: 'Pendientes', label: 'Pendientes', count: stats.pending },
      { key: 'Bloqueantes', label: 'Bloqueantes', count: stats.blocking },
      { key: 'Warnings', label: 'Warnings', count: stats.warnings },
      { key: 'Listos', label: 'Listos', count: stats.ready },
      { key: 'Opcionales/reservados', label: 'Opcionales/reservados', count: stats.optionalReserved }
    ];
  }

  loadMethods(): void {
    this.loading = true;
    this.api.getMethods().pipe(finalize(() => (this.loading = false))).subscribe({
      next: (items) => {
        this.methods = (items ?? [])
          .filter((method) => this.isActiveFlag(method.isActive))
          .filter((method) => this.serviceDescriptions.some((item) => item.operationKey === method.operationKey))
          .sort((a, b) => this.getServiceOrder(a.operationKey) - this.getServiceOrder(b.operationKey));
        const requestedMethodCode = this.route.snapshot.queryParamMap.get('method');
        const requestedMethod = requestedMethodCode
          ? this.methods.find((method) => method.code === requestedMethodCode || method.operationKey === requestedMethodCode)
          : undefined;
        const initialMethod = requestedMethod ?? this.methods[0];
        if (initialMethod) {
          this.createDraftForm.patchValue({ methodId: initialMethod.id });
        }
        this.refreshView();
        this.loadCatalogForSelectedMethod();
        this.loadMappingSets();
      },
      error: () => this.notifications.error('No fue posible cargar los servicios SOAP.')
    });
  }

  loadMappingSets(): void {
    const methodId = this.selectedMethodId;
    this.api.getMappingSets(methodId ?? undefined).subscribe({
      next: (items) => {
        this.mappingSets = items ?? [];
        this.refreshView();
      },
      error: () => this.notifications.error('No fue posible cargar la relacion de campos.')
    });
  }

  onMethodChange(): void {
    this.closeModal();
    this.selectedFilter = 'Todos';
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

  openAdvancedEditor(row?: MappingMatrixRow): void {
    if (row) {
      this.selectedRow = row;
      this.selectedMapping = row.mappingSet;
    }

    const mappingSet = this.selectedMapping ?? this.activeMappingSet;
    if (!mappingSet) {
      return;
    }

    this.router.navigate(['/integraciones/mappings', mappingSet.methodCode, mappingSet.id]);
  }

  setFilter(filter: MatrixFilter): void {
    this.selectedFilter = filter;
  }

  getStatusClass(status: MatrixStatus): string {
    if (status === 'Mapeado NACHA') return 'nacha';
    if (status === 'Mapeado transaccional') return 'transactional';
    if (status === 'Mapeado por ciclo/camara') return 'cycle';
    if (status === 'Mapeado desde respuesta diferencial') return 'differential';
    if (status === 'Constante tecnica') return 'constant';
    if (status === 'Placeholder / pendiente funcional') return 'placeholder';
    if (status === 'Opcional / reservado') return 'reserved';
    if (status === 'Inactivo') return 'inactive';
    return 'unmapped';
  }

  getMappingSetStatusLabel(mappingSet: IntegrationMappingSet | null): string {
    if (!mappingSet) {
      return 'Sin version de trabajo';
    }

    const normalized = this.normalizeStatus(mappingSet.status);
    if (normalized === 'Draft') return 'Borrador de trabajo';
    if (normalized === 'Published') return 'Publicado activo';
    if (normalized === 'Archived') return 'Archivado';
    return normalized || 'Sin estado';
  }

  getObservationLabel(row: MappingMatrixRow): string {
    if (row.status === 'Placeholder / pendiente funcional') {
      return 'Pendiente de definicion funcional.';
    }

    if (row.status === 'Sin mapear') {
      return row.required ? 'Requiere fuente o constante homologada.' : 'Sin relacion activa.';
    }

    if (row.status === 'Constante tecnica') {
      return 'Constante tecnica; revisar politica funcional.';
    }

    if (row.status === 'Opcional / reservado') {
      return 'Reservado por contrato; no bloquea la revision funcional.';
    }

    if (row.status === 'Inactivo') {
      return 'No participa en la version visible.';
    }

    return 'Listo con fuente funcional.';
  }

  getRowActionLabel(row: MappingMatrixRow): string {
    if (row.status === 'Sin mapear') {
      return 'Completar';
    }

    if (this.isBlockingRow(row) || this.isWarningRow(row)) {
      return 'Revisar';
    }

    return 'Ver detalle';
  }

  getSourceKindLabel(kind: string | number | null | undefined): string {
    switch (this.normalizeSourceKind(kind).toLowerCase()) {
      case 'nachaheader': return 'Archivo NACHA';
      case 'batchheader': return 'Lote NACHA';
      case 'entrydetail': return 'Detalle NACHA';
      case 'addendarecord': return 'Addenda NACHA';
      case 'batchcontrol': return 'Control lote NACHA';
      case 'filecontrol': return 'Control archivo NACHA';
      case 'differentialresponse': return 'Respuesta diferencial';
      case 'transaction': return 'Transaccion';
      case 'cycle': return 'Ciclo';
      case 'clearinghouse': return 'Camara';
      case 'constant': return 'Constante';
      case 'batch': return 'Lote operativo';
      case 'addenda': return 'Addenda NACHA';
      case 'financialinstitution': return 'Entidad financiera';
      case 'prenotification': return 'Prenotificacion';
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
      next: (items) => {
        this.transformations = items ?? [];
        this.refreshView();
      },
      error: () => {
        this.transformations = [];
        this.refreshView();
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
    let sourceLoaded = false;
    let parametersLoaded = false;
    const markReady = () => {
      if (sourceLoaded && parametersLoaded && this.catalogLoadState !== 'error') {
        this.catalogLoadState = 'ready';
        this.refreshView();
      }
    };

    this.api.getSourceCatalog(methodId).subscribe({
      next: (items) => {
        this.sourceCatalog = items ?? [];
        sourceLoaded = true;
        this.refreshView();
        markReady();
      },
      error: () => {
        this.catalogLoadState = 'error';
        this.notifications.error('No fue posible cargar la matriz de campos SOAP.');
        this.refreshView();
      }
    });

    this.api.getMethodParameters(methodId).subscribe({
      next: (items) => {
        this.targetFields = items ?? [];
        parametersLoaded = true;
        this.refreshView();
        markReady();
      },
      error: () => {
        this.catalogLoadState = 'error';
        this.notifications.error('No fue posible cargar los parametros SOAP.');
        this.refreshView();
      }
    });
  }

  private buildMatrixRow(
    parameter: IntegrationMethodParameter,
    mappingSet: IntegrationMappingSet | null,
    rule: IntegrationMappingRule | null
  ): MappingMatrixRow {
    const isInactive = !!mappingSet && (!mappingSet.isActive || this.normalizeStatus(mappingSet.status) === 'Archived' || rule?.enabled === false);
    const isOptionalReserved = !rule && this.isOptionalReservedParameter(parameter);
    const sourceVisual = this.resolveSourceVisual(rule);
    const status = this.getFunctionalMappingStatus(rule, sourceVisual, isInactive, isOptionalReserved);

    return {
      serviceName: this.selectedMethod?.operationKey ?? parameter.parameterPath.split('.')[0] ?? 'Servicio SOAP',
      parameterId: parameter.id,
      parameterSoap: parameter.parameterPath || parameter.displayName,
      parameterDescription: parameter.descriptionEs || parameter.displayName || parameter.uiHelpText || parameter.parameterPath,
      tableOrigin: isOptionalReserved ? 'Reservado por contrato' : sourceVisual.tableOrigin,
      fieldOrigin: isOptionalReserved ? 'Opcional sin fuente requerida' : sourceVisual.fieldOrigin,
      conversionRule: this.getConversionRuleLabel(rule, isOptionalReserved),
      required: rule?.requiredOverride ?? parameter.required,
      status,
      lastUpdated: this.getLastUpdatedLabel(mappingSet),
      mappingSet,
      rule,
      sourceField: sourceVisual.sourceField,
      technicalNote: this.getTechnicalNote(rule, sourceVisual, isOptionalReserved)
    };
  }

  private resolveSourceVisual(rule: IntegrationMappingRule | null): SourceVisual {
    if (!rule || rule.enabled === false) {
      return this.unmappedSourceVisual();
    }

    const normalizedKind = this.normalizeSourceKind(rule.sourceKind).toLowerCase();
    const sourceField = this.resolveSourceField(rule);
    const isConstant = normalizedKind === 'constant';
    const hasConstantValue = isConstant && (!!rule.fixedValue || !!rule.defaultValue || this.normalizeSourceFieldPath(rule.sourceFieldPath) === 'constant.value');
    const hasSourcePath = !!(rule.sourceFieldPath ?? '').trim();
    const hasValidSource = isConstant
      ? hasConstantValue
      : this.isKnownSourceKind(normalizedKind) && (!!sourceField || hasSourcePath);

    if (!hasValidSource) {
      return this.unmappedSourceVisual(sourceField);
    }

    return {
      tableOrigin: this.getSourceKindLabel(rule.sourceKind),
      fieldOrigin: this.getSourceDisplayName(rule, sourceField),
      sourceField,
      hasValidSource: true,
      sourceKind: normalizedKind
    };
  }

  private unmappedSourceVisual(sourceField: IntegrationSourceCatalogField | null = null): SourceVisual {
    return {
      tableOrigin: 'Sin mapear',
      fieldOrigin: 'Sin mapear',
      sourceField,
      hasValidSource: false,
      sourceKind: ''
    };
  }

  private getSourceDisplayName(rule: IntegrationMappingRule, sourceField: IntegrationSourceCatalogField | null): string {
    const normalizedKind = this.normalizeSourceKind(rule.sourceKind).toLowerCase();
    if (normalizedKind === 'constant') {
      if (this.isPlaceholderRule(rule)) {
        return 'Placeholder pendiente funcional';
      }

      return rule.fixedValue ? 'Valor fijo' : rule.defaultValue ? 'Valor por defecto' : 'Constante';
    }

    const sourcePath = (rule.sourceFieldPath || sourceField?.fieldPath || '').trim();
    const known = this.getKnownSourceFieldLabel(sourcePath);
    if (known) {
      return known;
    }

    return sourceField?.displayName || sourcePath || 'Fuente tecnica';
  }

  private getKnownSourceFieldLabel(sourcePath: string): string | null {
    const normalized = sourcePath.trim().toLowerCase();
    const labels: Record<string, string> = {
      'achtransaction.reference': 'Referencia',
      'transaction.reference': 'Referencia',
      'transaction.transactionexternalid': 'Id operacion cliente',
      'transaction.amount': 'Monto',
      'transaction.tracenumber': 'Trazabilidad',
      'transaction.companyidentification': 'Identificacion compania',
      'transaction.originatingdfi': 'Banco originador',
      'transaction.sourceaccountnumber': 'Cuenta origen',
      'transaction.id': 'Id transaccion',
      'achtransaction.amount': 'Monto',
      'achtransaction.tracenumber': 'Trazabilidad',
      'achtransaction.companyidentification': 'Identificacion compania',
      'achtransaction.originatingdfi': 'Banco originador',
      'batch.id': 'Id lote',
      'achbatch.id': 'Id lote',
      'cycle.id': 'Id ciclo',
      'cycle.processingdate': 'Fecha de proceso',
      'achcycle.processingdate': 'Fecha de proceso',
      'execution.datetimeutc': 'Fecha/hora ejecucion UTC',
      'execution.dateyyyymmdd': 'Fecha ejecucion yyyymmdd',
      'clearinghouse.id': 'Identificador interno',
      'clearinghouse.code': 'Codigo',
      'financialinstitution.routingnumber': 'Codigo ruta',
      'differentialresponse.idcanal': 'Canal',
      'differentialresponse.nombrecanal': 'Nombre canal',
      'differentialresponse.idtransaccion': 'Transaccion',
      'differentialresponse.idestado': 'Estado',
      'differentialresponse.codigocausalexterna': 'Causal externa',
      'differentialresponse.idtransaccionservicioexterno': 'Id transaccion servicio externo',
      'differentialresponse.descripcioncausalexterna': 'Descripcion causal externa',
      'nachaheaders.immediatedestination': 'Destino inmediato',
      'nachaheaders.immediateorigin': 'Origen inmediato',
      'nachaheaders.fileidmodifier': 'Modificador de archivo',
      'batchheaders.companyname': 'Nombre compania',
      'batchheaders.companyid': 'Identificacion compania',
      'batchheaders.companyidentification': 'Identificacion compania',
      'batchheaders.companyentrydescription': 'Descripcion entrada',
      'batchheaders.effectiveentrydate': 'Fecha efectiva',
      'entrydetails.amount': 'Monto',
      'entrydetails.accountnumber': 'Cuenta',
      'entrydetails.transactioncode': 'Codigo transaccion',
      'entrydetails.tracenumber': 'Trazabilidad',
      'addendarecords.infofromoriginator': 'Informacion de pago',
      'addendarecords.paymentrelatedinformation': 'Informacion de pago',
      'batchcontrols.entryaddendacount': 'Cantidad registros',
      'batchcontrols.entryhash': 'Hash entradas',
      'filecontrols.blockcount': 'Bloques',
      'prenotification.reference': 'Referencia prenotificacion',
      'prenotification.state': 'Estado prenotificacion',
      'addenda.addendatype': 'Tipo addenda'
    };

    return labels[normalized] ?? null;
  }

  private getConversionRuleLabel(rule: IntegrationMappingRule | null, isOptionalReserved: boolean): string {
    if (isOptionalReserved) {
      return 'Reservado / opcional';
    }

    if (!rule) {
      return 'Pendiente de definicion';
    }

    if (this.isPlaceholderRule(rule)) {
      return 'Pendiente funcional';
    }

    if (rule.fixedValue) {
      return 'Constante';
    }

    if (rule.defaultValue) {
      return 'Valor por defecto';
    }

    if (rule.transformationCode) {
      return this.getTransformationLabel(rule.transformationCode);
    }

    return 'Sin conversion';
  }

  private getTechnicalNote(rule: IntegrationMappingRule | null, sourceVisual: SourceVisual, isOptionalReserved: boolean): string {
    if (isOptionalReserved) {
      return 'Parametro contractual opcional/reservado para Proc_Contrapartidas.';
    }

    if (rule && sourceVisual.hasValidSource && this.isPlaceholderRule(rule)) {
      return 'Valor pendiente de definicion funcional; no debe tratarse como homologado.';
    }

    if (rule && sourceVisual.hasValidSource && sourceVisual.sourceKind === 'constant') {
      return 'Valor fijo o default tecnico visible para revision funcional.';
    }

    if (rule && sourceVisual.hasValidSource) {
      return 'Relacion activa con fuente funcional soportada por backend.';
    }

    if (rule && !sourceVisual.hasValidSource) {
      return 'La regla existe, pero no tiene fuente activa valida para mostrarla como mapeada.';
    }

    return 'Relacion construida desde catalogo controlado.';
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
    return ['nachaheader', 'batchheader', 'entrydetail', 'addendarecord', 'batchcontrol', 'filecontrol', 'differentialresponse']
      .includes(this.normalizeSourceKind(kind).toLowerCase());
  }

  private getFunctionalMappingStatus(
    rule: IntegrationMappingRule | null,
    sourceVisual: SourceVisual,
    isInactive: boolean,
    isOptionalReserved: boolean
  ): MatrixStatus {
    if (isInactive) {
      return 'Inactivo';
    }

    if (isOptionalReserved) {
      return 'Opcional / reservado';
    }

    if (!sourceVisual.hasValidSource) {
      return 'Sin mapear';
    }

    if (rule && this.isPlaceholderRule(rule)) {
      return 'Placeholder / pendiente funcional';
    }

    if (this.isNachaSourceKind(sourceVisual.sourceKind)) {
      return 'Mapeado NACHA';
    }

    if (this.isTransactionalSourceKind(sourceVisual.sourceKind)) {
      return 'Mapeado transaccional';
    }

    if (this.isCycleCameraSourceKind(sourceVisual.sourceKind)) {
      return 'Mapeado por ciclo/camara';
    }

    if (sourceVisual.sourceKind === 'differentialresponse') {
      return 'Mapeado desde respuesta diferencial';
    }

    if (sourceVisual.sourceKind === 'constant') {
      return 'Constante tecnica';
    }

    return 'Sin mapear';
  }

  private isMappedStatus(status: MatrixStatus): boolean {
    return status === 'Mapeado NACHA'
      || status === 'Mapeado transaccional'
      || status === 'Mapeado por ciclo/camara'
      || status === 'Mapeado desde respuesta diferencial'
      || status === 'Constante tecnica';
  }

  private matchesSelectedFilter(row: MappingMatrixRow): boolean {
    if (this.selectedFilter === 'Todos') {
      return true;
    }

    if (this.selectedFilter === 'Pendientes') {
      return this.isPendingRow(row);
    }

    if (this.selectedFilter === 'Bloqueantes') {
      return this.isBlockingRow(row);
    }

    if (this.selectedFilter === 'Warnings') {
      return this.isWarningRow(row);
    }

    if (this.selectedFilter === 'Listos') {
      return this.isReadyRow(row);
    }

    return row.status === 'Opcional / reservado';
  }

  private isReadyRow(row: MappingMatrixRow): boolean {
    return row.status === 'Mapeado NACHA'
      || row.status === 'Mapeado transaccional'
      || row.status === 'Mapeado por ciclo/camara'
      || row.status === 'Mapeado desde respuesta diferencial';
  }

  private isPendingRow(row: MappingMatrixRow): boolean {
    return row.status === 'Sin mapear' || row.status === 'Placeholder / pendiente funcional';
  }

  private isBlockingRow(row: MappingMatrixRow): boolean {
    return row.status === 'Placeholder / pendiente funcional'
      || (row.status === 'Sin mapear' && row.required);
  }

  private isWarningRow(row: MappingMatrixRow): boolean {
    return row.status === 'Constante tecnica';
  }

  private isKnownSourceKind(kind: string): boolean {
    return this.isNachaSourceKind(kind)
      || this.isTransactionalSourceKind(kind)
      || this.isCycleCameraSourceKind(kind)
      || kind === 'differentialresponse'
      || kind === 'constant';
  }

  private isNachaSourceKind(kind: string): boolean {
    return ['nachaheader', 'batchheader', 'entrydetail', 'addendarecord', 'batchcontrol', 'filecontrol', 'addenda']
      .includes(kind);
  }

  private isTransactionalSourceKind(kind: string): boolean {
    return ['transaction', 'batch', 'prenotification']
      .includes(kind);
  }

  private isCycleCameraSourceKind(kind: string): boolean {
    return ['cycle', 'clearinghouse', 'financialinstitution'].includes(kind);
  }

  private isPlaceholderRule(rule: IntegrationMappingRule | null): boolean {
    if (!rule) {
      return false;
    }

    if (this.normalizeSourceKind(rule.sourceKind).toLowerCase() === 'constant') {
      const hasConcreteValue = !!(rule.fixedValue ?? rule.defaultValue ?? '').trim();
      return hasConcreteValue
        ? [rule.fixedValue, rule.defaultValue].some((value) => this.isPlaceholderValue(value))
        : this.isPlaceholderValue(rule.sourceFieldPath);
    }

    return [rule.fixedValue, rule.defaultValue, rule.sourceFieldPath].some((value) => this.isPlaceholderValue(value));
  }

  private isPlaceholderValue(value: string | null | undefined): boolean {
    const normalized = (value ?? '').trim().toUpperCase();
    if (!normalized) {
      return false;
    }

    return new Set(['SEED', 'TEST', 'REF-1', 'CONSTANT.VALUE', '000010070', '900123456', 'ACH', '1', '1.0', '1.00', '0', '0.0', '0.00', '0.0.0.0'])
      .has(normalized);
  }

  private normalizeSourceFieldPath(path: string | null | undefined): string {
    return (path ?? '').trim().toLowerCase();
  }

  private isOptionalReservedParameter(parameter: IntegrationMethodParameter): boolean {
    const operationKey = this.selectedMethod?.operationKey ?? '';
    const parameterName = (parameter.parameterPath || parameter.displayName || '').split('.').pop() ?? '';
    const contrapartidasReservedResponseFields = ['ANSIDLOTE', 'ANSST', 'ANCLC', 'ANSIDTX', 'ANSIDREVER'];
    return operationKey === 'Proc_Contrapartidas'
      && !parameter.required
      && contrapartidasReservedResponseFields.includes(parameterName.toUpperCase());
  }

  private getServiceOrder(operationKey: string): number {
    const index = this.serviceOrder.indexOf(operationKey);
    return index === -1 ? Number.MAX_SAFE_INTEGER : index;
  }

  private isActiveFlag(value: unknown): boolean {
    if (value === false || value === 0) return false;
    if (typeof value === 'string') {
      const raw = value.trim().toLowerCase();
      return raw !== 'false' && raw !== '0' && raw !== 'inactive' && raw !== 'inactivo';
    }
    return true;
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
    if (lowered === 'nachaheaders') return 'NachaHeader';
    if (lowered === 'batchheaders') return 'BatchHeader';
    if (lowered === 'entrydetails') return 'EntryDetail';
    if (lowered === 'addendarecords') return 'AddendaRecord';
    if (lowered === 'batchcontrols') return 'BatchControl';
    if (lowered === 'filecontrols') return 'FileControl';
    if (lowered === 'differentialresponses') return 'DifferentialResponse';
    if (lowered === 'transactions' || lowered === 'achtransaction') return 'Transaction';
    if (lowered === 'cycles' || lowered === 'achcycle') return 'Cycle';
    if (lowered === 'clearinghouses') return 'ClearingHouse';
    if (lowered === 'constants') return 'Constant';
    if (lowered === 'batches' || lowered === 'achbatch') return 'Batch';
    if (lowered === 'addendas') return 'Addenda';
    if (lowered === 'financialinstitutions') return 'FinancialInstitution';
    if (lowered === 'prenotifications') return 'Prenotification';
    return raw;
  }

  private normalizeStatus(status: IntegrationMappingSet['status'] | number | string | null | undefined): string {
    if (status === null || status === undefined) return '';
    if (typeof status === 'number') {
      if (status === 1) return 'Draft';
      if (status === 2) return 'Published';
      if (status === 3) return 'Archived';
      return String(status);
    }

    const raw = String(status).trim();
    if (!raw) return '';
    if (/^\d+$/.test(raw)) return this.normalizeStatus(Number(raw));
    const lowered = raw.toLowerCase();
    if (lowered === 'draft') return 'Draft';
    if (lowered === 'published') return 'Published';
    if (lowered === 'archived') return 'Archived';
    return raw;
  }

  private refreshView(): void {
    this.cdr.detectChanges();
  }
}
