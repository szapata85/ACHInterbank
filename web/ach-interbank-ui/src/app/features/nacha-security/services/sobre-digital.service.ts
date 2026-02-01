import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';

@Injectable({ providedIn: 'root' })
export class SobreDigitalService {
  private readonly http = inject(HttpClient);
  private readonly api = inject(ApiService);

  encrypt(file: File): Observable<HttpResponse<Blob>> {
    const form = new FormData();
    form.append('file', file);

    return this.http.post(this.api.resolveUrl('SobreDigital/encrypt'), form, {
      observe: 'response',
      responseType: 'blob'
    });
  }

  decrypt(file: File): Observable<HttpResponse<Blob>> {
    const form = new FormData();
    form.append('file', file);

    return this.http.post(this.api.resolveUrl('SobreDigital/decrypt'), form, {
      observe: 'response',
      responseType: 'blob'
    });
  }
}
