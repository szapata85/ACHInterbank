import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  CenitFileRejectionCode,
  CenitPrenotificationPolicy,
  CenitReturnCode,
  CenitReturnOfReturnPolicy,
  CenitReturnPolicy,
  CenitTransactionTypePolicy
} from '../models/cenit.models';

@Injectable({ providedIn: 'root' })
export class CenitRegulatoryApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'api/regulatory-catalogs';

  getReturnCodes(): Observable<CenitReturnCode[]> {
    return this.api.get<CenitReturnCode[]>(`${this.basePath}/return-codes`);
  }

  getFileRejectionCodes(): Observable<CenitFileRejectionCode[]> {
    return this.api.get<CenitFileRejectionCode[]>(`${this.basePath}/file-rejection-codes`);
  }

  getTransactionTypePolicies(): Observable<CenitTransactionTypePolicy[]> {
    return this.api.get<CenitTransactionTypePolicy[]>(`${this.basePath}/transaction-type-policies`);
  }

  getReturnPolicies(): Observable<CenitReturnPolicy[]> {
    return this.api.get<CenitReturnPolicy[]>(`${this.basePath}/return-policies`);
  }

  getReturnOfReturnPolicies(): Observable<CenitReturnOfReturnPolicy[]> {
    return this.api.get<CenitReturnOfReturnPolicy[]>(`${this.basePath}/return-of-return-policies`);
  }

  getPrenotificationPolicies(): Observable<CenitPrenotificationPolicy[]> {
    return this.api.get<CenitPrenotificationPolicy[]>(`${this.basePath}/prenotification-policies`);
  }
}
