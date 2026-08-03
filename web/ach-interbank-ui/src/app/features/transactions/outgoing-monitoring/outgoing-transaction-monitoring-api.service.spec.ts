import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { OutgoingTransactionMonitoringApiService } from './outgoing-transaction-monitoring-api.service';

describe('OutgoingTransactionMonitoringApiService', () => {
  let service: OutgoingTransactionMonitoringApiService;
  let api: jasmine.SpyObj<ApiService>;

  beforeEach(() => {
    api = jasmine.createSpyObj<ApiService>('ApiService', ['get']);
    TestBed.configureTestingModule({ providers: [{ provide: ApiService, useValue: api }] });
    service = TestBed.inject(OutgoingTransactionMonitoringApiService);
  });

  it('envía paginación y filtros al servidor sin valores vacíos', () => {
    api.get.and.returnValue(of({ items: [], pageNumber: 1, pageSize: 25, totalItems: 0, totalPages: 0, hasPreviousPage: false, hasNextPage: false }));

    service.search({
      pageNumber: 1, pageSize: 25, sortBy: 'createdAt', sortDirection: 'desc',
      transactionExternalId: 'TX-001', cycleId: ''
    }).subscribe();

    expect(api.get).toHaveBeenCalledWith('api/transactions/outgoing-monitoring', {
      params: jasmine.objectContaining({ pageNumber: 1, pageSize: 25, transactionExternalId: 'TX-001' }),
      headers: { 'X-Skip-Loading': 'true' }
    });
    const options = api.get.calls.mostRecent().args[1] as { params: Record<string, unknown> };
    expect(options.params['cycleId']).toBeUndefined();
  });

  it('consulta el detalle por su identificador interno', () => {
    api.get.and.returnValue(of({}));
    service.getDetail(42).subscribe();
    expect(api.get).toHaveBeenCalledWith('api/transactions/outgoing-monitoring/42', {
      headers: { 'X-Skip-Loading': 'true' }
    });
  });
});
