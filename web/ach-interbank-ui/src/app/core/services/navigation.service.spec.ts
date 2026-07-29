import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { MenuItem } from '../models/menu.model';
import { ApiService } from './api.service';
import { NavigationService } from './navigation.service';

describe('NavigationService', () => {
  it('uses an empty backend menu without injecting local entries', (done) => {
    const { api, service } = createService([]);

    service.getMenu().subscribe((menu) => {
      expect(menu).toEqual([]);
      expect(api.get).toHaveBeenCalledOnceWith('api/navigation/menu');
      done();
    });
  });

  it('does not reinsert routes filtered out by backend permissions', (done) => {
    const { service } = createService([
      { id: 1, label: 'Inicio', route: '/dashboard', order: 1 }
    ]);

    service.getMenu().subscribe((menu) => {
      const routes = flattenRoutes(menu);

      expect(routes).toEqual(['/dashboard']);
      expect(routes).not.toContain('/transactions/cycle-configs');
      expect(routes).not.toContain('/ach-responses/manual-review');
      expect(routes).not.toContain('/ach/nacha/soap-uat-console');
      done();
    });
  });

  it('preserves backend order metadata recursively', (done) => {
    const { service } = createService([
      { id: 2, label: 'Segundo', route: '/second', order: 20 },
      {
        id: 1,
        label: 'Primero',
        route: '/first',
        order: 10,
        children: [
          { id: 12, label: 'Hijo segundo', route: '/first/second', order: 2 },
          { id: 11, label: 'Hijo primero', route: '/first/first', order: 1 }
        ]
      }
    ]);

    service.getMenu().subscribe((menu) => {
      expect(menu.map((item) => item.route)).toEqual(['/first', '/second']);
      expect(menu[0].children?.map((item) => item.route)).toEqual(['/first/first', '/first/second']);
      done();
    });
  });

  it('preserves configured icon keys for parent and child items', (done) => {
    const { service } = createService([
      {
        id: 1,
        label: 'Operación',
        route: '/transactions',
        icon: 'account_balance',
        children: [
          { id: 11, label: 'Ciclos', route: '/ach-cycles', icon: 'schedule' }
        ]
      }
    ]);

    service.getMenu().subscribe((menu) => {
      expect(menu[0].icon).toBe('account_balance');
      expect(menu[0].children?.[0].icon).toBe('schedule');
      done();
    });
  });

  it('LegacyLayoutsRoute_ShouldBeRemovedFromMenu', (done) => {
    const { service } = createService([
      { id: 1, label: 'Layouts NACHA', route: '/ach-cycles/nacha/layouts' },
      { id: 2, label: 'Reglas por cámara', route: '/transactions/clearing-house-rules' },
      { id: 3, label: 'Perfiles oficiales', route: '/nacha-config-admin/perfiles' }
    ]);

    service.getMenu().subscribe((menu) => {
      const routes = flattenRoutes(menu);

      expect(routes).not.toContain('/ach-cycles/nacha/layouts');
      expect(routes).not.toContain('/transactions/clearing-house-rules');
      expect(routes).toEqual(['/nacha-config-admin/perfiles']);
      done();
    });
  });

  it('LegacyDefinitionsRoute_ShouldBeRemovedFromMenu', (done) => {
    const { service } = createService([
      {
        id: 1,
        label: 'ACH',
        route: '/ach-cycles',
        children: [
          { id: 2, label: 'Definiciones NACHA', route: '/ACH-CYCLES/NACHA/DEFINITIONS/' },
          { id: 3, label: 'Ciclos', route: '/ach-cycles' }
        ]
      }
    ]);

    service.getMenu().subscribe((menu) => {
      const routes = flattenRoutes(menu);

      expect(routes).not.toContain('/ACH-CYCLES/NACHA/DEFINITIONS/');
      expect(routes).toEqual(['/ach-cycles', '/ach-cycles']);
      done();
    });
  });

  it('OfficialNavigation_ShouldExposeConfigProfilesWhenBackendReturnsIt', (done) => {
    const { service } = createService([
      {
        id: 1,
        label: 'Configuración NACHA-M',
        route: '/nacha-config-admin/perfiles',
        children: [
          { id: 2, label: 'Perfiles oficiales', route: '/nacha-config-admin/perfiles' },
          { id: 3, label: 'Registros oficiales', route: '/nacha-config-admin/records' },
          { id: 4, label: 'Variantes y campos', route: '/nacha-config-admin/variants-fields' }
        ]
      }
    ]);

    service.getMenu().subscribe((menu) => {
      const routes = flattenRoutes(menu);

      expect(routes).toContain('/nacha-config-admin/perfiles');
      expect(routes).toContain('/nacha-config-admin/records');
      expect(routes).toContain('/nacha-config-admin/variants-fields');
      expect(flattenLabels(menu)).toContain('Configuración NACHA-M');
      done();
    });
  });

  it('Navigation_ShouldExposeSoapUatConsoleOnlyWhenBackendReturnsIt', (done) => {
    const { service } = createService([
      { id: 1, label: 'Consola SOAP UAT', route: '/ach/nacha/soap-uat-console' }
    ]);

    service.getMenu().subscribe((menu) => {
      expect(flattenRoutes(menu)).toContain('/ach/nacha/soap-uat-console');
      expect(flattenLabels(menu)).toContain('Consola SOAP UAT');
      done();
    });
  });

  it('Navigation_ShouldExposeReconciliationConsoleOnlyWhenBackendReturnsIt', (done) => {
    const { service } = createService([
      { id: 1, label: 'Conciliación ACH', route: '/ach/reconciliation' }
    ]);

    service.getMenu().subscribe((menu) => {
      expect(flattenRoutes(menu)).toContain('/ach/reconciliation');
      expect(flattenLabels(menu)).toContain('Conciliación ACH');
      done();
    });
  });

  it('OfficialNavigation_ShouldNotExposeLegacyAsOfficial', (done) => {
    const { service } = createService([
      { id: 1, label: 'Layouts NACHA', route: '/nacha-layouts' },
      { id: 2, label: 'Definiciones NACHA', route: '/nacha-record-definitions' },
      { id: 3, label: 'Perfiles oficiales', route: '/nacha-config-admin/perfiles' }
    ]);

    service.getMenu().subscribe((menu) => {
      const labels = flattenLabels(menu).join(' ');
      const routes = flattenRoutes(menu);

      expect(labels).not.toContain('Layouts NACHA');
      expect(labels).not.toContain('Definiciones NACHA');
      expect(routes).not.toContain('/nacha-layouts');
      expect(routes).not.toContain('/nacha-record-definitions');
      expect(routes).toContain('/nacha-config-admin/perfiles');
      done();
    });
  });
});

function createService(menu: MenuItem[]): { api: jasmine.SpyObj<ApiService>; service: NavigationService } {
  const api = jasmine.createSpyObj<ApiService>('ApiService', ['get']);
  api.get.and.returnValue(of(menu));

  TestBed.configureTestingModule({
    providers: [
      NavigationService,
      { provide: ApiService, useValue: api }
    ]
  });

  return { api, service: TestBed.inject(NavigationService) };
}

function flattenRoutes(items: MenuItem[]): string[] {
  return items.flatMap((item) => [item.route, ...flattenRoutes(item.children ?? [])]);
}

function flattenLabels(items: MenuItem[]): string[] {
  return items.flatMap((item) => [item.label, ...flattenLabels(item.children ?? [])]);
}
