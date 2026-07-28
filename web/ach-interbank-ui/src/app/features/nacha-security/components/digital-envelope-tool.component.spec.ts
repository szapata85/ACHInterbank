import { HttpErrorResponse, HttpHeaders, HttpResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { ApplicationDownloadError, BlobDownloadService } from '../../../core/services/blob-download.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ClearingHousesApiService } from '../../ach-cycles/services/ach-cycles-api.service';
import { SobreDigitalCertificate, SobreDigitalService } from '../services/sobre-digital.service';
import { DigitalEnvelopeToolComponent } from './digital-envelope-tool.component';

describe('DigitalEnvelopeToolComponent', () => {
  let fixture: ComponentFixture<DigitalEnvelopeToolComponent>;
  let component: DigitalEnvelopeToolComponent;
  let service: jasmine.SpyObj<SobreDigitalService>;
  let downloads: jasmine.SpyObj<BlobDownloadService>;
  let notifications: jasmine.SpyObj<NotificationService>;

  beforeEach(async () => {
    service = jasmine.createSpyObj<SobreDigitalService>('SobreDigitalService', [
      'listCertificates', 'encrypt', 'decrypt'
    ]);
    service.listCertificates.and.returnValue(of(certificates));

    downloads = jasmine.createSpyObj<BlobDownloadService>('BlobDownloadService', ['save', 'fromHttpError']);
    downloads.save.and.resolveTo({
      fileName: '0001283.001.20260728.1.OUT.ENV',
      size: 2048,
      contentType: 'application/octet-stream'
    });
    downloads.fromHttpError.and.resolveTo(new ApplicationDownloadError('Error controlado', 422));
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'error']);

    await TestBed.configureTestingModule({
      imports: [DigitalEnvelopeToolComponent],
      providers: [
        provideNoopAnimations(),
        provideRouter([]),
        { provide: SobreDigitalService, useValue: service },
        { provide: BlobDownloadService, useValue: downloads },
        { provide: NotificationService, useValue: notifications },
        {
          provide: ClearingHousesApiService,
          useValue: { list: () => of([{ id: 1, code: 'ACHCOL', name: 'ACH Colombia', isActive: true }]) }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(DigitalEnvelopeToolComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('presenta pestañas y lenguaje operativo completamente en español', () => {
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Sobre digital NACHA-M');
    expect(text).toContain('Cifrar archivo');
    expect(text).toContain('Descifrar archivo');
    expect(text).not.toContain('Encrypt');
    expect(text).not.toContain('Decrypt');
    expect(text).not.toContain('OutboundEncryption');
    expect(text).not.toContain('InboundDecryption');
  });

  it('resuelve de forma automática y determinística la versión activa más reciente', () => {
    expect(component.selectedCertificate('encrypt')?.id).toBe(11);
    expect(component.selectedCertificate('decrypt')?.id).toBe(20);
    expect(component.hasDuplicateConfiguration('encrypt')).toBeTrue();
  });

  it('valida archivo vacío, tamaño y extensión del sobre digital', () => {
    selectFile('encrypt', new File([], 'vacio.OUT'));
    expect(component.fileError('encrypt')).toBe('El archivo está vacío.');

    selectFile('decrypt', new File(['contenido'], 'archivo.txt'));
    expect(component.fileError('decrypt')).toContain('.ENV');
  });

  it('cifra, descarga el Blob y muestra un resultado seguro', async () => {
    service.encrypt.and.returnValue(of(successResponse()));
    selectFile('encrypt', new File(['nacha'], '0001283.001.20260728.1.OUT'));

    component.submitEncrypt();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(service.encrypt).toHaveBeenCalledOnceWith(
      jasmine.any(File),
      11
    );
    expect(downloads.save).toHaveBeenCalledTimes(1);
    expect(component.result?.mode).toBe('encrypt');
    expect(fixture.nativeElement.textContent).toContain('Firma incorporada');
  });

  it('descifra únicamente .ENV y confirma firma e integridad cuando el backend responde exitosamente', async () => {
    downloads.save.and.resolveTo({
      fileName: '0001283.001.20260728.1.OUT',
      size: 106,
      contentType: 'application/octet-stream'
    });
    service.decrypt.and.returnValue(of(successResponse()));
    selectFile('decrypt', new File(['envelope'], '0001283.001.20260728.1.OUT.ENV'));

    component.submitDecrypt();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(service.decrypt).toHaveBeenCalledOnceWith(jasmine.any(File), 20);
    expect(fixture.nativeElement.textContent).toContain('Firma digital válida');
    expect(fixture.nativeElement.textContent).toContain('Integridad confirmada');
  });

  it('previene doble envío mientras una operación está pendiente', () => {
    const pending = new Subject<HttpResponse<Blob>>();
    service.encrypt.and.returnValue(pending);
    selectFile('encrypt', new File(['nacha'], 'archivo.OUT'));

    component.submitEncrypt();
    component.submitEncrypt();

    expect(service.encrypt).toHaveBeenCalledTimes(1);
    pending.complete();
  });

  it('traduce un error criptográfico Blob y conserva el código solo para soporte', async () => {
    service.decrypt.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 422,
      error: new Blob([JSON.stringify({
        detail: 'El sobre digital está alterado.',
        errorCode: 'ENVELOPE_INTEGRITY_INVALID',
        traceId: 'trace-envelope-42'
      })], { type: 'application/problem+json' })
    })));
    downloads.fromHttpError.and.resolveTo(new ApplicationDownloadError(
      'El sobre digital está alterado.',
      422,
      'ENVELOPE_INTEGRITY_INVALID',
      'trace-envelope-42'
    ));
    selectFile('decrypt', new File(['envelope'], 'archivo.ENV'));

    component.submitDecrypt();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.operationError?.title).toBe('No se confirmó la integridad del archivo');
    expect(fixture.nativeElement.textContent).toContain('Información para soporte');
    expect(fixture.nativeElement.textContent.match(/ENVELOPE_INTEGRITY_INVALID/g)?.length).toBe(1);
    expect(component.processingMode).toBeNull();
  });

  it('vuelve a intentar el mismo flujo que produjo el error', async () => {
    service.decrypt.and.returnValue(throwError(() => new HttpErrorResponse({ status: 422 })));
    selectFile('decrypt', new File(['envelope'], 'archivo.ENV'));

    component.submitDecrypt();
    await fixture.whenStable();
    component.retry();
    await fixture.whenStable();

    expect(service.decrypt).toHaveBeenCalledTimes(2);
    expect(service.encrypt).not.toHaveBeenCalled();
  });

  function selectFile(mode: 'encrypt' | 'decrypt', file: File): void {
    component.onFileSelected(mode, {
      target: { files: [file] }
    } as unknown as Event);
    fixture.detectChanges();
  }
});

const certificates: SobreDigitalCertificate[] = [
  {
    id: 10,
    code: 'ACHCOL-ENC',
    displayName: 'Cifrado anterior',
    fileName: 'anterior.cer',
    clearingHouseId: 1,
    environment: 1,
    purpose: 1,
    versionNumber: 1,
    hasPrivateKey: false,
    thumbprintMasked: 'A954AA...D3D701',
    notBefore: '2026-01-01T00:00:00Z',
    notAfter: '2027-01-01T00:00:00Z',
    canEncrypt: true,
    canDecrypt: false
  },
  {
    id: 11,
    code: 'ACHCOL-ENC',
    displayName: 'Cifrado vigente',
    fileName: 'vigente.cer',
    clearingHouseId: 1,
    environment: 1,
    purpose: 1,
    versionNumber: 2,
    hasPrivateKey: false,
    thumbprintMasked: 'B954AA...D3D702',
    notBefore: '2026-01-01T00:00:00Z',
    notAfter: '2027-01-01T00:00:00Z',
    canEncrypt: true,
    canDecrypt: false
  },
  {
    id: 20,
    code: 'CFA-DEC',
    displayName: 'Identidad privada CFA',
    fileName: 'identidad.pfx',
    clearingHouseId: 1,
    environment: 1,
    purpose: 2,
    versionNumber: 1,
    hasPrivateKey: true,
    thumbprintMasked: 'C954AA...D3D703',
    notBefore: '2026-01-01T00:00:00Z',
    notAfter: '2027-01-01T00:00:00Z',
    canEncrypt: true,
    canDecrypt: true
  }
];

function successResponse(): HttpResponse<Blob> {
  return new HttpResponse({
    status: 200,
    body: new Blob(['binary'], { type: 'application/octet-stream' }),
    headers: new HttpHeaders({
      'content-disposition': 'attachment; filename="resultado.ENV"',
      'content-type': 'application/octet-stream',
      'x-cryptographic-profile': 'ACH-V32'
    })
  });
}
