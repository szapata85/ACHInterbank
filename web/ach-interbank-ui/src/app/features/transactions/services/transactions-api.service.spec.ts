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


  it('getContextualThirdPartyAccounts_ShouldUseBackendContextWithoutLegacyActiveStatus', () => {
    apiSpy.get.and.returnValue(of({
      customerId: 11,
      destinationInstitutionId: 9102,
      clearingHouseId: 2,
      clearingHouseCode: 'CENIT',
      clearingHouseName: 'CENIT',
      transactionNature: 2,
      effectiveDate: '2026-10-12T00:00:00',
      serviceType: 'PPD',
      items: [{
        thirdPartyAccountId: 77,
        thirdPartyId: 55,
        destinationInstitutionId: 9102,
        destinationInstitutionName: 'Banco destino',
        destinationAccountNumber: '9876543210',
        accountType: 2,
        recipientIdNumber: '900123456',
        recipientName: 'Proveedor',
        recipientPersonType: 'PJ',
        eligibilityDecision: 6,
        eligibilityCode: 'VALIDATION_REJECTED',
        eligibilityMessage: 'La última validación fue rechazada.',
        isSelectable: false
      }]
    } as any));

    let received: any[] = [];
    service.getContextualThirdPartyAccounts('12345000001', 9102, 2 as any, 17)
      .subscribe((items) => received = items);

    const callArgs = apiSpy.get.calls.mostRecent().args;
    const options = callArgs[1] as any;
    expect(callArgs[0]).toBe('api/transactions/third-party-accounts/selector');
    expect(options.params.sourceAccountNumber).toBe('12345000001');
    expect(options.params.destinationInstitutionId).toBe(9102);
    expect(options.params.type).toBe(2);
    expect(options.params.companyEntryDescriptionId).toBe(17);
    expect(options.params.status).toBeUndefined();
    expect(received[0].eligibilityCode).toBe('VALIDATION_REJECTED');
    expect(received[0].isSelectable).toBeFalse();
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
    expect(apiSpy.post.calls.mostRecent().args[0]).toBe('api/transactions');
  });
});
