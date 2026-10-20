import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { MatSelectHarness } from '@angular/material/select/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { ManagedMftAdministration } from '../../core/models/ach-colombia-file-exchange.model';
import { AchColombiaOperationalScheduleAdministrationRow } from '../../core/models/ach-colombia-operational-schedule.model';
import { AchColombiaFileExchangeService } from '../../core/services/ach-colombia-file-exchange.service';
import { AchColombiaOperationalSchedulesService } from '../../core/services/ach-colombia-operational-schedules.service';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { AchColombiaMftAdministrationComponent } from './ach-colombia-mft-administration.component';

describe('AchColombiaMftAdministrationComponent', () => {
  let fixture: ComponentFixture<AchColombiaMftAdministrationComponent>;
  let component: AchColombiaMftAdministrationComponent;
  let api: jasmine.SpyObj<AchColombiaFileExchangeService>;
  let schedulesApi: jasmine.SpyObj<AchColombiaOperationalSchedulesService>;
  let auth: jasmine.SpyObj<AuthService>;
  let notifications: jasmine.SpyObj<NotificationService>;

  const model: ManagedMftAdministration = {
    profileName: 'MFT', provider: 'ManagedFolder', protocol: 'ManagedFile', profileEnabled: true,
    endpoint: 'sftp.cfa.test', port: 22, principal: 'svc-ach',
    automaticOutboundEnabled: false, automaticInboundEnabled: true,
    manualOutboundAllowed: true, manualInboundAllowed: true,
    maximumRetries: 3, retryDelaySeconds: 60, retentionDays: 90,
    outboundLocation: '/out', inboundLocation: '/in', archiveLocation: '/archive',
    credentialConfigured: true, credentialType: 'Password', credentialUpdatedAtUtc: '2026-10-09T00:00:00Z',
    concurrencyToken: 'token-1', sftpHostKeyFingerprint: 'SHA256:abc', sftpTimeoutSeconds: 30
  };

  const rows: AchColombiaOperationalScheduleAdministrationRow[] = [
    row(1, 'Ciclo 1', '19:01:00', '07:00:00', '06:30:00', null),
    row(2, 'Ciclo 2', '07:01:00', '09:00:00', null, null),
    row(7, 'Ciclo 7', '16:01:00', '18:00:00', null, '17:50:00')
  ];

  beforeEach(async () => {
    api = jasmine.createSpyObj('AchColombiaFileExchangeService', ['administration', 'updateAdministration', 'setCredential', 'testConnection']);
    api.administration.and.returnValue(of({ ...model }));
    api.updateAdministration.and.callFake(value => of({ ...value, concurrencyToken: 'token-2' }));
    api.setCredential.and.returnValue(of({ ...model, concurrencyToken: 'token-credential' }));
    api.testConnection.and.returnValue(of({ succeeded: true, code: 'ACHCOL_MFT_CONNECTION_OK', message: 'Conexión exitosa.' }));

    schedulesApi = jasmine.createSpyObj('AchColombiaOperationalSchedulesService', ['administration', 'create', 'update']);
    schedulesApi.administration.and.returnValue(of(rows));
    schedulesApi.create.and.returnValue(of({} as never));
    schedulesApi.update.and.returnValue(of({} as never));

    auth = jasmine.createSpyObj('AuthService', ['hasPermission']);
    auth.hasPermission.and.returnValue(true);
    notifications = jasmine.createSpyObj('NotificationService', ['success', 'error', 'info', 'warning']);

    await TestBed.configureTestingModule({
      imports: [AchColombiaMftAdministrationComponent],
      providers: [
        provideNoopAnimations(),
        { provide: AchColombiaFileExchangeService, useValue: api },
        { provide: AchColombiaOperationalSchedulesService, useValue: schedulesApi },
        { provide: AuthService, useValue: auth },
        { provide: NotificationService, useValue: notifications }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AchColombiaMftAdministrationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads the simple administration screen and never populates the password', () => {
    expect(api.administration).toHaveBeenCalled();
    expect(schedulesApi.administration).toHaveBeenCalled();
    expect(component.secret).toBe('');
    expect(fixture.nativeElement.textContent).toContain('Datos del intercambio con ACH Colombia');
    expect(fixture.nativeElement.textContent).toContain('Configuración de conexión');
    expect(fixture.nativeElement.textContent).toContain('Horarios automáticos');
    expect(fixture.nativeElement.querySelector('input[type=password]')).toBeNull();
  });

  it('shows only SFTP-specific fields when SFTP is selected', async () => {
    const connectionType = await TestbedHarnessEnvironment.loader(fixture)
      .getHarness(MatSelectHarness.with({ selector: 'mat-select[aria-label="Tipo de conexión"]' }));
    await connectionType.open();
    await connectionType.clickOptions({ text: 'SFTP' });
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Huella del servidor');
    expect(text).toContain('Credencial configurada');
    const password = fixture.nativeElement.querySelector('input[type=password]') as HTMLInputElement;
    expect(password).not.toBeNull();
    expect(password.type).toBe('password');
    expect(password.value).toBe('');

    await connectionType.open();
    await connectionType.clickOptions({ text: 'Carpeta compartida' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Huella del servidor');
  });

  it('preserves the existing credential when saving without a replacement', () => {
    component.connectionType = 'SftpEndpoint';
    component.secret = '';
    component.saveConnection();

    expect(api.setCredential).not.toHaveBeenCalled();
    expect(api.updateAdministration).toHaveBeenCalled();
  });

  it('rotates a credential using the concurrency token without returning it to the screen', () => {
    component.connectionType = 'SftpEndpoint';
    component.secret = 'new-secret';
    component.saveConnection();

    expect(api.setCredential).toHaveBeenCalledWith('Password', 'new-secret', 'token-1');
    expect(api.updateAdministration).toHaveBeenCalledWith(jasmine.objectContaining({ concurrencyToken: 'token-credential' }));
    expect(component.secret).toBe('');
  });

  it('tests the saved connection and shows a friendly result', () => {
    component.testConnection();
    fixture.detectChanges();

    expect(api.testConnection).toHaveBeenCalled();
    expect(notifications.success).toHaveBeenCalledWith('Conexión exitosa.');
    expect(fixture.nativeElement.textContent).toContain('Conexión exitosa.');
  });

  it('maps connection failures to friendly messages without technical details', () => {
    api.testConnection.and.returnValue(of({ succeeded: false, code: 'ACHCOL_MFT_SFTP_HOST_KEY_REJECTED', message: 'internal' }));
    component.testConnection();

    fixture.detectChanges();
    expect(notifications.error).toHaveBeenCalledWith('Huella del servidor no válida.');
    expect(fixture.nativeElement.textContent).toContain('Huella del servidor no válida.');
    expect(fixture.nativeElement.textContent).not.toContain('internal');
  });

  it('shows a clear reload message on edit conflict', () => {
    api.updateAdministration.and.returnValue(throwError(() => new HttpErrorResponse({ status: 409 })));
    component.saveConnection();

    expect(component.error).toContain('modificada por otro usuario');
    expect(notifications.error).toHaveBeenCalledWith(jasmine.stringMatching(/Recargue/));
  });

  it('renders C1 06:30, C7 17:50 and leaves C2 without invented time', () => {
    const c1 = component.schedules.find(x => x.cycleName === 'Ciclo 1')!;
    const c2 = component.schedules.find(x => x.cycleName === 'Ciclo 2')!;
    const c7 = component.schedules.find(x => x.cycleName === 'Ciclo 7')!;

    expect(c1.automaticTime).toBe('06:30');
    expect(c2.automaticTime).toBe('');
    expect(c2.latestTime).toBe('');
    expect(c7.latestTime).toBe('17:50');
    expect(component.windowLabel(c1)).toBe('19:01 – 07:00');
    expect(component.windowLabel(c7)).toBe('16:01 – 18:00');
  });

  it('keeps the regulatory window read-only', () => {
    const table = fixture.nativeElement.querySelector('[data-testid=automatic-schedules]');
    expect(table.textContent).toContain('Ventana ACH Colombia');
    expect(table.querySelectorAll('input[type=time]').length).toBe(rows.length * 2);
    expect(table.querySelector('input[aria-label*="Ventana"]')).toBeNull();
  });

  it('uses Config.Manage or CanManageAch for every mutable action and fails closed otherwise', async () => {
    expect(auth.hasPermission).toHaveBeenCalledWith(['Config.Manage', 'CanManageAch']);

    auth.hasPermission.and.returnValue(false);
    const readonlyFixture = TestBed.createComponent(AchColombiaMftAdministrationComponent);
    readonlyFixture.detectChanges();
    await readonlyFixture.whenStable();

    expect(readonlyFixture.nativeElement.textContent).not.toContain('Guardar conexión');
    expect(readonlyFixture.nativeElement.textContent).not.toContain('Probar conexión');
    expect(readonlyFixture.nativeElement.textContent).not.toContain('Guardar horarios');

    const readonlyComponent = readonlyFixture.componentInstance;
    const updateCalls = api.updateAdministration.calls.count();
    const credentialCalls = api.setCredential.calls.count();
    const testCalls = api.testConnection.calls.count();
    const scheduleCreateCalls = schedulesApi.create.calls.count();
    const scheduleUpdateCalls = schedulesApi.update.calls.count();

    readonlyComponent.saveConnection();
    readonlyComponent.testConnection();
    readonlyComponent.saveSchedules();

    expect(api.updateAdministration.calls.count()).toBe(updateCalls);
    expect(api.setCredential.calls.count()).toBe(credentialCalls);
    expect(api.testConnection.calls.count()).toBe(testCalls);
    expect(schedulesApi.create.calls.count()).toBe(scheduleCreateCalls);
    expect(schedulesApi.update.calls.count()).toBe(scheduleUpdateCalls);
  });

  function row(
    cycle: number,
    cycleName: string,
    regulatoryStartTime: string,
    regulatoryCutoffTime: string,
    automaticOutboundStartTime: string | null,
    automaticOutboundLatestStartTime: string | null
  ): AchColombiaOperationalScheduleAdministrationRow {
    return {
      clearingHouseId: 31,
      clearingHouseCycleConfigId: 100 + cycle,
      policyVersion: 'TEST',
      cycleName,
      regulatoryStartTime,
      regulatoryCutoffTime,
      regulatoryCycleActive: true,
      scheduleId: cycle === 2 ? null : 200 + cycle,
      automaticOutboundEnabled: false,
      automaticOutboundStartTime,
      automaticOutboundLatestStartTime,
      outboundExecutionStrategy: 2,
      effectiveTimeZoneId: 'America/Bogota',
      isActive: false,
      rowVersion: cycle === 2 ? null : `row-${cycle}`
    };
  }
});
