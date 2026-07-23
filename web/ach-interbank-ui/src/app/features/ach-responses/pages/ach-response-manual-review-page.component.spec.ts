import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { NotificationService } from '../../../core/services/notification.service';
import { AchResponsesApiService } from '../services/ach-responses-api.service';
import { AchResponseManualReviewPageComponent } from './ach-response-manual-review-page.component';

describe('AchResponseManualReviewPageComponent', () => {
  let apiSpy: jasmine.SpyObj<AchResponsesApiService>;
  let notificationSpy: jasmine.SpyObj<NotificationService>;
  let routerSpy: jasmine.SpyObj<Router>;

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

  beforeEach(() => {
    apiSpy = jasmine.createSpyObj<AchResponsesApiService>('AchResponsesApiService', ['search', 'getOrphans', 'beginOrphanReview', 'resolveOrphan']);
    apiSpy.search.and.returnValue(of(responseMock));
    apiSpy.getOrphans.and.returnValue(of([]));
    notificationSpy = jasmine.createSpyObj<NotificationService>('NotificationService', ['error', 'success', 'warning']);
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [AchResponseManualReviewPageComponent],
      providers: [
        { provide: AchResponsesApiService, useValue: apiSpy },
        { provide: NotificationService, useValue: notificationSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });
  });

  it('AchResponseManualReviewPageComponent_ShouldCreate', () => {
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('AchResponseManualReviewPageComponent_ShouldLoadOrphansAndRequireJustification', () => {
    apiSpy.getOrphans.and.returnValue(of([{ id: 'orphan-1', version: 'v1', responseType: 'Transaccion',
      externalCode: 'R01', clearingHouseId: 1, resolutionStatus: 'Pending', orphanReason: 'Sin correlación' }] as any));
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.selectOrphan(component.orphans[0]);

    component.beginSelectedReview();

    expect(apiSpy.beginOrphanReview).not.toHaveBeenCalled();
    expect(notificationSpy.error).toHaveBeenCalled();
  });

  it('AchResponseManualReviewPageComponent_ShouldStartManualReview', () => {
    const orphan = { id: 'orphan-1', version: 'version-1', responseType: 'Transaccion', externalCode: 'R01',
      clearingHouseId: 1, resolutionStatus: 'Pending', orphanReason: 'Sin correlación' } as any;
    apiSpy.getOrphans.and.returnValue(of([orphan]));
    apiSpy.beginOrphanReview.and.returnValue(of({ ...orphan, version: 'version-2', resolutionStatus: 'InReview' }));
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.selectOrphan(orphan);
    component.resolutionForm.patchValue({ reason: 'Análisis operativo' });

    component.beginSelectedReview();

    expect(apiSpy.beginOrphanReview).toHaveBeenCalledWith('orphan-1', 'version-1', 'Análisis operativo');
    expect(notificationSpy.success).toHaveBeenCalled();
  });

  it('AchResponseManualReviewPageComponent_ShouldLoadNoHomologadaByDefault', () => {
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    fixture.detectChanges();

    const request = apiSpy.search.calls.mostRecent().args[0];
    expect(request.estadoProcesamiento).toBe('NoHomologada');
  });

  it('AchResponseManualReviewPageComponent_ShouldApplySelectedCriticalStatus', () => {
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.filtrosForm.patchValue({ estadoProcesamiento: 'ErrorFuncional' });
    component.applyFilters();

    const request = apiSpy.search.calls.mostRecent().args[0];
    expect(request.estadoProcesamiento).toBe('ErrorFuncional');
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

    expect(component.filtrosForm.controls.estadoProcesamiento.value).toBe('NoHomologada');
    expect(component.filtrosForm.controls.pageNumber.value).toBe(1);
    expect(component.filtrosForm.controls.pageSize.value).toBe(20);
  });

  it('AchResponseManualReviewPageComponent_ShouldNavigateToDetail', () => {
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    const component = fixture.componentInstance;

    component.openDetail({ id: 'resp-1' } as any);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/ach-responses', 'resp-1']);
  });

  it('AchResponseManualReviewPageComponent_ShouldHandleSearchError', () => {
    apiSpy.search.and.returnValue(throwError(() => new Error('fail')));
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.error).toBeTrue();
    expect(component.loading).toBeFalse();
    expect(component.rows.length).toBe(0);
    expect(notificationSpy.error).toHaveBeenCalled();
  });

  it('AchResponseManualReviewPageComponent_ShouldClassifyPriority', () => {
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    const component = fixture.componentInstance;

    expect(component.getPriority('NoHomologada')).toBe('Alta');
    expect(component.getPriority('ErrorFuncional')).toBe('Alta');
    expect(component.getPriority('RequiereRevisionManual')).toBe('Media');
    expect(component.getPriority('PendienteReintento')).toBe('Media');
  });

  it('AchResponseManualReviewPageComponent_ShouldRenderPriorityAsElement', () => {
    const fixture = TestBed.createComponent(AchResponseManualReviewPageComponent);
    const component = fixture.componentInstance;
    const priorityColumn = component.columnas.find((column) => column.field === 'prioridadText');

    const rendered = priorityColumn?.cellRenderer?.({ value: 'Alta' } as any) as HTMLElement;
    expect(rendered instanceof HTMLElement).toBeTrue();
    expect(rendered.textContent).toBe('Alta');
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

  it('AchResponseManualReviewPageComponent_ShouldNotExposeForbiddenTerms', () => {
    const visibleKeys = Object.keys(responseMock.items[0]).join('|');
    const forbiddenTerms = ['idTransaccionAxon', 'Axon', 'Soap', 'SOAP', 'Wsdl', 'Envelope', 'RequestPayload', 'ResponsePayload', 'Xml'];

    forbiddenTerms.forEach((term) => expect(visibleKeys).not.toContain(term));
  });
});
