import { permissionGuard } from '../../core/guards/permission.guard';
import { NACHA_SECURITY_ROUTES } from './nacha-security-routing.module';
import { NACHA_SECURITY_PERMISSIONS } from './nacha-security-permissions';

describe('NachaSecurityRoutingModule', () => {
  const childRoutes = NACHA_SECURITY_ROUTES[0].children ?? [];

  it('conserva las rutas operativas de Seguridad NACHA sin interoperabilidad residual', () => {
    const paths = childRoutes.map(route => route.path);

    expect(paths).toContain('certificates');
    expect(paths).toContain('certificates/:id/versions');
    expect(paths).toContain('nacha/generate');
    expect(paths).toContain('nacha/generate-encrypted');
    expect(paths).toContain('digital-envelope/manual-encrypt');
    expect(paths).toContain('digital-envelope/manual-decrypt');
    expect(paths).toContain('sobre-digital');
    expect(paths.some(path => path?.includes('interoperability'))).toBeFalse();
  });

  it('mantiene permisos en las rutas que ejecutan acciones NACHA', () => {
    const byPath = new Map(childRoutes.map(route => [route.path, route]));

    expect(byPath.get('nacha/generate')?.data?.['permissions']).toContain(NACHA_SECURITY_PERMISSIONS.canGenerateNacha);
    expect(byPath.get('nacha/generate-encrypted')?.data?.['permissions']).toContain(NACHA_SECURITY_PERMISSIONS.canGenerateEncryptedNacha);
    expect(byPath.get('digital-envelope/manual-encrypt')?.data?.['permissions']).toContain(NACHA_SECURITY_PERMISSIONS.canManualEncryptEnvelope);
    expect(byPath.get('digital-envelope/manual-decrypt')?.data?.['permissions']).toContain(NACHA_SECURITY_PERMISSIONS.canManualDecryptEnvelope);
  });

  it('alinea certificados y sus versiones con la capacidad de lectura del backend', () => {
    const byPath = new Map(childRoutes.map(route => [route.path, route]));
    for (const path of ['certificates', 'certificates/:id/versions']) {
      const route = byPath.get(path);
      expect(route?.data?.['permissions']).toEqual([
        NACHA_SECURITY_PERMISSIONS.certificatesRead,
        NACHA_SECURITY_PERMISSIONS.canManageCertificates,
        NACHA_SECURITY_PERMISSIONS.canReadAch
      ]);
    }

    expect(NACHA_SECURITY_ROUTES[0].canActivate).toContain(permissionGuard);
    expect(NACHA_SECURITY_ROUTES[0].canActivateChild).toContain(permissionGuard);
  });

});
