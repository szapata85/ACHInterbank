export interface NachaConfigProfileListItem {
  id: number;
  profileCode: string;
  nombreEs: string;
  estado: string;
  camara: string;
  flujo: string;
  direccion: string;
  servicio?: string | null;
  versionMajor: number;
  versionMinor: number;
  effectiveFrom: string;
  effectiveTo?: string | null;
  rowVersion: string;
}

export interface NachaConfigProfileDetail extends NachaConfigProfileListItem {
  descripcion?: string | null;
  estadoNombre?: string | null;
  isEditable?: boolean;
  camaraNombre?: string | null;
  flujoNombre?: string | null;
  direccionNombre?: string | null;
  servicioNombre?: string | null;
  contextPriority: number;
  records: NachaConfigProfileRecord[];
  variantes: NachaConfigLayoutVariant[];
}

export interface NachaConfigWorkspace extends NachaConfigProfileDetail {
  estadoNombre: string;
  isEditable: boolean;
  camaraNombre: string;
  flujoNombre: string;
  direccionNombre: string;
  servicioNombre?: string | null;
}

export interface NachaConfigProfileRecord {
  id: number;
  recordCode: string;
  recordNameEs?: string | null;
  sequence: number;
  isEnabled: boolean;
  minOccurs: number;
  maxOccurs?: number | null;
  sourceStrategy: string;
  sourceStrategyName?: string | null;
}

export interface NachaConfigLayoutVariant {
  id: number;
  recordCode: string;
  variantCode: string;
  nombreEs: string;
  descripcion?: string | null;
  priority: number;
  isDefaultForRecord: boolean;
  totalLength: number;
  effectiveFrom?: string | null;
  effectiveTo?: string | null;
  fields: NachaConfigLayoutField[];
}

export interface NachaConfigLayoutField {
  id: number;
  fieldCode: string;
  fieldNameEs: string;
  startPosition: number;
  length: number;
  padChar?: string | null;
  justification?: string | null;
  formatMask?: string | null;
  sortOrder?: number | null;
  isVisibleInBackoffice?: boolean | null;
  transformationPipelineJson?: string | null;
  propertyPath?: string | null;
  sourceType?: string | null;
  sourceTypeName?: string | null;
  sourceDefinitionId?: number | null;
  constantValue?: string | null;
  entityName?: string | null;
  sqlObjectName?: string | null;
  expressionDsl?: string | null;
  externalCatalogCode?: string | null;
  fallbackPolicyJson?: string | null;
  isEnabled: boolean;
  reglas: NachaConfigFieldRule[];
}

export interface NachaConfigFieldRule {
  id: number;
  ruleType?: string | null;
  ruleTypeName?: string | null;
  errorCode: string;
  errorMessageEs: string;
  severity: string;
  severityName?: string | null;
  ruleCode?: string | null;
  conditionDsl?: string | null;
  ruleConfigJson?: string | null;
  order?: number | null;
  isEnabled: boolean;
}


export interface NachaConfigEditorOption {
  code: string;
  labelEs: string;
}

export interface NachaConfigEditorSourceTypeOption {
  code: string;
  labelEs: string;
  catalogLabelEs: string;
  usesFixedValue: boolean;
  requiresDefinitionSelection: boolean;
}

export interface NachaConfigEditorSourceOption {
  sourceDefinitionId: number;
  sourceTypeCode: string;
  labelEs: string;
  descriptionEs?: string | null;
}

export interface NachaConfigFieldEditor {
  profileId: number;
  fieldId: number;
  isEditable: boolean;
  rowVersion: string;
  field: NachaConfigLayoutField;
  sourceTypes: NachaConfigEditorSourceTypeOption[];
  sourceOptions: NachaConfigEditorSourceOption[];
  alignments: NachaConfigEditorOption[];
  formats: NachaConfigEditorOption[];
  severities: NachaConfigEditorOption[];
  sourceReferenceCount: number;
}

export interface NachaConfigFieldSourceEditRequest {
  sourceTypeCode: string;
  sourceDefinitionId?: number | null;
  fixedValue?: string | null;
}

export interface NachaConfigFieldRuleEditorUpdateRequest {
  id: number;
  errorMessageEs: string;
  severity: string;
  isEnabled: boolean;
}

export interface NachaConfigFieldEditorUpdateRequest {
  fieldNameEs: string;
  startPosition: number;
  length: number;
  padChar: string;
  justification: string;
  formatMask?: string | null;
  isEnabled: boolean;
  source: NachaConfigFieldSourceEditRequest;
  rules: NachaConfigFieldRuleEditorUpdateRequest[];
  expectedRowVersion: string;
}

export interface NachaConfigFieldRuleEditRequest {
  errorCode: string;
  errorMessageEs: string;
  severity: 'ERROR' | 'WARN';
  isEnabled: boolean;
  expectedRowVersion: string;
}

export interface NachaConfigLayoutVariantEditRequest {
  nombreEs: string;
  descripcion?: string | null;
  priority: number;
  isDefaultForRecord: boolean;
  effectiveFrom: string;
  effectiveTo?: string | null;
  expectedRowVersion: string;
}

export interface NachaConfigLayoutFieldEditRequest {
  fieldNameEs: string;
  startPosition: number;
  length: number;
  propertyPath?: string | null;
  isEnabled: boolean;
  expectedRowVersion: string;
}

export interface NachaConfigValidationIssue {
  severidad: string;
  codigo: string;
  mensaje: string;
}

export interface NachaConfigValidationResult {
  profileId: number;
  isValid: boolean;
  erroresBloqueantes: number;
  advertencias: number;
  resumen: string;
  issues: NachaConfigValidationIssue[];
}

export interface NachaConfigOperationalIssue {
  severity: string;
  code: string;
  messageEs: string;
  suggestedActionEs?: string | null;
  recordCode?: string | null;
  recordNameEs?: string | null;
  fieldId?: number | null;
  fieldNameEs?: string | null;
  technicalMessage: string;
}

export interface NachaConfigSelectionTest {
  success: boolean;
  summaryEs: string;
  resolvedProfileId?: number | null;
  resolvedProfileCode?: string | null;
  warningsEs: string[];
  technicalTrace: string[];
}

export interface NachaConfigFieldPreview {
  fieldId: number;
  fieldNameEs: string;
  startPosition: number;
  endPosition: number;
  length: number;
  isEnabled: boolean;
  isRequired: boolean;
  sourceSummaryEs: string;
  formatSummaryEs?: string | null;
  sampleValue?: string | null;
  relevantRulesEs: string[];
}

export interface NachaConfigRecordPreview {
  variantId: number;
  recordCode: string;
  recordNameEs: string;
  variantCode: string;
  variantNameEs: string;
  totalLength: number;
  isDefaultForRecord: boolean;
  contentPreviewAvailable: boolean;
  contentPreviewMessageEs?: string | null;
  renderedRecord?: string | null;
  fields: NachaConfigFieldPreview[];
}

export interface NachaConfigOperationalTestResult {
  profileId: number;
  processDateUtc: string;
  canPublish: boolean;
  resultLabelEs: string;
  erroresBloqueantes: number;
  advertencias: number;
  issues: NachaConfigOperationalIssue[];
  selection: NachaConfigSelectionTest;
  records: NachaConfigRecordPreview[];
}

export interface NachaConfigPublicationResult {
  profileId: number;
  publicado: boolean;
  mensaje: string;
  versionMajor: number;
  versionMinor: number;
  estado: string;
  estadoNombreEs: string;
  rowVersion?: string | null;
}

export interface NachaConfigChangeItem {
  scope: string;
  changeKind: string;
  elementNameEs: string;
  recordCode?: string | null;
  recordNameEs?: string | null;
  fieldId?: number | null;
  fieldNameEs?: string | null;
  propertyNameEs: string;
  beforeValueEs?: string | null;
  afterValueEs?: string | null;
  summaryEs: string;
}

export interface NachaConfigChangeReview {
  profileId: number;
  baseProfileId?: number | null;
  hasComparisonBase: boolean;
  isEditable: boolean;
  rowVersion: string;
  currentVersion: string;
  nextVersion: string;
  camaraNombreEs: string;
  flujoNombreEs: string;
  direccionNombreEs: string;
  servicioNombreEs?: string | null;
  effectiveFrom: string;
  effectiveTo?: string | null;
  changedFieldsCount: number;
  changeCount: number;
  summaryEs: string;
  changes: NachaConfigChangeItem[];
}

export interface NachaConfigHistoryItem {
  id: number;
  changedAtUtc: string;
  changedBy: string;
  changeType: string;
  entityName: string;
  correlationId?: string | null;
  actionNameEs: string;
  elementNameEs: string;
  version: string;
  summaryEs: string;
  detailEs: string[];
}

export interface NachaConfigSnapshotItem {
  id: number;
  createdAtUtc: string;
  createdBy: string;
  snapshotType: string;
  versionMajor: number;
  versionMinor: number;
}

export interface NachaConfigResolverPreviewRequest {
  camaraCode: string;
  flujoCode: string;
  direccionCode: string;
  servicioCode?: string | null;
  processDateUtc: string;
  recordCodes: string[];
}

export interface NachaConfigResolverPreviewResult {
  success: boolean;
  profileId?: number | null;
  profileCode?: string | null;
  layoutByRecordCode: Record<string, string>;
  trace: string[];
  warnings: string[];
}

export interface NachaConfigApiError {
  errorCode: string;
  message: string;
  currentRowVersion?: string | null;
  issues?: NachaConfigValidationIssue[];
}

export interface NachaConfigFilterCatalogOption {
  code: string;
  labelEs: string;
}

export interface NachaConfigFilterCatalogs {
  estados: NachaConfigFilterCatalogOption[];
  camaras: NachaConfigFilterCatalogOption[];
  flujos: NachaConfigFilterCatalogOption[];
  direcciones: NachaConfigFilterCatalogOption[];
  servicios: NachaConfigFilterCatalogOption[];
  registros?: NachaConfigFilterCatalogOption[];
  tiposOrigen?: NachaConfigFilterCatalogOption[];
  tiposRegla?: NachaConfigFilterCatalogOption[];
}

export interface NachaConfigProfilesDashboardReadModel {
  productiveStatus: string;
  isOfficialModel: boolean;
  legacyDeprecated: boolean;
  profileCount: number;
  publishedProfileCount: number;
  currentProfileCount: number;
  layoutVariantCount: number;
  fieldCount: number;
  clearingHouses: string[];
  recordTypes: string[];
  warnings: string[];
}

export interface NachaConfigProfileReadModel {
  profileId: number;
  profileCode: string;
  profileName: string;
  clearingHouseCode: string;
  flowType: string;
  status: string;
  version: string;
  isPublished: boolean;
  isCurrent: boolean;
  effectiveFrom: string;
  effectiveTo?: string | null;
  layoutVariantCount: number;
  fieldCount: number;
  recordTypes: string[];
  isOfficialModel: boolean;
  legacyDeprecated: boolean;
}

export interface NachaConfigProfileDetailReadModel extends NachaConfigProfileReadModel {
  variants: NachaConfigProfileVariantReadModel[];
  fields: NachaConfigProfileFieldReadModel[];
}

export interface NachaConfigProfileVariantReadModel {
  variantId: number;
  variantCode: string;
  recordType: string;
  recordLength: number;
  blockingFactor: number;
  isActive: boolean;
  fieldCount: number;
}

export interface NachaConfigProfileFieldReadModel {
  fieldId: number;
  recordType: string;
  fieldName: string;
  startPosition: number;
  length: number;
  endPosition: number;
  dataType: string;
  isRequired: boolean;
  defaultValue?: string | null;
  sourceFieldPath?: string | null;
  paddingDirection: string;
  paddingChar: string;
  format?: string | null;
  isComputed: boolean;
  isControlTotalField: boolean;
}
