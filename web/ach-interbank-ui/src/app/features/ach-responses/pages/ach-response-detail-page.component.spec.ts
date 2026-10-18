import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { AchResponseAuditModel, AchResponseDetailResponse, AchResponseReprocessModel } from '../models/ach-responses.models';
import { AchResponsesApiService } from '../services/ach-responses-api.service';
import { AchResponseDetailPageComponent } from './ach-response-detail-page.component';

describe('AchResponseDetailPageComponent', () => {
  let apiSpy: jasmine.SpyObj<AchResponsesApiService>;
  let notificationSpy: jasmine.SpyObj<NotificationService>;
  let routerSpy: jasmine.SpyObj<Router>;

  const detailMock: AchResponseDetailResponse = {
    id: 'resp-1',
    tipoRespuesta: 'Transaccion',
    idTransaccion: 'TX-1',
    codigoCamaraCompensacion: 'ACH',
    codigoEntidadOrigen: '001',
    codigoEntidadDestino: '002',
    codigoEstadoExterno: '00',
    codigoCausalExterna: 'R01',
    idEstadoInterno: 10,
    idEstadoServicioExterno: 20,
    estadoInternoNombre: 'Aplicada',
    causalNormalizada: 'R01',
    descripcionCausal: 'Fondos insuficientes',
    idTransaccionServicioExterno: 99,
    hashIdempotencia: 'hash',
    estadoProcesamiento: 'Homologada',
    motivoNoHomologacion: null,
    permiteNotificacion: true,
    correlationId: 'corr-1',
    fechaRecepcion: '2026-01-01T00:00:00Z',
    fechaCreacion: '2026-01-01T00:00:00Z',
    fechaActualizacion: '2026-01-01T00:01:00Z',
    notificationAttempts: [
      {
        id: 1,
        achResponseId: 'resp-1',
        numeroIntento: 1,
        estadoNotificacion: 'Exitosa',
        idCanal: 1,
        nombreCanal: 'API',
        idTransaccion: 'TX-1',
        idEstado: 10,
        causal: 'R01',
        idTransaccionServicioExterno: 99,
        descripcionCausal: 'Fondos insuficientes',
        existeError: false,
        codigoError: null,
        descripcionError: null,
        errorTecnico: null,
        fechaCreacion: '2026-01-01T00:00:00Z',
        fechaEnvio: '2026-01-01T00:00:10Z'
      }
    ],
    clearingHouseId: 1,
    appliedMappingId: 2,
    duplicateReceiptCount: 0,
    version: 'version-7'
  };

  const reprocessAttemptMock: AchResponseReprocessModel = {
    id: 5,
    achResponseId: 'resp-1',
    attemptNumber: 1,
    status: 'Completed',
    requestedBy: 'operator',
    reason: 'Motivo válido',
    correlationId: 'corr-rp-1',
    requestedAtUtc: '2026-01-01T01:00:00Z',
    startedAtUtc: '2026-01-01T01:00:01Z',
    completedAtUtc: '2026-01-01T01:00:02Z',
    result: 'OK',
    commandId: 'cmd-1',
    claimedBy: 'worker-1',
    resultCode: 'OK',
    errorDetailSanitized: null
  };

  const auditMock: AchResponseAuditModel = {
    id: 7,
    entityType: 'AchResponse',
    entityId: 'resp-1',
    action: 'ReprocessRequested',
    previousState: 'ErrorTecnico',
    newState: 'PendienteReproceso',
    actor: 'operator',
    reason: 'Motivo válido',
    correlationId: 'corr-audit-1',
    occurredAtUtc: '2026-01-01T01:00:00Z',
    sanitizedMetadata: '{"commandId":"cmd-1"}'
  };

  function configureWithId(id: string | null): void {
    apiSpy = jasmine.createSpyObj<AchResponsesApiService>('AchResponsesApiService', [
      'getDetail',
      'getReprocessAttempts',
      'requestReprocess',
      'getResponseAudit'
    ]);
    apiSpy.getDetail.and.returnValue(of(detailMock));
    apiSpy.getReprocessAttempts.and.returnValue(of([reprocessAttemptMock]));
    apiSpy.requestReprocess.and.returnValue(of(reprocessAttemptMock));
    apiSpy.getResponseAudit.and.returnValue(of([auditMock]));

    notificationSpy = jasmine.createSpyObj<NotificationService>('NotificationService', [
      'error',
      'success',
      'warning'
    ]);
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

  it('AchResponseDetailPageComponent_ShouldLoadDetailAndReprocessAttemptsOnInit', () => {
    configureWithId('resp-1');
    const fixture = TestBed.createComponent(AchResponseDetailPageComponent);

    fixture.detectChanges();

    expect(fixture.componentInstance.detail?.id).toBe('resp-1');
    expect(fixture.componentInstance.reprocessAttempts).toEqual([reprocessAttemptMock]);
    expect(apiSpy.getDetail).toHaveBeenCalledWith('resp-1');
    expect(apiSpy.getReprocessAttempts).toHaveBeenCalledWith('resp-1');
  });

  it('AchResponseDetailPageComponent_ShouldNotCallApis_WhenIdMissing', () => {
    configureWithId(null);
    const fixture = TestBed.createComponent(AchResponseDetailPageComponent);

    fixture.detectChanges();

    expect(fixture.componentInstance.error).toBeTrue();
    expect(apiSpy.getDetail).not.toHaveBeenCalled();
    expect(apiSpy.getReprocessAttempts).not.toHaveBeenCalled();
    expect(notificationSpy.error).toHaveBeenCalledWith('No se encontró el identificador de la respuesta ACH');
  });

  it('AchResponseDetailPageComponent_ShouldNavigateBackToList', () => {
    configureWithId('resp-1');
    const component = TestBed.createComponent(AchResponseDetailPageComponent).componentInstance;

    component.backToList();

    expect(routerSpy.navigate).toHaveBeenCalledWith(['/ach-responses']);
  });

  it('AchResponseDetailPageComponent_ShouldNavigateToNotificationAttempts', () => {
    configureWithId('resp-1');
    const component = TestBed.createComponent(AchResponseDetailPageComponent).componentInstance;
    component.detail = detailMock;

    component.goToAttempts();

    expect(routerSpy.navigate).toHaveBeenCalledWith(['/ach-responses', 'resp-1', 'notification-attempts']);
  });

  it('AchResponseDetailPageComponent_ShouldPreserveExactAllowedReprocessStatuses', () => {
    configureWithId('resp-1');
    const component = TestBed.createComponent(AchResponseDetailPageComponent).componentInstance;
    const allowed = ['ErrorTecnico', 'PendienteReintento', 'NoHomologada', 'ErrorFuncional', 'Resuelta'];
    const denied = ['Recibida', 'Homologada', 'Notificada', 'RequiereRevisionManual', 'Duplicada', 'Cerrada'];

    allowed.forEach((status) => {
      component.detail = { ...detailMock, estadoProcesamiento: status };
      expect(component.canReprocess()).withContext(status).toBeTrue();
    });
    denied.forEach((status) => {
      component.detail = { ...detailMock, estadoProcesamiento: status };
      expect(component.canReprocess()).withContext(status).toBeFalse();
    });
  });

  it('AchResponseDetailPageComponent_ShouldNotRequestReprocess_WhenStatusIsNotAllowed', () => {
    configureWithId('resp-1');
    const component = TestBed.createComponent(AchResponseDetailPageComponent).componentInstance;
    component.detail = { ...detailMock, estadoProcesamiento: 'Homologada' };
    component.reprocessReason = 'Motivo válido';

    component.requestReprocess();

    expect(apiSpy.requestReprocess).not.toHaveBeenCalled();
  });

  it('AchResponseDetailPageComponent_ShouldNotRequestReprocess_WhenTrimmedReasonHasLessThanFiveCharacters', () => {
    configureWithId('resp-1');
    const component = TestBed.createComponent(AchResponseDetailPageComponent).componentInstance;
    component.detail = { ...detailMock, estadoProcesamiento: 'ErrorTecnico' };
    component.reprocessReason = ' 1234 ';

    component.requestReprocess();

    expect(apiSpy.requestReprocess).not.toHaveBeenCalled();
    expect(notificationSpy.error).toHaveBeenCalledWith('La justificación de reproceso es obligatoria.');
  });

  it('AchResponseDetailPageComponent_ShouldSendIdCommandIdExpectedVersionAndTrimmedReason', () => {
    configureWithId('resp-1');
    const component = TestBed.createComponent(AchResponseDetailPageComponent).componentInstance;
    component.detail = { ...detailMock, estadoProcesamiento: 'ErrorTecnico' };
    component.reprocessReason = '  Motivo válido  ';

    component.requestReprocess();

    expect(apiSpy.requestReprocess).toHaveBeenCalledTimes(1);
    const [id, commandId, expectedVersion, reason] = apiSpy.requestReprocess.calls.mostRecent().args;
    expect(id).toBe('resp-1');
    expect(typeof commandId).toBe('string');
    expect(commandId.length).toBeGreaterThan(0);
    expect(expectedVersion).toBe('version-7');
    expect(reason).toBe('Motivo válido');
  });

  it('AchResponseDetailPageComponent_ShouldClearReasonAndReloadDetailAndHistoryOnReprocessSuccess', () => {
    configureWithId('resp-1');
    const component = TestBed.createComponent(AchResponseDetailPageComponent).componentInstance;
    component.detail = { ...detailMock, estadoProcesamiento: 'ErrorTecnico' };
    component.reprocessReason = 'Motivo válido';
    apiSpy.getDetail.calls.reset();
    apiSpy.getReprocessAttempts.calls.reset();

    component.requestReprocess();

    expect(notificationSpy.success).toHaveBeenCalledWith('Reproceso solicitado y pendiente de ejecución gobernada.');
    expect(component.reprocessReason).toBe('');
    expect(apiSpy.getDetail).toHaveBeenCalledOnceWith('resp-1');
    expect(apiSpy.getReprocessAttempts).toHaveBeenCalledOnceWith('resp-1');
  });

  it('AchResponseDetailPageComponent_ShouldWarnAndReloadDetailOnConflict409', () => {
    configureWithId('resp-1');
    const component = TestBed.createComponent(AchResponseDetailPageComponent).componentInstance;
    component.detail = { ...detailMock, estadoProcesamiento: 'ErrorTecnico' };
    component.reprocessReason = 'Motivo válido';
    apiSpy.requestReprocess.and.returnValue(throwError(() => new HttpErrorResponse({ status: 409 })));
    apiSpy.getDetail.calls.reset();
    apiSpy.getReprocessAttempts.calls.reset();

    component.requestReprocess();

    expect(notificationSpy.warning).toHaveBeenCalledWith(
      'La respuesta cambió o ya tiene un reproceso activo. Se recargó el detalle.'
    );
    expect(apiSpy.getDetail).toHaveBeenCalledOnceWith('resp-1');
    expect(apiSpy.getReprocessAttempts).not.toHaveBeenCalled();
  });

  it('AchResponseDetailPageComponent_ShouldExposeBackendDetailForNonConflictReprocessError', () => {
    configureWithId('resp-1');
    const component = TestBed.createComponent(AchResponseDetailPageComponent).componentInstance;
    component.detail = { ...detailMock, estadoProcesamiento: 'ErrorTecnico' };
    component.reprocessReason = 'Motivo válido';
    apiSpy.requestReprocess.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 400,
      error: { detail: 'Detalle controlado' }
    })));

    component.requestReprocess();

    expect(notificationSpy.error).toHaveBeenCalledWith('Detalle controlado');
  });

  it('AchResponseDetailPageComponent_ShouldUseFallbackForNonConflictReprocessErrorWithoutStringDetail', () => {
    configureWithId('resp-1');
    const component = TestBed.createComponent(AchResponseDetailPageComponent).componentInstance;
    component.detail = { ...detailMock, estadoProcesamiento: 'ErrorTecnico' };
    component.reprocessReason = 'Motivo válido';
    apiSpy.requestReprocess.and.returnValue(throwError(() => new HttpErrorResponse({ status: 500, error: {} })));

    component.requestReprocess();

    expect(notificationSpy.error).toHaveBeenCalledWith('No fue posible solicitar el reproceso.');
  });

  it('AchResponseDetailPageComponent_ShouldPreserveStatusTextsAndCanonicalStates', () => {
    configureWithId('resp-1');
    const component = TestBed.createComponent(AchResponseDetailPageComponent).componentInstance;

    expect(component.formatProcessingStatus('Homologada')).toBe('Homologada');
    expect(component.formatNotificationStatus('Exitosa')).toBe('Exitosa');
    expect(component.formatReprocessStatus('Pending')).toBe('Pendiente de ejecución');
    expect(component.formatReprocessStatus('Running')).toBe('En ejecución');
    expect(component.formatReprocessStatus('Completed')).toBe('Completado');
    expect(component.formatReprocessStatus('FailedFunctional')).toBe('Requiere revisión');
    expect(component.formatReprocessStatus('FailedTechnical')).toBe('Error técnico');

    expect(component.getProcessingStatusState('Notificada')).toBe('exitoso');
    expect(component.getProcessingStatusState('Homologada')).toBe('exitoso');
    expect(component.getProcessingStatusState('PendienteReintento')).toBe('pendiente');
    expect(component.getProcessingStatusState('RequiereRevisionManual')).toBe('pendiente');
    expect(component.getProcessingStatusState('NoHomologada')).toBe('pendiente');
    expect(component.getProcessingStatusState('ErrorFuncional')).toBe('fallido');
    expect(component.getProcessingStatusState('Recibida')).toBe('inactivo');

    expect(component.getNotificationStatusState('Exitosa')).toBe('exitoso');
    expect(component.getNotificationStatusState('Pendiente')).toBe('pendiente');
    expect(component.getNotificationStatusState('PendienteReintento')).toBe('pendiente');
    expect(component.getNotificationStatusState('RequiereRevisionManual')).toBe('pendiente');
    expect(component.getNotificationStatusState('ErrorFuncional')).toBe('fallido');
    expect(component.getNotificationStatusState('ErrorTecnico')).toBe('fallido');
    expect(component.getNotificationStatusState('Otro')).toBe('inactivo');

    expect(component.getReprocessStatusState('Completed')).toBe('exitoso');
    expect(component.getReprocessStatusState('Pending')).toBe('pendiente');
    expect(component.getReprocessStatusState('Running')).toBe('pendiente');
    expect(component.getReprocessStatusState('FailedFunctional')).toBe('pendiente');
    expect(component.getReprocessStatusState('FailedTechnical')).toBe('fallido');
    expect(component.getReprocessStatusState('Other')).toBe('inactivo');
  });

  it('AchResponseDetailPageComponent_ShouldRenderCanonicalStatusPrimitiveForProcessingNotificationAndReprocess', () => {
    configureWithId('resp-1');
    const fixture = TestBed.createComponent(AchResponseDetailPageComponent);

    fixture.detectChanges();
    fixture.detectChanges();

    const statusLabels = fixture.debugElement.queryAll(By.css('ui-etiqueta-estado'));
    expect(statusLabels.length).toBeGreaterThanOrEqual(3);
    expect(statusLabels.some((item) => item.componentInstance.texto === 'Homologada')).toBeTrue();
    expect(statusLabels.some((item) => item.componentInstance.texto === 'Exitosa')).toBeTrue();
    expect(statusLabels.some((item) => item.componentInstance.texto === 'Completado')).toBeTrue();
  });

  it('AchResponseDetailPageComponent_ShouldLoadAuditOnlyWhenOpeningFirstTime', () => {
    configureWithId('resp-1');
    const component = TestBed.createComponent(AchResponseDetailPageComponent).componentInstance;
    component.detail = detailMock;

    component.toggleAudit();

    expect(component.auditVisible).toBeTrue();
    expect(apiSpy.getResponseAudit).toHaveBeenCalledOnceWith('resp-1');
    expect(component.auditEntries).toEqual([auditMock]);
    expect(component.auditEntries[0].sanitizedMetadata).toBe('{"commandId":"cmd-1"}');
  });

  it('AchResponseDetailPageComponent_ShouldNotCallAuditApiWhenClosing', () => {
    configureWithId('resp-1');
    const component = TestBed.createComponent(AchResponseDetailPageComponent).componentInstance;
    component.detail = detailMock;
    component.auditVisible = true;
    component.auditEntries = [auditMock];

    component.toggleAudit();

    expect(component.auditVisible).toBeFalse();
    expect(apiSpy.getResponseAudit).not.toHaveBeenCalled();
  });

  it('AchResponseDetailPageComponent_ShouldNotReloadAuditWhenEntriesAreAlreadyLoaded', () => {
    configureWithId('resp-1');
    const component = TestBed.createComponent(AchResponseDetailPageComponent).componentInstance;
    component.detail = detailMock;
    component.auditVisible = false;
    component.auditEntries = [auditMock];

    component.toggleAudit();

    expect(component.auditVisible).toBeTrue();
    expect(apiSpy.getResponseAudit).not.toHaveBeenCalled();
  });

  it('AchResponseDetailPageComponent_ShouldHandleDetailLoadError', () => {
    configureWithId('resp-1');
    apiSpy.getDetail.and.returnValue(throwError(() => new Error('fail')));
    const fixture = TestBed.createComponent(AchResponseDetailPageComponent);

    fixture.detectChanges();

    expect(fixture.componentInstance.error).toBeTrue();
    expect(fixture.componentInstance.loading).toBeFalse();
    expect(fixture.componentInstance.detail).toBeNull();
    expect(notificationSpy.error).toHaveBeenCalledWith('No fue posible cargar el detalle de la respuesta ACH');
  });

  it('AchResponseDetailPageComponent_ShouldFormatValuesWithoutChangingExistingFormatters', () => {
    configureWithId('resp-1');
    const component = TestBed.createComponent(AchResponseDetailPageComponent).componentInstance;

    expect(component.formatValue('')).toBe('-');
    expect(component.formatValue(null)).toBe('-');
    expect(component.formatValue(true)).toBe('Sí');
    expect(component.formatDate('2026-01-01T00:00:00Z')).not.toBe('-');
  });
});
