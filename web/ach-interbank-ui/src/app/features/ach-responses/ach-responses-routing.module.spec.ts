import { ACH_RESPONSES_ROUTES } from './ach-responses-routing.module';
import { permissionGuard } from '../../core/guards/permission.guard';

describe('AchResponsesRoutingModule', () => {
  it('ShouldDefineExpectedRoutes', () => {
    const paths = ACH_RESPONSES_ROUTES.map((route) => route.path);

    expect(paths).toContain('');
    expect(paths).toContain('manual-review');
    expect(paths).toContain('status-mappings');
    expect(paths).toContain('dashboard');
    expect(paths).toContain(':id/notification-attempts');
    expect(paths).toContain(':id');
  });

  it('ShouldDefineRoutesInSafeOrder', () => {
    const paths = ACH_RESPONSES_ROUTES.map((route) => route.path);

    expect(paths).toEqual([
      '',
      'manual-review',
      'status-mappings',
      'dashboard',
      ':id/notification-attempts',
      ':id'
    ]);
  });

  it('ShouldProtectEveryResponseRouteWithReadPermission', () => {
    ACH_RESPONSES_ROUTES.forEach((route) => {
      expect(route.canActivate).withContext(`Falta guard en ${route.path}`).toContain(permissionGuard);
      expect(route.data?.['permissions']).withContext(`Permiso incorrecto en ${route.path}`).toEqual(['CanReadAch']);
    });
  });
});
