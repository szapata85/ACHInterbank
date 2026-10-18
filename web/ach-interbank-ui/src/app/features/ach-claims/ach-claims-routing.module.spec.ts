import { permissionGuard } from '../../core/guards/permission.guard';
import { APP_ROUTES } from '../../app-routing.module';
import { ACH_CLAIMS_ROUTES } from './ach-claims-routing.module';

describe('AchClaimsRoutingModule', () => {
  it('está publicada en la ruta principal /ach-claims', () => {
    const main = APP_ROUTES.find(route => route.path === '');
    expect(main?.children?.some(route => route.path === 'ach-claims')).toBeTrue();
  });

  it('protege listado y detalle exclusivamente con Claims.Read', () => {
    for (const path of ['', ':id']) {
      const route = ACH_CLAIMS_ROUTES.find(candidate => candidate.path === path);
      expect(route).withContext(`No existe la ruta ${path}`).toBeDefined();
      expect(route?.canActivate).toContain(permissionGuard);
      expect(route?.data?.['permissions']).toEqual(['Claims.Read']);
    }
  });
});
