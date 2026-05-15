import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../../../core/services/notification.service';
import { AchReturnsApiService } from '../../services/ach-returns-api.service';
import { AchReturnOfReturnManagementComponent } from './ach-return-of-return-management.component';

describe('AchReturnOfReturnManagementComponent', () => {
  let fixture: ComponentFixture<AchReturnOfReturnManagementComponent>;
  let component: AchReturnOfReturnManagementComponent;
  let apiSpy: jasmine.SpyObj<AchReturnsApiService>;
  let notifications: jasmine.SpyObj<NotificationService>;

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj<AchReturnsApiService>('AchReturnsApiService', [
      'evaluateReturnOfReturn',
      'generateReturnOfReturnAuditFile',
      'generateReturnOfReturnNachaFile'
    ]);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'warning', 'error']);

    await TestBed.configureTestingModule({
      imports: [AchReturnOfReturnManagementComponent],
      providers: [
        { provide: AchReturnsApiService, useValue: apiSpy },
        { provide: NotificationService, useValue: notifications }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AchReturnOfReturnManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('muestra resultado elegible', () => {
    apiSpy.evaluateReturnOfReturn.and.returnValue(of({ isEligible: true, isUniquePerTransaction: true, failures: [] } as any));
    component.evaluateForm.setValue({ sourceReturnTransactionId: 1, newReturnReasonCode: 'R02' });
    component.evaluate();
    expect(component.eligibilityResult?.isEligible).toBeTrue();
  });

  it('muestra failures cuando no es elegible', () => {
    apiSpy.evaluateReturnOfReturn.and.returnValue(of({ isEligible: false, isUniquePerTransaction: true, failures: [{ code: 'X', message: 'No', field: null }] } as any));
    component.evaluateForm.setValue({ sourceReturnTransactionId: 1, newReturnReasonCode: 'R02' });
    component.evaluate();
    expect(component.eligibilityResult?.failures.length).toBe(1);
  });

  it('bloquea generación sin flowIds', () => {
    component.generationForm.setValue({ flowIds: '' });
    component.generateAudit();
    expect(notifications.warning).toHaveBeenCalled();
  });

  it('maneja 409 con failures', () => {
    apiSpy.generateReturnOfReturnNachaFile.and.returnValue(throwError(() => ({ error: { failures: [{ message: 'Duplicado' }] } })));
    component.generationForm.setValue({ flowIds: '1' });
    spyOn(window, 'confirm').and.returnValue(true);
    component.generateNacha();
    expect(notifications.error).toHaveBeenCalledWith('Duplicado');
  });

  it('invoca descarga audit-file', () => {
    apiSpy.generateReturnOfReturnAuditFile.and.returnValue(of(new Blob(['x'], { type: 'text/plain' })));
    component.generationForm.setValue({ flowIds: '1' });
    component.generateAudit();
    expect(apiSpy.generateReturnOfReturnAuditFile).toHaveBeenCalled();
  });

  it('invoca descarga nacha-file', () => {
    apiSpy.generateReturnOfReturnNachaFile.and.returnValue(of(new Blob(['x'], { type: 'text/plain' })));
    component.generationForm.setValue({ flowIds: '1' });
    spyOn(window, 'confirm').and.returnValue(true);
    component.generateNacha();
    expect(apiSpy.generateReturnOfReturnNachaFile).toHaveBeenCalled();
  });
});
