import { HttpErrorResponse } from '@angular/common/http';
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialog, MatDialogRef } from '@angular/material/dialog';
import { MatSelectHarness } from '@angular/material/select/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { IntegrationConnectionApi } from '../data-access/integration-connection.api';
import {
  IntegrationConnectionChangePreviewDto,
  IntegrationConnectionMethodDto,
  IntegrationConnectionSettingsDto
} from '../data-access/integration-api.models';
import {
  RealSendConfirmationDialogComponent,
  SoapIntegrationSettingsComponent
} from './soap-integration-settings.component';

describe('SoapIntegrationSettingsComponent', () => {
  let fixture: ComponentFixture<SoapIntegrationSettingsComponent>;
  let component: SoapIntegrationSettingsComponent;
  let api: jasmine.SpyObj<IntegrationConnectionApi>;
  let notifications: jasmine.SpyObj<NotificationService>;
  let auth: jasmine.SpyObj<AuthService>;
  let dialog: jasmine.SpyObj<MatDialog>;
  let loader: HarnessLoader;

  const method = (
    methodName: string,
    endpoint: string,
    operatingMode: IntegrationConnectionMethodDto['operatingMode'],
    enabled: boolean
  ): IntegrationConnectionMethodDto => ({
    methodName,
    endpoint,
    soapAction: `http://tempuri.org/${methodName}`,
    operatingMode,
    enabled,
    sanitizedEndpoint: endpoint,
    sanitizedSoapAction: `http://tempuri.org/${methodName}`,
    configurationState: !enabled || operatingMode === 'Disabled'
      ? 'Disabled'
      : operatingMode === 'Live'
        ? 'RealSendEnabled'
        : 'Simulation',
    capabilities: {
      configurationStateKnown: true,
      canRequestRealSend: true,
      blockingReason: null
    },
    inputParameterMappings: [{ inputName: 'dato', soapParameterName: 'Dato', required: true }]
  });

  const settings: IntegrationConnectionSettingsDto = {
    revision: 'rev-7',
    wscfaachMappings: [
      method('Proc_Contrapartidas', 'http://uat***ocal/***', 'DryRun', true),
      method('Proc_Transacciones', 'http://uat***ocal/***', 'DryRun', true)
    ],
    wsAxonRespuestaTransaccionesMappings: [
      method('RegistrarRespuestaTransaccion', 'http://uat***ocal/***', 'Disabled', false)
    ]
  };

  const realSendPreview: IntegrationConnectionChangePreviewDto = {
    methodName: 'Proc_Transacciones',
    revision: 'rev-7',
    currentConfigurationState: 'Simulation',
    requestedConfigurationState: 'RealSendEnabled',
    sanitizedEndpoint: 'http://loca***7083/***',
    sanitizedSoapAction: 'http://tempuri.org/Proc_Transacciones',
    wouldEnableRealSend: true,
    canApply: true,
    blockingReason: null
  };

  beforeEach(async () => {
    api = jasmine.createSpyObj<IntegrationConnectionApi>('IntegrationConnectionApi', ['getSettings', 'previewChange', 'updateSettings']);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'error']);
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['hasPermission']);
    dialog = jasmine.createSpyObj<MatDialog>('MatDialog', ['open']);

    auth.hasPermission.and.returnValue(true);
    api.getSettings.and.returnValue(of(settings));
    api.previewChange.and.returnValue(of(realSendPreview));
    api.updateSettings.and.returnValue(of(settings));
    dialog.open.and.returnValue({ afterClosed: () => of(true) } as any);

    // MatDialogModule is imported by this standalone component. Provide the spy at
    // its own injector so the real dialog service can never be invoked in Karma.
    TestBed.overrideComponent(SoapIntegrationSettingsComponent, {
      add: { providers: [{ provide: MatDialog, useValue: dialog }] }
    });

    await TestBed.configureTestingModule({
      imports: [SoapIntegrationSettingsComponent, RealSendConfirmationDialogComponent],
      providers: [
        { provide: IntegrationConnectionApi, useValue: api },
        { provide: NotificationService, useValue: notifications },
        { provide: AuthService, useValue: auth },
        { provide: MatDialog, useValue: dialog },
        { provide: MatDialogRef, useValue: { close: jasmine.createSpy('close') } },
        {
          provide: MAT_DIALOG_DATA,
          useValue: {
            serviceName: 'Créditos recibidos en CFA',
            technicalName: 'Proc_Transacciones',
            sanitizedDestination: 'http://loca***7083/***'
          }
        },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({ method: 'Proc_Transacciones' }) } } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(SoapIntegrationSettingsComponent);
    component = fixture.componentInstance;
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
    loader = TestbedHarnessEnvironment.loader(fixture);
  });

  it('presenta la configuración avanzada de conexión con nombres funcionales primero', () => {
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Conexiones · Configuración avanzada');
    expect(text).toContain('Conexión entidad participante');
    expect(text).toContain('Créditos recibidos en CFA');
    expect(text).toContain('Débitos originados en CFA');
    expect(text).toContain('Respuestas de transacciones');
    expect(fixture.nativeElement.querySelectorAll('[data-testid="soap-service-card"]').length).toBe(3);
  });

  it('expone el modo de envío mediante Harness Material sin convertir seguridad backend en lógica frontend', async () => {
    component.beginEdit();
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();

    const select = await loader.getHarness(MatSelectHarness);
    await select.open();
    const options = await select.getOptions();
    expect(await Promise.all(options.map(option => option.getText()))).toEqual([
      'Envío real',
      'Simulación sin envío',
      'Inactivo'
    ]);
    await select.clickOptions({ text: 'Simulación sin envío' });
    expect(await select.getValueText()).toBe('Simulación sin envío');
  });

  it('muestra dirección y acción sanitizadas, modo y estado entregados por backend', () => {
    const text = fixture.nativeElement.textContent as string;
    const endpoint = fixture.nativeElement.querySelector('[data-testid="soap-sanitized-endpoint"]') as HTMLElement;

    expect(text).toContain('Dirección del servicio');
    expect(text).toContain('Acción técnica');
    expect(text).toContain('Modo de envío');
    expect(text).toContain('Estado');
    expect(endpoint.textContent).toContain('http://uat***ocal/***');
    expect(text).toContain('Simulación sin envío');
  });

  it('mantiene el detalle técnico avanzado colapsado por defecto', () => {
    expect(component.isEditingSelected).toBeFalse();
    const panel = fixture.nativeElement.querySelector('.soap-settings__advanced-panel');
    expect(panel).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Contenido avanzado colapsado por defecto');
  });

  it('falla cerrado si backend no entrega capability de estado vigente', () => {
    const incomplete: IntegrationConnectionSettingsDto = {
      ...settings,
      wscfaachMappings: settings.wscfaachMappings.map((item) => item.methodName === 'Proc_Transacciones'
        ? { ...item, capabilities: null, configurationState: 'Unknown' }
        : item)
    };
    api.getSettings.and.returnValue(of(incomplete));

    const incompleteFixture = TestBed.createComponent(SoapIntegrationSettingsComponent);
    incompleteFixture.detectChanges();
    const incompleteComponent = incompleteFixture.componentInstance;

    expect(incompleteComponent.canEditSelected).toBeFalse();
    incompleteComponent.beginEdit();
    expect(api.updateSettings).not.toHaveBeenCalled();
    expect(notifications.error).toHaveBeenCalledWith(
      'No es posible editar porque no se pudo determinar el permiso o el estado vigente.'
    );
  });

  it('no consulta backend ni permite edición cuando no puede determinar permiso', () => {
    auth.hasPermission.and.returnValue(false);
    api.getSettings.calls.reset();

    const deniedFixture = TestBed.createComponent(SoapIntegrationSettingsComponent);
    deniedFixture.detectChanges();

    expect(api.getSettings).not.toHaveBeenCalled();
    expect(deniedFixture.nativeElement.querySelector('[data-testid="soap-forbidden-state"]')).toBeTruthy();
    expect(deniedFixture.nativeElement.querySelector('[data-testid="soap-edit"]')).toBeNull();
  });

  it('permite lectura con CanReadAch sin habilitar administración', () => {
    auth.hasPermission.and.callFake((permissions: string[]) => permissions.includes('CanReadAch'));
    api.getSettings.calls.reset();

    const readOnlyFixture = TestBed.createComponent(SoapIntegrationSettingsComponent);
    readOnlyFixture.detectChanges();
    const readOnly = readOnlyFixture.componentInstance;

    expect(readOnly.canRead).toBeTrue();
    expect(readOnly.canManage).toBeFalse();
    expect(api.getSettings).toHaveBeenCalledTimes(1);
    expect(readOnlyFixture.nativeElement.querySelector('[data-testid="soap-edit"]')).toBeNull();
    readOnly.save();
    expect(api.previewChange).not.toHaveBeenCalled();
    expect(api.updateSettings).not.toHaveBeenCalled();
  });

  it('permite lectura con Integrations.Read sin habilitar administración', () => {
    auth.hasPermission.and.callFake((permissions: string[]) => permissions.includes('Integrations.Read'));
    api.getSettings.calls.reset();

    const readOnlyFixture = TestBed.createComponent(SoapIntegrationSettingsComponent);
    readOnlyFixture.detectChanges();
    const readOnly = readOnlyFixture.componentInstance;

    expect(readOnly.canRead).toBeTrue();
    expect(readOnly.canManage).toBeFalse();
    expect(api.getSettings).toHaveBeenCalledTimes(1);
  });

  it('no trata Integrations.ManageConnections por sí sola como permiso de lectura', () => {
    auth.hasPermission.and.callFake((permissions: string[]) => permissions.includes('Integrations.ManageConnections'));
    api.getSettings.calls.reset();

    const manageOnlyFixture = TestBed.createComponent(SoapIntegrationSettingsComponent);
    manageOnlyFixture.detectChanges();
    const manageOnly = manageOnlyFixture.componentInstance;

    expect(manageOnly.canManage).toBeTrue();
    expect(manageOnly.canRead).toBeFalse();
    expect(api.getSettings).not.toHaveBeenCalled();
    expect(manageOnlyFixture.nativeElement.querySelector('[data-testid="soap-forbidden-state"]')).toBeTruthy();
  });

  it('el diálogo exige confirmación explícita y solo muestra el destino sanitizado', () => {
    const dialogFixture = TestBed.createComponent(RealSendConfirmationDialogComponent);
    dialogFixture.detectChanges();
    const text = dialogFixture.nativeElement.textContent as string;

    expect(text).toContain('Confirmar configuración de envío real');
    expect(text).toContain('Créditos recibidos en CFA');
    expect(text).toContain('http://loca***7083/***');
    expect(text).toContain('Confirmar envío real');
    expect(text).toContain('Cancelar');
  });

  it('previsualiza en backend y exige MatDialog contextual antes de habilitar envío real', () => {
    component.beginEdit();
    component.editorForm.patchValue({
      operatingMode: 'Live',
      enabled: true,
      endpoint: 'http://localhost:7083/WSCFAACH.svc'
    });
    component.editorForm.markAsDirty();

    component.save();

    expect(api.previewChange).toHaveBeenCalledWith(jasmine.objectContaining({
      methodName: 'Proc_Transacciones',
      operatingMode: 'Live',
      enabled: true,
      expectedRevision: 'rev-7'
    }));
    expect(dialog.open).toHaveBeenCalledWith(
      RealSendConfirmationDialogComponent,
      jasmine.objectContaining({
        data: jasmine.objectContaining({
          serviceName: 'Créditos recibidos en CFA',
          technicalName: 'Proc_Transacciones',
          sanitizedDestination: 'http://loca***7083/***'
        })
      })
    );
    expect(api.updateSettings).toHaveBeenCalled();
  });

  it('no abre confirmación ni guarda si el preview del backend bloquea el envío real', () => {
    api.previewChange.and.returnValue(of({
      ...realSendPreview,
      canApply: false,
      blockingReason: 'El destino no está autorizado por el backend.'
    }));
    component.beginEdit();
    component.editorForm.patchValue({ operatingMode: 'Live', enabled: true });
    component.editorForm.markAsDirty();

    component.save();

    expect(api.previewChange).toHaveBeenCalled();
    expect(dialog.open).not.toHaveBeenCalled();
    expect(api.updateSettings).not.toHaveBeenCalled();
    expect(notifications.error).toHaveBeenCalledWith('El destino no está autorizado por el backend.');
  });

  it('usa destino sanitizado del backend y nunca el endpoint propuesto en el diálogo', () => {
    component.beginEdit();
    component.editorForm.patchValue({
      operatingMode: 'Live',
      enabled: true,
      endpoint: 'https://usuario:secreto@host.local/service?token=abc'
    });
    component.editorForm.markAsDirty();

    component.save();

    const config = dialog.open.calls.mostRecent().args[1] as any;
    expect(config.data.sanitizedDestination).toBe('http://loca***7083/***');
    expect(config.data.sanitizedDestination).not.toContain('secreto');
    expect(config.data.sanitizedDestination).not.toContain('token=');
  });

  it('cancelar el diálogo no persiste el cambio a envío real', () => {
    dialog.open.and.returnValue({ afterClosed: () => of(false) } as any);
    component.beginEdit();
    component.editorForm.patchValue({ operatingMode: 'Live', enabled: true });
    component.editorForm.markAsDirty();

    component.save();

    expect(api.previewChange).toHaveBeenCalled();
    expect(api.updateSettings).not.toHaveBeenCalled();
  });

  it('evita doble submit durante preview, confirmación o guardado', () => {
    component.beginEdit();
    component.editorForm.patchValue({ endpoint: 'nuevo-destino' });
    component.editorForm.markAsDirty();

    component.previewing = true;
    component.save();
    component.previewing = false;
    component.saving = true;
    component.save();
    component.saving = false;
    component.confirmationOpen = true;
    component.save();

    expect(api.previewChange).not.toHaveBeenCalled();
    expect(api.updateSettings).not.toHaveBeenCalled();
  });

  it('envía ExpectedRevision y refresca desde backend después de guardar sin éxito optimista', () => {
    const refreshed: IntegrationConnectionSettingsDto = {
      ...settings,
      revision: 'rev-8',
      wscfaachMappings: settings.wscfaachMappings.map((item) => item.methodName === 'Proc_Transacciones'
        ? { ...item, operatingMode: 'Disabled', enabled: false, configurationState: 'Disabled' }
        : item)
    };
    // The initial getSettings() ran in beforeEach. The next call after PUT
    // must return the authoritative refreshed state, not the initial snapshot.
    api.getSettings.and.returnValue(of(refreshed));
    api.updateSettings.and.returnValue(of({ ...settings, revision: 'respuesta-put-no-usada' }));

    component.beginEdit();
    component.editorForm.patchValue({ operatingMode: 'Disabled', enabled: false });
    component.editorForm.markAsDirty();
    component.save();

    const payload = api.updateSettings.calls.mostRecent().args[0];
    expect(payload.expectedRevision).toBe('rev-7');
    expect(api.getSettings).toHaveBeenCalledTimes(2);
    expect(component.selectedMethod?.group.get('configurationState')?.value).toBe('Disabled');
    expect(notifications.success).toHaveBeenCalledWith('Configuración técnica guardada y actualizada con el estado vigente.');
  });

  it('humaniza conflicto de Revision y exige recarga antes de otro intento', () => {
    api.updateSettings.and.returnValue(throwError(() => new HttpErrorResponse({ status: 409 })));
    component.beginEdit();
    component.editorForm.patchValue({ operatingMode: 'Disabled', enabled: false });
    component.editorForm.markAsDirty();

    component.save();

    expect(component.requiresReload).toBeTrue();
    expect(component.canSave).toBeFalse();
    expect(notifications.error).toHaveBeenCalledWith(
      'La configuración cambió en el sistema. Recarga antes de intentar guardar nuevamente.'
    );
  });

  it('no implementa seguridad de endpoint en Angular: una cadena no vacía se delega al preview backend', () => {
    component.beginEdit();
    component.editorForm.patchValue({
      endpoint: 'valor-que-backend-debe-evaluar',
      operatingMode: 'Live',
      enabled: true
    });
    component.editorForm.markAsDirty();

    expect(component.editorForm.controls.endpoint.hasError('pattern')).toBeFalse();
    component.save();
    expect(api.previewChange).toHaveBeenCalled();
  });

  it('configura foco seguro, Escape y restauración de foco en MatDialog', () => {
    component.beginEdit();
    component.editorForm.patchValue({ operatingMode: 'Live', enabled: true });
    component.editorForm.markAsDirty();

    component.save();

    const config = dialog.open.calls.mostRecent().args[1] as any;
    expect(config.autoFocus).toBe('first-tabbable');
    expect(config.restoreFocus).toBeTrue();
    expect(config.disableClose).toBeFalse();
    expect(config.maxHeight).toBe('calc(100dvh - 32px)');
    expect(config.role).toBe('alertdialog');
    expect(config.ariaLabel).toContain('Créditos recibidos en CFA');
  });

  it('mantiene estado loading hasta que backend responde', () => {
    const pending = new Subject<IntegrationConnectionSettingsDto>();
    api.getSettings.and.returnValue(pending.asObservable());

    const pendingFixture = TestBed.createComponent(SoapIntegrationSettingsComponent);
    pendingFixture.detectChanges();

    expect(pendingFixture.componentInstance.loading).toBeTrue();
    expect(pendingFixture.nativeElement.querySelector('mat-progress-bar')).toBeTruthy();

    pending.next(settings);
    pending.complete();
    pendingFixture.detectChanges();
    expect(pendingFixture.componentInstance.loading).toBeFalse();
  });

  it('muestra error recuperable cuando falla la carga', () => {
    api.getSettings.and.returnValue(throwError(() => new HttpErrorResponse({ status: 500 })));

    const failedFixture = TestBed.createComponent(SoapIntegrationSettingsComponent);
    failedFixture.detectChanges();

    expect(failedFixture.nativeElement.querySelector('[data-testid="soap-error-state"]')).toBeTruthy();
    expect(notifications.error).toHaveBeenCalledWith('No fue posible cargar la conexión de la entidad participante.');
  });

  it('muestra estados de error y forbidden de backend', () => {
    api.getSettings.and.returnValue(throwError(() => new HttpErrorResponse({ status: 403 })));

    const deniedByBackend = TestBed.createComponent(SoapIntegrationSettingsComponent);
    deniedByBackend.detectChanges();

    expect(deniedByBackend.nativeElement.querySelector('[data-testid="soap-forbidden-state"]')).toBeTruthy();
  });
  it('usa tabs/cards responsive sin tabla primaria y expone alertas semánticas', () => {
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('table')).toBeNull();

    component.backendStateIncomplete = true;
    (component as any).cdr.markForCheck();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="soap-backend-state-incomplete"][role="alert"]')).not.toBeNull();
  });

});
