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

  it('envía el código persistido al aprobar y conserva la representación funcional', () => {
    service.updateStatus(4, { status: 'Active' }).subscribe(row => {
      expect(row.status).toBe('Active');
    });

    const request = http.expectOne(candidate => candidate.url.endsWith('/api/customer-third-parties/4/status'));
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body.status).toBe(1);
    request.flush({
      id: 4,
      customerId: 2,
      customerName: 'Cliente sintético',
      destinationInstitutionName: 'Entidad UAT',
      destinationAccountNumber: '000000',
      recipientIdNumber: 'SINTETICO',
      status: 1
    });
  });

  it('filtra por código persistido y traduce el estado recibido para la UI', () => {
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
