import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { OutgoingTransactionMonitoringApiService } from './outgoing-transaction-monitoring-api.service';
import { OutgoingTransactionMonitoringListComponent } from './outgoing-transaction-monitoring-list.component';

describe('OutgoingTransactionMonitoringListComponent', () => {
  let fixture: ComponentFixture<OutgoingTransactionMonitoringListComponent>;
  let component: OutgoingTransactionMonitoringListComponent;
  let api: jasmine.SpyObj<OutgoingTransactionMonitoringApiService>;

  beforeEach(async () => {
    sessionStorage.clear();
    api = jasmine.createSpyObj('OutgoingTransactionMonitoringApiService', ['search', 'getClearingHouses', 'getDestinationInstitutions']);
    api.search.and.returnValue(of({ items: [], pageNumber: 1, pageSize: 25, totalItems: 0, totalPages: 0, hasPreviousPage: false, hasNextPage: false }));
    api.getClearingHouses.and.returnValue(of([]));
    api.getDestinationInstitutions.and.returnValue(of([]));
    await TestBed.configureTestingModule({
      imports: [OutgoingTransactionMonitoringListComponent],
      providers: [
        provideNoopAnimations(), provideRouter([]),
        { provide: OutgoingTransactionMonitoringApiService, useValue: api }
      ]
    }).compileComponents();
    fixture = TestBed.createComponent(OutgoingTransactionMonitoringListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('carga inicialmente los últimos siete días con paginación del servidor', () => {
    expect(api.search).toHaveBeenCalledTimes(1);
    const query = api.search.calls.mostRecent().args[0];
    expect(query.pageNumber).toBe(1);
    expect(query.pageSize).toBe(25);
    expect(query.sortBy).toBe('createdAt');
    expect(query.fromUtc).toBeTruthy();
    expect(query.toUtc).toBeTruthy();
  });

  it('limpia filtros y conserva el rango inicial útil', () => {
    component.form.patchValue({ transactionExternalId: 'TX-001', processStatus: 'Processed' });
    component.clearFilters();
    expect(component.form.controls.transactionExternalId.value).toBeNull();
    expect(component.form.controls.fromDate.value).toBeTruthy();
    expect(api.search).toHaveBeenCalledTimes(2);
  });

  it('solo expone la acción Ver detalle', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).not.toContain('Reprocesar');
    expect(text).not.toContain('Aprobar');
    expect(text).not.toContain('Rechazar');
  });

  it('navega al detalle por una ruta recargable', () => {
    const router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);
    component.viewDetail({ id: 9, transactionExternalId: 'TX-9' } as never);
    expect(router.navigate).toHaveBeenCalledWith(['/transactions/outgoing-monitoring', 9]);
  });

  it('impide consultar rangos de fechas o importes inválidos', () => {
    const calls = api.search.calls.count();
    component.form.patchValue({
      fromDate: new Date('2026-08-10'),
      toDate: new Date('2026-08-01'),
      minimumAmount: 200,
      maximumAmount: 100
    });

    component.search();

    expect(component.form.hasError('dateOrder')).toBeTrue();
    expect(component.form.hasError('amountOrder')).toBeTrue();
    expect(api.search.calls.count()).toBe(calls);
  });

  it('vuelve a la primera página al aplicar filtros y conserva paginación del servidor', () => {
    component.pageChanged({ pageIndex: 2, pageSize: 50, length: 120 });
    expect(api.search.calls.mostRecent().args[0].pageNumber).toBe(3);
    expect(api.search.calls.mostRecent().args[0].pageSize).toBe(50);

    component.form.patchValue({ transactionExternalId: '  TX-001  ' });
    component.search();

    const query = api.search.calls.mostRecent().args[0];
    expect(query.pageNumber).toBe(1);
    expect(query.transactionExternalId).toBe('TX-001');
  });

  it('normaliza y envia el codigo de respuesta al servidor', () => {
    component.form.patchValue({ responseCode: '  r01  ' });

    component.search();

    expect(api.search.calls.mostRecent().args[0].responseCode).toBe('R01');
  });

  it('restaura los filtros al regresar del detalle', async () => {
    sessionStorage.setItem('outgoing-monitoring-filters', JSON.stringify({
      transactionExternalId: 'TX-GUARDADA',
      fromDate: '2026-08-01T00:00:00.000Z',
      toDate: '2026-08-02T00:00:00.000Z'
    }));
    fixture.destroy();
    fixture = TestBed.createComponent(OutgoingTransactionMonitoringListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.form.controls.transactionExternalId.value).toBe('TX-GUARDADA');
    expect(component.form.controls.fromDate.value instanceof Date).toBeTrue();
  });
});
