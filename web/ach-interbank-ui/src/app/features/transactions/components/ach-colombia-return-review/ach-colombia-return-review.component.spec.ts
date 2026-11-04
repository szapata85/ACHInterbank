import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { MatDialog } from '@angular/material/dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import {
  AchColombiaReturnCandidate, AchColombiaReturnReviewApiService
} from '../../services/ach-colombia-return-review-api.service';
import { AchColombiaReturnReviewComponent } from './ach-colombia-return-review.component';

describe('AchColombiaReturnReviewComponent', () => {
  const candidate: AchColombiaReturnCandidate = {
    id: 7, originalTransactionId: 99, reference: 'REF-99', amount: 1000,
    transactionType: 'Credit', originalCycleId: 'ACHCOL-1',
    suggestedReturnCycleId: 'ACHCOL-2', reasonCode: 'R02',
    status: 'PREPARADA_SIN_ENVIO', preparedAtUtc: '2026-11-02T15:25:00Z',
    canApprove: true, blockingReason: null, canGenerateFile: false
  };
  let fixture: ComponentFixture<AchColombiaReturnReviewComponent>;
  let component: AchColombiaReturnReviewComponent;
  let api: jasmine.SpyObj<AchColombiaReturnReviewApiService>;
  let auth: jasmine.SpyObj<AuthService>;
  let dialog: jasmine.SpyObj<MatDialog>;
  let notifications: jasmine.SpyObj<NotificationService>;

  beforeEach(async () => {
    api = jasmine.createSpyObj<AchColombiaReturnReviewApiService>('AchColombiaReturnReviewApiService', ['list', 'decide', 'generateApprovedFile', 'recoverGeneratedFile']);
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['hasPermission']);
    dialog = jasmine.createSpyObj<MatDialog>('MatDialog', ['open']);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'warning', 'error']);
    api.list.and.returnValue(of([candidate]));
    api.decide.and.returnValue(of({ id: 7, status: 'APROBADA_SIN_ENVIO' }));
    api.generateApprovedFile.and.returnValue(of({ fileName: '00001234.RET', contentType: 'text/plain', content: 'QUJD', totalRecords: 1, totalReturns: 1 }));
    api.recoverGeneratedFile.and.returnValue(of({ fileName: '00001234.RET', contentType: 'text/plain', content: 'QUJD', totalRecords: 1, totalReturns: 1 }));
    auth.hasPermission.and.returnValue(true);
    dialog.open.and.returnValue({ afterClosed: () => of(true) } as ReturnType<MatDialog['open']>);
    await TestBed.configureTestingModule({
      imports: [AchColombiaReturnReviewComponent, NoopAnimationsModule],
      providers: [
        { provide: AchColombiaReturnReviewApiService, useValue: api },
        { provide: AuthService, useValue: auth },
        { provide: NotificationService, useValue: notifications }
      ]
    }).overrideComponent(AchColombiaReturnReviewComponent, {
      // MatDialogModule also registers MatDialog. Force the spy at the component injector
      // so these tests never open the real Angular Material dialog/overlay.
      add: { providers: [{ provide: MatDialog, useValue: dialog }] }
    }).compileComponents();
    fixture = TestBed.createComponent(AchColombiaReturnReviewComponent);
    expect(fixture.debugElement.injector.get(MatDialog)).toBe(dialog);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => fixture.destroy());

  it('muestra estado y causal sin generar archivos', () => {
    expect(api.list).toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Transacción 99');
    expect(fixture.nativeElement.textContent).toContain('R02');
    expect(fixture.nativeElement.textContent).toContain('no genera');
  });

  it('solo cambia estado tras confirmación explícita', () => {
    component.confirm(candidate, 'APROBAR');
    expect(dialog.open).toHaveBeenCalled();
    expect(api.decide).toHaveBeenCalledWith(7, 'APROBAR');
    expect(notifications.success).toHaveBeenCalled();
  });

  it('impide acciones si el usuario no tiene permiso', () => {
    auth.hasPermission.and.returnValue(false);
    component.confirm(candidate, 'APROBAR');
    expect(api.decide).not.toHaveBeenCalled();
  });

  it('no permite generar sin aprobación expresa y ventana vigente', () => {
    component.confirm(candidate, 'GENERAR');
    expect(api.generateApprovedFile).not.toHaveBeenCalled();
  });

  it('genera desde una aprobada tras confirmar, conserva el nombre oficial y no transmite', () => {
    const approved = { ...candidate, status: 'APROBADA_SIN_ENVIO', canApprove: false, canGenerateFile: true };
    const createObjectURL = spyOn(URL, 'createObjectURL').and.returnValue('blob:prueba');
    spyOn(URL, 'revokeObjectURL');
    const clickSpy = spyOn(HTMLAnchorElement.prototype, 'click');
    component.confirm(approved, 'GENERAR');
    expect(dialog.open).toHaveBeenCalled();
    expect(api.generateApprovedFile).toHaveBeenCalledWith(7);
    expect(createObjectURL).toHaveBeenCalled();
    expect(clickSpy).toHaveBeenCalled();
    expect(notifications.success).toHaveBeenCalled();
  });

  it('recupera el archivo ya generado sin volver a llamar al generador', () => {
    const created = { ...candidate, status: 'ARCHIVO_GENERADO_SIN_ENVIO', canApprove: false, canGenerateFile: false };
    spyOn(URL, 'createObjectURL').and.returnValue('blob:recuperado');
    spyOn(URL, 'revokeObjectURL');
    spyOn(HTMLAnchorElement.prototype, 'click');
    component.recover(created);
    expect(api.recoverGeneratedFile).toHaveBeenCalledWith(7);
    expect(api.generateApprovedFile).not.toHaveBeenCalled();
    expect(notifications.success).toHaveBeenCalled();
  });

  it('no anuncia generación correcta ante respuesta de conflicto', () => {
    const approved = { ...candidate, status: 'APROBADA_SIN_ENVIO', canApprove: false, canGenerateFile: true };
    api.generateApprovedFile.and.returnValue(throwError(() => new HttpErrorResponse({ status: 409 })));
    component.confirm(approved, 'GENERAR');
    expect(component.error).toContain('cambió');
    expect(notifications.success).not.toHaveBeenCalled();
  });

  it('muestra error de concurrencia y no afirma aprobación', () => {
    api.decide.and.returnValue(throwError(() => new HttpErrorResponse({ status: 409 })));
    component.confirm(candidate, 'APROBAR');
    expect(component.error).toContain('cambió');
    expect(notifications.success).not.toHaveBeenCalled();
  });
});
