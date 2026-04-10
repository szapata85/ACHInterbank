import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiService } from './api.service';
import { NavigationService } from './navigation.service';

describe('NavigationService', () => {
  it('should ensure bulk-create route exists in merged menu', (done) => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', ['get']);
    api.get.and.returnValue(of([]));

    TestBed.configureTestingModule({
      providers: [
        NavigationService,
        { provide: ApiService, useValue: api }
      ]
    });

    const service = TestBed.inject(NavigationService);
    service.getMenu().subscribe((menu) => {
      const transactions = menu.find((x) => x.route === '/transactions');
      const bulkRoute = transactions?.children?.some((x) => x.route === '/transactions/bulk-create');

      expect(transactions).toBeTruthy();
      expect(bulkRoute).toBeTrue();
      done();
    });
  });
});
