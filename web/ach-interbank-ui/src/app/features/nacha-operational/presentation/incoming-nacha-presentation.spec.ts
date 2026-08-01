import { abbreviatedIdentifier, logicalServiceName, operationalTone, technicalErrorMessage } from './incoming-nacha-presentation';

describe('presentación operativa NACHA-M', () => {
  it('humaniza servicios sin exponer una dirección física', () => {
    expect(logicalServiceName('Proc_Transacciones')).toBe('Procesamiento de transacciones');
    expect(logicalServiceName('RegistrarRespuestaTransaccion')).toBe('Registro de respuesta de transacción');
    expect(logicalServiceName('Proc_Transacciones')).not.toContain('http');
  });

  it('separa error técnico de rechazo funcional', () => {
    expect(technicalErrorMessage('SOAP_TIMEOUT', 'timeout')).toContain('no respondió dentro del tiempo esperado');
    expect(technicalErrorMessage('SOAP_TIMEOUT', 'timeout')).not.toContain('Rechazado');
    expect(operationalTone('Error técnico')).toBe('danger');
    expect(operationalTone('Pendiente de respuesta')).toBe('warning');
  });

  it('abrevia identificadores para soporte', () => {
    expect(abbreviatedIdentifier('1234567890ABCDEFGHIJ')).toBe('12345678…GHIJ');
  });
});
