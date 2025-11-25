import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const expectedRoles = route.data?.['roles'] as string[] | undefined;

  if (!authService.isAuthenticated()) {
    authService.logout();
    return router.parseUrl('/login');
  }

  if (!expectedRoles || expectedRoles.length === 0 || authService.hasRole(expectedRoles)) {
    return true;
  }

  return router.parseUrl('/');
};
