import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { DigitalEnvelopeCertificate, DigitalEnvelopeCertificateType } from '../models/digital-envelope-certificate.model';

@Injectable({ providedIn: 'root' })
export class DigitalEnvelopeCertificatesService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'nacha-security/certificates';

  list(): Observable<DigitalEnvelopeCertificate[]> {
    return this.api.get<DigitalEnvelopeCertificate[]>(this.basePath).pipe(map((items) => items ?? []));
  }

  upload(type: DigitalEnvelopeCertificateType, file: File, password?: string): Observable<DigitalEnvelopeCertificate> {
    const form = new FormData();
    form.append('type', type);
    form.append('file', file);
    if (password) {
      form.append('password', password);
    }

    return this.api.post<DigitalEnvelopeCertificate>(this.basePath, form);
  }

  delete(id: number): Observable<void> {
    return this.api.delete<void>(`${this.basePath}/${id}`);
  }
}
