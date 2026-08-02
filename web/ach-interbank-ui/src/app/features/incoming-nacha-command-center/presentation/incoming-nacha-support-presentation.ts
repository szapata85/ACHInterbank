const STATUS_LABELS: Readonly<Record<string, string>> = {
  Received: 'Recibido',
  PreValidating: 'Validando información inicial',
  Decrypting: 'Descifrando archivo',
  HeaderParsing: 'Leyendo encabezado',
  ValidatingCycle: 'Validando ciclo',
  Parsing: 'Interpretando contenido',
  ValidatingContent: 'Validando transacciones',
  Persisting: 'Guardando información',
  Persisted: 'Carga completada',
  Rejected: 'Rechazado',
  Failed: 'Error técnico',
  Pending: 'Pendiente de programación',
  Scheduled: 'Programado',
  Dispatching: 'Enviando al servicio',
  RetryPending: 'Pendiente de reintento',
  WaitingWindow: 'Esperando ventana operativa',
  Blocked: 'Bloqueado',
  Confirmed: 'Procesado',
  Completed: 'Procesado',
  PartiallyCompleted: 'Procesado parcialmente',
  FailedFinal: 'Error definitivo',
  Cancelled: 'Cancelado',
  Applied: 'Aplicada',
  ManualApplied: 'Aplicada manualmente',
  ManualRejected: 'Rechazada manualmente'
};

const ACTION_LABELS: Readonly<Record<string, string>> = {
  retry: 'Reintentar procesamiento',
  unblock: 'Desbloquear procesamiento',
  requeue: 'Reprogramar procesamiento',
  'mark-failed-final': 'Cerrar con error definitivo'
};

export function supportStatusLabel(value: string | null | undefined): string {
  if (!value?.trim()) return 'No disponible';
  return STATUS_LABELS[value] ?? value;
}

export function supportActionLabel(value: string): string {
  return ACTION_LABELS[value] ?? 'Acción de soporte';
}

export function supportActionsLabel(values: readonly string[] | null | undefined): string {
  return values?.length ? values.map(supportActionLabel).join(', ') : 'Ninguna disponible';
}

export function supportOutcomeLabel(isReplay: boolean, previousStatus: string, currentStatus: string): string {
  if (isReplay) return 'Solicitud ya atendida';
  return currentStatus === previousStatus ? 'Acción no aplicada' : 'Acción aplicada';
}
