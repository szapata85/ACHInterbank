import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import {
  SoapIntegrationSettings,
  SoapIntegrationSettingsService
} from '../../../core/services/soap-integration-settings.service';
import { SoapIntegrationSettingsComponent } from './soap-integration-settings.component';

describe('SoapIntegrationSettingsComponent', () => {
  let fixture: ComponentFixture<SoapIntegrationSettingsComponent>;
  let component: SoapIntegrationSettingsComponent;
  let service: jasmine.SpyObj<SoapIntegrationSettingsService>;
  let notifications: jasmine.SpyObj<NotificationService>;

  const settings: SoapIntegrationSettings = {
    wscfaachMappings: [
      {
        methodName: 'Proc_Contrapartidas',
        endpoint: 'http://uat.local/soap/proc-contrapartidas',
        soapAction: 'http://tempuri.org/IWSCFAACH/Proc_Contrapartidas',
        operatingMode: 'DryRun',
        enabled: true,
        inputParameterMappings: [{ inputName: 'transaccion', soapParameterName: 'Transaccion', required: true }]
      },
      {
        methodName: 'Proc_Transacciones',
        endpoint: 'http://uat.local/soap/proc-transacciones',
        soapAction: 'http://tempuri.org/IWSCFAACH/Proc_Transacciones',
        operatingMode: 'DryRun',
        enabled: true,
        inputParameterMappings: [{ inputName: 'lote', soapParameterName: 'Lote', required: true }]
      }
    ],
    wsAxonRespuestaTransaccionesMappings: [
      {
        methodName: 'RegistrarRespuestaTransaccion',
        endpoint: 'http://uat.local/soap/respuestas',
        soapAction: 'http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion',
        operatingMode: 'Disabled',
        enabled: false,
        inputParameterMappings: [{ inputName: 'respuesta', soapParameterName: 'Respuesta', required: true }]
      }
    ]
  };

  beforeEach(async () => {
    service = jasmine.createSpyObj<SoapIntegrationSettingsService>('SoapIntegrationSettingsService', ['refreshFromServer', 'updateSettings']);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'error']);
    service.refreshFromServer.and.returnValue(of(settings));
    service.updateSettings.and.returnValue(of(settings));

    await TestBed.configureTestingModule({
      imports: [SoapIntegrationSettingsComponent],
      providers: [
        { provide: SoapIntegrationSettingsService, useValue: service },
        { provide: NotificationService, useValue: notifications },
        { provide: ActivatedRoute, useValue: {} }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(SoapIntegrationSettingsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('carga configuracion desde backend y muestra los tres servicios obligatorios', () => {
    const text = fixture.nativeElement.textContent as string;

    expect(service.refreshFromServer).toHaveBeenCalledTimes(1);
    expect(text).toContain('Proc_Transacciones');
    expect(text).toContain('Proc_Contrapartidas');
    expect(text).toContain('RegistrarRespuestaTransaccion');
    expect(fixture.nativeElement.querySelectorAll('[data-testid="soap-service-card"]').length).toBe(3);
    expect(component.selectedMethodName).toBe('Proc_Transacciones');
  });

  it('permite seleccionar un servicio y deja visible el endpoint editable', () => {
    const registrar = component.allMethods.find((method) => component.methodNameFor(method.group) === 'RegistrarRespuestaTransaccion');
    expect(registrar).toBeTruthy();

    component.selectMethod(registrar!);
    fixture.detectChanges();

    const endpointInput = fixture.nativeElement.querySelector('[data-testid="soap-endpoint-input"]') as HTMLInputElement;
    expect(endpointInput.value).toBe('http://uat.local/soap/respuestas');
    expect(fixture.nativeElement.textContent).toContain('Respuesta diferencial');
  });

  it('guarda usando getRawValue y conserva la seleccion actual', () => {
    const procTransacciones = component.allMethods.find((method) => component.methodNameFor(method.group) === 'Proc_Transacciones')!;
    component.selectMethod(procTransacciones);
    component.beginEdit();
    procTransacciones.group.get('endpoint')?.setValue('http://uat.local/soap/proc-transacciones-editado');

    component.save();

    expect(service.updateSettings).toHaveBeenCalled();
    const payload = service.updateSettings.calls.mostRecent().args[0];
    expect(payload.wscfaachMappings.find((item) => item.methodName === 'Proc_Transacciones')?.endpoint)
      .toBe('http://uat.local/soap/proc-transacciones-editado');
    expect(component.selectedMethodName).toBe('Proc_Transacciones');
  });

  it('editar Proc_Contrapartidas conserva Proc_Transacciones sin sobrescribirlo', () => {
    const contrapartidas = component.allMethods.find((method) => component.methodNameFor(method.group) === 'Proc_Contrapartidas')!;
    component.selectMethod(contrapartidas);
    component.beginEdit();
    contrapartidas.group.patchValue({
      endpoint: 'http://localhost:7083/WSCFAACH.svc',
      operatingMode: 'Live'
    });

    component.save();

    const payload = service.updateSettings.calls.mostRecent().args[0];
    expect(payload.wscfaachMappings.find((item) => item.methodName === 'Proc_Contrapartidas')?.endpoint)
      .toBe('http://localhost:7083/WSCFAACH.svc');
    expect(payload.wscfaachMappings.find((item) => item.methodName === 'Proc_Transacciones')?.endpoint)
      .toBe('http://uat.local/soap/proc-transacciones');
  });

  it('cancelar una edición restaura endpoint y modo sin persistir', () => {
    const contrapartidas = component.allMethods.find((method) => component.methodNameFor(method.group) === 'Proc_Contrapartidas')!;
    component.selectMethod(contrapartidas);
    component.beginEdit();
    contrapartidas.group.patchValue({ endpoint: 'http://incorrecto.local', operatingMode: 'Live' });

    component.cancelEdit();

    expect(contrapartidas.group.get('endpoint')?.value).toBe('http://uat.local/soap/proc-contrapartidas');
    expect(contrapartidas.group.get('operatingMode')?.value).toBe('DryRun');
    expect(service.updateSettings).not.toHaveBeenCalled();
  });

  it('recargar hidrata la configuración persistida y conserva la selección', () => {
    const persisted: SoapIntegrationSettings = {
      ...settings,
      wscfaachMappings: settings.wscfaachMappings.map((item) => ({
        ...item,
        endpoint: 'http://localhost:7083/WSCFAACH.svc',
        operatingMode: 'Live'
      }))
    };
    service.refreshFromServer.and.returnValue(of(persisted));
    const contrapartidas = component.allMethods.find((method) => component.methodNameFor(method.group) === 'Proc_Contrapartidas')!;
    component.selectMethod(contrapartidas);

    component.reload();

    expect(component.selectedMethodName).toBe('Proc_Contrapartidas');
    expect(component.selectedMethod?.group.get('endpoint')?.value).toBe('http://localhost:7083/WSCFAACH.svc');
    expect(component.selectedMethod?.group.get('operatingMode')?.value).toBe('Live');
  });

  it('no muestra acciones de prueba SOAP ni validacion no soportadas por backend', () => {
    const text = fixture.nativeElement.textContent as string;

    expect(text).not.toContain('Validar localmente');
    expect(text).not.toContain('Ultima validacion');
    expect(text).not.toContain('Prueba de conexion');
  });

  it('muestra error si la carga inicial falla', () => {
    service.refreshFromServer.and.returnValue(throwError(() => new Error('fallo')));

    const failedFixture = TestBed.createComponent(SoapIntegrationSettingsComponent);
    failedFixture.detectChanges();

    expect(notifications.error).toHaveBeenCalledWith('No fue posible cargar la configuracion SOAP.');
  });
});
