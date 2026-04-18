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
  priority: number;
  isDefaultForRecord: boolean;
  totalLength: number;
  fields: NachaConfigLayoutField[];
}

export interface NachaConfigLayoutField {
  id: number;
  fieldCode: string;
  fieldNameEs: string;
  startPosition: number;
  length: number;
  propertyPath?: string | null;
  sourceType?: string | null;
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
