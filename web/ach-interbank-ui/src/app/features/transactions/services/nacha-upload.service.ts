import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';

@Injectable({ providedIn: 'root' })
export class NachaUploadService {
  private readonly http = inject(HttpClient);
  private readonly api = inject(ApiService);

  upload(file: File): Observable<string> {
    const form = new FormData();
    form.append('file', file);

    return this.http.post(this.api.resolveUrl('NachaUpload/upload'), form, { responseType: 'text' });
  }
}
