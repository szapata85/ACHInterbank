import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TransactionTypeEnum } from '../transactions.types';
import { ClearingHouseTransactionRulesApiService } from './clearing-house-transaction-rules-api.service';

describe('ClearingHouseTransactionRulesApiService', () => {
  let service: ClearingHouseTransactionRulesApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });

    service = TestBed.inject(ClearingHouseTransactionRulesApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lists rules with filters', () => {
    service.getRules({ clearingHouseId: 1, transactionNature: 'Debit', includeInactive: true }).subscribe((items) => {
      expect(items.length).toBe(1);
    });

    const req = httpMock.expectOne((request) =>
      request.url.endsWith('/api/clearing-house-transaction-rules')
      && request.params.get('clearingHouseId') === '1'
      && request.params.get('transactionNature') === 'Debit'
      && request.params.get('includeInactive') === 'true');

    req.flush([{ id: 10 }]);
  });

  it('sends preview request', () => {
    service.preview({
      clearingHouseId: 1,
      transactionType: TransactionTypeEnum.Debit,
      effectiveEntryDate: '2026-01-01',
      appliesToNachaExport: true
    }).subscribe((response) => {
      expect(response.decision).toBe('PRENOTIFICATION_REQUIRED');
    });

    const req = httpMock.expectOne((request) => request.url.endsWith('/api/transaction-prerequisite-policy/preview'));
    expect(req.request.method).toBe('POST');
    req.flush({ decision: 'PRENOTIFICATION_REQUIRED' });
  });
});
