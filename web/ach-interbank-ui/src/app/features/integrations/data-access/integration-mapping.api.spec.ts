import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { IntegrationMappingApi } from './integration-mapping.api';

describe('IntegrationMappingApi', () => {
  let api: jasmine.SpyObj<ApiService>;
  let service: IntegrationMappingApi;

  beforeEach(() => {
    api = jasmine.createSpyObj<ApiService>('ApiService', ['get', 'post', 'put']);
    api.get.and.returnValue(of([]));
    api.post.and.returnValue(of({} as any));
    api.put.and.returnValue(of({} as any));

    TestBed.configureTestingModule({
      providers: [IntegrationMappingApi, { provide: ApiService, useValue: api }]
    });
    service = TestBed.inject(IntegrationMappingApi);
  });

  it('consulta mapping sets filtrados por método', () => {
    service.getByMethod(4).subscribe();

    expect(api.get).toHaveBeenCalledOnceWith('api/integrations/mappingsets?methodId=4');
  });

  it('consulta el read model de relación de información por mapping set', () => {
    service.getWorkspace('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa').subscribe();

    expect(api.get).toHaveBeenCalledOnceWith(
      'api/integrations/mappingsets/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/workspace'
    );
  });

  it('actualiza un parámetro con Revision esperada y sin aceptar field paths en el contrato guiado', () => {
    const request = {
      expectedRevision: 14,
      sourceCatalogFieldId: 27,
      transformationCode: 'Trim',
      formatMask: null,
      updatedBy: 'qa-user'
    };

    service.updateParameterMapping('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 91, request).subscribe();

    expect(api.put).toHaveBeenCalledOnceWith(
      'api/integrations/mappingsets/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/parameters/91',
      request
    );
    expect((request as unknown as Record<string, unknown>)['sourceFieldPath']).toBeUndefined();
  });

  it('solicita preview backend sin enviar valores sensibles ni reconstruir campos en frontend', () => {
    const request = {
      sampleTransactionId: 1001,
      sampleCycleId: null,
      maxItems: 12,
      useControlledSample: false
    };

    service.preview('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', request).subscribe();

    expect(api.post).toHaveBeenCalledOnceWith(
      'api/integrations/mappingsets/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/preview',
      request
    );
    const payload = api.post.calls.mostRecent().args[1] as Record<string, unknown>;
    expect(payload['sourceAccountNumber']).toBeUndefined();
    expect(payload['previewValue']).toBeUndefined();
    expect(payload['rawValue']).toBeUndefined();
  });

  it('publica con Revision esperada explícita sin inferir elegibilidad en el cliente', () => {
    const request = { expectedRevision: 14, publishedBy: 'qa-user', publishNote: null };

    service.publishWithRevision('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', request).subscribe();

    expect(api.post).toHaveBeenCalledOnceWith(
      'api/integrations/mappingsets/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/publish',
      request
    );
  });

  it('delega la validación contract+mapping con control optimista en backend', () => {
    const request = {
      expectedMappingRevision: 12,
      expectedContractRevision: 7,
      includeWarnings: false
    };

    service.validateContractMapping('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', request).subscribe();

    expect(api.post).toHaveBeenCalledOnceWith(
      'api/integrations/mappingsets/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/validate-contract-mapping',
      request
    );
  });

  it('preserva la regla persistida al adaptarla a un request de upsert', () => {
    const request = service.toRuleRequest({
      id: 17,
      mappingSetId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      methodId: 4,
      parameterId: 91,
      sourceKind: 'Transaction',
      sourceCatalogFieldId: 8,
      sourceFieldPath: 'transaction.reference',
      fixedValue: null,
      defaultValue: null,
      transformationCode: 'Trim',
      formatMask: null,
      priority: 1,
      requiredOverride: null,
      enabled: true,
      conditionExpression: null
    });

    expect(request).toEqual({
      id: 17,
      methodId: 4,
      parameterId: 91,
      sourceKind: 'Transaction',
      sourceCatalogFieldId: 8,
      sourceFieldPath: 'transaction.reference',
      fixedValue: null,
      defaultValue: null,
      transformationCode: 'Trim',
      formatMask: null,
      priority: 1,
      requiredOverride: null,
      enabled: true,
      conditionExpression: null
    });
  });
});
