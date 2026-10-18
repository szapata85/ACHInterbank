import { ValidationErrors } from '@angular/forms';

export const MENSAJES_VALIDACION_POR_DEFECTO: Record<string, (error: any) => string> = {
  required: () => 'Completa este campo.',
  email: () => 'Ingresa un correo electrónico válido.',
  minlength: (e) => `Usa al menos ${e.requiredLength} caracteres.`,
  maxlength: (e) => `Usa máximo ${e.requiredLength} caracteres.`,
  min: (e) => `Ingresa un valor igual o mayor que ${e.min}.`,
  max: (e) => `Ingresa un valor igual o menor que ${e.max}.`,
  pattern: () => 'Revisa el formato del valor ingresado.'
};

export function resolverMensajeValidacion(
  errors: ValidationErrors | null | undefined,
  personalizados?: Record<string, (error: any) => string>
): string | null {
  if (!errors) {
    return null;
  }

  const mensajes = { ...MENSAJES_VALIDACION_POR_DEFECTO, ...(personalizados ?? {}) };
  const [primerCodigo] = Object.keys(errors);
  if (!primerCodigo) {
    return null;
  }

  const fabrica = mensajes[primerCodigo];
  if (!fabrica) {
    return 'Revisa el valor ingresado.';
  }

  return fabrica(errors[primerCodigo]);
}
