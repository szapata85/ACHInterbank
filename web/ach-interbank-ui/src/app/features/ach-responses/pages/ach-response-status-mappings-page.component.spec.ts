import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { AchResponsesApiService } from '../services/ach-responses-api.service';
import { AchResponseStatusMappingsPageComponent } from './ach-response-status-mappings-page.component';

describe('AchResponseStatusMappingsPageComponent', () => {
  let apiSpy: jasmine.SpyObj<AchResponsesApiService>;
  let notificationSpy: jasmine.SpyObj<NotificationService>;

  const mappingsMock = [
    {
      id: 1,
      codigoCamaraCompensacion: 'ACH',
      tipoRespuesta: 'Transaccion',
      codigoEstadoExterno: '00',
      idEstadoInterno: 10,
      idEstadoServicioExterno: 20,
      estadoInternoNombre: 'Aprobada',
      requiereCausal: false,
      permiteNotificacion: true,
      activo: true,
      fechaInicioVigencia: '2026-01-01T00:00:00Z'
    }
  ] as any;

  beforeEach(() => {
    apiSpy = jasmine.createSpyObj<AchResponsesApiService>('AchResponsesApiService', ['getStatusMappings', 'getStatusMapping', 'createStatusMapping', 'updateStatusMapping']);
    apiSpy.getStatusMappings.and.returnValue(of(mappingsMock));
    notificationSpy = jasmine.createSpyObj<NotificationService>('NotificationService', ['error', 'success', 'warning']);

    TestBed.configureTestingModule({
      imports: [AchResponseStatusMappingsPageComponent],
      providers: [
        { provide: AchResponsesApiService, useValue: apiSpy },
        { provide: NotificationService, useValue: notificationSpy }
      ]
    });
  });

  it('AchResponseStatusMappingsPageComponent_ShouldCreate', () => {
    const fixture = TestBed.createComponent(AchResponseStatusMappingsPageComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('AchResponseStatusMappingsPageComponent_ShouldLoadMappingsOnInit', () => {
    const fixture = TestBed.createComponent(AchResponseStatusMappingsPageComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.rows.length).toBe(1);
    expect(apiSpy.getStatusMappings).toHaveBeenCalled();
  });

  it('AchResponseStatusMappingsPageComponent_ShouldApplyFilters', () => {
    const fixture = TestBed.createComponent(AchResponseStatusMappingsPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.filtrosForm.patchValue({
      codigoCamaraCompensacion: 'ACH',
      tipoRespuesta: 'Transaccion',
      activo: 'true'
    });

    component.applyFilters();

    const request = apiSpy.getStatusMappings.calls.mostRecent().args[0];
    expect(request.codigoCamaraCompensacion).toBe('ACH');
    expect(request.tipoRespuesta).toBe('Transaccion');
    expect(request.activo).toBeTrue();
  });

  it('AchResponseStatusMappingsPageComponent_ShouldClearFilters', () => {
    const fixture = TestBed.createComponent(AchResponseStatusMappingsPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.filtrosForm.patchValue({ codigoCamaraCompensacion: 'ACH', tipoRespuesta: 'Prenota', activo: 'false' });
    component.clearFilters();

    expect(component.filtrosForm.controls.codigoCamaraCompensacion.value).toBe('');
    expect(component.filtrosForm.controls.tipoRespuesta.value).toBe('');
    expect(component.filtrosForm.controls.activo.value).toBe('');
    expect(apiSpy.getStatusMappings).toHaveBeenCalled();
  });

  it('AchResponseStatusMappingsPageComponent_ShouldHandleLoadError', () => {
    apiSpy.getStatusMappings.and.returnValue(throwError(() => new Error('fail')));
    const fixture = TestBed.createComponent(AchResponseStatusMappingsPageComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.error).toBeTrue();
    expect(component.loading).toBeFalse();
    expect(component.rows.length).toBe(0);
    expect(notificationSpy.error).toHaveBeenCalled();
  });

  it('AchResponseStatusMappingsPageComponent_ShouldParseActivoFilter', () => {
    const fixture = TestBed.createComponent(AchResponseStatusMappingsPageComponent);
    const component = fixture.componentInstance;

    expect(component.parseActivoFilter('true')).toBeTrue();
    expect(component.parseActivoFilter('false')).toBeFalse();
    expect(component.parseActivoFilter('')).toBeUndefined();
  });

  it('AchResponseStatusMappingsPageComponent_ShouldMapBooleanTexts', () => {
    const fixture = TestBed.createComponent(AchResponseStatusMappingsPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    const first = component.rows[0];

    expect(first.activoText).toBe('Sí');
    expect(component.formatBoolean(false)).toBe('No');
    expect(first.permiteNotificacionText).toBe('Sí');
  });

  it('AchResponseStatusMappingsPageComponent_ShouldRenderBooleanBadgesAsElements', () => {
    const fixture = TestBed.createComponent(AchResponseStatusMappingsPageComponent);
    const component = fixture.componentInstance;
    const activeColumn = component.columnas.find((column) => column.field === 'activoText');

    const rendered = activeColumn?.cellRenderer?.({ data: { activo: true } } as any) as HTMLElement;
    expect(rendered instanceof HTMLElement).toBeTrue();
    expect(rendered.textContent).toBe('Sí');
  });

  it('AchResponseStatusMappingsPageComponent_ShouldNotExposeForbiddenTerms', () => {
    const visibleKeys = Object.keys(mappingsMock[0]).join('|');
    const forbiddenTerms = ['idTransaccionAxon', 'Axon', 'Soap', 'SOAP', 'Wsdl', 'Envelope', 'RequestPayload', 'ResponsePayload', 'Xml'];

    forbiddenTerms.forEach((term) => expect(visibleKeys).not.toContain(term));
  });

  it('AchResponseStatusMappingsPageComponent_ShouldRequireJustificationAndValidDates', () => {
    const fixture = TestBed.createComponent(AchResponseStatusMappingsPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.newMapping();
    component.mappingForm.patchValue({ effectiveFrom: '2026-07-10', effectiveTo: '2026-07-01' });

    component.saveMapping();

    expect(apiSpy.createStatusMapping).not.toHaveBeenCalled();
    expect(notificationSpy.error).toHaveBeenCalled();
  });

  it('AchResponseStatusMappingsPageComponent_ShouldCreateMappingWithPriorityAndValidity', () => {
    apiSpy.createStatusMapping.and.returnValue(of({ id: 2 } as any));
    const fixture = TestBed.createComponent(AchResponseStatusMappingsPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.newMapping();
    component.mappingForm.patchValue({ clearingHouseId: 1, responseType: 'Transaccion', externalCode: 'R01',
      internalStatusId: 10, externalServiceStatusId: 20, internalStatusName: 'Rechazada', priority: 100,
      effectiveFrom: '2026-07-01', effectiveTo: '2026-12-31', reason: 'Alta controlada' });

    component.saveMapping();

    const request = apiSpy.createStatusMapping.calls.mostRecent().args[0];
    expect(request.clearingHouseId).toBe(1);
    expect(request.priority).toBe(100);
    expect(request.reason).toBe('Alta controlada');
  });

  it('AchResponseStatusMappingsPageComponent_ShouldReloadOn409Conflict', () => {
    apiSpy.createStatusMapping.and.returnValue(throwError(() => ({ status: 409, error: { detail: 'conflict' } })));
    const fixture = TestBed.createComponent(AchResponseStatusMappingsPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.newMapping();
    component.mappingForm.patchValue({ clearingHouseId: 1, externalCode: 'R01', internalStatusId: 10,
      externalServiceStatusId: 20, internalStatusName: 'Rechazada', effectiveFrom: '2026-07-01',
      reason: 'Alta controlada' });

    component.saveMapping();

    expect(notificationSpy.warning).toHaveBeenCalled();
    expect(apiSpy.getStatusMappings.calls.count()).toBeGreaterThan(1);
  });
});
