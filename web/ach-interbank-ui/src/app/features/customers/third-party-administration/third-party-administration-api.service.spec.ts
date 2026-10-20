import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ThirdPartyAdministrationApiService } from './third-party-administration-api.service';
import {
  ThirdPartyAccountType,
  ThirdPartyAdministrativeStatus,
  ThirdPartyTransactionNature
} from './third-party-administration.models';

describe('ThirdPartyAdministrationApiService', () => {
  let service: ThirdPartyAdministrationApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(ThirdPartyAdministrationApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('usa exclusivamente la superficie administrativa TPA5 para lista/detalle y master sin cámara', () => {
    service.listThirdParties(7, { search: 'ACME', status: ThirdPartyAdministrativeStatus.Active }).subscribe();
    const list = http.expectOne((request) => request.url.endsWith('/api/customers/7/third-parties'));
    expect(list.request.method).toBe('GET');
    expect(list.request.params.get('search')).toBe('ACME');
    list.flush({ items: [], total: 0, page: 1, pageSize: 20 });

    service.createThirdParty(7, { documentType: 'NIT', documentNumber: '900123', displayName: 'ACME', personType: 'PJ' }).subscribe();
    const create = http.expectOne((request) => request.url.endsWith('/api/customers/7/third-parties'));
    expect(create.request.method).toBe('POST');
    expect(create.request.body.clearingHouseId).toBeUndefined();
    expect(create.request.body.transactionNature).toBeUndefined();
    create.flush({ thirdPartyId: 11, customerId: 7 });
  });

  it('expone create/update/status de tercero y cuenta sin mutar identidad bancaria', () => {
    service.updateThirdParty(7, 11, { displayName: 'Nuevo nombre' }).subscribe();
    const updateParty = http.expectOne((request) => request.url.endsWith('/api/customers/7/third-parties/11'));
    expect(updateParty.request.method).toBe('PUT');
    expect(updateParty.request.body).toEqual({ displayName: 'Nuevo nombre' });
    updateParty.flush({});

    service.setThirdPartyStatus(7, 11, { status: ThirdPartyAdministrativeStatus.Inactive }).subscribe();
    const partyStatus = http.expectOne((request) => request.url.endsWith('/api/customers/7/third-parties/11/status'));
    expect(partyStatus.request.method).toBe('PUT');
    partyStatus.flush({});

    service.createAccount(7, 11, { financialInstitutionId: 3, accountType: ThirdPartyAccountType.Savings, accountNumber: '1234567890' }).subscribe();
    const createAccount = http.expectOne((request) => request.url.endsWith('/api/customers/7/third-parties/11/accounts'));
    expect(createAccount.request.method).toBe('POST');
    expect(createAccount.request.body.clearingHouseId).toBeUndefined();
    createAccount.flush({ thirdPartyAccountId: 22 });

    service.updateAccount(7, 11, 22, { status: ThirdPartyAdministrativeStatus.Draft }).subscribe();
    const updateAccount = http.expectOne((request) => request.url.endsWith('/api/customers/7/third-parties/11/accounts/22'));
    expect(updateAccount.request.body.financialInstitutionId).toBeUndefined();
    expect(updateAccount.request.body.accountNumber).toBeUndefined();
    updateAccount.flush({});

    service.setAccountStatus(7, 11, 22, { status: ThirdPartyAdministrativeStatus.Active }).subscribe();
    const accountStatus = http.expectOne((request) => request.url.endsWith('/api/customers/7/third-parties/11/accounts/22/status'));
    expect(accountStatus.request.method).toBe('PUT');
    accountStatus.flush({});
  });

  it('consulta historial y solicita revalidación contextual sin endpoint de aprobación/rechazo', () => {
    service.getValidationHistory(7, 11, 22).subscribe();
    const history = http.expectOne((request) => request.url.endsWith('/api/customers/7/third-parties/11/accounts/22/validation-history'));
    expect(history.request.method).toBe('GET');
    history.flush([]);

    service.requestRevalidation(7, 11, 22, {
      clearingHouseId: 2,
      transactionNature: ThirdPartyTransactionNature.Debit,
      effectiveDate: '2026-10-12',
      serviceType: 'PPD',
      companyEntryDescriptionId: 9,
      sourceAccountNumber: '5555666677778888',
      idempotencyKey: 'req-1'
    }).subscribe();
    const revalidation = http.expectOne((request) => request.url.endsWith('/api/customers/7/third-parties/11/accounts/22/revalidations'));
    expect(revalidation.request.method).toBe('POST');
    expect(revalidation.request.body.clearingHouseId).toBe(2);
    expect(revalidation.request.body.transactionNature).toBe(ThirdPartyTransactionNature.Debit);
    revalidation.flush({ attemptId: 44 });

    expect((service as any).approvePrenotification).toBeUndefined();
    expect((service as any).rejectPrenotification).toBeUndefined();
  });
});
