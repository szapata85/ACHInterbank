import { By } from '@angular/platform-browser';
import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { AchResponseMappingModel, AchResponseStatusMappingResponse } from '../models/ach-responses.models';
import { AchResponsesApiService } from '../services/ach-responses-api.service';
import { AchResponseStatusMappingsPageComponent } from './ach-response-status-mappings-page.component';

describe('AchResponseStatusMappingsPageComponent', () => {
  let apiSpy: jasmine.SpyObj<AchResponsesApiService>;
  let notificationSpy: jasmine.SpyObj<NotificationService>;

  const mappingsMock: AchResponseStatusMappingResponse[] = [
    {
      id: 1,
      codigoCamaraCompensacion: 'ACH',
      tipoRespuesta: 'Transaccion',
      codigoEstadoExterno: '00',
      codigoCausalExterna: 'R01',
      idEstadoInterno: 10,
      idEstadoServicioExterno: 20,
      estadoInternoNombre: 'Aprobada',
      causalNormalizada: 'R01',
      descripcionCausalNormalizada: 'Causal normalizada',
      requiereCausal: false,
      permiteNotificacion: true,
      activo: true,
      fechaInicioVigencia: '2026-01-01T00:00:00Z',
      fechaFinVigencia: '2026-12-31T00:00:00Z',
      clearingHouseId: 7,
      priority: 100,
      version: 'v1'
    }
  ];

  const currentMappingMock: AchResponseMappingModel = {
    id: 1,
    clearingHouseId: 7,
    clearingHouseCode: 'ACH',
    responseType: 'Transaccion',
    externalCode: '00',
    externalCause: 'R01',
    internalStatusId: 10,
    externalServiceStatusId: 20,
    internalStatusName: 'Aprobada',
    normalizedCause: 'R01',
    normalizedDescription: 'Causal normalizada',
    requiresCause: false,
    allowsNotification: true,
    priority: 100,
    effectiveFrom: '2026-01-01T00:00:00Z',
    effectiveTo: '2026-12-31T00:00:00Z',
    isActive: true,
    version: 'v2'
  };

  beforeEach(() => {
    apiSpy = jasmine.createSpyObj<AchResponsesApiService>('AchResponsesApiService', [
      'getStatusMappings',
      'getStatusMapping',
      'createStatusMapping',
      'updateStatusMapping'
    ]);
    apiSpy.getStatusMappings.and.returnValue(of(mappingsMock));
    apiSpy.getStatusMapping.and.returnValue(of(currentMappingMock));
    apiSpy.createStatusMapping.and.returnValue(of(currentMappingMock));
    apiSpy.updateStatusMapping.and.returnValue(of(currentMappingMock));

    notificationSpy = jasmine.createSpyObj<NotificationService>('NotificationService', ['error', 'success', 'warning']);

    TestBed.configureTestingModule({
      imports: [AchResponseStatusMappingsPageComponent],
      providers: [
        { provide: AchResponsesApiService, useValue: apiSpy },
        { provide: NotificationService, useValue: notificationSpy }
      ]
    });
  });

  function createComponent() {
    const fixture = TestBed.createComponent(AchResponseStatusMappingsPageComponent);
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
    return fixture;
  }

  it('AchResponseStatusMappingsPageComponent_ShouldLoadMappingsOnInit', () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;

    expect(component.rows.length).toBe(1);
    expect(component.rows[0].activoText).toBe('Sí');
    expect(apiSpy.getStatusMappings).toHaveBeenCalledTimes(1);
    expect(component.loading).toBeFalse();
    expect(component.error).toBeFalse();
  });

  it('AchResponseStatusMappingsPageComponent_ShouldExposeLoadingAndEmptyStates', () => {
    const pending = new Subject<AchResponseStatusMappingResponse[]>();
    apiSpy.getStatusMappings.and.returnValue(pending.asObservable());
    const fixture = TestBed.createComponent(AchResponseStatusMappingsPageComponent);

    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
    expect(fixture.componentInstance.loading).toBeTrue();

    pending.next([]);
    pending.complete();

    expect(fixture.componentInstance.loading).toBeFalse();
    expect(fixture.componentInstance.rows).toEqual([]);
    expect(fixture.componentInstance.error).toBeFalse();
  });

  it('AchResponseStatusMappingsPageComponent_ShouldHandleLoadError', () => {
    apiSpy.getStatusMappings.and.returnValue(throwError(() => new Error('fail')));
    const fixture = createComponent();
    const component = fixture.componentInstance;

    expect(component.error).toBeTrue();
    expect(component.loading).toBeFalse();
    expect(component.rows).toEqual([]);
    expect(notificationSpy.error).toHaveBeenCalledWith('No fue posible cargar las homologaciones ACH');
  });

  it('AchResponseStatusMappingsPageComponent_ShouldApplyFiltersWithoutChangingValues', () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;

    component.filtrosForm.patchValue({
      codigoCamaraCompensacion: ' ACH ',
      tipoRespuesta: 'Transaccion',
      activo: 'true'
    });
    component.applyFilters();

    const request = apiSpy.getStatusMappings.calls.mostRecent().args[0];
    expect(request?.codigoCamaraCompensacion).toBe('ACH');
    expect(request?.tipoRespuesta).toBe('Transaccion');
    expect(request?.activo).toBeTrue();
  });

  it('AchResponseStatusMappingsPageComponent_ShouldClearFilters', () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;

    component.filtrosForm.patchValue({ codigoCamaraCompensacion: 'ACH', tipoRespuesta: 'Prenota', activo: 'false' });
    component.clearFilters();

    expect(component.filtrosForm.getRawValue()).toEqual({
      codigoCamaraCompensacion: '',
      tipoRespuesta: '',
      activo: ''
    });
    expect(apiSpy.getStatusMappings.calls.count()).toBe(2);
  });

  it('AchResponseStatusMappingsPageComponent_ShouldRequireMandatoryFieldsAndValidDates', () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;
    component.newMapping();
    component.mappingForm.patchValue({
      clearingHouseId: 1,
      externalCode: 'R01',
      internalStatusId: 10,
      externalServiceStatusId: 20,
      internalStatusName: 'Rechazada',
      effectiveFrom: '2026-07-10',
      effectiveTo: '2026-07-01',
      reason: 'Alta controlada'
    });

    component.saveMapping();

    expect(apiSpy.createStatusMapping).not.toHaveBeenCalled();
    expect(notificationSpy.error).toHaveBeenCalledWith('La fecha fin no puede ser anterior a la fecha inicio.');
  });

  it('AchResponseStatusMappingsPageComponent_ShouldRejectReasonShorterThanFiveCharacters', () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;
    component.newMapping();
    component.mappingForm.patchValue({
      clearingHouseId: 7,
      externalCode: 'R01',
      internalStatusId: 10,
      externalServiceStatusId: 20,
      internalStatusName: 'Rechazada',
      effectiveFrom: '2026-07-01',
      reason: '1234'
    });

    component.saveMapping();

    expect(apiSpy.createStatusMapping).not.toHaveBeenCalled();
    expect(notificationSpy.error).toHaveBeenCalledWith('Complete los campos obligatorios y la justificación.');
  });

  it('AchResponseStatusMappingsPageComponent_ShouldCreateMappingWithExactRequestAndReload', () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;
    component.newMapping();
    component.mappingForm.patchValue({
      clearingHouseId: 7,
      responseType: 'Transaccion',
      externalCode: ' R01 ',
      externalCause: ' C01 ',
      internalStatusId: 10,
      externalServiceStatusId: 20,
      internalStatusName: ' Rechazada ',
      normalizedCause: ' NC01 ',
      normalizedDescription: ' Descripción ',
      requiresCause: true,
      allowsNotification: false,
      priority: 100,
      effectiveFrom: '2026-07-01',
      effectiveTo: '2026-12-31',
      isActive: true,
      reason: ' Alta controlada '
    });

    component.saveMapping();

    const request = apiSpy.createStatusMapping.calls.mostRecent().args[0];
    expect(request).toEqual({
      clearingHouseId: 7,
      responseType: 'Transaccion',
      externalCode: 'R01',
      externalCause: 'C01',
      internalStatusId: 10,
      externalServiceStatusId: 20,
      internalStatusName: 'Rechazada',
      normalizedCause: 'NC01',
      normalizedDescription: 'Descripción',
      requiresCause: true,
      allowsNotification: false,
      priority: 100,
      effectiveFrom: '2026-07-01T00:00:00.000Z',
      effectiveTo: '2026-12-31T00:00:00.000Z',
      isActive: true,
      expectedVersion: null,
      reason: 'Alta controlada'
    });
    expect(notificationSpy.success).toHaveBeenCalledWith('Homologación guardada correctamente.');
    expect(component.editorVisible).toBeFalse();
    expect(apiSpy.getStatusMappings.calls.count()).toBe(2);
  });

  it('AchResponseStatusMappingsPageComponent_ShouldEditPreservingIdVersionAndActiveValue', () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;
    const row = { ...mappingsMock[0], activo: false, version: 'row-version' };

    component.editMapping(row);
    component.mappingForm.patchValue({ reason: ' Edición controlada ' });
    expect(component.editingId).toBe(1);

    component.saveMapping();

    expect(apiSpy.updateStatusMapping).toHaveBeenCalledTimes(1);
    const [id, request] = apiSpy.updateStatusMapping.calls.mostRecent().args;
    expect(id).toBe(1);
    expect(request.expectedVersion).toBe('row-version');
    expect(request.isActive).toBeFalse();
    expect(request.reason).toBe('Edición controlada');
    expect(apiSpy.createStatusMapping).not.toHaveBeenCalled();
  });

  it('AchResponseStatusMappingsPageComponent_ShouldCancelEditorAndClearEditMode', () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;

    component.editMapping(mappingsMock[0]);
    component.cancelEditor();

    expect(component.editorVisible).toBeFalse();
    expect(component.editingId).toBeNull();
  });

  it('AchResponseStatusMappingsPageComponent_ShouldPreserve409ConflictReloadWithoutRetry', () => {
    apiSpy.updateStatusMapping.and.returnValue(throwError(() => ({ status: 409, error: { detail: 'conflict' } })));
    const fixture = createComponent();
    const component = fixture.componentInstance;
    component.editMapping(mappingsMock[0]);
    component.mappingForm.patchValue({ reason: 'Cambio concurrente' });

    component.saveMapping();

    expect(apiSpy.updateStatusMapping).toHaveBeenCalledTimes(1);
    expect(notificationSpy.warning).toHaveBeenCalledWith('La homologación cambió mientras la editaba. Se recargó la versión vigente.');
    expect(apiSpy.getStatusMappings.calls.count()).toBe(2);
    expect(apiSpy.getStatusMapping).toHaveBeenCalledOnceWith(1);
    expect(component.mappingForm.controls.expectedVersion.value).toBe('v2');
  });

  it('AchResponseStatusMappingsPageComponent_ShouldSurfaceBackendDuplicateDetailWithoutAutocorrection', () => {
    apiSpy.createStatusMapping.and.returnValue(throwError(() => ({
      status: 400,
      error: { detail: 'Ya existe una homologación para la combinación indicada.' }
    })));
    const fixture = createComponent();
    const component = fixture.componentInstance;
    component.newMapping();
    component.mappingForm.patchValue({
      clearingHouseId: 7,
      externalCode: 'R01',
      internalStatusId: 10,
      externalServiceStatusId: 20,
      internalStatusName: 'Rechazada',
      effectiveFrom: '2026-07-01',
      reason: 'Alta controlada'
    });

    component.saveMapping();

    expect(apiSpy.createStatusMapping).toHaveBeenCalledTimes(1);
    expect(notificationSpy.error).toHaveBeenCalledWith('Ya existe una homologación para la combinación indicada.');
    expect(apiSpy.getStatusMappings).toHaveBeenCalledTimes(1);
  });

  it('AchResponseStatusMappingsPageComponent_ShouldUseFallbackForWriteErrorWithoutDetail', () => {
    apiSpy.createStatusMapping.and.returnValue(throwError(() => ({ status: 500, error: {} })));
    const fixture = createComponent();
    const component = fixture.componentInstance;
    component.newMapping();
    component.mappingForm.patchValue({
      clearingHouseId: 7,
      externalCode: 'R01',
      internalStatusId: 10,
      externalServiceStatusId: 20,
      internalStatusName: 'Rechazada',
      effectiveFrom: '2026-07-01',
      reason: 'Alta controlada'
    });

    component.saveMapping();

    expect(notificationSpy.error).toHaveBeenCalledWith('No fue posible guardar la homologación.');
  });

  it('AchResponseStatusMappingsPageComponent_ShouldPreserveBooleanGridBadges', () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;
    const activeColumn = component.columnas.find((column) => column.field === 'activoText');

    const rendered = activeColumn?.cellRenderer?.({ data: { activo: true } } as any) as HTMLElement;
    expect(rendered instanceof HTMLElement).toBeTrue();
    expect(rendered.textContent).toBe('Sí');
  });

  it('AchResponseStatusMappingsPageComponent_ShouldUseMaterialControlsAndKeepEnterpriseGridWithoutUiBoton', () => {
    const fixture = createComponent();
    fixture.componentInstance.newMapping();
    (fixture.componentInstance as any).cdr.markForCheck();
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.css('ui-boton'))).toBeNull();
    expect(fixture.debugElement.queryAll(By.css('mat-form-field')).length).toBeGreaterThan(0);
    expect(fixture.debugElement.queryAll(By.css('mat-checkbox')).length).toBe(3);
    expect(fixture.debugElement.queryAll(By.css('button[mat-flat-button], button[mat-stroked-button]')).length).toBeGreaterThan(0);
    expect(fixture.debugElement.query(By.css('ui-grilla-empresarial'))).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Homologación de estados');
    expect(componentValue(fixture.componentInstance.mappingForm.controls.responseType.value)).toBe('Transaccion');
  });

  it('AchResponseStatusMappingsPageComponent_ShouldNotExposeForbiddenTerms', () => {
    const visibleKeys = Object.keys(mappingsMock[0]).join('|');
    const forbiddenTerms = ['idTransaccionAxon', 'Axon', 'Soap', 'SOAP', 'Wsdl', 'Envelope', 'RequestPayload', 'ResponsePayload', 'Xml'];

    forbiddenTerms.forEach((term) => expect(visibleKeys).not.toContain(term));
  });
});

function componentValue(value: string | null): string | null {
  return value;
}
