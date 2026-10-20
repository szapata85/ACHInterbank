import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { permissionGuard } from './permission.guard';
import { roleGuard } from './role.guard';

describe('authorization guards', () => {
  let auth: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    auth = jasmine.createSpyObj<AuthService>('AuthService', [
      'isAuthenticated',
      'hasPermission',
      'hasRole',
      'logout'
    ]);
    router = jasmine.createSpyObj<Router>('Router', ['parseUrl']);
    router.parseUrl.and.callFake((url: string) => ({ url }) as unknown as UrlTree);

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router }
      ]
    });
  });

  it('permissionGuard redirects authenticated users without permission to unauthorized', () => {
    auth.isAuthenticated.and.returnValue(true);
    auth.hasPermission.and.returnValue(false);

    const result = runPermissionGuard({ permissions: ['CanManageAch'] });

    expect(router.parseUrl).toHaveBeenCalledOnceWith('/unauthorized');
    expect((result as unknown as { url: string }).url).toBe('/unauthorized');
    expect(auth.logout).not.toHaveBeenCalled();
  });

  it('permissionGuard redirects unauthenticated users to login', () => {
    auth.isAuthenticated.and.returnValue(false);

    const result = runPermissionGuard({ permissions: ['CanReadAch'] });

    expect(auth.logout).toHaveBeenCalled();
    expect(router.parseUrl).toHaveBeenCalledOnceWith('/login');
    expect((result as unknown as { url: string }).url).toBe('/login');
  });

  it('permissionGuard allows users with the expected permission', () => {
    auth.isAuthenticated.and.returnValue(true);
    auth.hasPermission.and.returnValue(true);

    expect(runPermissionGuard({ permissions: ['CanReadAch'] })).toBeTrue();
  });

  it('roleGuard redirects authenticated users without role to unauthorized', () => {
    auth.isAuthenticated.and.returnValue(true);
    auth.hasRole.and.returnValue(false);

    const result = runRoleGuard({ roles: ['Admin'] });

    expect(router.parseUrl).toHaveBeenCalledOnceWith('/unauthorized');
    expect((result as unknown as { url: string }).url).toBe('/unauthorized');
    expect(auth.logout).not.toHaveBeenCalled();
  });

  it('roleGuard redirects unauthenticated users to login', () => {
    auth.isAuthenticated.and.returnValue(false);

    const result = runRoleGuard({ roles: ['Admin'] });

    expect(auth.logout).toHaveBeenCalled();
    expect(router.parseUrl).toHaveBeenCalledOnceWith('/login');
    expect((result as unknown as { url: string }).url).toBe('/login');
  });

  it('roleGuard allows users with the expected role', () => {
    auth.isAuthenticated.and.returnValue(true);
    auth.hasRole.and.returnValue(true);

    expect(runRoleGuard({ roles: ['ACH.Operator'] })).toBeTrue();
  });

  function runPermissionGuard(data: Record<string, unknown>) {
    const route = { data } as unknown as ActivatedRouteSnapshot;
    return TestBed.runInInjectionContext(() => permissionGuard(route, {} as RouterStateSnapshot));
  }

  function runRoleGuard(data: Record<string, unknown>) {
    const route = { data } as unknown as ActivatedRouteSnapshot;
    return TestBed.runInInjectionContext(() => roleGuard(route, {} as RouterStateSnapshot));
  }
});
