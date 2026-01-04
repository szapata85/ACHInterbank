import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class DigitalEnvelopeService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl.replace(/\/+$/, '');

  encrypt(file: File): Observable<HttpResponse<Blob>> {
    const form = new FormData();
    form.append('file', file);

    return this.http.post(`${this.apiBaseUrl}/SobreDigital/encrypt`, form, {
      observe: 'response',
      responseType: 'blob'
    });
  }

  decrypt(file: File): Observable<HttpResponse<Blob>> {
    const form = new FormData();
    form.append('file', file);

    return this.http.post(`${this.apiBaseUrl}/SobreDigital/decrypt`, form, {
      observe: 'response',
      responseType: 'blob'
    });
  }
}
