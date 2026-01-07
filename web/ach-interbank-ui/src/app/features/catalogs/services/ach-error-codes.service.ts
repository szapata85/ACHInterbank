import { Injectable } from '@angular/core';
import { AchErrorCode } from '../models/ach-error-code.model';

const ERROR_CODES: AchErrorCode[] = [
  {
    returnCode: 'R01',
    category: 'Receiving Entity',
    standardDescription: 'Insufficient Funds',
    additionalDetail: 'La cuenta no tiene fondos suficientes para cubrir la transacción.',
    applicability: 'Monetary'
  },
  {
    returnCode: 'R17',
    category: 'Receiving Entity',
    standardDescription: 'ID mismatch with Account',
    additionalDetail: 'El identificador del titular no coincide con la cuenta registrada.',
    applicability: 'Both'
  },
  {
    returnCode: 'D01',
    category: 'Operator',
    standardDescription: 'Effective date less than process date',
    additionalDetail: 'La fecha efectiva es anterior a la fecha de proceso.',
    applicability: 'Monetary'
  },
  {
    returnCode: 'D12',
    category: 'Operator',
    standardDescription: 'Invalid Addenda Type Code',
    additionalDetail: 'El código de addenda no corresponde al formato esperado.',
    applicability: 'Both'
  }
];

@Injectable({ providedIn: 'root' })
export class AchErrorCodesService {
  getAll(): AchErrorCode[] {
    return ERROR_CODES;
  }
}
