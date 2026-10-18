import { By } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AchResponsesApiService } from '../services/ach-responses-api.service';
import { AchResponseManualReviewPageComponent } from './ach-response-manual-review-page.component';

describe('AchResponseManualReviewPageComponent', () => {
  let apiSpy: jasmine.SpyObj<AchResponsesApiService>;
  let authSpy: jasmine.SpyObj<AuthService>;
  let notificationSpy: jasmine.SpyObj<NotificationService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let grantedPermissions: Set<string>;

  const responseMock = {
    items: [
      {
        id: 'resp-1',
        tipoRespuesta: 'Transaccion',
        idTransaccion: 'TX-1',
        codigoCamaraCompensacion: 'ACH',
        codigoEstadoExterno: '00',
        estadoProcesamiento: 'NoHomologada',
        permiteNotificacion: true,
        fechaRecepcion: '2026-01-01T00:00:00Z',
        fechaCreacion: '2026-01-01T00:00:00Z'
      }
    ],
    totalCount: 1,
    totalPages: 1,
    pageNumber: 1,
    pageSize: 20
  } as any;

  const orphanMock = {
    id: 'orphan-1',
    achResponseId: 'resp-orphan-1',
    clearingHouseId: 1,
    responseType: 'Transaccion',
    externalIdentifiers: 'ext-1',
    externalCode: 'R01',
    receivedAtUtc: '2026-01-01T00:00:00Z',
    operationalDate: '2026-01-01',
    correlationId: 'corr-1',
    orphanReason: 'Sin correlación',
    resolutionStatus: 'Pending',
    resolvedReference: 'REF-OLD',
    version: 'version-1'
  } as any;

  beforeEach(() => {
    apiSpy = jasmine.createSpyObj<AchResponsesApiService>('AchResponsesApiService', [
      'search',
      'getOrphans',
      'beginOrphanReview',
      'resolveOrphan'
    ]);
    apiSpy.search.and.returnValue(of(responseMock));
    apiSpy.getOrphans.and.returnValue(of([]));
    grantedPermissions = new Set(['CanReadAch', 'CanManageAch']);
    authSpy = jasmine.createSpyObj<AuthService>('AuthService', ['hasPermission']);
    authSpy.hasPermission.and.callFake((expected: string | string[]) => {
      const permissions = Array.isArray(expected) ? expected : [expected];
      return permissions.some((permission) => grantedPermissions.has(permission));
    });
    notificationSpy = jasmine.createSpyObj<NotificationService>('NotificationService', ['error', 'success', 'warning']);
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [AchResponseManualReviewPageComponent],
      providers: [
        { provide: AchResponsesApiService, useValue: apiSpy },
        { provide: AuthService, useValue: authSpy },
        { provide: NotificationService, useValue: notificationSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });
  });

  it('AchResponseManualReviewPageComponent_ShouldCreate', () => {
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('AchResponseManualReviewPageComponent_ShouldLoadCasesAndOrphansOnInit', () => {
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);

    fixture.detectChanges();

    expect(apiSpy.search).toHaveBeenCalledTimes(1);
    expect(apiSpy.getOrphans).toHaveBeenCalledOnceWith(undefined, undefined);
    expect(fixture.componentInstance.rows.length).toBe(1);
  });

  it('AchResponseManualReviewPageComponent_ShouldLoadNoHomologadaByDefault', () => {
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    fixture.detectChanges();

    const request = apiSpy.search.calls.mostRecent().args[0];
    expect(request.estadoProcesamiento).toBe('NoHomologada');
    expect(request.pageNumber).toBe(1);
    expect(request.pageSize).toBe(20);
  });

  it('AchResponseManualReviewPageComponent_ShouldNormalizeFiltersWithoutChangingRequestContract', () => {
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.filtrosForm.patchValue({
      fechaDesde: ' 2026-01-01 ',
      fechaHasta: '2026-01-31',
      estadoProcesamiento: 'ErrorFuncional',
      tipoRespuesta: 'Transaccion',
      idTransaccion: ' TX-77 ',
      codigoCamaraCompensacion: ' ACH ',
      codigoEntidadOrigen: ' 001 ',
      codigoEntidadDestino: '   ',
      pageNumber: 4,
      pageSize: 50
    });

    component.applyFilters();

    expect(apiSpy.search.calls.mostRecent().args[0]).toEqual({
      fechaDesde: '2026-01-01',
      fechaHasta: '2026-01-31',
      estadoProcesamiento: 'ErrorFuncional',
      tipoRespuesta: 'Transaccion',
      idTransaccion: 'TX-77',
      codigoCamaraCompensacion: 'ACH',
      codigoEntidadOrigen: '001',
      codigoEntidadDestino: undefined,
      pageNumber: 1,
      pageSize: 50
    });
  });

  it('AchResponseManualReviewPageComponent_ShouldClearFiltersToDefaultNoHomologada', () => {
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.filtrosForm.patchValue({
      idTransaccion: 'x',
      estadoProcesamiento: 'ErrorFuncional',
      pageNumber: 3,
      pageSize: 50
    });
    component.clearFilters();

    expect(component.filtrosForm.getRawValue()).toEqual({
      fechaDesde: '',
      fechaHasta: '',
      estadoProcesamiento: 'NoHomologada',
      tipoRespuesta: '',
      idTransaccion: '',
      codigoCamaraCompensacion: '',
      codigoEntidadOrigen: '',
      codigoEntidadDestino: '',
      pageNumber: 1,
      pageSize: 20
    });
  });

  it('AchResponseManualReviewPageComponent_ShouldRespectPaginationBoundsAndResetOnPageSize', () => {
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.totalPages = 3;
    apiSpy.search.calls.reset();

    component.filtrosForm.patchValue({ pageNumber: 1, pageSize: 20 });
    component.previousPage();
    expect(component.filtrosForm.controls.pageNumber.value).toBe(1);
    expect(apiSpy.search).not.toHaveBeenCalled();

    component.nextPage();
    expect(component.filtrosForm.controls.pageNumber.value).toBe(2);
    expect(apiSpy.search.calls.mostRecent().args[0].pageNumber).toBe(2);
    expect(apiSpy.search.calls.mostRecent().args[0].pageSize).toBe(20);

    component.filtrosForm.patchValue({ pageNumber: 3 });
    apiSpy.search.calls.reset();
    component.nextPage();
    expect(component.filtrosForm.controls.pageNumber.value).toBe(3);
    expect(apiSpy.search).not.toHaveBeenCalled();

    component.setPageSize(50);
    expect(component.filtrosForm.controls.pageNumber.value).toBe(1);
    expect(apiSpy.search.calls.mostRecent().args[0].pageSize).toBe(50);
  });

  it('AchResponseManualReviewPageComponent_ShouldNavigateToDetail', () => {
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);

    fixture.componentInstance.openDetail({ id: 'resp-1' } as any);

    expect(routerSpy.navigate).toHaveBeenCalledWith(['/ach-responses', 'resp-1']);
  });

  it('AchResponseManualReviewPageComponent_ShouldSelectOrphanWithBackendVersionAndResolvedReference', () => {
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    const component = fixture.componentInstance;

    component.selectOrphan(orphanMock);

    expect(component.resolutionForm.getRawValue()).toEqual({
      orphanId: 'orphan-1',
      expectedVersion: 'version-1',
      reason: '',
      functionalReference: 'REF-OLD',
      reject: false
    });
  });

  it('AchResponseManualReviewPageComponent_ShouldKeepReadModeButHideMutationControlsWithoutManagementPermission', () => {
    grantedPermissions = new Set(['CanReadAch']);
    apiSpy.getOrphans.and.returnValue(of([orphanMock]));
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);

    fixture.detectChanges();

    expect(apiSpy.search).toHaveBeenCalledTimes(1);
    expect(apiSpy.getOrphans).toHaveBeenCalledTimes(1);
    expect(fixture.componentInstance.canManageManualReview).toBeFalse();
    expect(fixture.debugElement.query(By.css('.resolution-form'))).toBeNull();
    expect(fixture.debugElement.query(By.css('.orphan-section button'))).toBeNull();
  });

  it('AchResponseManualReviewPageComponent_ShouldRejectMutationCallsWithCanReadAchOnly', () => {
    grantedPermissions = new Set(['CanReadAch']);
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    const component = fixture.componentInstance;
    component.selectOrphan(orphanMock);
    component.resolutionForm.patchValue({ reason: 'Motivo válido', functionalReference: 'REF-1' });

    component.beginSelectedReview();
    component.resolveSelectedOrphan();

    expect(apiSpy.beginOrphanReview).not.toHaveBeenCalled();
    expect(apiSpy.resolveOrphan).not.toHaveBeenCalled();
    expect(notificationSpy.error).toHaveBeenCalledWith('No tiene permisos para modificar la revisión manual.');
  });

  it('AchResponseManualReviewPageComponent_ShouldAcceptFineGrainedNachaGenerateForMutableActions', () => {
    grantedPermissions = new Set(['CanReadAch', 'Nacha.Generate']);
    apiSpy.beginOrphanReview.and.returnValue(of({ ...orphanMock, version: 'version-2', resolutionStatus: 'InReview' }));
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    const component = fixture.componentInstance;
    component.selectOrphan(orphanMock);
    component.resolutionForm.patchValue({ reason: 'Motivo válido' });

    expect(component.canManageManualReview).toBeTrue();
    component.beginSelectedReview();

    expect(apiSpy.beginOrphanReview).toHaveBeenCalledOnceWith('orphan-1', 'version-1', 'Motivo válido');
  });

  it('AchResponseManualReviewPageComponent_ShouldNotBeginReviewWithoutOrphan', () => {
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    const component = fixture.componentInstance;
    component.resolutionForm.patchValue({ reason: 'Motivo válido' });

    component.beginSelectedReview();

    expect(apiSpy.beginOrphanReview).not.toHaveBeenCalled();
    expect(notificationSpy.error).toHaveBeenCalledWith('Seleccione una huérfana e indique una justificación.');
  });

  it('AchResponseManualReviewPageComponent_ShouldNotBeginReviewWithShortReason', () => {
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    const component = fixture.componentInstance;
    component.selectOrphan(orphanMock);
    component.resolutionForm.patchValue({ reason: 'abcd' });

    component.beginSelectedReview();

    expect(apiSpy.beginOrphanReview).not.toHaveBeenCalled();
  });

  it('AchResponseManualReviewPageComponent_ShouldBeginReviewWithTrimmedReasonAndRefreshReturnedVersion', () => {
    const reviewed = { ...orphanMock, version: 'version-2', resolutionStatus: 'InReview' };
    apiSpy.beginOrphanReview.and.returnValue(of(reviewed));
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.selectOrphan(orphanMock);
    component.resolutionForm.patchValue({ reason: '  Análisis operativo  ' });
    apiSpy.getOrphans.calls.reset();

    component.beginSelectedReview();

    expect(apiSpy.beginOrphanReview).toHaveBeenCalledOnceWith('orphan-1', 'version-1', 'Análisis operativo');
    expect(notificationSpy.success).toHaveBeenCalledWith('Revisión iniciada.');
    expect(component.resolutionForm.controls.expectedVersion.value).toBe('version-2');
    expect(component.resolutionForm.controls.reason.value).toBe('');
    expect(apiSpy.getOrphans).toHaveBeenCalledOnceWith(undefined, undefined);
  });

  it('AchResponseManualReviewPageComponent_ShouldRequireFunctionalReferenceForAssociation', () => {
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    const component = fixture.componentInstance;
    component.selectOrphan({ ...orphanMock, resolvedReference: null });
    component.resolutionForm.patchValue({ reason: 'Motivo válido', functionalReference: '   ', reject: false });

    component.resolveSelectedOrphan();

    expect(apiSpy.resolveOrphan).not.toHaveBeenCalled();
  });

  it('AchResponseManualReviewPageComponent_ShouldAssociateWithTrimmedValuesAndResetForm', () => {
    apiSpy.resolveOrphan.and.returnValue(of({ ...orphanMock, resolutionStatus: 'Associated', version: 'version-2' }));
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.selectOrphan(orphanMock);
    component.resolutionForm.patchValue({
      reason: '  Referencia validada  ',
      functionalReference: '  TX-FUNC-99  ',
      reject: false
    });
    apiSpy.search.calls.reset();
    apiSpy.getOrphans.calls.reset();

    component.resolveSelectedOrphan();

    expect(apiSpy.resolveOrphan).toHaveBeenCalledOnceWith(
      'orphan-1',
      'version-1',
      'Referencia validada',
      'TX-FUNC-99',
      false
    );
    expect(notificationSpy.success).toHaveBeenCalledWith('Respuesta huérfana asociada.');
    expect(component.resolutionForm.getRawValue()).toEqual({
      orphanId: '',
      expectedVersion: '',
      reason: '',
      functionalReference: '',
      reject: false
    });
    expect(apiSpy.getOrphans).toHaveBeenCalledTimes(1);
    expect(apiSpy.search).toHaveBeenCalledTimes(1);
  });

  it('AchResponseManualReviewPageComponent_ShouldRejectWithNullFunctionalReference', () => {
    apiSpy.resolveOrphan.and.returnValue(of({ ...orphanMock, resolutionStatus: 'Rejected', version: 'version-2' }));
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.selectOrphan(orphanMock);
    component.resolutionForm.patchValue({ reason: '  Rechazo operativo  ', functionalReference: '', reject: true });

    component.resolveSelectedOrphan();

    expect(apiSpy.resolveOrphan).toHaveBeenCalledOnceWith(
      'orphan-1',
      'version-1',
      'Rechazo operativo',
      null,
      true
    );
    expect(notificationSpy.success).toHaveBeenCalledWith('Respuesta huérfana rechazada.');
  });

  it('AchResponseManualReviewPageComponent_ShouldHandleBeginReviewConflictWithoutAutomaticRetry', () => {
    apiSpy.beginOrphanReview.and.returnValue(throwError(() => ({ status: 409, error: {} })));
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.selectOrphan(orphanMock);
    component.resolutionForm.patchValue({ reason: 'Motivo válido' });
    apiSpy.getOrphans.calls.reset();

    component.beginSelectedReview();

    expect(apiSpy.beginOrphanReview).toHaveBeenCalledTimes(1);
    expect(notificationSpy.warning).toHaveBeenCalledWith('Otro usuario modificó el caso. Se recargó la versión vigente.');
    expect(apiSpy.getOrphans).toHaveBeenCalledOnceWith(undefined, undefined);
  });

  it('AchResponseManualReviewPageComponent_ShouldHandleResolveConflictWithoutAutomaticRetry', () => {
    apiSpy.resolveOrphan.and.returnValue(throwError(() => ({ status: 409, error: {} })));
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.selectOrphan(orphanMock);
    component.resolutionForm.patchValue({ reason: 'Motivo válido', functionalReference: 'REF-2', reject: false });
    apiSpy.getOrphans.calls.reset();

    component.resolveSelectedOrphan();

    expect(apiSpy.resolveOrphan).toHaveBeenCalledTimes(1);
    expect(notificationSpy.warning).toHaveBeenCalledWith('Otro usuario modificó el caso. Se recargó la versión vigente.');
    expect(apiSpy.getOrphans).toHaveBeenCalledOnceWith(undefined, undefined);
  });

  it('AchResponseManualReviewPageComponent_ShouldUseBackendDetailForOperationError', () => {
    apiSpy.beginOrphanReview.and.returnValue(throwError(() => ({ status: 400, error: { detail: 'Detalle controlado' } })));
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    const component = fixture.componentInstance;
    component.selectOrphan(orphanMock);
    component.resolutionForm.patchValue({ reason: 'Motivo válido' });

    component.beginSelectedReview();

    expect(notificationSpy.error).toHaveBeenCalledWith('Detalle controlado');
  });

  it('AchResponseManualReviewPageComponent_ShouldUseFallbackForOperationErrorWithoutDetail', () => {
    apiSpy.resolveOrphan.and.returnValue(throwError(() => ({ status: 500, error: {} })));
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    const component = fixture.componentInstance;
    component.selectOrphan(orphanMock);
    component.resolutionForm.patchValue({ reason: 'Motivo válido', functionalReference: 'REF-2' });

    component.resolveSelectedOrphan();

    expect(notificationSpy.error).toHaveBeenCalledWith('No fue posible completar la operación.');
  });

  it('AchResponseManualReviewPageComponent_ShouldExposeOrphanLoadErrorForCanonicalRetryState', () => {
    apiSpy.getOrphans.and.returnValue(throwError(() => new Error('fail')));
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);

    fixture.detectChanges();

    expect(fixture.componentInstance.orphanError).toBeTrue();
    expect(fixture.componentInstance.orphanLoading).toBeFalse();
    expect(fixture.debugElement.query(By.css('ui-estado-error'))).toBeTruthy();
  });

  it('AchResponseManualReviewPageComponent_ShouldPreservePriorityClassificationAndGridRenderers', () => {
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    const component = fixture.componentInstance;

    expect(component.getPriority('NoHomologada')).toBe('Alta');
    expect(component.getPriority('ErrorFuncional')).toBe('Alta');
    expect(component.getPriority('RequiereRevisionManual')).toBe('Media');
    expect(component.getPriority('PendienteReintento')).toBe('Media');
    expect(component.getPriority('Otro')).toBe('Baja');

    const priorityColumn = component.columnas.find((column) => column.field === 'prioridadText');
    const rendered = priorityColumn?.cellRenderer?.({ value: 'Alta' } as any) as HTMLElement;
    expect(rendered instanceof HTMLElement).toBeTrue();
    expect(rendered.textContent).toBe('Alta');
  });

  it('AchResponseManualReviewPageComponent_ShouldMapOnlyKnownOrphanStatusesVisually', () => {
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    const component = fixture.componentInstance;

    expect(component.getOrphanResolutionStatusState('Resolved')).toBe('exitoso');
    expect(component.getOrphanResolutionStatusState('Associated')).toBe('exitoso');
    expect(component.getOrphanResolutionStatusState('Completed')).toBe('exitoso');
    expect(component.getOrphanResolutionStatusState('Pending')).toBe('pendiente');
    expect(component.getOrphanResolutionStatusState('InReview')).toBe('pendiente');
    expect(component.getOrphanResolutionStatusState('Rejected')).toBe('fallido');
    expect(component.getOrphanResolutionStatusState('Failed')).toBe('fallido');
    expect(component.getOrphanResolutionStatusState('Otro')).toBe('inactivo');
  });

  it('AchResponseManualReviewPageComponent_ShouldUseFoundationControlsWithoutUiBotonAndKeepEnterpriseGrid', () => {
    apiSpy.getOrphans.and.returnValue(of([orphanMock]));
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);

    fixture.detectChanges();

    expect(fixture.debugElement.query(By.css('ui-boton'))).toBeNull();
    expect(fixture.debugElement.query(By.css('ui-grilla-empresarial'))).toBeTruthy();
    expect(fixture.debugElement.queryAll(By.css('mat-form-field')).length).toBeGreaterThan(0);
    expect(fixture.debugElement.query(By.css('mat-checkbox'))).toBeTruthy();
    expect(fixture.debugElement.query(By.css('ui-etiqueta-estado'))).toBeTruthy();
  });

  it('AchResponseManualReviewPageComponent_ShouldRenderDetailActionAsButtonElement', () => {
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    const component = fixture.componentInstance;
    const actionColumn = component.columnas.find((column) => column.headerName === 'Acciones');

    const rendered = actionColumn?.cellRenderer?.({} as any) as HTMLButtonElement;

    expect(rendered instanceof HTMLButtonElement).toBeTrue();
    expect(rendered.textContent).toBe('Ver detalle');
    expect(rendered.dataset['action']).toBe('detalle');
  });
});
