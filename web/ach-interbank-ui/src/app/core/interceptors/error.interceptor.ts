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
import { AuthService } from '../services/auth.service';
import { NotificationService } from '../services/notification.service';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          this.authService.logout();
          this.notifications.info('Tu sesión expiró, por favor inicia sesión nuevamente.');
          this.router.navigate(['/auth/login'], {
            queryParams: { returnUrl: this.router.url }
          });
        } else if (error.status === 403) {
          this.notifications.warning('No tienes permisos para acceder a esta opción.');
          this.router.navigate(['/unauthorized']);
        } else if (error.status >= 500 || error.status === 0) {
          this.notifications.error('Ocurrió un problema procesando la solicitud. Intenta más tarde.');
        } else if (error.status === 404) {
          this.notifications.info('El recurso solicitado no existe o ya no está disponible.');
          this.router.navigate(['/not-found']);
        }

        return throwError(() => error);
      })
    );
  }
}
