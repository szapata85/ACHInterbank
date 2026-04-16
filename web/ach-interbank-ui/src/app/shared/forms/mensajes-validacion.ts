import { ValidationErrors } from '@angular/forms';

export const MENSAJES_VALIDACION_POR_DEFECTO: Record<string, (error: any) => string> = {
  required: () => 'Este campo es obligatorio.',
  email: () => 'Ingrese un correo electrónico válido.',
  minlength: (e) => `Debe tener mínimo ${e.requiredLength} caracteres.`,
  maxlength: (e) => `Debe tener máximo ${e.requiredLength} caracteres.`,
  min: (e) => `El valor mínimo permitido es ${e.min}.`,
  max: (e) => `El valor máximo permitido es ${e.max}.`,
  pattern: () => 'El formato ingresado no es válido.'
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
    return 'El valor ingresado no es válido.';
  }

  return fabrica(errors[primerCodigo]);
}
