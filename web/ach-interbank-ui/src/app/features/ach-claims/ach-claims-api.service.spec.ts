import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { AchClaimsApiService } from './ach-claims-api.service';
import { ClaimCaseStatus, ClaimCauseCode } from './ach-claims.models';

describe('AchClaimsApiService', () => {
  let service: AchClaimsApiService;
  let api: jasmine.SpyObj<ApiService>;

  beforeEach(() => {
    api = jasmine.createSpyObj<ApiService>('ApiService', ['get', 'post']);
    TestBed.configureTestingModule({ providers: [{ provide: ApiService, useValue: api }] });
    service = TestBed.inject(AchClaimsApiService);
  });

  it('consulta con filtros y paginación del servidor sin enviar valores vacíos', () => {
    api.get.and.returnValue(of({ items: [], pageNumber: 2, pageSize: 25, totalItems: 0 }));

    service.query({
      status: ClaimCaseStatus.Open,
      causeCode: ClaimCauseCode.Dev14,
      transactionId: 77,
      participantId: undefined,
      pageNumber: 2,
      pageSize: 25
    }).subscribe();

    expect(api.get).toHaveBeenCalledWith('api/ach-claims', {
      params: jasmine.objectContaining({ status: 1, causeCode: 1, transactionId: 77, pageNumber: 2, pageSize: 25 }),
      headers: { 'X-Skip-Loading': 'true' }
    });
    const options = api.get.calls.mostRecent().args[1] as { params: Record<string, unknown> };
    expect(options.params['participantId']).toBeUndefined();
  });

  it('consulta el detalle por número de reclamación', () => {
    api.get.and.returnValue(of({}));
    service.getById(42).subscribe();
    expect(api.get).toHaveBeenCalledWith('api/ach-claims/42', { headers: { 'X-Skip-Loading': 'true' } });
  });

  it('consulta reclamaciones relacionadas con una transacción', () => {
    api.get.and.returnValue(of([]));
    service.getByTransaction(90).subscribe();
    expect(api.get).toHaveBeenCalledWith('api/ach-claims/by-transaction/90', { headers: { 'X-Skip-Loading': 'true' } });
  });

  it('envía Idempotency-Key en creación y mutaciones', () => {
    api.post.and.returnValue(of({}));
    service.createDev14({ transactionIds: [1, 2] }, 'key-create').subscribe();
    service.recordProgress(5, { observation: 'Avance' }, 'key-progress').subscribe();
    service.resolve(5, { resolutionNotes: 'Resultado' }, 'key-resolve').subscribe();
    service.expire(5, {}, 'key-expire').subscribe();

    expect(api.post.calls.argsFor(0)[2]).toEqual({ headers: { 'Idempotency-Key': 'key-create' } });
    expect(api.post.calls.argsFor(1)[2]).toEqual({ headers: { 'Idempotency-Key': 'key-progress' } });
    expect(api.post.calls.argsFor(2)[2]).toEqual({ headers: { 'Idempotency-Key': 'key-resolve' } });
    expect(api.post.calls.argsFor(3)[2]).toEqual({ headers: { 'Idempotency-Key': 'key-expire' } });
  });
});
