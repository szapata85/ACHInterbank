import { Route, Routes } from '@angular/router';
import { APP_ROUTES } from './app-routing.module';
import { permissionGuard } from './core/guards/permission.guard';
import { ACH_CYCLES_ROUTES } from './features/ach-cycles/ach-cycles-routing.module';
import { ALIASES_ROUTES } from './features/aliases/aliases-routing.module';
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

  it('protege el simulador UAT con permiso de gestión', () => {
    expectPermission(UAT_ROUTES, 'nacha-inbound-simulator', 'CanManageAch');
    expectPermission(NACHA_OPERATIONAL_ROUTES, 'nacha/soap-uat-console', 'CanManageAch');
  });

  it('evalúa los permisos de gestión en rutas hijas de alias y clientes', () => {
    const aliasChildren = ALIASES_ROUTES[0].children ?? [];
    const customerChildren = CUSTOMERS_ROUTES[0].children ?? [];
    expectPermission(aliasChildren, 'new', 'CanManageAliases');
    expectPermission(aliasChildren, ':id/edit', 'CanManageAliases');
    expectPermission(customerChildren, 'new', 'CanManageAch');
    expectPermission(customerChildren, ':id/edit', 'CanManageAch');
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
