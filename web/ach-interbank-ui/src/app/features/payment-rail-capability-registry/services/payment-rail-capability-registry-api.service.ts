import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { PaymentRailCapabilityItem, PaymentRailItem } from '../models/payment-rail-capability-registry.models';

@Injectable({ providedIn: 'root' })
export class PaymentRailCapabilityRegistryApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'api/payment-rails/capability-registry';

  getRails(): Observable<PaymentRailItem[]> {
    return this.api.get<PaymentRailItem[]>(`${this.basePath}/rails`);
  }

  getCapabilitiesByRail(railCode: string, asOfUtc?: string): Observable<PaymentRailCapabilityItem[]> {
    const options = asOfUtc ? { params: { asOfUtc } } : undefined;
    return this.api.get<PaymentRailCapabilityItem[]>(`${this.basePath}/rails/${encodeURIComponent(railCode)}/capabilities`, options);
  }

  getCapabilityByRail(railCode: string, capabilityCode: string, asOfUtc?: string): Observable<PaymentRailCapabilityItem> {
    const options = asOfUtc ? { params: { asOfUtc } } : undefined;
    return this.api.get<PaymentRailCapabilityItem>(
      `${this.basePath}/rails/${encodeURIComponent(railCode)}/capabilities/${encodeURIComponent(capabilityCode)}`,
      options
    );
  }
}
