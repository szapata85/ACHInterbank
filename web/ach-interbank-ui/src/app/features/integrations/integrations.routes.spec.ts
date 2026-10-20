import { permissionGuard } from '../../core/guards/permission.guard';
import { INTEGRATIONS_ROUTES } from './integrations.routes';

describe('INTEGRATIONS_ROUTES', () => {
  it('expone Conexiones como ruta principal sin redirect', () => {
    const route = INTEGRATIONS_ROUTES.find((item) => item.path === '');

    expect(route).toBeDefined();
    expect(route?.pathMatch).toBe('full');
    expect(route?.loadComponent).toBeDefined();
    expect(route?.redirectTo).toBeUndefined();
  });

  it('carga de forma lazy las pantallas standalone que siguen siendo dependencias internas', () => {
    const functionalPaths = [
      'contrato',
      'relacion-informacion',
      'relacion-informacion/:methodCode/:mappingSetId',
      'conexion'
    ];

    functionalPaths.forEach((path) => {
      const route = INTEGRATIONS_ROUTES.find((item) => item.path === path);
      expect(route).withContext(`Falta ${path}`).toBeDefined();
      expect(route?.loadComponent).withContext(`Falta lazy loadComponent en ${path}`).toBeDefined();
      expect(route?.redirectTo).toBeUndefined();
    });
  });

  it('protege explícitamente Contrato del servicio con Integrations.Read o CanReadAch sin heredar la autorización de siblings', () => {
    const route = INTEGRATIONS_ROUTES.find((item) => item.path === 'contrato');
    const connection = INTEGRATIONS_ROUTES.find((item) => item.path === 'conexion');

    expect(route?.canActivate).toEqual([permissionGuard]);
    expect(route?.data?.['permissions']).toEqual(['Integrations.Read', 'CanReadAch']);
    expect(route?.data?.['breadcrumb']).toBe('Contrato del servicio');
    expect(connection?.canActivate).toEqual([permissionGuard]);
    expect(connection?.data?.['permissions']).toEqual(['Integrations.Read', 'CanReadAch']);
  });

  it('protege explícitamente Relación de información y su editor con Integrations.Read o CanReadAch', () => {
    const list = INTEGRATIONS_ROUTES.find((item) => item.path === 'relacion-informacion');
    const editor = INTEGRATIONS_ROUTES.find((item) => item.path === 'relacion-informacion/:methodCode/:mappingSetId');

    expect(list?.canActivate).toEqual([permissionGuard]);
    expect(list?.data?.['permissions']).toEqual(['Integrations.Read', 'CanReadAch']);
    expect(editor?.canActivate).toEqual([permissionGuard]);
    expect(editor?.data?.['permissions']).toEqual(['Integrations.Read', 'CanReadAch']);
  });

  it('protege explícitamente Conexión entidad participante con Integrations.Read o CanReadAch', () => {
    const connection = INTEGRATIONS_ROUTES.find((item) => item.path === 'conexion');

    expect(connection?.canActivate).toEqual([permissionGuard]);
    expect(connection?.data?.['permissions']).toEqual(['Integrations.Read', 'CanReadAch']);
    expect(connection?.data?.['breadcrumb']).toBe('Conexión entidad participante');
  });

  it('no conserva rutas técnicas redundantes ni ciclos de redirect', () => {
    const paths = INTEGRATIONS_ROUTES.map((route) => route.path);

    expect(new Set(paths).size).toBe(paths.length);
    expect(paths).not.toContain('mappings');
    expect(paths).not.toContain('soap-settings');
    expect(INTEGRATIONS_ROUTES.some((route) => route.redirectTo)).toBeFalse();
  });
});
