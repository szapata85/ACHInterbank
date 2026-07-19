import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const permissionGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const expectedPermissions = route.data?.['permissions'] as string[] | undefined;

  if (!authService.isAuthenticated()) {
    authService.logout();
    return router.parseUrl('/login');
  }

  if (!expectedPermissions || expectedPermissions.length === 0 || authService.hasPermission(expectedPermissions)) {
    return true;
  }

  return router.parseUrl('/unauthorized');
};
