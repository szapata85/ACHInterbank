import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { TransactionTypeEnum } from '../../core/models/transaction-types.model';
import { OperationalRestrictionsService } from './operational-restrictions.service';

describe('OperationalRestrictionsService', () => {
  let service: OperationalRestrictionsService;
  let api: jasmine.SpyObj<ApiService>;

  beforeEach(() => {
    api = jasmine.createSpyObj<ApiService>('ApiService', ['get', 'post', 'put', 'patch']);
    api.get.and.returnValue(of([]));
    api.post.and.returnValue(of({}));
    api.put.and.returnValue(of({}));
    api.patch.and.returnValue(of({}));

    TestBed.configureTestingModule({
      providers: [OperationalRestrictionsService, { provide: ApiService, useValue: api }]
    });
    service = TestBed.inject(OperationalRestrictionsService);
  });

  it('usa exclusivamente el endpoint administrativo existente y serializa filtros definidos', () => {
    service.list({
      clearingHouseCode: 'ACHCOL',
      policyVersion: 'V35',
      clearingHouseCycleConfigId: 51,
      transactionType: TransactionTypeEnum.Debit,
      isPrenotification: false,
      isActive: true,
      effectiveAt: '2026-09-30'
    }).subscribe();

    expect(api.get).toHaveBeenCalledWith('api/cfa-operational-transaction-restrictions', {
      params: {
        clearingHouseCode: 'ACHCOL',
        policyVersion: 'V35',
        clearingHouseCycleConfigId: 51,
        transactionType: TransactionTypeEnum.Debit,
        isPrenotification: false,
        isActive: true,
        effectiveAt: '2026-09-30'
      }
    });
  });

  it('conserva RowVersion en update y cambios de estado', () => {
    const update = {
      clearingHouseCode: 'ACHCOL', clearingHouseCycleConfigId: 51, transactionType: TransactionTypeEnum.Debit,
      isPrenotification: false, maxAmountPerTransaction: 1, maxAmountPerCycle: 2, maxTransactionsPerCycle: 3,
      allowedAccountTypes: [], effectiveFrom: '2026-01-01', effectiveTo: null, isActive: true,
      expectedRowVersion: 'AQID'
    };
    service.update(7, update).subscribe();
    service.deactivate(7, { expectedRowVersion: 'AQID' }).subscribe();
    service.activate(7, { expectedRowVersion: 'BAUG' }).subscribe();

    expect(api.put).toHaveBeenCalledWith('api/cfa-operational-transaction-restrictions/7', update);
    expect(api.patch).toHaveBeenCalledWith('api/cfa-operational-transaction-restrictions/7/deactivate', { expectedRowVersion: 'AQID' });
    expect(api.patch).toHaveBeenCalledWith('api/cfa-operational-transaction-restrictions/7/activate', { expectedRowVersion: 'BAUG' });
  });
});
