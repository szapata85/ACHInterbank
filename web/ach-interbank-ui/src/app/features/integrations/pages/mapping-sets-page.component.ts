import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import {
  IntegrationMappingPreviewItemDto,
  IntegrationMappingValidationIssueDto,
  IntegrationMappingWorkspaceParameterStatus,
  IntegrationMappingWorkspaceSourceCompatibilityDto,
  IntegrationMappingWorkspaceSourceGroupDto,
  IntegrationMappingWorkspaceTransformationOptionDto
} from '../data-access/integration-api.models';
import {
  IntegrationInformationParameterReadModel,
  toIntegrationInformationParameterReadModel
} from '../data-access/integration-read.models';
import { presentIntegrationStatus } from '../presentation/mapping-status.presenter';
import {
  PresentedIntegrationValidationIssue,
  presentIntegrationValidationIssue
} from '../presentation/mapping-validation.presenter';
import { MappingWorkspaceStore } from '../state/mapping-workspace.store';
import { MappingPublicationDialogComponent, MappingPublicationDialogData } from './mapping-publication-dialog.component';

type StatusFilter = 'All' | IntegrationMappingWorkspaceParameterStatus;

type ParameterEditForm = FormGroup<{
  sourceSearch: FormControl<string>;
  sourceCatalogFieldId: FormControl<number | null>;
  transformationCode: FormControl<string>;
  formatMask: FormControl<string>;
}>;

@Component({
  selector: 'app-mapping-sets-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    MatRadioModule,
    MatSelectModule,
    MatTabsModule
  ],
  templateUrl: './mapping-sets-page.component.html',
  styleUrls: ['./mapping-sets-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [MappingWorkspaceStore]
})
export class MappingSetsPageComponent {
  readonly store = inject(MappingWorkspaceStore);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchSignal = signal('');
  private readonly statusFilterSignal = signal<StatusFilter>('All');
  private readonly editingParameterIdSignal = signal<number | null>(null);
  private readonly sourceSearchSignal = signal('');
  private readonly sourceIdSignal = signal<number | null>(null);
  private readonly transformationCodeSignal = signal('');
  private readonly formatMaskSignal = signal('');

  readonly canManage = this.auth.hasPermission(['Integrations.ManageMappings', 'CanManageAch']);
  readonly canValidate = this.auth.hasPermission(['Integrations.Validate', 'CanManageAch']);
  readonly canPublish = this.auth.hasPermission(['Integrations.Publish', 'CanManageAch']);

  readonly filters = this.fb.nonNullable.group({
    search: '',
    status: 'All' as StatusFilter
  });

  readonly parameterForm: ParameterEditForm = new FormGroup({
    sourceSearch: new FormControl('', { nonNullable: true }),
    sourceCatalogFieldId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    transformationCode: new FormControl('', { nonNullable: true }),
    formatMask: new FormControl('', { nonNullable: true })
  });

  readonly statusOptions: ReadonlyArray<{ value: StatusFilter; label: string }> = [
    { value: 'All', label: 'Todos' },
    { value: 'Complete', label: 'Completo' },
    { value: 'Pending', label: 'Pendiente' },
    { value: 'Invalid', label: 'Inválido' },
    { value: 'NotApplicable', label: 'No aplica' }
  ];

  readonly parameters = computed<IntegrationInformationParameterReadModel[]>(() =>
    this.store.parameters().map(toIntegrationInformationParameterReadModel)
  );

  readonly previewByParameter = computed(() => new Map<number, IntegrationMappingPreviewItemDto>(
    (this.store.preview()?.items ?? []).map(item => [item.parameterId, item])
  ));

  readonly generalValidationIssues = computed<PresentedIntegrationValidationIssue[]>(() =>
    (this.store.preview()?.validation?.issues ?? []).map(issue =>
      presentIntegrationValidationIssue(issue, this.functionalNameForIssue(issue))
    )
  );

  readonly pendingRequiredParameters = computed(() =>
    this.parameters().filter(parameter => parameter.required && parameter.status === 'Pending')
  );

  readonly publicationDisabled = computed(() => {
    const workspace = this.store.workspace();
    return !this.canPublish
      || !workspace
      || workspace.mappingSet.status !== 'Draft'
      || workspace.publication.pendingRequired > 0
      || !this.store.canPublish()
      || this.store.publishing();
  });

  readonly filteredParameters = computed(() => {
    const search = this.normalize(this.searchSignal());
    const status = this.statusFilterSignal();

    return this.parameters().filter((parameter) => {
      const matchesStatus = status === 'All' || parameter.status === status;
      if (!matchesStatus) {
        return false;
      }

      if (!search) {
        return true;
      }

      return [parameter.functionalName, parameter.functionalDescription, parameter.categoryLabel]
        .some((value) => this.normalize(value).includes(search));
    });
  });

  readonly selectedMethodIndex = computed(() => {
    const methodId = this.store.methodId();
    const index = this.store.methods().findIndex((method) => method.id === methodId);
    return index < 0 ? 0 : index;
  });

  readonly editingParameterId = this.editingParameterIdSignal.asReadonly();
  readonly editingParameter = computed(() => {
    const parameterId = this.editingParameterIdSignal();
    return parameterId == null ? null : this.parameters().find(parameter => parameter.parameterId === parameterId) ?? null;
  });

  readonly selectedCompatibility = computed<IntegrationMappingWorkspaceSourceCompatibilityDto | null>(() => {
    const workspace = this.store.workspace();
    const parameterId = this.editingParameterIdSignal();
    const sourceCatalogFieldId = this.sourceIdSignal();
    if (!workspace || parameterId == null || sourceCatalogFieldId == null) {
      return null;
    }

    return workspace.sourceCompatibility.find(item =>
      item.parameterId === parameterId && item.sourceCatalogFieldId === sourceCatalogFieldId) ?? null;
  });

  readonly compatibleTreatments = computed<IntegrationMappingWorkspaceTransformationOptionDto[]>(() =>
    this.selectedCompatibility()?.transformations ?? []
  );

  readonly selectedTreatment = computed<IntegrationMappingWorkspaceTransformationOptionDto | null>(() => {
    const code = this.transformationCodeSignal();
    return this.compatibleTreatments().find(item => item.code === code) ?? null;
  });

  readonly treatmentSupportsMask = computed(() => {
    const workspace = this.store.workspace();
    const code = this.transformationCodeSignal();
    if (!workspace || !code) {
      return false;
    }
    return workspace.transformations.some(item => item.code === code && item.supportsFormatMask);
  });

  readonly filteredSourceGroups = computed<IntegrationMappingWorkspaceSourceGroupDto[]>(() => {
    const workspace = this.store.workspace();
    const parameterId = this.editingParameterIdSignal();
    if (!workspace || parameterId == null) {
      return [];
    }

    const allowedSourceIds = new Set(
      workspace.sourceCompatibility
        .filter(item => item.parameterId === parameterId)
        .map(item => item.sourceCatalogFieldId)
    );
    const search = this.normalize(this.sourceSearchSignal());

    return workspace.sourceGroups
      .map(group => ({
        ...group,
        fields: group.fields.filter(field => {
          if (!allowedSourceIds.has(field.id)) {
            return false;
          }
          if (!search) {
            return true;
          }
          return [field.displayName, field.entityName, field.fieldPath, String(field.id)]
            .some(value => this.normalize(value).includes(search));
        })
      }))
      .filter(group => group.fields.length > 0);
  });

  readonly canSaveParameter = computed(() => {
    const parameterId = this.editingParameterIdSignal();
    const sourceId = this.sourceIdSignal();
    const compatibility = this.selectedCompatibility();
    const treatmentCode = this.transformationCodeSignal();
    const treatment = this.selectedTreatment();
    const mask = this.formatMaskSignal().trim();

    if (!this.canManage || this.store.workspace()?.mappingSet.status !== 'Draft' || parameterId == null || sourceId == null || !compatibility) {
      return false;
    }
    if (!treatmentCode && !compatibility.allowsNoTransformation) {
      return false;
    }
    if (treatmentCode && !treatment) {
      return false;
    }
    if (treatment?.requiresFormatMask && !mask) {
      return false;
    }
    return this.parameterForm.valid && this.store.savingParameterId() === null;
  });

  constructor() {
    this.filters.controls.search.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(value => this.searchSignal.set(value));
    this.filters.controls.status.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(value => this.statusFilterSignal.set(value));

    this.parameterForm.controls.sourceSearch.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(value => this.sourceSearchSignal.set(value));
    this.parameterForm.controls.sourceCatalogFieldId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(value => {
        this.sourceIdSignal.set(value);
        this.syncTreatmentForSource();
      });
    this.parameterForm.controls.transformationCode.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(value => {
        this.transformationCodeSignal.set(value);
        this.syncMaskValidation();
      });
    this.parameterForm.controls.formatMask.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(value => this.formatMaskSignal.set(value));

    effect(() => {
      const savedParameterId = this.store.saveSucceededParameterId();
      if (savedParameterId == null || savedParameterId !== this.editingParameterIdSignal()) {
        return;
      }
      const refreshed = this.parameters().find(parameter => parameter.parameterId === savedParameterId);
      if (refreshed) {
        this.hydrateParameterForm(refreshed);
      }
    });

    this.store.load(this.route.snapshot.queryParamMap.get('method'), this.canValidate);
  }

  onMethodTabChange(index: number): void {
    const method = this.store.methods()[index];
    if (!method) {
      return;
    }

    this.cancelEdit();
    this.clearFilters();
    this.store.selectMethod(method.id);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { method: method.operationKey },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  clearFilters(): void {
    this.filters.setValue({ search: '', status: 'All' });
  }

  retry(): void {
    this.cancelEdit();
    this.store.reload();
  }

  retryPreview(): void {
    if (!this.canValidate) return;
    this.store.refreshPreview();
  }

  reloadAfterRevisionConflict(): void {
    this.cancelEdit();
    this.store.reloadAfterRevisionConflict();
  }

  reviewPublication(): void {
    if (!this.canPublish) return;
    this.openPublicationDialog();
  }

  publishConfiguration(): void {
    if (this.publicationDisabled()) {
      return;
    }
    this.openPublicationDialog();
  }

  previewFor(parameterId: number): IntegrationMappingPreviewItemDto | null {
    return this.previewByParameter().get(parameterId) ?? null;
  }

  validationIssuesFor(parameterId: number): PresentedIntegrationValidationIssue[] {
    const parameter = this.parameters().find(item => item.parameterId === parameterId);
    const preview = this.previewFor(parameterId);
    return (preview?.validationIssues ?? []).map(issue =>
      presentIntegrationValidationIssue(issue, parameter?.functionalName)
    );
  }

  previewContextLabel(contextMode: string | null | undefined): string {
    switch (contextMode) {
      case 'controlled-sample': return 'Muestra controlada sanitizada';
      case 'real-transaction': return 'Muestra de transacción sanitizada';
      case 'real-latest': return 'Muestra reciente sanitizada';
      default: return 'Muestra sanitizada';
    }
  }

  previewErrorMessage(): string {
    const error = this.store.previewError();
    switch (error?.kind) {
      case 'forbidden': return 'No tiene permisos para consultar esta vista previa.';
      case 'network': return 'No fue posible comunicarse con el servicio de vista previa.';
      case 'http': return 'El servicio no pudo calcular la vista previa. Intente nuevamente.';
      default: return 'No fue posible obtener la vista previa. Intente nuevamente.';
    }
  }

  startEdit(parameter: IntegrationInformationParameterReadModel): void {
    if (!this.canManage || this.store.workspace()?.mappingSet.status !== 'Draft') {
      return;
    }
    this.store.clearSaveState();
    this.editingParameterIdSignal.set(parameter.parameterId);
    this.hydrateParameterForm(parameter);
  }

  cancelEdit(): void {
    this.editingParameterIdSignal.set(null);
    this.store.clearSaveState();
    this.parameterForm.reset({
      sourceSearch: '',
      sourceCatalogFieldId: null,
      transformationCode: '',
      formatMask: ''
    });
    this.sourceSearchSignal.set('');
    this.sourceIdSignal.set(null);
    this.transformationCodeSignal.set('');
    this.formatMaskSignal.set('');
  }

  saveParameter(): void {
    const parameterId = this.editingParameterIdSignal();
    const sourceCatalogFieldId = this.parameterForm.controls.sourceCatalogFieldId.value;
    if (parameterId == null || sourceCatalogFieldId == null || !this.canSaveParameter()) {
      this.parameterForm.markAllAsTouched();
      return;
    }

    this.store.saveParameter(parameterId, {
      sourceCatalogFieldId,
      transformationCode: this.parameterForm.controls.transformationCode.value || null,
      formatMask: this.parameterForm.controls.formatMask.value || null
    }, this.auth.currentUser?.username ?? 'usuario');
  }

  isEditing(parameterId: number): boolean {
    return this.editingParameterIdSignal() === parameterId;
  }

  statusLabel(status: string | null | undefined): string {
    return presentIntegrationStatus(status).label;
  }

  statusTone(status: string | null | undefined): string {
    return presentIntegrationStatus(status).tone;
  }

  statusCount(status: StatusFilter): number {
    const coverage = this.store.coverage();
    if (!coverage) {
      return 0;
    }

    switch (status) {
      case 'Complete': return coverage.complete;
      case 'Pending': return coverage.pending;
      case 'Invalid': return coverage.invalid;
      case 'NotApplicable': return coverage.notApplicable;
      default: return coverage.total;
    }
  }

  trackParameter(_: number, parameter: IntegrationInformationParameterReadModel): number {
    return parameter.parameterId;
  }

  trackSourceGroup(_: number, group: IntegrationMappingWorkspaceSourceGroupDto): string {
    return group.key;
  }

  trackSource(_: number, source: { id: number }): number {
    return source.id;
  }

  trackTreatment(_: number, treatment: IntegrationMappingWorkspaceTransformationOptionDto): string {
    return treatment.code;
  }

  private openPublicationDialog(): void {
    if (!this.canPublish) return;
    const workspace = this.store.workspace();
    if (!workspace) {
      return;
    }

    const data: MappingPublicationDialogData = {
      serviceName: this.store.selectedMethod()?.displayName || this.store.selectedMethod()?.functionalNature || 'Servicio CFA',
      contractVersion: workspace.contract?.versionLabel || 'Sin versión contractual asociada',
      mappingSetName: workspace.mappingSet.name,
      revision: workspace.mappingSet.revision,
      total: workspace.coverage.total,
      complete: workspace.coverage.complete,
      pendingRequired: workspace.publication.pendingRequired,
      newRequiredPending: workspace.publication.newRequiredPending,
      blockingIssues: workspace.publication.blockingIssues,
      pendingRequiredNames: this.pendingRequiredParameters().map(parameter => parameter.functionalName),
      canPublish: workspace.publication.canPublish && workspace.publication.pendingRequired === 0
    };

    this.dialog.open(MappingPublicationDialogComponent, {
      data,
      width: 'min(42rem, calc(100vw - 2rem))',
      maxWidth: 'calc(100vw - 2rem)',
      maxHeight: 'calc(100dvh - 2rem)',
      autoFocus: 'first-heading',
      restoreFocus: true,
      disableClose: false,
      closeOnNavigation: true,
      role: 'alertdialog',
      ariaLabel: 'Confirmar publicación de configuración CFA'
    }).afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(confirmed => {
        if (confirmed !== true) {
          return;
        }
        this.store.publish(this.auth.currentUser?.username ?? 'usuario');
      });
  }

  private hydrateParameterForm(parameter: IntegrationInformationParameterReadModel): void {
    const currentRule = parameter.currentRule;
    const sourceId = currentRule?.sourceCatalogFieldId ?? null;
    const treatmentCode = currentRule?.transformationCode ?? '';
    this.parameterForm.reset({
      sourceSearch: '',
      sourceCatalogFieldId: sourceId,
      transformationCode: treatmentCode,
      formatMask: currentRule?.formatMask ?? ''
    }, { emitEvent: false });
    this.sourceSearchSignal.set('');
    this.sourceIdSignal.set(sourceId);
    this.transformationCodeSignal.set(treatmentCode);
    this.formatMaskSignal.set(currentRule?.formatMask ?? '');
    this.syncTreatmentForSource();
    this.syncMaskValidation();
    this.parameterForm.markAsPristine();
  }

  private syncTreatmentForSource(): void {
    const compatibility = this.selectedCompatibility();
    const treatmentControl = this.parameterForm.controls.transformationCode;
    const currentCode = treatmentControl.value;
    const currentAllowed = !currentCode
      ? compatibility?.allowsNoTransformation === true
      : compatibility?.transformations.some(item => item.code === currentCode) === true;

    if (!currentAllowed) {
      treatmentControl.setValue('', { emitEvent: false });
      this.transformationCodeSignal.set('');
    }

    treatmentControl.setValidators(compatibility && !compatibility.allowsNoTransformation ? [Validators.required] : []);
    treatmentControl.updateValueAndValidity({ emitEvent: false });
    this.syncMaskValidation();
  }

  private syncMaskValidation(): void {
    const maskControl = this.parameterForm.controls.formatMask;
    const treatment = this.selectedTreatment();
    maskControl.setValidators(treatment?.requiresFormatMask ? [Validators.required] : []);
    maskControl.updateValueAndValidity({ emitEvent: false });
  }

  private functionalNameForIssue(issue: IntegrationMappingValidationIssueDto): string | null {
    const parameter = this.parameters().find(item =>
      item.technicalDetails.parameterPath.toLocaleLowerCase('es') === issue.path.toLocaleLowerCase('es'));
    return parameter?.functionalName ?? null;
  }

  private normalize(value: string | null | undefined): string {
    return (value ?? '').trim().toLocaleLowerCase('es');
  }
}
