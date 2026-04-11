import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiService } from './api.service';
import { NavigationService } from './navigation.service';

describe('NavigationService', () => {
  it('injects cycle-config route into default transactions menu', (done) => {
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
      const cycleConfigItem = transactions?.children?.find((x) => x.route === '/transactions/cycle-configs');

      expect(transactions).toBeTruthy();
      expect(cycleConfigItem).toBeTruthy();
      done();
    });
  });
});
