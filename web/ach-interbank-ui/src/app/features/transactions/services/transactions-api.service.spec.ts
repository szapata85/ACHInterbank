import { TestBed } from '@angular/core/testing';
import { TransactionsApiService } from './transactions-api.service';
import { ApiService } from '../../../core/services/api.service';
import { of } from 'rxjs';

describe('TransactionsApiService', () => {
  let service: TransactionsApiService;
  let apiSpy: jasmine.SpyObj<ApiService>;

  beforeEach(() => {
    apiSpy = jasmine.createSpyObj<ApiService>('ApiService', ['get', 'post']);
    apiSpy.post.and.returnValue(of({ id: 1 } as any));

    TestBed.configureTestingModule({
      providers: [
        TransactionsApiService,
        { provide: ApiService, useValue: apiSpy }
      ]
    });

    service = TestBed.inject(TransactionsApiService);
  });

  it('TransactionsApiService_ShouldPostCreateTransactionToExpectedEndpoint', () => {
    service.createTransaction({
      amount: 100,
      transactionExternalId: 'TX-1',
      type: 0 as any,
      accountType: 0 as any,
      isPrenotification: false,
      destinationInstitutionId: 2,
      sourceAccountNumber: '123456',
      destinationAccountNumber: '654321',
      companyName: 'EMPRESA',
      companyIdentification: 'ABCD',
      sourcePersonType: 'PJ',
      recipientPersonType: 'PN',
      companyEntryDescriptionId: 1,
      addendas: [{ addendaType: '05', information: 'INFO' }]
    } as any).subscribe();

    expect(apiSpy.post).toHaveBeenCalled();
    expect(apiSpy.post.calls.mostRecent().args[0]).toBe('transactions');
  });
});
