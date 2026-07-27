import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CustomersApiService } from './customers-api.service';

describe('CustomersApiService', () => {
  let service: CustomersApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(CustomersApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('consulta la ruta canónica /customers sin prefijo alternativo', () => {
    service.getAll().subscribe(customers => expect(customers).toEqual([]));

    const request = http.expectOne(candidate => candidate.url.endsWith('/customers'));
    expect(request.request.method).toBe('GET');
    expect(request.request.url).not.toContain('/api/customers');
    request.flush([]);
  });
});
