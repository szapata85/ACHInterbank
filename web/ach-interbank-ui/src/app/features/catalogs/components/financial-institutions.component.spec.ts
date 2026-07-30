import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { of, Subject, throwError } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { DestinationInstitution } from '../../transactions/transactions.models';
import { FinancialInstitutionStatusEnum } from '../../transactions/transactions.types';
import { FinancialInstitutionAdminService } from '../services/financial-institution-admin.service';
import { CatalogActionConfirmDialogData } from './catalog-action-confirm-dialog.component';
import { FinancialInstitutionsComponent } from './financial-institutions.component';

describe('FinancialInstitutionsComponent', () => {
  let fixture: ComponentFixture<FinancialInstitutionsComponent>;
  let component: FinancialInstitutionsComponent;
  let service: jasmine.SpyObj<FinancialInstitutionAdminService>;
  let dialog: jasmine.SpyObj<MatDialog>;
  let notifications: jasmine.SpyObj<NotificationService>;

  const defaultSource: DestinationInstitution = {
    id: 73,
    name: 'Institución origen configurable',
    routingNumber: '123456789',
    transitCode: '001',
    checkDigit: '7',
    isDefaultSource: true,
    status: FinancialInstitutionStatusEnum.Active
  };

  beforeEach(async () => {
    service = jasmine.createSpyObj<FinancialInstitutionAdminService>(
      'FinancialInstitutionAdminService',
      ['list', 'create', 'update', 'setStatus']
    );
    service.list.and.returnValue(of([defaultSource]));

    notifications = jasmine.createSpyObj<NotificationService>(
      'NotificationService',
      ['success', 'error']
    );

    await TestBed.configureTestingModule({
      imports: [FinancialInstitutionsComponent, NoopAnimationsModule],
      providers: [
        { provide: FinancialInstitutionAdminService, useValue: service },
        { provide: NotificationService, useValue: notifications }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(FinancialInstitutionsComponent);
    component = fixture.componentInstance;
    const injectedDialog = (
      component as unknown as { dialog: MatDialog }
    ).dialog;
    spyOn(injectedDialog, 'open');
    dialog = injectedDialog as jasmine.SpyObj<MatDialog>;
    fixture.detectChanges();
  });

  afterEach(() => fixture.destroy());

  it('loads all institutions including inactive through the existing endpoint contract', () => {
    expect(service.list).toHaveBeenCalledOnceWith(true);
    expect(component.institutions).toEqual([defaultSource]);
    expect(component.loading).toBeFalse();
    expect(component.loadError).toBeFalse();
  });

  it('uses numeric validators and does not submit an invalid form', () => {
    component.startCreate();
    component.form.patchValue({
      name: '   ',
      routingNumber: '12A',
      transitCode: 'XYZ'
    });

    component.save();
    fixture.detectChanges();

    expect(component.form.invalid).toBeTrue();
    expect(component.form.controls.name.hasError('pattern')).toBeTrue();
    expect(component.form.controls.routingNumber.hasError('pattern')).toBeTrue();
    expect(component.form.controls.transitCode.hasError('pattern')).toBeTrue();
    expect(service.create).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('El nombre debe contener');
    expect(fixture.nativeElement.textContent).toContain('Usa únicamente dígitos');
  });

  it('calculates the check digit when routing and transit form eight numeric digits', () => {
    component.startCreate();
    component.form.patchValue({
      name: 'Institución calculada',
      routingNumber: '12345',
      transitCode: '678'
    });

    expect(component.form.controls.checkDigit.value).toBe('0');
    expect(component.form.valid).toBeTrue();
  });

  it('preserves an existing default source and its persisted digit without assuming its id', () => {
    service.update.and.returnValue(of(defaultSource));
    component.startEdit(defaultSource);

    expect(component.form.controls.checkDigit.value).toBe('7');
    expect(component.form.controls.isDefaultSource.value).toBeTrue();

    component.save();

    expect(service.update).toHaveBeenCalledTimes(1);
    const [id, payload] = service.update.calls.mostRecent().args;
    expect(id).toBe(73);
    expect(payload.isDefaultSource).toBeTrue();
    expect(payload.checkDigit).toBe('7');
    expect(component.successMessage).toBe('Institución actualizada correctamente.');
    expect(notifications.success).toHaveBeenCalledWith('Institución actualizada correctamente.');
  });

  it('prevents duplicate create requests while the first request is pending', () => {
    const pending = new Subject<DestinationInstitution>();
    service.create.and.returnValue(pending);
    component.startCreate();
    component.form.patchValue({
      name: 'Nueva institución',
      routingNumber: '12345',
      transitCode: '678',
      isDefaultSource: false,
      status: FinancialInstitutionStatusEnum.Active
    });

    component.save();
    component.save();

    expect(service.create).toHaveBeenCalledTimes(1);
    expect(component.saving).toBeTrue();

    pending.error({ error: { detail: 'Registro duplicado' } });
    expect(component.saving).toBeFalse();
    expect(component.operationError).toBe('Registro duplicado');
    expect(component.showForm).toBeTrue();
  });

  it('keeps the editor open and exposes a server error when update fails', () => {
    service.update.and.returnValue(
      throwError(() => ({ error: { detail: 'La institución cambió en el servidor' } }))
    );
    component.startEdit(defaultSource);

    component.save();

    expect(component.showForm).toBeTrue();
    expect(component.operationError).toBe('La institución cambió en el servidor');
    expect(notifications.error).toHaveBeenCalledWith('La institución cambió en el servidor');
  });

  it('changes status only after confirmation and warns when the item is the default source', () => {
    dialog.open.and.returnValue({ afterClosed: () => of(true) } as never);
    service.setStatus.and.returnValue(of(void 0));

    component.toggleStatus(defaultSource);

    expect(dialog.open).toHaveBeenCalledTimes(1);
    const dialogConfig = dialog.open.calls.mostRecent().args[1] as {
      data?: CatalogActionConfirmDialogData;
    };
    expect(dialogConfig.data?.message).toContain('origen por defecto');
    expect(service.setStatus).toHaveBeenCalledOnceWith(
      73,
      FinancialInstitutionStatusEnum.Inactive
    );
    expect(notifications.success).toHaveBeenCalledWith('Institución desactivada correctamente.');
  });

  it('does not change status when confirmation is cancelled', () => {
    dialog.open.and.returnValue({ afterClosed: () => of(false) } as never);

    component.toggleStatus(defaultSource);

    expect(service.setStatus).not.toHaveBeenCalled();
  });
});
