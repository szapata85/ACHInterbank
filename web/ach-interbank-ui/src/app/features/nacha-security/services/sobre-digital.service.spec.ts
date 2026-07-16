import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { SobreDigitalService } from './sobre-digital.service';

describe('SobreDigitalService', () => {
  let service: SobreDigitalService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(SobreDigitalService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('consulta certificados reales bajo el prefijo /api', () => {
    service.listCertificates().subscribe();
    const request = http.expectOne((candidate) => candidate.url.endsWith('/api/nacha-security/digital-envelope/certificates'));
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('envia cifrado multipart y espera Blob con respuesta completa', () => {
    const file = new File([new Uint8Array([1, 2, 3])], 'archivo.OUT');
    service.encrypt(file, 41).subscribe((response) => {
      expect(response.body).toEqual(jasmine.any(Blob));
    });

    const request = http.expectOne((candidate) => candidate.url.endsWith('/api/nacha-security/digital-envelope/encrypt'));
    expect(request.request.method).toBe('POST');
    expect(request.request.responseType).toBe('blob');
    expect(request.request.body).toEqual(jasmine.any(FormData));
    expect((request.request.body as FormData).get('certificateVersionId')).toBe('41');
    expect((request.request.body as FormData).get('file')).toBe(file);
    request.flush(new Blob([new Uint8Array([9, 8, 7])]), {
      headers: { 'Content-Disposition': "attachment; filename*=UTF-8''archivo.OUT.ENV" }
    });
  });

  it('envia descifrado multipart al mismo contrato relativo', () => {
    const file = new File([new Uint8Array([9, 8, 7])], 'archivo.OUT.ENV');
    service.decrypt(file, 42).subscribe();

    const request = http.expectOne((candidate) => candidate.url.endsWith('/api/nacha-security/digital-envelope/decrypt'));
    expect(request.request.method).toBe('POST');
    expect(request.request.responseType).toBe('blob');
    expect((request.request.body as FormData).get('certificateVersionId')).toBe('42');
    expect((request.request.body as FormData).get('file')).toBe(file);
    request.flush(new Blob([new Uint8Array([1, 2, 3])]));
  });
});
