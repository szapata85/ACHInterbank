import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CustomerThirdPartiesService } from './customer-third-parties.service';

describe('CustomerThirdPartiesService', () => {
  let service: CustomerThirdPartiesService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(CustomerThirdPartiesService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('consulta solamente mediante GET y traduce el estado persistido para la UI', () => {
    service.search({ status: 'Rejected', page: 1, pageSize: 20 }).subscribe(response => {
      expect(response.items[0].status).toBe('Rejected');
    });

    const request = http.expectOne(candidate =>
      candidate.url.endsWith('/api/customer-third-parties')
      && candidate.params.get('status') === '2');
    expect(request.request.method).toBe('GET');
    request.flush({
      items: [{
        id: 5,
        customerId: 2,
        customerName: 'Cliente sintético',
        destinationInstitutionName: 'Entidad UAT',
        destinationAccountNumber: '000000',
        recipientIdNumber: 'SINTETICO',
        status: 2
      }],
      total: 1,
      page: 1,
      pageSize: 20
    });
  });
});
