import { IntegrationMappingValidationIssueDto } from '../data-access/integration-api.models';

export interface PresentedIntegrationValidationIssue {
  severity: string;
  code: string;
  functionalMessage: string;
  technicalMessage: string;
  path: string;
  category: string;
}

const functionalMessages: Readonly<Record<string, string>> = {
  REQUIRED_PARAMETER_MISSING: 'Este dato requerido por CFA todavía no tiene un origen configurado.',
  REQUIRED_PARAMETER_INACTIVE: 'La configuración de este dato requerido está deshabilitada.',
  REQUIRED_MAPPING_DISABLED: 'La configuración requerida para este dato no está habilitada.',
  SOURCE_NOT_DEFINED: 'El dato no tiene un origen válido configurado.',
  SOURCE_FIELD_NOT_FOUND: 'El origen configurado ya no está disponible en el catálogo del servicio.',
  TYPE_INCOMPATIBLE: 'El tipo del origen no es compatible con el dato requerido por CFA.',
  CARDINALITY_INCOMPATIBLE: 'La forma del origen no es compatible con el dato requerido por CFA.',
  TRANSFORMATION_INVALID: 'El tratamiento configurado no está disponible o no es compatible.',
  FORMAT_INVALID: 'La configuración adicional del tratamiento no es válida.',
  CONSTANT_WITHOUT_VALUE: 'La configuración fija no tiene un valor utilizable.',
  CONFLICTING_RULES_DUPLICATE_PRIORITY: 'Hay más de una regla activa con la misma prioridad para este dato.',
  PRIORITY_RESULT_INCONSISTENT: 'La prioridad de las reglas activas no permite determinar un resultado único.',
  CONDITION_NOT_ALLOWED: 'La configuración contiene una condición que no admite el editor guiado.',
  EXPRESSION_NOT_ALLOWED: 'La configuración contiene una expresión que no admite el editor guiado.',
  CONTRACT_PARAMETER_UNSUPPORTED: 'Este dato contractual requiere una capacidad que el editor guiado todavía no soporta.',
  RULE_PARAMETER_NOT_IN_CONTRACT: 'Existe una regla asociada a un dato que ya no pertenece al contrato vigente.',
  CLONED_MAPPING_INCOMPATIBLE: 'Este dato requiere una nueva configuración después del cambio de contrato.'
};

export function presentIntegrationValidationIssue(
  issue: IntegrationMappingValidationIssueDto,
  functionalName?: string | null
): PresentedIntegrationValidationIssue {
  const prefix = functionalName?.trim() ? `${functionalName.trim()}: ` : '';
  const fallback = issue.severity?.toLocaleLowerCase('es') === 'warning'
    ? 'Revisa esta configuración antes de continuar.'
    : 'Revisa la configuración de este dato.';

  return {
    severity: issue.severity,
    code: issue.code,
    functionalMessage: `${prefix}${functionalMessages[issue.code] ?? fallback}`,
    technicalMessage: issue.message,
    path: issue.path,
    category: issue.category
  };
}
