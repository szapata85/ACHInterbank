import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { BlobDownloadService } from '../../../core/services/blob-download.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ClearingHousesApiService } from '../../ach-cycles/services/ach-cycles-api.service';
import { FinancialInstitutionsApiService } from '../../transactions/services/financial-institutions-api.service';
import { CertificateListItem } from '../models/certificate-management.model';
import { CertificateManagementApiService } from '../services/certificate-management-api.service';
import { NachaCertificateManagerComponent } from './nacha-certificate-manager.component';

describe('NachaCertificateManagerComponent', () => {
  let fixture: ComponentFixture<NachaCertificateManagerComponent>;
  let component: NachaCertificateManagerComponent;
  let api: jasmine.SpyObj<CertificateManagementApiService>;
  let dialog: MatDialog;

  beforeEach(async () => {
    api = jasmine.createSpyObj('CertificateManagementApiService', [
      'list', 'revoke', 'delete', 'downloadPublic'
    ]);
    api.list.and.returnValue(of([
      certificateItem(),
      certificateItem({
        id: 2,
        code: 'ACHCOL-VALIDATION',
        displayName: 'Certificado de ACH Colombia',
        fileName: 'ACHcolombia.cer',
        financialInstitutionId: null,
        financialInstitutionName: null,
        clearingHouseId: 1,
        clearingHouseName: 'ACH Colombia',
        purpose: 'ClearingHouseValidation',
        holderType: 'ClearingHouse',
        hasPrivateKey: false,
        canDelete: true
      })
    ]));

    await TestBed.configureTestingModule({
      imports: [NachaCertificateManagerComponent],
      providers: [
        provideNoopAnimations(),
        { provide: CertificateManagementApiService, useValue: api },
        { provide: BlobDownloadService, useValue: jasmine.createSpyObj('BlobDownloadService', ['save', 'fromHttpError']) },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['success', 'error']) },
        {
          provide: ClearingHousesApiService,
          useValue: { list: () => of([{ id: 1, code: 'ACHCOL', name: 'ACH Colombia', isActive: true }]) }
        },
        {
          provide: FinancialInstitutionsApiService,
          useValue: { getAll: () => of([{ id: 7, code: 'CFA', name: 'CFA', isDefaultSource: true }]) }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaCertificateManagerComponent);
    component = fixture.componentInstance;
    dialog = (component as unknown as { dialog: MatDialog }).dialog;
    fixture.detectChanges();
  });

  it('presenta la administración funcional exclusivamente en español', () => {
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Administración de certificados de seguridad');
    expect(text).toContain('Certificado de CFA');
    expect(text).toContain('Certificados de cámaras compensadoras');
    expect(text).toContain('Válido desde');
    expect(text).toContain('Fecha de carga');
    expect(text).not.toContain('Upload');
    expect(text).not.toContain('Clearing House');
    expect(text).not.toContain('Valid');
  });

  it('separa los certificados por propietario y muestra los estados traducidos', () => {
    expect(component.cfaCertificates.length).toBe(1);
    expect(component.clearingHouseCertificates.length).toBe(1);
    expect(component.statusLabel(component.cfaCertificates[0])).toBe('Vigente');
    expect(component.useLabel(component.cfaCertificates[0])).toBe('Firmar y descifrar información de CFA');
    expect(component.useLabel(component.clearingHouseCertificates[0])).toBe('Validar información recibida');
  });

  it('muestra fechas oficiales y la fecha de carga como datos separados sin desplazamiento de día', () => {
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('22/06/2026');
    expect(text).toContain('22/06/2027');
    expect(text).toContain('30/07/2026');
  });

  it('mantiene la información técnica cerrada inicialmente', () => {
    const panels = fixture.nativeElement.querySelectorAll('mat-expansion-panel');
    expect(panels.length).toBeGreaterThan(0);
    expect(fixture.nativeElement.querySelectorAll('mat-expansion-panel.mat-expanded').length).toBe(0);
  });

  it('resuelve la institución de origen y abre la carga guiada para CFA', () => {
    const open = spyOn(dialog, 'open').and.returnValue({ afterClosed: () => of(false) } as never);
    component.openUpload('CfaSigningAndDecryption');
    const data = open.calls.mostRecent().args[1]?.data as {
      defaultInstitution: { id: number };
      initialPurpose: string;
    };
    expect(data.defaultInstitution.id).toBe(7);
    expect(data.initialPurpose).toBe('CfaSigningAndDecryption');
  });

  it('exige motivo para revocar y conserva el contrato funcional', () => {
    api.revoke.and.returnValue(of(certificateItem({ functionalStatus: 'Revoked' })));
    spyOn(dialog, 'open').and.returnValue({
      afterClosed: () => of('Rotación programada')
    } as never);

    component.requestRevoke(component.cfaCertificates[0]);

    expect(api.revoke).toHaveBeenCalledOnceWith(1, 'Rotación programada');
  });
});

function certificateItem(overrides: Partial<CertificateListItem> = {}): CertificateListItem {
  return {
    id: 1,
    code: 'CFA-SIGN-DECRYPT',
    displayName: 'Certificado de CFA',
    fileName: 'CFA.pfx',
    financialInstitutionId: 7,
    financialInstitutionName: 'CFA',
    clearingHouseId: null,
    clearingHouseName: null,
    environment: 'Production',
    purpose: 'CfaSigningAndDecryption',
    holderType: 'Participant',
    status: 'Active',
    functionalStatus: 'Valid',
    daysRemaining: 52,
    versionNumber: 1,
    subject: 'CN=Titular de prueba',
    issuer: 'CN=Emisor de prueba',
    serialNumber: '01',
    thumbprint: '93A5AABBCCDDEEFF001122334455667788A8B100',
    fingerprintSha256: 'AABB',
    notBefore: '2026-06-22T20:27:17Z',
    notAfter: '2027-06-22T20:27:17Z',
    hasPrivateKey: true,
    keyAlgorithm: 'RSA',
    keySize: 2048,
    signatureAlgorithm: 'sha256RSA',
    uploadedAtUtc: '2026-07-30T12:00:00Z',
    uploadedBy: 'pruebas',
    canDelete: false,
    ...overrides
  };
}
