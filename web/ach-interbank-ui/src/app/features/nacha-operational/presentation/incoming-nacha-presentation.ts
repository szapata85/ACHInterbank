export type OperationalTone = 'success' | 'warning' | 'danger' | 'info' | 'neutral';

export function operationalTone(value: string | null | undefined): OperationalTone {
  const normalized = (value ?? '').toLocaleLowerCase('es-CO');
  if (/(error técnico|fall|rechaz|bloquead)/.test(normalized)) return 'danger';
  if (/(novedad|devuelt|reintento|pendiente|parcial)/.test(normalized)) return 'warning';
  if (/(exitos|correctamente|complet|procesado)/.test(normalized)) return 'success';
  if (/(curso|validando|programado|recibido)/.test(normalized)) return 'info';
  return 'neutral';
}

export function logicalServiceName(operation: string | null | undefined): string {
  switch ((operation ?? '').replaceAll('_', '').toLocaleLowerCase('es-CO')) {
    case 'proctransacciones': return 'Procesamiento de transacciones';
    case 'proccontrapartidas': return 'Procesamiento de contrapartidas';
    case 'registrarrespuestatransaccion': return 'Registro de respuesta de transacción';
    default: return operation ? 'Servicio de integración ACH' : 'Pendiente de asignación';
  }
}

export function technicalErrorMessage(code: string | null | undefined, message: string | null | undefined): string {
  const normalized = `${code ?? ''} ${message ?? ''}`.toLocaleLowerCase('es-CO');
  if (normalized.includes('timeout') || normalized.includes('tiempo')) {
    return 'El servicio no respondió dentro del tiempo esperado. La transacción conserva su programación de recuperación.';
  }
  if (normalized.includes('connection') || normalized.includes('conex')) {
    return 'No fue posible establecer comunicación con el servicio. La transacción no obtuvo un resultado funcional.';
  }
  return message?.trim()
    ? 'El procesamiento presentó una dificultad técnica controlada. Consulte la información de soporte para ampliar el diagnóstico.'
    : 'No se registraron detalles técnicos adicionales.';
}

export function abbreviatedIdentifier(value: string | null | undefined): string {
  if (!value) return 'No disponible';
  return value.length <= 12 ? value : `${value.slice(0, 8)}…${value.slice(-4)}`;
}
