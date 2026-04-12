import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';
import {
  IntegrationMappingAdminService,
  IntegrationMappingRule,
  IntegrationMappingSet,
  IntegrationMethodParameter,
  IntegrationSourceCatalogField,
  IntegrationTransformationCatalog,
  PreviewResult,
  ValidationResult
} from '../../../core/services/integration-mapping-admin.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-mapping-editor-page',
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

  get coverage() {
    const total = this.parameters.length;
    const covered = this.parameters.filter((p) => this.mappingSet?.rules.some((r) => r.parameterId === p.id && r.enabled)).length;
    const missing = total - covered;
    const invalid = this.validationResult?.issues.filter((x) => x.severity === 'Error').length ?? 0;
    return { total, covered, missing, invalid };
  }

  loadAll(): void {
    if (!this.mappingSetId) {
      return;
    }

    this.loading = true;
    this.api.getMappingSetById(this.mappingSetId).subscribe({
      next: (set) => {
        this.mappingSet = set;
        this.selectedParameterId = this.selectedParameterId ?? this.parameters[0]?.id;

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
    if (!this.mappingSet || !this.selectedParameterId) {
      return;
    }

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
        this.notifications.success('Regla guardada.');
        this.populateFormFromSelectedRule();
      },
      error: () => this.notifications.error('No fue posible guardar la regla.')
    });
  }

  runValidation(): void {
    if (!this.mappingSet) return;
    this.api.validate(this.mappingSet.id).subscribe({
      next: (result) => {
        this.validationResult = result;
        this.notifications.success(result.isValid ? 'MappingSet válido.' : 'Se encontraron observaciones de validación.');
      },
      error: () => this.notifications.error('No fue posible validar el MappingSet.')
    });
  }

  runPreview(): void {
    if (!this.mappingSet) return;
    this.api.preview(this.mappingSet.id).subscribe({
      next: (result) => (this.previewResult = result),
      error: () => this.notifications.error('No fue posible generar preview.')
    });
  }

  publish(): void {
    if (!this.mappingSet) return;
    this.api.publish(this.mappingSet.id, 'ui-admin', 'Publicado desde SPA').subscribe({
      next: (updated) => {
        this.mappingSet = updated;
        this.notifications.success('MappingSet publicado correctamente.');
      },
      error: () => this.notifications.error('No se pudo publicar. Revisa validación y cobertura.')
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

  getParameterStatus(parameterId: number): 'covered' | 'missing' {
    const hasActive = !!this.mappingSet?.rules.some((x) => x.parameterId === parameterId && x.enabled);
    return hasActive ? 'covered' : 'missing';
  }

  getSourceOptionsByKind(kind: string | null | undefined): IntegrationSourceCatalogField[] {
    if (!kind) return [];
    return this.sourceCatalog.filter((x) => x.sourceKind === kind);
  }
}
