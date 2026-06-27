import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import {
  IntegrationMappingAdminService,
  IntegrationMappingSet,
  IntegrationMethod,
  IntegrationMethodParameter,
  IntegrationSourceKind,
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
    { id: 7, sourceKind: 'Transaction', entityName: 'AchTransaction', fieldPath: 'AchTransaction.Reference', displayName: 'Reference', dataType: 'string', cardinality: 'Scalar', nullable: false, sortOrder: 7, isActive: true },
    { id: 8, sourceKind: 'DifferentialResponse', entityName: 'AchResponse', fieldPath: 'differentialResponse.idTransaccion', displayName: 'Id transaccion', dataType: 'string', cardinality: 'Scalar', nullable: false, sortOrder: 8, isActive: true },
    { id: 9, sourceKind: 'DifferentialResponse', entityName: 'AchResponse', fieldPath: 'differentialResponse.idCanal', displayName: 'Id canal', dataType: 'int', cardinality: 'Scalar', nullable: false, sortOrder: 9, isActive: true },
    { id: 10, sourceKind: 'DifferentialResponse', entityName: 'AchResponse', fieldPath: 'differentialResponse.nombreCanal', displayName: 'Nombre canal', dataType: 'string', cardinality: 'Scalar', nullable: false, sortOrder: 10, isActive: true },
    { id: 11, sourceKind: 'DifferentialResponse', entityName: 'AchResponse', fieldPath: 'differentialResponse.idEstado', displayName: 'Id estado', dataType: 'int', cardinality: 'Scalar', nullable: false, sortOrder: 11, isActive: true },
    { id: 12, sourceKind: 'DifferentialResponse', entityName: 'AchResponse', fieldPath: 'differentialResponse.codigoCausalExterna', displayName: 'Causal externa', dataType: 'string', cardinality: 'Scalar', nullable: true, sortOrder: 12, isActive: true },
    { id: 13, sourceKind: 'DifferentialResponse', entityName: 'AchResponse', fieldPath: 'differentialResponse.idTransaccionServicioExterno', displayName: 'Id transaccion servicio externo', dataType: 'int', cardinality: 'Scalar', nullable: false, sortOrder: 13, isActive: true },
    { id: 14, sourceKind: 'DifferentialResponse', entityName: 'AchResponse', fieldPath: 'differentialResponse.descripcionCausalExterna', displayName: 'Descripcion causal externa', dataType: 'string', cardinality: 'Scalar', nullable: true, sortOrder: 14, isActive: true },
    { id: 15, sourceKind: 'Cycle', entityName: 'AchCycle', fieldPath: 'cycle.processingDate', displayName: 'Fecha proceso ciclo', dataType: 'datetime', cardinality: 'Scalar', nullable: false, sortOrder: 15, isActive: true },
    { id: 16, sourceKind: 'ClearingHouse', entityName: 'ClearingHouse', fieldPath: 'clearinghouse.id', displayName: 'Id camara', dataType: 'int', cardinality: 'Scalar', nullable: false, sortOrder: 16, isActive: true },
    { id: 17, sourceKind: 'Constant', entityName: 'Constant', fieldPath: 'constant.value', displayName: 'Valor fijo', dataType: 'string', cardinality: 'Scalar', nullable: true, sortOrder: 17, isActive: true },
    { id: 18, sourceKind: 'Batch', entityName: 'AchBatch', fieldPath: 'batch.id', displayName: 'Id lote', dataType: 'int', cardinality: 'Scalar', nullable: false, sortOrder: 18, isActive: true },
    { id: 19, sourceKind: 'FinancialInstitution', entityName: 'FinancialInstitution', fieldPath: 'financialInstitution.routingNumber', displayName: 'Routing number', dataType: 'string', cardinality: 'Scalar', nullable: false, sortOrder: 19, isActive: true },
    { id: 20, sourceKind: 'EntryDetail', entityName: 'EntryDetails', fieldPath: 'EntryDetails.Amount', displayName: 'Amount', dataType: 'number', cardinality: 'Scalar', nullable: false, sortOrder: 20, isActive: true }
  ];

  const targetFields: IntegrationMethodParameter[] = [
    { id: 9, methodId: 1, parameterPath: 'Proc_Contrapartidas.CuentaOrigen', displayName: 'CuentaOrigen', descriptionEs: 'Cuenta origen debito', category: 'SOAP', exampleValue: '0000003101', uiHelpText: '', dataType: 'string', direction: 'Input', cardinality: 'Scalar', required: true, sortOrder: 1, isActive: true },
    { id: 18, methodId: 1, parameterPath: 'OFIDTX', displayName: 'OFIDTX', descriptionEs: 'Id transaccion origen', category: 'SOAP', exampleValue: 'REF-1', uiHelpText: '', dataType: 'string', direction: 'Input', cardinality: 'Scalar', required: true, sortOrder: 2, isActive: true },
    { id: 19, methodId: 1, parameterPath: 'OFFECHEFEC', displayName: 'OFFECHEFEC', descriptionEs: 'Fecha efectiva', category: 'SOAP', exampleValue: '20260625', uiHelpText: '', dataType: 'string', direction: 'Input', cardinality: 'Scalar', required: true, sortOrder: 3, isActive: true },
    { id: 20, methodId: 1, parameterPath: 'OFIDCAMCOMPE', displayName: 'OFIDCAMCOMPE', descriptionEs: 'Camara compensacion', category: 'SOAP', exampleValue: '1', uiHelpText: '', dataType: 'int', direction: 'Input', cardinality: 'Scalar', required: true, sortOrder: 4, isActive: true },
    { id: 21, methodId: 1, parameterPath: 'OFDD', displayName: 'OFDD', descriptionEs: 'Naturaleza debito/credito', category: 'SOAP', exampleValue: 'C', uiHelpText: '', dataType: 'string', direction: 'Input', cardinality: 'Scalar', required: true, sortOrder: 5, isActive: true },
    { id: 22, methodId: 1, parameterPath: 'OFIDLOT', displayName: 'OFIDLOT', descriptionEs: 'Id lote', category: 'SOAP', exampleValue: '1', uiHelpText: '', dataType: 'int', direction: 'Input', cardinality: 'Scalar', required: true, sortOrder: 6, isActive: true },
    { id: 23, methodId: 1, parameterPath: 'ANSIDLOTE', displayName: 'ANSIDLOTE', descriptionEs: 'Id lote respuesta reservado', category: 'SOAP', exampleValue: '0', uiHelpText: '', dataType: 'int', direction: 'Input', cardinality: 'Scalar', required: false, sortOrder: 7, isActive: true },
    { id: 24, methodId: 1, parameterPath: 'ANCLC', displayName: 'ANCLC', descriptionEs: 'Codigo local reservado', category: 'SOAP', exampleValue: '', uiHelpText: '', dataType: 'string', direction: 'Input', cardinality: 'Scalar', required: false, sortOrder: 8, isActive: true },
    { id: 10, methodId: 3, parameterPath: 'Proc_Transacciones.TraceNumber', displayName: 'TraceNumber', descriptionEs: 'Trace destino', category: 'SOAP', exampleValue: '123', uiHelpText: '', dataType: 'string', direction: 'Input', cardinality: 'Scalar', required: true, sortOrder: 1, isActive: true },
    { id: 11, methodId: 2, parameterPath: 'idCanal', displayName: 'Id canal', descriptionEs: 'Identificador del canal', category: 'Respuesta transaccion', exampleValue: '1', uiHelpText: '', dataType: 'int', direction: 'Input', cardinality: 'Scalar', required: true, sortOrder: 1, isActive: true },
    { id: 12, methodId: 2, parameterPath: 'nombreCanal', displayName: 'Nombre canal', descriptionEs: 'Nombre del canal', category: 'Respuesta transaccion', exampleValue: 'ACH', uiHelpText: '', dataType: 'string', direction: 'Input', cardinality: 'Scalar', required: true, sortOrder: 2, isActive: true },
    { id: 13, methodId: 2, parameterPath: 'idTransaccion', displayName: 'Id transaccion', descriptionEs: 'Id transaccion', category: 'Respuesta transaccion', exampleValue: 'TX-1', uiHelpText: '', dataType: 'string', direction: 'Input', cardinality: 'Scalar', required: true, sortOrder: 3, isActive: true },
    { id: 14, methodId: 2, parameterPath: 'idEstado', displayName: 'Id estado', descriptionEs: 'Id estado', category: 'Respuesta transaccion', exampleValue: '1', uiHelpText: '', dataType: 'int', direction: 'Input', cardinality: 'Scalar', required: true, sortOrder: 4, isActive: true },
    { id: 15, methodId: 2, parameterPath: 'causal', displayName: 'Causal', descriptionEs: 'Causal', category: 'Respuesta transaccion', exampleValue: 'R03', uiHelpText: '', dataType: 'string', direction: 'Input', cardinality: 'Scalar', required: false, sortOrder: 5, isActive: true },
    { id: 16, methodId: 2, parameterPath: 'idTransaccionAxon', displayName: 'Id transaccion Axon', descriptionEs: 'Id transaccion Axon', category: 'Respuesta transaccion', exampleValue: '1001', uiHelpText: '', dataType: 'int', direction: 'Input', cardinality: 'Scalar', required: true, sortOrder: 6, isActive: true },
    { id: 17, methodId: 2, parameterPath: 'descripcionCausal', displayName: 'Descripcion causal', descriptionEs: 'Descripcion causal', category: 'Respuesta transaccion', exampleValue: 'Cuenta no localizada', uiHelpText: '', dataType: 'string', direction: 'Input', cardinality: 'Scalar', required: false, sortOrder: 7, isActive: true }
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
        },
        {
          id: 102,
          mappingSetId: '11111111-1111-1111-1111-111111111111',
          methodId: 1,
          parameterId: 18,
          sourceKind: 'Transaction',
          sourceCatalogFieldId: 7,
          sourceFieldPath: 'AchTransaction.Reference',
          fixedValue: null,
          defaultValue: null,
          transformationCode: null,
          formatMask: null,
          priority: 1,
          requiredOverride: true,
          enabled: true,
          conditionExpression: null
        },
        {
          id: 103,
          mappingSetId: '11111111-1111-1111-1111-111111111111',
          methodId: 1,
          parameterId: 19,
          sourceKind: 'Cycle',
          sourceCatalogFieldId: 15,
          sourceFieldPath: 'cycle.processingDate',
          fixedValue: null,
          defaultValue: null,
          transformationCode: null,
          formatMask: null,
          priority: 1,
          requiredOverride: true,
          enabled: true,
          conditionExpression: null
        },
        {
          id: 104,
          mappingSetId: '11111111-1111-1111-1111-111111111111',
          methodId: 1,
          parameterId: 20,
          sourceKind: 'ClearingHouse',
          sourceCatalogFieldId: 16,
          sourceFieldPath: 'clearinghouse.id',
          fixedValue: null,
          defaultValue: null,
          transformationCode: null,
          formatMask: null,
          priority: 1,
          requiredOverride: true,
          enabled: true,
          conditionExpression: null
        },
        {
          id: 105,
          mappingSetId: '11111111-1111-1111-1111-111111111111',
          methodId: 1,
          parameterId: 21,
          sourceKind: 'Constant',
          sourceCatalogFieldId: 17,
          sourceFieldPath: 'constant.value',
          fixedValue: 'C',
          defaultValue: null,
          transformationCode: null,
          formatMask: null,
          priority: 1,
          requiredOverride: true,
          enabled: true,
          conditionExpression: null
        },
        {
          id: 106,
          mappingSetId: '11111111-1111-1111-1111-111111111111',
          methodId: 1,
          parameterId: 22,
          sourceKind: 'Batch',
          sourceCatalogFieldId: 18,
          sourceFieldPath: 'batch.id',
          fixedValue: null,
          defaultValue: null,
          transformationCode: null,
          formatMask: null,
          priority: 1,
          requiredOverride: true,
          enabled: true,
          conditionExpression: null
        }
      ]
    },
    {
      id: '22222222-2222-2222-2222-222222222222',
      methodId: 3,
      methodCode: 'WSCFAACH.Proc_Transacciones',
      name: 'Transacciones UAT',
      version: 1,
      status: 'Published',
      isActive: true,
      notes: 'Version controlada',
      publishedAtUtc: '2026-06-01T00:00:00Z',
      publishedBy: 'qa',
      rules: [
        {
          id: 101,
          mappingSetId: '22222222-2222-2222-2222-222222222222',
          methodId: 3,
          parameterId: 10,
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
    },
    {
      id: '33333333-3333-3333-3333-333333333333',
      methodId: 2,
      methodCode: 'WSAXON.RegistrarRespuestaTransaccion',
      name: 'RegistrarRespuestaTransaccion WSDL',
      version: 1,
      status: 'Published',
      isActive: true,
      notes: 'Version WSDL',
      publishedAtUtc: '2026-06-01T00:00:00Z',
      publishedBy: 'qa',
      rules: [
        { id: 201, mappingSetId: '33333333-3333-3333-3333-333333333333', methodId: 2, parameterId: 11, sourceKind: 'DifferentialResponse', sourceCatalogFieldId: 9, sourceFieldPath: 'differentialResponse.idCanal', fixedValue: null, defaultValue: null, transformationCode: null, formatMask: null, priority: 1, requiredOverride: true, enabled: true, conditionExpression: null },
        { id: 202, mappingSetId: '33333333-3333-3333-3333-333333333333', methodId: 2, parameterId: 12, sourceKind: 'DifferentialResponse', sourceCatalogFieldId: 10, sourceFieldPath: 'differentialResponse.nombreCanal', fixedValue: null, defaultValue: null, transformationCode: null, formatMask: null, priority: 1, requiredOverride: true, enabled: true, conditionExpression: null },
        { id: 203, mappingSetId: '33333333-3333-3333-3333-333333333333', methodId: 2, parameterId: 13, sourceKind: 'DifferentialResponse', sourceCatalogFieldId: 8, sourceFieldPath: 'differentialResponse.idTransaccion', fixedValue: null, defaultValue: null, transformationCode: null, formatMask: null, priority: 1, requiredOverride: true, enabled: true, conditionExpression: null },
        { id: 204, mappingSetId: '33333333-3333-3333-3333-333333333333', methodId: 2, parameterId: 14, sourceKind: 'DifferentialResponse', sourceCatalogFieldId: 11, sourceFieldPath: 'differentialResponse.idEstado', fixedValue: null, defaultValue: null, transformationCode: null, formatMask: null, priority: 1, requiredOverride: true, enabled: true, conditionExpression: null },
        { id: 205, mappingSetId: '33333333-3333-3333-3333-333333333333', methodId: 2, parameterId: 15, sourceKind: 'DifferentialResponse', sourceCatalogFieldId: 12, sourceFieldPath: 'differentialResponse.codigoCausalExterna', fixedValue: null, defaultValue: null, transformationCode: null, formatMask: null, priority: 1, requiredOverride: false, enabled: true, conditionExpression: null },
        { id: 206, mappingSetId: '33333333-3333-3333-3333-333333333333', methodId: 2, parameterId: 16, sourceKind: 'DifferentialResponse', sourceCatalogFieldId: 13, sourceFieldPath: 'differentialResponse.idTransaccionServicioExterno', fixedValue: null, defaultValue: null, transformationCode: null, formatMask: null, priority: 1, requiredOverride: true, enabled: true, conditionExpression: null },
        { id: 207, mappingSetId: '33333333-3333-3333-3333-333333333333', methodId: 2, parameterId: 17, sourceKind: 'DifferentialResponse', sourceCatalogFieldId: 14, sourceFieldPath: 'differentialResponse.descripcionCausalExterna', fixedValue: null, defaultValue: null, transformationCode: null, formatMask: null, priority: 1, requiredOverride: false, enabled: true, conditionExpression: null }
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
    api.getMappingSets.and.callFake((methodId?: number) => of(mappingSets.filter((set) => !methodId || set.methodId === methodId)));
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

  const selectService = (index: number) => {
    const buttons = fixture.nativeElement.querySelectorAll('[data-testid="soap-service-option"]') as NodeListOf<HTMLButtonElement>;
    buttons[index].click();
    fixture.detectChanges();
  };

  const rowText = (parameter: string): string => {
    const rows = Array.from(fixture.nativeElement.querySelectorAll('[data-testid="mapping-matrix-row"]')) as HTMLElement[];
    const row = rows.find((item) => item.textContent?.includes(parameter));
    return row?.textContent ?? '';
  };

  it('renderiza la pantalla como Matriz de campos SOAP', () => {
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Matriz de campos SOAP');
    expect(text).toContain('Parámetro SOAP');
    expect(text).toContain('Estado funcional');
    expect(text).toContain('Origen');
    expect(text).toContain('Campo / relación');
    expect(text).toContain('Observación');
    expect(text).toContain('Acción');
    expect(text).not.toContain('Tabla origen');
    expect(text).not.toContain('Campo origen');
    expect(text).not.toContain('Regla de conversión');
    expect(text).not.toContain('Obligatorio');
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-matrix-table"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-card"]')).toBeFalsy();
  });

  it('renderiza resumen funcional compacto y filtros principales', () => {
    const text = fixture.nativeElement.textContent as string;
    const summary = fixture.nativeElement.querySelector('[data-testid="mapping-functional-summary"]') as HTMLElement;
    const filterBar = fixture.nativeElement.querySelector('[data-testid="mapping-filter-bar"]') as HTMLElement;

    expect(summary).toBeTruthy();
    expect(summary.textContent).toContain('Total parámetros');
    expect(summary.textContent).toContain('Listos');
    expect(summary.textContent).toContain('Pendientes');
    expect(summary.textContent).toContain('Bloqueantes');
    expect(summary.textContent).toContain('Alertas');
    expect(summary.textContent).toContain('Opcionales/reservados');
    expect(filterBar).toBeTruthy();
    expect(text).toContain('Todos');
    expect(text).toContain('Pendientes');
    expect(text).toContain('Bloqueantes');
    expect(text).toContain('Alertas');
    expect(text).toContain('Listos');
    expect(text).toContain('Opcionales');
  });

  it('permite filtrar pendientes y bloqueantes desde la matriz calculada', () => {
    selectService(1);

    component.mappingSets = [
      {
        ...mappingSets[0],
        rules: mappingSets[0].rules
          .filter((rule) => rule.parameterId !== 18)
          .map((rule) => rule.parameterId === 21 ? { ...rule, fixedValue: 'SEED', defaultValue: null } : rule)
      }
    ];

    component.setFilter('Pendientes');
    expect(component.filteredMatrixRows.map((row) => row.parameterSoap)).toEqual(['OFIDTX', 'OFDD']);

    component.setFilter('Bloqueantes');
    expect(component.filteredMatrixRows.map((row) => row.parameterSoap)).toEqual(['OFIDTX', 'OFDD']);
  });

  it('permite filtrar opcionales reservados', () => {
    selectService(1);

    component.setFilter('Opcionales');

    expect(component.filteredMatrixRows.map((row) => row.parameterSoap)).toEqual(['ANSIDLOTE', 'ANCLC']);
  });

  it('muestra los tres servicios con descripcion funcional en espanol', () => {
    const text = () => fixture.nativeElement.textContent as string;

    expect(text()).toContain('Proc_Contrapartidas');
    expect(text()).toContain('Proc_Transacciones');
    expect(text()).toContain('RegistrarRespuestaTransaccion');
    expect(text()).toContain('créditos monetarios recibidos desde otra entidad financiera');

    const buttons = fixture.nativeElement.querySelectorAll('[data-testid="soap-service-option"]') as NodeListOf<HTMLButtonElement>;
    buttons[1].click();
    fixture.detectChanges();
    expect(text()).toContain('débitos monetarios originados por CFA');

    buttons[2].click();
    fixture.detectChanges();
    expect(text()).toContain('No realiza movimiento monetario');
  });

  it('resuelve una relacion parametro SOAP contra tabla y campo origen permitido', () => {
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('TraceNumber');
    expect(text).toContain('Archivo NACHA');
    expect(text).toContain('Modificador de archivo');
    expect(text).toContain('Limpiar espacios');
    expect(text).toContain('Mapeado NACHA');
    expect(text).not.toContain('MonetaryDebitRequest');
    expect(text).not.toContain('OutboundRequest');
  });

  it('filtra fuentes no permitidas de las opciones principales de edicion', () => {
    const options = component.allowedSourceFields
      .map((field) => `${component.getSourceKindLabel(field.sourceKind)} ${component.getSourceFieldLabel(field)}`)
      .join(' ');

    expect(options).toContain('Archivo NACHA');
    expect(options).toContain('Lote NACHA');
    expect(options).toContain('Detalle NACHA');
    expect(options).toContain('Addenda NACHA');
    expect(options).toContain('Control lote NACHA');
    expect(options).toContain('Control archivo NACHA');
    expect(options).toContain('Respuesta diferencial');
    expect(options).not.toContain('AchTransaction');
  });

  it('muestra RegistrarRespuestaTransaccion con los siete parámetros WSDL reales', () => {
    selectService(2);

    const text = fixture.nativeElement.textContent as string;
    const rows = fixture.nativeElement.querySelectorAll('[data-testid="mapping-matrix-row"]') as NodeListOf<HTMLElement>;

    expect(rows.length).toBe(7);
    expect(text).toContain('idCanal');
    expect(text).toContain('nombreCanal');
    expect(text).toContain('idTransaccion');
    expect(text).toContain('idEstado');
    expect(text).toContain('causal');
    expect(text).toContain('idTransaccionAxon');
    expect(text).toContain('descripcionCausal');
    expect(text).toContain('Respuesta diferencial');
    expect(text).toContain('Mapeado desde respuesta diferencial');
    rows.forEach((row) => expect(row.textContent).not.toContain('Sin mapear'));
    expect(text).not.toContain('ANSIDLOTE');
  });

  it('clasifica Transaction como fuente transaccional mapeada y no como Sin mapear', () => {
    selectService(1);

    const text = rowText('OFIDTX');

    expect(text).toContain('Transacción');
    expect(text).toContain('Referencia');
    expect(text).toContain('Mapeado transaccional');
    expect(text).not.toContain('Sin mapear');
    expect(text).not.toContain('Mapeado técnico');
  });

  it('clasifica Cycle como fuente de ciclo/camara mapeada', () => {
    selectService(1);

    const text = rowText('OFFECHEFEC');

    expect(text).toContain('Ciclo');
    expect(text).toContain('Fecha de proceso');
    expect(text).toContain('Mapeado por ciclo/cámara');
    expect(text).not.toContain('Sin mapear');
  });

  it('clasifica ClearingHouse como fuente de ciclo/camara mapeada', () => {
    selectService(1);

    const text = rowText('OFIDCAMCOMPE');

    expect(text).toContain('Cámara');
    expect(text).toContain('Identificador interno');
    expect(text).toContain('Mapeado por ciclo/cámara');
    expect(text).not.toContain('Sin mapear');
  });

  it('clasifica FinancialInstitution como fuente de ciclo/camara mapeada', () => {
    selectService(1);

    component.mappingSets = [
      {
        ...mappingSets[0],
        rules: mappingSets[0].rules.map((rule) => rule.parameterId === 20
          ? {
            ...rule,
            sourceKind: 'FinancialInstitution',
            sourceCatalogFieldId: 19,
            sourceFieldPath: 'financialInstitution.routingNumber'
          }
          : rule)
      }
    ];

    const row = component.matrixRows.find((item) => item.parameterSoap === 'OFIDCAMCOMPE')!;
    const text = `${row.tableOrigin} ${row.fieldOrigin} ${row.status}`;

    expect(text).toContain('Entidad financiera');
    expect(text).toContain('Código ruta');
    expect(text).toContain('Mapeado por ciclo/cámara');
    expect(text).not.toContain('Sin mapear');
  });

  it('clasifica Constant como constante tecnica mapeada', () => {
    selectService(1);

    const text = rowText('OFDD');

    expect(text).toContain('Constante');
    expect(text).toContain('Valor fijo');
    expect(text).toContain('Constante técnica');
    expect(text).not.toContain('Sin mapear');
  });

  it('clasifica Constant con SEED como placeholder pendiente funcional', () => {
    selectService(1);

    component.mappingSets = [
      {
        ...mappingSets[0],
        rules: mappingSets[0].rules.map((rule) => rule.parameterId === 21
          ? { ...rule, fixedValue: 'SEED', defaultValue: null }
          : rule)
      }
    ];

    const row = component.matrixRows.find((item) => item.parameterSoap === 'OFDD')!;
    const text = `${row.fieldOrigin} ${row.conversionRule} ${row.status}`;

    expect(text).toContain('Pendiente funcional');
    expect(text).not.toContain('Constante técnica');
  });

  it('clasifica DifferentialResponse como fuente valida para RegistrarRespuestaTransaccion', () => {
    selectService(2);

    const text = rowText('idEstado');

    expect(text).toContain('Respuesta diferencial');
    expect(text).toContain('Estado');
    expect(text).toContain('Mapeado desde respuesta diferencial');
    expect(text).not.toContain('Sin mapear');
  });

  it('clasifica fuentes NACHA como Mapeado NACHA', () => {
    selectService(0);

    const cases = [
      { fieldId: 1, kind: 'NachaHeader' as IntegrationSourceKind, path: 'NachaHeaders.FileIdModifier', table: 'Archivo NACHA', field: 'Modificador de archivo' },
      { fieldId: 2, kind: 'BatchHeader' as IntegrationSourceKind, path: 'BatchHeaders.CompanyIdentification', table: 'Lote NACHA', field: 'Identificación de compañía' },
      { fieldId: 20, kind: 'EntryDetail' as IntegrationSourceKind, path: 'EntryDetails.Amount', table: 'Detalle NACHA', field: 'Monto' }
    ];

    for (const item of cases) {
      component.mappingSets = [
        {
          ...mappingSets[1],
          rules: [
            {
              ...mappingSets[1].rules[0],
              sourceKind: item.kind,
              sourceCatalogFieldId: item.fieldId,
              sourceFieldPath: item.path
            }
          ]
        }
      ];

      const row = component.matrixRows.find((item) => item.parameterSoap === 'Proc_Transacciones.TraceNumber')!;
      const text = `${row.tableOrigin} ${row.fieldOrigin} ${row.status}`;
      expect(text).toContain(item.table);
      expect(text).toContain(item.field);
      expect(text).toContain('Mapeado NACHA');
      expect(text).not.toContain('Sin mapear');
    }
  });

  it('clasifica ANS de Proc_Contrapartidas sin regla activa como opcional reservado', () => {
    selectService(1);

    const text = `${rowText('ANSIDLOTE')} ${rowText('ANCLC')}`;

    expect(text).toContain('Reservado por contrato');
    expect(text).toContain('Opcional / reservado');
    expect(text).not.toContain('Sin mapear');
  });

  it('mantiene Sin mapear cuando no hay regla activa ni fuente valida', () => {
    selectService(1);

    component.mappingSets = [
      {
        ...mappingSets[0],
        rules: mappingSets[0].rules.filter((rule) => rule.parameterId !== 18)
      }
    ];

    const row = component.matrixRows.find((item) => item.parameterSoap === 'OFIDTX')!;
    const text = `${row.tableOrigin} ${row.fieldOrigin} ${row.status}`;

    expect(text).toContain('Sin mapear');
    expect(text).not.toContain('Mapeado transaccional');
  });

  it('prioriza el MappingSet publicado activo sobre borradores existentes', () => {
    selectService(1);

    component.mappingSets = [
      {
        ...mappingSets[0],
        id: '44444444-4444-4444-4444-444444444444',
        name: 'Contrapartidas Draft vacio',
        status: 'Draft',
        version: 0,
        publishedAtUtc: null,
        publishedBy: '',
        rules: []
      },
      mappingSets[0]
    ];
    fixture.detectChanges();

    expect(component.activeMappingSet?.id).toBe('11111111-1111-1111-1111-111111111111');
    expect(component.matrixStats.mapped).toBeGreaterThan(0);
    expect((fixture.nativeElement.textContent as string)).toContain('Publicado activo');
    expect((fixture.nativeElement.textContent as string)).not.toContain('Borrador de trabajo');
  });

  it('usa un borrador solo cuando no existe MappingSet publicado', () => {
    selectService(1);

    component.mappingSets = [
      {
        ...mappingSets[0],
        id: '44444444-4444-4444-4444-444444444444',
        name: 'Contrapartidas Draft unico',
        status: 'Draft',
        version: 0,
        publishedAtUtc: null,
        publishedBy: '',
        rules: []
      }
    ];

    expect(component.activeMappingSet?.id).toBe('44444444-4444-4444-4444-444444444444');
    expect(component.getMappingSetStatusLabel(component.activeMappingSet)).toBe('Borrador de trabajo');
  });

  it('conserva el boton Ver auditoria y carga el historial del mapping activo', () => {
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    const historyButton = buttons.find((button) => button.textContent?.includes('Ver auditoría'));

    expect(historyButton).toBeTruthy();
    historyButton!.click();
    fixture.detectChanges();

    expect(api.getHistory).toHaveBeenCalledWith('22222222-2222-2222-2222-222222222222');
    expect((fixture.nativeElement.textContent as string)).toContain('Auditoría');
  });

  it('mantiene navegacion hacia editor avanzado desde accion secundaria', () => {
    selectService(1);

    const row = component.matrixRows.find((item) => item.parameterSoap === 'OFIDTX')!;
    component.openAdvancedEditor(row);

    expect(router.navigate).toHaveBeenCalledWith(['/integraciones/mappings', 'WSCFAACH.Proc_Contrapartidas', '11111111-1111-1111-1111-111111111111']);
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
