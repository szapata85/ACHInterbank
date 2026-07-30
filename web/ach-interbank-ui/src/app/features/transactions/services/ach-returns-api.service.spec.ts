import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AchReturnsApiService } from './ach-returns-api.service';
import { ApiService } from '../../../core/services/api.service';
import { ReturnEligibleTransaction } from '../transactions.models';

describe('AchReturnsApiService', () => {
  let service: AchReturnsApiService;
  let apiSpy: jasmine.SpyObj<ApiService>;

  beforeEach(() => {
    apiSpy = jasmine.createSpyObj<ApiService>('ApiService', ['get', 'post', 'postBlob']);
    TestBed.configureTestingModule({ providers: [{ provide: ApiService, useValue: apiSpy }] });
    service = TestBed.inject(AchReturnsApiService);
  });

  it('getTransactionsByCycle conserva el contrato y la precisión recibida', () => {
    const rows: ReturnEligibleTransaction[] = [{
      id: 10,
      traceNumber: 'TRC-10',
      amount: 1234.56,
      transactionCode: '27',
      reference: 'REF-10',
      sourceAccountNumber: '****7890',
      destinationAccountNumber: '****3210',
      originatingDfi: '11122233',
      receivingDfi: '44455566',
      achCycleId: 'cycle/1',
      effectiveEntryDate: '2026-05-24',
      isPrenotification: false,
      isEligible: true
    }];
    apiSpy.get.and.returnValue(of(rows));
    let response: ReturnEligibleTransaction[] = [];

    service.getTransactionsByCycle('cycle/1').subscribe((items) => response = items);

    expect(apiSpy.get).toHaveBeenCalledWith('ach-returns/cycles/cycle%2F1/transactions');
    expect(response).toBe(rows);
    expect(response[0].amount).toBe(1234.56);
  });

  it('evaluateReturnOfReturn llama endpoint correcto', () => {
    apiSpy.post.and.returnValue({ subscribe() {} } as any);
    service.evaluateReturnOfReturn({ sourceReturnTransactionId: 10, newReturnReasonCode: 'R02' });
    expect(apiSpy.post).toHaveBeenCalledWith('ach-returns/return-of-return/evaluate', jasmine.any(Object));
  });

  it('generateReturnOfReturnAuditFile llama endpoint blob correcto', () => {
    apiSpy.postBlob.and.returnValue({ subscribe() {} } as any);
    service.generateReturnOfReturnAuditFile({ flowIds: [10] });
    expect(apiSpy.postBlob).toHaveBeenCalledWith('ach-returns/return-of-return/generate-audit-file', jasmine.any(Object));
  });

  it('generateReturnOfReturnNachaFile llama endpoint blob correcto', () => {
    apiSpy.postBlob.and.returnValue({ subscribe() {} } as any);
    service.generateReturnOfReturnNachaFile({ flowIds: [10] });
    expect(apiSpy.postBlob).toHaveBeenCalledWith('ach-returns/return-of-return/generate-nacha-file', jasmine.any(Object));
  });
});
