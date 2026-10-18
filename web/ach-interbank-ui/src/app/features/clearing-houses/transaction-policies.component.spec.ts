import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, Subject, of, throwError } from 'rxjs';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MatDialog } from '@angular/material/dialog';
import { AuthService } from '../../core/services/auth.service';
import { ClearingHousesService } from '../../core/services/clearing-houses.service';
import { TransactionPoliciesService } from './transaction-policies.service';
import { TransactionPoliciesComponent } from './transaction-policies.component';
import { TransactionTypeEnum } from '../../core/models/transaction-types.model';
import { OperationalConfirmDialogComponent } from '../../shared/components/operational-confirm-dialog.component';

describe('TransactionPoliciesComponent', () => {
  let fixture: ComponentFixture<TransactionPoliciesComponent>;
  let component: TransactionPoliciesComponent;
  let routeParams$: BehaviorSubject<ReturnType<typeof convertToParamMap>>;
  let getHouse: jasmine.Spy;
  let listPolicies: jasmine.Spy;
  let houseRequests: Map<number, Subject<any>>;
  let policyRequests: Map<number, Subject<any>>;
  let createPolicy: jasmine.Spy;
  let updateMetadata: jasmine.Spy;
  let previewPolicy: jasmine.Spy;
  let closePolicy: jasmine.Spy;
  let activatePolicy: jasmine.Spy;

  beforeEach(async () => {
    routeParams$ = new BehaviorSubject(convertToParamMap({ id: '7' }));
    houseRequests = new Map();
    policyRequests = new Map();
    getHouse = jasmine.createSpy('get').and.callFake((id: number) => houseRequests.get(id)!);
    listPolicies = jasmine.createSpy('list').and.callFake((id: number) => policyRequests.get(id)!);
    createPolicy = jasmine.createSpy('create').and.returnValue(of(policy(7, 3)));
    updateMetadata = jasmine.createSpy('updateMetadata').and.returnValue(of(policy(7, 3)));
    previewPolicy = jasmine.createSpy('preview').and.returnValue(of({
      ruleConfigured: true,
      requiresPrenotification: true,
      prenotificationMode: 'Mandatory',
      normativeSource: 'DSP',
      normativeReference: '4.7',
      decision: 'Allowed',
      message: 'Regla vigente.'
    }));
    closePolicy = jasmine.createSpy('close').and.returnValue(of(policy(7, 3)));
    activatePolicy = jasmine.createSpy('activate').and.returnValue(of(policy(7, 3)));

    await TestBed.configureTestingModule({
      imports: [TransactionPoliciesComponent],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: ActivatedRoute, useValue: { paramMap: routeParams$.asObservable() } },
        { provide: AuthService, useValue: { hasPermission: () => true } },
        { provide: ClearingHousesService, useValue: { get: getHouse } },
        {
          provide: TransactionPoliciesService,
          useValue: {
            list: listPolicies,
            create: createPolicy,
            updateMetadata,
            preview: previewPolicy,
            close: closePolicy,
            activate: activatePolicy
          }
        }
      ]
    }).compileComponents();
  });

  it('starts loading and clears it after both requests assign the camera and policies', () => {
    createFor(7);
    expect(component.loading).toBeTrue();

    completeLoad(7, 'CENIT', [policy(7, null)]);

    expect(component.loading).toBeFalse();
    expect(component.clearingHouse?.code).toBe('CENIT');
    expect(component.policies).toEqual([policy(7, null)]);
    expect(component.lead(component.currentDebit)).toBe('Sin plazo mínimo documentado');
  });

  it('clears loading and shows the HTTP error message', () => {
    createFor(7);
    houseRequests.get(7)!.error({ status: 404 });

    expect(component.loading).toBeFalse();
    expect(component.error).toBe('La cámara solicitada no existe o ya no está disponible.');
  });

  it('does not request data for an invalid route id', () => {
    routeParams$.next(convertToParamMap({ id: 'invalid' }));
    fixture = TestBed.createComponent(TransactionPoliciesComponent);
    component = fixture.componentInstance;
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();

    expect(component.loading).toBeFalse();
    expect(component.error).toBe('La cámara indicada no es válida.');
    expect(getHouse).not.toHaveBeenCalled();
    expect(listPolicies).not.toHaveBeenCalled();
  });

  it('loads again when the route id changes', () => {
    createFor(7);
    completeLoad(7, 'CENIT', [policy(7, null)]);

    houseRequests.set(8, new Subject());
    policyRequests.set(8, new Subject());
    routeParams$.next(convertToParamMap({ id: '8' }));

    expect(component.loading).toBeTrue();
    expect(getHouse).toHaveBeenCalledWith(8);
    expect(listPolicies).toHaveBeenCalledWith(8);
    completeLoad(8, 'ACHCOL', [policy(8, 3)]);
    expect(component.loading).toBeFalse();
    expect(component.clearingHouse?.code).toBe('ACHCOL');
    expect(component.lead(component.currentDebit)).toBe('3 días hábiles');
  });

  it('clears and disables lead days for optional policies', () => {
    createFor(7);
    completeLoad(7, 'CENIT', [policy(7, null)]);

    component.createVersion();
    component.form.controls.prenotificationMode.setValue('Optional');

    expect(component.form.controls.prenotificationLeadBusinessDays.disabled).toBeTrue();
    expect(component.form.controls.prenotificationLeadBusinessDays.value).toBeNull();
  });

  it('keeps listening for route changes after an HTTP error', () => {
    createFor(7);
    houseRequests.get(7)!.error({ status: 403 });
    expect(component.loading).toBeFalse();
    expect(component.error).toContain('permisos');

    houseRequests.set(8, new Subject());
    policyRequests.set(8, new Subject());
    routeParams$.next(convertToParamMap({ id: '8' }));
    completeLoad(8, 'ACHCOL', [policy(8, 3)]);

    expect(component.clearingHouse?.code).toBe('ACHCOL');
    expect(component.error).toBe('');
  });

  it('renders no local h1 because the application shell owns the page heading', () => {
    createFor(7);
    completeLoad(7, 'ACHCOL', [policy(7, 3)]);
    expect(fixture.nativeElement.querySelectorAll('h1').length).toBe(0);
    expect(fixture.nativeElement.textContent).toContain('Reglas de prenotificación');
  });

  it('renders foundation loading, page header and empty states without changing the policy table behavior', () => {
    createFor(7);
    expect(fixture.nativeElement.querySelector('ui-estado-carga')).not.toBeNull();

    completeLoad(7, 'CENIT', []);

    expect(fixture.nativeElement.querySelector('app-page-header')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Reglas de prenotificación');
    expect(fixture.nativeElement.querySelector('ui-estado-vacio')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.policy-page__table-wrap')).toBeNull();
  });

  it('renders the shared error state and keeps the back action on load failure', () => {
    createFor(7);
    houseRequests.get(7)!.error({ status: 403 });
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('ui-estado-error')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('No tiene permisos');
    expect(fixture.nativeElement.textContent).toContain('Volver a Cámaras compensadoras');
  });

  it('prioritizes operational decisions and keeps normative references out of summaries and the main table', () => {
    createFor(7);
    completeLoad(7, 'ACHCOL', [
      policy(7, 3),
      { ...policy(7, null), id: 8, transactionType: TransactionTypeEnum.Credit, prenotificationMode: 'Optional' }
    ]);
    const summary = fixture.nativeElement.querySelector('.policy-page__summary') as HTMLElement;
    const table = fixture.nativeElement.querySelector('.policy-page__table-wrap') as HTMLElement;
    expect(summary.textContent).toContain('Bloquea la exportación');
    expect(summary.textContent).toContain('No bloquea por ausencia');
    expect(summary.textContent).not.toContain('4.7');
    expect(table.textContent).not.toContain('Referencia');
    expect(table.textContent).not.toContain('4.7');
  });

  it('keeps normative detail available and names metadata editing as traceability', () => {
    createFor(7);
    completeLoad(7, 'CENIT', [policy(7, null)]);
    component.viewNormativeDetail(component.policies[0]);
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
    expect(document.body.textContent).toContain('Detalle normativo');
    expect(document.body.textContent).toContain('4.7');
    component.createVersion();
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
    expect(document.body.textContent).toContain('Trazabilidad normativa');
  });

  it('validates integer, non-negative and maximum lead days', () => {
    createFor(7);
    completeLoad(7, 'ACHCOL', [policy(7, 3)]);
    component.createVersion();
    const lead = component.form.controls.prenotificationLeadBusinessDays;
    lead.setValue(1.5);
    expect(lead.hasError('integer')).toBeTrue();
    lead.setValue(-1);
    expect(lead.hasError('min')).toBeTrue();
    lead.setValue(366);
    expect(lead.hasError('max')).toBeTrue();
  });

  it('creates an effective-dated version with the existing trimmed payload', () => {
    createFor(7);
    completeLoad(7, 'CENIT', [policy(7, 3)]);
    component.createVersion();
    component.form.setValue({
      transactionType: TransactionTypeEnum.Debit,
      prenotificationMode: 'Mandatory',
      prenotificationLeadBusinessDays: 3,
      effectiveFrom: new Date(2026, 0, 10),
      effectiveTo: new Date(2026, 1, 10),
      normativeSource: ' DSP ',
      normativeReference: ' 4.7 ',
      notes: ' nota operativa '
    });

    (component as any).persist();

    expect(createPolicy).toHaveBeenCalledOnceWith(7, {
      transactionType: TransactionTypeEnum.Debit,
      prenotificationMode: 'Mandatory',
      prenotificationLeadBusinessDays: 3,
      effectiveFrom: '2026-01-10',
      effectiveTo: '2026-02-10',
      isActive: true,
      normativeSource: 'DSP',
      normativeReference: '4.7',
      notes: 'nota operativa'
    });
  });

  it('edits only traceability metadata for an existing policy', () => {
    createFor(7);
    completeLoad(7, 'CENIT', [policy(7, 3)]);
    const existing = component.policies[0];
    component.editMetadata(existing);
    component.form.controls.normativeSource.setValue(' Fuente ');
    component.form.controls.normativeReference.setValue(' Ref ');
    component.form.controls.notes.setValue(' Nota ');

    (component as any).persist();

    expect(updateMetadata).toHaveBeenCalledOnceWith(7, existing.id, {
      normativeSource: 'Fuente',
      normativeReference: 'Ref',
      notes: 'Nota'
    });
    expect(component.form.controls.transactionType.disabled).toBeTrue();
    expect(component.form.controls.effectiveFrom.disabled).toBeTrue();
  });

  it('keeps the existing 409 overlap handling without retrying the create operation', () => {
    createPolicy.and.returnValue(throwError(() => ({ status: 409, error: { detail: 'La vigencia se solapa.' } })));
    createFor(7);
    completeLoad(7, 'CENIT', [policy(7, 3)]);
    component.createVersion();
    component.form.patchValue({
      transactionType: TransactionTypeEnum.Debit,
      prenotificationMode: 'Mandatory',
      prenotificationLeadBusinessDays: 3,
      effectiveFrom: new Date(2026, 0, 10),
      normativeSource: 'DSP',
      normativeReference: '4.7'
    });

    (component as any).persist();

    expect(createPolicy).toHaveBeenCalledTimes(1);
    expect(component.form.hasError('overlap')).toBeTrue();
  });

  it('uses the shared operational dialog for destructive cancellation without closing the editor', () => {
    createFor(7);
    completeLoad(7, 'CENIT', [policy(7, 3)]);
    component.createVersion();
    component.form.controls.notes.setValue('Cambio pendiente');
    component.form.controls.notes.markAsDirty();
    const dialog = (component as any).dialog as MatDialog;
    const open = spyOn(dialog, 'open').and.returnValue({ afterClosed: () => of(false) } as any);

    component.cancel();

    expect(open).toHaveBeenCalledWith(OperationalConfirmDialogComponent, jasmine.objectContaining({
      data: jasmine.objectContaining({
        title: 'Descartar cambios',
        confirmLabel: 'Descartar',
        icon: 'edit_off',
        destructive: true
      }),
      width: 'min(92vw, 540px)'
    }));
  });

  it('activates a future version through the existing API after confirmation', () => {
    createFor(7);
    const future = { ...policy(7, 3), id: 70, effectiveFrom: '2099-01-01' };
    completeLoad(7, 'CENIT', [future]);
    spyOn<any>(component, 'confirm').and.returnValue(of(true));

    component.activate(future);

    expect(activatePolicy).toHaveBeenCalledOnceWith(7, 70);
  });

  it('closes validity with the selected effective date and reloads through the existing API', () => {
    createFor(7);
    const existing = { ...policy(7, 3), id: 71 };
    completeLoad(7, 'CENIT', [existing]);
    const dialog = (component as any).dialog as MatDialog;
    spyOn(dialog, 'open').and.returnValue({ afterClosed: () => of(new Date(2026, 0, 31)) } as any);

    component.close(existing);

    expect(closePolicy).toHaveBeenCalledOnceWith(7, 71, '2026-01-31');
  });

  it('preserves the existing permission-derived management surface', () => {
    createFor(7);
    completeLoad(7, 'CENIT', [policy(7, 3)]);

    expect(component.canRead).toBeTrue();
    expect(component.canManage).toBeTrue();
    expect(fixture.nativeElement.textContent).toContain('Crear nueva versión');
  });

  function createFor(id: number): void {
    houseRequests.set(id, new Subject());
    policyRequests.set(id, new Subject());
    fixture = TestBed.createComponent(TransactionPoliciesComponent);
    component = fixture.componentInstance;
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
  }

  function completeLoad(id: number, code: string, policies: any[]): void {
    houseRequests.get(id)!.next({ id, code, name: code, isActive: true, isReady: true });
    houseRequests.get(id)!.complete();
    policyRequests.get(id)!.next(policies);
    policyRequests.get(id)!.complete();
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
  }

  function policy(clearingHouseId: number, leadDays: number | null): any {
    return {
      id: clearingHouseId,
      clearingHouseId,
      clearingHouseName: 'Cámara',
      transactionType: TransactionTypeEnum.Debit,
      prenotificationMode: 'Mandatory',
      prenotificationLeadBusinessDays: leadDays,
      effectiveFrom: '2026-01-01',
      effectiveTo: null,
      isActive: true,
      normativeSource: 'DSP',
      normativeReference: '4.7',
      notes: '',
      createdAt: '2026-01-01',
      updatedAt: '2026-01-01'
    };
  }
});
