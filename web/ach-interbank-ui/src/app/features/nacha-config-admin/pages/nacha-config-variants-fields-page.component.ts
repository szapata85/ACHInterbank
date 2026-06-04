import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
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
  private readonly fb = inject(FormBuilder);

  readonly puedeGestionar = this.auth.hasPermission('CanManageAch');

  profiles: ProfileOption[] = [];
  selectedProfileId: number | null = null;
  selectedProfile: NachaConfigProfileDetail | null = null;
  selectedRecordCode: string | null = null;
  selectedVariantId: number | null = null;
  selectedFieldId: number | null = null;

  loadingProfiles = false;
  loadingDetail = false;
  savingVariant = false;
  savingField = false;

  profilesError = '';
  detailError = '';
  variantSaveError = '';
  fieldSaveError = '';
  variantSaveIssues: NachaConfigValidationIssue[] = [];
  fieldSaveIssues: NachaConfigValidationIssue[] = [];

  readonly variantForm = this.fb.group({
    nombreEs: ['', [Validators.required, Validators.minLength(3)]],
    descripcion: [''],
    priority: [1, [Validators.required, Validators.min(1)]],
    isDefaultForRecord: [false],
    effectiveFrom: ['', Validators.required],
    effectiveTo: ['']
  });

  readonly fieldForm = this.fb.group({
    fieldNameEs: ['', [Validators.required, Validators.minLength(2)]],
    startPosition: [1, [Validators.required, Validators.min(1)]],
    length: [1, [Validators.required, Validators.min(1)]],
    propertyPath: [''],
    isEnabled: [true]
  });

  ngOnInit(): void {
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

  get puedeEditarVariantActual(): boolean {
    return this.puedeEditar && !!this.selectedVariant;
  }

  get puedeEditarFieldActual(): boolean {
    return this.puedeEditar && !!this.selectedField;
  }

  loadProfiles(): void {
    this.loadingProfiles = true;
    this.profilesError = '';

    this.query.perfilesReadOnly()
      .pipe(finalize(() => {
        this.loadingProfiles = false;
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
        },
        error: () => {
          this.profiles = [];
          this.clearWorkspace();
          this.profilesError = 'No fue posible cargar nacha-config profiles oficiales.';
          this.notifications.error(this.profilesError);
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
    this.syncForms();
  }

  onSelectVariant(variantId: number): void {
    if (!variantId || variantId === this.selectedVariantId) {
      return;
    }

    this.selectedVariantId = variantId;
    this.selectedFieldId = this.selectedFields.some((field) => field.id === this.selectedFieldId)
      ? this.selectedFieldId
      : this.selectedFields[0]?.id ?? null;
    this.syncForms();
  }

  onSelectField(fieldId: number): void {
    if (!fieldId || fieldId === this.selectedFieldId) {
      return;
    }

    this.selectedFieldId = fieldId;
    this.syncForms();
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
      }))
      .subscribe({
        next: () => {
          this.notifications.success('Variant actualizada correctamente.');
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
      }))
      .subscribe({
        next: () => {
          this.notifications.success('Field actualizado correctamente.');
          this.reloadDetailAfterSave();
        },
        error: (error) => this.handleSaveError(error, 'field')
      });
  }

  irADetallePerfil(): void {
    if (!this.selectedProfileId) {
      return;
    }

    void this.router.navigate(['/nacha-config-admin/perfiles', this.selectedProfileId]);
  }

  irARecords(): void {
    void this.router.navigate(['/nacha-config-admin/records']);
  }

  recargarDetalleActual(): void {
    if (!this.selectedProfileId) {
      return;
    }

    this.loadDetail(this.selectedProfileId, this.selectedRecordCode, this.selectedVariantId, this.selectedFieldId);
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

  isSelectedRecord(record: NachaConfigProfileRecord): boolean {
    return record.recordCode === this.selectedRecordCode;
  }

  isSelectedVariant(variant: NachaConfigLayoutVariant): boolean {
    return variant.id === this.selectedVariantId;
  }

  isSelectedField(field: NachaConfigLayoutField): boolean {
    return field.id === this.selectedFieldId;
  }

  formatDate(value?: string | null): string {
    return value ? new Date(value).toLocaleDateString('es-CO') : '-';
  }

  fieldValue(field: NachaConfigLayoutField): string {
    return field.propertyPath?.trim() || 'Sin propertyPath';
  }

  fieldRulesCount(field: NachaConfigLayoutField): number {
    return field.reglas?.length ?? 0;
  }

  private loadDetail(profileId: number, preferredRecordCode?: string | null, preferredVariantId?: number | null, preferredFieldId?: number | null): void {
    this.loadingDetail = true;
    this.detailError = '';
    this.variantSaveError = '';
    this.fieldSaveError = '';
    this.variantSaveIssues = [];
    this.fieldSaveIssues = [];

    this.query.detalle(profileId)
      .pipe(finalize(() => {
        this.loadingDetail = false;
      }))
      .subscribe({
        next: (detail) => {
          this.selectedProfile = detail;
          this.selectedProfileId = detail.id;
          this.reconcileSelection(detail, preferredRecordCode, preferredVariantId, preferredFieldId);
          this.syncForms();
        },
        error: () => {
          this.selectedProfile = null;
          this.clearWorkspaceSelection();
          this.detailError = 'No fue posible cargar el detalle del perfil NACHA Config.';
          this.notifications.error(this.detailError);
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
      this.selectedFieldId
    );
  }

  private reconcileSelection(
    detail: NachaConfigProfileDetail,
    preferredRecordCode?: string | null,
    preferredVariantId?: number | null,
    preferredFieldId?: number | null
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

  private syncForms(): void {
    this.syncVariantForm();
    this.syncFieldForm();
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
  }

  private handleSaveError(error: unknown, target: 'variant' | 'field'): void {
    const apiError = error as Partial<NachaConfigApiError> | null;
    const message = apiError?.message?.trim() || `No fue posible guardar el ${target}.`;
    const issues = apiError?.issues ?? [];

    if (target === 'variant') {
      this.variantSaveError = message;
      this.variantSaveIssues = issues;
    } else {
      this.fieldSaveError = message;
      this.fieldSaveIssues = issues;
    }

    this.notifications.error(message);
  }

  private resolveProfileSelection(preferredProfileId: number | null): number {
    if (preferredProfileId && this.profiles.some((profile) => profile.profileId === preferredProfileId)) {
      return preferredProfileId;
    }

    return this.profiles[0].profileId;
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
