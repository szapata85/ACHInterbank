export interface TechnicalPresentation {
  functionalName: string;
  technicalValue: string;
}

const PROFILE_NAMES: Readonly<Record<string, string>> = {
  LEGACY_ACH_SALIDA_ORIGINAL_V1_0: 'Perfil heredado de salidas ACH'
};

const VARIANT_NAMES: Readonly<Record<string, string>> = {
  LEGACY_R1_BASE: 'Variante base de la cabecera de archivo',
  LEGACY_R5_BASE: 'Variante base de la cabecera de lote',
  LEGACY_R6_BASE: 'Variante base del detalle de transacción',
  LEGACY_R7_BASE: 'Variante base de información adicional',
  LEGACY_R8_BASE: 'Variante base del control de lote',
  LEGACY_R9_BASE: 'Variante base del control de archivo'
};

const FIELD_NAMES_BY_SUFFIX: Readonly<Record<string, string>> = {
  PRIORITYCODE: 'Código de prioridad',
  IMMEDIATEDESTINATION: 'Destino inmediato',
  IMMEDIATEORIGIN: 'Origen inmediato',
  FILECREATIONDATE: 'Fecha de creación del archivo',
  FILECREATIONTIME: 'Hora de creación del archivo',
  FILEIDMODIFIER: 'Modificador de identificación del archivo',
  RECORDSIZE: 'Longitud del registro',
  BLOCKINGFACTOR: 'Factor de bloqueo',
  FORMATCODE: 'Código de formato',
  IMMEDIATEDESTINATIONNAME: 'Nombre del destino inmediato',
  IMMEDIATEORIGINNAME: 'Nombre del origen inmediato',
  REFERENCECODE: 'Código de referencia',
  SERVICECLASSCODE: 'Código de clase de servicio',
  COMPANYNAME: 'Nombre de la empresa',
  COMPANYDISCRETIONARYDATA: 'Datos discrecionales de la empresa',
  COMPANYIDENTIFICATION: 'Identificación de la empresa',
  STANDARDENTRYCLASSCODE: 'Código de clase de entrada estándar',
  COMPANYENTRYDESCRIPTION: 'Descripción de la entrada de la empresa',
  COMPANYDESCRIPTIVEDATE: 'Fecha descriptiva de la empresa',
  EFFECTIVEENTRYDATE: 'Fecha efectiva de la entrada',
  SETTLEMENTDATE: 'Fecha de liquidación',
  ORIGINATORSTATUSCODE: 'Código de estado del originador',
  ORIGINATINGDFI: 'Institución financiera originadora',
  BATCHNUMBER: 'Número de lote',
  TRANSACTIONCODE: 'Código de transacción',
  RECEIVINGDFI: 'Institución financiera receptora',
  CHECKDIGIT: 'Dígito de verificación',
  ACCOUNTNUMBER: 'Número de cuenta',
  AMOUNT: 'Valor de la transacción',
  RECIPIENTIDNUMBER: 'Identificación del receptor',
  RECEIVERNAME: 'Nombre del receptor',
  DISCRETIONARYDATA: 'Datos discrecionales',
  ADDENDUMINDICATOR: 'Indicador de información adicional',
  TRACENUMBER: 'Número de seguimiento',
  ADDENDATYPE: 'Tipo de información adicional',
  INFORMATION: 'Información adicional',
  SEQUENCENUMBER: 'Número de secuencia',
  ENTRYDETAILSEQUENCENUMBER: 'Secuencia del detalle de la entrada',
  ENTRYADDENDACOUNT: 'Cantidad de entradas e información adicional',
  ENTRYHASH: 'Suma de verificación de entradas',
  TOTALDEBITAMOUNT: 'Valor total de débitos',
  TOTALCREDITAMOUNT: 'Valor total de créditos',
  MESSAGEAUTHENTICATIONCODE: 'Código de autenticación del mensaje',
  BATCHCOUNT: 'Cantidad de lotes',
  BLOCKCOUNT: 'Cantidad de bloques'
};

const STATUS_NAMES: Readonly<Record<string, string>> = {
  BORRADOR: 'Borrador',
  PUBLICADO: 'Publicado',
  INACTIVO: 'Inactivo',
  ACTIVO: 'Activo',
  ARCHIVADO: 'Archivado',
  DEPRECADO: 'Obsoleto',
  ENABLED: 'Habilitado',
  DISABLED: 'Inactivo'
};

const SOURCE_TYPE_NAMES: Readonly<Record<string, string>> = {
  ENTIDAD: 'Entidad del dominio',
  CONSTANTE: 'Valor constante',
  EXPRESION: 'Expresión calculada',
  SQL: 'Consulta de base de datos',
  SQL_VIEW: 'Vista de base de datos',
  SQL_PROCEDURE: 'Procedimiento de base de datos',
  CATALOGO_EXTERNO: 'Catálogo externo',
  TABLE_DRIVEN: 'Configuración parametrizada'
};

const FLOW_NAMES: Readonly<Record<string, string>> = {
  ORIGINAL: 'Operaciones originales',
  DEVOLUCION: 'Devoluciones',
  RETORNO: 'Devoluciones',
  ENTRADA: 'Entradas',
  SALIDA: 'Salidas'
};

const DIRECTION_NAMES: Readonly<Record<string, string>> = {
  ENTRADA: 'Entrada',
  SALIDA: 'Salida',
  INBOUND: 'Entrada',
  OUTBOUND: 'Salida'
};

const SERVICE_NAMES: Readonly<Record<string, string>> = {
  PPD: 'Pagos y depósitos preacordados',
  CCD: 'Créditos y débitos corporativos',
  CTX: 'Intercambio corporativo',
  WEB: 'Débitos autorizados por internet',
  TEL: 'Débitos autorizados por teléfono'
};

const SEVERITY_NAMES: Readonly<Record<string, string>> = {
  ERROR: 'Error bloqueante',
  WARN: 'Advertencia',
  WARNING: 'Advertencia'
};

const TECHNICAL_NAME_PATTERN =
  /\b(?:legacy|layout|profile|record|variant|field|source|property|path|default|enabled|disabled|required|warning|left|right|padding|format|mask|fallback|pipeline|expression|entity|constant|table|driven|draft|published|read|only|priority|immediate|destination|origin|file|creation|time|modifier|size|blocking|factor|code|name|service|class|company|identification|entry|effective|settlement|batch|transaction|receiving|check|digit|account|amount|recipient|receiver|discretionary|addendum|indicator|trace|information|sequence|hash|total|debit|credit|message|authentication|count|number)\b/i;

export function profilePresentation(profileCode: string, persistedName?: string | null): TechnicalPresentation {
  return {
    functionalName: PROFILE_NAMES[normalizeCode(profileCode)]
      ?? preferredPersistedName(persistedName)
      ?? readableFallback(profileCode, 'Perfil de configuración'),
    technicalValue: profileCode
  };
}

export function variantPresentation(
  variantCode: string,
  persistedName?: string | null,
  recordCode?: string | null
): TechnicalPresentation {
  const normalizedCode = normalizeCode(variantCode);
  const conventionalVariant = /^R(\d+)_(BASE|ALT)$/.exec(normalizedCode);
  const conventionalName = conventionalVariant
    ? conventionalVariant[2] === 'BASE'
      ? `Variante base del registro ${conventionalVariant[1]}`
      : `Variante alternativa del registro ${conventionalVariant[1]}`
    : null;
  return {
    functionalName: VARIANT_NAMES[normalizedCode]
      ?? preferredPersistedName(persistedName)
      ?? conventionalName
      ?? readableFallback(variantCode, recordCode ? `Variante del registro ${recordCode}` : 'Variante configurada'),
    technicalValue: variantCode
  };
}

export function fieldPresentation(fieldCode: string, persistedName?: string | null): TechnicalPresentation {
  const normalizedCode = normalizeCode(fieldCode);
  const suffix = normalizedCode.replace(/^R\d+_/, '');
  return {
    functionalName: preferredPersistedName(persistedName)
      ?? FIELD_NAMES_BY_SUFFIX[suffix]
      ?? readableFallback(suffix, 'Campo configurado'),
    technicalValue: fieldCode
  };
}

export function statusPresentation(value?: string | null): string {
  return codeLabel(value, STATUS_NAMES, value ? 'Estado configurado' : 'Estado pendiente');
}

export function sourceTypePresentation(value?: string | null): string {
  return codeLabel(value, SOURCE_TYPE_NAMES, value ? 'Origen configurado' : 'Tipo de origen pendiente');
}

export function sourceStrategyPresentation(value?: string | null): string {
  return codeLabel(value, SOURCE_TYPE_NAMES, value ? 'Estrategia configurada' : 'Estrategia pendiente');
}

export function flowPresentation(value?: string | null): string {
  return codeLabel(value, FLOW_NAMES, value ? 'Flujo configurado' : 'Flujo pendiente');
}

export function directionPresentation(value?: string | null): string {
  return codeLabel(value, DIRECTION_NAMES, value ? 'Dirección configurada' : 'Dirección pendiente');
}

export function servicePresentation(value?: string | null): string {
  return codeLabel(value, SERVICE_NAMES, value ? 'Servicio configurado' : 'Sin clase de servicio');
}

export function severityPresentation(value?: string | null): string {
  return codeLabel(value, SEVERITY_NAMES, value ? 'Severidad configurada' : 'Severidad pendiente');
}

export function justificationPresentation(value?: string | null): string {
  const code = normalizeCode(value);
  if (code === 'R') {
    return 'Alineación a la derecha';
  }
  if (code === 'L') {
    return 'Alineación a la izquierda';
  }
  return code ? 'Alineación configurada' : 'Alineación pendiente';
}

function preferredPersistedName(value?: string | null): string | null {
  const name = value?.trim();
  if (!name || TECHNICAL_NAME_PATTERN.test(splitTechnicalWords(name))) {
    return null;
  }
  return name;
}

function splitTechnicalWords(value: string): string {
  return value
    .replace(/([a-záéíóúñ])([A-Z])/g, '$1 $2')
    .replace(/[_-]+/g, ' ');
}

function readableFallback(value: string, fallback: string): string {
  const wordTranslations: Readonly<Record<string, string>> = {
    FIELD: 'Campo',
    RECORD: 'Registro',
    VARIANT: 'Variante',
    PROFILE: 'Perfil',
    BASE: 'base',
    ALT: 'alternativa',
    CUSTOM: 'personalizado',
    AMOUNT: 'valor',
    TRANSACTION: 'transacción',
    REFERENCE: 'referencia',
    CODE: 'código',
    NAME: 'nombre'
  };
  const readable = splitTechnicalWords(value)
    .split(/\s+/)
    .map(word => wordTranslations[word.toUpperCase()] ?? word)
    .join(' ')
    .toLocaleLowerCase('es')
    .replace(/^\w/, character => character.toLocaleUpperCase('es'))
    .trim();
  return readable || fallback;
}

function codeLabel(
  value: string | null | undefined,
  catalog: Readonly<Record<string, string>>,
  fallback: string
): string {
  const code = normalizeCode(value);
  return catalog[code] ?? fallback;
}

function normalizeCode(value?: string | null): string {
  return value?.trim().toUpperCase() ?? '';
}
