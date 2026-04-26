import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { PaymentRailCapabilityRegistryApiService } from './payment-rail-capability-registry-api.service';

describe('PaymentRailCapabilityRegistryApiService', () => {
  let service: PaymentRailCapabilityRegistryApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });

    service = TestBed.inject(PaymentRailCapabilityRegistryApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('debe consultar rieles con GET', () => {
    service.getRails().subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/payment-rails/capability-registry/rails`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('debe consultar capabilities por riel con GET', () => {
    service.getCapabilitiesByRail('CENIT', '2026-04-26T00:00:00.000Z').subscribe();

    const req = httpMock.expectOne(
      `${environment.apiBaseUrl}/api/payment-rails/capability-registry/rails/CENIT/capabilities?asOfUtc=2026-04-26T00:00:00.000Z`
    );
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('debe consultar capability específica con GET', () => {
    service.getCapabilityByRail('CENIT', 'Netting').subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/payment-rails/capability-registry/rails/CENIT/capabilities/Netting`);
    expect(req.request.method).toBe('GET');
    req.flush({ railCode: 'CENIT', capabilityCode: 'Netting', state: 'ShadowOnly', source: 'StrategyDefault', evaluatedAtUtc: '2026-04-26T00:00:00Z', version: 'v1' });
  });
});
