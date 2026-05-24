import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../../../core/services/notification.service';
import { AchCyclesApiService } from '../../../ach-cycles/services/ach-cycles-api.service';
import { AchReturnsApiService } from '../../services/ach-returns-api.service';
import { ReturnReasonsApiService } from '../../services/return-reasons-api.service';
import { AchReturnsManagementComponent } from './ach-returns-management.component';

describe('AchReturnsManagementComponent', () => {
  let fixture: ComponentFixture<AchReturnsManagementComponent>;
  let component: AchReturnsManagementComponent;
  let cyclesApi: jasmine.SpyObj<AchCyclesApiService>;
  let returnsApi: jasmine.SpyObj<AchReturnsApiService>;
  let reasonsApi: jasmine.SpyObj<ReturnReasonsApiService>;
  let notifications: jasmine.SpyObj<NotificationService>;

  const cycle = {
    id: 'cycle-1',
    cycleName: 'Ciclo UAT',
    processingDate: '2026-05-24T00:00:00Z',
    clearingHouseId: 1,
    clearingHouseName: 'ACH Colombia',
    startTime: '08:00:00',
    endTime: '12:00:00',
    status: 'Open'
  } as any;

  beforeEach(async () => {
    cyclesApi = jasmine.createSpyObj<AchCyclesApiService>('AchCyclesApiService', ['search']);
    returnsApi = jasmine.createSpyObj<AchReturnsApiService>('AchReturnsApiService', ['getTransactionsByCycle', 'generateFile']);
    reasonsApi = jasmine.createSpyObj<ReturnReasonsApiService>('ReturnReasonsApiService', ['getForReturns']);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'warning', 'error']);

    cyclesApi.search.and.returnValue(of({ items: [cycle], total: 1, page: 1, pageSize: 100 }));
    returnsApi.getTransactionsByCycle.and.returnValue(of([]));
    returnsApi.generateFile.and.returnValue(of(new Blob(['RET'])));
    reasonsApi.getForReturns.and.returnValue(of([{ id: 1, code: 'R01', description: 'Fondos insuficientes', category: 'R', isForReturn: true }]));

    await TestBed.configureTestingModule({
      imports: [AchReturnsManagementComponent],
      providers: [
        { provide: AchCyclesApiService, useValue: cyclesApi },
        { provide: AchReturnsApiService, useValue: returnsApi },
        { provide: ReturnReasonsApiService, useValue: reasonsApi },
        { provide: NotificationService, useValue: notifications }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AchReturnsManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('TransactionsReturns_ShouldLoadData_WhenApiReturnsRows', () => {
    returnsApi.getTransactionsByCycle.and.returnValue(of([
      { id: 10, traceNumber: 'TRC-10', reference: 'REF-10', amount: 1000, transactionCode: '27', isEligible: true } as any
    ]));

    component.loadTransactions();

    expect(returnsApi.getTransactionsByCycle).toHaveBeenCalledWith('cycle-1');
    expect(component.rows.length).toBe(1);
    expect(component.loading).toBeFalse();
    expect(component.loadError).toBe('');
  });

  it('TransactionsReturns_ShouldShowEmptyState_WhenApiReturnsEmpty', () => {
    returnsApi.getTransactionsByCycle.and.returnValue(of([]));

    component.loadTransactions();
    fixture.detectChanges();

    expect(component.rows).toEqual([]);
    expect(component.loading).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain('No hay devoluciones registradas');
  });

  it('TransactionsReturns_ShouldShowError_WhenApiFails', () => {
    returnsApi.getTransactionsByCycle.and.returnValue(throwError(() => new Error('boom')));

    component.loadTransactions();
    fixture.detectChanges();

    expect(component.rows).toEqual([]);
    expect(component.loading).toBeFalse();
    expect(component.loadError).toContain('No fue posible cargar');
    expect(notifications.error).toHaveBeenCalled();
  });

  it('TransactionsReturns_ShouldClearLoading_OnSuccess', () => {
    returnsApi.getTransactionsByCycle.and.returnValue(of([]));

    component.loadTransactions();

    expect(component.loading).toBeFalse();
  });

  it('TransactionsReturns_ShouldClearLoading_OnError', () => {
    returnsApi.getTransactionsByCycle.and.returnValue(throwError(() => new Error('boom')));

    component.loadTransactions();

    expect(component.loading).toBeFalse();
  });

  it('TransactionsReturns_ShouldNotRenderBlankPage', () => {
    expect(fixture.nativeElement.textContent).toContain('Gestión de Devoluciones ACH');
  });

  it('TransactionsReturns_ShouldRenderGridColumns', () => {
    const headers = component.columnDefs.map((column) => column.headerName);

    expect(headers).toContain('Trace');
    expect(headers).toContain('Referencia');
    expect(headers).toContain('Monto');
    expect(headers).toContain('Estado');
  });

  it('TransactionsReturns_ShouldKeepActionsVisible', () => {
    expect(fixture.nativeElement.textContent).toContain('Consultar');
    expect(fixture.nativeElement.textContent).toContain('Asignar causal / Generar');
  });

  it('TransactionsReturns_ShouldNotRenderWhiteCriticalButtons', () => {
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    const criticalButtons = buttons.filter((button) => /Consultar|Asignar causal|Limpiar/.test(button.textContent ?? ''));

    expect(criticalButtons.length).toBeGreaterThan(0);
    criticalButtons.forEach((button) => {
      expect(button.textContent?.trim() || button.getAttribute('aria-label') || '').not.toBe('');
    });
  });

  it('TransactionsReturns_ShouldUseProcessingDate_WhenApiDoesNotReturnDate', () => {
    expect(component.cycleOptions[0].etiqueta).toContain('2026-05-24');
  });
});
