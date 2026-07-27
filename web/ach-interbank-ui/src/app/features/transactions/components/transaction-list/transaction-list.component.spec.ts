import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { NotificationService } from '../../../../core/services/notification.service';
import { AchCyclesApiService, ClearingHousesApiService } from '../../../ach-cycles/services/ach-cycles-api.service';
import { TransactionsApiService } from '../../services/transactions-api.service';
import { TransactionListComponent } from './transaction-list.component';

describe('TransactionListComponent', () => {
  let component: TransactionListComponent;
  let fixture: ComponentFixture<TransactionListComponent>;
  let transactionsApi: jasmine.SpyObj<TransactionsApiService>;
  let cyclesApi: jasmine.SpyObj<AchCyclesApiService>;
  let clearingHousesApi: jasmine.SpyObj<ClearingHousesApiService>;
  let notifications: jasmine.SpyObj<NotificationService>;

  const cycleResponse = {
    items: [
      { id: 'cycle-1', cycleName: 'Ciclo operativo' },
      { id: 'cycle-2', cycleName: 'Ciclo nocturno' }
    ],
    total: 2,
    page: 1,
    pageSize: 100
  };

  beforeEach(async () => {
    transactionsApi = jasmine.createSpyObj<TransactionsApiService>(
      'TransactionsApiService',
      ['getAll', 'getIntegrationResult']
    );
    cyclesApi = jasmine.createSpyObj<AchCyclesApiService>('AchCyclesApiService', ['search']);
    clearingHousesApi = jasmine.createSpyObj<ClearingHousesApiService>('ClearingHousesApiService', ['list']);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['error', 'warning']);

    transactionsApi.getAll.and.returnValue(of([]));
    cyclesApi.search.and.returnValue(of(cycleResponse as any));
    clearingHousesApi.list.and.returnValue(of([
      { id: 7, name: 'ACH Colombia' },
      { id: 8, name: 'CENIT' }
    ] as any));

    await TestBed.configureTestingModule({
      imports: [TransactionListComponent, NoopAnimationsModule],
      providers: [
        { provide: TransactionsApiService, useValue: transactionsApi },
        { provide: AchCyclesApiService, useValue: cyclesApi },
        { provide: ClearingHousesApiService, useValue: clearingHousesApi },
        { provide: NotificationService, useValue: notifications },
        { provide: Router, useValue: jasmine.createSpyObj<Router>('Router', ['navigate']) }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TransactionListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('crea el formulario reactivo con los valores iniciales funcionales', () => {
    expect(component.filtrosForm.controls.selectedDate.value).toBeNull();
    expect(component.filtrosForm.controls.selectedClearingHouseId.value).toBeNull();
    expect(component.filtrosForm.controls.selectedCycleId.value).toBe('Ciclo operativo');
    expect(component.filtrosForm.valid).toBeTrue();
    expect(transactionsApi.getAll).toHaveBeenCalledWith({
      achCycleId: null,
      achCycleName: 'Ciclo operativo',
      effectiveDate: undefined,
      clearingHouseId: undefined
    });
  });

  it('valida una fecha inválida, marca el control y bloquea la consulta', fakeAsync(() => {
    transactionsApi.getAll.calls.reset();
    component.filtrosForm.controls.selectedDate.setValue(new Date('invalid'));

    component.applyFilters();
    fixture.detectChanges();
    tick();

    expect(component.filtrosForm.controls.selectedDate.hasError('invalidDate')).toBeTrue();
    expect(component.filtrosForm.controls.selectedDate.touched).toBeTrue();
    expect(component.searchAttempted).toBeTrue();
    expect(transactionsApi.getAll).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Ingresa una fecha válida.');
  }));

  it('conserva exactamente el contrato de parámetros en una búsqueda válida', () => {
    transactionsApi.getAll.calls.reset();
    component.filtrosForm.setValue({
      selectedCycleId: 'cycle-1',
      selectedClearingHouseId: 7,
      selectedDate: new Date(2026, 6, 27)
    }, { emitEvent: false });

    component.applyFilters();

    expect(transactionsApi.getAll).toHaveBeenCalledTimes(1);
    expect(transactionsApi.getAll).toHaveBeenCalledWith({
      achCycleId: 'cycle-1',
      achCycleName: null,
      effectiveDate: '2026-07-27',
      clearingHouseId: 7
    });
  });

  it('filtra las cámaras por texto sin convertir el texto en valor funcional', () => {
    component.clearingHouseSearchControl.setValue('cen');

    expect(component.filteredClearingHouseOptions.map((option) => option.label)).toEqual(['CENIT']);
    expect(component.filtrosForm.controls.selectedClearingHouseId.value).toBeNull();
  });

  it('filtra los ciclos por texto sin convertir el texto en valor funcional', () => {
    component.cycleSearchControl.setValue('noct');

    expect(component.filteredCycleOptions.map((option) => option.label)).toEqual(['Ciclo nocturno']);
    expect(component.filtrosForm.controls.selectedCycleId.value).toBeNull();
  });

  it('selecciona una cámara por autocompletado y conserva su ID numérico', () => {
    component.selectClearingHouse({ id: 8, label: 'CENIT' });

    expect(component.filtrosForm.controls.selectedClearingHouseId.value).toBe(8);
    expect(component.displayClearingHouseOption(component.clearingHouseSearchControl.value)).toBe('CENIT');
    expect(cyclesApi.search).toHaveBeenCalledWith(jasmine.objectContaining({ clearingHouseId: 8 }));
  });

  it('selecciona un ciclo por autocompletado y conserva su ID string', () => {
    component.selectClearingHouse({ id: 7, label: 'ACH Colombia' });
    component.selectCycle({ id: 'cycle-2', label: 'Ciclo nocturno' });

    expect(component.filtrosForm.controls.selectedCycleId.value).toBe('cycle-2');
    expect(component.displayCycleOption(component.cycleSearchControl.value)).toBe('Ciclo nocturno');
  });

  it('evita un segundo envío mientras la consulta está en curso', () => {
    const pending = new Subject<any[]>();
    transactionsApi.getAll.calls.reset();
    transactionsApi.getAll.and.returnValue(pending);

    component.applyFilters();
    component.applyFilters();

    expect(transactionsApi.getAll).toHaveBeenCalledTimes(1);
    expect(component.loading).toBeTrue();

    pending.next([]);
    pending.complete();
    expect(component.loading).toBeFalse();
  });

  it('limpia filtros, estados de interacción y restaura los valores predeterminados', () => {
    component.filtrosForm.setValue({
      selectedCycleId: 'cycle-1',
      selectedClearingHouseId: 7,
      selectedDate: new Date(2026, 6, 27)
    }, { emitEvent: false });
    component.filtrosForm.markAllAsTouched();
    component.filtrosForm.markAsDirty();
    component.clearingHouseSearchControl.setValue('texto cámara');
    component.cycleSearchControl.setValue('texto ciclo');
    component.searchAttempted = true;
    component.returnView = 'received';
    transactionsApi.getAll.calls.reset();

    component.clearFilters();

    expect(component.filtrosForm.controls.selectedDate.value).toBeNull();
    expect(component.filtrosForm.controls.selectedClearingHouseId.value).toBeNull();
    expect(component.filtrosForm.controls.selectedCycleId.value).toBe('Ciclo operativo');
    expect(component.clearingHouseSearchControl.value).toBe('');
    expect(component.cycleSearchControl.value).toBe('');
    expect(component.filtrosForm.pristine).toBeTrue();
    expect(component.filtrosForm.untouched).toBeTrue();
    expect(component.searchAttempted).toBeFalse();
    expect(component.returnView).toBe('all');
    expect(transactionsApi.getAll).toHaveBeenCalledTimes(1);
  });

  it('renderiza los filtros con Angular Material y acciones accesibles', () => {
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelectorAll('mat-form-field').length).toBe(3);
    expect(element.querySelectorAll('mat-autocomplete').length).toBe(2);
    expect(element.querySelector('input[data-testid="transaction-filter-clearing-house"]')).not.toBeNull();
    expect(element.querySelector('input[data-testid="transaction-filter-cycle"]')).not.toBeNull();
    expect(element.querySelector('input[formcontrolname="selectedDate"]')).not.toBeNull();
    expect(element.querySelector('[data-testid="transaction-search"]')?.textContent).toContain('Buscar');
    expect(element.querySelector('[data-testid="transaction-clear"]')?.textContent).toContain('Limpiar filtros');
  });

  it('presenta un error controlado y permite reintentar sin perder filtros', () => {
    transactionsApi.getAll.calls.reset();
    transactionsApi.getAll.and.returnValue(throwError(() => new Error('fallo controlado')));

    component.applyFilters();
    fixture.detectChanges();

    expect(component.loadError).toBeTrue();
    expect(component.loading).toBeFalse();
    expect(notifications.error).toHaveBeenCalledWith('No fue posible cargar las transacciones');
    expect(fixture.nativeElement.querySelector('[data-testid="transactions-error"]')).not.toBeNull();
  });
});
