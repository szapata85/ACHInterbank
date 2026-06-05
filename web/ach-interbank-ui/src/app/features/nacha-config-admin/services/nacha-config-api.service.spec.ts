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
