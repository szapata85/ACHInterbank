import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
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

const DEFAULT_SETTINGS: SoapIntegrationSettings = {
  wscfaachMappings: [
    {
      methodName: 'PLValidarUsuarioBV',
      endpoint: 'http://esparta/WSCFAACH/WSCFAACH.svc',
      soapAction: 'http://tempuri.org/IWSCFAACH/PLValidarUsuarioBV',
      enabled: true,
      inputParameterMappings: [{ inputName: 'usuario', soapParameterName: 'Usuario', required: true }]
    },
    {
      methodName: 'Proc_Contrapartidas',
      endpoint: 'http://esparta/WSCFAACH/WSCFAACH.svc',
      soapAction: 'http://tempuri.org/IWSCFAACH/Proc_Contrapartidas',
      enabled: true,
      inputParameterMappings: [{ inputName: 'transaccion', soapParameterName: 'Transaccion', required: true }]
    },
    {
      methodName: 'Proc_Transacciones',
      endpoint: 'http://esparta/WSCFAACH/WSCFAACH.svc',
      soapAction: 'http://tempuri.org/IWSCFAACH/Proc_Transacciones',
      enabled: true,
      inputParameterMappings: [{ inputName: 'lote', soapParameterName: 'Lote', required: true }]
    }
  ],
  wsAxonRespuestaTransaccionesMappings: [
    {
      methodName: 'RegistrarRespuestaTransaccion',
      endpoint: 'http://esparta/WSCFAACH/WSAxonRespuestaTransacciones.svc',
      soapAction: 'http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion',
      enabled: true,
      inputParameterMappings: [{ inputName: 'respuesta', soapParameterName: 'Respuesta', required: true }]
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
