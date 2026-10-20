import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { NachaExportApiService } from './nacha-export-api.service';

describe('NachaExportApiService', () => {
  let service: NachaExportApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(NachaExportApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('solicita solamente el endpoint plano para la descarga NACHA-M', () => {
    service.downloadFile('cycle/42', false).subscribe();

    const request = http.expectOne(candidate => candidate.url.endsWith('/NachaExport/cycle%2F42'));
    expect(request.request.method).toBe('GET');
    expect(request.request.responseType).toBe('blob');
    expect(request.request.url).not.toContain('sobre-digital');
    request.flush(new Blob(['nacha']));
  });

  it('solicita solamente el endpoint de sobre digital para la descarga ENV', () => {
    service.downloadFile('cycle-42', true).subscribe();

    const request = http.expectOne(candidate =>
      candidate.urlWithParams.endsWith('/NachaExport/cycle-42/sobre-digital?forceEncryption=true'));
    expect(request.request.method).toBe('GET');
    expect(request.request.responseType).toBe('blob');
    http.expectNone(candidate => candidate.url.endsWith('/NachaExport/cycle-42'));
    request.flush(new Blob(['envelope']));
  });
});
