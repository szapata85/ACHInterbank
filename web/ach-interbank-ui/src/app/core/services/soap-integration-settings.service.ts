import { Injectable, inject } from '@angular/core';
import { Observable, ReplaySubject, tap } from 'rxjs';
import { ApiService } from './api.service';

export interface SoapInputParameterMapping {
  inputName: string;
  soapParameterName: string;
  defaultValue?: string | null;
  required: boolean;
}

export interface SoapEndpointMethodMapping {
  methodName: string;
  endpoint: string;
  soapAction: string;
  enabled: boolean;
  inputParameterMappings: SoapInputParameterMapping[];
}

export interface SoapIntegrationSettings {
  wscfaachMappings: SoapEndpointMethodMapping[];
  wsAxonRespuestaTransaccionesMappings: SoapEndpointMethodMapping[];
}

@Injectable({ providedIn: 'root' })
export class SoapIntegrationSettingsService {
  private readonly api = inject(ApiService);
  private readonly settingsSubject = new ReplaySubject<SoapIntegrationSettings>(1);
  readonly settings$ = this.settingsSubject.asObservable();

  updateSettings(settings: SoapIntegrationSettings): Observable<SoapIntegrationSettings> {
    return this.api.put<SoapIntegrationSettings>('api/users/soap-integrations', settings).pipe(
      tap((response) => this.settingsSubject.next(response))
    );
  }

  refreshFromServer(): Observable<SoapIntegrationSettings> {
    return this.api.get<SoapIntegrationSettings>('api/users/soap-integrations').pipe(
      tap((response) => this.settingsSubject.next(response))
    );
  }
}
