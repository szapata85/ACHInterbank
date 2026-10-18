import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  IntegrationContractArtifactAnalysisDto,
  IntegrationContractArtifactImportResultDto,
  IntegrationContractArtifactPreviewDto,
  IntegrationContractDiffDto,
  IntegrationContractMappingTransitionResultDto,
  IntegrationContractVersionDetailDto,
  IntegrationContractVersionDto
} from './integration-api.models';

export interface CreateIntegrationContractVersionRequest {
  versionLabel: string;
  operationName: string;
  targetNamespace: string;
  soapAction: string;
  sourceReference?: string | null;
  notes?: string | null;
  actor: string;
}

export interface CloneIntegrationContractVersionRequest {
  sourceContractVersionId: string;
  versionLabel: string;
  notes?: string | null;
  actor: string;
}

export interface UpdateIntegrationContractParameterPresentationRequest {
  displayName: string;
  descriptionEs?: string | null;
  uiHelpText?: string | null;
  category?: string | null;
  exampleValue?: string | null;
  expectedRevision: number;
  actor: string;
}

export interface ActivateIntegrationContractVersionRequest {
  mappingSetId: string;
  expectedContractRevision: number;
  expectedMappingRevision: number;
  activatedBy: string;
}

export interface RollbackIntegrationContractVersionRequest {
  targetMappingSetId: string;
  currentContractVersionId: string;
  currentMappingSetId: string;
  expectedTargetContractRevision: number;
  expectedTargetMappingRevision: number;
  expectedCurrentContractRevision: number;
  expectedCurrentMappingRevision: number;
  rolledBackBy: string;
}

export interface AnalyzeIntegrationContractArtifactRequest {
  file: File;
  operationName?: string | null;
  standaloneXsdSoapAction?: string | null;
}

export interface ImportIntegrationContractArtifactRequest extends AnalyzeIntegrationContractArtifactRequest {
  versionLabel: string;
  notes?: string | null;
}

export interface IntegrationContractParameterPresentationResultDto {
  contractVersionId: string;
  contractRevision: number;
  parameter: IntegrationContractVersionDetailDto['parameters'][number];
}

@Injectable({ providedIn: 'root' })
export class IntegrationContractApi {
  private readonly api = inject(ApiService);

  getByMethod(methodId: number): Observable<IntegrationContractVersionDto[]> {
    return this.api.get<IntegrationContractVersionDto[]>(`api/integrations/methods/${methodId}/contracts`);
  }

  getById(contractVersionId: string): Observable<IntegrationContractVersionDetailDto> {
    return this.api.get<IntegrationContractVersionDetailDto>(`api/integrations/contracts/${contractVersionId}`);
  }

  create(methodId: number, request: CreateIntegrationContractVersionRequest): Observable<IntegrationContractVersionDto> {
    return this.api.post<IntegrationContractVersionDto>(`api/integrations/methods/${methodId}/contracts`, request);
  }

  clone(methodId: number, request: CloneIntegrationContractVersionRequest): Observable<IntegrationContractVersionDetailDto> {
    return this.api.post<IntegrationContractVersionDetailDto>(`api/integrations/methods/${methodId}/contracts/clone`, request);
  }

  updateParameterPresentation(
    contractVersionId: string,
    parameterId: number,
    request: UpdateIntegrationContractParameterPresentationRequest
  ): Observable<IntegrationContractParameterPresentationResultDto> {
    return this.api.put<IntegrationContractParameterPresentationResultDto>(
      `api/integrations/contracts/${contractVersionId}/parameters/${parameterId}/presentation`,
      request
    );
  }

  compare(leftId: string, rightId: string): Observable<IntegrationContractDiffDto> {
    return this.api.get<IntegrationContractDiffDto>(`api/integrations/contracts/${leftId}/compare/${rightId}`);
  }

  activate(
    contractVersionId: string,
    request: ActivateIntegrationContractVersionRequest
  ): Observable<IntegrationContractMappingTransitionResultDto> {
    return this.api.post<IntegrationContractMappingTransitionResultDto>(
      `api/integrations/contracts/${contractVersionId}/activate`,
      request
    );
  }

  rollback(
    targetContractVersionId: string,
    request: RollbackIntegrationContractVersionRequest
  ): Observable<IntegrationContractMappingTransitionResultDto> {
    return this.api.post<IntegrationContractMappingTransitionResultDto>(
      `api/integrations/contracts/${targetContractVersionId}/rollback`,
      request
    );
  }

  analyze(request: AnalyzeIntegrationContractArtifactRequest): Observable<IntegrationContractArtifactAnalysisDto> {
    return this.api.post<IntegrationContractArtifactAnalysisDto>('api/integrations/contracts/analyze', this.toAnalyzeFormData(request));
  }

  preview(
    methodId: number,
    baseContractVersionId: string,
    request: AnalyzeIntegrationContractArtifactRequest
  ): Observable<IntegrationContractArtifactPreviewDto> {
    return this.api.post<IntegrationContractArtifactPreviewDto>(
      `api/integrations/methods/${methodId}/contracts/preview/${baseContractVersionId}`,
      this.toAnalyzeFormData(request)
    );
  }

  import(methodId: number, request: ImportIntegrationContractArtifactRequest): Observable<IntegrationContractArtifactImportResultDto> {
    const formData = this.toAnalyzeFormData(request);
    formData.set('VersionLabel', request.versionLabel);
    this.setOptional(formData, 'Notes', request.notes);
    return this.api.post<IntegrationContractArtifactImportResultDto>(`api/integrations/methods/${methodId}/contracts/import`, formData);
  }

  private toAnalyzeFormData(request: AnalyzeIntegrationContractArtifactRequest): FormData {
    const formData = new FormData();
    formData.set('File', request.file, request.file.name);
    this.setOptional(formData, 'OperationName', request.operationName);
    this.setOptional(formData, 'StandaloneXsdSoapAction', request.standaloneXsdSoapAction);
    return formData;
  }

  private setOptional(formData: FormData, key: string, value?: string | null): void {
    if (value != null && value.trim().length > 0) {
      formData.set(key, value);
    }
  }
}
