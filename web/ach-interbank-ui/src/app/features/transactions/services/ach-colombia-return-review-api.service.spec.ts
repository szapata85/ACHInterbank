import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { AchColombiaReturnReviewApiService } from './ach-colombia-return-review-api.service';

describe('AchColombiaReturnReviewApiService', () => {
  let api: jasmine.SpyObj<ApiService>;
  let service: AchColombiaReturnReviewApiService;

  beforeEach(() => {
    api = jasmine.createSpyObj<ApiService>('ApiService', ['get', 'post']);
    TestBed.configureTestingModule({ providers: [{ provide: ApiService, useValue: api }] });
    service = TestBed.inject(AchColombiaReturnReviewApiService);
  });

  it('consulta solo candidatas ACH Colombia con límite de servidor', () => {
    api.get.and.returnValue(of([]));
    service.list().subscribe((rows) => expect(rows).toEqual([]));
    expect(api.get).toHaveBeenCalledWith('api/ach-colombia/devoluciones-automaticas/candidatas?limit=50');
  });

  it('genera una sola candidata aprobada con respuesta y nombre de archivo', () => {
    const output = { fileName: '00001234.RET', contentType: 'text/plain', content: 'QUJD', totalRecords: 1, totalReturns: 1 };
    api.post.and.returnValue(of(output));
    service.generateApprovedFile(7).subscribe(file => expect(file.fileName).toBe('00001234.RET'));
    expect(api.post).toHaveBeenCalledWith(
      'api/ach-colombia/devoluciones-automaticas/candidatas/7/generar-archivo', {}
    );
  });

  it('recupera el archivo existente sin generar nuevamente', () => {
    const output = { fileName: '00001234.RET', contentType: 'text/plain', content: 'QUJD', totalRecords: 1, totalReturns: 1 };
    api.get.and.returnValue(of(output));
    service.recoverGeneratedFile(7).subscribe(file => expect(file.fileName).toBe('00001234.RET'));
    expect(api.get).toHaveBeenCalledWith(
      'api/ach-colombia/devoluciones-automaticas/candidatas/7/archivo'
    );
  });

  it('envía únicamente la decisión y el identificador de candidata', () => {
    api.post.and.returnValue(of({ id: 7, status: 'APROBADA_SIN_ENVIO' }));
    service.decide(7, 'APROBAR').subscribe((result) => expect(result.status).toBe('APROBADA_SIN_ENVIO'));
    expect(api.post).toHaveBeenCalledWith(
      'api/ach-colombia/devoluciones-automaticas/candidatas/7/decision', { action: 'APROBAR' }
    );
  });
});
