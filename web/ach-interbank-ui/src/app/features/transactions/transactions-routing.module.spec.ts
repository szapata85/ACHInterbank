import { permissionGuard } from '../../core/guards/permission.guard';
import { TRANSACTIONS_ROUTES } from './transactions-routing.module';

describe('TransactionsRoutingModule', () => {
  it('ruta /transactions/returns-ror existe', () => {
    expect(TRANSACTIONS_ROUTES.some((route) => route.path === 'returns-ror')).toBeTrue();
  });

  it('protege el listado con permiso de lectura', () => {
    expectRoutePermission('list', 'CanReadAch');
  });

  it('protege listado y detalle del monitor con el permiso funcional específico', () => {
    expectRouteContainsPermission('outgoing-monitoring', 'OutgoingTransactions.Monitor.Read');
    expectRouteContainsPermission('outgoing-monitoring/:id', 'OutgoingTransactions.Monitor.Read');
  });

  it('protege las rutas con acciones de mutación con permiso de gestión', () => {
    [
      'create',
      'bulk-create',
      'bulk-ingestion/upload',
      'bulk-ingestion/tracking',
      'bulk-ingestion/:batchId',
      'nacha-upload',
      'cycle-configs',
      'returns',
      'returns-ror'
    ].forEach((path) => expectRoutePermission(path, 'CanManageAch'));
  });

  it('redirige la ruta histórica de reglas al listado de cámaras', () => {
    const route = TRANSACTIONS_ROUTES.find((candidate) => candidate.path === 'clearing-house-rules');
    expect(route?.redirectTo).toBe('/clearing-houses');
    expect(route?.component).toBeUndefined();
  });

  function expectRoutePermission(path: string, permission: string): void {
    const route = TRANSACTIONS_ROUTES.find((candidate) => candidate.path === path);

    expect(route).withContext(`No existe la ruta ${path}`).toBeDefined();
    expect(route?.canActivate).withContext(`Falta guard en ${path}`).toContain(permissionGuard);
    expect(route?.data?.['permissions']).toEqual([permission]);
  }

  function expectRouteContainsPermission(path: string, permission: string): void {
    const route = TRANSACTIONS_ROUTES.find((candidate) => candidate.path === path);

    expect(route).withContext(`No existe la ruta ${path}`).toBeDefined();
    expect(route?.canActivate).withContext(`Falta guard en ${path}`).toContain(permissionGuard);
    expect(route?.data?.['permissions']).toContain(permission);
  }
});
