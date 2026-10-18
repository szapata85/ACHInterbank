import {
  IntegrationConnectionMethodDto,
  IntegrationContractParameterDto,
  IntegrationContractVersionDto,
  IntegrationMappingCoverageSummaryDto,
  IntegrationMappingParameterValidationDto,
  IntegrationMappingSetDto,
  IntegrationMappingWorkspaceCurrentRuleDto,
  IntegrationMappingWorkspaceParameterDto,
  IntegrationMappingWorkspaceParameterStatus,
  IntegrationMethodDto,
  IntegrationSourceCatalogFieldDto,
  IntegrationTransformationCatalogDto
} from './integration-api.models';

export interface IntegrationServiceCapabilitiesReadModel {
  canEditMapping: boolean | null;
  canPublish: boolean | null;
  canManageContract: boolean | null;
  canManageConnection: boolean | null;
}

export interface IntegrationServiceReadModel extends IntegrationServiceCapabilitiesReadModel {
  methodId: number;
  functionalTitle: string;
  functionalDescription: string;
  technicalMethodName: string;
  movesMoney: boolean;
  contractVersionLabel: string | null;
  contractStatus: string | null;
  mappingStatus: string | null;
  mappingCoverage: IntegrationMappingCoverageSummaryDto | null;
  connectionStatus: string | null;
}

export interface IntegrationParameterTechnicalDetailsReadModel {
  xmlElementName: string;
  dataType: string;
  xsdType: string;
  direction: string;
  cardinality: string;
  nillable: boolean;
  minOccurs: number;
  maxOccurs: number | null;
  supportStatus: string;
}

export interface IntegrationParameterReadModel {
  parameterId: number;
  functionalName: string;
  functionalDescription: string;
  technicalName: string;
  required: boolean;
  categoryLabel: string;
  status: string | null;
  sourceSummary: string | null;
  transformationSummary: string | null;
  exampleValue: string;
  uiHelpText: string;
  technicalDetails: IntegrationParameterTechnicalDetailsReadModel;
}


export interface IntegrationInformationTechnicalDetailsReadModel {
  parameterPath: string;
  xmlElementName: string;
  dataType: string;
  xsdType: string;
  targetNamespace: string | null;
  sortOrder: number;
}

export interface IntegrationInformationParameterReadModel {
  parameterId: number;
  functionalName: string;
  functionalDescription: string;
  required: boolean;
  categoryLabel: string;
  status: IntegrationMappingWorkspaceParameterStatus;
  sourceSummary: string | null;
  transformationSummary: string | null;
  currentRule: IntegrationMappingWorkspaceCurrentRuleDto | null;
  technicalDetails: IntegrationInformationTechnicalDetailsReadModel;
}

export function toIntegrationInformationParameterReadModel(
  parameter: IntegrationMappingWorkspaceParameterDto
): IntegrationInformationParameterReadModel {
  return {
    parameterId: parameter.parameterId,
    functionalName: parameter.functionalName,
    functionalDescription: parameter.functionalDescription,
    required: parameter.required,
    categoryLabel: parameter.category,
    status: parameter.status,
    sourceSummary: parameter.source ?? null,
    transformationSummary: parameter.transformation ?? null,
    currentRule: parameter.currentRule ?? null,
    technicalDetails: {
      parameterPath: parameter.technicalDetails.parameterPath,
      xmlElementName: parameter.technicalDetails.xmlElementName,
      dataType: parameter.technicalDetails.dataType,
      xsdType: parameter.technicalDetails.xsdType,
      targetNamespace: parameter.technicalDetails.targetNamespace ?? null,
      sortOrder: parameter.technicalDetails.sortOrder
    }
  };
}

export interface IntegrationServiceSnapshotInput {
  contract?: IntegrationContractVersionDto | null;
  mapping?: IntegrationMappingSetDto | null;
  mappingCoverage?: IntegrationMappingCoverageSummaryDto | null;
  connection?: IntegrationConnectionMethodDto | null;
  capabilities?: Partial<IntegrationServiceCapabilitiesReadModel> | null;
}

export interface IntegrationParameterSnapshotInput {
  validation?: IntegrationMappingParameterValidationDto | null;
  source?: IntegrationSourceCatalogFieldDto | null;
  transformation?: IntegrationTransformationCatalogDto | null;
}

export function toIntegrationServiceReadModel(
  method: IntegrationMethodDto,
  snapshot: IntegrationServiceSnapshotInput = {}
): IntegrationServiceReadModel {
  return {
    methodId: method.id,
    functionalTitle: method.displayName,
    functionalDescription: method.mappingPurpose,
    technicalMethodName: method.operationKey || method.code,
    movesMoney: method.movesMoney,
    contractVersionLabel: snapshot.contract?.versionLabel ?? null,
    contractStatus: snapshot.contract?.status ?? null,
    mappingStatus: snapshot.mapping?.status ?? null,
    mappingCoverage: snapshot.mappingCoverage ?? null,
    connectionStatus: snapshot.connection == null
      ? null
      : snapshot.connection.enabled
        ? snapshot.connection.operatingMode
        : 'Disabled',
    canEditMapping: snapshot.capabilities?.canEditMapping ?? null,
    canPublish: snapshot.capabilities?.canPublish ?? null,
    canManageContract: snapshot.capabilities?.canManageContract ?? null,
    canManageConnection: snapshot.capabilities?.canManageConnection ?? null
  };
}

export function toIntegrationParameterReadModel(
  parameter: IntegrationContractParameterDto,
  snapshot: IntegrationParameterSnapshotInput = {}
): IntegrationParameterReadModel {
  return {
    parameterId: parameter.id,
    functionalName: parameter.displayName,
    functionalDescription: parameter.descriptionEs,
    technicalName: parameter.parameterPath,
    required: parameter.required,
    categoryLabel: parameter.category,
    status: snapshot.validation?.status ?? null,
    sourceSummary: snapshot.source?.displayName ?? null,
    transformationSummary: snapshot.transformation?.displayName ?? null,
    exampleValue: parameter.exampleValue,
    uiHelpText: parameter.uiHelpText,
    technicalDetails: {
      xmlElementName: parameter.xmlElementName,
      dataType: parameter.dataType,
      xsdType: parameter.xsdType,
      direction: parameter.direction,
      cardinality: parameter.cardinality,
      nillable: parameter.nillable,
      minOccurs: parameter.minOccurs,
      maxOccurs: parameter.maxOccurs ?? null,
      supportStatus: parameter.supportStatus
    }
  };
}


export type IntegrationContractDisplayState = 'Draft' | 'Active' | 'Retired';

export interface IntegrationContractRelationshipReadModel {
  mappingSetId: string | null;
  mappingRevision: number | null;
  status: string | null;
  label: string;
}

export interface IntegrationContractVersionReadModel {
  id: string;
  versionLabel: string;
  backendStatus: IntegrationContractVersionDto['status'];
  displayState: IntegrationContractDisplayState;
  statusLabel: string;
  revision: number;
  isReadOnly: boolean;
  canEditFunctionalMetadata: boolean;
  notes: string | null;
  sourceType: IntegrationContractVersionDto['sourceType'];
  sourceLabel: string;
  importedAtUtc: string | null;
  importedBy: string | null;
  activatedAtUtc: string | null;
  activatedBy: string | null;
  relationship: IntegrationContractRelationshipReadModel;
}

export function toIntegrationContractVersionReadModel(
  contract: IntegrationContractVersionDto,
  mappings: IntegrationMappingSetDto[] = []
): IntegrationContractVersionReadModel {
  const mapping = mappings.find((item) => item.contractVersionId === contract.id) ?? null;
  return {
    id: contract.id,
    versionLabel: contract.versionLabel,
    backendStatus: contract.status,
    displayState: contractDisplayState(contract.status),
    statusLabel: contractStatusLabel(contract.status),
    revision: contract.revision,
    isReadOnly: contract.capabilities?.isReadOnly ?? true,
    canEditFunctionalMetadata: contract.capabilities?.canEditFunctionalMetadata ?? false,
    notes: contract.notes ?? null,
    sourceType: contract.sourceType,
    sourceLabel: contractSourceLabel(contract.sourceType),
    importedAtUtc: contract.importedAtUtc ?? null,
    importedBy: contract.importedBy ?? null,
    activatedAtUtc: contract.activatedAtUtc ?? null,
    activatedBy: contract.activatedBy ?? null,
    relationship: {
      mappingSetId: mapping?.id ?? null,
      mappingRevision: mapping?.revision ?? null,
      status: mapping?.status ?? null,
      label: mappingStatusLabel(mapping?.status ?? null)
    }
  };
}

function contractDisplayState(status: IntegrationContractVersionDto['status']): IntegrationContractDisplayState {
  if (status === 'Active') return 'Active';
  if (status === 'Retired') return 'Retired';
  return 'Draft';
}

function contractStatusLabel(status: IntegrationContractVersionDto['status']): string {
  switch (status) {
    case 'Draft':
      return 'Borrador';
    case 'Validated':
      return 'Borrador validado';
    case 'Active':
      return 'Activa';
    case 'Retired':
      return 'Retirada';
  }
}

function contractSourceLabel(sourceType: IntegrationContractVersionDto['sourceType']): string {
  switch (sourceType) {
    case 'WsdlImport':
      return 'Importada desde contrato local';
    case 'ManualApproved':
      return 'Versión administrada';
    case 'BaselineMigration':
      return 'Versión base migrada';
  }
}

function mappingStatusLabel(status: IntegrationMappingSetDto['status'] | null): string {
  switch (status) {
    case 'Draft':
      return 'Relación en borrador';
    case 'Published':
      return 'Relación publicada';
    case 'Archived':
      return 'Relación archivada';
    default:
      return 'Sin relación asociada';
  }
}
