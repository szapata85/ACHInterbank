import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { AchColombiaFileExchangeService, ManagedMftAdministration } from '../ach-colombia-file-exchange/ach-colombia-file-exchange.service';
import { AchColombiaMftAdministrationComponent } from './ach-colombia-mft-administration.component';

describe('AchColombiaMftAdministrationComponent', () => {
  let fixture: ComponentFixture<AchColombiaMftAdministrationComponent>;
  let component: AchColombiaMftAdministrationComponent;
  let api: jasmine.SpyObj<AchColombiaFileExchangeService>;
  let auth: jasmine.SpyObj<AuthService>;
  const model: ManagedMftAdministration = { profileName: 'MFT', provider: 'ManagedFolder', protocol: 'ManagedFile', profileEnabled: true, automaticOutboundEnabled: false, automaticInboundEnabled: false, manualOutboundAllowed: true, manualInboundAllowed: true, maximumRetries: 3, retryDelaySeconds: 60, retentionDays: 90, outboundLocation: 'out', inboundLocation: 'in', archiveLocation: 'archive', credentialConfigured: true, credentialType: 'Password', concurrencyToken: 'token' };

  beforeEach(async () => {
    api = jasmine.createSpyObj('AchColombiaFileExchangeService', ['administration', 'updateAdministration', 'setCredential']);
    api.administration.and.returnValue(of(model)); api.updateAdministration.and.returnValue(of(model)); api.setCredential.and.returnValue(of(model));
    auth = jasmine.createSpyObj('AuthService', ['hasPermission']); auth.hasPermission.and.returnValue(true);
    await TestBed.configureTestingModule({ imports: [AchColombiaMftAdministrationComponent], providers: [
      { provide: AchColombiaFileExchangeService, useValue: api }, { provide: AuthService, useValue: auth },
      { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['success', 'error']) }
    ] }).compileComponents();
    fixture = TestBed.createComponent(AchColombiaMftAdministrationComponent); component = fixture.componentInstance; fixture.detectChanges();
  });

  it('loads safe credential metadata without populating the secret', () => {
    expect(api.administration).toHaveBeenCalled();
    expect(component.secret).toBe('');
    expect(fixture.nativeElement.querySelector('input[type=password]').value).toBe('');
  });

  it('clears the secret after successful rotation', () => {
    component.secret = 'new-secret'; component.rotate();
    expect(api.setCredential).toHaveBeenCalledWith('Password', 'new-secret');
    expect(component.secret).toBe('');
  });

  it('does not render management controls without CanManageAch', async () => {
    auth.hasPermission.and.returnValue(false);
    const readonlyFixture = TestBed.createComponent(AchColombiaMftAdministrationComponent);
    readonlyFixture.detectChanges();
    expect(readonlyFixture.nativeElement.textContent).not.toContain('Guardar configuración');
    expect(readonlyFixture.nativeElement.textContent).not.toContain('Rotar credencial');
  });
});
