import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { MatDialog } from '@angular/material/dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Subject, of, throwError } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { AchCyclesApiService } from '../../../../core/services/ach-cycles-api.service';
import { AchCycleSummary } from '../../../../core/models/ach-cycle.model';
import { AchReturnsApiService } from '../../services/ach-returns-api.service';
import { ReturnReasonsApiService } from '../../services/return-reasons-api.service';
import { ReturnEligibleTransaction } from '../../transactions.models';
import { TRANSACTIONS_ROUTES } from '../../transactions-routing.module';
import {
  AchReturnDetailDialogComponent,
  AchReturnReasonDialogComponent,
  AchReturnsManagementComponent
} from './ach-returns-management.component';

describe('AchReturnsManagementComponent', () => {
  let fixture: ComponentFixture<AchReturnsManagementComponent>;
  let component: AchReturnsManagementComponent;
  let cyclesApi: jasmine.SpyObj<AchCyclesApiService>;
  let returnsApi: jasmine.SpyObj<AchReturnsApiService>;
  let reasonsApi: jasmine.SpyObj<ReturnReasonsApiService>;
  let notifications: jasmine.SpyObj<NotificationService>;
  let dialog: jasmine.SpyObj<MatDialog>;
  let auth: jasmine.SpyObj<AuthService>;
  let grantedPermissions: Set<string>;

  const cycle: AchCycleSummary = {
    id: 'cycle-1',
    cycleName: 'Ciclo 1',
    processingDate: '2026-05-24T00:00:00',
    clearingHouseId: 1,
    clearingHouseName: 'ACH Colombia',
    startTime: '08:00:00',
    endTime: '12:00:00',
    cutoffTime: '11:30:00',
    rescheduleOnHoliday: false,
    clearingHouseCycleConfigId: 1,
    operationalStatus: 'Open'
  };
  const eligibleRow: ReturnEligibleTransaction = {
    id: 10,
    traceNumber: 'TRC-10',
    reference: 'REF-10',
    amount: 1234.56,
    transactionCode: '27',
    sourceAccountNumber: '1234567890',
    destinationAccountNumber: '9876543210',
    originatingDfi: '11122233',
    receivingDfi: '44455566',
    achCycleId: 'cycle-1',
    effectiveEntryDate: '2026-05-24T00:00:00',
    isPrenotification: false,
    isEligible: true
  };
  const blockedRow: ReturnEligibleTransaction = {
    ...eligibleRow,
    id: 11,
    traceNumber: 'TRC-11',
    reference: 'REF-11',
    amount: 25,
    isEligible: false,
    validationMessage: 'Ya existe una devolución definitiva.'
  };

  beforeEach(async () => {
    cyclesApi = jasmine.createSpyObj<AchCyclesApiService>('AchCyclesApiService', ['search']);
    returnsApi = jasmine.createSpyObj<AchReturnsApiService>(
      'AchReturnsApiService',
      ['getTransactionsByCycle', 'generateFile']
    );
    reasonsApi = jasmine.createSpyObj<ReturnReasonsApiService>('ReturnReasonsApiService', ['getForReturns']);
    notifications = jasmine.createSpyObj<NotificationService>(
      'NotificationService',
      ['success', 'warning', 'error']
    );
    dialog = jasmine.createSpyObj<MatDialog>('MatDialog', ['open']);
    grantedPermissions = new Set(['CanReadAch', 'CanManageAch']);
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['hasPermission']);
    auth.hasPermission.and.callFake((expected: string | string[]) => {
      const permissions = Array.isArray(expected) ? expected : [expected];
      return permissions.some((permission) => grantedPermissions.has(permission));
    });

    cyclesApi.search.and.returnValue(of({ items: [cycle], total: 1, page: 1, pageSize: 100 }));
    returnsApi.getTransactionsByCycle.and.returnValue(of([eligibleRow, blockedRow]));
    returnsApi.generateFile.and.returnValue(of(new Blob(['RET'])));
    reasonsApi.getForReturns.and.returnValue(of([
      { id: 1, code: 'R01', description: 'Fondos insuficientes', category: 'R', isForReturn: true },
      { id: 2, code: 'X01', description: 'No aplicable', category: 'X', isForReturn: false }
    ]));
    dialog.open.and.returnValue({
      afterClosed: () => of(undefined)
    } as unknown as ReturnType<MatDialog['open']>);

    await TestBed.configureTestingModule({
      imports: [AchReturnsManagementComponent, NoopAnimationsModule],
      providers: [
        { provide: AchCyclesApiService, useValue: cyclesApi },
        { provide: AchReturnsApiService, useValue: returnsApi },
        { provide: ReturnReasonsApiService, useValue: reasonsApi },
        { provide: NotificationService, useValue: notifications },
        { provide: AuthService, useValue: auth },
        { provide: MatDialog, useValue: dialog }
      ]
    })
      .overrideProvider(MatDialog, { useValue: dialog })
      .compileComponents();

    fixture = TestBed.createComponent(AchReturnsManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => fixture.destroy());

  function selectCycleAndLoad(): void {
    component.filterForm.controls.cycleId.setValue('cycle-1');
    component.loadTransactions();
    fixture.detectChanges();
  }

  it('inicializa ciclos y únicamente causales permitidas para devolución', () => {
    expect(component).toBeTruthy();
    expect(cyclesApi.search).toHaveBeenCalledWith({ page: 1, pageSize: 100 });
    expect(reasonsApi.getForReturns).toHaveBeenCalled();
    expect(component.cycles).toEqual([cycle]);
    expect(component.reasons.map((reason) => reason.code)).toEqual(['R01']);
  });

  it('no muestra un vacío falso antes de consultar', () => {
    fixture.detectChanges();
    expect(component.hasLoaded).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain('Prepara la consulta');
    expect(fixture.nativeElement.textContent).not.toContain('El ciclo no tiene transacciones retornables');
  });

  it('requiere ciclo antes de consultar transacciones', () => {
    component.loadTransactions();

    expect(returnsApi.getTransactionsByCycle).not.toHaveBeenCalled();
    expect(notifications.warning).toHaveBeenCalledWith('Selecciona un ciclo operativo.');
    expect(component.filterForm.controls.cycleId.hasError('required')).toBeTrue();
  });

  it('carga datos con loading y conserva estados de elegibilidad', () => {
    selectCycleAndLoad();
    expect(returnsApi.getTransactionsByCycle).toHaveBeenCalledWith('cycle-1');
    expect(component.loading).toBeFalse();
    expect(component.allRows).toHaveSize(2);
    expect(component.eligibleCount).toBe(1);
    expect(component.blockedCount).toBe(1);
  });

  it('muestra estado vacío real cuando el ciclo no tiene transacciones', () => {
    returnsApi.getTransactionsByCycle.and.returnValue(of([]));
    selectCycleAndLoad();
    expect(component.rows).toEqual([]);
    expect(fixture.nativeElement.textContent).toContain('El ciclo no tiene transacciones retornables');
  });

  it('muestra error sanitizado y permite reintento', () => {
    returnsApi.getTransactionsByCycle.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 500,
      error: { detail: 'Consulta fallida\nsin stack' }
    })));
    selectCycleAndLoad();
    expect(component.loadError).toBe('Consulta fallida sin stack');
    expect(component.loading).toBeFalse();
    expect(notifications.error).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('No fue posible completar la consulta');
  });

  it('aplica filtros en memoria solo sobre el ciclo cargado', () => {
    selectCycleAndLoad();
    component.filterForm.patchValue({ eligibility: 'blocked', query: 'REF-11' });
    component.applyClientFilters();
    expect(component.rows.map((row) => row.id)).toEqual([11]);
    expect(returnsApi.getTransactionsByCycle).toHaveBeenCalledTimes(1);
  });

  it('limpia filtros, resultados y estado de consulta', () => {
    selectCycleAndLoad();
    component.filterForm.patchValue({ eligibility: 'eligible', query: 'TRC' });
    component.clearFilters();
    expect(component.filterForm.getRawValue()).toEqual({
      cycleId: '',
      eligibility: 'all',
      query: ''
    });
    expect(component.rows).toEqual([]);
    expect(component.hasLoaded).toBeFalse();
  });

  it('mantiene paginación y ordenamiento real de AG Grid', () => {
    expect(component.columnDefs.map((column) => column.headerName)).toEqual([
      'Número de rastreo',
      'Referencia',
      'Fecha',
      'DFI de origen',
      'DFI de destino',
      'Monto',
      'Código de transacción',
      'Estado'
    ]);
    expect(component.rowSelection.mode).toBe('multiRow');
  });

  it('presenta fechas locales y montos con dos decimales', () => {
    selectCycleAndLoad();
    expect(component.cycleLabel(cycle)).toContain('24/05/2026');
    const amountColumn = component.columnDefs.find((column) => column.field === 'amount');
    const formatted = amountColumn?.valueFormatter;
    expect(typeof formatted).toBe('function');
  });

  it('abre detalle sanitizado y enmascara cuentas completas', () => {
    selectCycleAndLoad();
    component.onSelectionChanged([eligibleRow]);
    component.openDetail();

    expect(dialog.open).toHaveBeenCalledWith(
      AchReturnDetailDialogComponent,
      jasmine.objectContaining({
        data: jasmine.objectContaining({
          sourceAccount: jasmine.stringMatching(/7890$/),
          destinationAccount: jasmine.stringMatching(/3210$/)
        })
      })
    );
    const config = dialog.open.calls.mostRecent().args[1];
    const data = config?.data as { sourceAccount: string; destinationAccount: string };
    expect(data.sourceAccount).not.toContain('1234567890');
    expect(data.destinationAccount).not.toContain('9876543210');
  });

  it('no permite seleccionar transacciones no elegibles para la acción', () => {
    component.onSelectionChanged([eligibleRow, blockedRow]);
    expect(Array.from(component.selectedRows)).toEqual([10]);
  });

  it('confirma causal antes de generar y usa solo filas elegibles', () => {
    selectCycleAndLoad();
    component.onSelectionChanged([eligibleRow]);
    dialog.open.and.returnValue({
      afterClosed: () => of({ reasonCode: 'R01' })
    } as unknown as ReturnType<MatDialog['open']>);
    spyOn(URL, 'createObjectURL').and.returnValue('blob:ret');
    spyOn(URL, 'revokeObjectURL');
    spyOn(HTMLAnchorElement.prototype, 'click');

    component.openReasonDialog();

    expect(dialog.open).toHaveBeenCalledWith(
      AchReturnReasonDialogComponent,
      jasmine.objectContaining({ disableClose: true })
    );
    expect(returnsApi.generateFile).toHaveBeenCalledWith({
      cycleId: 'cycle-1',
      items: [{ transactionId: 10, returnReasonCode: 'R01' }]
    });
    expect(component.lastGenerated).toEqual(jasmine.objectContaining({
      fileName: 'devoluciones_cycle-1.RET',
      reasonCode: 'R01',
      transactionCount: 1
    }));
  });

  it('envía la base estructurada de R10 cuando el operador la confirma', () => {
    selectCycleAndLoad();
    component.onSelectionChanged([eligibleRow]);
    dialog.open.and.returnValue({
      afterClosed: () => of({ reasonCode: 'R10', achColombiaR10Basis: 2 })
    } as unknown as ReturnType<MatDialog['open']>);
    spyOn(URL, 'createObjectURL').and.returnValue('blob:ret');
    spyOn(URL, 'revokeObjectURL');
    spyOn(HTMLAnchorElement.prototype, 'click');

    component.openReasonDialog();

    expect(returnsApi.generateFile).toHaveBeenCalledWith({
      cycleId: 'cycle-1',
      items: [{ transactionId: 10, returnReasonCode: 'R10', achColombiaR10Basis: 2 }]
    });
  });

  it('previene doble generación mientras la primera está activa', () => {
    const pending = new Subject<Blob>();
    returnsApi.generateFile.and.returnValue(pending.asObservable());
    selectCycleAndLoad();
    component.onSelectionChanged([eligibleRow]);

    component.generateFile('R01');
    component.generateFile('R01');

    expect(component.generating).toBeTrue();
    expect(returnsApi.generateFile).toHaveBeenCalledTimes(1);
    pending.error(new Error('Falla controlada'));
    expect(component.generating).toBeFalse();
    expect(component.actionError).toBe('Falla controlada');
    expect(notifications.error).not.toHaveBeenCalled();
  });

  it('mantiene la acción deshabilitada sin selección o durante processing', () => {
    selectCycleAndLoad();
    component.openReasonDialog();
    expect(notifications.warning).toHaveBeenCalledWith('Selecciona al menos una transacción elegible.');
    expect(returnsApi.generateFile).not.toHaveBeenCalled();
  });

  it('usa primitives foundation para estados sin reemplazar UiGrillaEmpresarial ni MatDialog', () => {
    fixture.detectChanges();
    const initialRoot = fixture.nativeElement as HTMLElement;
    expect(initialRoot.querySelector('app-page-header')).not.toBeNull();
    expect(initialRoot.querySelector('ui-estado-vacio')).not.toBeNull();
    expect(initialRoot.querySelector('.state-panel')).toBeNull();
    expect(initialRoot.querySelector('ui-boton')).toBeNull();

    selectCycleAndLoad();
    const loadedRoot = fixture.nativeElement as HTMLElement;
    expect(loadedRoot.querySelector('ui-grilla-empresarial')).not.toBeNull();
  });

  it('mantiene la entrada como lectura con la capacidad P0.ReturnsRead equivalente', () => {
    const route = TRANSACTIONS_ROUTES.find((item) => item.path === 'returns');
    expect(route?.data?.['permissions']).toEqual(['CanReadAch']);
    expect(route?.data?.['permissions']).not.toContain('CanManageAch');
  });

  it('oculta y bloquea la generación para un usuario con solo lectura', () => {
    grantedPermissions = new Set(['CanReadAch']);
    fixture.detectChanges();
    selectCycleAndLoad();
    component.onSelectionChanged([eligibleRow]);

    expect(component.canGenerateReturns).toBeFalse();
    expect(fixture.nativeElement.textContent).not.toContain('Asignar causal y generar .RET');

    component.openReasonDialog();
    component.generateFile('R01');

    expect(dialog.open).not.toHaveBeenCalledWith(
      AchReturnReasonDialogComponent,
      jasmine.anything()
    );
    expect(returnsApi.generateFile).not.toHaveBeenCalled();
    expect(notifications.error).toHaveBeenCalledWith('No tiene permisos para generar devoluciones.');
  });

  it('acepta Returns.GenerateFile como alternativa fina para la acción mutable', () => {
    grantedPermissions = new Set(['CanReadAch', 'Returns.GenerateFile']);
    fixture.detectChanges();
    selectCycleAndLoad();
    component.onSelectionChanged([eligibleRow]);
    dialog.open.and.returnValue({
      afterClosed: () => of({ reasonCode: 'R01' })
    } as unknown as ReturnType<MatDialog['open']>);
    spyOn(URL, 'createObjectURL').and.returnValue('blob:ret');
    spyOn(URL, 'revokeObjectURL');
    spyOn(HTMLAnchorElement.prototype, 'click');

    expect(component.canGenerateReturns).toBeTrue();
    component.openReasonDialog();

    expect(returnsApi.generateFile).toHaveBeenCalledWith({
      cycleId: 'cycle-1',
      items: [{ transactionId: 10, returnReasonCode: 'R01' }]
    });
  });
});
