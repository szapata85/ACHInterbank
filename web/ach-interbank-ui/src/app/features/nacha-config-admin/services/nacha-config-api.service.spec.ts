import { HttpHeaders } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { NachaConfigApiService } from './nacha-config-api.service';
import { NACHA_CONFIG_READ_FALLBACK_DELAY_MS } from './nacha-config-read-retry';

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

  it('Service_ShouldCallConfigProfilesReadOnlyEndpoints', () => {
    service.listarPerfilesReadOnly().subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/ach/nacha/config-profiles`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('Service_ShouldCallConfigProfilesReadOnlyDetailEndpoint', () => {
    service.obtenerPerfilReadOnly(9).subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/ach/nacha/config-profiles/9`);
    expect(req.request.method).toBe('GET');
    req.flush({ profileId: 9, variants: [], fields: [] });
  });

  it('debe consultar catálogos de filtro', () => {
    service.catalogosFiltro().subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/nacha-config/catalogos-filtro`);
    expect(req.request.method).toBe('GET');
    req.flush({ estados: [], camaras: [], flujos: [], direcciones: [], servicios: [] });
  });

  it('reintenta GET 429 respetando Retry-After y entrega el resultado recuperado', fakeAsync(() => {
    const url = `${environment.apiBaseUrl}/nacha-config/catalogos-filtro`;
    const retries: number[] = [];
    let result: unknown;

    service.catalogosFiltro((event) => retries.push(event.delayMs)).subscribe((value) => (result = value));
    httpMock.expectOne(url).flush(
      {},
      {
        status: 429,
        statusText: 'Too Many Requests',
        headers: new HttpHeaders({ 'Retry-After': '2' })
      }
    );

    tick(1999);
    httpMock.expectNone(url);
    tick(1);
    httpMock.expectOne(url).flush({ estados: [], camaras: [{ code: 'JOB6TEST' }], flujos: [], direcciones: [], servicios: [] });

    expect(retries).toEqual([2000]);
    expect((result as any).camaras[0].code).toBe('JOB6TEST');
  }));

  it('usa el fallback acotado cuando GET 429 no incluye Retry-After', fakeAsync(() => {
    const url = `${environment.apiBaseUrl}/nacha-config/catalogos-filtro`;
    let completed = false;

    service.catalogosFiltro().subscribe(() => (completed = true));
    httpMock.expectOne(url).flush({}, { status: 429, statusText: 'Too Many Requests' });

    tick(NACHA_CONFIG_READ_FALLBACK_DELAY_MS - 1);
    httpMock.expectNone(url);
    tick(1);
    httpMock.expectOne(url).flush({ estados: [], camaras: [], flujos: [], direcciones: [], servicios: [] });

    expect(completed).toBeTrue();
  }));

  it('agota exactamente dos reintentos GET 429 y propaga el error final', fakeAsync(() => {
    const url = `${environment.apiBaseUrl}/nacha-config/catalogos-filtro`;
    let finalStatus = 0;

    service.catalogosFiltro().subscribe({ error: (error) => (finalStatus = error.status) });
    for (let attempt = 1; attempt <= 3; attempt += 1) {
      httpMock.expectOne(url).flush({}, { status: 429, statusText: 'Too Many Requests' });
      if (attempt < 3) {
        tick(NACHA_CONFIG_READ_FALLBACK_DELAY_MS);
      }
    }

    expect(finalStatus).toBe(429);
    httpMock.expectNone(url);
  }));

  it('no reintenta operaciones mutables POST o PUT ante 429', fakeAsync(() => {
    const postUrl = `${environment.apiBaseUrl}/nacha-config/perfiles`;
    const putUrl = `${environment.apiBaseUrl}/nacha-config/perfiles/9`;
    let errors = 0;

    service.crearBorrador({ profileCode: 'JOB6-MUTABLE' }).subscribe({ error: () => (errors += 1) });
    httpMock.expectOne(postUrl).flush({}, { status: 429, statusText: 'Too Many Requests' });

    service.editarBorrador(9, { nombreEs: 'Sin retry' }).subscribe({ error: () => (errors += 1) });
    httpMock.expectOne(putUrl).flush({}, { status: 429, statusText: 'Too Many Requests' });

    tick(NACHA_CONFIG_READ_FALLBACK_DELAY_MS * 3);
    httpMock.expectNone(postUrl);
    httpMock.expectNone(putUrl);
    expect(errors).toBe(2);
  }));

  it('debe publicar enviando expectedRowVersion', () => {
    service.publicar(9, 'abc=').subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/nacha-config/perfiles/9/publicar`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ expectedRowVersion: 'abc=' });
    req.flush({ publicado: true });
  });

  it('debe crear borrador', () => {
    service.crearBorrador({
      profileCode: 'UAT-NACHA-CONFIG-001',
      nombreEs: 'Perfil UAT',
      descripcion: 'Descripcion',
      camaraCode: 'ACH',
      flujoCode: 'ORIGINAL',
      direccionCode: 'SALIDA',
      servicioCode: 'PPD',
      effectiveFrom: '2026-01-01'
    }).subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/nacha-config/perfiles`);
    expect(req.request.method).toBe('POST');
    req.flush({ id: 99 });
  });

  it('debe editar borrador', () => {
    service.editarBorrador(9, {
      nombreEs: 'Perfil UAT',
      descripcion: 'Descripcion',
      contextPriority: 150,
      effectiveFrom: '2026-01-01',
      effectiveTo: null,
      expectedRowVersion: 'abc='
    }).subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/nacha-config/perfiles/9`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({
      nombreEs: 'Perfil UAT',
      descripcion: 'Descripcion',
      contextPriority: 150,
      effectiveFrom: '2026-01-01',
      effectiveTo: null,
      expectedRowVersion: 'abc='
    });
    req.flush({ id: 9 });
  });

  it('debe actualizar secuencia de records', () => {
    service.actualizarSecuencia(9, {
      expectedRowVersion: 'abc=',
      records: [
        { profileRecordId: 101, sequence: 10 },
        { profileRecordId: 102, sequence: 20 }
      ]
    }).subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/nacha-config/perfiles/9/records/secuencia`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({
      expectedRowVersion: 'abc=',
      records: [
        { profileRecordId: 101, sequence: 10 },
        { profileRecordId: 102, sequence: 20 }
      ]
    });
    req.flush({});
  });

  it('debe actualizar variant', () => {
    service.actualizarVariante(9, 77, {
      nombreEs: 'Variant editable',
      descripcion: 'Descripcion',
      priority: 2,
      isDefaultForRecord: true,
      effectiveFrom: '2026-01-01',
      effectiveTo: null,
      expectedRowVersion: 'row-v'
    }).subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/nacha-config/perfiles/9/variantes/77`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({
      nombreEs: 'Variant editable',
      descripcion: 'Descripcion',
      priority: 2,
      isDefaultForRecord: true,
      effectiveFrom: '2026-01-01',
      effectiveTo: null,
      expectedRowVersion: 'row-v'
    });
    req.flush({});
  });

  it('debe actualizar field', () => {
    service.actualizarField(9, 88, {
      fieldNameEs: 'Field editable',
      startPosition: 3,
      length: 12,
      propertyPath: 'Transaction.Amount',
      isEnabled: false,
      expectedRowVersion: 'row-v2'
    }).subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/nacha-config/perfiles/9/fields/88`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({
      fieldNameEs: 'Field editable',
      startPosition: 3,
      length: 12,
      propertyPath: 'Transaction.Amount',
      isEnabled: false,
      expectedRowVersion: 'row-v2'
    });
    req.flush({});
  });

  it('debe actualizar rule', () => {
    service.actualizarRule(9, 99, {
      errorCode: 'ERR_UPDATED',
      errorMessageEs: 'Mensaje actualizado',
      severity: 'WARN',
      isEnabled: false,
      expectedRowVersion: 'row-v3'
    }).subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/nacha-config/perfiles/9/rules/99`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({
      errorCode: 'ERR_UPDATED',
      errorMessageEs: 'Mensaje actualizado',
      severity: 'WARN',
      isEnabled: false,
      expectedRowVersion: 'row-v3'
    });
    req.flush({});
  });

  it('debe clonar perfil', () => {
    service.clonarPerfil(9, {
      nuevoProfileCode: 'UAT-NACHA-CONFIG-CLONE',
      nuevoNombreEs: 'Perfil clonado',
      effectiveFrom: '2026-01-01',
      expectedRowVersion: 'abc='
    }).subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/nacha-config/perfiles/9/clonar`);
    expect(req.request.method).toBe('POST');
    req.flush({ id: 19 });
  });

  it('debe validar perfil', () => {
    service.validar(9).subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/nacha-config/perfiles/9/validar`);
    expect(req.request.method).toBe('POST');
    req.flush({ profileId: 9, isValid: true, erroresBloqueantes: 0, advertencias: 0, resumen: 'OK', issues: [] });
  });

  it('debe inactivar y archivar perfiles', () => {
    service.inactivar(9, 'abc=').subscribe();
    let req = httpMock.expectOne(`${environment.apiBaseUrl}/nacha-config/perfiles/9/inactivar`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ expectedRowVersion: 'abc=' });
    req.flush({});

    service.archivar(9, 'def=').subscribe();
    req = httpMock.expectOne(`${environment.apiBaseUrl}/nacha-config/perfiles/9/archivar`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ expectedRowVersion: 'def=' });
    req.flush({});
  });
});
