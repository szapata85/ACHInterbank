import {
  supportActionLabel,
  supportActionsLabel,
  supportOutcomeLabel,
  supportStatusLabel
} from './incoming-nacha-support-presentation';

describe('presentación del soporte NACHA-M', () => {
  it('humaniza los estados técnicos conocidos', () => {
    expect(supportStatusLabel('WaitingWindow')).toBe('Esperando ventana operativa');
    expect(supportStatusLabel('RetryPending')).toBe('Pendiente de reintento');
    expect(supportStatusLabel('FailedFinal')).toBe('Error definitivo');
  });

  it('humaniza las acciones autorizadas sin exponer sus claves internas', () => {
    expect(supportActionLabel('retry')).toBe('Reintentar procesamiento');
    expect(supportActionsLabel(['unblock', 'requeue']))
      .toBe('Desbloquear procesamiento, Reprogramar procesamiento');
    expect(supportActionsLabel([])).toBe('Ninguna disponible');
  });

  it('diferencia una solicitud repetida de una acción aplicada', () => {
    expect(supportOutcomeLabel(true, 'Blocked', 'Blocked')).toBe('Solicitud ya atendida');
    expect(supportOutcomeLabel(false, 'Blocked', 'Scheduled')).toBe('Acción aplicada');
    expect(supportOutcomeLabel(false, 'Blocked', 'Blocked')).toBe('Acción no aplicada');
  });
});
