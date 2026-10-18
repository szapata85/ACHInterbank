import { presentIntegrationValidationIssue } from './mapping-validation.presenter';

describe('mapping validation presenter P6.3', () => {
  it('humaniza códigos backend sin perder el código ni mensaje técnico', () => {
    const result = presentIntegrationValidationIssue({
      severity: 'Error',
      code: 'REQUIRED_PARAMETER_MISSING',
      message: 'Falta regla para parámetro obligatorio OFIDTX.',
      path: 'OFIDTX',
      category: 'Structural'
    }, 'Número de referencia');

    expect(result.functionalMessage).toContain('Número de referencia');
    expect(result.functionalMessage).toContain('origen configurado');
    expect(result.code).toBe('REQUIRED_PARAMETER_MISSING');
    expect(result.technicalMessage).toBe('Falta regla para parámetro obligatorio OFIDTX.');
  });

  it('usa un mensaje funcional seguro para códigos desconocidos y conserva diagnóstico', () => {
    const result = presentIntegrationValidationIssue({
      severity: 'Warning',
      code: 'BACKEND_NEW_CODE',
      message: 'Detalle backend controlado.',
      path: 'Dato',
      category: 'Functional'
    });

    expect(result.functionalMessage).toBe('Revisa esta configuración antes de continuar.');
    expect(result.code).toBe('BACKEND_NEW_CODE');
    expect(result.technicalMessage).toBe('Detalle backend controlado.');
  });
});
