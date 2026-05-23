import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import {
  IntegrationMappingAdminService,
  IntegrationMappingSet,
  IntegrationMethod,
  IntegrationMethodParameter,
  IntegrationSourceCatalogField
} from '../../../core/services/integration-mapping-admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { MappingSetsPageComponent } from './mapping-sets-page.component';

describe('MappingSetsPageComponent', () => {
  let fixture: ComponentFixture<MappingSetsPageComponent>;
  let component: MappingSetsPageComponent;
  let api: jasmine.SpyObj<IntegrationMappingAdminService>;
  let router: jasmine.SpyObj<Router>;

  const methods: IntegrationMethod[] = [
    {
      id: 1,
      code: 'WSCFAACH.Proc_Contrapartidas',
      displayName: 'Proc_Contrapartidas',
      soapClientCode: 'WscfaachSoapClient',
      isActive: true,
      integrationKey: 'WSCFAACH',
      operationKey: 'Proc_Contrapartidas',
      mappingDirection: 'OutboundRequest',
      mappingPurpose: 'MonetaryDebitRequest',
      functionalNature: 'Debito monetario',
      functionalOriginator: 'CFA originadora',
      movesMoney: true
    },
    {
      id: 3,
      code: 'WSCFAACH.Proc_Transacciones',
      displayName: 'Proc_Transacciones',
      soapClientCode: 'WscfaachSoapClient',
      isActive: true,
      integrationKey: 'WSCFAACH',
      operationKey: 'Proc_Transacciones',
      mappingDirection: 'OutboundRequest',
      mappingPurpose: 'MonetaryCreditRequest',
      functionalNature: 'Credito monetario',
      functionalOriginator: 'Entidad financiera externa; CFA receptora',
      movesMoney: true
    },
    {
      id: 2,
      code: 'WSAXON.RegistrarRespuestaTransaccion',
      displayName: 'RegistrarRespuestaTransaccion',
      soapClientCode: 'WsAxonRespuestaTransaccionesSoapClient',
      isActive: true,
      integrationKey: 'WSAXON',
      operationKey: 'RegistrarRespuestaTransaccion',
      mappingDirection: 'InboundResponse',
      mappingPurpose: 'DifferentialResponseNotification',
      functionalNature: 'Respuesta diferencial / notificacion',
      functionalOriginator: 'Entidad/camara/proveedor externo',
      movesMoney: false
    }
  ];

  const sourceCatalog: IntegrationSourceCatalogField[] = [
    { id: 1, sourceKind: 'NachaHeader', entityName: 'NachaHeaders', fieldPath: 'NachaHeaders.FileIdModifier', displayName: 'FileIdModifier', dataType: 'string', cardinality: 'Scalar', nullable: false, sortOrder: 1, isActive: true },
    { id: 2, sourceKind: 'BatchHeader', entityName: 'BatchHeaders', fieldPath: 'BatchHeaders.CompanyIdentification', displayName: 'CompanyIdentification', dataType: 'string', cardinality: 'Scalar', nullable: false, sortOrder: 2, isActive: true },
    { id: 3, sourceKind: 'EntryDetail', entityName: 'EntryDetails', fieldPath: 'EntryDetails.TraceNumber', displayName: 'TraceNumber', dataType: 'string', cardinality: 'Scalar', nullable: false, sortOrder: 3, isActive: true },
    { id: 4, sourceKind: 'AddendaRecord', entityName: 'AddendaRecords', fieldPath: 'AddendaRecords.PaymentRelatedInformation', displayName: 'PaymentRelatedInformation', dataType: 'string', cardinality: 'Scalar', nullable: true, sortOrder: 4, isActive: true },
    { id: 5, sourceKind: 'BatchControl', entityName: 'BatchControls', fieldPath: 'BatchControls.EntryHash', displayName: 'EntryHash', dataType: 'number', cardinality: 'Scalar', nullable: false, sortOrder: 5, isActive: true },
    { id: 6, sourceKind: 'FileControl', entityName: 'FileControls', fieldPath: 'FileControls.BlockCount', displayName: 'BlockCount', dataType: 'number', cardinality: 'Scalar', nullable: false, sortOrder: 6, isActive: true },
    { id: 7, sourceKind: 'Transaction', entityName: 'AchTransaction', fieldPath: 'AchTransaction.Reference', displayName: 'Reference', dataType: 'string', cardinality: 'Scalar', nullable: false, sortOrder: 7, isActive: true },
    { id: 8, sourceKind: 'Prenotification', entityName: 'Prenotification', fieldPath: 'Prenotification.Reference', displayName: 'Prenotification Reference', dataType: 'string', cardinality: 'Scalar', nullable: false, sortOrder: 8, isActive: true },
    { id: 9, sourceKind: 'DifferentialResponse', entityName: 'DifferentialResponse', fieldPath: 'DifferentialResponse.ReasonCode', displayName: 'ReasonCode', dataType: 'string', cardinality: 'Scalar', nullable: true, sortOrder: 9, isActive: true }
  ];

  const targetFields: IntegrationMethodParameter[] = [
    { id: 10, methodId: 3, parameterPath: 'Proc_Transacciones.TraceNumber', displayName: 'TraceNumber', descriptionEs: 'Trace destino', category: 'SOAP', exampleValue: '123', uiHelpText: '', dataType: 'string', direction: 'Input', cardinality: 'Scalar', required: true, sortOrder: 1, isActive: true },
    { id: 11, methodId: 2, parameterPath: 'RegistrarRespuestaTransaccion.CodigoRespuesta', displayName: 'CodigoRespuesta', descriptionEs: 'Codigo respuesta', category: 'SOAP', exampleValue: '00', uiHelpText: '', dataType: 'string', direction: 'Input', cardinality: 'Scalar', required: true, sortOrder: 1, isActive: true }
  ];

  const mappingSets: IntegrationMappingSet[] = [
    {
      id: 'mapping-1',
      methodId: 1,
      methodCode: 'WSCFAACH.Proc_Contrapartidas',
      name: 'Contrapartidas UAT',
      version: 1,
      status: 'Published',
      isActive: true,
      notes: 'Version controlada',
      publishedAtUtc: null,
      publishedBy: 'qa',
      rules: []
    }
  ];

  beforeEach(async () => {
    api = jasmine.createSpyObj<IntegrationMappingAdminService>('IntegrationMappingAdminService', [
      'getMethods',
      'getMappingSets',
      'createDraft',
      'getSourceCatalog',
      'getMethodParameters'
    ]);
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    api.getMethods.and.returnValue(of(methods));
    api.getMappingSets.and.callFake((methodId?: number) => of(methodId === 2 ? [] : mappingSets));
    api.getSourceCatalog.and.returnValue(of(sourceCatalog));
    api.getMethodParameters.and.callFake((methodId: number) => of(targetFields.filter((field) => field.methodId === methodId)));
    api.createDraft.and.returnValue(of(mappingSets[0]));

    await TestBed.configureTestingModule({
      imports: [MappingSetsPageComponent],
      providers: [
        { provide: IntegrationMappingAdminService, useValue: api },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['success', 'error']) },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: { get: () => null } } } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MappingSetsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('carga opciones de integracion incluyendo WsAxonRespuestaTransaccionesSoapClient', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('WSCFAACH');
    expect(text).toContain('WSAXON');
    expect(text).toContain('WsAxonRespuestaTransaccionesSoapClient');
    expect(text).toContain('Proc_Transacciones');
    expect(text).toContain('RegistrarRespuestaTransaccion');
    expect(text).toContain('MonetaryCreditRequest');
    expect(text).toContain('DifferentialResponseNotification');
    expect(text).toContain('OutboundRequest');
    expect(text).toContain('InboundResponse');
    expect(fixture.nativeElement.querySelector('[data-testid="integration-select"]')).toBeTruthy();
  });

  it('muestra fuentes NACHA-M desagregadas desde catalogo controlado', () => {
    const text = fixture.nativeElement.textContent as string;

    expect(fixture.nativeElement.querySelector('[data-testid="mapping-catalog-panel"]')).toBeTruthy();
    expect(text).toContain('NachaHeaders');
    expect(text).toContain('BatchHeaders');
    expect(text).toContain('EntryDetails');
    expect(text).toContain('AddendaRecords');
    expect(text).toContain('BatchControls');
    expect(text).toContain('FileControls');
    expect(text).toContain('No hay SQL libre ni seleccion arbitraria de tablas');
  });

  it('permite seleccionar WsAxon y muestra estado vacio claro cuando no hay mappings', () => {
    component.createDraftForm.patchValue({ methodId: 2 });
    component.onMethodChange();
    fixture.detectChanges();

    expect(api.getMappingSets).toHaveBeenCalledWith(2);
    expect(fixture.nativeElement.querySelector('[data-testid="empty-mappings-state"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('No hay mappings para la integracion seleccionada.');
  });

  it('abre detalle read-only y modal de edicion guiada', () => {
    component.openDetail(mappingSets[0]);
    expect(component.modalMode).toBe('detail');
    expect(component.selectedMapping?.name).toBe('Contrapartidas UAT');

    component.closeModal();
    component.openEdit(mappingSets[0]);
    expect(component.modalMode).toBe('edit');
    expect(component.selectedMapping?.name).toBe('Contrapartidas UAT');
  });

  it('muestra acciones visibles por card sin grilla principal', () => {
    expect(fixture.nativeElement.querySelector('ui-grilla-empresarial')).toBeFalsy();
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-card"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-detail-button"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-edit-button"]')).toBeTruthy();
  });
});
