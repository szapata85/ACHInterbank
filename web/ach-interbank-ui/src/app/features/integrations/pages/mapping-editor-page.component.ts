import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
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

@Component({
  selector: 'app-mapping-editor-page',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './mapping-editor-page.component.html',
  styleUrls: ['./mapping-editor-page.component.scss']
})
export class MappingEditorPageComponent implements OnInit {
  private readonly api = inject(IntegrationMappingAdminService);
  private readonly route = inject(ActivatedRoute);
  private readonly notifications = inject(NotificationService);
  private readonly fb = inject(FormBuilder);

  mappingSetId = '';
  methodCode = '';

  loading = false;
  mappingSet?: IntegrationMappingSet;
  parameters: IntegrationMethodParameter[] = [];
  sourceCatalog: IntegrationSourceCatalogField[] = [];
  transformations: IntegrationTransformationCatalog[] = [];

  selectedParameterId?: number;
  validationResult?: ValidationResult;
  previewResult?: PreviewResult;
  historyItems: MappingSetHistoryItem[] = [];

  previewUseControlledSample = true;
  previewSampleTransactionId?: number;
  previewSampleCycleId?: string;

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
    this.mappingSetId = this.route.snapshot.paramMap.get('mappingSetId') ?? '';
    this.methodCode = this.route.snapshot.paramMap.get('methodCode') ?? '';
    this.loadAll();
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
    if (!this.mappingSetId) return;

    this.loading = true;
    this.api.getMappingSetById(this.mappingSetId).subscribe({
      next: (set) => {
        this.mappingSet = set;

        forkJoin({
          parameters: this.api.getMethodParameters(set.methodId),
          sourceCatalog: this.api.getSourceCatalog(set.methodId),
          transformations: this.api.getTransformations()
        }).subscribe({
          next: ({ parameters, sourceCatalog, transformations }) => {
            this.parameters = parameters;
            this.sourceCatalog = sourceCatalog;
            this.transformations = transformations;
            this.selectedParameterId = this.selectedParameterId ?? parameters[0]?.id;
            this.populateFormFromSelectedRule();
            this.loadHistory();
          },
          error: () => this.notifications.error('No fue posible cargar catálogos del editor.'),
          complete: () => (this.loading = false)
        });
      },
      error: () => {
        this.notifications.error('No fue posible cargar el MappingSet.');
        this.loading = false;
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

  saveRule(): void {
    if (!this.mappingSet || !this.selectedParameterId) return;

    const payload = {
      id: this.ruleForm.value.id,
      methodId: this.mappingSet.methodId,
      parameterId: this.selectedParameterId,
      sourceKind: this.ruleForm.value.sourceKind,
      sourceCatalogFieldId: this.ruleForm.value.sourceCatalogFieldId,
      sourceFieldPath: this.ruleForm.value.sourceFieldPath,
      fixedValue: this.ruleForm.value.fixedValue,
      defaultValue: this.ruleForm.value.defaultValue,
      transformationCode: this.ruleForm.value.transformationCode,
      formatMask: this.ruleForm.value.formatMask,
      priority: Number(this.ruleForm.value.priority ?? 1),
      requiredOverride: this.ruleForm.value.requiredOverride,
      enabled: Boolean(this.ruleForm.value.enabled),
      conditionExpression: this.ruleForm.value.conditionExpression
    };

    this.api.upsertRules(this.mappingSet.id, 'ui-admin', [payload]).subscribe({
      next: (updated) => {
        this.mappingSet = updated;
        this.notifications.success('Regla guardada. Ejecuta validación para confirmar consistencia.');
        this.populateFormFromSelectedRule();
      },
      error: () => this.notifications.error('No fue posible guardar la regla.')
    });
  }

  runValidation(onDone?: (isValid: boolean) => void): void {
    if (!this.mappingSet) return;

    this.api.validate(this.mappingSet.id).subscribe({
      next: (result) => {
        this.validationResult = result;
        const message = result.isValid
          ? 'MappingSet válido y completo. Puedes publicar.'
          : 'Se encontraron observaciones estructurales/funcionales. Corrígelas antes de publicar.';
        this.notifications.success(message);
        onDone?.(result.isValid);
      },
      error: () => {
        this.notifications.error('No fue posible validar el MappingSet.');
        onDone?.(false);
      }
    });
  }

  runPreview(): void {
    if (!this.mappingSet) return;

    this.api
      .preview(this.mappingSet.id, {
        sampleTransactionId: this.previewSampleTransactionId,
        sampleCycleId: this.previewSampleCycleId,
        useControlledSample: this.previewUseControlledSample,
        maxItems: 200
      })
      .subscribe({
        next: (result) => {
          this.previewResult = result;
          this.notifications.success(`Preview generado usando contexto: ${result.contextMode}.`);
        },
        error: () => this.notifications.error('No fue posible generar preview.')
      });
  }

  publish(): void {
    if (!this.mappingSet) return;

    this.runValidation((isValid) => {
      if (!isValid) {
        this.notifications.error('Publicación bloqueada: MappingSet inválido o incompleto.');
        return;
      }

      this.api.publish(this.mappingSet!.id, 'ui-admin', 'Publicado desde SPA').subscribe({
        next: (updated) => {
          this.mappingSet = updated;
          this.notifications.success('MappingSet publicado correctamente.');
        },
        error: () => this.notifications.error('No se pudo publicar. Revisa validación y cobertura.')
      });
    });
  }

  clone(): void {
    if (!this.mappingSet) return;
    this.api.clone(this.mappingSet.id, `${this.mappingSet.name} (clone)`, 'ui-admin').subscribe({
      next: (created) => {
        this.notifications.success('MappingSet clonado.');
        this.mappingSet = created;
        this.mappingSetId = created.id;
        this.loadAll();
      },
      error: () => this.notifications.error('No fue posible clonar el MappingSet.')
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

  getParameterIssues(parameterPath: string): ValidationResult['issues'] {
    return (this.validationResult?.issues ?? []).filter((x) => x.path === parameterPath);
  }

  getSourceOptionsByKind(kind: string | null | undefined): IntegrationSourceCatalogField[] {
    if (!kind) return [];
    return this.sourceCatalog.filter((x) => x.sourceKind === kind);
  }

  getSourceKindLabel(kind: string | null | undefined): string {
    switch (kind) {
      case 'Transaction': return 'Dato de transacción';
      case 'Batch': return 'Dato de lote';
      case 'Cycle': return 'Dato de ciclo';
      case 'ClearingHouse': return 'Dato de cámara';
      case 'Constant': return 'Valor fijo';
      case 'Addenda': return 'Dato complementario';
      default: return 'No definido';
    }
  }

  trackByParameterId(_: number, parameter: IntegrationMethodParameter): number {
    return parameter.id;
  }
}
