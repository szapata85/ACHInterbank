import { HttpErrorResponse, HttpHeaders, HttpResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import {
  ApplicationDownloadError,
  BlobDownloadService,
  extractContentDispositionFileName
} from './blob-download.service';

describe('BlobDownloadService', () => {
  let service: BlobDownloadService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(BlobDownloadService);
  });

  it('lee filename y filename* UTF-8 desde Content-Disposition', () => {
    expect(extractContentDispositionFileName('attachment; filename="archivo.OUT"')).toBe('archivo.OUT');
    expect(extractContentDispositionFileName("attachment; filename*=UTF-8''C%C3%A1mara%20ACH.ENV"))
      .toBe('Cámara ACH.ENV');
  });

  it('rechaza nombres inseguros', () => {
    expect(extractContentDispositionFileName('attachment; filename="../secreto.txt"')).toBeNull();
  });

  it('convierte Problem Details recibido como Blob en error tipado', async () => {
    const problem = new Blob([JSON.stringify({
      title: 'Perfil no disponible',
      detail: 'No existe un perfil vigente.',
      errorCode: 'NACHA_PROFILE_NOT_PUBLISHED',
      traceId: 'trace-42'
    })], { type: 'application/problem+json' });

    const result = await service.fromHttpError(
      new HttpErrorResponse({ status: 422, error: problem }),
      'Error de descarga'
    );

    expect(result).toEqual(jasmine.any(ApplicationDownloadError));
    expect(result.message).toBe('No existe un perfil vigente.');
    expect(result.errorCode).toBe('NACHA_PROFILE_NOT_PUBLISHED');
    expect(result.traceId).toBe('trace-42');
  });

  it('rechaza una respuesta exitosa cuyo contenido es Problem Details', async () => {
    const response = new HttpResponse({
      status: 200,
      body: new Blob([JSON.stringify({ detail: 'Error funcional' })], { type: 'application/problem+json' }),
      headers: new HttpHeaders({
        'content-type': 'application/problem+json',
        'content-disposition': 'attachment; filename="error.json"'
      })
    });

    await expectAsync(service.save(response)).toBeRejectedWithError(ApplicationDownloadError, 'Error funcional');
  });

  it('rechaza HTML aunque la respuesta HTTP sea exitosa', async () => {
    const response = new HttpResponse({
      status: 200,
      body: new Blob(['<html>Error de proxy</html>'], { type: 'text/html' }),
      headers: new HttpHeaders({
        'content-type': 'text/html',
        'content-disposition': 'attachment; filename="archivo.ach"'
      })
    });

    await expectAsync(service.save(response))
      .toBeRejectedWithError(ApplicationDownloadError, 'El servidor devolvió una página HTML en lugar del archivo solicitado.');
  });
});
