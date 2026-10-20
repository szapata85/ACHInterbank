import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed, fakeAsync, flush } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarRef } from '@angular/material/snack-bar';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { ClearingHousesService } from '../../core/services/clearing-houses.service';
import { OperationalConfirmDialogComponent } from '../../shared/components/operational-confirm-dialog.component';
import { ClearingHouseCycleConfigsApiService } from '../../core/services/clearing-house-cycle-configs-api.service';
import { AccountTypeEnum, TransactionTypeEnum } from '../../core/models/transaction-types.model';
import { OperationalRestrictionsComponent } from './operational-restrictions.component';
import { OperationalRestrictionsService } from './operational-restrictions.service';

describe('OperationalRestrictionsComponent', () => {
  let fixture: ComponentFixture<OperationalRestrictionsComponent>;
  let component: OperationalRestrictionsComponent;
  let api: jasmine.SpyObj<OperationalRestrictionsService>;
  let houses: jasmine.SpyObj<ClearingHousesService>;
  let cycles: jasmine.SpyObj<ClearingHouseCycleConfigsApiService>;
  let auth: jasmine.SpyObj<AuthService>;
  let dialog: MatDialog;

  beforeEach(async () => {
    api = jasmine.createSpyObj('OperationalRestrictionsService', ['list', 'get', 'create', 'update', 'activate', 'deactivate']);
    houses = jasmine.createSpyObj('ClearingHousesService', ['list']);
    cycles = jasmine.createSpyObj('ClearingHouseCycleConfigsApiService', ['getByClearingHouse']);
    auth = jasmine.createSpyObj('AuthService', ['hasPermission']);
    auth.hasPermission.and.returnValue(true);

    api.list.and.returnValue(of([restriction()]));
    api.get.and.callFake((id: number) => of({ ...restriction(), id }));
    api.create.and.returnValue(of(restriction()));
    api.update.and.returnValue(of(restriction()));
    api.activate.and.returnValue(of({ ...restriction(), isActive: true }));
    api.deactivate.and.returnValue(of({ ...restriction(), isActive: false }));
    houses.list.and.returnValue(of({ items: [house(31, 'ACHCOL'), house(77, 'CENIT')], page: 1, pageSize: 20, totalCount: 2, totalPages: 1 }));
    cycles.getByClearingHouse.and.callFake(({ clearingHouseId }: any) => of(clearingHouseId === 31 ? achCycles() : cenitCycles()));

    await TestBed.configureTestingModule({
      imports: [OperationalRestrictionsComponent],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: OperationalRestrictionsService, useValue: api },
        { provide: ClearingHousesService, useValue: houses },
        { provide: ClearingHouseCycleConfigsApiService, useValue: cycles },
        { provide: AuthService, useValue: auth }
      ]
    }).compileComponents();

    createFixture();
  });

  afterEach(() => {
    TestBed.inject(MatDialog).closeAll();
  });

  it('carga y lista restricciones operacionales CFA con Angular Material', () => {
    expect(api.list).toHaveBeenCalled();
    expect(component.dataSource.data.length).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('Restricciones operativas');
    expect(fixture.nativeElement.textContent).toContain('ACHCOL · V35 · Ciclo 5');
    expect(fixture.nativeElement.querySelector('table[mat-table]')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('mat-paginator')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('mat-form-field')).not.toBeNull();
  });

  it('envía filtros mínimos sin hardcodear ids de cámaras', () => {
    api.list.calls.reset();
    component.filterForm.patchValue({
      clearingHouseCode: 'ACHCOL',
      policyVersion: 'V35',
      cycleConfigId: 105,
      transactionType: TransactionTypeEnum.Debit,
      prenotification: 'no',
      active: 'yes',
      effectiveAt: new Date(2026, 8, 30)
    }, { emitEvent: false });
    component.applyFilters();

    expect(api.list).toHaveBeenCalledWith(jasmine.objectContaining({
      clearingHouseCode: 'ACHCOL',
      policyVersion: 'V35',
      clearingHouseCycleConfigId: 105,
      transactionType: TransactionTypeEnum.Debit,
      isPrenotification: false,
      isActive: true,
      effectiveAt: '2026-09-30'
    }));
  });

  it('respeta Read sin exponer acciones Manage', () => {
    fixture.destroy();
    auth.hasPermission.and.callFake((permission: string | string[]) => {
      const values = Array.isArray(permission) ? permission : [permission];
      return values.includes('OperationalRestrictions.Read');
    });
    createFixture();

    expect(component.canRead).toBeTrue();
    expect(component.canManage).toBeFalse();
    expect(fixture.nativeElement.textContent).not.toContain('Nueva restricción');
    expect(fixture.nativeElement.textContent).toContain('Restricciones operativas');
  });

  it('crea una regla global y conserva null como herencia de AllowedAccountTypes', () => {
    component.create();
    component.form.patchValue({
      transactionType: TransactionTypeEnum.Credit,
      maxAmountPerTransaction: 25000000,
      accountTypesMode: 'inherit',
      effectiveFrom: new Date(2026, 0, 1)
    });
    component.save();

    expect(api.create).toHaveBeenCalledWith(jasmine.objectContaining({
      clearingHouseCode: null,
      clearingHouseCycleConfigId: null,
      transactionType: TransactionTypeEnum.Credit,
      maxAmountPerTransaction: 25000000,
      allowedAccountTypes: null
    }));
  });

  it('trata allowlist explícita vacía como deny-all sin fallback', () => {
    component.create();
    component.form.patchValue({
      accountTypesMode: 'explicit',
      checking: false,
      savings: false,
      electronicDeposits: false,
      effectiveFrom: new Date(2026, 0, 1)
    });
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
    expect(document.body.textContent).toContain('Allowlist vacía');

    component.save();
    expect(api.create.calls.mostRecent().args[0].allowedAccountTypes).toEqual([]);
  });

  it('crea una allowlist explícita con los tipos seleccionados', () => {
    component.create();
    component.form.patchValue({
      accountTypesMode: 'explicit',
      checking: true,
      savings: true,
      electronicDeposits: false,
      effectiveFrom: new Date(2026, 0, 1)
    });
    component.save();
    expect(api.create.calls.mostRecent().args[0].allowedAccountTypes).toEqual([
      AccountTypeEnum.Checking,
      AccountTypeEnum.Savings
    ]);
  });

  it('edita usando ExpectedRowVersion para control de concurrencia', () => {
    const row = restriction();
    component.edit(row);
    component.form.controls.maxTransactionsPerCycle.setValue(700);
    component.save();

    expect(api.update).toHaveBeenCalledWith(row.id, jasmine.objectContaining({
      expectedRowVersion: row.rowVersion,
      clearingHouseCode: 'ACHCOL',
      clearingHouseCycleConfigId: 105,
      maxTransactionsPerCycle: 700
    }));
  });

  it('activa y desactiva usando RowVersion y el diálogo operacional compartido', fakeAsync(() => {
    const active = restriction();
    component.changeStatus(active);
    const deactivateDialog = dialog.openDialogs.at(-1);
    expect(deactivateDialog?.componentInstance instanceof OperationalConfirmDialogComponent).toBeTrue();
    expect((deactivateDialog?.componentInstance as OperationalConfirmDialogComponent).data).toEqual(jasmine.objectContaining({
      confirmLabel: 'Desactivar',
      destructive: true
    }));
    deactivateDialog?.close(true);
    flush();
    expect(api.deactivate).toHaveBeenCalledWith(active.id, { expectedRowVersion: active.rowVersion });

    const inactive = { ...active, isActive: false, rowVersion: 'CQkJ' };
    component.changeStatus(inactive);
    const activateDialog = dialog.openDialogs.at(-1);
    expect(activateDialog?.componentInstance instanceof OperationalConfirmDialogComponent).toBeTrue();
    expect((activateDialog?.componentInstance as OperationalConfirmDialogComponent).data).toEqual(jasmine.objectContaining({
      confirmLabel: 'Activar',
      destructive: false
    }));
    activateDialog?.close(true);
    flush();
    expect(api.activate).toHaveBeenCalledWith(inactive.id, { expectedRowVersion: inactive.rowVersion });
  }));

  it('ofrece recarga clara ante conflicto RowVersion 409', () => {
    const row = restriction();
    const conflict = new HttpErrorResponse({
      status: 409,
      error: { code: 'CFA_OPERATIONAL_RESTRICTION_CONCURRENCY', message: 'La restricción fue modificada.', currentRowVersion: 'TkVX' }
    });
    api.update.and.returnValue(throwError(() => conflict));
    api.get.and.returnValue(of({ ...row, rowVersion: 'TkVX', maxTransactionsPerCycle: 999 }));
    // Spy on the instance injected into this component, and drive the action
    // deterministically instead of relying on the global snackbar overlay timer.
    const reloadAction = new Subject<void>();
    const snackBar = (component as unknown as { snackBar: MatSnackBar }).snackBar;
    const snackSpy = spyOn(snackBar, 'open').and.returnValue({
      onAction: () => reloadAction.asObservable()
    } as MatSnackBarRef<any>);

    component.edit(row);
    component.form.controls.maxTransactionsPerCycle.setValue(700);
    component.save();
    expect(api.update).toHaveBeenCalledWith(row.id, jasmine.objectContaining({ expectedRowVersion: row.rowVersion }));
    expect(snackSpy).toHaveBeenCalledWith(jasmine.any(String), 'Recargar', jasmine.objectContaining({ duration: 9000 }));
    reloadAction.next();
    reloadAction.complete();

    expect(api.get).toHaveBeenCalledWith(row.id);
    expect(component.editing?.rowVersion).toBe('TkVX');
    expect(component.form.controls.maxTransactionsPerCycle.value).toBe(999);
  });

  it('limpia PolicyVersion y ciclo incompatibles al cambiar de cámara', () => {
    component.create();
    component.form.controls.scope.setValue('cycle');
    component.form.controls.clearingHouseCode.setValue('ACHCOL');
    component.form.controls.policyVersion.setValue('V35');
    component.form.controls.cycleConfigId.setValue(105);
    expect(component.form.controls.cycleConfigId.value).toBe(105);

    component.form.controls.clearingHouseCode.setValue('CENIT');
    expect(component.form.controls.policyVersion.value).toBeNull();
    expect(component.form.controls.cycleConfigId.value).toBeNull();
    expect(component.editorCycles.map(item => item.id)).toEqual([901]);
  });

  it('asocia PolicyVersion y ciclo mediante ClearingHouseCycleConfigId real', () => {
    component.create();
    component.form.controls.scope.setValue('cycle');
    component.form.controls.clearingHouseCode.setValue('ACHCOL');
    component.form.controls.policyVersion.setValue('V35');
    component.form.controls.cycleConfigId.setValue(105);
    component.form.controls.effectiveFrom.setValue(new Date(2026, 0, 1));
    component.save();

    const payload = api.create.calls.mostRecent().args[0];
    expect(payload.clearingHouseCode).toBe('ACHCOL');
    expect(payload.clearingHouseCycleConfigId).toBe(105);
    expect((payload as any).policyVersion).toBeUndefined();
  });

  it('mantiene aislamiento visual entre PolicyVersions con el mismo nombre de ciclo', () => {
    component.create();
    component.form.controls.scope.setValue('cycle');
    component.form.controls.clearingHouseCode.setValue('ACHCOL');
    component.form.controls.policyVersion.setValue('V35');
    expect(component.editorCycleOptions.map(item => item.id)).toEqual([105]);
    component.form.controls.policyVersion.setValue('V36');
    expect(component.editorCycleOptions.map(item => item.id)).toEqual([205]);
  });

  it('muestra errores de API sin mezclar regulación con restricciones CFA', () => {
    api.list.and.returnValue(throwError(() => new HttpErrorResponse({ status: 500, error: { detail: 'Fallo controlado de consulta.' } })));
    component.load();
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
    expect(component.error).toContain('Fallo controlado de consulta.');
    expect(fixture.nativeElement.textContent).toContain('Fallo controlado de consulta.');
    expect(fixture.nativeElement.textContent).toContain('no modifican las reglas regulatorias');
  });

  it('renderiza formulario reactivo con Material Datepicker, Checkbox y SlideToggle', () => {
    component.create();
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
    expect(document.body.querySelector('mat-slide-toggle')).not.toBeNull();
    component.form.controls.accountTypesMode.setValue('explicit');
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
    expect(document.body.querySelectorAll('mat-checkbox').length).toBe(3);
    expect(document.body.querySelector('mat-datepicker')).not.toBeNull();
  });

  function createFixture(): void {
    fixture = TestBed.createComponent(OperationalRestrictionsComponent);
    component = fixture.componentInstance;
    dialog = TestBed.inject(MatDialog);
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
  }

  function house(id: number, code: string): any {
    return {
      id, code, name: code === 'ACHCOL' ? 'ACH Colombia' : 'CENIT', originCode: code, isActive: true,
      timeZoneId: 'America/Bogota', holidayStrategy: 'Colombian', requiresNachaProfile: false,
      activeCycleCount: 5, isReady: true, missingRequirements: [], createdAt: '2026-01-01T00:00:00Z'
    };
  }

  function achCycles(): any[] {
    return [
      cycle(105, 31, 'V35', 'Ciclo 5'),
      cycle(205, 31, 'V36', 'Ciclo 5')
    ];
  }

  function cenitCycles(): any[] {
    return [cycle(901, 77, 'CENIT-2026', 'Ciclo 1')];
  }

  function cycle(id: number, clearingHouseId: number, policyVersion: string, cycleName: string): any {
    return {
      id, clearingHouseId, clearingHouseName: clearingHouseId === 31 ? 'ACH Colombia' : 'CENIT', policyVersion, cycleName,
      startTime: '08:00:00', endTime: '10:00:00', cutoffTime: '09:30:00', outputReleaseTime: '10:30:00',
      allowsMonetaryCredit: true, allowsMonetaryDebit: true, allowsCreditPrenotification: true,
      allowsDebitPrenotification: true, allowsReturn: true, allowsReturnOfReturn: true, isActive: true,
      effectiveFrom: '2026-01-01T00:00:00Z', effectiveTo: null, isCurrent: true
    };
  }

  function restriction(): any {
    return {
      id: 41,
      clearingHouseId: 31,
      clearingHouseCode: 'ACHCOL',
      clearingHouseName: 'ACH Colombia',
      clearingHouseCycleConfigId: 105,
      policyVersion: 'V35',
      cycleName: 'Ciclo 5',
      transactionType: TransactionTypeEnum.Debit,
      isPrenotification: false,
      maxAmountPerTransaction: null,
      maxAmountPerCycle: null,
      maxTransactionsPerCycle: null,
      allowedAccountTypes: [AccountTypeEnum.Checking, AccountTypeEnum.Savings],
      effectiveFrom: '2026-01-01T00:00:00Z',
      effectiveTo: null,
      isActive: true,
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: '2026-09-30T12:00:00Z',
      rowVersion: 'AQID'
    };
  }
});
