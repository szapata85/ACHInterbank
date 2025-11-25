import { Injectable, inject } from '@angular/core';
import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { LoadingService } from '../services/loading.service';

/**
 * Interceptor que muestra un overlay de carga global mientras existan peticiones en vuelo.
 * Se puede omitir agregando el header `X-Skip-Loading: true` en la solicitud.
 */
@Injectable()
export class LoadingInterceptor implements HttpInterceptor {
  private readonly loading = inject(LoadingService);

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const skip = req.headers.get('X-Skip-Loading');
    if (!skip) {
      this.loading.show();
    }

    return next.handle(req).pipe(
      finalize(() => {
        if (!skip) {
          this.loading.hide();
        }
      })
    );
  }
}
