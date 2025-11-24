import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpRequest
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { TokenStorageService } from './token-storage.service';
import { Router } from '@angular/router';
import { AuthService } from '../core/services/auth.service';

export function authInterceptor(req: HttpRequest<unknown>, next: HttpHandlerFn): Observable<HttpEvent<unknown>> {
  const tokenService = inject(TokenStorageService);
  const authService = inject(AuthService);
  const router = inject(Router);
  const token = tokenService.getAccessToken();

  const secureReq = token
    ? req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        },
        withCredentials: true
      })
    : req.clone({ withCredentials: true });

  return next(secureReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        authService.logout();
        router.navigate(['/login']);
      }
      return throwError(() => error);
    })
  );
}
