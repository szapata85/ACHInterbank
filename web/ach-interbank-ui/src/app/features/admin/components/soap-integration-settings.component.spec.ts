import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import {
  SoapIntegrationSettings,
  SoapIntegrationSettingsService
} from '../../../core/services/soap-integration-settings.service';
import { SoapIntegrationSettingsComponent } from './soap-integration-settings.component';

describe('SoapIntegrationSettingsComponent', () => {
  let fixture: ComponentFixture<SoapIntegrationSettingsComponent>;
  let component: SoapIntegrationSettingsComponent;
  let settings$: BehaviorSubject<SoapIntegrationSettings>;
  let service: jasmine.SpyObj<SoapIntegrationSettingsService>;

  const settings: SoapIntegrationSettings = {
    wscfaachMappings: [
      {
        methodName: 'Proc_Contrapartidas',
        endpoint: 'http://uat.local/servicios/soap/proc-contrapartidas/endpoint-largo-para-validar-truncado',
        soapAction: 'http://tempuri.org/IWSCFAACH/Proc_Contrapartidas',
        enabled: true,
        inputParameterMappings: [{ inputName: 'transaccion', soapParameterName: 'Transaccion', required: true }]
      }
    ],
    wsAxonRespuestaTransaccionesMappings: [
      {
        methodName: 'RegistrarRespuestaTransaccion',
        endpoint: 'http://uat.local/servicios/soap/respuestas',
        soapAction: 'http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion',
        enabled: false,
        inputParameterMappings: [{ inputName: 'respuesta', soapParameterName: 'Respuesta', required: true }]
      }
    ]
  };

  beforeEach(async () => {
    settings$ = new BehaviorSubject<SoapIntegrationSettings>(settings);
    service = jasmine.createSpyObj<SoapIntegrationSettingsService>('SoapIntegrationSettingsService', ['refreshFromServer', 'updateSettings'], {
      settings$: settings$.asObservable()
    });
    service.refreshFromServer.and.returnValue(of(settings));
    service.updateSettings.and.returnValue(of(settings));

    await TestBed.configureTestingModule({
      imports: [SoapIntegrationSettingsComponent],
      providers: [
        { provide: SoapIntegrationSettingsService, useValue: service },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['success', 'error']) },
        { provide: ActivatedRoute, useValue: {} }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(SoapIntegrationSettingsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renderiza lista compacta y no formulario gigante inicial', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Servicios configurados');
    expect(text).toContain('Proc_Contrapartidas');
    expect(fixture.nativeElement.querySelector('.desktop-table')).toBeFalsy();
    expect(fixture.nativeElement.querySelectorAll('[data-testid="soap-service-card"]').length).toBe(2);
    expect(fixture.nativeElement.querySelector('[data-testid="soap-service-detail-button"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="soap-service-edit-button"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="soap-service-test-button"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.modal-panel')).toBeFalsy();
    expect(fixture.nativeElement.querySelector('.edit-form')).toBeFalsy();
  });

  it('abre detalle read-only con endpoint completo', () => {
    component.openDetail(component.allMethods[0]);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Detalle tecnico');
    expect(text).toContain(settings.wscfaachMappings[0].endpoint);
    expect(text).toContain('Secretos y certificados privados');
  });

  it('abre modal de edicion y guardar llama el servicio esperado', () => {
    component.openEdit(component.allMethods[0]);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.edit-form')).toBeTruthy();
    component.save();

    expect(service.updateSettings).toHaveBeenCalled();
  });

  it('abre modal de prueba y ejecuta validacion local sin cambiar modo a Live', () => {
    component.openTest(component.allMethods[0]);
    fixture.detectChanges();

    component.runConnectionTest();
    fixture.detectChanges();

    expect(component.lastTestResult?.status).toBe('OK');
    expect(component.modeFor(component.allMethods[0].group)).toBe('DryRun/UAT-local');
    expect(fixture.nativeElement.textContent).toContain('Validacion local correcta');
  });

  it('cancela modal sin guardar', () => {
    component.openEdit(component.allMethods[0]);
    component.closeModal();
    fixture.detectChanges();

    expect(component.modalMode).toBeNull();
    expect(service.updateSettings).not.toHaveBeenCalled();
  });
});
