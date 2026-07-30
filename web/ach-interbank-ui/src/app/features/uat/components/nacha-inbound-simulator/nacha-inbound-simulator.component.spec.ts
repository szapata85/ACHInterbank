import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { MatDialog } from '@angular/material/dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Subject, of, throwError } from 'rxjs';
import { NachaInboundSimulatorComponent } from './nacha-inbound-simulator.component';
import {
  GenerateNachaInboundSimulationRequest,
  NachaInboundSimulationResult,
  NachaInboundSimulatorService
} from '../../services/nacha-inbound-simulator.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { FinancialInstitutionsApiService } from '../../../transactions/services/financial-institutions-api.service';
import { FinancialInstitutionStatusEnum } from '../../../transactions/transactions.types';
import { ClearingHousesApiService } from '../../../ach-cycles/services/ach-cycles-api.service';
import { ClearingHousesService } from '../../../clearing-houses/clearing-houses.service';
import { NachaConfigApiService } from '../../../nacha-config-admin/services/nacha-config-api.service';
import { NachaConfigProfileReadModel } from '../../../nacha-config-admin/models/nacha-config-admin.models';

describe('NachaInboundSimulatorComponent', () => {
  let fixture: ComponentFixture<NachaInboundSimulatorComponent>;
  let component: NachaInboundSimulatorComponent;
  let service: jasmine.SpyObj<NachaInboundSimulatorService>;
  let financialInstitutionsApi: jasmine.SpyObj<FinancialInstitutionsApiService>;
  let clearingHousesApi: jasmine.SpyObj<ClearingHousesApiService>;
  let clearingHousesService: jasmine.SpyObj<ClearingHousesService>;
  let nachaConfigApi: jasmine.SpyObj<NachaConfigApiService>;
  let notifications: jasmine.SpyObj<NotificationService>;
  let dialog: jasmine.SpyObj<MatDialog>;

  const officialProfile: NachaConfigProfileReadModel = {
    profileId: 11,
    profileCode: 'OFFICIAL-ACH-IN',
    profileName: 'Perfil oficial ACH entrada',
    clearingHouseCode: 'ACH',
    flowType: 'ORIGINAL',
    status: 'PUBLICADO',
    version: 'v1.0',
    isPublished: true,
    isCurrent: true,
    effectiveFrom: '2026-01-01',
    layoutVariantCount: 7,
    fieldCount: 40,
    recordTypes: ['1', '5', '6', '7', '8', '9'],
    isOfficialModel: true,
    legacyDeprecated: true
  };
  const result: NachaInboundSimulationResult = {
    id: 91,
    simulationId: 'SIM-UAT-91',
    fileName: '9999990.001.20260520.1.OUT',
    downloadUrl: '/api/uat/nacha-inbound-simulator/91/file',
    evidenceUrl: '/api/uat/nacha-inbound-simulator/91/evidence',
    sha256: 'A'.repeat(64),
    fileSizeBytes: 1060,
    generatedOnly: true,
    autoImported: false,
    uploadRequired: true,
    externalTransmission: false,
    message: 'Archivo UAT generado.'
  };

  beforeEach(async () => {
    service = jasmine.createSpyObj<NachaInboundSimulatorService>(
      'NachaInboundSimulatorService',
      ['list', 'preview', 'generate', 'eligibleDifferentialTransactions', 'availableCycles', 'downloadUrl']
    );
    financialInstitutionsApi = jasmine.createSpyObj<FinancialInstitutionsApiService>(
      'FinancialInstitutionsApiService',
      ['getAll']
    );
    clearingHousesApi = jasmine.createSpyObj<ClearingHousesApiService>('ClearingHousesApiService', ['list']);
    clearingHousesService = jasmine.createSpyObj<ClearingHousesService>('ClearingHousesService', ['profiles']);
    nachaConfigApi = jasmine.createSpyObj<NachaConfigApiService>('NachaConfigApiService', ['listarPerfilesReadOnly']);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'warning', 'error']);
    dialog = jasmine.createSpyObj<MatDialog>('MatDialog', ['open']);

    service.list.and.returnValue(of([]));
    service.preview.and.returnValue(of({
      eligible: true,
      decision: 'ELIGIBLE',
      message: 'Elegible',
      simulationMode: 'IncomingTransactions'
    }));
    service.generate.and.returnValue(of(result));
    service.eligibleDifferentialTransactions.and.returnValue(of({
      items: [],
      page: 1,
      pageSize: 10,
      total: 0
    }));
    service.availableCycles.and.returnValue(of([{
      cycleId: 'ACH-20260520-C1',
      cycleCode: 'ACH-20260520-C1',
      cycleName: 'Ciclo 1',
      clearingHouseId: 1,
      clearingHouseCode: 'ACHCOL',
      clearingHouseName: 'ACH Colombia',
      processingDate: '2026-05-20',
      transactionCount: 3,
      status: 'Disponible'
    }]));
    service.downloadUrl.and.returnValue('/download');

    financialInstitutionsApi.getAll.and.returnValue(of([
      {
        id: 1,
        name: 'Institución receptora',
        routingNumber: '00001',
        transitCode: '283',
        checkDigit: '0',
        isDefaultSource: true,
        status: FinancialInstitutionStatusEnum.Active
      },
      {
        id: 2,
        name: 'Banco UAT Externo',
        routingNumber: '99999',
        transitCode: '900',
        checkDigit: '0',
        isDefaultSource: false,
        status: FinancialInstitutionStatusEnum.Active
      },
      {
        id: 3,
        name: 'Banco inactivo',
        routingNumber: '88888',
        transitCode: '800',
        checkDigit: '0',
        isDefaultSource: false,
        status: FinancialInstitutionStatusEnum.Inactive
      }
    ]));
    clearingHousesApi.list.and.returnValue(of([{
      id: 1,
      code: 'ACHCOL',
      name: 'ACH Colombia',
      requiresNachaProfile: true
    }]));
    clearingHousesService.profiles.and.returnValue(of([
      { id: 10, code: 'LEGACY', name: 'Perfil legado', isPublished: true, isCurrent: true },
      { id: 11, code: 'OFFICIAL', name: 'Perfil oficial', isPublished: true, isCurrent: true }
    ]));
    nachaConfigApi.listarPerfilesReadOnly.and.returnValue(of([
      {
        ...officialProfile,
        profileId: 10,
        profileCode: 'LEGACY',
        profileName: 'Perfil legado',
        isOfficialModel: false,
        legacyDeprecated: true
      },
      officialProfile
    ]));
    dialog.open.and.returnValue({
      afterClosed: () => of(true)
    } as unknown as ReturnType<MatDialog['open']>);

    await TestBed.configureTestingModule({
      imports: [NachaInboundSimulatorComponent, NoopAnimationsModule],
      providers: [
        { provide: NachaInboundSimulatorService, useValue: service },
        { provide: FinancialInstitutionsApiService, useValue: financialInstitutionsApi },
        { provide: ClearingHousesApiService, useValue: clearingHousesApi },
        { provide: ClearingHousesService, useValue: clearingHousesService },
        { provide: NachaConfigApiService, useValue: nachaConfigApi },
        { provide: NotificationService, useValue: notifications },
        { provide: MatDialog, useValue: dialog }
      ]
    })
      .overrideProvider(MatDialog, { useValue: dialog })
      .compileComponents();

    createComponent();
  });

  afterEach(() => fixture.destroy());

  function createComponent(): void {
    fixture = TestBed.createComponent(NachaInboundSimulatorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  function makeFormValid(): void {
    component.form.patchValue({
      clearingHouseCode: 'ACHCOL',
      originFinancialInstitutionId: 2,
      cycleCode: 'ACH-20260520-C1',
      businessDate: new Date(2026, 4, 20)
    });
  }

  it('inicializa catálogos, historial y ciclos dinámicos', () => {
    expect(component).toBeTruthy();
    expect(service.list).toHaveBeenCalled();
    expect(clearingHousesApi.list).toHaveBeenCalled();
    expect(financialInstitutionsApi.getAll).toHaveBeenCalledWith(false);
    expect(component.clearingHouses.map((item) => item.code)).toEqual(['ACHCOL']);
    expect(component.availableCycles.map((item) => item.cycleCode)).toEqual(['ACH-20260520-C1']);
  });

  it('excluye destino e instituciones inactivas del origen', () => {
    expect(component.originFinancialInstitutions.map((item) => item.id)).toEqual([2]);
    expect(component.defaultDestination?.id).toBe(1);
  });

  it('bloquea cuando no existe institución predeterminada activa', () => {
    fixture.destroy();
    financialInstitutionsApi.getAll.and.returnValue(of([{
      id: 2,
      name: 'Banco UAT Externo',
      routingNumber: '99999',
      transitCode: '900',
      checkDigit: '0',
      isDefaultSource: false,
      status: FinancialInstitutionStatusEnum.Active
    }]));
    createComponent();

    makeFormValid();
    component.executeGenerate();

    expect(component.defaultDestination).toBeNull();
    expect(component.institutionCatalogError).toContain('No existe');
    expect(service.generate).not.toHaveBeenCalled();
  });

  it('bloquea cuando existen varias instituciones predeterminadas activas', () => {
    fixture.destroy();
    financialInstitutionsApi.getAll.and.returnValue(of([
      {
        id: 1,
        name: 'Destino uno',
        routingNumber: '00001',
        transitCode: '283',
        checkDigit: '0',
        isDefaultSource: true,
        status: FinancialInstitutionStatusEnum.Active
      },
      {
        id: 4,
        name: 'Destino dos',
        routingNumber: '00002',
        transitCode: '284',
        checkDigit: '0',
        isDefaultSource: true,
        status: FinancialInstitutionStatusEnum.Active
      }
    ]));
    createComponent();

    expect(component.defaultDestination).toBeNull();
    expect(component.institutionCatalogError).toContain('varias');
  });

  it('muestra únicamente perfiles oficiales, publicados y vigentes de la cámara', () => {
    expect(clearingHousesService.profiles).toHaveBeenCalledWith('ACHCOL');
    expect(component.activeProfiles.map((profile) => profile.profileId)).toEqual([11]);
    expect(component.activeProfiles[0].isOfficialModel).toBeTrue();
  });

  it('aplica validadores específicos de monto, causal y referencias', () => {
    component.form.controls.amount.setValue('123.456');
    expect(component.form.controls.amount.hasError('money')).toBeTrue();

    component.requestModeChange('DifferentialResponses');
    component.form.controls.scenarioType.setValue('IncomingCreditReturn');
    component.simulationContextChanged();
    expect(component.form.controls.reasonCode.hasError('required')).toBeTrue();

    component.form.controls.scenarioType.setValue('IncomingPrenotificationResponse');
    component.simulationContextChanged();
    expect(component.form.controls.pendingPrenotificationReferencesText.hasError('required')).toBeTrue();
  });

  it('no ejecuta con formulario inválido', () => {
    component.form.controls.clearingHouseCode.setValue('');
    component.executeGenerate();
    expect(service.generate).not.toHaveBeenCalled();
  });

  it('confirma antes de generar desde la interfaz', () => {
    makeFormValid();
    component.confirmGeneration();
    expect(dialog.open).toHaveBeenCalled();
    expect(service.generate).toHaveBeenCalledTimes(1);
  });

  it('construye payload con precisión monetaria y fecha local', () => {
    makeFormValid();
    component.form.controls.amount.setValue('1234.56');
    component.executeGenerate();

    const payload: GenerateNachaInboundSimulationRequest = service.generate.calls.mostRecent().args[0];
    expect(payload.amount).toBe(1234.56);
    expect(payload.businessDate).toBe('2026-05-20');
    expect(payload.originFinancialInstitutionId).toBe(2);
    expect('destinationFinancialInstitutionId' in payload).toBeFalse();
  });

  it('previene doble ejecución mientras la primera solicitud está activa', () => {
    const pending = new Subject<NachaInboundSimulationResult>();
    service.generate.and.returnValue(pending.asObservable());
    makeFormValid();

    component.executeGenerate();
    component.executeGenerate();

    expect(component.generating).toBeTrue();
    expect(service.generate).toHaveBeenCalledTimes(1);
    pending.next(result);
    pending.complete();
    expect(component.generating).toBeFalse();
  });

  it('mantiene loading y resultado trazable en éxito', () => {
    makeFormValid();
    component.executeGenerate();
    fixture.detectChanges();

    expect(component.generating).toBeFalse();
    expect(component.result?.simulationId).toBe('SIM-UAT-91');
    expect(component.result?.sha256).toHaveSize(64);
    expect(fixture.nativeElement.textContent).toContain('Simulación generada');
    expect(fixture.nativeElement.textContent).toContain('SIM-UAT-91');
  });

  it('sanitiza el error y libera loading', () => {
    service.generate.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 422,
      error: { detail: 'Regla no configurada\nDetalle controlado' }
    })));
    makeFormValid();

    component.executeGenerate();

    expect(component.generating).toBeFalse();
    expect(notifications.error).toHaveBeenCalledWith('Regla no configurada Detalle controlado');
  });

  it('valida configuración sin generar archivo', () => {
    makeFormValid();
    component.preview();
    expect(service.preview).toHaveBeenCalledTimes(1);
    expect(service.generate).not.toHaveBeenCalled();
    expect(notifications.success).toHaveBeenCalled();
  });

  it('limpia resultado y prepara una nueva simulación coherente', () => {
    makeFormValid();
    component.executeGenerate();
    component.resetSimulation();

    expect(component.result).toBeNull();
    expect(component.form.controls.simulationMode.value).toBe('IncomingTransactions');
    expect(component.form.controls.amount.value).toBe('1000.00');
    expect(component.selectedTransactionIds.size).toBe(0);
  });

  it('distingue error de historial del estado vacío', () => {
    service.list.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 500,
      error: { message: 'Consulta no disponible' }
    })));
    component.loadHistory();
    fixture.detectChanges();

    expect(component.historyError).toBe('Consulta no disponible');
    expect(component.hasLoadedHistory).toBeTrue();
    expect(fixture.nativeElement.textContent).toContain('No se pudo cargar el historial');
  });
});
