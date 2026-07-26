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
  camaraNombre?: string | null;
  contextPriority: number;
  records: NachaConfigProfileRecord[];
  variantes: NachaConfigLayoutVariant[];
}

export interface NachaConfigProfileRecord {
  id: number;
  recordCode: string;
  sequence: number;
  isEnabled: boolean;
  minOccurs: number;
  maxOccurs?: number | null;
  sourceStrategy: string;
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
  errorCode: string;
  errorMessageEs: string;
  severity: string;
  isEnabled: boolean;
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

export interface NachaConfigPublicationResult {
  profileId: number;
  publicado: boolean;
  mensaje: string;
  versionMajor: number;
  versionMinor: number;
  rowVersion?: string | null;
}

export interface NachaConfigHistoryItem {
  id: number;
  changedAtUtc: string;
  changedBy: string;
  changeType: string;
  entityName: string;
  correlationId?: string | null;
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
