import { OperationalErrorView } from '../models/operational-error.model';
import {
  ApplicationDownloadError,
  ApplicationProblemDetails
} from '../services/blob-download.service';

interface ErrorDefinition {
  title: string;
  message: string;
  action: string;
  retryable: boolean;
  correctionLabel?: string;
  correctionRoute?: string;
}

const certificateCorrection = {
  correctionLabel: 'Ir a Certificados de seguridad NACHA-M',
  correctionRoute: '/nacha-security/certificates'
};

export const NACHA_ERROR_CATALOG: Readonly<Record<string, ErrorDefinition>> = {
  NACHA_PROFILE_NOT_PUBLISHED: {
    title: 'No hay un perfil NACHA-M publicado',
    message: 'La cámara seleccionada no tiene un perfil publicado para generar el archivo.',
    action: 'Publica el perfil correspondiente en Configuración NACHA-M y vuelve a intentar.',
    retryable: true
  },
  NACHA_PROFILE_NOT_EFFECTIVE: {
    title: 'El perfil NACHA-M no está vigente',
    message: 'El perfil configurado no aplica para la fecha operativa del ciclo.',
    action: 'Revisa la vigencia del perfil en Configuración NACHA-M y vuelve a intentar.',
    retryable: true
  },
  NACHA_PROFILE_AMBIGUOUS: {
    title: 'Hay más de un perfil NACHA-M aplicable',
    message: 'La configuración no permite determinar de forma única qué perfil debe utilizarse.',
    action: 'Corrige las vigencias superpuestas en Configuración NACHA-M.',
    retryable: true
  },
  NACHA_CLEARING_HOUSE_PROFILE_NOT_CONFIGURED: {
    title: 'La cámara no tiene un perfil NACHA-M configurado',
    message: 'No existe una configuración aplicable para esta cámara compensadora.',
    action: 'Configura el perfil de la cámara y vuelve a intentar.',
    retryable: true
  },
  NACHA_CLEARING_HOUSE_PROFILE_AMBIGUOUS: {
    title: 'La configuración de la cámara es ambigua',
    message: 'Existe más de una configuración aplicable para la cámara y la fecha operativa.',
    action: 'Corrige las configuraciones superpuestas antes de generar el archivo.',
    retryable: true
  },
  NACHA_REQUIRED_RECORD_MISSING: {
    title: 'Falta un registro obligatorio en el archivo NACHA-M',
    message: 'La información del ciclo no permite construir uno de los registros requeridos.',
    action: 'Corrige la información operativa indicada y vuelve a intentar.',
    retryable: true
  },
  NACHA_REQUIRED_FIELD_MISSING: {
    title: 'Falta información obligatoria para generar el archivo',
    message: 'Una transacción o entidad relacionada no tiene un dato requerido por NACHA-M.',
    action: 'Corrige el dato indicado en la información para soporte y vuelve a intentar.',
    retryable: true
  },
  NACHA_FIELD_SOURCE_NOT_FOUND: {
    title: 'No se encontró el origen de un dato NACHA-M',
    message: 'La configuración del perfil referencia un dato que no está disponible en el ciclo.',
    action: 'Revisa la regla y el origen del campo en Configuración NACHA-M.',
    retryable: true
  },
  NACHA_FIELD_RULE_FAILED: {
    title: 'No se puede generar el archivo NACHA-M',
    message: 'Una de las transacciones no tiene toda la información requerida para construir el registro de detalle.',
    action: 'Corrige la información de la transacción y vuelve a intentar.',
    retryable: true
  },
  CERTIFICATE_NOT_FOUND: {
    title: 'No se encontró el certificado requerido',
    message: 'No existe un certificado configurado para completar esta operación.',
    action: 'Carga o activa el certificado correspondiente y vuelve a intentar.',
    retryable: true,
    ...certificateCorrection
  },
  CERTIFICATE_INACTIVE: {
    title: 'El certificado no está activo',
    message: 'El certificado requerido está registrado, pero no está habilitado para uso operativo.',
    action: 'Activa una versión válida del certificado y vuelve a intentar.',
    retryable: true,
    ...certificateCorrection
  },
  CERTIFICATE_EXPIRED: {
    title: 'El certificado está vencido',
    message: 'El certificado requerido para completar la operación ya no está vigente.',
    action: 'Activa una versión vigente en Certificados de seguridad NACHA-M.',
    retryable: true,
    ...certificateCorrection
  },
  CERTIFICATE_NOT_YET_VALID: {
    title: 'El certificado aún no está vigente',
    message: 'La fecha de inicio de vigencia del certificado es posterior a la fecha actual.',
    action: 'Selecciona o activa un certificado que ya se encuentre vigente.',
    retryable: true,
    ...certificateCorrection
  },
  CERTIFICATE_PURPOSE_INVALID: {
    title: 'El certificado no corresponde a esta operación',
    message: 'El propósito configurado no permite utilizar el certificado en este proceso.',
    action: 'Configura un certificado con el propósito correcto.',
    retryable: true,
    ...certificateCorrection
  },
  CERTIFICATE_PRIVATE_KEY_REQUIRED: {
    title: 'Se requiere una clave privada',
    message: 'El certificado seleccionado no dispone de la clave privada necesaria.',
    action: 'Configura una identidad privada protegida para esta cámara.',
    retryable: true,
    ...certificateCorrection
  },
  CERTIFICATE_PRIVATE_KEY_UNAVAILABLE: {
    title: 'La clave privada no está disponible',
    message: 'No fue posible acceder al material privado desde el almacenamiento seguro.',
    action: 'Verifica la configuración del certificado y del almacenamiento seguro.',
    retryable: true,
    ...certificateCorrection
  },
  SIGNING_CERTIFICATE_NOT_FOUND: {
    title: 'No hay un certificado de firma disponible',
    message: 'No existe un certificado activo y vigente para firmar el sobre digital.',
    action: 'Carga o activa un certificado de firma de salida.',
    retryable: true,
    ...certificateCorrection
  },
  DIGITAL_ENVELOPE_CONFIGURATION_INVALID: {
    title: 'La configuración del sobre digital está incompleta',
    message: 'No se pudieron resolver todos los certificados requeridos para proteger el archivo.',
    action: 'Revisa la cobertura criptográfica de la cámara y vuelve a intentar.',
    retryable: true,
    ...certificateCorrection
  },
  FILE_TOO_LARGE: {
    title: 'El archivo supera el tamaño permitido',
    message: 'El archivo seleccionado es mayor al límite admitido por la operación.',
    action: 'Selecciona un archivo de hasta 50 MB.',
    retryable: true
  },
  ENVELOPE_INVALID: {
    title: 'El archivo no es un sobre digital válido',
    message: 'El contenido no cumple la estructura esperada para un sobre digital NACHA-M.',
    action: 'Verifica el archivo recibido y vuelve a seleccionarlo.',
    retryable: true
  },
  ENVELOPE_INTEGRITY_INVALID: {
    title: 'No se confirmó la integridad del archivo',
    message: 'El sobre digital parece alterado o corrupto y no puede procesarse de forma segura.',
    action: 'Solicita nuevamente el archivo a la entidad de origen.',
    retryable: false
  },
  SIGNED_CONTENT_INVALID: {
    title: 'El contenido firmado no es válido',
    message: 'No fue posible validar o recuperar el contenido firmado del sobre digital.',
    action: 'Verifica el origen del archivo y el certificado de firma configurado.',
    retryable: false,
    ...certificateCorrection
  }
};

const FIELD_LABELS: Readonly<Record<string, string>> = {
  INDIVIDUALNAME: 'Nombre del receptor',
  RECEIVERNAME: 'Nombre del receptor',
  COMPANYNAME: 'Nombre de la entidad originadora',
  RECEIVINGDFIIDENTIFICATION: 'Entidad receptora',
  DFIACCOUNTNUMBER: 'Cuenta receptora'
};

export function presentNachaError(
  error: ApplicationDownloadError,
  fallbackTitle = 'No fue posible completar la operación'
): OperationalErrorView {
  const code = error.errorCode?.trim();
  const definition = code ? NACHA_ERROR_CATALOG[code] : undefined;
  const details = error.problem;
  const fieldCode = details?.fieldCode ?? details?.fieldName;
  const fieldDisplayName = details?.fieldDisplayName
    ?? (fieldCode ? FIELD_LABELS[fieldCode.toUpperCase()] : undefined);

  let message = definition?.message ?? safeOperationalMessage(error.message);
  let action = definition?.action ?? 'Revisa la información e inténtalo nuevamente.';
  if (code === 'NACHA_FIELD_RULE_FAILED' && fieldDisplayName) {
    message = `Una de las transacciones no tiene registrado el dato “${fieldDisplayName.toLocaleLowerCase('es-CO')}”. Este dato es obligatorio para construir el registro de detalle.`;
    action = `Corrige ${fieldDisplayName.toLocaleLowerCase('es-CO')} y vuelve a intentar.`;
  }

  return {
    title: definition?.title ?? safeTitle(error.title, fallbackTitle),
    message,
    action,
    severity: 'error',
    retryable: definition?.retryable ?? (error.status !== 401 && error.status !== 403),
    correctionLabel: definition?.correctionLabel,
    correctionRoute: definition?.correctionRoute,
    support: {
      errorCode: code,
      traceId: error.traceId,
      ruleId: details?.ruleId,
      recordType: details?.recordType,
      fieldCode,
      fieldDisplayName,
      startPosition: details?.startPosition,
      expectedLength: details?.expectedLength,
      reason: details?.reason ?? details?.cause,
      endpoint: details?.instance
    }
  };
}

export function applicationErrorFromUnknown(
  problem: ApplicationProblemDetails | undefined,
  status: number,
  fallback: string
): ApplicationDownloadError {
  return new ApplicationDownloadError(
    problem?.detail ?? problem?.message ?? fallback,
    status,
    problem?.errorCode ?? problem?.code,
    problem?.traceId,
    problem?.title,
    problem
  );
}

function safeOperationalMessage(value: string): string {
  if (!value || /RuleId=|ExpectedLength=|LongitudEsperada=|Field=|Campo=/i.test(value)) {
    return 'La operación no pudo completarse porque no se cumplieron todas las condiciones requeridas.';
  }
  return value;
}

function safeTitle(value: string | undefined, fallback: string): string {
  if (!value || /^[A-Z0-9_]+$/.test(value)) {
    return fallback;
  }
  return value;
}
