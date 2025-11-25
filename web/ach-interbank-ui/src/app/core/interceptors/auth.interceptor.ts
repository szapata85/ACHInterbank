import { Injectable, inject } from '@angular/core';
import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest
} from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { TokenStorageService } from '../../security/token-storage.service';
import { AuthService } from '../services/auth.service';
import { environment } from '../../../environments/environment';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private readonly tokenService = inject(TokenStorageService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly apiBase = environment.apiBaseUrl;

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const token = this.tokenService.getAccessToken();
    const shouldAttach = this.isApiRequest(req.url);

    const secureReq = token && shouldAttach
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

  private isApiRequest(url: string): boolean {
    if (/^https?:\/\//i.test(url)) {
      return url.startsWith(this.apiBase);
    }
    return true;
  }
}
