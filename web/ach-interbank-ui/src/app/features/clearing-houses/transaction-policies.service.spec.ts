import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { TransactionTypeEnum } from '../transactions/transactions.types';
import { TransactionPoliciesService } from './transaction-policies.service';

describe('TransactionPoliciesService', () => {
  let service: TransactionPoliciesService;
  let api: jasmine.SpyObj<ApiService>;

  beforeEach(() => {
    api = jasmine.createSpyObj<ApiService>('ApiService', ['get', 'post', 'patch']);
    TestBed.configureTestingModule({
      providers: [
        TransactionPoliciesService,
        { provide: ApiService, useValue: api }
      ]
    });
    service = TestBed.inject(TransactionPoliciesService);
  });

  it('normaliza el enum numérico entregado por la API', () => {
    api.get.and.returnValue(of([
      policyResponse({ prenotificationMode: 1 }),
      policyResponse({ id: 2, transactionType: TransactionTypeEnum.Credit, prenotificationMode: 2 })
    ]));

    service.list(1).subscribe(policies => {
      expect(policies.map(policy => policy.prenotificationMode)).toEqual(['Mandatory', 'Optional']);
    });
  });

  it('envía el modo como enum numérico al crear una versión', () => {
    api.post.and.returnValue(of(policyResponse({ prenotificationMode: 1 })));

    service.create(1, {
      transactionType: TransactionTypeEnum.Debit,
      prenotificationMode: 'Mandatory',
      prenotificationLeadBusinessDays: null,
      effectiveFrom: '2026-08-01T00:00:00.000Z',
      effectiveTo: null,
      isActive: true,
      normativeSource: 'Fuente',
      normativeReference: 'Referencia',
      notes: null
    }).subscribe();

    expect(api.post).toHaveBeenCalledWith('api/clearing-houses/1/transaction-policies', jasmine.objectContaining({
      prenotificationMode: 1
    }));
  });
});

function policyResponse(overrides: Record<string, unknown> = {}) {
  return {
    id: 1,
    clearingHouseId: 1,
    clearingHouseName: 'ACH Colombia',
    transactionType: TransactionTypeEnum.Debit,
    prenotificationMode: 1,
    prenotificationLeadBusinessDays: 3,
    effectiveFrom: '2025-01-01T00:00:00Z',
    effectiveTo: null,
    isActive: true,
    normativeSource: 'Fuente',
    normativeReference: 'Referencia',
    notes: '',
    createdAt: '2025-01-01T00:00:00Z',
    updatedAt: '2025-01-01T00:00:00Z',
    ...overrides
  };
}
