import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { AchReconciliationService } from './ach-reconciliation.service';

describe('AchReconciliationService', () => {
  let api: jasmine.SpyObj<ApiService>;
  let service: AchReconciliationService;

  beforeEach(() => {
    api = jasmine.createSpyObj<ApiService>('ApiService', ['get']);
    TestBed.configureTestingModule({
      providers: [
        AchReconciliationService,
        { provide: ApiService, useValue: api }
      ]
    });
    service = TestBed.inject(AchReconciliationService);
  });

  it('ReconciliationService_ShouldCallGetOnlyEndpoints', () => {
    api.get.and.returnValue(of({}));

    service.getDashboard().subscribe();
    service.getItems().subscribe();
    service.getItem('resp-1').subscribe();
    service.getItemByCorrelation('corr-1').subscribe();

    const urls = api.get.calls.allArgs().map(args => args[0]);
    expect(urls).toEqual([
      'api/ach/reconciliation/dashboard',
      'api/ach/reconciliation/items',
      'api/ach/reconciliation/items/resp-1',
      'api/ach/reconciliation/items/by-correlation/corr-1'
    ]);
  });
});
