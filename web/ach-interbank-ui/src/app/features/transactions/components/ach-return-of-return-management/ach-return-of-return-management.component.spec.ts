import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { AchReturnsApiService } from '../../services/ach-returns-api.service';
import { AchReturnOfReturnManagementComponent } from './ach-return-of-return-management.component';

describe('AchReturnOfReturnManagementComponent', () => {
  let fixture: ComponentFixture<AchReturnOfReturnManagementComponent>;
  let component: AchReturnOfReturnManagementComponent;
  let apiSpy: jasmine.SpyObj<AchReturnsApiService>;
  let notifications: jasmine.SpyObj<NotificationService>;
  let auth: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj<AchReturnsApiService>('AchReturnsApiService', [
      'evaluateReturnOfReturn',
      'generateReturnOfReturnAuditFile',
      'generateReturnOfReturnNachaFile'
    ]);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'warning', 'error']);
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['hasPermission']);
    auth.hasPermission.and.returnValue(true);

    await TestBed.configureTestingModule({
      imports: [AchReturnOfReturnManagementComponent],
      providers: [
        { provide: AchReturnsApiService, useValue: apiSpy },
        { provide: AuthService, useValue: auth },
        { provide: NotificationService, useValue: notifications }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AchReturnOfReturnManagementComponent);
    component = fixture.componentInstance;
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
  });

  it('evalúa elegibilidad con source exacto y causal trimmed', () => {
    apiSpy.evaluateReturnOfReturn.and.returnValue(of({ isEligible: true, isUniquePerTransaction: true, failures: [] } as any));
    component.evaluateForm.setValue({ sourceReturnTransactionId: 17, newReturnReasonCode: ' R02 ' });

    component.evaluate();

    expect(apiSpy.evaluateReturnOfReturn).toHaveBeenCalledOnceWith({
      sourceReturnTransactionId: 17,
      newReturnReasonCode: 'R02',
      source: 'spa-angular-ror'
    });
    expect(component.eligibilityResult?.isEligible).toBeTrue();
    expect(notifications.success).toHaveBeenCalledWith('La devolución de devolución es elegible.');
    expect(apiSpy.generateReturnOfReturnNachaFile).not.toHaveBeenCalled();
  });

  it('no evalúa sin sourceReturnTransactionId válido', () => {
    component.evaluateForm.setValue({ sourceReturnTransactionId: 0, newReturnReasonCode: 'R02' });

    component.evaluate();

    expect(apiSpy.evaluateReturnOfReturn).not.toHaveBeenCalled();
    expect(notifications.warning).toHaveBeenCalledWith('Complete los datos requeridos para evaluar elegibilidad.');
  });

  it('preserva failures de una evaluación no elegible', () => {
    apiSpy.evaluateReturnOfReturn.and.returnValue(of({
      isEligible: false,
      isUniquePerTransaction: true,
      failures: [{ code: 'X', message: 'No', field: null }]
    } as any));
    component.evaluateForm.setValue({ sourceReturnTransactionId: 1, newReturnReasonCode: 'R02' });

    component.evaluate();

    expect(component.eligibilityResult?.failures.length).toBe(1);
    expect(notifications.warning).toHaveBeenCalledWith('La devolución de devolución no es elegible.');
  });

  it('parsea flowIds separados por coma y descarta inválidos para auditoría', () => {
    apiSpy.generateReturnOfReturnAuditFile.and.returnValue(of(new Blob(['x'], { type: 'text/plain' })));
    spyOn<any>(component, 'downloadBlob');
    component.generationForm.setValue({ flowIds: ' 4, abc, 0, -2, 7, 2.5, 9 ' });

    component.generateAudit();

    expect(apiSpy.generateReturnOfReturnAuditFile).toHaveBeenCalledOnceWith({
      flowIds: [4, 7, 9],
      source: 'spa-angular-ror'
    });
  });

  it('bloquea generación de auditoría sin flowIds válidos', () => {
    component.generationForm.setValue({ flowIds: 'abc,0,-1,2.5' });

    component.generateAudit();

    expect(apiSpy.generateReturnOfReturnAuditFile).not.toHaveBeenCalled();
    expect(notifications.warning).toHaveBeenCalledWith('Ingrese al menos un ID de flujo válido.');
  });

  it('genera auditoría sin exigir evaluación y conserva nombre de descarga', () => {
    apiSpy.generateReturnOfReturnAuditFile.and.returnValue(of(new Blob(['x'], { type: 'text/plain' })));
    const downloadSpy = spyOn<any>(component, 'downloadBlob');
    spyOn(Date, 'now').and.returnValue(123456);
    component.eligibilityResult = null;
    component.generationForm.setValue({ flowIds: '1,2' });

    component.generateAudit();

    expect(apiSpy.generateReturnOfReturnAuditFile).toHaveBeenCalledOnceWith({ flowIds: [1, 2], source: 'spa-angular-ror' });
    expect(downloadSpy).toHaveBeenCalledWith(jasmine.any(Blob), 'ROR_AUDIT_123456.ach');
    expect(notifications.success).toHaveBeenCalledWith('Archivo de auditoría ROR generado correctamente.');
  });

  it('bloquea NACHA si no hay evaluación previa', () => {
    component.generationForm.setValue({ flowIds: '1' });

    component.generateNacha();

    expect(notifications.warning).toHaveBeenCalledWith('Debe evaluar elegibilidad antes de generar NACHA-M productivo.');
    expect(apiSpy.generateReturnOfReturnNachaFile).not.toHaveBeenCalled();
  });

  it('bloquea NACHA si la evaluación no es elegible', () => {
    component.eligibilityResult = {
      isEligible: false,
      isUniquePerTransaction: true,
      failures: [{ code: 'X', message: 'No' }]
    } as any;
    component.generationForm.setValue({ flowIds: '1' });

    component.generateNacha();

    expect(notifications.warning).toHaveBeenCalledWith('La evaluación actual no es elegible. No se puede generar NACHA-M productivo.');
    expect(apiSpy.generateReturnOfReturnNachaFile).not.toHaveBeenCalled();
  });

  it('requiere confirmación explícita antes de generar NACHA productivo', () => {
    component.eligibilityResult = { isEligible: true, isUniquePerTransaction: true, failures: [] } as any;
    component.generationForm.setValue({ flowIds: '1' });
    spyOn(window, 'confirm').and.returnValue(false);

    component.generateNacha();

    expect(window.confirm).toHaveBeenCalled();
    expect(apiSpy.generateReturnOfReturnNachaFile).not.toHaveBeenCalled();
  });

  it('genera NACHA elegible con flowIds, source y nombre productivo intactos', () => {
    apiSpy.generateReturnOfReturnNachaFile.and.returnValue(of(new Blob(['x'], { type: 'text/plain' })));
    component.eligibilityResult = { isEligible: true, isUniquePerTransaction: true, failures: [] } as any;
    component.generationForm.setValue({ flowIds: ' 3, 8 ' });
    spyOn(window, 'confirm').and.returnValue(true);
    spyOn(Date, 'now').and.returnValue(654321);
    const downloadSpy = spyOn<any>(component, 'downloadBlob');

    component.generateNacha();

    expect(apiSpy.generateReturnOfReturnNachaFile).toHaveBeenCalledOnceWith({
      flowIds: [3, 8],
      source: 'spa-angular-ror'
    });
    expect(downloadSpy).toHaveBeenCalledWith(jasmine.any(Blob), 'RORNACHA_654321.ach');
    expect(notifications.success).toHaveBeenCalledWith('Archivo NACHA-M productivo de devolución de devolución generado correctamente.');
  });

  it('preserva code, message y field de failures funcionales del backend', () => {
    apiSpy.generateReturnOfReturnNachaFile.and.returnValue(throwError(() => ({
      error: {
        failures: [
          { code: 'DUP', field: 'flowIds', message: 'Duplicado' },
          { code: 'X', field: null, message: 'Otro' }
        ]
      }
    })));
    component.eligibilityResult = { isEligible: true, isUniquePerTransaction: true, failures: [] } as any;
    component.generationForm.setValue({ flowIds: '1' });
    spyOn(window, 'confirm').and.returnValue(true);

    component.generateNacha();

    expect(component.generationFailures).toEqual([
      { code: 'DUP', message: 'Duplicado', field: 'flowIds' },
      { code: 'X', message: 'Otro', field: null }
    ]);
    expect(notifications.error).toHaveBeenCalledWith('Duplicado');
  });

  it('usa el fallback funcional cuando el backend no entrega failures', () => {
    apiSpy.generateReturnOfReturnAuditFile.and.returnValue(throwError(() => ({ error: {} })));
    component.generationForm.setValue({ flowIds: '1' });

    component.generateAudit();

    expect(component.generationFailures).toEqual([]);
    expect(notifications.error).toHaveBeenCalledWith('No fue posible generar el archivo de auditoría ROR.');
  });


  it('usa Returns.GenerateFile o CanManageAch como frontera de generación', () => {
    void component.canGenerateFiles;

    expect(auth.hasPermission).toHaveBeenCalledWith(['Returns.GenerateFile', 'CanManageAch']);
  });

  it('falla cerrado al intentar generar auditoría sin capacidad mutable', () => {
    auth.hasPermission.and.returnValue(false);
    component.generationForm.setValue({ flowIds: '1' });

    component.generateAudit();

    expect(apiSpy.generateReturnOfReturnAuditFile).not.toHaveBeenCalled();
    expect(notifications.error).toHaveBeenCalledWith('No tiene permisos para generar archivos de devolución de devolución.');
  });

  it('falla cerrado al intentar generar NACHA sin capacidad mutable', () => {
    auth.hasPermission.and.returnValue(false);
    component.eligibilityResult = { isEligible: true, isUniquePerTransaction: true, failures: [] } as any;
    component.generationForm.setValue({ flowIds: '1' });

    component.generateNacha();

    expect(apiSpy.generateReturnOfReturnNachaFile).not.toHaveBeenCalled();
    expect(notifications.error).toHaveBeenCalledWith('No tiene permisos para generar archivos de devolución de devolución.');
  });

  it('usa Material y no renderiza ui-boton', () => {
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
    const host: HTMLElement = fixture.nativeElement;

    expect(host.querySelector('ui-boton')).toBeNull();
    expect(host.querySelectorAll('mat-form-field').length).toBe(3);
    expect(host.querySelectorAll('button[mat-flat-button], button[mat-stroked-button]').length).toBe(4);
  });
});
