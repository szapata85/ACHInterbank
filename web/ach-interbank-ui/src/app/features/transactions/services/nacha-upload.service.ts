import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class NachaUploadService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl.replace(/\/+$/, '');

  upload(file: File): Observable<string> {
    const form = new FormData();
    form.append('file', file);

    return this.http.post(`${this.apiBaseUrl}/NachaUpload/upload`, form, { responseType: 'text' });
  }
}
