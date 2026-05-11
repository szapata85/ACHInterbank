export function formatAchProcessingStatus(status: string | null | undefined): string {
  return status?.trim() ? status : '-';
}

export function getAchProcessingStatusClass(status: string | null | undefined): string {
  if (status === 'Notificada' || status === 'Homologada') return 'estado-exitoso';
  if (status === 'PendienteReintento' || status === 'RequiereRevisionManual' || status === 'NoHomologada') return 'estado-advertencia';
  if (status === 'ErrorFuncional') return 'estado-error';
  return 'estado-neutro';
}

export function formatAchNotificationStatus(status: string | null | undefined): string {
  return status?.trim() ? status : '-';
}

export function getAchNotificationStatusClass(status: string | null | undefined): string {
  if (status === 'Exitosa') return 'estado-exitoso';
  if (status === 'Pendiente' || status === 'PendienteReintento' || status === 'RequiereRevisionManual') return 'estado-advertencia';
  if (status === 'ErrorFuncional' || status === 'ErrorTecnico') return 'estado-error';
  return 'estado-neutro';
}

export function getAchManualReviewPriority(status: string | null | undefined): 'Alta' | 'Media' | 'Baja' {
  if (status === 'NoHomologada' || status === 'ErrorFuncional') return 'Alta';
  if (status === 'RequiereRevisionManual' || status === 'PendienteReintento') return 'Media';
  return 'Baja';
}

export function getAchPriorityClass(priority: string): string {
  if (priority === 'Alta') return 'prioridad-alta';
  if (priority === 'Media') return 'prioridad-media';
  return 'prioridad-baja';
}
