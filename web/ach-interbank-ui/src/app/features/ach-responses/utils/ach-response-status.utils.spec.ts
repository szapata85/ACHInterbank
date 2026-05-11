import { formatAchNotificationStatus, formatAchProcessingStatus, getAchManualReviewPriority, getAchNotificationStatusClass, getAchPriorityClass, getAchProcessingStatusClass } from './ach-response-status.utils';

describe('ach-response-status.utils', () => {
  it('formats statuses', () => {
    expect(formatAchProcessingStatus('')).toBe('-');
    expect(formatAchNotificationStatus('Exitosa')).toBe('Exitosa');
  });

  it('maps classes and priorities', () => {
    expect(getAchProcessingStatusClass('Notificada')).toBe('estado-exitoso');
    expect(getAchNotificationStatusClass('ErrorTecnico')).toBe('estado-error');
    expect(getAchManualReviewPriority('ErrorFuncional')).toBe('Alta');
    expect(getAchPriorityClass('Media')).toBe('prioridad-media');
  });
});
