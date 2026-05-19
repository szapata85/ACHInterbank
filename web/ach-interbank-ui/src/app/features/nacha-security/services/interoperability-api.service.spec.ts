import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { ApiService } from '../../../core/services/api.service';
import { InteroperabilityStatus } from '../models/nacha-security-operation.model';
import { InteroperabilityApiService } from './interoperability-api.service';

describe('InteroperabilityApiService', () => {
  let service: InteroperabilityApiService;
  let apiSpy: jasmine.SpyObj<ApiService>;

  beforeEach(() => {
    apiSpy = jasmine.createSpyObj<ApiService>('ApiService', ['get', 'post']);

    TestBed.configureTestingModule({
      providers: [
        InteroperabilityApiService,
        { provide: ApiService, useValue: apiSpy }
      ]
    });

    service = TestBed.inject(InteroperabilityApiService);
  });

  it('InteroperabilityApiService_ShouldUseDocumentedPendingStatusRoute', () => {
    const status: InteroperabilityStatus = {
      officialVectorStatus: 'Pending',
      officialMetadataLoaded: false,
      goNoGo: 'NO_GO',
      identifierIvHardening: {
        allowed: false,
        reason: 'PENDIENTE VALIDAR backend'
      }
    };
    apiSpy.get.and.returnValue(of(status));

    service.getStatus().subscribe();

    expect(apiSpy.get).toHaveBeenCalledWith('nacha-security/interoperability/status');
  });

  it('InteroperabilityApiService_ShouldUseDocumentedPendingHarnessRoute', () => {
    apiSpy.post.and.returnValue(of({}));

    service.runHarness().subscribe();

    expect(apiSpy.post).toHaveBeenCalledWith('nacha-security/interoperability/run-harness', {});
  });

  it('InteroperabilityApiService_ShouldUseDocumentedPendingReportRoute', () => {
    apiSpy.get.and.returnValue(of({}));

    service.getReport('REP-001').subscribe();

    expect(apiSpy.get).toHaveBeenCalledWith('nacha-security/interoperability/reports/REP-001');
  });
});
