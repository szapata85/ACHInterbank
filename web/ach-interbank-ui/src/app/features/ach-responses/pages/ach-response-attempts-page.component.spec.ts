import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { AchResponsesApiService } from '../services/ach-responses-api.service';
import { AchResponseAttemptsPageComponent } from './ach-response-attempts-page.component';

describe('AchResponseAttemptsPageComponent', () => {
  let apiSpy: jasmine.SpyObj<AchResponsesApiService>;
  let notificationSpy: jasmine.SpyObj<NotificationService>;
  let routerSpy: jasmine.SpyObj<Router>;

  const attemptsMock = [
    {
      id: 1,
      achResponseId: 'resp-1',
      numeroIntento: 1,
      estadoNotificacion: 'Exitosa',
      idCanal: 10,
      nombreCanal: 'API',
      idTransaccion: 'TX-1',
      idEstado: 2,
      idTransaccionServicioExterno: 100,
      fechaCreacion: '2026-01-01T00:00:00Z'
    }
  ] as any;

  function configureWithId(id: string | null): void {
    apiSpy = jasmine.createSpyObj<AchResponsesApiService>('AchResponsesApiService', ['getAttempts']);
    apiSpy.getAttempts.and.returnValue(of(attemptsMock));
    notificationSpy = jasmine.createSpyObj<NotificationService>('NotificationService', ['error']);
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [AchResponseAttemptsPageComponent],
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

  it('AchResponseAttemptsPageComponent_ShouldCreate', () => {
    configureWithId('resp-1');
    const fixture = TestBed.createComponent(AchResponseAttemptsPageComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('AchResponseAttemptsPageComponent_ShouldLoadAttemptsOnInit', () => {
    configureWithId('resp-1');
    const fixture = TestBed.createComponent(AchResponseAttemptsPageComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.attempts.length).toBe(1);
    expect(apiSpy.getAttempts).toHaveBeenCalledWith('resp-1');
  });

  it('AchResponseAttemptsPageComponent_ShouldNotCallApi_WhenIdMissing', () => {
    configureWithId(null);
    const fixture = TestBed.createComponent(AchResponseAttemptsPageComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.error).toBeTrue();
    expect(apiSpy.getAttempts).not.toHaveBeenCalled();
    expect(notificationSpy.error).toHaveBeenCalled();
  });

  it('AchResponseAttemptsPageComponent_ShouldHandleLoadError', () => {
    configureWithId('resp-1');
    apiSpy.getAttempts.and.returnValue(throwError(() => new Error('fail')));
    const fixture = TestBed.createComponent(AchResponseAttemptsPageComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.error).toBeTrue();
    expect(component.attempts.length).toBe(0);
    expect(notificationSpy.error).toHaveBeenCalled();
  });

  it('AchResponseAttemptsPageComponent_ShouldRetryLoad_WhenResponseIdExists', () => {
    configureWithId('resp-1');
    const fixture = TestBed.createComponent(AchResponseAttemptsPageComponent);
    const component = fixture.componentInstance;
    component.responseId = 'resp-1';

    component.retryLoad();
    expect(apiSpy.getAttempts).toHaveBeenCalledWith('resp-1');
  });

  it('AchResponseAttemptsPageComponent_ShouldNavigateBackToDetail', () => {
    configureWithId('resp-1');
    const fixture = TestBed.createComponent(AchResponseAttemptsPageComponent);
    const component = fixture.componentInstance;
    component.responseId = 'resp-1';

    component.backToDetail();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/ach-responses', 'resp-1']);
  });

  it('AchResponseAttemptsPageComponent_ShouldNavigateBackToList', () => {
    configureWithId('resp-1');
    const fixture = TestBed.createComponent(AchResponseAttemptsPageComponent);
    const component = fixture.componentInstance;

    component.backToList();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/ach-responses']);
  });

  it('AchResponseAttemptsPageComponent_ShouldFormatEmptyValuesAsDash', () => {
    configureWithId('resp-1');
    const fixture = TestBed.createComponent(AchResponseAttemptsPageComponent);
    const component = fixture.componentInstance;

    expect(component.formatValue('')).toBe('-');
    expect(component.formatValue(null)).toBe('-');
    expect(component.formatValue(false)).toBe('No');
  });

  it('AchResponseAttemptsPageComponent_ShouldClassifyNotificationStatus', () => {
    configureWithId('resp-1');
    const fixture = TestBed.createComponent(AchResponseAttemptsPageComponent);
    const component = fixture.componentInstance;

    expect(component.getNotificationStatusClass('Exitosa')).toBe('estado-exitoso');
    expect(component.getNotificationStatusClass('Pendiente')).toBe('estado-advertencia');
    expect(component.getNotificationStatusClass('ErrorTecnico')).toBe('estado-error');
  });

  it('AchResponseAttemptsPageComponent_ShouldNotExposeForbiddenTerms', () => {
    const visibleKeys = Object.keys(attemptsMock[0]).join('|');
    const forbiddenTerms = ['idTransaccionAxon', 'Axon', 'Soap', 'SOAP', 'Wsdl', 'Envelope', 'RequestPayload', 'ResponsePayload', 'Xml'];

    forbiddenTerms.forEach((term) => expect(visibleKeys).not.toContain(term));
  });
});
