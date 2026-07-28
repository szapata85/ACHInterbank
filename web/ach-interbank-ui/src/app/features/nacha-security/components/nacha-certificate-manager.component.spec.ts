import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { ApplicationDownloadError, BlobDownloadService } from '../../../core/services/blob-download.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ClearingHousesApiService } from '../../ach-cycles/services/ach-cycles-api.service';
import { CertificateListItem } from '../models/certificate-management.model';
import { CertificateManagementApiService } from '../services/certificate-management-api.service';
import { NachaCertificateManagerComponent } from './nacha-certificate-manager.component';

describe('NachaCertificateManagerComponent', () => {
  let fixture: ComponentFixture<NachaCertificateManagerComponent>;
  let component: NachaCertificateManagerComponent;
  let api: jasmine.SpyObj<CertificateManagementApiService>;
  let downloads: jasmine.SpyObj<BlobDownloadService>;
  let notifications: jasmine.SpyObj<NotificationService>;
  let dialog: MatDialog;
  let openDialog: jasmine.Spy;

  beforeEach(async () => {
    api = jasmine.createSpyObj<CertificateManagementApiService>('CertificateManagementApiService', [
      'list', 'uploadPublic', 'uploadPrivate', 'validate', 'activate', 'revoke'
    ]);
    api.list.and.returnValue(of([]));
    api.validate.and.returnValue(of({ isValid: true, errors: [] }));

    downloads = jasmine.createSpyObj<BlobDownloadService>('BlobDownloadService', ['fromHttpError']);
    downloads.fromHttpError.and.resolveTo(new ApplicationDownloadError('Error controlado', 400));
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'error']);
    await TestBed.configureTestingModule({
      imports: [NachaCertificateManagerComponent],
      providers: [
        provideNoopAnimations(),
        provideRouter([]),
        { provide: CertificateManagementApiService, useValue: api },
        { provide: BlobDownloadService, useValue: downloads },
        { provide: NotificationService, useValue: notifications },
        { provide: AuthService, useValue: { hasPermission: () => true } },
        {
          provide: ClearingHousesApiService,
          useValue: {
            list: () => of([
              { id: 1, code: 'ACHCOL', name: 'ACH Colombia', isActive: true },
              { id: 3, code: 'REDTEST', name: 'Red sintética', isActive: true }
            ])
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaCertificateManagerComponent);
    component = fixture.componentInstance;
    dialog = (component as unknown as { dialog: MatDialog }).dialog;
    openDialog = spyOn(dialog, 'open');
    fixture.detectChanges();
  });

  it('crea filtros reactivos con cámara y ambiente obligatorios', () => {
    expect(component.contextForm.controls.clearingHouseId.value).toBe(1);
    expect(component.contextForm.controls.environment.value).toBe('Test');
    expect(component.contextForm.valid).toBeTrue();
    expect(fixture.nativeElement.textContent).toContain('Cámara compensadora');
    expect(fixture.nativeElement.textContent).toContain('Propósito');
  });

  it('traduce propósito, titular, estado y ambiente aunque la API serialice enums numéricos', () => {
    const certificate = certificateItem({ purpose: 1, holderType: 2, status: 1, environment: 1 });
    api.list.and.returnValue(of([certificate]));

    component.refresh();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Cifrado de salida');
    expect(text).toContain('Cámara compensadora');
    expect(text).toContain('Borrador');
    expect(text).toContain('Pruebas');
    expect(text).not.toContain('OutboundEncryption');
    expect(text).not.toContain('ClearingHouse');
    expect(text).not.toContain('Draft');
  });

  it('distingue certificados vencidos y próximos a vencer', () => {
    const expired = certificateItem({ id: 1, status: 'Active', notAfter: '2026-07-20T00:00:00Z' });
    const expiring = certificateItem({ id: 2, status: 'Active', notAfter: '2026-08-10T00:00:00Z' });
    api.list.and.returnValue(of([expired, expiring]));

    component.refresh();

    expect(component.summary.expired).toBe(1);
    expect(component.summary.expiring).toBe(1);
    expect(component.statusLabel(expired)).toBe('Vencido');
    expect(component.validityMessage(expiring)).toContain('vence dentro');
  });

  it('carga una versión pública, evita doble envío y limpia el archivo', () => {
    component.showUploadPanel = true;
    fixture.detectChanges();
    const pending = new Subject<CertificateListItem>();
    api.uploadPublic.and.returnValue(pending);
    selectFile('EncryptionPublic', new File(['certificate'], 'ACHcolombia.cer'));

    component.upload('EncryptionPublic');
    component.upload('EncryptionPublic');

    expect(api.uploadPublic).toHaveBeenCalledTimes(1);
    pending.next(certificateItem({ fileName: 'ACHcolombia.cer' }));
    pending.complete();
    expect(component.forms.EncryptionPublic.get('file')?.value).toBeNull();
  });

  it('limpia siempre la contraseña privada y presenta Problem Details sin coerción de objeto', async () => {
    component.showUploadPanel = true;
    fixture.detectChanges();
    api.uploadPrivate.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 400,
      error: { title: 'Certificado privado inválido', detail: 'La contraseña es incorrecta.' }
    })));
    downloads.fromHttpError.and.resolveTo(new ApplicationDownloadError(
      'La contraseña es incorrecta.',
      400,
      undefined,
      undefined,
      'Certificado privado inválido'
    ));
    selectFile('SigningKeyPair', new File(['pkcs12'], 'CFA.pfx'));
    component.forms.SigningKeyPair.get('password')?.setValue('in-memory-only');

    component.upload('SigningKeyPair');
    await fixture.whenStable();

    expect(component.forms.SigningKeyPair.get('password')?.value).toBe('');
    expect(component.operationError?.message).toContain('La contraseña es incorrecta');
    expect(component.operationError?.message).not.toContain('[object Object]');
  });

  it('rechaza formatos incompatibles antes de llamar a la API', () => {
    component.showUploadPanel = true;
    fixture.detectChanges();
    selectFile('EncryptionPublic', new File(['pkcs12'], 'wrong.pfx'));

    component.upload('EncryptionPublic');

    expect(component.fileError('EncryptionPublic')).toContain('Formato no permitido');
    expect(api.uploadPublic).not.toHaveBeenCalled();
  });

  it('activa una versión mediante validación previa y confirmación', () => {
    const certificate = certificateItem({ status: 'Draft' });
    api.activate.and.returnValue(of({ ...certificate, status: 'Active' }));
    openDialog.and.returnValue({ afterClosed: () => of(true) } as never);

    component.requestActivate(certificate);

    expect(api.validate).toHaveBeenCalledOnceWith(certificate.id);
    expect(api.activate).toHaveBeenCalledOnceWith(certificate.id);
  });

  it('exige un motivo para revocar y lo envía al contrato existente', () => {
    const certificate = certificateItem({ status: 'Active' });
    api.revoke.and.returnValue(of({ ...certificate, status: 'Revoked' }));
    openDialog.and.returnValue({ afterClosed: () => of('Rotación operativa controlada') } as never);

    component.requestRevoke(certificate);

    expect(api.revoke).toHaveBeenCalledOnceWith(certificate.id, 'Rotación operativa controlada');
  });

  it('abre el detalle seguro sin exponer material privado ni referencias internas', () => {
    const certificate = certificateItem({ hasPrivateKey: true, secretRefMasked: '****1234' });
    openDialog.and.returnValue({} as never);

    component.openDetails(certificate);

    expect(openDialog).toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).not.toContain('****1234');
  });

  it('construye códigos por cámara sin hardcodear ACH Colombia o CENIT', () => {
    component.contextForm.controls.clearingHouseId.setValue(3);
    component.applyFilters();

    expect(component.slots[0].code).toBe('REDTEST-OUTBOUND-ENCRYPTION');
    expect(component.slots[1].code).toBe('CFA-REDTEST-OUTBOUND-SIGNING');
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

function certificateItem(overrides: Partial<CertificateListItem> = {}): CertificateListItem {
  return {
    id: 1,
    code: 'ACHCOL-OUTBOUND-ENCRYPTION',
    displayName: 'ACH Colombia - cifrado de salida',
    fileName: 'ACHcolombia.cer',
    clearingHouseId: 1,
    environment: 'Test',
    purpose: 'OutboundEncryption',
    holderType: 'ClearingHouse',
    status: 'Draft',
    versionNumber: 1,
    subject: 'CN=ACH COLOMBIA',
    issuer: 'CN=Autoridad de prueba',
    serialNumber: '01',
    thumbprint: 'A954AABBCCDDEEFF001122334455667788D3D701',
    fingerprintSha256: 'A954AABBCCDDEEFF001122334455667788D3D701',
    notBefore: '2026-01-01T00:00:00Z',
    notAfter: '2027-01-01T00:00:00Z',
    hasPrivateKey: false,
    keyAlgorithm: 'RSA',
    keySize: 2048,
    signatureAlgorithm: 'sha256RSA',
    uploadedAtUtc: '2026-07-14T00:00:00Z',
    uploadedBy: 'tester',
    ...overrides
  };
}
