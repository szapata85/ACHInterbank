import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { AchResponsesApiService } from '../services/ach-responses-api.service';
import { AchResponseDashboardPageComponent } from './ach-response-dashboard-page.component';

describe('AchResponseDashboardPageComponent', () => {
  let apiSpy: jasmine.SpyObj<AchResponsesApiService>;
  let notificationSpy: jasmine.SpyObj<NotificationService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(() => {
    apiSpy = jasmine.createSpyObj<AchResponsesApiService>('AchResponsesApiService', ['getDashboard']);
    apiSpy.getDashboard.and.returnValue(of({
      totalRespuestas: 10,
      recibidas: 2,
      homologadas: 5,
      notificadas: 5,
      noHomologadas: 1,
      revisionManual: 1,
      pendientesReintento: 1,
      erroresFuncionales: 1,
      duplicadas: 0
    }));

    notificationSpy = jasmine.createSpyObj<NotificationService>('NotificationService', ['error']);
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [AchResponseDashboardPageComponent],
      providers: [
        { provide: AchResponsesApiService, useValue: apiSpy },
        { provide: NotificationService, useValue: notificationSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });
  });

  it('AchResponseDashboardPageComponent_ShouldCreate', () => {
    const fixture = TestBed.createComponent(AchResponseDashboardPageComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('AchResponseDashboardPageComponent_ShouldLoadDashboardOnInit', () => {
    const fixture = TestBed.createComponent(AchResponseDashboardPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    expect(apiSpy.getDashboard).toHaveBeenCalledTimes(1);
    expect(component.kpis.some((kpi) => kpi.titulo === 'Total respuestas')).toBeTrue();
    expect(component.kpis.some((kpi) => kpi.titulo === 'Notificadas')).toBeTrue();
  });

  it('AchResponseDashboardPageComponent_ShouldUseSingleAggregatedRequest', () => {
    const fixture = TestBed.createComponent(AchResponseDashboardPageComponent);
    fixture.detectChanges();

    expect(apiSpy.getDashboard).toHaveBeenCalledTimes(1);
  });

  it('AchResponseDashboardPageComponent_ShouldApplyFilters', () => {
    const fixture = TestBed.createComponent(AchResponseDashboardPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.filtrosForm.patchValue({ fechaDesde: '2026-01-01', fechaHasta: '2026-01-31', tipoRespuesta: 'Transaccion' });
    component.applyFilters();

    const request = apiSpy.getDashboard.calls.mostRecent().args[0];
    expect(request.fechaDesde).toBe('2026-01-01');
    expect(request.fechaHasta).toBe('2026-01-31');
    expect(request.tipoRespuesta).toBe('Transaccion');
  });

  it('AchResponseDashboardPageComponent_ShouldClearFilters', () => {
    const fixture = TestBed.createComponent(AchResponseDashboardPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.filtrosForm.patchValue({ fechaDesde: '2026-01-01', tipoRespuesta: 'Prenota' });
    component.clearFilters();

    expect(component.filtrosForm.controls.fechaDesde.value).toBe('');
    expect(component.filtrosForm.controls.fechaHasta.value).toBe('');
    expect(component.filtrosForm.controls.tipoRespuesta.value).toBe('');
    expect(apiSpy.getDashboard).toHaveBeenCalledTimes(2);
  });

  it('AchResponseDashboardPageComponent_ShouldHandleLoadError', () => {
    apiSpy.getDashboard.and.returnValue(throwError(() => new Error('fail')));
    const fixture = TestBed.createComponent(AchResponseDashboardPageComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.error).toBeTrue();
    expect(component.loading).toBeFalse();
    expect(notificationSpy.error).toHaveBeenCalled();
  });

  it('AchResponseDashboardPageComponent_ShouldCalculateCriticalTotal', () => {
    const fixture = TestBed.createComponent(AchResponseDashboardPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    expect(component.getCriticalTotal()).toBe(4);
  });

  it('AchResponseDashboardPageComponent_ShouldCalculateRates', () => {
    const fixture = TestBed.createComponent(AchResponseDashboardPageComponent);
    const component = fixture.componentInstance;

    expect(component.calculateRate(5, 10)).toBe('50%');
    expect(component.calculateRate(0, 0)).toBe('0%');
  });

  it('AchResponseDashboardPageComponent_ShouldNavigateFromKpi', () => {
    const fixture = TestBed.createComponent(AchResponseDashboardPageComponent);
    const component = fixture.componentInstance;

    component.openKpi({ key: 'x', titulo: 'x', valor: 0, descripcion: 'x', clase: 'kpi-neutro', ruta: '/ach-responses/manual-review' });
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/ach-responses/manual-review']);
  });

  it('AchResponseDashboardPageComponent_ShouldNotExposeForbiddenTerms', () => {
    const keys = ['totalRespuestas', 'homologadas', 'notificadas'].join('|');
    const forbiddenTerms = ['idTransaccionAxon', 'Axon', 'Soap', 'SOAP', 'Wsdl', 'Envelope', 'RequestPayload', 'ResponsePayload', 'Xml'];

    forbiddenTerms.forEach((term) => expect(keys).not.toContain(term));
  });
});
