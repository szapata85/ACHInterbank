export type IntegrationStatusTone = 'neutral' | 'info' | 'success' | 'warning' | 'danger';

export interface IntegrationStatusPresentation {
  label: string;
  tone: IntegrationStatusTone;
}

const labels: Record<string, IntegrationStatusPresentation> = {
  Draft: { label: 'Borrador', tone: 'neutral' },
  Validated: { label: 'Validado', tone: 'info' },
  Active: { label: 'Activo', tone: 'success' },
  Retired: { label: 'Retirado', tone: 'neutral' },
  Published: { label: 'Publicada', tone: 'success' },
  Archived: { label: 'Archivada', tone: 'neutral' },
  Complete: { label: 'Completo', tone: 'success' },
  Pending: { label: 'Pendiente', tone: 'warning' },
  Invalid: { label: 'Inválido', tone: 'danger' },
  NotApplicable: { label: 'No aplica', tone: 'neutral' },
  valid: { label: 'Listo', tone: 'success' },
  incomplete: { label: 'Incompleto', tone: 'warning' },
  invalid: { label: 'Con errores', tone: 'danger' },
  inactive: { label: 'Inactivo', tone: 'neutral' },
  Live: { label: 'Envío habilitado', tone: 'warning' },
  DryRun: { label: 'Simulación sin envío', tone: 'info' },
  Disabled: { label: 'Deshabilitado', tone: 'neutral' }
};

export function presentIntegrationStatus(status: string | null | undefined): IntegrationStatusPresentation {
  if (!status) {
    return { label: 'Sin información', tone: 'neutral' };
  }

  return labels[status] ?? { label: status, tone: 'neutral' };
}
