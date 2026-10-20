import { TestBed } from '@angular/core/testing';
import { BrowserPageReloader, CHUNK_RELOAD_GUARD_WINDOW_MS, CHUNK_RELOAD_STORAGE_KEY, ChunkLoadRecoveryService } from './chunk-load-recovery.service';

describe('ChunkLoadRecoveryService', () => {
  let service: ChunkLoadRecoveryService;
  let pageReloader: { reload: jasmine.Spy };
  let now = 1_700_000_000_000;

  beforeEach(() => {
    sessionStorage.clear();
    localStorage.clear();
    pageReloader = {
      reload: jasmine.createSpy('reload')
    };

    spyOn(Date, 'now').and.callFake(() => now);

    TestBed.configureTestingModule({
      providers: [
        ChunkLoadRecoveryService,
        { provide: BrowserPageReloader, useValue: pageReloader }
      ]
    });

    service = TestBed.inject(ChunkLoadRecoveryService);
  });

  it('should recognize ChunkLoadError and reload once', () => {
    const handled = service.handle(createChunkError('Loading chunk 76 failed.\n(http://localhost:743/common.0eaf8c717cf19b2d.js)'));

    expect(handled).toBeTrue();
    expect(pageReloader.reload).toHaveBeenCalledTimes(1);
    expect(sessionStorage.getItem('ach.interbank.access_token')).toBeNull();
    expect(sessionStorage.getItem(CHUNK_RELOAD_STORAGE_KEY)).toContain(String(now));
  });

  it('should recognize Loading chunk failed without relying on the error name', () => {
    const handled = service.handle(new Error('Loading chunk 76 failed.'));

    expect(handled).toBeTrue();
    expect(pageReloader.reload).toHaveBeenCalledTimes(1);
  });

  it('should recognize dynamic import failures from rejected promises', () => {
    const handled = service.handle(new Error('Failed to fetch dynamically imported module'));

    expect(handled).toBeTrue();
    expect(pageReloader.reload).toHaveBeenCalledTimes(1);
  });

  it('should ignore normal functional errors', () => {
    const handled = service.handle(new Error('Validation failed'));

    expect(handled).toBeFalse();
    expect(pageReloader.reload).not.toHaveBeenCalled();
    expect(sessionStorage.getItem(CHUNK_RELOAD_STORAGE_KEY)).toBeNull();
  });

  it('should reload only once during the guard window', () => {
    const first = service.handle(createChunkError('Loading chunk 76 failed.'));
    const second = service.handle(createChunkError('Loading chunk 76 failed.'));

    expect(first).toBeTrue();
    expect(second).toBeFalse();
    expect(pageReloader.reload).toHaveBeenCalledTimes(1);
  });

  it('should preserve session and local storage contents while recovering', () => {
    sessionStorage.setItem('ach.interbank.access_token', 'token-value');
    localStorage.setItem('ach.bulk.recentBatchIds', JSON.stringify(['batch-1']));

    const handled = service.handle(createChunkError('ChunkLoadError'));

    expect(handled).toBeTrue();
    expect(pageReloader.reload).toHaveBeenCalledTimes(1);
    expect(sessionStorage.getItem('ach.interbank.access_token')).toBe('token-value');
    expect(localStorage.getItem('ach.bulk.recentBatchIds')).toBe(JSON.stringify(['batch-1']));
  });

  it('should allow another recovery after the guard expires', () => {
    expect(service.handle(createChunkError('Loading chunk 76 failed.'))).toBeTrue();
    expect(pageReloader.reload).toHaveBeenCalledTimes(1);

    now += CHUNK_RELOAD_GUARD_WINDOW_MS + 1;

    expect(service.handle(createChunkError('Loading chunk 76 failed.'))).toBeTrue();
    expect(pageReloader.reload).toHaveBeenCalledTimes(2);
  });
});

function createChunkError(message: string): Error {
  const error = new Error(message);
  error.name = 'ChunkLoadError';
  return error;
}
