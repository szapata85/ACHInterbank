import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import {
  NachaConfigApiError,
  NachaConfigLayoutField,
  NachaConfigLayoutFieldEditRequest,
  NachaConfigLayoutVariant,
  NachaConfigLayoutVariantEditRequest,
  NachaConfigFieldRule,
  NachaConfigFieldRuleEditRequest,
  NachaConfigProfileDetail,
  NachaConfigProfileReadModel,
  NachaConfigProfileRecord,
  NachaConfigValidationIssue
} from '../models/nacha-config-admin.models';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';
import {
  directionPresentation,
  fieldPresentation,
  flowPresentation,
  justificationPresentation,
  profilePresentation,
  servicePresentation,
  severityPresentation,
  sourceStrategyPresentation,
  sourceTypePresentation,
  statusPresentation,
  variantPresentation
} from '../presentation/nacha-config-presentation.catalog';

interface ProfileOption extends NachaConfigProfileReadModel {}

type DetailMode = 'variant' | 'field' | 'rules';
type SemanticState = 'configured' | 'calculated' | 'not-applicable' | 'pending' | 'blocking';

interface SemanticDetailItem {
  label: string;
  value: string;
  state: SemanticState;
  explanation: string;
  technicalValue?: boolean;
  secondaryTechnicalLabel?: string;
  secondaryTechnicalValue?: string;
}

interface TechnicalDetailItem {
  label: string;
  value: string;
}

@Component({
  selector: 'app-nacha-config-variants-fields-page',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './nacha-config-variants-fields-page.component.html',
  styleUrls: ['./nacha-config-variants-fields-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.Default
})
export class NachaConfigVariantsFieldsPageComponent implements OnInit {
  private readonly query = inject(NachaConfigQueryService);
  private readonly command = inject(NachaConfigCommandService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private detailRequestId = 0;

  readonly puedeGestionar = this.auth.hasPermission('CanManageAch');

  profiles: ProfileOption[] = [];
  selectedProfileId: number | null = null;
  selectedProfile: NachaConfigProfileDetail | null = null;
  selectedRecordCode: string | null = null;
  selectedVariantId: number | null = null;
  selectedFieldId: number | null = null;
  selectedRuleId: number | null = null;
  detailMode: DetailMode = 'field';
  fieldFilter = '';

  readonly semanticLegend: ReadonlyArray<{ state: SemanticState; label: string; explanation: string }> = [
    { state: 'configured', label: 'Configurado', explanation: 'El valor está almacenado y disponible para esta configuración.' },
    { state: 'calculated', label: 'Calculado por el sistema', explanation: 'El valor se deriva de otros datos y no se captura manualmente.' },
    { state: 'not-applicable', label: 'No aplicable', explanation: 'La configuración elegida no necesita este valor.' },
    { state: 'pending', label: 'Pendiente de configuración', explanation: 'Es un dato opcional que todavía puede completarse.' },
    { state: 'blocking', label: 'Error bloqueante', explanation: 'Falta un dato obligatorio para utilizar esta configuración.' }
  ];

  loadingProfiles = false;
  loadingDetail = false;
  savingVariant = false;
  savingField = false;
  savingRule = false;

  profilesError = '';
  detailError = '';
  variantSaveError = '';
  fieldSaveError = '';
  ruleSaveError = '';
  variantSaveIssues: NachaConfigValidationIssue[] = [];
  fieldSaveIssues: NachaConfigValidationIssue[] = [];
  ruleSaveIssues: NachaConfigValidationIssue[] = [];

  readonly variantForm = this.fb.group({
    nombreEs: ['', [Validators.required, Validators.minLength(3)]],
    descripcion: [''],
    priority: [1, [Validators.required, Validators.min(1)]],
    isDefaultForRecord: [false],
    effectiveFrom: ['', Validators.required],
    effectiveTo: ['']
  });

  readonly fieldForm = this.fb.group(
    {
      fieldNameEs: ['', [Validators.required, Validators.minLength(2)]],
      startPosition: [1, [Validators.required, Validators.min(1)]],
      length: [1, [Validators.required, Validators.min(1)]],
      propertyPath: [''],
      isEnabled: [true]
    },
    { validators: [() => this.fieldOverlapValidation()] }
  );

  readonly ruleForm = this.fb.group({
    errorCode: ['', [Validators.required, Validators.minLength(3)]],
    errorMessageEs: ['', [Validators.required, Validators.minLength(3)]],
    severity: ['ERROR', [Validators.required]],
    isEnabled: [true]
  });

  ngOnInit(): void {
    const requestedProfileId = Number(this.route.snapshot.queryParamMap.get('profileId'));
    const requestedVariantId = Number(this.route.snapshot.queryParamMap.get('variantId'));
    const requestedFieldId = Number(this.route.snapshot.queryParamMap.get('fieldId'));
    this.selectedProfileId = Number.isFinite(requestedProfileId) && requestedProfileId > 0 ? requestedProfileId : null;
    this.selectedRecordCode = this.route.snapshot.queryParamMap.get('recordCode');
    this.selectedVariantId = Number.isFinite(requestedVariantId) && requestedVariantId > 0 ? requestedVariantId : null;
    this.selectedFieldId = Number.isFinite(requestedFieldId) && requestedFieldId > 0 ? requestedFieldId : null;
    this.loadProfiles();
  }

  get estadoNormalizado(): string {
    return this.selectedProfile?.estado?.toUpperCase?.() ?? '';
  }

  get puedeEditar(): boolean {
    return this.puedeGestionar && this.estadoNormalizado === 'BORRADOR';
  }

  get selectedRecord(): NachaConfigProfileRecord | null {
    if (!this.selectedProfile || !this.selectedRecordCode) {
      return null;
    }

    return this.sortedRecords
      .find((record) => record.recordCode === this.selectedRecordCode) ?? null;
  }

  get sortedRecords(): NachaConfigProfileRecord[] {
    if (!this.selectedProfile) {
      return [];
    }

    return [...(this.selectedProfile.records ?? [])]
      .sort((a, b) => a.sequence - b.sequence || a.recordCode.localeCompare(b.recordCode));
  }

  get recordVariants(): NachaConfigLayoutVariant[] {
    if (!this.selectedProfile || !this.selectedRecordCode) {
      return [];
    }

    return [...(this.selectedProfile.variantes ?? [])]
      .filter((variant) => variant.recordCode === this.selectedRecordCode)
      .sort((a, b) => a.priority - b.priority || a.variantCode.localeCompare(b.variantCode));
  }

  get selectedVariant(): NachaConfigLayoutVariant | null {
    if (!this.selectedVariantId) {
      return null;
    }

    return this.recordVariants.find((variant) => variant.id === this.selectedVariantId) ?? null;
  }

  get selectedFields(): NachaConfigLayoutField[] {
    if (!this.selectedVariant) {
      return [];
    }

    return [...(this.selectedVariant.fields ?? [])]
      .sort((a, b) => a.startPosition - b.startPosition || a.length - b.length || a.fieldCode.localeCompare(b.fieldCode));
  }

  get selectedField(): NachaConfigLayoutField | null {
    if (!this.selectedFieldId) {
      return null;
    }

    return this.selectedFields.find((field) => field.id === this.selectedFieldId) ?? null;
  }

  get visibleFields(): NachaConfigLayoutField[] {
    const filter = this.fieldFilter.trim().toLocaleLowerCase('es');
    if (!filter) {
      return this.selectedFields;
    }

    return this.selectedFields.filter(field =>
      `${field.fieldCode} ${field.fieldNameEs} ${this.fieldFunctionalName(field)} ${field.propertyPath ?? ''}`
        .toLocaleLowerCase('es')
        .includes(filter)
    );
  }

  get selectedRules(): NachaConfigFieldRule[] {
    if (!this.selectedField) {
      return [];
    }

    return [...(this.selectedField.reglas ?? [])]
      .sort((a, b) => a.errorCode.localeCompare(b.errorCode) || a.severity.localeCompare(b.severity) || a.id - b.id);
  }

  get selectedRule(): NachaConfigFieldRule | null {
    if (!this.selectedRuleId) {
      return null;
    }

    return this.selectedRules.find((rule) => rule.id === this.selectedRuleId) ?? null;
  }

  get puedeEditarVariantActual(): boolean {
    return this.puedeEditar && !!this.selectedVariant;
  }

  get puedeEditarFieldActual(): boolean {
    return this.puedeEditar && !!this.selectedField;
  }

  get puedeEditarRuleActual(): boolean {
    return this.puedeEditar && !!this.selectedRule;
  }

  get fieldEndPosition(): number | null {
    const start = Number(this.fieldForm.controls.startPosition.value);
    const length = Number(this.fieldForm.controls.length.value);
    return Number.isFinite(start) && Number.isFinite(length) && start >= 1 && length >= 1
      ? start + length - 1
      : null;
  }

  get variantSemanticItems(): SemanticDetailItem[] {
    const variant = this.selectedVariant;
    if (!variant) {
      return [];
    }

    return [
      this.semanticItem(
        'Nombre funcional',
        this.variantFunctionalName(variant),
        'configured',
        'Nombre en español utilizado para identificar la variante.',
        false,
        'Código técnico',
        variant.variantCode
      ),
      this.semanticItem(
        'Descripción',
        variant.descripcion,
        variant.descripcion?.trim() ? 'configured' : 'pending',
        variant.descripcion?.trim()
          ? 'Describe el propósito funcional de esta variante.'
          : 'La variante funciona sin descripción, pero conviene documentar su propósito.'
      ),
      this.semanticItem('Prioridad', variant.priority, 'configured', 'Orden utilizado para evaluar variantes del mismo registro.'),
      this.semanticItem(
        'Uso predeterminado',
        variant.isDefaultForRecord ? 'Sí' : 'No',
        'configured',
        variant.isDefaultForRecord
          ? 'Es la variante de respaldo para este tipo de registro.'
          : 'Se utiliza únicamente cuando su criterio de selección aplica.'
      ),
      this.semanticItem('Longitud total', `${variant.totalLength} caracteres`, 'configured', 'Longitud fija definida para el registro NACHA-M.'),
      this.semanticItem('Vigencia inicial', this.formatDate(variant.effectiveFrom), 'configured', 'Fecha desde la que esta variante puede utilizarse.'),
      this.semanticItem(
        'Vigencia final',
        variant.effectiveTo ? this.formatDate(variant.effectiveTo) : 'Vigencia abierta',
        variant.effectiveTo ? 'configured' : 'not-applicable',
        variant.effectiveTo
          ? 'Fecha hasta la que esta variante puede utilizarse.'
          : 'No requiere fecha final mientras permanezca vigente.'
      )
    ];
  }

  get fieldSemanticItems(): SemanticDetailItem[] {
    const field = this.selectedField;
    if (!field) {
      return [];
    }

    const sourceType = field.sourceType?.trim().toUpperCase() ?? '';
    const sourceTypeValue = sourceTypePresentation(sourceType);

    return [
      this.semanticItem('Código técnico', field.fieldCode, 'configured', 'Identificador persistido del campo dentro de la variante.', true),
      this.semanticItem(
        'Nombre funcional',
        this.fieldFunctionalName(field),
        'configured',
        'Nombre en español utilizado por los administradores.'
      ),
      this.semanticItem('Posición inicial', field.startPosition, 'configured', 'Primera posición ocupada por el campo.'),
      this.semanticItem('Longitud', `${field.length} caracteres`, 'configured', 'Cantidad de caracteres reservados.'),
      this.semanticItem(
        'Posición final',
        field.startPosition + field.length - 1,
        'calculated',
        'Se obtiene sumando posición inicial y longitud, menos uno.'
      ),
      this.semanticItem(
        'Tipo de origen',
        sourceTypeValue,
        sourceType ? 'configured' : 'blocking',
        sourceType
          ? 'Indica cómo se obtiene el valor del campo.'
          : 'No existe un tipo de origen; el campo no puede resolverse.',
        false,
        sourceType ? 'Código persistido' : undefined,
        sourceType || undefined
      ),
      this.sourceDefinitionSemantic(field, sourceType),
      this.semanticItem(
        'Máscara de formato',
        field.formatMask,
        field.formatMask?.trim() ? 'configured' : 'not-applicable',
        field.formatMask?.trim()
          ? 'La máscara se aplica antes de escribir el valor.'
          : 'El campo se escribe sin una máscara adicional.',
        !!field.formatMask?.trim()
      ),
      this.semanticItem(
        'Carácter de relleno',
        this.paddingLabel(field.padChar),
        field.padChar !== null && field.padChar !== undefined ? 'configured' : 'blocking',
        'Carácter utilizado para completar la longitud fija.',
        true
      ),
      this.semanticItem(
        'Alineación',
        justificationPresentation(field.justification),
        field.justification?.trim() ? 'configured' : 'blocking',
        'Define hacia qué lado se alinea el contenido antes del relleno.',
        false,
        field.justification?.trim() ? 'Código persistido' : undefined,
        field.justification?.trim() || undefined
      ),
      this.semanticItem(
        'Transformaciones adicionales',
        field.transformationPipelineJson?.trim() ? 'Transformaciones configuradas' : 'No requiere transformaciones',
        field.transformationPipelineJson?.trim() ? 'configured' : 'not-applicable',
        field.transformationPipelineJson?.trim()
          ? 'Existe una transformación declarativa antes de generar el campo.'
          : 'El valor puede escribirse directamente con su origen y formato actuales.'
      ),
      this.semanticItem(
        'Visibilidad administrativa',
        field.isVisibleInBackoffice === false ? 'Oculto en la consola administrativa' : 'Visible en la consola administrativa',
        field.isVisibleInBackoffice === null || field.isVisibleInBackoffice === undefined ? 'pending' : 'configured',
        'Controla la visibilidad operativa del campo, no su presencia en el archivo.'
      ),
      this.semanticItem(
        'Estado',
        field.isEnabled ? 'Habilitado' : 'Inactivo',
        'configured',
        field.isEnabled ? 'El campo participa en la variante.' : 'El campo se conserva, pero no participa actualmente.'
      )
    ];
  }

  get fieldTechnicalItems(): TechnicalDetailItem[] {
    const field = this.selectedField;
    if (!field) {
      return [];
    }

    return [
      { label: 'Código del campo', value: field.fieldCode },
      { label: 'Ruta técnica del dato', value: field.propertyPath?.trim() ?? '' },
      { label: 'Código del tipo de origen', value: field.sourceType?.trim() ?? '' },
      { label: 'Entidad configurada', value: field.entityName?.trim() ?? '' },
      { label: 'Objeto de base de datos', value: field.sqlObjectName?.trim() ?? '' },
      { label: 'Expresión de cálculo', value: field.expressionDsl?.trim() ?? '' },
      { label: 'Código de catálogo externo', value: field.externalCatalogCode?.trim() ?? '' },
      { label: 'Máscara de formato', value: field.formatMask?.trim() ?? '' },
      { label: 'Carácter de relleno', value: field.padChar ?? '' },
      { label: 'Código de alineación', value: field.justification?.trim() ?? '' },
      { label: 'Transformaciones (JSON)', value: field.transformationPipelineJson?.trim() ?? '' },
      { label: 'Política alternativa (JSON)', value: field.fallbackPolicyJson?.trim() ?? '' }
    ].filter(item => item.value !== '');
  }

  loadProfiles(): void {
    this.loadingProfiles = true;
    this.profilesError = '';

    this.query.perfilesReadOnly()
      .pipe(finalize(() => {
        this.loadingProfiles = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (profiles) => {
          this.profiles = [...(profiles ?? [])].sort((a, b) => a.profileCode.localeCompare(b.profileCode));
          if (this.profiles.length === 0) {
            this.clearWorkspace();
            return;
          }

          const nextProfileId = this.resolveProfileSelection(this.selectedProfileId);
          this.selectedProfileId = nextProfileId;
          this.loadDetail(nextProfileId, undefined, undefined, undefined);
          this.cdr.markForCheck();
        },
        error: () => {
          this.profiles = [];
          this.clearWorkspace();
          this.profilesError = 'No fue posible cargar los perfiles oficiales de configuración NACHA-M.';
          this.notifications.error(this.profilesError);
          this.cdr.markForCheck();
        }
      });
  }

  onProfileChange(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    if (!Number.isFinite(value) || value <= 0 || value === this.selectedProfileId) {
      return;
    }

    this.selectedProfileId = value;
    this.detailMode = 'field';
    this.fieldFilter = '';
    this.loadDetail(value, undefined, undefined, undefined);
  }

  onRecordChange(event: Event): void {
    this.onSelectRecord((event.target as HTMLSelectElement).value);
  }

  onVariantChange(event: Event): void {
    this.onSelectVariant(Number((event.target as HTMLSelectElement).value));
  }

  onFieldFilter(event: Event): void {
    this.fieldFilter = (event.target as HTMLInputElement).value;
  }

  showDetail(mode: DetailMode): void {
    this.detailMode = mode;
  }

  onSelectRecord(recordCode: string): void {
    if (!recordCode || recordCode === this.selectedRecordCode) {
      return;
    }

    this.selectedRecordCode = recordCode;
    const variants = this.recordVariants;
    this.selectedVariantId = variants.some((variant) => variant.id === this.selectedVariantId)
      ? this.selectedVariantId
      : variants[0]?.id ?? null;
    this.selectedFieldId = this.selectedFields.some((field) => field.id === this.selectedFieldId)
      ? this.selectedFieldId
      : this.selectedFields[0]?.id ?? null;
    this.selectedRuleId = this.selectedRules.some((rule) => rule.id === this.selectedRuleId)
      ? this.selectedRuleId
      : this.selectedRules[0]?.id ?? null;
    this.detailMode = 'variant';
    this.fieldFilter = '';
    this.syncForms();
    this.syncContextUrl();
  }

  onSelectVariant(variantId: number): void {
    if (!variantId || variantId === this.selectedVariantId) {
      return;
    }

    this.selectedVariantId = variantId;
    this.selectedFieldId = this.selectedFields.some((field) => field.id === this.selectedFieldId)
      ? this.selectedFieldId
      : this.selectedFields[0]?.id ?? null;
    this.selectedRuleId = this.selectedRules.some((rule) => rule.id === this.selectedRuleId)
      ? this.selectedRuleId
      : this.selectedRules[0]?.id ?? null;
    this.detailMode = 'variant';
    this.fieldFilter = '';
    this.syncForms();
    this.syncContextUrl();
  }

  onSelectField(fieldId: number): void {
    if (!fieldId || fieldId === this.selectedFieldId) {
      return;
    }

    this.selectedFieldId = fieldId;
    this.selectedRuleId = this.selectedRules.some((rule) => rule.id === this.selectedRuleId)
      ? this.selectedRuleId
      : this.selectedRules[0]?.id ?? null;
    this.detailMode = 'field';
    this.syncForms();
    this.syncContextUrl();
  }

  onSelectRule(ruleId: number): void {
    if (!ruleId || ruleId === this.selectedRuleId) {
      return;
    }

    this.selectedRuleId = ruleId;
    this.detailMode = 'rules';
    this.syncForms();
    this.syncContextUrl();
  }

  guardarVariant(): void {
    if (!this.puedeEditarVariantActual || !this.selectedProfile || !this.selectedVariant || this.savingVariant) {
      return;
    }

    if (this.variantForm.invalid) {
      this.variantForm.markAllAsTouched();
      return;
    }

    this.variantSaveError = '';
    this.variantSaveIssues = [];
    this.savingVariant = true;

    const form = this.variantForm.getRawValue();
    const payload: NachaConfigLayoutVariantEditRequest = {
      nombreEs: this.normalizeText(form.nombreEs),
      descripcion: this.normalizeNullableText(form.descripcion),
      priority: Number(form.priority ?? 0),
      isDefaultForRecord: Boolean(form.isDefaultForRecord),
      effectiveFrom: this.normalizeText(form.effectiveFrom),
      effectiveTo: this.normalizeNullableText(form.effectiveTo),
      expectedRowVersion: this.selectedProfile.rowVersion
    };

    this.command.actualizarVariante(this.selectedProfile.id, this.selectedVariant.id, payload as unknown as Record<string, unknown>)
      .pipe(finalize(() => {
        this.savingVariant = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: () => {
          this.notifications.success('Variante actualizada correctamente.');
          this.reloadDetailAfterSave();
        },
        error: (error) => this.handleSaveError(error, 'variant')
      });
  }

  guardarField(): void {
    if (!this.puedeEditarFieldActual || !this.selectedProfile || !this.selectedField || this.savingField) {
      return;
    }

    if (this.fieldForm.invalid) {
      this.fieldForm.markAllAsTouched();
      return;
    }

    this.fieldSaveError = '';
    this.fieldSaveIssues = [];
    this.savingField = true;

    const form = this.fieldForm.getRawValue();
    const payload: NachaConfigLayoutFieldEditRequest = {
      fieldNameEs: this.normalizeText(form.fieldNameEs),
      startPosition: Number(form.startPosition ?? 0),
      length: Number(form.length ?? 0),
      propertyPath: this.normalizeNullableText(form.propertyPath),
      isEnabled: Boolean(form.isEnabled),
      expectedRowVersion: this.selectedProfile.rowVersion
    };

    this.command.actualizarField(this.selectedProfile.id, this.selectedField.id, payload as unknown as Record<string, unknown>)
      .pipe(finalize(() => {
        this.savingField = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: () => {
          this.notifications.success('Campo actualizado correctamente.');
          this.reloadDetailAfterSave();
        },
        error: (error) => this.handleSaveError(error, 'field')
      });
  }

  guardarRule(): void {
    if (!this.puedeEditarRuleActual || !this.selectedProfile || !this.selectedRule || this.savingRule) {
      return;
    }

    if (this.ruleForm.invalid) {
      this.ruleForm.markAllAsTouched();
      return;
    }

    this.ruleSaveError = '';
    this.ruleSaveIssues = [];
    this.savingRule = true;

    const form = this.ruleForm.getRawValue();
    const payload: NachaConfigFieldRuleEditRequest = {
      errorCode: this.normalizeText(form.errorCode),
      errorMessageEs: this.normalizeText(form.errorMessageEs),
      severity: this.normalizeText(form.severity).toUpperCase() as 'ERROR' | 'WARN',
      isEnabled: Boolean(form.isEnabled),
      expectedRowVersion: this.selectedProfile.rowVersion
    };

    this.command.actualizarRule(this.selectedProfile.id, this.selectedRule.id, payload as unknown as Record<string, unknown>)
      .pipe(finalize(() => {
        this.savingRule = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: () => {
          this.notifications.success('Regla actualizada correctamente.');
          this.reloadDetailAfterSave();
        },
        error: (error) => this.handleSaveError(error, 'rule')
      });
  }

  irADetallePerfil(): void {
    if (!this.selectedProfileId) {
      return;
    }

    void this.router.navigate(['/nacha-config-admin/perfiles', this.selectedProfileId]);
  }

  irARecords(): void {
    void this.router.navigate(['/nacha-config-admin/records'], {
      queryParams: { profileId: this.selectedProfileId }
    });
  }

  cancelarVariant(): void {
    this.syncVariantForm();
    this.variantSaveError = '';
    this.variantSaveIssues = [];
  }

  cancelarField(): void {
    this.syncFieldForm();
    this.fieldSaveError = '';
    this.fieldSaveIssues = [];
  }

  cancelarRule(): void {
    this.syncRuleForm();
    this.ruleSaveError = '';
    this.ruleSaveIssues = [];
  }

  recargarDetalleActual(): void {
    if (!this.selectedProfileId) {
      return;
    }

    this.loadDetail(this.selectedProfileId, this.selectedRecordCode, this.selectedVariantId, this.selectedFieldId, this.selectedRuleId);
  }

  trackByProfileId(_: number, profile: ProfileOption): number {
    return profile.profileId;
  }

  trackByRecordId(_: number, record: NachaConfigProfileRecord): number {
    return record.id;
  }

  trackByVariantId(_: number, variant: NachaConfigLayoutVariant): number {
    return variant.id;
  }

  trackByFieldId(_: number, field: NachaConfigLayoutField): number {
    return field.id;
  }

  trackByRuleId(_: number, rule: NachaConfigFieldRule): number {
    return rule.id;
  }

  isSelectedRecord(record: NachaConfigProfileRecord): boolean {
    return record.recordCode === this.selectedRecordCode;
  }

  isSelectedVariant(variant: NachaConfigLayoutVariant): boolean {
    return variant.id === this.selectedVariantId;
  }

  isSelectedField(field: NachaConfigLayoutField): boolean {
    return field.id === this.selectedFieldId;
  }

  isSelectedRule(rule: NachaConfigFieldRule): boolean {
    return rule.id === this.selectedRuleId;
  }

  formatDate(value?: string | null): string {
    return value ? new Date(value).toLocaleDateString('es-CO') : 'Fecha no disponible';
  }

  fieldValue(field: NachaConfigLayoutField): string {
    return field.propertyPath?.trim()
      || this.sourceDefinitionSemantic(field, field.sourceType?.toUpperCase() ?? '').value;
  }

  fieldRulesCount(field: NachaConfigLayoutField): number {
    return field.reglas?.length ?? 0;
  }

  sourceStrategyLabel(value: string): string {
    return sourceStrategyPresentation(value);
  }

  semanticStateLabel(state: SemanticState): string {
    return this.semanticLegend.find(item => item.state === state)?.label ?? state;
  }

  recordDescription(code: string): string {
    const names: Record<string, string> = {
      '1': 'Cabecera de archivo',
      '5': 'Cabecera de lote',
      '6': 'Detalle de transacción',
      '7': 'Información adicional',
      '8': 'Control de lote',
      '9': 'Control de archivo'
    };
    return names[code] ?? 'Registro configurado';
  }

  profileFunctionalName(profile: Pick<NachaConfigProfileReadModel, 'profileCode' | 'profileName'> | NachaConfigProfileDetail): string {
    const persistedName = 'profileName' in profile ? profile.profileName : profile.nombreEs;
    return profilePresentation(profile.profileCode, persistedName).functionalName;
  }

  variantFunctionalName(variant: NachaConfigLayoutVariant): string {
    return variantPresentation(variant.variantCode, variant.nombreEs, variant.recordCode).functionalName;
  }

  fieldFunctionalName(field: NachaConfigLayoutField): string {
    return fieldPresentation(field.fieldCode, field.fieldNameEs).functionalName;
  }

  statusLabel(value?: string | null): string {
    return statusPresentation(value);
  }

  flowLabel(value?: string | null): string {
    return flowPresentation(value);
  }

  directionLabel(value?: string | null): string {
    return directionPresentation(value);
  }

  serviceLabel(value?: string | null): string {
    return servicePresentation(value);
  }

  sourceTypeLabel(value?: string | null): string {
    return sourceTypePresentation(value);
  }

  severityLabel(value?: string | null): string {
    return severityPresentation(value);
  }

  private loadDetail(
    profileId: number,
    preferredRecordCode?: string | null,
    preferredVariantId?: number | null,
    preferredFieldId?: number | null,
    preferredRuleId?: number | null
  ): void {
    const requestId = ++this.detailRequestId;
    this.loadingDetail = true;
    this.detailError = '';
    this.variantSaveError = '';
    this.fieldSaveError = '';
    this.ruleSaveError = '';
    this.variantSaveIssues = [];
    this.fieldSaveIssues = [];
    this.ruleSaveIssues = [];

    this.query.detalle(profileId)
      .pipe(finalize(() => {
        if (requestId === this.detailRequestId) {
          this.loadingDetail = false;
          this.cdr.markForCheck();
        }
      }))
      .subscribe({
        next: (detail) => {
          if (requestId !== this.detailRequestId) {
            return;
          }
          this.selectedProfile = detail;
          this.selectedProfileId = detail.id;
          this.reconcileSelection(detail, preferredRecordCode, preferredVariantId, preferredFieldId, preferredRuleId ?? this.selectedRuleId);
          this.syncForms();
          this.syncContextUrl();
          this.cdr.markForCheck();
        },
        error: () => {
          if (requestId !== this.detailRequestId) {
            return;
          }
          this.selectedProfile = null;
          this.clearWorkspaceSelection();
          this.detailError = 'No fue posible cargar el detalle del perfil de configuración NACHA-M.';
          this.notifications.error(this.detailError);
          this.cdr.markForCheck();
        }
      });
  }

  private reloadDetailAfterSave(): void {
    if (!this.selectedProfileId) {
      return;
    }

    this.loadDetail(
      this.selectedProfileId,
      this.selectedRecordCode,
      this.selectedVariantId,
      this.selectedFieldId,
      this.selectedRuleId
    );
  }

  private reconcileSelection(
    detail: NachaConfigProfileDetail,
    preferredRecordCode?: string | null,
    preferredVariantId?: number | null,
    preferredFieldId?: number | null,
    preferredRuleId?: number | null
  ): void {
    const records = [...(detail.records ?? [])].sort((a, b) => a.sequence - b.sequence || a.recordCode.localeCompare(b.recordCode));
    const nextRecordCode = this.pickRecordCode(records, preferredRecordCode ?? this.selectedRecordCode);
    this.selectedRecordCode = nextRecordCode;

    const variants = this.variantsForRecordCode(detail, nextRecordCode);
    const nextVariantId = this.pickVariantId(variants, preferredVariantId ?? this.selectedVariantId);
    this.selectedVariantId = nextVariantId;

    const fields = this.fieldsForVariantId(detail, nextVariantId);
    const nextFieldId = this.pickFieldId(fields, preferredFieldId ?? this.selectedFieldId);
    this.selectedFieldId = nextFieldId;

    const rules = this.rulesForFieldId(detail, nextFieldId);
    const nextRuleId = this.pickRuleId(rules, preferredRuleId ?? this.selectedRuleId);
    this.selectedRuleId = nextRuleId;
  }

  private pickRecordCode(records: NachaConfigProfileRecord[], preferred?: string | null): string | null {
    if (preferred && records.some((record) => record.recordCode === preferred)) {
      return preferred;
    }

    return records[0]?.recordCode ?? null;
  }

  private pickVariantId(variants: NachaConfigLayoutVariant[], preferred?: number | null): number | null {
    if (preferred && variants.some((variant) => variant.id === preferred)) {
      return preferred;
    }

    return variants[0]?.id ?? null;
  }

  private pickFieldId(fields: NachaConfigLayoutField[], preferred?: number | null): number | null {
    if (preferred && fields.some((field) => field.id === preferred)) {
      return preferred;
    }

    return fields[0]?.id ?? null;
  }

  private pickRuleId(rules: NachaConfigFieldRule[], preferred?: number | null): number | null {
    if (preferred && rules.some((rule) => rule.id === preferred)) {
      return preferred;
    }

    return rules[0]?.id ?? null;
  }

  private variantsForRecordCode(detail: NachaConfigProfileDetail, recordCode: string | null): NachaConfigLayoutVariant[] {
    if (!recordCode) {
      return [];
    }

    return [...(detail.variantes ?? [])]
      .filter((variant) => variant.recordCode === recordCode)
      .sort((a, b) => a.priority - b.priority || a.variantCode.localeCompare(b.variantCode));
  }

  private fieldsForVariantId(detail: NachaConfigProfileDetail, variantId: number | null): NachaConfigLayoutField[] {
    if (!variantId) {
      return [];
    }

    const variant = [...(detail.variantes ?? [])].find((item) => item.id === variantId);
    return variant
      ? [...(variant.fields ?? [])].sort((a, b) => a.startPosition - b.startPosition || a.length - b.length || a.fieldCode.localeCompare(b.fieldCode))
      : [];
  }

  private rulesForFieldId(detail: NachaConfigProfileDetail, fieldId: number | null): NachaConfigFieldRule[] {
    if (!fieldId) {
      return [];
    }

    const field = [...(detail.variantes ?? [])]
      .reduce<NachaConfigLayoutField[]>((acc, variant) => acc.concat(variant.fields ?? []), [])
      .find((item) => item.id === fieldId);

    return [...(field?.reglas ?? [])]
      .sort((a, b) => a.errorCode.localeCompare(b.errorCode) || a.severity.localeCompare(b.severity) || a.id - b.id);
  }

  private syncForms(): void {
    this.syncVariantForm();
    this.syncFieldForm();
    this.syncRuleForm();
  }

  private syncVariantForm(): void {
    if (!this.selectedVariant) {
      this.variantForm.reset(
        {
          nombreEs: '',
          descripcion: '',
          priority: 1,
          isDefaultForRecord: false,
          effectiveFrom: '',
          effectiveTo: ''
        },
        { emitEvent: false }
      );
      this.variantForm.disable({ emitEvent: false });
      return;
    }

    this.variantForm.reset(
      {
        nombreEs: this.selectedVariant.nombreEs,
        descripcion: this.selectedVariant.descripcion ?? '',
        priority: this.selectedVariant.priority,
        isDefaultForRecord: this.selectedVariant.isDefaultForRecord,
        effectiveFrom: this.dateInputValue(this.selectedVariant.effectiveFrom),
        effectiveTo: this.dateInputValue(this.selectedVariant.effectiveTo)
      },
      { emitEvent: false }
    );

    if (this.puedeEditarVariantActual) {
      this.variantForm.enable({ emitEvent: false });
    } else {
      this.variantForm.disable({ emitEvent: false });
    }
  }

  private syncFieldForm(): void {
    if (!this.selectedField) {
      this.fieldForm.reset(
        {
          fieldNameEs: '',
          startPosition: 1,
          length: 1,
          propertyPath: '',
          isEnabled: true
        },
        { emitEvent: false }
      );
      this.fieldForm.disable({ emitEvent: false });
      return;
    }

    this.fieldForm.reset(
      {
        fieldNameEs: this.selectedField.fieldNameEs,
        startPosition: this.selectedField.startPosition,
        length: this.selectedField.length,
        propertyPath: this.selectedField.propertyPath ?? '',
        isEnabled: this.selectedField.isEnabled
      },
      { emitEvent: false }
    );

    if (this.puedeEditarFieldActual) {
      this.fieldForm.enable({ emitEvent: false });
    } else {
      this.fieldForm.disable({ emitEvent: false });
    }
  }

  private syncRuleForm(): void {
    if (!this.selectedRule) {
      this.ruleForm.reset(
        {
          errorCode: '',
          errorMessageEs: '',
          severity: 'ERROR',
          isEnabled: true
        },
        { emitEvent: false }
      );
      this.ruleForm.disable({ emitEvent: false });
      return;
    }

    this.ruleForm.reset(
      {
        errorCode: this.selectedRule.errorCode,
        errorMessageEs: this.selectedRule.errorMessageEs,
        severity: this.selectedRule.severity,
        isEnabled: this.selectedRule.isEnabled
      },
      { emitEvent: false }
    );

    if (this.puedeEditarRuleActual) {
      this.ruleForm.enable({ emitEvent: false });
    } else {
      this.ruleForm.disable({ emitEvent: false });
    }
  }

  private clearWorkspace(): void {
    this.selectedProfileId = null;
    this.selectedProfile = null;
    this.clearWorkspaceSelection();
    this.syncForms();
  }

  private clearWorkspaceSelection(): void {
    this.selectedRecordCode = null;
    this.selectedVariantId = null;
    this.selectedFieldId = null;
    this.selectedRuleId = null;
  }

  private handleSaveError(error: unknown, target: 'variant' | 'field' | 'rule'): void {
    const apiError = error as Partial<NachaConfigApiError> | null;
    const targetName = target === 'variant' ? 'variante' : target === 'field' ? 'campo' : 'regla';
    const message = apiError?.errorCode === 'CONCURRENCY_CONFLICT' || apiError?.errorCode?.includes('409')
      ? 'El perfil cambió mientras lo editabas. Recarga los datos e intenta nuevamente.'
      : apiError?.issues?.length
        ? `No fue posible guardar la ${targetName}. Revisa las observaciones e intenta nuevamente.`
        : `No fue posible guardar la ${targetName}. Revisa los datos e intenta nuevamente.`;
    const issues = apiError?.issues ?? [];

    if (target === 'variant') {
      this.variantSaveError = message;
      this.variantSaveIssues = issues;
    } else if (target === 'field') {
      this.fieldSaveError = message;
      this.fieldSaveIssues = issues;
    } else {
      this.ruleSaveError = message;
      this.ruleSaveIssues = issues;
    }

    this.notifications.error(message);
  }

  private resolveProfileSelection(preferredProfileId: number | null): number {
    if (preferredProfileId && this.profiles.some((profile) => profile.profileId === preferredProfileId)) {
      return preferredProfileId;
    }

    return this.profiles[0].profileId;
  }

  private fieldOverlapValidation(): ValidationErrors | null {
    const start = Number(this.fieldForm?.controls.startPosition.value);
    const length = Number(this.fieldForm?.controls.length.value);
    if (!Number.isFinite(start) || !Number.isFinite(length) || start < 1 || length < 1) {
      return null;
    }
    const end = start + length - 1;
    const overlaps = this.selectedFields
      .filter((field) => field.id !== this.selectedFieldId)
      .some((field) => {
        const fieldEnd = field.startPosition + field.length - 1;
        return start <= fieldEnd && end >= field.startPosition;
      });
    return overlaps ? { overlap: true } : null;
  }

  private syncContextUrl(): void {
    if (!this.selectedProfileId) {
      return;
    }
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        profileId: this.selectedProfileId,
        recordCode: this.selectedRecordCode,
        variantId: this.selectedVariantId,
        fieldId: this.selectedFieldId
      },
      replaceUrl: true
    });
  }

  private dateInputValue(value?: string | null): string {
    return value ? new Date(value).toISOString().slice(0, 10) : '';
  }

  private normalizeText(value: unknown): string {
    return typeof value === 'string' ? value.trim() : `${value ?? ''}`.trim();
  }

  private normalizeNullableText(value: unknown): string | null {
    const text = this.normalizeText(value);
    return text.length > 0 ? text : null;
  }

  private semanticItem(
    label: string,
    rawValue: unknown,
    state: SemanticState,
    explanation: string,
    technicalValue = false,
    secondaryTechnicalLabel?: string,
    secondaryTechnicalValue?: string
  ): SemanticDetailItem {
    const value = rawValue === null || rawValue === undefined || `${rawValue}`.trim() === ''
      ? this.semanticStateLabel(state)
      : `${rawValue}`;
    return {
      label,
      value,
      state,
      explanation,
      technicalValue,
      secondaryTechnicalLabel,
      secondaryTechnicalValue
    };
  }

  private sourceDefinitionSemantic(field: NachaConfigLayoutField, sourceType: string): SemanticDetailItem {
    if (sourceType === 'CONSTANTE') {
      return this.semanticItem(
        'Definición de origen',
        field.constantValue,
        field.constantValue !== null && field.constantValue !== undefined ? 'configured' : 'blocking',
        field.constantValue !== null && field.constantValue !== undefined
          ? 'El campo utiliza el valor constante almacenado.'
          : 'Un origen constante necesita un valor definido.',
        field.constantValue !== null && field.constantValue !== undefined
      );
    }

    if (sourceType === 'ENTIDAD') {
      const path = field.propertyPath?.trim();
      const qualifiedPath = path
        ? field.entityName?.trim() && !path.startsWith(`${field.entityName.trim()}.`)
          ? `${field.entityName}.${path}`
          : path
        : '';
      return this.semanticItem(
        'Ruta técnica del dato',
        qualifiedPath,
        qualifiedPath ? 'configured' : 'blocking',
        qualifiedPath
          ? field.entityName?.trim()
            ? 'La propiedad se obtiene de la entidad indicada.'
            : 'La propiedad se resuelve sobre el contexto del registro; no requiere una entidad fija.'
          : 'Un origen de entidad necesita una ruta técnica del dato.',
        true
      );
    }

    if (sourceType === 'EXPRESION') {
      return this.semanticItem(
        'Definición de origen',
        field.expressionDsl?.trim() ? 'Expresión declarativa definida' : '',
        field.expressionDsl?.trim() ? 'calculated' : 'blocking',
        field.expressionDsl?.trim()
          ? 'El sistema calcula el valor evaluando la expresión configurada.'
          : 'Un origen calculado necesita una expresión declarativa.'
      );
    }

    if (sourceType === 'SQL_VIEW' || sourceType === 'SQL_PROCEDURE') {
      return this.semanticItem(
        'Objeto SQL de origen',
        field.sqlObjectName,
        field.sqlObjectName?.trim() ? 'configured' : 'blocking',
        field.sqlObjectName?.trim()
          ? 'El valor se consulta desde el objeto SQL configurado.'
          : 'El tipo de origen SQL necesita un objeto configurado.',
        !!field.sqlObjectName?.trim()
      );
    }

    if (field.externalCatalogCode?.trim()) {
      return this.semanticItem(
        'Catálogo externo',
        field.externalCatalogCode,
        'configured',
        'El valor se resuelve mediante el catálogo configurado.',
        true
      );
    }

    return this.semanticItem(
      'Definición de origen',
      '',
      sourceType ? 'pending' : 'blocking',
      sourceType
        ? 'El tipo de origen existe, pero su detalle todavía no está configurado.'
        : 'Sin tipo ni definición de origen el campo no puede resolverse.'
    );
  }

  private paddingLabel(value?: string | null): string {
    if (value === ' ') {
      return 'Espacio';
    }
    return value?.length ? value : '';
  }

}
