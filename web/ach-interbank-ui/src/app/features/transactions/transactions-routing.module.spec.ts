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

  it('protege las rutas legacy con acciones de mutación con permiso de gestión', () => {
    ['create', 'bulk-create', 'cycle-configs'].forEach((path) => expectRouteContainsPermission(path, 'CanManageAch'));
    expectRouteContainsPermission('create', 'Transactions.Create');
  });

  it('protege bulk-ingestion/upload con capability fina o gestión legacy, sin lectura', () => {
    const route = TRANSACTIONS_ROUTES.find((candidate) => candidate.path === 'bulk-ingestion/upload');

    expect(route).toBeDefined();
    expect(route?.canActivate).toContain(permissionGuard);
    expect(route?.data?.['permissions']).toEqual(['BulkIngestion.Upload', 'CanManageAch']);
    expect(route?.data?.['permissions']).not.toContain('BulkIngestion.Read');
    expect(route?.data?.['permissions']).not.toContain('CanReadAch');
    expect(route?.data?.['roles']).toBeUndefined();
  });

  it('protege returns-ror como superficie mixta con capacidad de lectura para entrada', () => {
    expectRouteContainsPermission('returns-ror', 'Returns.Read');
    expectRouteContainsPermission('returns-ror', 'CanReadAch');
    const route = TRANSACTIONS_ROUTES.find((candidate) => candidate.path === 'returns-ror');
    expect(route?.data?.['permissions']).not.toContain('CanManageAch');
  });

  it('protege seguimiento y detalle de cargas con la capacidad de lectura del backend', () => {
    for (const path of ['bulk-ingestion/tracking', 'bulk-ingestion/:batchId']) {
      const route = TRANSACTIONS_ROUTES.find((candidate) => candidate.path === path);

      expect(route).withContext(`No existe la ruta ${path}`).toBeDefined();
      expect(route?.canActivate).withContext(`Falta guard en ${path}`).toContain(permissionGuard);
      expect(route?.data?.['permissions']).toEqual(['BulkIngestion.Read', 'CanReadAch']);
      expect(route?.data?.['permissions']).not.toContain('CanManageAch');
      expect(route?.data?.['roles']).toBeUndefined();
    }
  });

  it('mantiene Devoluciones como superficie de lectura y deja la mutación a controles de acción', () => {
    const route = TRANSACTIONS_ROUTES.find((candidate) => candidate.path === 'returns');

    expect(route).toBeDefined();
    expect(route?.canActivate).toContain(permissionGuard);
    expect(route?.data?.['permissions']).toEqual(['CanReadAch']);
    expect(route?.data?.['permissions']).not.toContain('CanManageAch');
  });

  it('protege la carga NACHA-M con la capacidad fina o gestión legacy, sin fallback de lectura', () => {
    const route = TRANSACTIONS_ROUTES.find((candidate) => candidate.path === 'nacha-upload');

    expect(route).toBeDefined();
    expect(route?.canActivate).toContain(permissionGuard);
    expect(route?.data?.['permissions']).toEqual(['Nacha.Upload', 'CanManageAch']);
    expect(route?.data?.['permissions']).not.toContain('CanReadAch');
  });

  it('redirige cycle-configs al listado de cámaras sin montar una pantalla que requiere :id', () => {
    const route = TRANSACTIONS_ROUTES.find((candidate) => candidate.path === 'cycle-configs');

    expect(route).toBeDefined();
    expect(route?.pathMatch).toBe('full');
    expect(route?.redirectTo).toBe('/clearing-houses');
    expect(route?.component).toBeUndefined();
    expect(route?.canActivate).toContain(permissionGuard);
    expect(route?.data?.['permissions']).toEqual(['CanManageAch']);
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
