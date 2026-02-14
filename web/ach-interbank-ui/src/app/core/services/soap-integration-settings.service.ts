import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { ApiService } from './api.service';

export interface SoapEndpointMethodMapping {
  methodName: string;
  endpoint: string;
  soapAction: string;
  enabled: boolean;
}

export interface SoapIntegrationSettings {
  wscfaachMappings: SoapEndpointMethodMapping[];
  wsAxonRespuestaTransaccionesMappings: SoapEndpointMethodMapping[];
}

const DEFAULT_SETTINGS: SoapIntegrationSettings = {
  wscfaachMappings: [
    {
      methodName: 'PLValidarUsuarioBV',
      endpoint: '',
      soapAction: 'http://tempuri.org/IWSCFAACH/PLValidarUsuarioBV',
      enabled: true
    },
    {
      methodName: 'Proc_Contrapartidas',
      endpoint: '',
      soapAction: 'http://tempuri.org/IWSCFAACH/Proc_Contrapartidas',
      enabled: true
    },
    {
      methodName: 'Proc_Transacciones',
      endpoint: '',
      soapAction: 'http://tempuri.org/IWSCFAACH/Proc_Transacciones',
      enabled: true
    }
  ],
  wsAxonRespuestaTransaccionesMappings: [
    {
      methodName: 'RegistrarRespuestaTransaccion',
      endpoint: '',
      soapAction: 'http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion',
      enabled: true
    }
  ]
};

@Injectable({ providedIn: 'root' })
export class SoapIntegrationSettingsService {
  private readonly api = inject(ApiService);
  private readonly settingsSubject = new BehaviorSubject<SoapIntegrationSettings>(DEFAULT_SETTINGS);
  readonly settings$ = this.settingsSubject.asObservable();

  constructor() {
    this.refreshFromServer().subscribe();
  }

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
