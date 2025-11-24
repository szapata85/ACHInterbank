import { Injectable } from '@angular/core';
import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest
} from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { TokenStorageService } from './token-storage.service';
import { AuthService } from '../core/services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(
    private readonly tokenService: TokenStorageService,
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const token = this.tokenService.getAccessToken();
    const secureReq = token
      ? req.clone({
          setHeaders: {
            Authorization: `Bearer ${token}`
          },
          withCredentials: true
        })
      : req.clone({ withCredentials: true });

    return next.handle(secureReq).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          this.authService.logout();
          this.router.navigate(['/login']);
        }
        return throwError(() => error);
      })
    );
  }
}
