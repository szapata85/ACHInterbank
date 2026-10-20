export type IntegrationContractStatus = 'Draft' | 'Validated' | 'Active' | 'Retired';
export type IntegrationContractSourceType = 'WsdlImport' | 'ManualApproved' | 'BaselineMigration';

export interface IntegrationContractVersionCapabilitiesDto {
  isReadOnly: boolean;
  canEditFunctionalMetadata: boolean;
}
export type IntegrationMappingStatus = 'Draft' | 'Published' | 'Archived';
export type IntegrationParameterDirection = 'Input' | 'Output';
export type IntegrationParameterCardinality = 'Scalar' | 'Object' | 'Collection';
export type IntegrationSourceKind =
  | 'Transaction'
  | 'Addenda'
  | 'Batch'
  | 'Cycle'
  | 'ClearingHouse'
  | 'Constant'
  | 'Expression'
  | 'NachaHeader'
  | 'BatchHeader'
  | 'EntryDetail'
  | 'AddendaRecord'
  | 'BatchControl'
  | 'FileControl'
  | 'Prenotification'
  | 'DifferentialResponse';

export interface IntegrationMethodDto {
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

export interface IntegrationMethodParameterDto {
  id: number;
  methodId: number;
  parameterPath: string;
  displayName: string;
  descriptionEs: string;
  category: string;
  exampleValue: string;
  uiHelpText: string;
  dataType: string;
  direction: IntegrationParameterDirection;
  cardinality: IntegrationParameterCardinality;
  required: boolean;
  sortOrder: number;
  isActive: boolean;
}

export interface IntegrationContractVersionDto {
  id: string;
  methodId: number;
  versionLabel: string;
  status: IntegrationContractStatus;
  operationName: string;
  targetNamespace: string;
  soapAction: string;
  sourceType: IntegrationContractSourceType;
  sourceDocumentName?: string | null;
  sourceDocumentHash?: string | null;
  sourceReference?: string | null;
  notes?: string | null;
  revision: number;
  importedAtUtc?: string | null;
  importedBy?: string | null;
  activatedAtUtc?: string | null;
  activatedBy?: string | null;
  capabilities?: IntegrationContractVersionCapabilitiesDto | null;
}

export interface IntegrationContractParameterDto {
  id: number;
  contractVersionId?: string | null;
  methodId: number;
  parameterPath: string;
  xmlElementName: string;
  displayName: string;
  descriptionEs: string;
  uiHelpText: string;
  category: string;
  exampleValue: string;
  dataType: string;
  xsdType: string;
  direction: IntegrationParameterDirection;
  cardinality: IntegrationParameterCardinality;
  required: boolean;
  nillable: boolean;
  minOccurs: number;
  maxOccurs?: number | null;
  supportStatus: string;
  sortOrder: number;
  isActive: boolean;
}

export interface IntegrationContractVersionDetailDto {
  contract: IntegrationContractVersionDto;
  parameters: IntegrationContractParameterDto[];
}

export type IntegrationContractChangeType =
  | 'Added'
  | 'Removed'
  | 'RequiredChanged'
  | 'TypeChanged'
  | 'CardinalityChanged'
  | 'OrderChanged'
  | 'NamespaceChanged'
  | 'DirectionChanged';
export type IntegrationContractDiffImpact = 'MappingRequired' | 'RequiresDevelopment';

export interface IntegrationContractDiffChangeDto {
  changeType: IntegrationContractChangeType;
  technicalName: string;
  functionalName: string;
  impact: IntegrationContractDiffImpact;
  leftValue?: string | null;
  rightValue?: string | null;
}

export interface IntegrationContractDiffDto {
  leftContractVersionId: string;
  rightContractVersionId: string;
  methodId: number;
  summary: {
    added: number;
    removed: number;
    modified: number;
    requiresMapping: number;
    requiresDevelopment: number;
  };
  changes: IntegrationContractDiffChangeDto[];
}

export interface IntegrationSourceCatalogFieldDto {
  id: number;
  methodId?: number | null;
  sourceKind: IntegrationSourceKind;
  entityName: string;
  fieldPath: string;
  displayName: string;
  dataType: string;
  cardinality: IntegrationParameterCardinality;
  nullable: boolean;
  sortOrder: number;
  isActive: boolean;
}

export interface IntegrationTransformationCatalogDto {
  code: string;
  displayName: string;
  description: string;
  supportsFormatMask: boolean;
  supportsMultipleSources: boolean;
}

export interface IntegrationMappingRuleDto {
  id: number;
  mappingSetId: string;
  methodId: number;
  parameterId: number;
  sourceKind: IntegrationSourceKind;
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

export interface IntegrationMappingSetDto {
  id: string;
  methodId: number;
  methodCode: string;
  contractVersionId?: string | null;
  revision: number;
  name: string;
  version: number;
  status: IntegrationMappingStatus;
  isActive: boolean;
  notes: string;
  publishedAtUtc?: string | null;
  publishedBy: string;
  rules: IntegrationMappingRuleDto[];
}

export interface IntegrationMappingValidationIssueDto {
  severity: string;
  code: string;
  message: string;
  path: string;
  category: string;
}

export interface IntegrationMappingParameterValidationDto {
  parameterId: number;
  parameterPath: string;
  required: boolean;
  status: string;
  resolutionKind: string;
  hints: string[];
}

export interface IntegrationMappingCoverageSummaryDto {
  totalParameters: number;
  validParameters: number;
  incompleteParameters: number;
  invalidParameters: number;
  inactiveParameters: number;
  coveredByDefaultOrFixed: number;
  coveredBySourceField: number;
}

export type IntegrationMappingWorkspaceParameterStatus = 'Complete' | 'Pending' | 'Invalid' | 'NotApplicable';

export interface IntegrationMappingWorkspaceMappingSetDto {
  id: string;
  name: string;
  status: IntegrationMappingStatus;
  revision: number;
}

export interface IntegrationMappingWorkspaceContractDto {
  id: string;
  versionLabel: string;
  targetNamespace: string;
}

export interface IntegrationMappingWorkspaceCoverageDto {
  total: number;
  configured: number;
  pendingRequired: number;
  complete: number;
  pending: number;
  invalid: number;
  notApplicable: number;
}

export interface IntegrationMappingWorkspacePublicationDto {
  canPublish: boolean;
  pendingRequired: number;
  newRequiredPending: number;
  blockingIssues: number;
}

export interface IntegrationMappingWorkspaceTechnicalDetailsDto {
  parameterPath: string;
  xmlElementName: string;
  dataType: string;
  xsdType: string;
  targetNamespace?: string | null;
  sortOrder: number;
}

export interface IntegrationMappingWorkspaceCurrentRuleDto {
  ruleId?: number | null;
  sourceCatalogFieldId?: number | null;
  sourceKind?: IntegrationSourceKind | null;
  sourceFieldPath?: string | null;
  transformationCode?: string | null;
  formatMask?: string | null;
  priority: number;
  enabled: boolean;
}

export interface IntegrationMappingWorkspaceTransformationOptionDto {
  code: string;
  displayName: string;
  description: string;
  requiresFormatMask: boolean;
}

export interface IntegrationMappingWorkspaceSourceCompatibilityDto {
  parameterId: number;
  sourceCatalogFieldId: number;
  allowsNoTransformation: boolean;
  transformations: IntegrationMappingWorkspaceTransformationOptionDto[];
}

export interface IntegrationMappingWorkspaceParameterDto {
  parameterId: number;
  functionalName: string;
  functionalDescription: string;
  technicalName: string;
  required: boolean;
  dataType: string;
  category: string;
  status: IntegrationMappingWorkspaceParameterStatus;
  source?: string | null;
  transformation?: string | null;
  currentRule?: IntegrationMappingWorkspaceCurrentRuleDto | null;
  technicalDetails: IntegrationMappingWorkspaceTechnicalDetailsDto;
}

export interface IntegrationMappingWorkspaceSourceGroupDto {
  key: string;
  label: string;
  fields: IntegrationSourceCatalogFieldDto[];
}

export interface IntegrationMappingWorkspaceDto {
  mappingSet: IntegrationMappingWorkspaceMappingSetDto;
  contract?: IntegrationMappingWorkspaceContractDto | null;
  coverage: IntegrationMappingWorkspaceCoverageDto;
  publication: IntegrationMappingWorkspacePublicationDto;
  parameters: IntegrationMappingWorkspaceParameterDto[];
  sourceGroups: IntegrationMappingWorkspaceSourceGroupDto[];
  transformations: IntegrationTransformationCatalogDto[];
  sourceCompatibility: IntegrationMappingWorkspaceSourceCompatibilityDto[];
}

export interface IntegrationMappingValidationResultDto {
  mappingSetId: string;
  isValid: boolean;
  issues: IntegrationMappingValidationIssueDto[];
  coverage: IntegrationMappingCoverageSummaryDto;
  parameters: IntegrationMappingParameterValidationDto[];
}

export interface IntegrationMappingPreviewItemDto {
  parameterId: number;
  parameterPath: string;
  resolvedFrom: string;
  previewValue?: string | null;
  sourceSection: string;
  resolutionKind: string;
  appliedTransformation?: string | null;
  priority: number;
  enabled: boolean;
  functionalName?: string;
  functionalDescription?: string;
  required?: boolean;
  sourceDisplayName?: string | null;
  transformationDisplayName?: string | null;
  validationStatus?: string;
  validationIssues?: IntegrationMappingValidationIssueDto[];
  isMasked?: boolean;
}

export interface IntegrationMappingPreviewResultDto {
  mappingSetId: string;
  methodId: number;
  methodCode: string;
  contextMode: string;
  items: IntegrationMappingPreviewItemDto[];
  payloadPreviewJson: string;
  rawPreviewJson: string;
  validation?: IntegrationMappingValidationResultDto;
}

export interface IntegrationMappingSetHistoryDto {
  id: string;
  mappingSetId: string;
  methodId: number;
  version: number;
  status: IntegrationMappingStatus;
  action: string;
  performedBy: string;
  performedAtUtc: string;
  snapshotHash: string;
}

export interface IntegrationContractMappingValidationResultDto {
  contractVersionId: string;
  contractRevision: number;
  mappingSetId: string;
  mappingRevision: number;
  isComplete: boolean;
  mappingValidation: IntegrationMappingValidationResultDto;
}

export type IntegrationMappingContractCloneClassification = 'Copied' | 'New' | 'Removed' | 'Incompatible';

export interface IntegrationMappingContractCloneItemDto {
  sourceParameterPath?: string | null;
  targetParameterPath?: string | null;
  sourceParameterId?: number | null;
  targetParameterId?: number | null;
  classification: IntegrationMappingContractCloneClassification;
  copiedRuleCount: number;
  reason: string;
}

export interface IntegrationMappingContractCloneResultDto {
  sourceMappingSetId: string;
  sourceContractVersionId: string;
  targetContractVersionId: string;
  targetMappingSet: IntegrationMappingSetDto;
  classifications: IntegrationMappingContractCloneItemDto[];
  validation: IntegrationContractMappingValidationResultDto;
}

export interface IntegrationContractMappingTransitionResultDto {
  transition: string;
  contract: IntegrationContractVersionDto;
  mappingSet: IntegrationMappingSetDto;
  previousContractVersionId?: string | null;
  previousMappingSetId?: string | null;
  validation: IntegrationMappingValidationResultDto;
  transitionedAtUtc: string;
  transitionedBy: string;
}

export type IntegrationContractArtifactKind = 'Unknown' | 'Wsdl11' | 'XmlSchema';

export interface IntegrationContractArtifactIssueDto {
  code: string;
  message: string;
}

export interface IntegrationContractArtifactParameterDto {
  parameterPath: string;
  xmlElementName: string;
  displayName: string;
  dataType: string;
  xsdType: string;
  direction: IntegrationParameterDirection;
  cardinality: IntegrationParameterCardinality;
  required: boolean;
  nillable: boolean;
  minOccurs: number;
  maxOccurs?: number | null;
  supportStatus: string;
  sortOrder: number;
}

export interface IntegrationContractArtifactAnalysisDto {
  fileName: string;
  artifactKind: IntegrationContractArtifactKind;
  sizeBytes: number;
  sha256: string;
  operationName?: string | null;
  targetNamespace?: string | null;
  soapAction?: string | null;
  availableOperations: string[];
  importable: boolean;
  issues: IntegrationContractArtifactIssueDto[];
  parameters: IntegrationContractArtifactParameterDto[];
}

export interface IntegrationContractArtifactPreviewDto {
  analysis: IntegrationContractArtifactAnalysisDto;
  diff?: IntegrationContractDiffDto | null;
}

export interface IntegrationContractArtifactImportResultDto {
  analysis: IntegrationContractArtifactAnalysisDto;
  contract: IntegrationContractVersionDetailDto;
}

export type IntegrationOperatingMode = 'Live' | 'DryRun' | 'Disabled';
export type IntegrationConnectionState = 'RealSendEnabled' | 'Simulation' | 'Disabled' | 'Unknown';

export interface IntegrationConnectionInputMappingDto {
  inputName: string;
  soapParameterName: string;
  defaultValue?: string | null;
  required: boolean;
}

export interface IntegrationConnectionCapabilitiesDto {
  configurationStateKnown: boolean;
  canRequestRealSend: boolean;
  blockingReason?: string | null;
}

export interface IntegrationConnectionMethodDto {
  methodName: string;
  endpoint: string;
  soapAction: string;
  operatingMode: IntegrationOperatingMode;
  enabled: boolean;
  sanitizedEndpoint: string;
  sanitizedSoapAction: string;
  configurationState: IntegrationConnectionState;
  capabilities?: IntegrationConnectionCapabilitiesDto | null;
  inputParameterMappings: IntegrationConnectionInputMappingDto[];
}

export interface IntegrationConnectionSettingsDto {
  revision: string;
  expectedRevision?: string | null;
  wscfaachMappings: IntegrationConnectionMethodDto[];
  wsAxonRespuestaTransaccionesMappings: IntegrationConnectionMethodDto[];
}

export interface IntegrationConnectionChangePreviewRequestDto {
  methodName: string;
  endpoint: string;
  soapAction: string;
  operatingMode: IntegrationOperatingMode;
  enabled: boolean;
  expectedRevision: string;
}

export interface IntegrationConnectionChangePreviewDto {
  methodName: string;
  revision: string;
  currentConfigurationState: IntegrationConnectionState;
  requestedConfigurationState: IntegrationConnectionState;
  sanitizedEndpoint: string;
  sanitizedSoapAction: string;
  wouldEnableRealSend: boolean;
  canApply: boolean;
  blockingReason?: string | null;
}
