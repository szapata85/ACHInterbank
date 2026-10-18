import { NachaSecurityOperationStatus } from '../models/nacha-security-operation.model';
import {
  nachaOperationErrorMessage,
  nachaOperationStatusLabel
} from './nacha-operation-presentation';

describe('NACHA operation presentation', () => {
  it('humaniza todos los estados visibles de una operación', () => {
    const expected: Record<NachaSecurityOperationStatus, string> = {
      Pending: 'Pendiente',
      Running: 'En proceso',
      Success: 'Completado',
      Failed: 'Fallido',
      Rejected: 'Rechazado',
      Expired: 'Vencido'
    };

    for (const [status, label] of Object.entries(expected)) {
      expect(nachaOperationStatusLabel(status as NachaSecurityOperationStatus)).toBe(label);
    }
  });

  it('no presenta el mensaje técnico del backend como error principal', () => {
    const technicalMessage = 'System.Security.Cryptography.CryptographicException: bad signature';
    const presented = nachaOperationErrorMessage({
      code: 'SIGNATURE_VALIDATION_FAILED',
      message: technicalMessage,
      retryable: false
    });

    expect(presented).toContain('firma digital');
    expect(presented).not.toContain(technicalMessage);
  });
});
