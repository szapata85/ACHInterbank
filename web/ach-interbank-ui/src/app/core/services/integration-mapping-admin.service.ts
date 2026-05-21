import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

export type MappingSetStatus = 'Draft' | 'Published' | 'Archived';

export interface IntegrationMethod {
  id: number;
  code: string;
  displayName: string;
  soapClientCode: string;
  isActive: boolean;
  integrationKey: string;
  operationKey: string;
  mappingDirection: string;
  mappingPurpose: string;
  functionalNature: string;
  functionalOriginator: string;
  movesMoney: boolean;
}

export interface IntegrationMethodParameter {
  id: number;
  methodId: number;
  parameterPath: string;
  displayName: string;
  descriptionEs: string;
  category: string;
  exampleValue: string;
  uiHelpText: string;
  dataType: string;
  direction: 'Input' | 'Output';
  cardinality: 'Scalar' | 'Object' | 'Collection';
  required: boolean;
  sortOrder: number;
  isActive: boolean;
}

export interface IntegrationSourceCatalogField {
  id: number;
  methodId?: number | null;
  sourceKind: 'Transaction' | 'Addenda' | 'Batch' | 'Cycle' | 'ClearingHouse' | 'Constant' | 'Expression';
  entityName: string;
  fieldPath: string;
  displayName: string;
  dataType: string;
  cardinality: 'Scalar' | 'Object' | 'Collection';
  nullable: boolean;
  sortOrder: number;
  isActive: boolean;
}

export interface IntegrationTransformationCatalog {
  code: string;
  displayName: string;
  description: string;
  supportsFormatMask: boolean;
  supportsMultipleSources: boolean;
}

export interface IntegrationMappingRule {
  id: number;
  mappingSetId: string;
  methodId: number;
  parameterId: number;
  sourceKind: 'Transaction' | 'Addenda' | 'Batch' | 'Cycle' | 'ClearingHouse' | 'Constant' | 'Expression';
  sourceCatalogFieldId?: number | null;
  sourceFieldPath: string;
  fixedValue?: string | null;
  defaultValue?: string | null;
  transformationCode?: string | null;
  formatMask?: string | null;
  priority: number;
  requiredOverride?: boolean | null;
  enabled: boolean;
  conditionExpression?: string | null;
}

export interface IntegrationMappingSet {
  id: string;
  methodId: number;
  methodCode: string;
  name: string;
  version: number;
  status: MappingSetStatus;
  isActive: boolean;
  notes: string;
  publishedAtUtc?: string | null;
  publishedBy: string;
  rules: IntegrationMappingRule[];
}

export interface ValidationIssue {
  severity: 'Error' | 'Warning' | string;
  code: string;
  message: string;
  path: string;
  category: 'Structural' | 'Functional' | string;
}

export interface ParameterValidationStatus {
  parameterId: number;
  parameterPath: string;
  required: boolean;
  status: 'valid' | 'incomplete' | 'invalid' | 'inactive' | string;
  resolutionKind: 'default-fixed' | 'source-field' | 'expression' | 'none' | string;
  hints: string[];
}

export interface CoverageSummary {
  totalParameters: number;
  validParameters: number;
  incompleteParameters: number;
  invalidParameters: number;
  inactiveParameters: number;
  coveredByDefaultOrFixed: number;
  coveredBySourceField: number;
}

export interface ValidationResult {
  mappingSetId: string;
  isValid: boolean;
  issues: ValidationIssue[];
  coverage: CoverageSummary;
  parameters: ParameterValidationStatus[];
}

export interface PreviewItem {
  parameterId: number;
  parameterPath: string;
  resolvedFrom: string;
  previewValue?: string | null;
  sourceSection: 'ciclo-camara' | 'transaccion' | 'lote' | 'addenda' | 'configuracion' | string;
  resolutionKind: 'default-fixed' | 'source-field' | 'expression' | string;
  appliedTransformation?: string | null;
  priority: number;
  enabled: boolean;
}

export interface PreviewResult {
  mappingSetId: string;
  methodId: number;
  methodCode: string;
  contextMode: string;
  items: PreviewItem[];
  payloadPreviewJson: string;
  rawPreviewJson: string;
}

export interface MappingSetHistoryItem {
  id: string;
  mappingSetId: string;
  methodId: number;
  version: number;
  status: MappingSetStatus;
  action: string;
  performedBy: string;
  performedAtUtc: string;
  snapshotHash: string;
}

export interface MappingSetComparisonMetadata {
  mappingSetId: string;
  name: string;
  version: number;
  status: MappingSetStatus;
  publishedAtUtc?: string | null;
  publishedBy: string;
  notes: string;
}

export interface MappingSetRuleComparison {
  leftRuleId?: number | null;
  rightRuleId?: number | null;
  parameterId: number;
  parameterPath: string;
  parameterGroup: 'ciclo-camara' | 'transaccion' | 'lote' | 'addenda' | 'configuracion' | string;
  changeType: 'Added' | 'Removed' | 'Modified' | 'Equal' | string;
  changedFields: string[];
  potentialImpact: string;
  left?: IntegrationMappingRule | null;
  right?: IntegrationMappingRule | null;
}

export interface MappingSetComparisonResult {
  left: MappingSetComparisonMetadata;
  right: MappingSetComparisonMetadata;
  rules: MappingSetRuleComparison[];
}

@Injectable({ providedIn: 'root' })
export class IntegrationMappingAdminService {
  private readonly api = inject(ApiService);

  getMethods(): Observable<IntegrationMethod[]> {
    return this.api.get<IntegrationMethod[]>('api/integrations/methods');
  }

  getMethodParameters(methodId: number): Observable<IntegrationMethodParameter[]> {
    return this.api.get<IntegrationMethodParameter[]>(`api/integrations/methods/${methodId}/parameters`);
  }

  getSourceCatalog(methodId?: number): Observable<IntegrationSourceCatalogField[]> {
    const query = methodId ? `?methodId=${methodId}` : '';
    return this.api.get<IntegrationSourceCatalogField[]>(`api/integrations/source-catalog${query}`);
  }

  getTransformations(): Observable<IntegrationTransformationCatalog[]> {
    return this.api.get<IntegrationTransformationCatalog[]>('api/integrations/transformations');
  }

  getMappingSets(methodId?: number): Observable<IntegrationMappingSet[]> {
    const query = methodId ? `?methodId=${methodId}` : '';
    return this.api.get<IntegrationMappingSet[]>(`api/integrations/mappingsets${query}`);
  }

  getMappingSetById(id: string): Observable<IntegrationMappingSet> {
    return this.api.get<IntegrationMappingSet>(`api/integrations/mappingsets/${id}`);
  }

  getPublished(methodId: number): Observable<IntegrationMappingSet> {
    return this.api.get<IntegrationMappingSet>(`api/integrations/mappingsets/published?methodId=${methodId}`);
  }

  createDraft(methodId: number, name: string, notes: string, createdBy: string): Observable<IntegrationMappingSet> {
    return this.api.post<IntegrationMappingSet>('api/integrations/mappingsets', { methodId, name, notes, createdBy });
  }

  updateDraft(id: string, payload: { name: string; notes: string; isActive: boolean; updatedBy: string }): Observable<IntegrationMappingSet> {
    return this.api.put<IntegrationMappingSet>(`api/integrations/mappingsets/${id}`, payload);
  }

  upsertRules(id: string, updatedBy: string, rules: Array<Record<string, unknown>>): Observable<IntegrationMappingSet> {
    return this.api.put<IntegrationMappingSet>(`api/integrations/mappingsets/${id}/rules`, { updatedBy, rules });
  }

  validate(id: string): Observable<ValidationResult> {
    return this.api.post<ValidationResult>(`api/integrations/mappingsets/${id}/validate`, { includeWarnings: true });
  }

  preview(id: string, options?: { sampleTransactionId?: number | null; sampleCycleId?: string | null; useControlledSample?: boolean; maxItems?: number }): Observable<PreviewResult> {
    return this.api.post<PreviewResult>(`api/integrations/mappingsets/${id}/preview`, {
      sampleTransactionId: options?.sampleTransactionId ?? null,
      sampleCycleId: options?.sampleCycleId ?? null,
      useControlledSample: options?.useControlledSample ?? true,
      maxItems: options?.maxItems ?? 15
    });
  }

  publish(id: string, publishedBy: string, publishNote?: string): Observable<IntegrationMappingSet> {
    return this.api.post<IntegrationMappingSet>(`api/integrations/mappingsets/${id}/publish`, { publishedBy, publishNote });
  }

  clone(id: string, newName: string, clonedBy: string): Observable<IntegrationMappingSet> {
    return this.api.post<IntegrationMappingSet>(`api/integrations/mappingsets/${id}/clone`, { newName, clonedBy });
  }

  getHistory(id: string): Observable<MappingSetHistoryItem[]> {
    return this.api.get<MappingSetHistoryItem[]>(`api/integrations/mappingsets/${id}/history`);
  }

  compare(leftMappingSetId: string, rightMappingSetId: string): Observable<MappingSetComparisonResult> {
    return this.api.post<MappingSetComparisonResult>('api/integrations/mappingsets/compare', { leftMappingSetId, rightMappingSetId });
  }
}
