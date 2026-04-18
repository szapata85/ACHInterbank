import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { NachaConfigApiService } from './nacha-config-api.service';

describe('NachaConfigApiService', () => {
  let service: NachaConfigApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });

    service = TestBed.inject(NachaConfigApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('debe consultar perfiles', () => {
    service.listarPerfiles().subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/nacha-config/perfiles`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('debe consultar catálogos de filtro', () => {
    service.catalogosFiltro().subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/nacha-config/catalogos-filtro`);
    expect(req.request.method).toBe('GET');
    req.flush({ estados: [], camaras: [], flujos: [], direcciones: [], servicios: [] });
  });

  it('debe publicar enviando expectedRowVersion', () => {
    service.publicar(9, 'abc=').subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/nacha-config/perfiles/9/publicar`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ expectedRowVersion: 'abc=' });
    req.flush({ publicado: true });
  });
});
