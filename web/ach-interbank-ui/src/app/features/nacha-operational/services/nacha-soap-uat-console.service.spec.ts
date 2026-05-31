import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, throwError } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { NachaSoapUatConsoleService } from './nacha-soap-uat-console.service';

describe('NachaSoapUatConsoleService', () => {
  let service: NachaSoapUatConsoleService;
  let api: jasmine.SpyObj<ApiService>;

  beforeEach(() => {
    api = jasmine.createSpyObj<ApiService>('ApiService', ['get', 'post', 'put', 'patch', 'delete']);
    api.get.and.callFake(((path: string) => {
      if (path.endsWith('/dashboard')) return of(dashboard());
      if (path.endsWith('/candidates')) return of(candidates());
      if (path.endsWith('/audit')) return of(audit());
      return of(candidates()[0]);
    }) as any);

    TestBed.configureTestingModule({
      providers: [{ provide: ApiService, useValue: api }]
    });

    service = TestBed.inject(NachaSoapUatConsoleService);
  });

  it('ConsoleService_ShouldCallGetOnlyEndpoints', async () => {
    await firstValueFrom(service.getConsoleData());

    expect(api.get).toHaveBeenCalledWith('api/ach/nacha/soap-uat-console/dashboard');
    expect(api.get).toHaveBeenCalledWith('api/ach/nacha/soap-uat-console/candidates');
    expect(api.get).toHaveBeenCalledWith('api/ach/nacha/soap-uat-console/audit');
    expect(api.post).not.toHaveBeenCalled();
    expect(api.put).not.toHaveBeenCalled();
    expect(api.patch).not.toHaveBeenCalled();
    expect(api.delete).not.toHaveBeenCalled();
  });

  it('ConsoleService_ShouldHandlePartialWarnings', async () => {
    const result = await firstValueFrom(service.getDashboard());

    expect(result.isPartialData).toBeTrue();
    expect(result.warnings.join(' ')).toContain('parcial');
  });

  it('ConsoleService_ShouldHandleErrorState', async () => {
    api.get.and.returnValue(throwError(() => ({ status: 404 })));

    await expectAsync(firstValueFrom(service.getCandidate('missing'))).toBeRejectedWithError('Candidato SOAP/UAT no encontrado.');
  });
});

function dashboard() {
  return {
    productiveStatus: 'NO-GO',
    productiveExecution: false,
    wouldInvokeRealSoap: false,
    totalCandidates: 2,
    totalReadyForUat: 0,
    totalBlocked: 1,
    totalManualReview: 1,
    totalRegistrarRespuesta: 1,
    totalProcTransacciones: 1,
    totalProcContrapartidas: 0,
    totalNone: 0,
    totalSimulationPassed: 1,
    totalSimulationFailed: 1,
    totalResilienceWarnings: 1,
    totalDuplicateOrIdempotent: 2,
    lastUpdatedAt: '2026-05-31T12:00:00Z',
    dataSource: 'parcial',
    isPartialData: true,
    warnings: ['Consola parcial read-only.']
  };
}

function candidates() {
  return [
    {
      correlationId: 'corr-proc',
      fileName: 'entrada.ach',
      entryTraceNumber: '***0001',
      decisionType: 'CreditoEntrante',
      operationCandidate: 'ProcTransacciones',
      requiresMonetaryMovement: true,
      productiveExecution: false,
      wouldInvokeRealSoap: false,
      isReadyForUat: false,
      isBlocked: true,
      blockReasons: ['NO-GO'],
      manualReviewRequired: false,
      readinessStatus: 'BlockedByNoGo',
      simulationStatus: 'Passed',
      resilienceStatus: 'Warning',
      idempotencyStatus: 'Idempotent',
      lastAttemptAt: '2026-05-31T12:00:00Z',
      attemptCount: 1,
      dataSource: 'backend read-only',
      isPersisted: true,
      isDerived: true
    }
  ];
}

function audit() {
  return [{ correlationId: 'corr-proc', phase: '6B.5', eventType: 'Audit', severity: 'Information', message: 'Sanitized', isBlocked: false, timestamp: '2026-05-31T12:00:00Z', sanitizedDetails: { Payload: 'Sanitized' }, dataSource: 'backend read-only', isPersisted: true }];
}
