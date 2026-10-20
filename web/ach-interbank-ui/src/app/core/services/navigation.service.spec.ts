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

  it('keeps an already filtered backend tree filtered without reintroducing local entries', (done) => {
    const { service } = createService([
      { id: 1, label: 'Inicio', route: '/dashboard', order: 10 }
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

  it('preserves backend labels routes hierarchy and children while sorting siblings only by backend order', (done) => {
    const { service } = createService([
      { id: 3, label: 'Root B', route: '/root-b', order: 20 },
      {
        id: 1,
        label: 'Parent A',
        route: '/parent-a',
        order: 10,
        exact: false,
        children: [
          { id: 12, label: 'Child A2', route: '/parent-a/a2', order: 20 },
          {
            id: 11,
            label: 'Child A1',
            route: '/parent-a/a1',
            order: 10,
            children: [
              { id: 111, label: 'Grandchild A1.1', route: '/parent-a/a1/one', order: 10 }
            ]
          }
        ]
      }
    ]);

    service.getMenu().subscribe((menu) => {
      expect(menu.map((item) => item.route)).toEqual(['/parent-a', '/root-b']);
      expect(menu.map((item) => item.order)).toEqual([10, 20]);
      expect(menu[0]).toEqual(jasmine.objectContaining({
        id: 1,
        label: 'Parent A',
        route: '/parent-a',
        order: 10,
        exact: false
      }));
      expect(menu[0].children?.map((item) => item.label)).toEqual(['Child A1', 'Child A2']);
      expect(menu[0].children?.map((item) => item.route)).toEqual(['/parent-a/a1', '/parent-a/a2']);
      expect(menu[0].children?.map((item) => item.order)).toEqual([10, 20]);
      expect(menu[0].children?.[0].children?.map((item) => item.route)).toEqual(['/parent-a/a1/one']);
      done();
    });
  });

  it('preserves configured icon keys without changing navigation semantics', (done) => {
    const { service } = createService([
      {
        id: 1,
        label: 'Operación',
        route: '/transactions',
        icon: 'account_balance',
        order: 10,
        children: [
          { id: 11, label: 'Ciclos', route: '/ach-cycles', icon: 'schedule', order: 10 }
        ]
      }
    ]);

    service.getMenu().subscribe((menu) => {
      expect(menu[0].icon).toBe('account_balance');
      expect(menu[0].children?.[0].icon).toBe('schedule');
      done();
    });
  });

  it('does not filter or rewrite backend items based on route values', (done) => {
    const { service } = createService([
      { id: 1, label: 'Alias entregado por backend', route: '/aliases', order: 10 },
      { id: 2, label: 'Layout entregado por backend', route: '/ach-cycles/nacha/layouts', order: 20 },
      { id: 3, label: 'Definición entregada por backend', route: '/ach-cycles/nacha/definitions', order: 30 },
      { id: 4, label: 'Layout raíz entregado por backend', route: '/nacha-layouts', order: 40 },
      { id: 5, label: 'Definición raíz entregada por backend', route: '/nacha-record-definitions', order: 50 },
      { id: 6, label: 'Regla entregada por backend', route: '/transactions/clearing-house-rules', order: 60 },
      { id: 7, label: 'Fecha especial entregada por backend', route: '/catalogs/clearing-house-special-dates', order: 70 },
      { id: 8, label: 'Ruta con casing del backend', route: '/ACH-CYCLES/NACHA/DEFINITIONS/', order: 80 }
    ]);

    service.getMenu().subscribe((menu) => {
      expect(flattenRoutes(menu)).toEqual([
        '/aliases',
        '/ach-cycles/nacha/layouts',
        '/ach-cycles/nacha/definitions',
        '/nacha-layouts',
        '/nacha-record-definitions',
        '/transactions/clearing-house-rules',
        '/catalogs/clearing-house-special-dates',
        '/ACH-CYCLES/NACHA/DEFINITIONS/'
      ]);
      expect(flattenLabels(menu)).toEqual([
        'Alias entregado por backend',
        'Layout entregado por backend',
        'Definición entregada por backend',
        'Layout raíz entregado por backend',
        'Definición raíz entregada por backend',
        'Regla entregada por backend',
        'Fecha especial entregada por backend',
        'Ruta con casing del backend'
      ]);
      done();
    });
  });

  it('preserves a new backend child unknown to Angular without a hardcoded rule', (done) => {
    const { service } = createService([
      {
        id: 10,
        label: 'Parent A',
        route: '/parent-a',
        order: 10,
        children: [
          { id: 101, label: 'Child A1', route: '/parent-a/a1', order: 10 },
          { id: 199, label: 'Nueva opción futura', route: '/parent-a/future-child', order: 20 }
        ]
      }
    ]);

    service.getMenu().subscribe((menu) => {
      expect(menu[0].children).toHaveSize(2);
      expect(menu[0].children?.[1]).toEqual(jasmine.objectContaining({
        id: 199,
        label: 'Nueva opción futura',
        route: '/parent-a/future-child',
        order: 20
      }));
      done();
    });
  });

  it('preserves representative visible labels exactly as delivered by backend', (done) => {
    const labels = [
      'Inicio',
      'Operación',
      'Clientes',
      'Seguimiento y control',
      'Administración de parámetros',
      'Seguridad de archivos',
      'Reportes',
      'Administración',
      'Herramientas de prueba',
      'Conexiones',
      'Conexión entidad participante',
      'Configuración del intercambio de archivos'
    ];
    const { service } = createService(labels.map((label, index) => ({
      id: index + 1,
      label,
      route: `/regression-${index + 1}`,
      order: (index + 1) * 10
    })));

    service.getMenu().subscribe((menu) => {
      expect(menu.map((item) => item.label)).toEqual(labels);
      done();
    });
  });

  it('preserva Conexiones y sus hijos exactamente desde el backend', (done) => {
    const { service } = createService([
      {
        id: 29,
        label: 'Conexiones',
        route: '/integraciones',
        order: 80,
        exact: false,
        children: [
          { id: 102, label: 'Conexión entidad participante', route: '/integraciones/conexion', order: 30 },
          { id: 100, label: 'Contrato del servicio', route: '/integraciones/contrato', order: 10 },
          { id: 103, label: 'Configuración del intercambio de archivos', route: '/administracion/mft-ach', order: 40 },
          { id: 101, label: 'Relación de información', route: '/integraciones/relacion-informacion', order: 20 }
        ]
      }
    ]);

    service.getMenu().subscribe((menu) => {
      expect(menu).toHaveSize(1);
      expect(menu[0]).toEqual(jasmine.objectContaining({
        id: 29,
        label: 'Conexiones',
        route: '/integraciones',
        order: 80,
        exact: false
      }));
      expect(menu[0].children).toHaveSize(4);
      expect(menu[0].children?.map((item) => item.label)).toEqual([
        'Contrato del servicio',
        'Relación de información',
        'Conexión entidad participante',
        'Configuración del intercambio de archivos'
      ]);
      expect(menu[0].children?.map((item) => item.route)).toEqual([
        '/integraciones/contrato',
        '/integraciones/relacion-informacion',
        '/integraciones/conexion',
        '/administracion/mft-ach'
      ]);
      expect(menu[0].children?.map((item) => item.order)).toEqual([10, 20, 30, 40]);
      expect(flattenLabels(menu)).not.toContain('Centro de integración CFA');
      done();
    });
  });

  it('does not fabricate or substitute integration routes that backend did not return', (done) => {
    const { service } = createService([
      {
        id: 50,
        label: 'Operación',
        route: '/operacion',
        order: 10,
        children: [
          { id: 51, label: 'Elemento autorizado', route: '/operacion/autorizado', order: 10 }
        ]
      }
    ]);

    service.getMenu().subscribe((menu) => {
      expect(flattenRoutes(menu)).toEqual(['/operacion', '/operacion/autorizado']);
      expect(flattenRoutes(menu)).not.toContain('/integraciones');
      expect(flattenLabels(menu)).not.toContain('Centro de integración CFA');
      done();
    });
  });

  it('OfficialNavigation_ShouldExposeConfigProfilesWhenBackendReturnsIt', (done) => {
    const { service } = createService([
      {
        id: 1,
        label: 'Configuración NACHA-M',
        route: '/nacha-config-admin/perfiles',
        order: 10,
        children: [
          { id: 2, label: 'Perfiles oficiales', route: '/nacha-config-admin/perfiles', order: 10 },
          { id: 3, label: 'Registros oficiales', route: '/nacha-config-admin/records', order: 20 },
          { id: 4, label: 'Variantes y campos', route: '/nacha-config-admin/variants-fields', order: 30 }
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
      { id: 1, label: 'Consola SOAP UAT', route: '/ach/nacha/soap-uat-console', order: 10 }
    ]);

    service.getMenu().subscribe((menu) => {
      expect(flattenRoutes(menu)).toContain('/ach/nacha/soap-uat-console');
      expect(flattenLabels(menu)).toContain('Consola SOAP UAT');
      done();
    });
  });

  it('Navigation_ShouldExposeReconciliationConsoleOnlyWhenBackendReturnsIt', (done) => {
    const { service } = createService([
      { id: 1, label: 'Conciliación ACH', route: '/ach/reconciliation', order: 10 }
    ]);

    service.getMenu().subscribe((menu) => {
      expect(flattenRoutes(menu)).toContain('/ach/reconciliation');
      expect(flattenLabels(menu)).toContain('Conciliación ACH');
      done();
    });
  });

  it('preserves missing children metadata instead of manufacturing an empty children array', (done) => {
    const backendItem: MenuItem = {
      id: 1,
      label: 'Leaf',
      route: '/leaf',
      order: 10
    };
    const { service } = createService([backendItem]);

    service.getMenu().subscribe((menu) => {
      expect(menu[0].children).toBeUndefined();
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
