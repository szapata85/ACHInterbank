import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import {
  IntegrationMappingAdminService,
  IntegrationMappingSet,
  IntegrationMethod,
  IntegrationMethodParameter,
  IntegrationSourceCatalogField,
  IntegrationTransformationCatalog
} from '../../../core/services/integration-mapping-admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { MappingSetsPageComponent } from './mapping-sets-page.component';

describe('MappingSetsPageComponent', () => {
  let fixture: ComponentFixture<MappingSetsPageComponent>;
  let component: MappingSetsPageComponent;
  let api: jasmine.SpyObj<IntegrationMappingAdminService>;
  let router: jasmine.SpyObj<Router>;
  let auth: jasmine.SpyObj<AuthService>;

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
    { id: 7, sourceKind: 'Transaction', entityName: 'AchTransaction', fieldPath: 'AchTransaction.Reference', displayName: 'Reference', dataType: 'string', cardinality: 'Scalar', nullable: false, sortOrder: 7, isActive: true }
  ];

  const targetFields: IntegrationMethodParameter[] = [
    { id: 9, methodId: 1, parameterPath: 'Proc_Contrapartidas.CuentaOrigen', displayName: 'CuentaOrigen', descriptionEs: 'Cuenta origen debito', category: 'SOAP', exampleValue: '0000003101', uiHelpText: '', dataType: 'string', direction: 'Input', cardinality: 'Scalar', required: true, sortOrder: 1, isActive: true },
    { id: 10, methodId: 3, parameterPath: 'Proc_Transacciones.TraceNumber', displayName: 'TraceNumber', descriptionEs: 'Trace destino', category: 'SOAP', exampleValue: '123', uiHelpText: '', dataType: 'string', direction: 'Input', cardinality: 'Scalar', required: true, sortOrder: 1, isActive: true },
    { id: 11, methodId: 2, parameterPath: 'RegistrarRespuestaTransaccion.CodigoRespuesta', displayName: 'CodigoRespuesta', descriptionEs: 'Codigo respuesta', category: 'SOAP', exampleValue: '00', uiHelpText: '', dataType: 'string', direction: 'Input', cardinality: 'Scalar', required: true, sortOrder: 1, isActive: true }
  ];

  const transformations: IntegrationTransformationCatalog[] = [
    { code: 'Trim', displayName: 'Trim', description: 'Limpia espacios', supportsFormatMask: false, supportsMultipleSources: false }
  ];

  const mappingSets: IntegrationMappingSet[] = [
    {
      id: '11111111-1111-1111-1111-111111111111',
      methodId: 1,
      methodCode: 'WSCFAACH.Proc_Contrapartidas',
      name: 'Contrapartidas UAT',
      version: 1,
      status: 'Published',
      isActive: true,
      notes: 'Version controlada',
      publishedAtUtc: '2026-06-01T00:00:00Z',
      publishedBy: 'qa',
      rules: [
        {
          id: 100,
          mappingSetId: '11111111-1111-1111-1111-111111111111',
          methodId: 1,
          parameterId: 9,
          sourceKind: 'NachaHeader',
          sourceCatalogFieldId: 1,
          sourceFieldPath: 'NachaHeaders.FileIdModifier',
          fixedValue: null,
          defaultValue: null,
          transformationCode: 'Trim',
          formatMask: null,
          priority: 1,
          requiredOverride: true,
          enabled: true,
          conditionExpression: null
        }
      ]
    }
  ];

  beforeEach(async () => {
    api = jasmine.createSpyObj<IntegrationMappingAdminService>('IntegrationMappingAdminService', [
      'getMethods',
      'getMappingSets',
      'createDraft',
      'getSourceCatalog',
      'getMethodParameters',
      'getTransformations',
      'getHistory',
      'clone',
      'upsertRules'
    ]);
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['hasPermission']);
    auth.hasPermission.and.returnValue(true);
    api.getMethods.and.returnValue(of(methods));
    api.getMappingSets.and.callFake((methodId?: number) => of(methodId === 1 ? mappingSets : []));
    api.getSourceCatalog.and.returnValue(of(sourceCatalog));
    api.getMethodParameters.and.callFake((methodId: number) => of(targetFields.filter((field) => field.methodId === methodId)));
    api.getTransformations.and.returnValue(of(transformations));
    api.getHistory.and.returnValue(of([]));
    api.clone.and.returnValue(of({ ...mappingSets[0], id: '22222222-2222-2222-2222-222222222222', status: 'Draft', version: 0 }));
    api.createDraft.and.returnValue(of({ ...mappingSets[0], id: '33333333-3333-3333-3333-333333333333', status: 'Draft', version: 0, rules: [] }));
    api.upsertRules.and.returnValue(of({ ...mappingSets[0], status: 'Draft', version: 0 }));

    await TestBed.configureTestingModule({
      imports: [MappingSetsPageComponent],
      providers: [
        { provide: IntegrationMappingAdminService, useValue: api },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['success', 'error']) },
        { provide: Router, useValue: router },
        { provide: AuthService, useValue: auth },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: { get: () => null } } } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MappingSetsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renderiza la pantalla como Matriz de campos SOAP', () => {
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Matriz de campos SOAP');
    expect(text).toContain('Servicio SOAP');
    expect(text).toContain('Parametro SOAP');
    expect(text).toContain('Tabla origen');
    expect(text).toContain('Campo origen');
    expect(text).toContain('Regla de conversion');
    expect(text).toContain('Obligatorio');
    expect(text).toContain('Estado');
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-matrix-table"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-card"]')).toBeFalsy();
  });

  it('muestra los tres servicios con descripcion funcional en espanol', () => {
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Proc_Contrapartidas');
    expect(text).toContain('Proc_Transacciones');
    expect(text).toContain('RegistrarRespuestaTransaccion');
    expect(text).toContain('debitos monetarios originados por CFA');
    expect(text).toContain('creditos monetarios recibidos desde otra entidad financiera');
    expect(text).toContain('No realiza movimiento monetario');
  });

  it('resuelve una relacion parametro SOAP contra tabla y campo origen permitido', () => {
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('CuentaOrigen');
    expect(text).toContain('NachaHeaders');
    expect(text).toContain('FileIdModifier');
    expect(text).toContain('Limpiar espacios');
    expect(text).toContain('Mapeado');
    expect(text).not.toContain('MonetaryDebitRequest');
    expect(text).not.toContain('OutboundRequest');
  });

  it('filtra fuentes no permitidas de las opciones principales de edicion', () => {
    const options = component.allowedSourceFields
      .map((field) => `${component.getSourceKindLabel(field.sourceKind)} ${field.displayName}`)
      .join(' ');

    expect(options).toContain('NachaHeaders');
    expect(options).toContain('BatchHeaders');
    expect(options).toContain('EntryDetails');
    expect(options).toContain('AddendaRecords');
    expect(options).toContain('BatchControls');
    expect(options).toContain('FileControls');
    expect(options).not.toContain('AchTransaction');
  });

  it('mueve ruta tecnica y auditoria fuera de la vista principal', () => {
    const mainText = fixture.nativeElement.querySelector('.mapping-matrix-page').textContent as string;
    expect(mainText).not.toContain('NachaHeaders.FileIdModifier');

    expect(component.matrixRows[0].sourceField?.fieldPath).toBe('NachaHeaders.FileIdModifier');
  });

  it('permite usuario solo consulta sin acciones de edicion', async () => {
    auth.hasPermission.and.returnValue(false);
    const readOnlyFixture = TestBed.createComponent(MappingSetsPageComponent);
    readOnlyFixture.detectChanges();

    expect(readOnlyFixture.nativeElement.querySelector('[data-testid="mapping-detail-button"]')).toBeTruthy();
    expect(readOnlyFixture.nativeElement.querySelector('[data-testid="mapping-edit-button"]')).toBeFalsy();
  });
});
