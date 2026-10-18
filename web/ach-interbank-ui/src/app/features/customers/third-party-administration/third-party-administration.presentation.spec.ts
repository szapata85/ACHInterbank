import {
  maskForDisplay,
  validationStatusLabel
} from './third-party-administration.presentation';
import { ThirdPartyAccountValidationStatus } from './third-party-administration.models';

describe('third-party administration presentation', () => {
  it('presenta estados interbancarios sin confundir activo administrativo con elegibilidad', () => {
    expect(validationStatusLabel(ThirdPartyAccountValidationStatus.PendingResponse)).toBe('Pendiente');
    expect(validationStatusLabel(ThirdPartyAccountValidationStatus.Accepted)).toBe('Aceptada / certificada');
    expect(validationStatusLabel(ThirdPartyAccountValidationStatus.TacitlyAccepted)).toBe('Aceptación tácita');
    expect(validationStatusLabel(ThirdPartyAccountValidationStatus.Rejected)).toBe('Rechazada');
    expect(validationStatusLabel(ThirdPartyAccountValidationStatus.RequiresCorrection)).toBe('Requiere corrección / revalidación');
    expect(validationStatusLabel(ThirdPartyAccountValidationStatus.TechnicalReview)).toBe('Revisión técnica');
  });

  it('enmascara valores sensibles y nunca devuelve el número completo', () => {
    expect(maskForDisplay('1234567890123456')).toBe('••••••••3456');
    expect(maskForDisplay('1234')).toBe('••••');
    expect(maskForDisplay('')).toBe('—');
  });
});
