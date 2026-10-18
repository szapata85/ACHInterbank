import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { IntegrationConnectionApi } from '../data-access/integration-connection.api';
import { IntegrationContractApi } from '../data-access/integration-contract.api';
import { IntegrationMappingApi } from '../data-access/integration-mapping.api';
import {
  IntegrationConnectionMethodDto,
  IntegrationContractVersionDto,
  IntegrationMappingSetDto,
  IntegrationMethodDto
} from '../data-access/integration-api.models';
import { IntegrationWorkspaceApi } from '../data-access/integration-workspace.api';
import { IntegrationCenterStore } from './integration-center.store';

function method(
  id: number,
  operationKey: string,
  displayName: string,
  mappingPurpose: string
): IntegrationMethodDto {
  return {
    id,
    code: operationKey.toUpperCase(),
    displayName,
    soapClientCode: operationKey === 'RegistrarRespuestaTransaccion' ? 'WSAXON' : 'WSCFAACH',
    isActive: true,
    integrationKey: 'CFA',
    operationKey,
    mappingDirection: 'Inbound',
    mappingPurpose,
    functionalNature: 'Service',
    functionalOriginator: 'CFA',
    movesMoney: operationKey !== 'RegistrarRespuestaTransaccion'
  };
}

function connectionMethod(
  methodName: string,
  endpoint: string,
  soapAction: string,
  operatingMode: IntegrationConnectionMethodDto['operatingMode'],
  enabled: boolean
): IntegrationConnectionMethodDto {
  return {
    methodName,
    endpoint,
    soapAction,
    operatingMode,
    enabled,
    sanitizedEndpoint: endpoint,
    sanitizedSoapAction: soapAction,
    configurationState: !enabled || operatingMode === 'Disabled'
      ? 'Disabled'
      : operatingMode === 'Live' ? 'RealSendEnabled' : 'Simulation',
    inputParameterMappings: []
  };
}

describe('IntegrationCenterStore', () => {
  let workspaceApi: jasmine.SpyObj<IntegrationWorkspaceApi>;
  let contractApi: jasmine.SpyObj<IntegrationContractApi>;
  let mappingApi: jasmine.SpyObj<IntegrationMappingApi>;
  let connectionApi: jasmine.SpyObj<IntegrationConnectionApi>;
  const user$ = new BehaviorSubject<any>({ permissions: ['CanReadAch'] });
  const authStub = {
    user$: user$.asObservable(),
    getCurrentUser: () => user$.value
  };

  const credit = method(1, 'Proc_Transacciones', 'Créditos recibidos en CFA', 'Procesa créditos recibidos en CFA.');
  const debit = method(2, 'Proc_Contrapartidas', 'Débitos originados en CFA', 'Procesa débitos originados en CFA.');
  const response = method(3, 'RegistrarRespuestaTransaccion', 'Respuestas de transacciones', 'Registra respuestas de transacciones.');

  beforeEach(() => {
    user$.next({ permissions: ['CanReadAch'] });
    workspaceApi = jasmine.createSpyObj<IntegrationWorkspaceApi>('IntegrationWorkspaceApi', ['getMethods']);
    contractApi = jasmine.createSpyObj<IntegrationContractApi>('IntegrationContractApi', ['getByMethod']);
    mappingApi = jasmine.createSpyObj<IntegrationMappingApi>('IntegrationMappingApi', ['getPublished', 'validate']);
    connectionApi = jasmine.createSpyObj<IntegrationConnectionApi>('IntegrationConnectionApi', ['getSettings']);

    connectionApi.getSettings.and.returnValue(of({
      revision: 'rev-test',
      wscfaachMappings: [
        connectionMethod('Proc_Transacciones', 'https://local.test/credit', 'urn:credit', 'Live', true),
        connectionMethod('Proc_Contrapartidas', 'https://local.test/debit', 'urn:debit', 'DryRun', true)
      ],
      wsAxonRespuestaTransaccionesMappings: [
        connectionMethod('RegistrarRespuestaTransaccion', 'https://local.test/response', 'urn:response', 'Live', false)
      ]
    }));
    contractApi.getByMethod.and.callFake((methodId: number) => of([activeContract(methodId)]));
    mappingApi.getPublished.and.callFake((methodId: number) => of(publishedMapping(methodId)));
    mappingApi.validate.and.returnValue(of({
      mappingSetId: 'mapping-1',
      isValid: true,
      issues: [],
      coverage: {
        totalParameters: 10,
        validParameters: 9,
        incompleteParameters: 1,
        invalidParameters: 0,
        inactiveParameters: 0,
        coveredByDefaultOrFixed: 2,
        coveredBySourceField: 7
      },
      parameters: []
    }));

    TestBed.configureTestingModule({
      providers: [
        IntegrationCenterStore,
        { provide: IntegrationWorkspaceApi, useValue: workspaceApi },
        { provide: IntegrationContractApi, useValue: contractApi },
        { provide: IntegrationMappingApi, useValue: mappingApi },
        { provide: IntegrationConnectionApi, useValue: connectionApi },
        { provide: AuthService, useValue: authStub }
      ]
    });
  });

  it('carga las tres tarjetas CFA en orden y proyecta solo estados recibidos del backend', () => {
    workspaceApi.getMethods.and.returnValue(of([response, debit, credit]));
    const store = TestBed.inject(IntegrationCenterStore);

    store.load();

    expect(store.status()).toBe('ready');
    expect(store.services().map((item) => item.technicalMethodName)).toEqual([
      'Proc_Transacciones',
      'Proc_Contrapartidas',
      'RegistrarRespuestaTransaccion'
    ]);
    expect(store.services()[0].contractStatus).toBe('Active');
    expect(store.services()[0].mappingStatus).toBe('Published');
    expect(store.services()[0].mappingCoverage?.validParameters).toBe(9);
    expect(store.services()[0].connectionStatus).toBe('Live');
    expect(store.services()[2].connectionStatus).toBe('Disabled');
    expect(store.services()[0].canPublish).toBeNull();
  });

  it('mantiene permisos de presentación sincronizados con la sesión sin convertirlos en capacidades contractuales', () => {
    workspaceApi.getMethods.and.returnValue(of([]));
    const store = TestBed.inject(IntegrationCenterStore);

    expect(store.permissions()).toEqual({ canRead: true, canManage: false });
    user$.next({ permissions: ['CanReadAch', 'CanManageAch'] });

    expect(store.permissions()).toEqual({ canRead: true, canManage: true });
  });

  it('acepta Integrations.Read como alternativa semántica de lectura del centro', () => {
    user$.next({ permissions: ['Integrations.Read'] });
    workspaceApi.getMethods.and.returnValue(of([]));
    const store = TestBed.inject(IntegrationCenterStore);

    store.load();

    expect(store.permissions()).toEqual({ canRead: true, canManage: false });
    expect(workspaceApi.getMethods).toHaveBeenCalled();
  });

  it('expone forbidden sin llamar a APIs cuando la sesión no concede lectura de integraciones', () => {
    user$.next({ permissions: [] });
    const store = TestBed.inject(IntegrationCenterStore);

    store.load();

    expect(store.status()).toBe('forbidden');
    expect(store.error()?.kind).toBe('forbidden');
    expect(workspaceApi.getMethods).not.toHaveBeenCalled();
  });

  it('expone error reintentable ante falla no opcional del backend', () => {
    workspaceApi.getMethods.and.returnValue(throwError(() => new HttpErrorResponse({ status: 500, statusText: 'Server Error' })));
    const store = TestBed.inject(IntegrationCenterStore);

    store.load();

    expect(store.status()).toBe('error');
    expect(store.error()?.status).toBe(500);
    expect(store.services()).toEqual([]);
  });
});

function activeContract(methodId: number): IntegrationContractVersionDto {
  return {
    id: `contract-${methodId}`,
    methodId,
    versionLabel: 'v2',
    status: 'Active',
    operationName: 'operation',
    targetNamespace: 'urn:test',
    soapAction: 'urn:test/action',
    sourceType: 'WsdlImport',
    revision: 2
  };
}

function publishedMapping(methodId: number): IntegrationMappingSetDto {
  return {
    id: `mapping-${methodId}`,
    methodId,
    methodCode: `METHOD_${methodId}`,
    contractVersionId: `contract-${methodId}`,
    revision: 3,
    name: `Configuración ${methodId}`,
    version: 2,
    status: 'Published',
    isActive: true,
    notes: '',
    publishedBy: 'backend',
    rules: []
  };
}
