import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { IntegrationContractApi } from './integration-contract.api';

describe('IntegrationContractApi', () => {
  let api: jasmine.SpyObj<ApiService>;
  let service: IntegrationContractApi;

  beforeEach(() => {
    api = jasmine.createSpyObj<ApiService>('ApiService', ['get', 'post', 'put']);
    api.get.and.returnValue(of([]));
    api.post.and.returnValue(of({} as any));
    api.put.and.returnValue(of({} as any));

    TestBed.configureTestingModule({
      providers: [IntegrationContractApi, { provide: ApiService, useValue: api }]
    });
    service = TestBed.inject(IntegrationContractApi);
  });

  it('consulta versiones contractuales por método sin reconstruir estado en cliente', () => {
    service.getByMethod(9).subscribe();

    expect(api.get).toHaveBeenCalledOnceWith('api/integrations/methods/9/contracts');
  });

  it('envía las revisiones esperadas exactamente al activar el par contrato/mapping', () => {
    const request = {
      mappingSetId: '22222222-2222-2222-2222-222222222222',
      expectedContractRevision: 5,
      expectedMappingRevision: 8,
      activatedBy: 'operator'
    };

    service.activate('11111111-1111-1111-1111-111111111111', request).subscribe();

    expect(api.post).toHaveBeenCalledOnceWith(
      'api/integrations/contracts/11111111-1111-1111-1111-111111111111/activate',
      request
    );
  });

  it('construye multipart para análisis local sin añadir actor ni reglas contractuales', () => {
    const file = new File(['<definitions/>'], 'service.wsdl', { type: 'text/xml' });

    service.analyze({ file, operationName: 'Proc_Transacciones' }).subscribe();

    const [, body] = api.post.calls.mostRecent().args as [string, FormData];
    expect(body.get('File')).toEqual(file);
    expect(body.get('OperationName')).toBe('Proc_Transacciones');
    expect(body.has('Actor')).toBeFalse();
  });
  it('envía metadata funcional y ExpectedRevision sin campos técnicos editables', () => {
    const request = {
      displayName: 'Tipo de cliente',
      descriptionEs: 'Descripción funcional',
      uiHelpText: 'Ayuda',
      category: 'Cliente',
      exampleValue: 'N',
      expectedRevision: 9,
      actor: 'operador'
    };

    service.updateParameterPresentation('11111111-1111-1111-1111-111111111111', 23, request).subscribe();

    expect(api.put).toHaveBeenCalledOnceWith(
      'api/integrations/contracts/11111111-1111-1111-1111-111111111111/parameters/23/presentation',
      request
    );
    expect((api.put.calls.mostRecent().args[1] as Record<string, unknown>)['xsdType']).toBeUndefined();
    expect((api.put.calls.mostRecent().args[1] as Record<string, unknown>)['parameterPath']).toBeUndefined();
  });

  it('envía preview multipart contra la versión base sin crear contrato', () => {
    const file = new File(['<definitions/>'], 'service.wsdl', { type: 'text/xml' });

    service.preview(9, '11111111-1111-1111-1111-111111111111', { file, operationName: 'Proc_Transacciones' }).subscribe();

    const [url, body] = api.post.calls.mostRecent().args as [string, FormData];
    expect(url).toBe('api/integrations/methods/9/contracts/preview/11111111-1111-1111-1111-111111111111');
    expect(body.get('File')).toEqual(file);
    expect(body.get('OperationName')).toBe('Proc_Transacciones');
    expect(body.has('VersionLabel')).toBeFalse();
  });

  it('importa con multipart y solo envía metadata de creación del borrador', () => {
    const file = new File(['<definitions/>'], 'service.wsdl', { type: 'text/xml' });

    service.import(9, {
      file,
      operationName: 'Proc_Transacciones',
      versionLabel: 'v2',
      notes: 'Nueva versión'
    }).subscribe();

    const [url, body] = api.post.calls.mostRecent().args as [string, FormData];
    expect(url).toBe('api/integrations/methods/9/contracts/import');
    expect(body.get('VersionLabel')).toBe('v2');
    expect(body.get('Notes')).toBe('Nueva versión');
    expect(body.has('Status')).toBeFalse();
    expect(body.has('Activate')).toBeFalse();
  });

});
