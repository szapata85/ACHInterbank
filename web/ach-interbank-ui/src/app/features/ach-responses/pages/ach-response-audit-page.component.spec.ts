import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { of, Subject, throwError } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { AchResponsesApiService } from '../services/ach-responses-api.service';
import { AchResponseAuditPageComponent } from './ach-response-audit-page.component';

describe('AchResponseAuditPageComponent', () => {
  let apiSpy: jasmine.SpyObj<AchResponsesApiService>;
  let notificationSpy: jasmine.SpyObj<NotificationService>;

  const auditEntries = [
    {
      id: 1,
      entityType: 'response',
      entityId: 'resp-1',
      action: 'Created',
      previousState: null,
      newState: 'Recibida',
      actor: 'system',
      reason: 'Ingreso',
      correlationId: 'corr-1',
      occurredAtUtc: '2026-01-01T00:00:00Z',
      sanitizedMetadata: '{"source":"api"}'
    }
  ] as any;

  beforeEach(() => {
    apiSpy = jasmine.createSpyObj<AchResponsesApiService>('AchResponsesApiService', ['getResponseAudit', 'getMappingAudit']);
    apiSpy.getResponseAudit.and.returnValue(of(auditEntries));
    apiSpy.getMappingAudit.and.returnValue(of(auditEntries));
    notificationSpy = jasmine.createSpyObj<NotificationService>('NotificationService', ['error']);

    TestBed.configureTestingModule({
      imports: [AchResponseAuditPageComponent],
      providers: [
        { provide: AchResponsesApiService, useValue: apiSpy },
        { provide: NotificationService, useValue: notificationSpy }
      ]
    });
  });

  it('AchResponseAuditPageComponent_ShouldUseResponseAuditForResponseEntity', () => {
    const fixture = TestBed.createComponent(AchResponseAuditPageComponent);
    const component = fixture.componentInstance;
    component.form.setValue({ entityType: 'response', entityId: 'resp-1' });

    component.load();

    expect(apiSpy.getResponseAudit).toHaveBeenCalledWith('resp-1');
    expect(apiSpy.getMappingAudit).not.toHaveBeenCalled();
    expect(component.entries.length).toBe(1);
  });

  it('AchResponseAuditPageComponent_ShouldKeepMappingValueAndUseMappingAudit', () => {
    const fixture = TestBed.createComponent(AchResponseAuditPageComponent);
    const component = fixture.componentInstance;
    component.form.setValue({ entityType: 'mapping', entityId: '42' });

    component.load();

    expect(component.form.controls.entityType.value).toBe('mapping');
    expect(apiSpy.getMappingAudit).toHaveBeenCalledWith(42);
    expect(apiSpy.getResponseAudit).not.toHaveBeenCalled();
  });

  it('AchResponseAuditPageComponent_ShouldExposeCanonicalLoadingState', () => {
    const pending = new Subject<any[]>();
    apiSpy.getResponseAudit.and.returnValue(pending);
    const fixture = TestBed.createComponent(AchResponseAuditPageComponent);
    const component = fixture.componentInstance;
    component.form.setValue({ entityType: 'response', entityId: 'resp-1' });

    component.load();
    fixture.detectChanges();

    expect(component.loading).toBeTrue();
    expect(fixture.debugElement.query(By.css('ui-estado-carga'))).toBeTruthy();

    pending.next(auditEntries);
    pending.complete();
  });

  it('AchResponseAuditPageComponent_ShouldExposeCanonicalEmptyState', () => {
    apiSpy.getResponseAudit.and.returnValue(of([]));
    const fixture = TestBed.createComponent(AchResponseAuditPageComponent);
    const component = fixture.componentInstance;
    component.form.setValue({ entityType: 'response', entityId: 'resp-1' });

    component.load();
    fixture.detectChanges();

    expect(component.loaded).toBeTrue();
    expect(component.entries.length).toBe(0);
    expect(fixture.debugElement.query(By.css('ui-estado-vacio'))).toBeTruthy();
  });

  it('AchResponseAuditPageComponent_ShouldExposeCanonicalErrorState', () => {
    apiSpy.getResponseAudit.and.returnValue(throwError(() => new Error('fail')));
    const fixture = TestBed.createComponent(AchResponseAuditPageComponent);
    const component = fixture.componentInstance;
    component.form.setValue({ entityType: 'response', entityId: 'resp-1' });

    component.load();
    fixture.detectChanges();

    expect(component.error).toBeTrue();
    expect(component.loading).toBeFalse();
    expect(component.entries).toEqual([]);
    expect(notificationSpy.error).toHaveBeenCalledWith('No fue posible cargar la auditoría.');
    expect(fixture.debugElement.query(By.css('ui-estado-error'))).toBeTruthy();
  });

  it('AchResponseAuditPageComponent_ShouldRejectMissingEntityIdWithoutApiCall', () => {
    const fixture = TestBed.createComponent(AchResponseAuditPageComponent);
    const component = fixture.componentInstance;
    component.form.setValue({ entityType: 'response', entityId: '' });

    component.load();

    expect(apiSpy.getResponseAudit).not.toHaveBeenCalled();
    expect(apiSpy.getMappingAudit).not.toHaveBeenCalled();
    expect(notificationSpy.error).toHaveBeenCalledWith('Indique el identificador de la entidad.');
  });
});
