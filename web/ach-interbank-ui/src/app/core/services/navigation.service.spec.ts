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

  it('LegacyLayoutsRoute_ShouldBeRemovedFromMenu', (done) => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', ['get']);
    api.get.and.returnValue(of([
      { id: 1, label: 'Layouts NACHA', route: '/ach-cycles/nacha/layouts' },
      { id: 2, label: 'Definiciones NACHA', route: '/ach-cycles/nacha/definitions' }
    ]));

    TestBed.configureTestingModule({
      providers: [
        NavigationService,
        { provide: ApiService, useValue: api }
      ]
    });

    const service = TestBed.inject(NavigationService);

    service.getMenu().subscribe((menu) => {
      const routes = flattenRoutes(menu);

      expect(routes).not.toContain('/ach-cycles/nacha/layouts');
      expect(routes).not.toContain('/nacha-layouts');
      expect(routes).toContain('/nacha-config-admin/variants-fields');
      expect(flattenLabels(menu)).toContain('Variantes y campos');
      done();
    });
  });

  it('LegacyDefinitionsRoute_ShouldBeRemovedFromMenu', (done) => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', ['get']);
    api.get.and.returnValue(of([
      {
        id: 1,
        label: 'ACH',
        route: '/ach-cycles',
        children: [{ id: 2, label: 'Definiciones NACHA', route: '/ach-cycles/nacha/definitions' }]
      }
    ]));

    TestBed.configureTestingModule({
      providers: [
        NavigationService,
        { provide: ApiService, useValue: api }
      ]
    });

    const service = TestBed.inject(NavigationService);

    service.getMenu().subscribe((menu) => {
      const routes = flattenRoutes(menu);

      expect(routes).not.toContain('/ach-cycles/nacha/definitions');
      expect(routes).not.toContain('/nacha-record-definitions');
      expect(routes).toContain('/nacha-config-admin/records');
      expect(flattenLabels(menu)).toContain('Registros oficiales');
      done();
    });
  });

  it('OfficialNavigation_ShouldExposeConfigProfiles', (done) => {
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
      const routes = flattenRoutes(menu);
      const labels = flattenLabels(menu);

      expect(routes).toContain('/nacha-config-admin/perfiles');
      expect(routes).toContain('/nacha-config-admin/records');
      expect(routes).toContain('/nacha-config-admin/variants-fields');
      expect(routes).not.toContain('/ach-cycles/nacha/layouts');
      expect(routes).not.toContain('/ach-cycles/nacha/definitions');
      expect(routes).not.toContain('/nacha-layouts');
      expect(routes).not.toContain('/nacha-record-definitions');
      expect(labels).toContain('Configuración NACHA-M');
      expect(labels).toContain('Perfiles oficiales');
      expect(labels).toContain('Registros oficiales');
      expect(labels).toContain('Variantes y campos');
      done();
    });
  });

  it('Navigation_ShouldExposeSoapUatConsole', (done) => {
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
      const routes = flattenRoutes(menu);
      const labels = flattenLabels(menu);

      expect(routes).toContain('/ach/nacha/soap-uat-console');
      expect(labels).toContain('SOAP UAT Console');
      done();
    });
  });

  it('Navigation_ShouldExposeReconciliationConsole', (done) => {
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
      const routes = flattenRoutes(menu);
      const labels = flattenLabels(menu);

      expect(routes).toContain('/ach/reconciliation');
      expect(labels).toContain('Conciliacion ACH');
      done();
    });
  });

  it('OfficialNavigation_ShouldNotExposeLegacyAsOfficial', (done) => {
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
      const labels = flattenLabels(menu).join(' ');
      const routes = flattenRoutes(menu);

      expect(labels).not.toContain('Layouts NACHA');
      expect(labels).not.toContain('Definiciones NACHA');
      expect(labels).not.toContain('legacy');
      expect(routes).not.toContain('/ach-cycles/nacha/layouts');
      expect(routes).not.toContain('/ach-cycles/nacha/definitions');
      expect(labels).toContain('Perfiles oficiales');
      expect(labels).toContain('Registros oficiales');
      expect(labels).toContain('Variantes y campos');
      done();
    });
  });
});

function flattenRoutes(items: Array<{ route: string; children?: Array<{ route: string; children?: any[] }> }>): string[] {
  return items.flatMap((item) => [item.route, ...flattenRoutes(item.children ?? [])]);
}

function flattenLabels(items: Array<{ label: string; children?: Array<{ label: string; children?: any[] }> }>): string[] {
  return items.flatMap((item) => [item.label, ...flattenLabels(item.children ?? [])]);
}
