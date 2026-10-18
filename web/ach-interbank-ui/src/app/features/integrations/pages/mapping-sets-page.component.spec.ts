import { HttpErrorResponse } from '@angular/common/http';
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialogHarness } from '@angular/material/dialog/testing';
import { MatInputHarness } from '@angular/material/input/testing';
import { MatRadioButtonHarness } from '@angular/material/radio/testing';
import { MatSelectHarness } from '@angular/material/select/testing';
import { FormGroup } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import {
  IntegrationMappingPreviewResultDto,
  IntegrationMappingSetDto,
  IntegrationMappingWorkspaceDto,
  IntegrationMethodDto
} from '../data-access/integration-api.models';
import { IntegrationMappingApi } from '../data-access/integration-mapping.api';
import { IntegrationWorkspaceApi } from '../data-access/integration-workspace.api';
import { MappingSetsPageComponent } from './mapping-sets-page.component';

describe('MappingSetsPageComponent P6.4', () => {
  let fixture: ComponentFixture<MappingSetsPageComponent>;
  let component: MappingSetsPageComponent;
  let mappingApi: jasmine.SpyObj<IntegrationMappingApi>;
  let workspaceApi: jasmine.SpyObj<IntegrationWorkspaceApi>;
  let router: jasmine.SpyObj<Router>;
  let loader: HarnessLoader;

  const methods: IntegrationMethodDto[] = [
    {
      id: 1,
      code: 'WSCFAACH.Proc_Transacciones',
      displayName: 'Créditos recibidos en CFA',
      soapClientCode: 'WSCFAACH',
      isActive: true,
      integrationKey: 'CFA',
      operationKey: 'Proc_Transacciones',
      mappingDirection: 'Inbound',
      mappingPurpose: 'Datos funcionales requeridos para procesar créditos recibidos.',
      functionalNature: 'Credit',
      functionalOriginator: 'External',
      movesMoney: true
    }
  ];

  const mappingSets: IntegrationMappingSetDto[] = [
    {
      id: '11111111-1111-1111-1111-111111111111',
      methodId: 1,
      methodCode: 'Proc_Transacciones',
      contractVersionId: '22222222-2222-2222-2222-222222222222',
      revision: 8,
      name: 'Configuración de trabajo',
      version: 0,
      status: 'Draft',
      isActive: true,
      notes: '',
      publishedBy: '',
      rules: []
    }
  ];

  const workspace: IntegrationMappingWorkspaceDto = {
    mappingSet: {
      id: mappingSets[0].id,
      name: mappingSets[0].name,
      status: 'Draft',
      revision: 8
    },
    contract: {
      id: '22222222-2222-2222-2222-222222222222',
      versionLabel: 'cfa-2026-11',
      targetNamespace: 'http://tempuri.org/'
    },
    coverage: {
      total: 4,
      configured: 2,
      pendingRequired: 1,
      complete: 1,
      pending: 1,
      invalid: 1,
      notApplicable: 1
    },
    publication: {
      canPublish: false,
      pendingRequired: 1,
      newRequiredPending: 1,
      blockingIssues: 2
    },
    parameters: [
      {
        parameterId: 10,
        functionalName: 'Tipo de cliente',
        functionalDescription: 'Clasificación funcional del cliente.',
        technicalName: 'TIPOCLIENTE',
        required: true,
        dataType: 'string',
        category: 'Cliente',
        status: 'Complete',
        source: 'Referencia de la transacción',
        transformation: 'Recortar',
        currentRule: {
          ruleId: 501,
          sourceCatalogFieldId: 101,
          sourceKind: 'Transaction',
          sourceFieldPath: 'transaction.reference',
          transformationCode: 'Trim',
          formatMask: null,
          priority: 1,
          enabled: true
        },
        technicalDetails: {
          parameterPath: 'TIPOCLIENTE',
          xmlElementName: 'TIPOCLIENTE',
          dataType: 'string',
          xsdType: 'xsd:string',
          targetNamespace: 'http://tempuri.org/',
          sortOrder: 10
        }
      },
      {
        parameterId: 11,
        functionalName: 'Número de referencia',
        functionalDescription: 'Referencia funcional de la transacción.',
        technicalName: 'OFIDTX',
        required: true,
        dataType: 'string',
        category: 'Transacción',
        status: 'Pending',
        source: null,
        transformation: null,
        currentRule: null,
        technicalDetails: {
          parameterPath: 'OFIDTX',
          xmlElementName: 'OFIDTX',
          dataType: 'string',
          xsdType: 'xsd:string',
          targetNamespace: 'http://tempuri.org/',
          sortOrder: 20
        }
      },
      {
        parameterId: 12,
        functionalName: 'Fecha efectiva',
        functionalDescription: 'Fecha operacional que aplica al movimiento.',
        technicalName: 'OFFECHEFEC',
        required: true,
        dataType: 'string',
        category: 'Operación',
        status: 'Invalid',
        source: null,
        transformation: null,
        currentRule: null,
        technicalDetails: {
          parameterPath: 'OFFECHEFEC',
          xmlElementName: 'OFFECHEFEC',
          dataType: 'string',
          xsdType: 'xsd:string',
          targetNamespace: 'http://tempuri.org/',
          sortOrder: 30
        }
      },
      {
        parameterId: 13,
        functionalName: 'Descripción adicional',
        functionalDescription: 'Información opcional complementaria.',
        technicalName: 'ANOBSERVACION',
        required: false,
        dataType: 'string',
        category: 'Información adicional',
        status: 'NotApplicable',
        source: null,
        transformation: null,
        currentRule: null,
        technicalDetails: {
          parameterPath: 'ANOBSERVACION',
          xmlElementName: 'ANOBSERVACION',
          dataType: 'string',
          xsdType: 'xsd:string',
          targetNamespace: 'http://tempuri.org/',
          sortOrder: 40
        }
      }
    ],
    sourceGroups: [
      {
        key: 'Transaction',
        label: 'Transacción',
        fields: [
          {
            id: 101,
            methodId: 1,
            sourceKind: 'Transaction',
            entityName: 'AchTransaction',
            fieldPath: 'transaction.reference',
            displayName: 'Referencia de la transacción',
            dataType: 'string',
            cardinality: 'Scalar',
            nullable: true,
            sortOrder: 1,
            isActive: true
          }
        ]
      },
      {
        key: 'Cycle',
        label: 'Ciclo',
        fields: [
          {
            id: 201,
            methodId: 1,
            sourceKind: 'Cycle',
            entityName: 'AchCycle',
            fieldPath: 'cycle.processingDate',
            displayName: 'Fecha de proceso del ciclo',
            dataType: 'datetime',
            cardinality: 'Scalar',
            nullable: false,
            sortOrder: 2,
            isActive: true
          }
        ]
      }
    ],
    transformations: [
      {
        code: 'Trim',
        displayName: 'Recortar',
        description: 'Elimina espacios al inicio y final.',
        supportsFormatMask: false,
        supportsMultipleSources: false
      },
      {
        code: 'DateFormat',
        displayName: 'Formatear fecha',
        description: 'Formatea una fecha con una máscara aprobada.',
        supportsFormatMask: true,
        supportsMultipleSources: false
      }
    ],
    sourceCompatibility: [
      {
        parameterId: 10,
        sourceCatalogFieldId: 101,
        allowsNoTransformation: true,
        transformations: [
          {
            code: 'Trim',
            displayName: 'Recortar',
            description: 'Elimina espacios al inicio y final.',
            requiresFormatMask: false
          }
        ]
      },
      {
        parameterId: 10,
        sourceCatalogFieldId: 201,
        allowsNoTransformation: true,
        transformations: [
          {
            code: 'DateFormat',
            displayName: 'Formatear fecha',
            description: 'Formatea una fecha con una máscara aprobada.',
            requiresFormatMask: true
          }
        ]
      },
      {
        parameterId: 11,
        sourceCatalogFieldId: 101,
        allowsNoTransformation: true,
        transformations: []
      },
      {
        parameterId: 12,
        sourceCatalogFieldId: 201,
        allowsNoTransformation: false,
        transformations: [
          {
            code: 'DateFormat',
            displayName: 'Formatear fecha',
            description: 'Formatea una fecha con una máscara aprobada.',
            requiresFormatMask: true
          }
        ]
      }
    ]
  };

  const preview: IntegrationMappingPreviewResultDto = {
    mappingSetId: mappingSets[0].id,
    methodId: 1,
    methodCode: 'Proc_Transacciones',
    contextMode: 'real-latest',
    items: [
      {
        parameterId: 10,
        parameterPath: 'TIPOCLIENTE',
        resolvedFrom: 'transaction.reference',
        previewValue: '••••9012',
        sourceSection: 'transaccion',
        resolutionKind: 'source-field',
        appliedTransformation: 'Trim',
        priority: 1,
        enabled: true,
        functionalName: 'Tipo de cliente',
        functionalDescription: 'Clasificación funcional del cliente.',
        required: true,
        sourceDisplayName: 'Referencia de la transacción',
        transformationDisplayName: 'Recortar',
        validationStatus: 'valid',
        validationIssues: [],
        isMasked: true
      },
      {
        parameterId: 11,
        parameterPath: 'OFIDTX',
        resolvedFrom: 'Sin origen configurado',
        previewValue: null,
        sourceSection: 'sin-configurar',
        resolutionKind: 'none',
        appliedTransformation: null,
        priority: 0,
        enabled: false,
        functionalName: 'Número de referencia',
        functionalDescription: 'Referencia funcional de la transacción.',
        required: true,
        sourceDisplayName: null,
        transformationDisplayName: null,
        validationStatus: 'incomplete',
        validationIssues: [
          {
            severity: 'Error',
            code: 'REQUIRED_PARAMETER_MISSING',
            message: 'Required parameter OFIDTX has no enabled mapping rule.',
            path: 'OFIDTX',
            category: 'Coverage'
          }
        ],
        isMasked: false
      },
      {
        parameterId: 12,
        parameterPath: 'OFFECHEFEC',
        resolvedFrom: 'cycle.processingDate',
        previewValue: '2026-10-13',
        sourceSection: 'ciclo-camara',
        resolutionKind: 'source-field',
        appliedTransformation: 'DateFormat',
        priority: 1,
        enabled: true,
        functionalName: 'Fecha efectiva',
        functionalDescription: 'Fecha operacional que aplica al movimiento.',
        required: true,
        sourceDisplayName: 'Fecha de proceso del ciclo',
        transformationDisplayName: 'Formatear fecha',
        validationStatus: 'invalid',
        validationIssues: [
          {
            severity: 'Error',
            code: 'TRANSFORMATION_INVALID',
            message: 'Transformation DateFormat is not valid for the configured rule.',
            path: 'OFFECHEFEC',
            category: 'Transformation'
          }
        ],
        isMasked: false
      },
      {
        parameterId: 13,
        parameterPath: 'ANOBSERVACION',
        resolvedFrom: 'Sin origen configurado',
        previewValue: null,
        sourceSection: 'sin-configurar',
        resolutionKind: 'none',
        appliedTransformation: null,
        priority: 0,
        enabled: false,
        functionalName: 'Descripción adicional',
        functionalDescription: 'Información opcional complementaria.',
        required: false,
        sourceDisplayName: null,
        transformationDisplayName: null,
        validationStatus: 'inactive',
        validationIssues: [],
        isMasked: false
      }
    ],
    payloadPreviewJson: '{"TIPOCLIENTE":"••••9012"}',
    rawPreviewJson: '[{"parameterId":10,"previewValue":"••••9012","isMasked":true}]',
    validation: {
      mappingSetId: mappingSets[0].id,
      isValid: false,
      issues: [
        {
          severity: 'Error',
          code: 'REQUIRED_PARAMETER_MISSING',
          message: 'Required parameter OFIDTX has no enabled mapping rule.',
          path: 'OFIDTX',
          category: 'Coverage'
        },
        {
          severity: 'Error',
          code: 'TRANSFORMATION_INVALID',
          message: 'Transformation DateFormat is not valid for the configured rule.',
          path: 'OFFECHEFEC',
          category: 'Transformation'
        }
      ],
      coverage: {
        totalParameters: 4,
        validParameters: 1,
        incompleteParameters: 1,
        invalidParameters: 1,
        inactiveParameters: 1,
        coveredByDefaultOrFixed: 0,
        coveredBySourceField: 2
      },
      parameters: [
        { parameterId: 10, parameterPath: 'TIPOCLIENTE', required: true, status: 'valid', resolutionKind: 'source-field', hints: [] },
        { parameterId: 11, parameterPath: 'OFIDTX', required: true, status: 'incomplete', resolutionKind: 'none', hints: [] },
        { parameterId: 12, parameterPath: 'OFFECHEFEC', required: true, status: 'invalid', resolutionKind: 'source-field', hints: [] },
        { parameterId: 13, parameterPath: 'ANOBSERVACION', required: false, status: 'inactive', resolutionKind: 'none', hints: [] }
      ]
    }
  };

  async function createComponent(): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [MappingSetsPageComponent, NoopAnimationsModule],
      providers: [
        { provide: IntegrationMappingApi, useValue: mappingApi },
        { provide: IntegrationWorkspaceApi, useValue: workspaceApi },
        {
          provide: AuthService,
          useValue: {
            hasPermission: (permission: string | string[]) => (Array.isArray(permission) ? permission : [permission]).includes('CanManageAch'),
            currentUser: { username: 'qa-user' }
          }
        },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap({ method: 'Proc_Transacciones' }) } }
        },
        { provide: Router, useValue: router }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MappingSetsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    loader = TestbedHarnessEnvironment.documentRootLoader(fixture);
  }

  beforeEach(() => {
    mappingApi = jasmine.createSpyObj<IntegrationMappingApi>('IntegrationMappingApi', [
      'getByMethod',
      'getWorkspace',
      'preview',
      'updateParameterMapping',
      'publishWithRevision'
    ]);
    workspaceApi = jasmine.createSpyObj<IntegrationWorkspaceApi>('IntegrationWorkspaceApi', ['getMethods']);
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    router.navigate.and.resolveTo(true);

    workspaceApi.getMethods.and.returnValue(of(methods));
    mappingApi.getByMethod.and.returnValue(of(mappingSets));
    mappingApi.getWorkspace.and.returnValue(of(workspace));
    mappingApi.preview.and.returnValue(of(preview));
    mappingApi.updateParameterMapping.and.returnValue(of(mappingSets[0]));
    mappingApi.publishWithRevision.and.returnValue(of(mappingSets[0]));
  });

  afterEach(() => TestBed.resetTestingModule());

  it('mantiene la lista funcional, búsqueda, filtros y cobertura del read model backend', async () => {
    await createComponent();

    expect(fixture.nativeElement.querySelectorAll('[data-testid="mapping-parameter-item"]').length).toBe(4);
    expect(fixture.nativeElement.querySelector('[data-testid="coverage-total"]').textContent.trim()).toBe('4');

    const search = fixture.nativeElement.querySelector('[data-testid="mapping-search"]') as HTMLInputElement;
    search.value = 'referencia';
    search.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('[data-testid="mapping-parameter-item"]').length).toBe(1);

    component.filters.controls.search.setValue('');
    component.filters.controls.status.setValue('Invalid');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-parameter-item"]').textContent).toContain('Fecha efectiva');
  });

  it('expone búsqueda, selector de origen y tratamiento guiado mediante Harness Material', async () => {
    await createComponent();
    const configurable = component.parameters().find(parameter => parameter.parameterId === 10)!;
    component.startEdit(configurable);
    fixture.detectChanges();

    const editor = await loader.getChildLoader('#mapping-editor-10');
    const sourceSearch = await editor.getHarness(MatInputHarness);
    await sourceSearch.setValue('Referencia');
    expect(await sourceSearch.getValue()).toBe('Referencia');

    const source = await editor.getHarness(MatRadioButtonHarness.with({ label: /Referencia de la transacción/ }));
    await source.check();
    expect(await source.isChecked()).toBeTrue();
    fixture.detectChanges();

    const treatment = await editor.getHarness(MatSelectHarness);
    await treatment.open();
    await treatment.clickOptions({ text: /Recortar/ });
    expect(await treatment.getValueText()).toContain('Recortar');
  });

  it('usa un Reactive Form tipado y no expone controles para paths libres ni expresiones', async () => {
    await createComponent();
    component.startEdit(component.parameters()[0]);
    fixture.detectChanges();

    expect(component.parameterForm instanceof FormGroup).toBeTrue();
    expect(Object.keys(component.parameterForm.controls)).toEqual([
      'sourceSearch',
      'sourceCatalogFieldId',
      'transformationCode',
      'formatMask'
    ]);
    expect(fixture.nativeElement.querySelector('input[formcontrolname="sourceFieldPath"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('input[formcontrolname="conditionExpression"]')).toBeNull();
  });

  it('presenta el catálogo backend agrupado con nombre funcional primero y detalle técnico secundario', async () => {
    await createComponent();
    component.startEdit(component.parameters()[0]);
    fixture.detectChanges();

    const selector = fixture.nativeElement.querySelector('[data-testid="mapping-source-selector"]');
    const options = fixture.nativeElement.querySelectorAll('[data-testid="mapping-source-option"]');
    expect(selector).not.toBeNull();
    expect(options.length).toBe(2);
    expect(options[0].textContent).toContain('Referencia de la transacción');
    expect(options[0].textContent).toContain('transaction.reference');
    expect(options[0].textContent).toContain('ID 101');
  });

  it('busca orígenes sin perder la agrupación funcional informada por backend', async () => {
    await createComponent();
    component.startEdit(component.parameters()[0]);
    component.parameterForm.controls.sourceSearch.setValue('fecha de proceso');
    fixture.detectChanges();

    const groups = component.filteredSourceGroups();
    expect(groups.length).toBe(1);
    expect(groups[0].label).toBe('Ciclo');
    expect(groups[0].fields.map(field => field.id)).toEqual([201]);
  });

  it('muestra únicamente tratamientos compatibles informados por backend y admite sin tratamiento solo cuando está permitido', async () => {
    await createComponent();
    component.startEdit(component.parameters()[0]);

    component.parameterForm.controls.sourceCatalogFieldId.setValue(201);
    fixture.detectChanges();

    expect(component.compatibleTreatments().map(item => item.code)).toEqual(['DateFormat']);
    expect(component.selectedCompatibility()?.allowsNoTransformation).toBeTrue();

    component.startEdit(component.parameters()[2]);
    component.parameterForm.controls.sourceCatalogFieldId.setValue(201);
    fixture.detectChanges();
    expect(component.selectedCompatibility()?.allowsNoTransformation).toBeFalse();
    expect(component.parameterForm.controls.transformationCode.hasError('required')).toBeTrue();
  });

  it('valida desde metadata backend la configuración adicional requerida por el tratamiento', async () => {
    await createComponent();
    component.startEdit(component.parameters()[2]);
    component.parameterForm.controls.sourceCatalogFieldId.setValue(201);
    component.parameterForm.controls.transformationCode.setValue('DateFormat');
    fixture.detectChanges();

    expect(component.parameterForm.controls.formatMask.hasError('required')).toBeTrue();
    expect(component.canSaveParameter()).toBeFalse();

    component.parameterForm.controls.formatMask.setValue('yyyyMMdd');
    fixture.detectChanges();
    expect(component.parameterForm.controls.formatMask.valid).toBeTrue();
  });

  it('guarda por identificador de catálogo con Revision esperada y sin enviar field paths libres', async () => {
    await createComponent();
    component.startEdit(component.parameters()[0]);
    component.parameterForm.controls.sourceCatalogFieldId.setValue(101);
    component.parameterForm.controls.transformationCode.setValue('Trim');

    component.saveParameter();

    expect(mappingApi.updateParameterMapping).toHaveBeenCalledOnceWith(
      workspace.mappingSet.id,
      10,
      {
        expectedRevision: 8,
        sourceCatalogFieldId: 101,
        transformationCode: 'Trim',
        formatMask: null,
        updatedBy: 'qa-user'
      }
    );
    const payload = mappingApi.updateParameterMapping.calls.mostRecent().args[2] as unknown as Record<string, unknown>;
    expect(payload['sourceFieldPath']).toBeUndefined();
    expect(payload['conditionExpression']).toBeUndefined();
  });

  it('refresca el workspace desde backend después del guardado y no aplica éxito local antes de la respuesta', async () => {
    const save$ = new Subject<IntegrationMappingSetDto>();
    const refreshed: IntegrationMappingWorkspaceDto = {
      ...workspace,
      mappingSet: { ...workspace.mappingSet, revision: 9 },
      parameters: workspace.parameters.map(parameter => parameter.parameterId === 10
        ? {
            ...parameter,
            source: 'Fecha de proceso del ciclo',
            transformation: 'Formatear fecha',
            currentRule: {
              ruleId: 501,
              sourceCatalogFieldId: 201,
              sourceKind: 'Cycle' as const,
              sourceFieldPath: 'cycle.processingDate',
              transformationCode: 'DateFormat',
              formatMask: 'yyyyMMdd',
              priority: 1,
              enabled: true
            }
          }
        : parameter)
    };
    mappingApi.updateParameterMapping.and.returnValue(save$);
    mappingApi.getWorkspace.and.returnValues(of(workspace), of(refreshed));
    await createComponent();

    component.startEdit(component.parameters()[0]);
    component.parameterForm.controls.sourceCatalogFieldId.setValue(201);
    component.parameterForm.controls.transformationCode.setValue('DateFormat');
    component.parameterForm.controls.formatMask.setValue('yyyyMMdd');
    component.saveParameter();
    fixture.detectChanges();

    expect(component.parameters()[0].sourceSummary).toBe('Referencia de la transacción');
    expect(mappingApi.getWorkspace).toHaveBeenCalledTimes(1);

    save$.next(mappingSets[0]);
    save$.complete();
    fixture.detectChanges();

    expect(mappingApi.getWorkspace).toHaveBeenCalledTimes(2);
    expect(component.parameters()[0].sourceSummary).toBe('Fecha de proceso del ciclo');
    expect(component.store.workspace()?.mappingSet.revision).toBe(9);
  });

  it('muestra error o forbidden de guardado sin asumir éxito local', async () => {
    mappingApi.updateParameterMapping.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 403, statusText: 'Forbidden' }))
    );
    await createComponent();

    component.startEdit(component.parameters()[0]);
    component.parameterForm.controls.sourceCatalogFieldId.setValue(101);
    component.parameterForm.controls.transformationCode.setValue('Trim');
    component.saveParameter();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('[data-testid="mapping-save-error"][role="alert"]');
    expect(error).not.toBeNull();
    expect(component.store.saveError()?.kind).toBe('forbidden');
    expect(component.store.saveSucceededParameterId()).toBeNull();
  });

  it('conserva loading, empty, error y forbidden de carga con semántica observable', async () => {
    const loading$ = new Subject<IntegrationMethodDto[]>();
    workspaceApi.getMethods.and.returnValue(loading$);
    await createComponent();
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-loading-state"]')).not.toBeNull();

    TestBed.resetTestingModule();
    workspaceApi.getMethods.and.returnValue(of(methods));
    mappingApi.getByMethod.and.returnValue(of([]));
    await createComponent();
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-empty-state"]')).not.toBeNull();

    TestBed.resetTestingModule();
    mappingApi.getByMethod.and.returnValue(throwError(() => new HttpErrorResponse({ status: 500, statusText: 'Error' })));
    await createComponent();
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-error-state"][role="alert"]')).not.toBeNull();

    TestBed.resetTestingModule();
    mappingApi.getByMethod.and.returnValue(throwError(() => new HttpErrorResponse({ status: 403, statusText: 'Forbidden' })));
    await createComponent();
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-forbidden-state"][role="alert"]')).not.toBeNull();
  });

  it('muestra por campo dato CFA, origen, tratamiento, valor sanitizado y estado provistos por backend', async () => {
    await createComponent();

    const fieldPreviews = fixture.nativeElement.querySelectorAll('[data-testid="mapping-field-preview"]');
    expect(fieldPreviews.length).toBe(4);
    expect(fieldPreviews[0].textContent).toContain('Dato requerido por CFA');
    expect(fieldPreviews[0].textContent).toContain('Tipo de cliente · Requerido');
    expect(fieldPreviews[0].textContent).toContain('Origen del dato');
    expect(fieldPreviews[0].textContent).toContain('Referencia de la transacción');
    expect(fieldPreviews[0].textContent).toContain('Tratamiento del dato');
    expect(fieldPreviews[0].textContent).toContain('Recortar');
    expect(fieldPreviews[0].textContent).toContain('Valor resultante sanitizado');
    expect(fieldPreviews[0].textContent).toContain('••••9012');
    expect(fieldPreviews[0].textContent).toContain('Listo');
  });

  it('no reconstruye PII completa en frontend y presenta únicamente el valor sanitizado recibido', async () => {
    await createComponent();

    const pageText = fixture.nativeElement.textContent as string;
    expect(pageText).toContain('••••9012');
    expect(pageText).not.toContain('123456789012');
    expect(fixture.nativeElement.querySelector('.integration-information__protected-badge')?.textContent).toContain('Dato protegido');
  });

  it('humaniza errores backend y conserva código, ruta y mensaje técnico en detalle colapsado', async () => {
    await createComponent();

    const fieldPreviews = fixture.nativeElement.querySelectorAll('[data-testid="mapping-field-preview"]');
    const pendingPreview = fieldPreviews[1] as HTMLElement;
    expect(pendingPreview.textContent).toContain('Número de referencia: Este dato requerido por CFA todavía no tiene un origen configurado.');

    const technical = pendingPreview.querySelector('[data-testid="mapping-field-validation-technical-detail"]') as HTMLDetailsElement;
    expect(technical.open).toBeFalse();
    expect(technical.textContent).toContain('REQUIRED_PARAMETER_MISSING');
    expect(technical.textContent).toContain('OFIDTX');
    expect(technical.textContent).toContain('Required parameter OFIDTX has no enabled mapping rule.');
    expect(technical.querySelector('summary')?.tagName).toBe('SUMMARY');
  });

  it('mantiene loading, error y forbidden de preview sin ocultar el workspace', async () => {
    const previewLoading$ = new Subject<IntegrationMappingPreviewResultDto>();
    mappingApi.preview.and.returnValue(previewLoading$);
    await createComponent();
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-preview-loading-state"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-parameter-list"]')).not.toBeNull();

    TestBed.resetTestingModule();
    mappingApi.preview.and.returnValue(throwError(() => new HttpErrorResponse({ status: 500, statusText: 'Error' })));
    await createComponent();
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-preview-error-state"][role="alert"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-parameter-list"]')).not.toBeNull();

    TestBed.resetTestingModule();
    mappingApi.preview.and.returnValue(throwError(() => new HttpErrorResponse({ status: 403, statusText: 'Forbidden' })));
    await createComponent();
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-preview-forbidden-state"][role="alert"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-parameter-list"]')).not.toBeNull();
  });

  it('muestra conflicto de Revision, conserva detalle técnico y recarga el estado vigente sin merge', async () => {
    const refreshed: IntegrationMappingWorkspaceDto = {
      ...workspace,
      mappingSet: { ...workspace.mappingSet, revision: 9 }
    };
    mappingApi.getWorkspace.and.returnValues(of(workspace), of(refreshed));
    mappingApi.updateParameterMapping.and.returnValue(
      throwError(() => new HttpErrorResponse({
        status: 409,
        error: {
          detail: 'La configuración cambió. Revisión esperada=8, actual=9.',
          expectedRevision: 8,
          currentRevision: 9
        }
      }))
    );
    await createComponent();
    component.startEdit(component.parameters()[0]);
    component.parameterForm.controls.sourceCatalogFieldId.setValue(101);
    component.parameterForm.controls.transformationCode.setValue('Trim');
    component.saveParameter();
    fixture.detectChanges();

    const conflict = fixture.nativeElement.querySelector('[data-testid="mapping-revision-conflict"][role="alert"]') as HTMLElement;
    const technical = conflict.querySelector('[data-testid="mapping-revision-technical-detail"]') as HTMLDetailsElement;
    expect(conflict.textContent).toContain('No se hizo una mezcla automática');
    expect(technical.open).toBeFalse();
    expect(technical.textContent).toContain('Revision esperada');
    expect(technical.textContent).toContain('Revision vigente');
    expect(component.store.workspace()?.mappingSet.revision).toBe(8);

    (conflict.querySelector('[data-testid="mapping-revision-reload"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(mappingApi.getWorkspace).toHaveBeenCalledTimes(2);
    expect(component.store.workspace()?.mappingSet.revision).toBe(9);
    expect(component.editingParameterId()).toBeNull();
  });

  it('representa el escenario contract-driven de un campo scalar nuevo como 1 dato nuevo pendiente y bloquea publicación', async () => {
    await createComponent();

    const summary = fixture.nativeElement.querySelector('[data-testid="mapping-publication-pending-summary"]') as HTMLElement;
    const cta = fixture.nativeElement.querySelector('[data-testid="mapping-publication-cta"]') as HTMLButtonElement;
    expect(summary.textContent.trim()).toBe('1 dato nuevo pendiente');
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-publication-required-pending"]').textContent).toContain('Número de referencia');
    expect(cta.disabled).toBeTrue();

    component.store.publish('qa-user');
    expect(mappingApi.publishWithRevision).not.toHaveBeenCalled();
  });

  it('abre MatDialog humanizado con servicio, versión, cobertura y confirmación deshabilitada cuando backend bloquea', async () => {
    await createComponent();
    const review = fixture.nativeElement.querySelector('[data-testid="mapping-publication-review"]') as HTMLButtonElement;
    review.focus();
    review.click();
    fixture.detectChanges();

    const dialog = await loader.getHarness(MatDialogHarness);
    expect((document.activeElement?.textContent ?? '').trim()).toContain('Publicar configuración CFA');
    const text = await dialog.getText();
    expect(text).toContain('Créditos recibidos en CFA');
    expect(text).toContain('cfa-2026-11');
    expect(text).toContain('1 dato nuevo pendiente');
    const confirm = document.querySelector('[data-testid="mapping-publication-confirm"]') as HTMLButtonElement;
    expect(confirm.disabled).toBeTrue();

    await dialog.close();
    await fixture.whenStable();
    expect(document.activeElement).toBe(review);
  });

  it('impide bypass de CTA y diálogo si backend reporta required pendiente aunque canPublish sea true', async () => {
    const inconsistent: IntegrationMappingWorkspaceDto = {
      ...workspace,
      publication: { canPublish: true, pendingRequired: 1, newRequiredPending: 1, blockingIssues: 1 }
    };
    mappingApi.getWorkspace.and.returnValue(of(inconsistent));
    await createComponent();

    const cta = fixture.nativeElement.querySelector('[data-testid="mapping-publication-cta"]') as HTMLButtonElement;
    expect(cta.disabled).toBeTrue();

    const review = fixture.nativeElement.querySelector('[data-testid="mapping-publication-review"]') as HTMLButtonElement;
    review.click();
    fixture.detectChanges();
    await loader.getHarness(MatDialogHarness);

    const confirm = document.querySelector('[data-testid="mapping-publication-confirm"]') as HTMLButtonElement;
    expect(confirm.disabled).toBeTrue();
    component.store.publish('qa-user');
    expect(mappingApi.publishWithRevision).not.toHaveBeenCalled();
  });

  it('publica solo tras confirmación, envía Revision esperada y refresca workspace después de respuesta exitosa', async () => {
    const eligible: IntegrationMappingWorkspaceDto = {
      ...workspace,
      coverage: { total: 4, configured: 4, pendingRequired: 0, complete: 4, pending: 0, invalid: 0, notApplicable: 0 },
      publication: { canPublish: true, pendingRequired: 0, newRequiredPending: 0, blockingIssues: 0 },
      parameters: workspace.parameters.map(parameter => ({ ...parameter, status: 'Complete' as const }))
    };
    const publishedWorkspace: IntegrationMappingWorkspaceDto = {
      ...eligible,
      mappingSet: { ...eligible.mappingSet, status: 'Published' as const, revision: 9 },
      publication: { ...eligible.publication, canPublish: false }
    };
    const publish$ = new Subject<IntegrationMappingSetDto>();
    mappingApi.getWorkspace.and.returnValues(of(eligible), of(publishedWorkspace));
    mappingApi.publishWithRevision.and.returnValue(publish$);
    await createComponent();

    const cta = fixture.nativeElement.querySelector('[data-testid="mapping-publication-cta"]') as HTMLButtonElement;
    expect(cta.disabled).toBeFalse();
    cta.click();
    fixture.detectChanges();
    const dialog = await loader.getHarness(MatDialogHarness);
    (document.querySelector('[data-testid="mapping-publication-confirm"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(mappingApi.publishWithRevision).toHaveBeenCalledOnceWith(eligible.mappingSet.id, {
      expectedRevision: 8,
      publishedBy: 'qa-user',
      publishNote: null
    });
    expect(mappingApi.getWorkspace).toHaveBeenCalledTimes(1);
    expect(component.store.publicationSucceeded()).toBeFalse();

    publish$.next({ ...mappingSets[0], status: 'Published', revision: 9 });
    publish$.complete();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(mappingApi.getWorkspace).toHaveBeenCalledTimes(2);
    expect(component.store.workspace()?.mappingSet.status).toBe('Published');
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-publication-success"]')).not.toBeNull();
    expect(document.querySelector('[data-testid="mapping-publication-dialog"]')).toBeNull();
  });

  it('muestra conflicto de publicación y no asume éxito optimista', async () => {
    const eligible: IntegrationMappingWorkspaceDto = {
      ...workspace,
      publication: { canPublish: true, pendingRequired: 0, newRequiredPending: 0, blockingIssues: 0 },
      coverage: { ...workspace.coverage, pendingRequired: 0 }
    };
    mappingApi.getWorkspace.and.returnValue(of(eligible));
    mappingApi.publishWithRevision.and.returnValue(
      throwError(() => new HttpErrorResponse({
        status: 409,
        error: { detail: 'publish conflict', expectedRevision: 8, currentRevision: 10 }
      }))
    );
    await createComponent();
    component.publishConfiguration();
    fixture.detectChanges();
    (document.querySelector('[data-testid="mapping-publication-confirm"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const conflict = fixture.nativeElement.querySelector('[data-testid="mapping-revision-conflict"]') as HTMLElement;
    expect(conflict.textContent).toContain('actualizó esta configuración antes de publicarla');
    expect(component.store.publicationSucceeded()).toBeFalse();
    expect(component.store.workspace()?.mappingSet.revision).toBe(8);
  });

  it('mantiene detalle técnico colapsado y controles de edición navegables por teclado', async () => {
    await createComponent();
    component.startEdit(component.parameters()[0]);
    fixture.detectChanges();

    const details = fixture.nativeElement.querySelector('[data-testid="mapping-technical-detail"]') as HTMLDetailsElement;
    const search = fixture.nativeElement.querySelector('[data-testid="mapping-source-search"]') as HTMLInputElement;
    const radios = fixture.nativeElement.querySelectorAll('[data-testid="mapping-source-option"] input[type="radio"]');
    const editButton = fixture.nativeElement.querySelector('[data-testid="mapping-edit-button"]');
    const previewTechnical = fixture.nativeElement.querySelector('[data-testid="mapping-validation-technical-detail"]') as HTMLDetailsElement;
    const previewRefresh = fixture.nativeElement.querySelector('[data-testid="mapping-preview-refresh"]') as HTMLButtonElement;

    expect(details.open).toBeFalse();
    expect(previewTechnical.open).toBeFalse();
    expect(previewTechnical.querySelector('summary')?.tabIndex).toBeGreaterThanOrEqual(0);
    expect(previewRefresh.tagName).toBe('BUTTON');
    expect(details.querySelector('summary')?.tagName).toBe('SUMMARY');
    expect(search.getAttribute('type')).toBe('search');
    expect(radios.length).toBeGreaterThan(0);
    expect(editButton.getAttribute('aria-expanded')).toBe('true');
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-parameter-editor"] form')).toBeNull();
  });
  it('usa cards/listas como interacción primaria y mantiene regiones dinámicas anunciables', async () => {
    await createComponent();

    expect(fixture.nativeElement.querySelector('table')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-result-count"][aria-live="polite"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="mapping-general-validation"][aria-live="polite"]')).not.toBeNull();
  });

});
