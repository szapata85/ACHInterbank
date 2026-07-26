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

interface ProfileOption extends NachaConfigProfileReadModel {}

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
    this.loadDetail(value, undefined, undefined, undefined);
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
    this.syncForms();
    this.syncContextUrl();
  }

  onSelectRule(ruleId: number): void {
    if (!ruleId || ruleId === this.selectedRuleId) {
      return;
    }

    this.selectedRuleId = ruleId;
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
    return value ? new Date(value).toLocaleDateString('es-CO') : '-';
  }

  fieldValue(field: NachaConfigLayoutField): string {
    return field.propertyPath?.trim() || 'Sin ruta técnica';
  }

  fieldRulesCount(field: NachaConfigLayoutField): number {
    return field.reglas?.length ?? 0;
  }

  sourceStrategyLabel(value: string): string {
    return value === 'TABLE_DRIVEN' ? 'Configuración parametrizada' : value;
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
        effectiveFrom: this.dateInputValue(this.selectedProfile?.effectiveFrom),
        effectiveTo: this.dateInputValue(this.selectedProfile?.effectiveTo)
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
}
