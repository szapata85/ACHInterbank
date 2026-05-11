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

  it('injects ach-responses routes into default menu', (done) => {
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
      const achResponses = menu.find((x) => x.route === '/ach-responses');

      expect(achResponses).toBeTruthy();
      expect(achResponses?.children?.find((x) => x.route === '/ach-responses')).toBeTruthy();
      expect(achResponses?.children?.find((x) => x.route === '/ach-responses/manual-review')).toBeTruthy();
      expect(achResponses?.children?.find((x) => x.route === '/ach-responses/status-mappings')).toBeTruthy();
      expect(achResponses?.children?.find((x) => x.route === '/ach-responses/dashboard')).toBeTruthy();
      done();
    });
  });
});
