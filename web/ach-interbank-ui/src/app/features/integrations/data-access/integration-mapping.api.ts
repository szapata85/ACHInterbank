import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { ApiService } from '../../../core/services/api.service';
import {
  IntegrationContractMappingValidationResultDto,
  IntegrationMappingContractCloneResultDto,
  IntegrationMappingPreviewResultDto,
  IntegrationMappingRuleDto,
  IntegrationMappingSetDto,
  IntegrationMappingSetHistoryDto,
  IntegrationMappingValidationResultDto,
  IntegrationMappingWorkspaceDto,
  IntegrationSourceKind
} from './integration-api.models';

export interface CreateIntegrationMappingSetRequest {
  methodId: number;
  name: string;
  notes: string;
  createdBy: string;
}

export interface UpdateIntegrationMappingSetRequest {
  name: string;
  notes: string;
  isActive: boolean;
  updatedBy: string;
}

export interface UpsertIntegrationMappingRuleRequest {
  id?: number | null;
  methodId: number;
  parameterId: number;
  sourceKind: IntegrationSourceKind;
  sourceCatalogFieldId?: number | null;
  sourceFieldPath?: string | null;
  fixedValue?: string | null;
  defaultValue?: string | null;
  transformationCode?: string | null;
  formatMask?: string | null;
  priority: number;
  requiredOverride?: boolean | null;
  enabled: boolean;
  conditionExpression?: string | null;
}

export interface UpsertIntegrationMappingRulesRequest {
  updatedBy: string;
  rules: UpsertIntegrationMappingRuleRequest[];
}

export interface UpdateIntegrationParameterMappingRequest {
  expectedRevision: number;
  sourceCatalogFieldId: number;
  transformationCode?: string | null;
  formatMask?: string | null;
  updatedBy: string;
}

export interface PreviewIntegrationMappingSetRequest {
  sampleTransactionId?: number | null;
  sampleCycleId?: string | null;
  maxItems?: number;
  useControlledSample?: boolean;
}

export interface PublishIntegrationMappingSetRequest {
  expectedRevision: number;
  publishedBy: string;
  publishNote?: string | null;
}

export interface CloneIntegrationMappingToContractVersionRequest {
  targetContractVersionId: string;
  newName: string;
  clonedBy: string;
  expectedSourceMappingRevision: number;
  expectedTargetContractRevision: number;
}

export interface ValidateIntegrationContractMappingRequest {
  expectedMappingRevision: number;
  expectedContractRevision: number;
  includeWarnings?: boolean;
}

@Injectable({ providedIn: 'root' })
export class IntegrationMappingApi {
  private readonly api = inject(ApiService);

  getByMethod(methodId?: number): Observable<IntegrationMappingSetDto[]> {
    const query = methodId == null ? '' : `?methodId=${methodId}`;
    return this.api.get<IntegrationMappingSetDto[]>(`api/integrations/mappingsets${query}`);
  }

  getById(mappingSetId: string): Observable<IntegrationMappingSetDto> {
    return this.api.get<IntegrationMappingSetDto>(`api/integrations/mappingsets/${mappingSetId}`);
  }

  getWorkspace(mappingSetId: string): Observable<IntegrationMappingWorkspaceDto> {
    return this.api.get<IntegrationMappingWorkspaceDto>(`api/integrations/mappingsets/${mappingSetId}/workspace`);
  }

  getPublished(methodId: number): Observable<IntegrationMappingSetDto> {
    return this.api.get<IntegrationMappingSetDto>(`api/integrations/mappingsets/published?methodId=${methodId}`);
  }

  create(request: CreateIntegrationMappingSetRequest): Observable<IntegrationMappingSetDto> {
    return this.api.post<IntegrationMappingSetDto>('api/integrations/mappingsets', request);
  }

  update(mappingSetId: string, request: UpdateIntegrationMappingSetRequest): Observable<IntegrationMappingSetDto> {
    return this.api.put<IntegrationMappingSetDto>(`api/integrations/mappingsets/${mappingSetId}`, request);
  }

  upsertRules(mappingSetId: string, request: UpsertIntegrationMappingRulesRequest): Observable<IntegrationMappingSetDto> {
    return this.api.put<IntegrationMappingSetDto>(`api/integrations/mappingsets/${mappingSetId}/rules`, request);
  }

  updateParameterMapping(
    mappingSetId: string,
    parameterId: number,
    request: UpdateIntegrationParameterMappingRequest
  ): Observable<IntegrationMappingSetDto> {
    return this.api.put<IntegrationMappingSetDto>(
      `api/integrations/mappingsets/${mappingSetId}/parameters/${parameterId}`,
      request
    );
  }

  validate(mappingSetId: string, includeWarnings = true): Observable<IntegrationMappingValidationResultDto> {
    return this.api.post<IntegrationMappingValidationResultDto>(`api/integrations/mappingsets/${mappingSetId}/validate`, {
      includeWarnings
    });
  }

  preview(mappingSetId: string, request: PreviewIntegrationMappingSetRequest = {}): Observable<IntegrationMappingPreviewResultDto> {
    return this.api.post<IntegrationMappingPreviewResultDto>(`api/integrations/mappingsets/${mappingSetId}/preview`, {
      sampleTransactionId: request.sampleTransactionId ?? null,
      sampleCycleId: request.sampleCycleId ?? null,
      maxItems: request.maxItems ?? 3,
      useControlledSample: request.useControlledSample ?? false
    });
  }

  publishWithRevision(
    mappingSetId: string,
    request: PublishIntegrationMappingSetRequest
  ): Observable<IntegrationMappingSetDto> {
    return this.api.post<IntegrationMappingSetDto>(`api/integrations/mappingsets/${mappingSetId}/publish`, {
      expectedRevision: request.expectedRevision,
      publishedBy: request.publishedBy,
      publishNote: request.publishNote ?? null
    });
  }

  publish(mappingSetId: string, publishedBy: string, publishNote?: string | null): Observable<IntegrationMappingSetDto> {
    return this.getById(mappingSetId).pipe(
      switchMap(mapping => this.publishWithRevision(mappingSetId, {
        expectedRevision: mapping.revision,
        publishedBy,
        publishNote: publishNote ?? null
      }))
    );
  }

  clone(mappingSetId: string, newName: string, clonedBy: string): Observable<IntegrationMappingSetDto> {
    return this.api.post<IntegrationMappingSetDto>(`api/integrations/mappingsets/${mappingSetId}/clone`, { newName, clonedBy });
  }

  cloneToContractVersion(
    mappingSetId: string,
    request: CloneIntegrationMappingToContractVersionRequest
  ): Observable<IntegrationMappingContractCloneResultDto> {
    return this.api.post<IntegrationMappingContractCloneResultDto>(
      `api/integrations/mappingsets/${mappingSetId}/clone-to-contract-version`,
      request
    );
  }

  validateContractMapping(
    mappingSetId: string,
    request: ValidateIntegrationContractMappingRequest
  ): Observable<IntegrationContractMappingValidationResultDto> {
    return this.api.post<IntegrationContractMappingValidationResultDto>(
      `api/integrations/mappingsets/${mappingSetId}/validate-contract-mapping`,
      {
        ...request,
        includeWarnings: request.includeWarnings ?? true
      }
    );
  }

  getHistory(mappingSetId: string): Observable<IntegrationMappingSetHistoryDto[]> {
    return this.api.get<IntegrationMappingSetHistoryDto[]>(`api/integrations/mappingsets/${mappingSetId}/history`);
  }

  toRuleRequest(rule: IntegrationMappingRuleDto): UpsertIntegrationMappingRuleRequest {
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
      conditionExpression: rule.conditionExpression ?? null
    };
  }
}
