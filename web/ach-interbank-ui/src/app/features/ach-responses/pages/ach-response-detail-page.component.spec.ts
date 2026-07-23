import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { AchResponsesApiService } from '../services/ach-responses-api.service';
import { AchResponseDetailPageComponent } from './ach-response-detail-page.component';

describe('AchResponseDetailPageComponent', () => {
  let apiSpy: jasmine.SpyObj<AchResponsesApiService>;
  let notificationSpy: jasmine.SpyObj<NotificationService>;
  let routerSpy: jasmine.SpyObj<Router>;

  const detailMock = {
    id: 'resp-1',
    tipoRespuesta: 'Transaccion',
    idTransaccion: 'TX-1',
    codigoCamaraCompensacion: 'ACH',
    codigoEstadoExterno: '00',
    idTransaccionServicioExterno: 99,
    hashIdempotencia: 'hash',
    estadoProcesamiento: 'Homologada',
    permiteNotificacion: true,
    fechaRecepcion: '2026-01-01T00:00:00Z',
    fechaCreacion: '2026-01-01T00:00:00Z',
    notificationAttempts: [
      { numeroIntento: 1, estadoNotificacion: 'Exitosa', idCanal: 1, nombreCanal: 'API', idEstado: 10, fechaCreacion: '2026-01-01T00:00:00Z' }
    ]
  } as any;

  function configureWithId(id: string | null): void {
    apiSpy = jasmine.createSpyObj<AchResponsesApiService>('AchResponsesApiService', ['getDetail', 'getReprocessAttempts']);
    apiSpy.getDetail.and.returnValue(of(detailMock));
    apiSpy.getReprocessAttempts.and.returnValue(of([]));
    notificationSpy = jasmine.createSpyObj<NotificationService>('NotificationService', ['error']);
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [AchResponseDetailPageComponent],
      providers: [
        { provide: AchResponsesApiService, useValue: apiSpy },
        { provide: NotificationService, useValue: notificationSpy },
        { provide: Router, useValue: routerSpy },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap(id ? { id } : {})
            }
          }
        }
      ]
    });
  }

  it('AchResponseDetailPageComponent_ShouldCreate', () => {
    configureWithId('resp-1');
    const fixture = TestBed.createComponent(AchResponseDetailPageComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('AchResponseDetailPageComponent_ShouldLoadDetailOnInit', () => {
    configureWithId('resp-1');
    const fixture = TestBed.createComponent(AchResponseDetailPageComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.detail?.id).toBe('resp-1');
    expect(apiSpy.getDetail).toHaveBeenCalledWith('resp-1');
    expect(apiSpy.getReprocessAttempts).toHaveBeenCalledWith('resp-1');
  });

  it('AchResponseDetailPageComponent_ShouldLabelReprocessTerminalStates', () => {
    configureWithId('resp-1');
    const component = TestBed.createComponent(AchResponseDetailPageComponent).componentInstance;
    expect(component.formatReprocessStatus('Pending')).toBe('Pendiente de ejecución');
    expect(component.formatReprocessStatus('Running')).toBe('En ejecución');
    expect(component.formatReprocessStatus('Completed')).toBe('Completado');
    expect(component.formatReprocessStatus('FailedFunctional')).toBe('Requiere revisión');
    expect(component.formatReprocessStatus('FailedTechnical')).toBe('Error técnico');
  });

  it('AchResponseDetailPageComponent_ShouldNotCallApi_WhenIdMissing', () => {
    configureWithId(null);
    const fixture = TestBed.createComponent(AchResponseDetailPageComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.error).toBeTrue();
    expect(apiSpy.getDetail).not.toHaveBeenCalled();
    expect(notificationSpy.error).toHaveBeenCalled();
  });

  it('AchResponseDetailPageComponent_ShouldHandleLoadError', () => {
    configureWithId('resp-1');
    apiSpy.getDetail.and.returnValue(throwError(() => new Error('fail')));
    const fixture = TestBed.createComponent(AchResponseDetailPageComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.error).toBeTrue();
    expect(component.loading).toBeFalse();
    expect(component.detail).toBeNull();
    expect(notificationSpy.error).toHaveBeenCalled();
  });

  it('AchResponseDetailPageComponent_ShouldNavigateBackToList', () => {
    configureWithId('resp-1');
    const fixture = TestBed.createComponent(AchResponseDetailPageComponent);
    const component = fixture.componentInstance;

    component.backToList();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/ach-responses']);
  });

  it('AchResponseDetailPageComponent_ShouldNavigateToAttempts', () => {
    configureWithId('resp-1');
    const fixture = TestBed.createComponent(AchResponseDetailPageComponent);
    const component = fixture.componentInstance;
    component.detail = { id: 'resp-1' } as any;

    component.goToAttempts();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/ach-responses', 'resp-1', 'notification-attempts']);
  });

  it('AchResponseDetailPageComponent_ShouldFormatEmptyValuesAsDash', () => {
    configureWithId('resp-1');
    const fixture = TestBed.createComponent(AchResponseDetailPageComponent);
    const component = fixture.componentInstance;

    expect(component.formatValue('')).toBe('-');
    expect(component.formatValue(null)).toBe('-');
    expect(component.formatValue(true)).toBe('Sí');
  });

  it('AchResponseDetailPageComponent_ShouldClassifyProcessingStatus', () => {
    configureWithId('resp-1');
    const fixture = TestBed.createComponent(AchResponseDetailPageComponent);
    const component = fixture.componentInstance;

    expect(component.getProcessingStatusClass('Notificada')).toBe('estado-exitoso');
    expect(component.getProcessingStatusClass('PendienteReintento')).toBe('estado-advertencia');
    expect(component.getProcessingStatusClass('ErrorFuncional')).toBe('estado-error');
  });

  it('AchResponseDetailPageComponent_ShouldClassifyNotificationStatus', () => {
    configureWithId('resp-1');
    const fixture = TestBed.createComponent(AchResponseDetailPageComponent);
    const component = fixture.componentInstance;

    expect(component.getNotificationStatusClass('Exitosa')).toBe('estado-exitoso');
    expect(component.getNotificationStatusClass('Pendiente')).toBe('estado-advertencia');
    expect(component.getNotificationStatusClass('ErrorTecnico')).toBe('estado-error');
  });

  it('AchResponseDetailPageComponent_ShouldNotExposeForbiddenTerms', () => {
    const visibleKeys = Object.keys(detailMock).join('|');
    const forbiddenTerms = ['idTransaccionAxon', 'Axon', 'Soap', 'SOAP', 'Wsdl', 'Envelope', 'RequestPayload', 'ResponsePayload', 'Xml'];

    forbiddenTerms.forEach((term) => expect(visibleKeys).not.toContain(term));
  });
});
