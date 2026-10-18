import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { MenuItem } from '../models/menu.model';
import { NavigationNamingService, RouteNamingEntry } from './navigation-naming.service';
import { NavigationService } from './navigation.service';

describe('NavigationNamingService', () => {
  let service: NavigationNamingService;

  const menu: MenuItem[] = [
    { id: 1, label: 'Inicio', route: '/dashboard', exact: true },
    {
      id: 50,
      label: 'Administración de parámetros',
      route: '/administracion-parametros',
      children: [
        {
          id: 58,
          label: 'Conexiones',
          route: '/integraciones',
          children: [
            { id: 581, label: 'Contrato del servicio', route: '/integraciones/contrato', exact: true },
            { id: 582, label: 'Relación de información', route: '/integraciones/relacion-informacion', exact: true },
            { id: 583, label: 'Conexión entidad participante', route: '/integraciones/conexion', exact: true },
            { id: 584, label: 'Configuración del intercambio de archivos', route: '/administracion/mft-ach', exact: true }
          ]
        }
      ]
    },
    {
      id: 70,
      label: 'Reportes',
      route: '/reports',
      children: [
        { id: 701, label: 'Transacciones enviadas', route: '/reports/sent', exact: true },
        { id: 702, label: 'Transacciones recibidas', route: '/reports/received', exact: true }
      ]
    },
    {
      id: 80,
      label: 'Administración',
      route: '/administracion',
      children: [
        {
          id: 81,
          label: 'Usuarios y acceso',
          route: '/users',
          children: [
            { id: 811, label: 'Administrar usuarios', route: '/users/list', exact: true }
          ]
        }
      ]
    }
  ];

  beforeEach(() => {
    const navigationService = jasmine.createSpyObj<NavigationService>('NavigationService', ['getMenu']);
    navigationService.getMenu.and.returnValue(of(menu));

    TestBed.configureTestingModule({
      providers: [{ provide: NavigationService, useValue: navigationService }]
    });

    service = TestBed.inject(NavigationNamingService);
  });

  it('uses backend ancestors and label for an exact menu-backed route', () => {
    const naming = service.resolveEntries(menu, '/integraciones/conexion', [
      route('/integraciones', 'Centro de integración CFA', 'Centro de integración CFA'),
      route('/integraciones/conexion', 'Conexión CFA', 'Conexión CFA')
    ]);

    expect(naming.pageTitle).toBe('Conexión entidad participante');
    expect(naming.breadcrumbs.map((crumb) => crumb.label)).toEqual([
      'Administración de parámetros',
      'Conexiones',
      'Conexión entidad participante'
    ]);
  });

  it('keeps backend naming as source of truth after menu load even when route metadata has a different fallback', () => {
    const changed = structuredClone(menu);
    changed[2].label = 'Canonical Y';

    const naming = service.resolveEntries(changed, '/reports', [
      route('/reports', 'Fallback X', 'Fallback X')
    ]);

    expect(naming.pageTitle).toBe('Canonical Y');
    expect(naming.breadcrumbs.map((crumb) => crumb.label)).toEqual(['Canonical Y']);
  });

  it('reflects a synthetic backend label change without Angular route-to-label configuration', () => {
    const changed = structuredClone(menu);
    const connections = changed[1].children![0];
    connections.label = 'Conexiones sintéticas';
    connections.children![2].label = 'Conexión sintética futura';

    const naming = service.resolveEntries(changed, '/integraciones/conexion', [
      route('/integraciones', 'Alias Angular viejo', 'Alias Angular viejo'),
      route('/integraciones/conexion', 'Alias child viejo', 'Alias child viejo')
    ]);

    expect(naming.pageTitle).toBe('Conexión sintética futura');
    expect(naming.breadcrumbs.map((crumb) => crumb.label)).toEqual([
      'Administración de parámetros',
      'Conexiones sintéticas',
      'Conexión sintética futura'
    ]);
  });

  it('uses a future backend child automatically once its registered URL is activated', () => {
    const changed = structuredClone(menu);
    changed[1].children![0].children!.push({
      id: 585,
      label: 'Conexión futura registrada',
      route: '/integraciones/futura',
      exact: true
    });

    const naming = service.resolveEntries(changed, '/integraciones/futura', [
      route('/integraciones', 'Alias viejo', 'Alias viejo'),
      route('/integraciones/futura', 'Fallback de ruta', 'Fallback de ruta')
    ]);

    expect(naming.pageTitle).toBe('Conexión futura registrada');
    expect(naming.breadcrumbs.at(-1)?.label).toBe('Conexión futura registrada');
  });

  it('uses Reportes for the landing and ignores query params and fragments', () => {
    const naming = service.resolveEntries(menu, '/reports?x=1#section', [
      route('/reports', 'Reportes', 'Reportes ACH')
    ]);

    expect(naming.pageTitle).toBe('Reportes');
    expect(naming.breadcrumbs.map((crumb) => crumb.label)).toEqual(['Reportes']);
  });

  it('uses backend naming for a Reportes child instead of route metadata', () => {
    const naming = service.resolveEntries(menu, '/reports/sent', [
      route('/reports', 'Reportes', 'Reportes ACH'),
      route('/reports/sent', undefined, 'Enviados')
    ]);

    expect(naming.pageTitle).toBe('Transacciones enviadas');
    expect(naming.breadcrumbs.map((crumb) => crumb.label)).toEqual(['Reportes', 'Transacciones enviadas']);
  });

  it('keeps the canonical backend hierarchy for MFT even though its URL prefix is unrelated', () => {
    const naming = service.resolveEntries(menu, '/administracion/mft-ach', [
      route('/administracion/mft-ach', 'MFT ACH', 'Administración MFT ACH')
    ]);

    expect(naming.pageTitle).toBe('Configuración del intercambio de archivos');
    expect(naming.breadcrumbs.map((crumb) => crumb.label)).toEqual([
      'Administración de parámetros',
      'Conexiones',
      'Configuración del intercambio de archivos'
    ]);
  });

  it('keeps contextual route metadata and never exposes a parameter token as the title', () => {
    const naming = service.resolveEntries(menu, '/users/42/edit', [
      route('/users/42/edit', 'Editar usuario', 'Editar usuario')
    ]);

    expect(naming.pageTitle).toBe('Editar usuario');
    expect(naming.pageTitle).not.toContain(':id');
    expect(naming.breadcrumbs.map((crumb) => crumb.label)).toEqual([
      'Administración',
      'Usuarios y acceso',
      'Editar usuario'
    ]);
  });

  it('keeps contextual metadata when no MenuItem exists', () => {
    const naming = service.resolveEntries([], '/users/42/roles', [
      route('/users/42/roles', 'Perfiles de acceso', 'Administrar perfiles de acceso')
    ]);

    expect(naming.menuBacked).toBeFalse();
    expect(naming.pageTitle).toBe('Administrar perfiles de acceso');
    expect(naming.breadcrumbs.map((crumb) => crumb.label)).toEqual(['Perfiles de acceso']);
  });

  it('canonicalizes route-linked local breadcrumbs while preserving contextual labels', () => {
    expect(service.canonicalLabelForRoute(menu, '/integraciones', 'Centro de integración CFA')).toBe('Conexiones');
    expect(service.canonicalLabelForRoute(menu, undefined, 'Editor de configuración')).toBe('Editor de configuración');
  });

  function route(url: string, breadcrumb?: string, title?: string): RouteNamingEntry {
    return { url, breadcrumb, title };
  }
});
