import { TestBed } from '@angular/core/testing';
import { HttpParams } from '@angular/common/http';
import { firstValueFrom, of } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { IncomingNachaCommandCenterService } from './incoming-nacha-command-center.service';

describe('IncomingNachaCommandCenterService', () => {
  let service: IncomingNachaCommandCenterService;
  let api: jasmine.SpyObj<ApiService>;

  beforeEach(() => {
    api = jasmine.createSpyObj<ApiService>('ApiService', ['get']);
    api.get.and.returnValue(of({ items: [], page: 2, pageSize: 20, totalItems: 0 }));
    TestBed.configureTestingModule({ providers: [{ provide: ApiService, useValue: api }] });
    service = TestBed.inject(IncomingNachaCommandCenterService);
  });

  it('construye parámetros de filtros, paginación y ordenamiento sin valores vacíos', async () => {
    await firstValueFrom(service.getFiles({ page: 2, pageSize: 20, fileName: 'archivo.OUT', resultCode: 'R16', sortBy: 'fileName', sortDescending: false }));
    const params = api.get.calls.mostRecent().args[1]?.params as HttpParams;
    expect(api.get.calls.mostRecent().args[0]).toBe('incoming-nacha-command-center/ingestions');
    expect(params.get('page')).toBe('2');
    expect(params.get('resultCode')).toBe('R16');
    expect(params.has('clearingHouseId')).toBeFalse();
  });

  it('consume las rutas reales de validaciones, lotes, transacciones y addendas', () => {
    service.getValidations('id').subscribe();
    service.getBatches('id', 1, 10, 'batchNumber', false).subscribe();
    service.getTransactions('id', { page: 1, pageSize: 10, sortBy: 'traceNumber', sortDescending: false }).subscribe();
    service.getAddendas('id', 9).subscribe();
    const paths = api.get.calls.allArgs().map((args) => args[0]);
    expect(paths).toContain('incoming-nacha-command-center/ingestions/id/validations');
    expect(paths).toContain('incoming-nacha-command-center/ingestions/id/batches');
    expect(paths).toContain('incoming-nacha-command-center/ingestions/id/transactions');
    expect(paths).toContain('incoming-nacha-command-center/ingestions/id/transactions/9/addendas');
  });
});
