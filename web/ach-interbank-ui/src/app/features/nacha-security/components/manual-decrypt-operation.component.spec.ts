import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { NEVER, Subject, of } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AuthorizeDownloadResponse, NachaSecurityOperationResponse } from '../models/nacha-security-operation.model';
import { NachaSecurityOperationsApiService } from '../services/nacha-security-operations-api.service';
import { ManualDecryptOperationComponent } from './manual-decrypt-operation.component';

describe('ManualDecryptOperationComponent', () => {
  let fixture: ComponentFixture<ManualDecryptOperationComponent>;
  let component: ManualDecryptOperationComponent;
  let service: jasmine.SpyObj<NachaSecurityOperationsApiService>;

  beforeEach(async () => {
    service = jasmine.createSpyObj<NachaSecurityOperationsApiService>('NachaSecurityOperationsApiService', [
      'manualDecrypt', 'authorizeDownload', 'downloadArtifact'
    ]);

    await TestBed.configureTestingModule({
      imports: [ManualDecryptOperationComponent],
      providers: [
        provideNoopAnimations(),
        { provide: NachaSecurityOperationsApiService, useValue: service },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['error', 'success']) },
        { provide: AuthService, useValue: { hasPermission: () => true } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ManualDecryptOperationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('usa la foundation Material y el encabezado compartido', () => {
    expect(fixture.nativeElement.querySelector('app-page-header')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('mat-card')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('button.mat-mdc-button-base')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[appButton]')).toBeNull();
  });

  it('autoriza la descarga antes de solicitar el artefacto', () => {
    const authorization = new Subject<AuthorizeDownloadResponse>();
    service.authorizeDownload.and.returnValue(authorization);
    service.downloadArtifact.and.returnValue(NEVER);
    component.operation = operation();

    component.authorizeAndDownload();

    expect(service.authorizeDownload).toHaveBeenCalledOnceWith('OP-001');
    expect(service.downloadArtifact).not.toHaveBeenCalled();

    authorization.next({ operationId: 'OP-001', authorized: true });

    expect(service.downloadArtifact).toHaveBeenCalledOnceWith('OP-001');
  });

  it('no guarda el archivo ni material sensible en storage durante el descifrado', () => {
    const storageSet = spyOn(Storage.prototype, 'setItem');
    service.manualDecrypt.and.returnValue(of(operation()));

    component.form.controls.file.setValue(new File(['contenido'], 'archivo.ENV'));
    component.submit();

    expect(storageSet).not.toHaveBeenCalled();
  });
});

function operation(): NachaSecurityOperationResponse {
  return {
    operationId: 'OP-001',
    operationType: 'ManualEnvelopeDecrypt',
    status: 'Success',
    requestedBy: 'tester',
    requestedAtUtc: '2026-09-30T12:00:00Z',
    failCloseApplied: true,
    legacyFallbackUsed: false,
    artifact: {
      externalFileName: 'resultado.txt',
      contentType: 'text/plain',
      downloadAvailable: true
    },
    certificateSummary: {}
  };
}
