import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { IncomingNachaOrphan, IncomingNachaOrphanCandidate } from '../models/incoming-nacha-command-center.models';
import { IncomingNachaCommandCenterApiService } from '../services/incoming-nacha-command-center-api.service';
import { IncomingNachaOrphansPageComponent } from './incoming-nacha-orphans-page.component';

describe('IncomingNachaOrphansPageComponent', () => {
  let api: jasmine.SpyObj<IncomingNachaCommandCenterApiService>;
  let notifications: jasmine.SpyObj<NotificationService>;

  const orphan: IncomingNachaOrphan = {
    id: 'orphan-1', ingestionId: 'ingestion-1', fileName: 'return.OUT', receivedAtUtc: '2026-08-08T12:00:00Z',
    clearingHouseId: 1, achCycleId: 'C1', operationalDate: '2026-08-08', entryDetailId: 10,
    addendaRecordId: 20, amount: 125.5, traceNumber: '000100000000999', originalTraceNumber: '123456780000001',
    returnReasonCode: 'R01', returnReasonDescription: 'Fondos insuficientes', accountNumberMasked: '****2222',
    recipientNameMasked: 'U***', originInstitution: '****0001', destinationInstitution: '****0002',
    linkType: 'Ambiguous', candidateTransactionIds: [501], resolutionStatus: 'Sin relación', resolvedBy: ''
  };
  const candidate: IncomingNachaOrphanCandidate = {
    achTransactionId: 501, traceNumber: '123456780000001', amount: 125.5, effectiveEntryDate: '2026-08-08',
    state: 'Pending', accountNumberMasked: '****2222', originInstitution: '****0001', destinationInstitution: '****0002',
    isCompatible: true, incompatibilityReasons: []
  };

  beforeEach(() => {
    api = jasmine.createSpyObj<IncomingNachaCommandCenterApiService>(
      'IncomingNachaCommandCenterApiService',
      ['getOrphans', 'getOrphan', 'getOrphanCandidates', 'resolveOrphan']);
    api.getOrphans.and.returnValue(of({ items: [orphan], page: 1, pageSize: 50, totalItems: 1 }));
    api.getOrphanCandidates.and.returnValue(of([candidate]));
    api.getOrphan.and.returnValue(of({ ...orphan, resolutionStatus: 'Resuelta', resolvedAchTransactionId: 501, resolvedBy: 'operador.uat' }));
    api.resolveOrphan.and.returnValue(of({
      isResolved: true, status: 'Applied', message: 'Aplicada', isIdempotentReplay: false
    }));
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'error']);

    TestBed.configureTestingModule({
      imports: [IncomingNachaOrphansPageComponent, NoopAnimationsModule],
      providers: [
        { provide: IncomingNachaCommandCenterApiService, useValue: api },
        { provide: NotificationService, useValue: notifications },
        provideRouter([])
      ]
    });
  });

  it('debe consultar y mostrar las devoluciones pendientes', () => {
    const fixture = TestBed.createComponent(IncomingNachaOrphansPageComponent);
    fixture.detectChanges();

    expect(api.getOrphans).toHaveBeenCalled();
    expect(fixture.componentInstance.orphans).toEqual([orphan]);
    expect(fixture.nativeElement.textContent).toContain('Devoluciones recibidas sin relación');
    expect(fixture.nativeElement.textContent).toContain('R01');
  });

  it('debe exigir confirmación y justificación antes de relacionar', () => {
    const fixture = TestBed.createComponent(IncomingNachaOrphansPageComponent);
    fixture.detectChanges();
    fixture.componentInstance.selectOrphan(orphan);
    fixture.componentInstance.resolve();

    expect(api.getOrphanCandidates).toHaveBeenCalledWith('orphan-1');
    expect(api.resolveOrphan).not.toHaveBeenCalled();
    expect(fixture.componentInstance.resolutionForm.invalid).toBeTrue();
  });

  it('debe relacionar una candidata compatible y refrescar el estado', () => {
    const fixture = TestBed.createComponent(IncomingNachaOrphansPageComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.selectOrphan(orphan);
    component.resolutionForm.patchValue({
      candidateId: 501,
      justification: 'Validación operativa controlada',
      comment: 'Coinciden los datos',
      confirmed: true
    });

    component.resolve();

    expect(api.resolveOrphan).toHaveBeenCalledWith('orphan-1', jasmine.objectContaining({ achTransactionId: 501 }));
    expect(notifications.success).toHaveBeenCalled();
    expect(component.selected?.resolutionStatus).toBe('Resuelta');
    expect(component.orphans).toEqual([]);
  });
});
