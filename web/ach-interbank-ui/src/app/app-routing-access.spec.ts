import { Route, Routes } from '@angular/router';
import { APP_ROUTES } from './app-routing.module';
import { permissionGuard } from './core/guards/permission.guard';
import { ACH_CYCLES_ROUTES } from './features/ach-cycles/ach-cycles-routing.module';
import { CUSTOMERS_ROUTES } from './features/customers/customers-routing.module';
import { NACHA_OPERATIONAL_ROUTES } from './features/nacha-operational/nacha-operational-routing.module';
import { NACHA_CONFIG_ADMIN_ROUTES } from './features/nacha-config-admin/nacha-config-admin-routing.module';
import { SCHEDULER_ROUTES } from './features/scheduler/scheduler-routing.module';
import { UAT_ROUTES } from './features/uat/uat-routing.module';

describe('route access control', () => {
  it('separa lectura y gestión en ciclos ACH', () => {
    expectPermission(ACH_CYCLES_ROUTES, '', 'CanReadAch');
    expectPermission(ACH_CYCLES_ROUTES, 'nacha/export', 'CanReadAch');
    expectPermission(ACH_CYCLES_ROUTES, 'new', 'CanManageAch');
    expectPermission(ACH_CYCLES_ROUTES, ':id/edit', 'CanManageAch');
  });

  it('protege la administración del scheduler con permiso de gestión', () => {
    expectPermission(SCHEDULER_ROUTES, 'tasks', 'Scheduler.View');
  });

  it('mantiene las pantallas NACHA Config disponibles en modo lectura', () => {
    NACHA_CONFIG_ADMIN_ROUTES.filter((route) => route.component).forEach((route) => {
      expect(route.canActivate).withContext(`Falta guard en ${route.path}`).toContain(permissionGuard);
      expect(route.data?.['permissions']).withContext(`Falta permiso fino de lectura en ${route.path}`).toContain('Config.Read');
      expect(route.data?.['permissions']).withContext(`Falta permiso legacy de lectura en ${route.path}`).toContain('CanReadAch');
    });
  });

  it('exige TestTools.Access en el simulador sin fallback ni gating SPA adicional por ambiente', () => {
    const privateRoutes = APP_ROUTES.find((route) => route.path === '' && route.children)?.children ?? [];
    const uat = privateRoutes.find((route) => route.path === 'uat');

    expect(uat).toBeDefined();
    expect(uat?.canActivate).toEqual([permissionGuard]);
    expect(uat?.data?.['permissions']).toEqual(['TestTools.Access']);
    expect(uat?.data?.['roles']).toBeUndefined();
    expect(uat?.canMatch).toBeUndefined();

    const simulator = findRoute(UAT_ROUTES, 'nacha-inbound-simulator');
    expect(simulator?.canActivate).toEqual([permissionGuard]);
    expect(simulator?.data?.['permissions']).toEqual(['TestTools.Access']);
    expect(simulator?.data?.['roles']).toBeUndefined();
    expect(simulator?.canMatch).toBeUndefined();

    const serviceTests = privateRoutes.find((route) => route.path === 'ach/nacha/soap-uat-console');
    expect(serviceTests).toBeDefined();
    expect(serviceTests?.canActivate).toEqual([permissionGuard]);
    expect(serviceTests?.data?.['permissions']).toEqual(['TestTools.Access']);
    expect(serviceTests?.data?.['roles']).toBeUndefined();
    expect(serviceTests?.canMatch).toBeUndefined();
    expect(serviceTests?.loadComponent).toBeDefined();
    expect(NACHA_OPERATIONAL_ROUTES.find((route) => route.path === 'nacha/soap-uat-console')).toBeUndefined();

    const ach = privateRoutes.find((route) => route.path === 'ach');
    expect(ach?.data?.['roles']).toEqual(['Admin', 'ACH.Operator']);
    expect(ach?.data?.['permissions']).toEqual(['CanReadAch']);
  });

  it('evalúa los permisos de gestión en rutas hijas de clientes', () => {
    const customerChildren = CUSTOMERS_ROUTES[0].children ?? [];
    expectPermission(customerChildren, 'new', 'CanManageAch');
    expectPermission(customerChildren, ':id/edit', 'CanManageAch');
  });

  it('protege los registros del sistema con capacidades semánticas y fallbacks legacy', () => {
    const privateRoutes = APP_ROUTES.find((route) => route.path === '' && route.children)?.children ?? [];

    const auditLogs = privateRoutes.find((route) => route.path === 'audit-logs');
    expect(auditLogs).toBeDefined();
    expect(auditLogs?.canActivate).toEqual([permissionGuard]);
    expect(auditLogs?.data?.['permissions']).toEqual(['AuditLogs.Read', 'CanManageUsers']);
    expect(auditLogs?.data?.['roles']).toBeUndefined();

    const authLogs = privateRoutes.find((route) => route.path === 'auth-logs');
    expect(authLogs).toBeDefined();
    expect(authLogs?.canActivate).toEqual([permissionGuard]);
    expect(authLogs?.data?.['permissions']).toEqual(['AuthLogs.Read', 'CanManageUsers']);
    expect(authLogs?.data?.['roles']).toBeUndefined();

    const navigationLogs = privateRoutes.find((route) => route.path === 'navigation-logs');
    expect(navigationLogs).toBeDefined();
    expect(navigationLogs?.canActivate).toEqual([permissionGuard]);
    expect(navigationLogs?.data?.['permissions']).toEqual(['NavigationLogs.Read', 'CanReadAch']);
    expect(navigationLogs?.data?.['roles']).toBeUndefined();
  });

  it('mantiene /aliases retirado y cubierto por el not-found existente', () => {
    const privateRoutes = APP_ROUTES.find((route) => route.path === '' && route.children)?.children ?? [];
    expect(privateRoutes.find((route) => route.path === 'aliases')).toBeUndefined();
    expect(privateRoutes.find((route) => route.path === '**')?.redirectTo).toBe('not-found');
  });

  it('mantiene la configuración de ciclos bajo el contexto canónico de clearing house', () => {
    const privateRoutes = APP_ROUTES.find((route) => route.path === '' && route.children)?.children ?? [];
    const clearingHouses = privateRoutes.find((route) => route.path === 'clearing-houses');
    const cycleConfig = clearingHouses?.children?.find((route) => route.path === ':id/cycles');

    expect(clearingHouses).toBeDefined();
    expect(cycleConfig).toBeDefined();
    expect(cycleConfig?.canActivate).toContain(permissionGuard);
    expect(cycleConfig?.data?.['permissions']).toEqual(['ClearingHouses.View', 'ClearingHouses.ManageCycles']);
    expect(cycleConfig?.loadComponent).toBeDefined();
    expect(cycleConfig?.redirectTo).toBeUndefined();
  });

  it('expone Conexiones como ruta principal lazy sin alias técnicos', () => {
    const privateRoutes = APP_ROUTES.find((route) => route.path === '' && route.children)?.children ?? [];
    const integrations = privateRoutes.find((route) => route.path === 'integraciones');

    expect(integrations).toBeDefined();
    expect(integrations?.canActivate).toContain(permissionGuard);
    expect(integrations?.data?.['permissions']).toEqual(['Integrations.Read', 'CanReadAch', 'CanManageAch']);
    expect(integrations?.data?.['title']).toBe('Conexiones');
    expect(integrations?.loadChildren).toBeDefined();
    expect(integrations?.redirectTo).toBeUndefined();
    expect(privateRoutes.find((route) => route.path === 'soap-integrations')).toBeUndefined();
  });

  it('protege la administración de restricciones operacionales CFA con permiso fino', () => {
    const privateRoutes = APP_ROUTES.find((route) => route.path === '' && route.children)?.children ?? [];
    const route = privateRoutes.find((child) => child.path === 'operational-restrictions');
    expect(route).toBeDefined();
    expect(route?.canActivate).toContain(permissionGuard);
    expect(route?.data?.['permissions']).toEqual(['OperationalRestrictions.Read']);
  });

  it('redirige las rutas legacy NACHA-M explícitamente a not-found', () => {
    expectRedirect(ACH_CYCLES_ROUTES, 'nacha/layouts', '/not-found');
    expectRedirect(ACH_CYCLES_ROUTES, 'nacha/definitions', '/not-found');

    const privateRoutes = APP_ROUTES.find((route) => route.path === '' && route.children)?.children ?? [];
    expectRedirect(privateRoutes, 'nacha-layouts', 'not-found');
    expectRedirect(privateRoutes, 'nacha-record-definitions', 'not-found');
  });
});

function expectPermission(routes: Routes, path: string, permission: string): void {
  const route = findRoute(routes, path);

  expect(route).withContext(`No existe la ruta ${path}`).toBeDefined();
  expect(route?.canActivate).withContext(`Falta guard en ${path}`).toContain(permissionGuard);
  expect(route?.data?.['permissions']).toEqual([permission]);
}

function expectRedirect(routes: Routes, path: string, redirectTo: string): void {
  const route = findRoute(routes, path);

  expect(route).withContext(`No existe el redirect ${path}`).toBeDefined();
  expect(route?.pathMatch).toBe('full');
  expect(route?.redirectTo).toBe(redirectTo);
}

function findRoute(routes: Routes, path: string): Route | undefined {
  return routes.find((route) => route.path === path);
}
