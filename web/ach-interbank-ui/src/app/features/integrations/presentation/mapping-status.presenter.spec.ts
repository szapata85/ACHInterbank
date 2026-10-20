import { presentIntegrationStatus } from './mapping-status.presenter';

describe('presentIntegrationStatus', () => {
  it('traduce estados backend a lenguaje funcional y conserva estados desconocidos', () => {
    expect(presentIntegrationStatus('Published')).toEqual({ label: 'Publicada', tone: 'success' });
    expect(presentIntegrationStatus('Complete')).toEqual({ label: 'Completo', tone: 'success' });
    expect(presentIntegrationStatus('Pending')).toEqual({ label: 'Pendiente', tone: 'warning' });
    expect(presentIntegrationStatus('Invalid')).toEqual({ label: 'Inválido', tone: 'danger' });
    expect(presentIntegrationStatus('NotApplicable')).toEqual({ label: 'No aplica', tone: 'neutral' });
    expect(presentIntegrationStatus('DryRun')).toEqual({ label: 'Simulación sin envío', tone: 'info' });
    expect(presentIntegrationStatus('FutureBackendState')).toEqual({ label: 'FutureBackendState', tone: 'neutral' });
    expect(presentIntegrationStatus(null)).toEqual({ label: 'Sin información', tone: 'neutral' });
  });
});
