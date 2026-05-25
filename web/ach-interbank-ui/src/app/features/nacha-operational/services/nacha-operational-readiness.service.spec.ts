import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { NachaOperationalReadinessService } from './nacha-operational-readiness.service';

describe('NachaOperationalReadinessService', () => {
  let service: NachaOperationalReadinessService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(NachaOperationalReadinessService);
  });

  it('Service_ShouldReturnOperationalSummaryNoGo', async () => {
    const summary = await firstValueFrom(service.getOperationalSummary());

    expect(summary.productiveStatus).toBe('NO-GO');
    expect(summary.productiveExecution).toBeFalse();
    expect(summary.wouldInvokeRealSoap).toBeFalse();
  });

  it('Service_ShouldReturnFilesReadOnlyDemoData', async () => {
    const files = await firstValueFrom(service.getFiles());

    expect(files.length).toBeGreaterThan(0);
    expect(files.every((file) => !!file.fileName)).toBeTrue();
  });

  it('Service_ShouldReturnDecisionsWithoutRealSoapExecution', async () => {
    const decisions = await firstValueFrom(service.getDecisions());

    expect(decisions.some((decision) => decision.soapOperationCandidate === 'ProcTransacciones')).toBeTrue();
    expect(decisions.every((decision) => decision.soapOperationCandidate !== 'RealSoapInvocation')).toBeTrue();
  });

  it('Service_ShouldReturnSoapReadinessWithRealSoapDisabled', async () => {
    const readiness = await firstValueFrom(service.getSoapReadiness());

    expect(readiness.every((item) => item.productiveExecution === false)).toBeTrue();
    expect(readiness.every((item) => item.wouldInvokeRealSoap === false)).toBeTrue();
  });

  it('Service_ShouldReturnSanitizedAudit', async () => {
    const audit = await firstValueFrom(service.getAudit());
    const joined = JSON.stringify(audit);

    expect(joined).not.toContain('password');
    expect(joined).not.toContain('token');
    expect(joined).not.toContain('1234567890123456');
  });
});
