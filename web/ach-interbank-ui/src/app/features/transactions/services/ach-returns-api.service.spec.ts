import { TestBed } from '@angular/core/testing';
import { AchReturnsApiService } from './ach-returns-api.service';
import { ApiService } from '../../../core/services/api.service';

describe('AchReturnsApiService', () => {
  let service: AchReturnsApiService;
  let apiSpy: jasmine.SpyObj<ApiService>;

  beforeEach(() => {
    apiSpy = jasmine.createSpyObj<ApiService>('ApiService', ['get', 'post', 'postBlob']);
    TestBed.configureTestingModule({ providers: [{ provide: ApiService, useValue: apiSpy }] });
    service = TestBed.inject(AchReturnsApiService);
  });

  it('evaluateReturnOfReturn llama endpoint correcto', () => {
    apiSpy.post.and.returnValue({ subscribe() {} } as any);
    service.evaluateReturnOfReturn({ sourceReturnTransactionId: 10, newReturnReasonCode: 'R02' });
    expect(apiSpy.post).toHaveBeenCalledWith('ach-returns/return-of-return/evaluate', jasmine.any(Object));
  });

  it('generateReturnOfReturnAuditFile llama endpoint blob correcto', () => {
    apiSpy.postBlob.and.returnValue({ subscribe() {} } as any);
    service.generateReturnOfReturnAuditFile({ flowIds: [10] });
    expect(apiSpy.postBlob).toHaveBeenCalledWith('ach-returns/return-of-return/generate-audit-file', jasmine.any(Object));
  });

  it('generateReturnOfReturnNachaFile llama endpoint blob correcto', () => {
    apiSpy.postBlob.and.returnValue({ subscribe() {} } as any);
    service.generateReturnOfReturnNachaFile({ flowIds: [10] });
    expect(apiSpy.postBlob).toHaveBeenCalledWith('ach-returns/return-of-return/generate-nacha-file', jasmine.any(Object));
  });
});
