import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { By } from '@angular/platform-browser';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { AchResponsesApiService } from '../services/ach-responses-api.service';
import { AchResponseListPageComponent } from './ach-response-list-page.component';

describe('AchResponseListPageComponent', () => {
  let apiSpy: jasmine.SpyObj<AchResponsesApiService>;
  let notificationsSpy: jasmine.SpyObj<NotificationService>;
  let routerSpy: jasmine.SpyObj<Router>;

  const response = {
    items: [
      {
        id: '1',
        tipoRespuesta: 'Transaccion',
        idTransaccion: 'TX-1',
        codigoCamaraCompensacion: 'ACH',
        codigoEstadoExterno: '00',
        estadoProcesamiento: 'Recibida',
        permiteNotificacion: true,
        fechaRecepcion: '2026-05-01T00:00:00Z',
        fechaCreacion: '2026-05-01T00:00:00Z'
      }
    ],
    pageNumber: 1,
    pageSize: 20,
    totalCount: 1,
    totalPages: 1
  } as any;

  beforeEach(() => {
    apiSpy = jasmine.createSpyObj<AchResponsesApiService>('AchResponsesApiService', ['search']);
    apiSpy.search.and.returnValue(of(response));
    notificationsSpy = jasmine.createSpyObj<NotificationService>('NotificationService', ['error']);
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [AchResponseListPageComponent],
      providers: [
        { provide: AchResponsesApiService, useValue: apiSpy },
        { provide: NotificationService, useValue: notificationsSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });
  });

  it('AchResponseListPageComponent_ShouldCreate', () => {
    const fixture = TestBed.createComponent(AchResponseListPageComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('AchResponseListPageComponent_ShouldLoadResponsesIntoEnterpriseGridOnInit', () => {
    const fixture = TestBed.createComponent(AchResponseListPageComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    const grid = fixture.debugElement.query(By.css('ui-grilla-empresarial'));

    expect(component.rows.length).toBe(1);
    expect(component.totalCount).toBe(1);
    expect(grid).toBeTruthy();
    expect(grid.componentInstance.datos.length).toBe(1);
  });

  it('AchResponseListPageComponent_ShouldPreserveFiltersAndRequestPagination', () => {
    const fixture = TestBed.createComponent(AchResponseListPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.filtrosForm.patchValue({
      fechaDesde: '2026-01-01',
      fechaHasta: '2026-01-31',
      tipoRespuesta: 'Transaccion',
      idTransaccion: 'TX-1',
      codigoCamaraCompensacion: 'ACH',
      codigoEntidadOrigen: '001',
      codigoEntidadDestino: '002',
      codigoEstadoExterno: '00',
      estadoProcesamiento: 'Homologada',
      correlationId: 'corr-1',
      pageNumber: 4,
      pageSize: 50
    });

    component.applyFilters();

    expect(apiSpy.search.calls.mostRecent().args[0]).toEqual({
      fechaDesde: '2026-01-01',
      fechaHasta: '2026-01-31',
      tipoRespuesta: 'Transaccion',
      idTransaccion: 'TX-1',
      codigoCamaraCompensacion: 'ACH',
      codigoEntidadOrigen: '001',
      codigoEntidadDestino: '002',
      codigoEstadoExterno: '00',
      estadoProcesamiento: 'Homologada',
      correlationId: 'corr-1',
      pageNumber: 1,
      pageSize: 50
    });
  });

  it('AchResponseListPageComponent_ShouldClearFilters', () => {
    const fixture = TestBed.createComponent(AchResponseListPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.filtrosForm.patchValue({ idTransaccion: 'X', pageNumber: 4, pageSize: 50 });
    component.clearFilters();

    expect(component.filtrosForm.controls.idTransaccion.value).toBe('');
    expect(component.filtrosForm.controls.pageNumber.value).toBe(1);
    expect(component.filtrosForm.controls.pageSize.value).toBe(20);
    expect(apiSpy.search).toHaveBeenCalled();
  });

  it('AchResponseListPageComponent_ShouldNavigateToDetail', () => {
    const fixture = TestBed.createComponent(AchResponseListPageComponent);
    const component = fixture.componentInstance;

    component.openDetail({ id: 'abc' } as any);

    expect(routerSpy.navigate).toHaveBeenCalledWith(['/ach-responses', 'abc']);
  });

  it('AchResponseListPageComponent_ShouldHandleSearchError', () => {
    apiSpy.search.and.returnValue(throwError(() => new Error('fail')));
    const fixture = TestBed.createComponent(AchResponseListPageComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.error).toBeTrue();
    expect(component.loading).toBeFalse();
    expect(component.rows.length).toBe(0);
  });

  it('AchResponseListPageComponent_ShouldBuildSearchRequestWithoutEmptyStrings', () => {
    const fixture = TestBed.createComponent(AchResponseListPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.filtrosForm.patchValue({
      idTransaccion: 'TX-1',
      codigoEntidadOrigen: ' ',
      correlationId: ''
    });
    component.applyFilters();

    const requestArg = apiSpy.search.calls.mostRecent().args[0];
    expect(requestArg.idTransaccion).toBe('TX-1');
    expect(requestArg.codigoEntidadOrigen).toBeUndefined();
    expect(requestArg.correlationId).toBeUndefined();
  });

  it('AchResponseListPageComponent_ShouldMoveToNextAndPreviousPageWithSamePageSize', () => {
    const fixture = TestBed.createComponent(AchResponseListPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.totalPages = 3;
    component.filtrosForm.patchValue({ pageNumber: 1, pageSize: 20 });
    apiSpy.search.calls.reset();

    component.nextPage();
    expect(component.filtrosForm.controls.pageNumber.value).toBe(2);
    expect(apiSpy.search.calls.mostRecent().args[0].pageNumber).toBe(2);
    expect(apiSpy.search.calls.mostRecent().args[0].pageSize).toBe(20);

    component.previousPage();
    expect(component.filtrosForm.controls.pageNumber.value).toBe(1);
    expect(apiSpy.search.calls.mostRecent().args[0].pageNumber).toBe(1);
    expect(apiSpy.search.calls.mostRecent().args[0].pageSize).toBe(20);
  });

  it('AchResponseListPageComponent_ShouldResetPageWhenPageSizeChanges', () => {
    const fixture = TestBed.createComponent(AchResponseListPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.filtrosForm.patchValue({ pageNumber: 3 });
    component.setPageSize(50);

    const request = apiSpy.search.calls.mostRecent().args[0];
    expect(request.pageNumber).toBe(1);
    expect(request.pageSize).toBe(50);
  });

  it('AchResponseListPageComponent_ShouldExposeFechaCreacionColumn', () => {
    const fixture = TestBed.createComponent(AchResponseListPageComponent);
    const component = fixture.componentInstance;

    expect(component.columnas.some((column) => column.field === 'fechaCreacionText')).toBeTrue();
  });

  it('AchResponseListPageComponent_ShouldRenderProcessingStatusWithoutHtmlString', () => {
    const fixture = TestBed.createComponent(AchResponseListPageComponent);
    const component = fixture.componentInstance;
    const statusColumn = component.columnas.find((column) => column.field === 'estadoProcesamiento');

    const rendered = statusColumn?.cellRenderer?.({ value: 'Homologada' } as any) as HTMLElement;

    expect(rendered).toBeTruthy();
    expect(rendered instanceof HTMLElement).toBeTrue();
    expect(rendered.textContent).toBe('Homologada');
  });

  it('AchResponseListPageComponent_ShouldRenderDetailActionAsButtonElement', () => {
    const fixture = TestBed.createComponent(AchResponseListPageComponent);
    const component = fixture.componentInstance;
    const actionColumn = component.columnas.find((column) => column.headerName === 'Acciones');

    const rendered = actionColumn?.cellRenderer?.({} as any) as HTMLButtonElement;

    expect(rendered).toBeTruthy();
    expect(rendered instanceof HTMLButtonElement).toBeTrue();
    expect(rendered.type).toBe('button');
    expect(rendered.textContent).toBe('Detalle');
    expect(rendered.dataset['action']).toBe('detalle');
  });
});
