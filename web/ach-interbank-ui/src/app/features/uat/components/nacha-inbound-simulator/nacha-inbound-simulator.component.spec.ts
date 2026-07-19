import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { NachaInboundSimulatorComponent } from './nacha-inbound-simulator.component';
import { NachaInboundSimulatorService } from '../../services/nacha-inbound-simulator.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { FinancialInstitutionsApiService } from '../../../transactions/services/financial-institutions-api.service';
import { FinancialInstitutionStatusEnum } from '../../../transactions/transactions.types';

describe('NachaInboundSimulatorComponent', () => {
  let fixture: ComponentFixture<NachaInboundSimulatorComponent>;
  let component: NachaInboundSimulatorComponent;
  let service: jasmine.SpyObj<NachaInboundSimulatorService>;
  let financialInstitutionsApi: jasmine.SpyObj<FinancialInstitutionsApiService>;

  beforeEach(async () => {
    service = jasmine.createSpyObj<NachaInboundSimulatorService>('NachaInboundSimulatorService', ['list', 'preview', 'generate', 'eligibleDifferentialTransactions', 'downloadUrl']);
    financialInstitutionsApi = jasmine.createSpyObj<FinancialInstitutionsApiService>('FinancialInstitutionsApiService', ['getAll']);
    service.list.and.returnValue(of([]));
    service.preview.and.returnValue(of({
      eligible: true,
      decision: 'ELIGIBLE',
      message: 'Elegible',
      simulationMode: 'IncomingTransactions'
    }));
    service.eligibleDifferentialTransactions.and.returnValue(of({ items: [], page: 1, pageSize: 10, total: 0 }));
    service.generate.and.returnValue(of({
      id: 1,
      simulationId: 'sim',
      fileName: '9999990.001.20260520.1.OUT',
      downloadUrl: '/api/uat/nacha-inbound-simulator/1/file',
      evidenceUrl: '/api/uat/nacha-inbound-simulator/1/evidence',
      sha256: 'A'.repeat(64),
      fileSizeBytes: 1060,
      generatedOnly: true,
      autoImported: false,
      uploadRequired: true,
      externalTransmission: false,
      message: 'Debe cargarse manualmente por NachaUpload.'
    }));
    service.downloadUrl.and.returnValue('/download');
    financialInstitutionsApi.getAll.and.returnValue(of([
      { id: 1, name: 'Cooperativa Financiera de Antioquia', routingNumber: '00001', transitCode: '283', checkDigit: '0', isDefaultSource: true, status: FinancialInstitutionStatusEnum.Active },
      { id: 2, name: 'Banco UAT Externo ACH', routingNumber: '99999', transitCode: '900', checkDigit: '0', isDefaultSource: false, status: FinancialInstitutionStatusEnum.Active }
    ]));

    await TestBed.configureTestingModule({
      imports: [NachaInboundSimulatorComponent],
      providers: [
        { provide: NachaInboundSimulatorService, useValue: service },
        { provide: FinancialInstitutionsApiService, useValue: financialInstitutionsApi },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['success', 'error']) }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaInboundSimulatorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('carga la pantalla y lista simulaciones', () => {
    expect(component).toBeTruthy();
    expect(service.list).toHaveBeenCalled();
    expect(financialInstitutionsApi.getAll).toHaveBeenCalled();
    expect(component.originFinancialInstitutions.length).toBe(1);
    expect(component.defaultDestination?.isDefaultSource).toBeTrue();
  });

  it('valida camara y escenario requeridos', () => {
    component.form.controls.clearingHouseCode.setValue('');
    component.generate();
    expect(service.generate).not.toHaveBeenCalled();
  });

  it('requiere causal para rechazo o devolucion', () => {
    component.changeMode('DifferentialResponses');
    component.form.controls.scenarioType.setValue('IncomingCreditReturn');
    component.form.controls.reasonCode.setValue('');
    component.generate();
    expect(service.generate).not.toHaveBeenCalled();
  });

  it('separa modos y limpia campos incompatibles al cambiar', () => {
    component.form.controls.reasonCode.setValue('R01');
    component.form.markAsPristine();

    component.changeMode('DifferentialResponses');

    expect(component.isDifferentialMode()).toBeTrue();
    expect(component.form.controls.scenarioType.value).toBe('IncomingCreditConfirmation');
    expect(component.form.controls.reasonCode.value).toBe('');
    expect(service.eligibleDifferentialTransactions).toHaveBeenCalled();
  });

  it('llama endpoint generate y muestra resultado descargable', () => {
    component.form.controls.originFinancialInstitutionId.setValue(2);
    component.generate();
    expect(service.generate).toHaveBeenCalled();
    const payload = service.generate.calls.mostRecent().args[0];
    expect(payload.originFinancialInstitutionId).toBe(2);
    expect('destinationFinancialInstitutionCode' in payload).toBeFalse();
    expect(component.result?.generatedOnly).toBeTrue();
    expect(component.result?.autoImported).toBeFalse();
    expect(component.result?.uploadRequired).toBeTrue();
  });

  it('muestra error 422 controlado', () => {
    service.generate.and.returnValue(throwError(() => new HttpErrorResponse({ status: 422, error: { detail: 'Regla no configurada' } })));
    component.form.controls.originFinancialInstitutionId.setValue(2);
    component.generate();
    expect(service.generate).toHaveBeenCalled();
  });
});
