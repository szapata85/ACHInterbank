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
}

export interface IntegrationMethodParameter {
  id: number;
  methodId: number;
  parameterPath: string;
  displayName: string;
  dataType: string;
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
}

export interface ValidationResult {
  mappingSetId: string;
  isValid: boolean;
  issues: ValidationIssue[];
}

export interface PreviewItem {
  parameterPath: string;
  resolvedFrom: string;
  previewValue?: string | null;
  priority: number;
  enabled: boolean;
}

export interface PreviewResult {
  mappingSetId: string;
  methodId: number;
  methodCode: string;
  items: PreviewItem[];
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

  preview(id: string): Observable<PreviewResult> {
    return this.api.post<PreviewResult>(`api/integrations/mappingsets/${id}/preview`, { maxItems: 5 });
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
}
