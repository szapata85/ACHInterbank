import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { CertificateListItem } from '../models/certificate-management.model';
import { CertificateManagementApiService } from '../services/certificate-management-api.service';
import { ClearingHousesApiService } from '../../ach-cycles/services/ach-cycles-api.service';
import { NachaCertificateManagerComponent } from './nacha-certificate-manager.component';

describe('NachaCertificateManagerComponent', () => {
  let fixture: ComponentFixture<NachaCertificateManagerComponent>;
  let component: NachaCertificateManagerComponent;
  let api: jasmine.SpyObj<CertificateManagementApiService>;

  beforeEach(async () => {
    api = jasmine.createSpyObj<CertificateManagementApiService>('CertificateManagementApiService', [
      'list', 'uploadPublic', 'uploadPrivate'
    ]);
    api.list.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [NachaCertificateManagerComponent],
      providers: [
        { provide: CertificateManagementApiService, useValue: api },
        {
          provide: ClearingHousesApiService,
          useValue: { list: () => of([{ id: 1, code: 'ACHCOL', name: 'ACH Colombia', isActive: true }]) }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaCertificateManagerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('shows a public certificate success and clears the file control', () => {
    const certificate = certificateItem('ACHCOL-OUTBOUND-ENCRYPTION', 'ACHcolombia.cer', false);
    api.uploadPublic.and.returnValue(of(certificate));
    api.list.and.returnValue(of([certificate]));
    selectFile('EncryptionPublic', new File(['certificate'], 'ACHcolombia.cer'));

    component.upload('EncryptionPublic');

    expect(api.uploadPublic).toHaveBeenCalledTimes(1);
    expect(component.success).toContain('ACHcolombia.cer');
    expect(component.forms.EncryptionPublic.get('file')?.value).toBeNull();
    expect(component.slots[0].certificate?.fileName).toBe('ACHcolombia.cer');
  });

  it('shows a private certificate success, marks the private key and clears password and file', () => {
    const certificate = certificateItem('CFA-OUTBOUND-SIGNING', 'CFA.pfx', true);
    api.uploadPrivate.and.returnValue(of(certificate));
    api.list.and.returnValue(of([certificate]));
    selectFile('SigningKeyPair', new File(['pkcs12'], 'CFA.pfx'));
    component.forms.SigningKeyPair.get('password')?.setValue('in-memory-only');

    component.upload('SigningKeyPair');

    expect(api.uploadPrivate).toHaveBeenCalledTimes(1);
    expect(component.slots[1].certificate?.hasPrivateKey).toBeTrue();
    expect(component.forms.SigningKeyPair.get('password')?.value).toBe('');
    expect(component.forms.SigningKeyPair.get('file')?.value).toBeNull();
    expect(component.uploadingType).toBeUndefined();
  });

  it('keeps an incorrect-password ProblemDetails visible and never renders object coercion', () => {
    api.uploadPrivate.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 400,
      error: { title: 'Certificado privado inválido', detail: 'La contraseña es incorrecta.' }
    })));
    selectFile('SigningKeyPair', new File(['pkcs12'], 'CFA.pfx'));
    component.forms.SigningKeyPair.get('password')?.setValue('wrong');

    component.upload('SigningKeyPair');
    fixture.detectChanges();

    expect(component.error).toContain('La contraseña es incorrecta.');
    expect(component.error).not.toContain('[object Object]');
    expect(api.list).toHaveBeenCalledTimes(1);
    expect(component.forms.SigningKeyPair.get('password')?.value).toBe('');
  });

  it('prevents a second submission while the first upload is pending', () => {
    const pending = new Subject<CertificateListItem>();
    api.uploadPrivate.and.returnValue(pending.asObservable());
    selectFile('SigningKeyPair', new File(['pkcs12'], 'CFA.pfx'));
    component.forms.SigningKeyPair.get('password')?.setValue('in-memory-only');

    component.upload('SigningKeyPair');
    component.upload('SigningKeyPair');

    expect(api.uploadPrivate).toHaveBeenCalledTimes(1);
    pending.error(new HttpErrorResponse({ status: 500 }));
  });

  it('rejects a PFX selected as a public certificate', () => {
    selectFile('EncryptionPublic', new File(['pkcs12'], 'wrong.pfx'));
    expect(component.fileError('EncryptionPublic')).toContain('Formato no permitido');
    expect(component.forms.EncryptionPublic.invalid).toBeTrue();
  });

  function selectFile(type: 'EncryptionPublic' | 'SigningKeyPair', file: File): void {
    const input = fixture.nativeElement.querySelector(
      `[data-certificate-type="${type}"] input[type="file"]`
    ) as HTMLInputElement;
    Object.defineProperty(input, 'files', { configurable: true, value: [file] });
    input.dispatchEvent(new Event('change'));
    fixture.detectChanges();
  }
});

function certificateItem(code: string, fileName: string, hasPrivateKey: boolean): CertificateListItem {
  return {
    id: hasPrivateKey ? 2 : 1,
    code,
    displayName: code,
    fileName,
    clearingHouseId: 1,
    environment: 'Test',
    purpose: hasPrivateKey ? 'OutboundSigning' : 'OutboundEncryption',
    holderType: hasPrivateKey ? 'Participant' : 'ClearingHouse',
    status: 'Draft',
    versionNumber: 1,
    subject: hasPrivateKey ? 'CN=CFA' : 'CN=ACH COLOMBIA',
    issuer: 'CN=Test CA',
    serialNumber: '01',
    thumbprint: 'THUMBPRINT',
    fingerprintSha256: 'FINGERPRINT',
    notBefore: '2026-01-01T00:00:00Z',
    notAfter: '2027-01-01T00:00:00Z',
    hasPrivateKey,
    keyAlgorithm: 'RSA',
    keySize: 2048,
    signatureAlgorithm: 'sha256RSA',
    uploadedAtUtc: '2026-07-14T00:00:00Z',
    uploadedBy: 'tester'
  };
}
