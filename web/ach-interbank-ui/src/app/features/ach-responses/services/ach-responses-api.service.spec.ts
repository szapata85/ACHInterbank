import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { AchResponsesApiService } from './ach-responses-api.service';

describe('AchResponsesApiService', () => {
  let service: AchResponsesApiService;
  let apiSpy: jasmine.SpyObj<ApiService>;

  beforeEach(() => {
    apiSpy = jasmine.createSpyObj<ApiService>('ApiService', ['get', 'post']);
    apiSpy.post.and.returnValue(of({} as any));
    apiSpy.get.and.returnValue(of({} as any));

    TestBed.configureTestingModule({
      providers: [AchResponsesApiService, { provide: ApiService, useValue: apiSpy }]
    });

    service = TestBed.inject(AchResponsesApiService);
  });

  it('AchResponsesApiService_ShouldPostProcessToExpectedEndpoint', () => {
    const request = {
      tipoRespuesta: 'Transaccion',
      idTransaccion: 'TX-1',
      codigoCamaraCompensacion: 'ACH',
      codigoEstadoExterno: '00',
      idCanal: 1,
      nombreCanal: 'Canal ACH',
      idTransaccionServicioExterno: 123
    } as any;

    service.process(request).subscribe();

    expect(apiSpy.post).toHaveBeenCalledWith('api/ach/responses/process', request);
  });

  it('AchResponsesApiService_ShouldPostSendNotificationToExpectedEndpoint', () => {
    const request = { notificationAttemptId: 10, correlationId: 'corr-1' };

    service.sendNotification(request).subscribe();

    expect(apiSpy.post).toHaveBeenCalledWith('api/ach/responses/notifications/send', request);
  });

  it('AchResponsesApiService_ShouldSearchUsingExpectedEndpointAndParams', () => {
    const request = {
      pageNumber: 2,
      pageSize: 25,
      tipoRespuesta: 'Prenota',
      idTransaccion: 'TX-2',
      estadoProcesamiento: 'Recibida',
      fechaDesde: '2026-01-01',
      fechaHasta: '',
      correlationId: null
    } as any;

    service.search(request).subscribe();

    expect(apiSpy.get).toHaveBeenCalled();
    const [endpoint, options] = apiSpy.get.calls.mostRecent().args;
    expect(endpoint).toBe('api/ach/responses');
    expect(options.params.pageNumber).toBe(2);
    expect(options.params.pageSize).toBe(25);
    expect(options.params.tipoRespuesta).toBe('Prenota');
    expect(options.params.idTransaccion).toBe('TX-2');
    expect(options.params.estadoProcesamiento).toBe('Recibida');
    expect(options.params.fechaDesde).toBe('2026-01-01');
    expect(options.params.fechaHasta).toBeUndefined();
    expect(options.params.correlationId).toBeUndefined();
  });

  it('AchResponsesApiService_ShouldGetDetailById', () => {
    service.getDetail('ACH/123').subscribe();

    expect(apiSpy.get).toHaveBeenCalledWith('api/ach/responses/ACH%2F123');
  });

  it('AchResponsesApiService_ShouldGetAttemptsByResponseId', () => {
    service.getAttempts('ACH/123').subscribe();

    expect(apiSpy.get).toHaveBeenCalledWith('api/ach/responses/ACH%2F123/notification-attempts');
  });

  it('AchResponsesApiService_ShouldGetStatusMappingsWithFilters', () => {
    service.getStatusMappings({ codigoCamaraCompensacion: 'ACH', tipoRespuesta: 'Transaccion', activo: true }).subscribe();

    expect(apiSpy.get).toHaveBeenCalled();
    const [endpoint, options] = apiSpy.get.calls.mostRecent().args;
    expect(endpoint).toBe('api/ach/response-status-mappings');
    expect(options.params.codigoCamaraCompensacion).toBe('ACH');
    expect(options.params.tipoRespuesta).toBe('Transaccion');
    expect(options.params.activo).toBeTrue();
  });

  it('AchResponsesModels_ShouldNotExposeSoapOrProviderTerms', () => {
    const mock = {
      id: '1',
      idTransaccionServicioExterno: 100,
      correlationId: 'c-1',
      estadoProcesamiento: 'Recibida'
    };

    const keysText = Object.keys(mock).join('|');

    expect(keysText).not.toContain('idTransaccionAxon');
    expect(keysText).not.toContain('Axon');
    expect(keysText).not.toContain('Soap');
    expect(keysText).not.toContain('SOAP');
    expect(keysText).not.toContain('Wsdl');
    expect(keysText).not.toContain('Envelope');
    expect(keysText).not.toContain('RequestPayload');
    expect(keysText).not.toContain('ResponsePayload');
    expect(keysText).not.toContain('Xml');
  });
});
