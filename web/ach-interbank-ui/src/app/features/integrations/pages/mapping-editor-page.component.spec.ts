import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import {
  IntegrationMappingAdminService,
  IntegrationMappingSet,
  IntegrationMethodParameter,
  IntegrationSourceCatalogField
} from '../../../core/services/integration-mapping-admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { MappingEditorPageComponent } from './mapping-editor-page.component';

describe('MappingEditorPageComponent', () => {
  const mappingSetId = 'dc1b034b-4de3-4043-93cc-79072bf8a5e9';
  const methodCode = 'WSCFAACH.Proc_Transacciones';

  function buildMappingSet(overrides: Partial<IntegrationMappingSet> = {}): IntegrationMappingSet {
    return {
      id: mappingSetId,
      methodId: 2,
      methodCode,
      name: 'ProcTransacciones Published NACHA desagregado',
      version: 1,
      status: 'Published',
      isActive: true,
      notes: 'Mapping UAT/local de referencia.',
      publishedBy: 'seed',
      rules: [
        {
          id: 71,
          mappingSetId,
          methodId: 2,
          parameterId: 24,
          sourceKind: 'EntryDetail',
          sourceCatalogFieldId: 100,
          sourceFieldPath: 'EntryDetails.TraceNumber',
          priority: 1,
          enabled: true
        }
      ],
      ...overrides
    };
  }

  const parameters: IntegrationMethodParameter[] = [
    {
      id: 23,
      methodId: 2,
      parameterPath: 'TREG',
      displayName: 'Tipo de registro',
      descriptionEs: 'Tipo de registro ACH transacción.',
      exampleValue: '6',
      uiHelpText: 'Tipo de registro según layout ACH.',
      category: 'Entrada transacción',
      dataType: 'string',
      direction: 'Input',
      cardinality: 'Scalar',
      required: true,
      sortOrder: 1,
      isActive: true
    },
    {
      id: 24,
      methodId: 2,
      parameterPath: 'TIPTRAN',
      displayName: 'Trace destino',
      descriptionEs: 'Campo destino SOAP/XML para transacción.',
      exampleValue: '22',
      uiHelpText: 'Código según tabla operativa.',
      category: 'Entrada transacción',
      dataType: 'string',
      direction: 'Input',
      cardinality: 'Scalar',
      required: true,
      sortOrder: 2,
      isActive: true
    }
  ];

  const sourceCatalog: IntegrationSourceCatalogField[] = [
    {
      id: 98,
      methodId: 2,
      sourceKind: 'NachaHeader',
      entityName: 'NachaHeaders',
      fieldPath: 'NachaHeaders.ImmediateOrigin',
      displayName: 'ImmediateOrigin',
      dataType: 'string',
      cardinality: 'Scalar',
      nullable: false,
      sortOrder: 1,
      isActive: true
    },
    {
      id: 99,
      methodId: 2,
      sourceKind: 'BatchHeader',
      entityName: 'BatchHeaders',
      fieldPath: 'BatchHeaders.CompanyIdentification',
      displayName: 'CompanyIdentification',
      dataType: 'string',
      cardinality: 'Scalar',
      nullable: false,
      sortOrder: 2,
      isActive: true
    },
    {
      id: 100,
      methodId: 2,
      sourceKind: 'EntryDetail',
      entityName: 'EntryDetails',
      fieldPath: 'EntryDetails.TraceNumber',
      displayName: 'TraceNumber',
      dataType: 'string',
      cardinality: 'Scalar',
      nullable: false,
      sortOrder: 3,
      isActive: true
    }
  ];

  function createApiMock(overrides: Partial<Record<keyof IntegrationMappingAdminService, jasmine.Spy>> = {}) {
    const apiMock = {
      getMappingSetById: jasmine.createSpy().and.returnValue(of(buildMappingSet())),
      getMethodParameters: jasmine.createSpy().and.returnValue(of(parameters)),
      getSourceCatalog: jasmine.createSpy().and.returnValue(of(sourceCatalog)),
      getTransformations: jasmine.createSpy().and.returnValue(of([])),
      getHistory: jasmine.createSpy().and.returnValue(of([])),
      upsertRules: jasmine.createSpy().and.returnValue(of(buildMappingSet())),
      validate: jasmine.createSpy().and.returnValue(
        of({
          mappingSetId,
          isValid: true,
          issues: [],
          coverage: {
            totalParameters: 2,
            validParameters: 2,
            incompleteParameters: 0,
            invalidParameters: 0,
            inactiveParameters: 0,
            coveredByDefaultOrFixed: 1,
            coveredBySourceField: 1
          },
          parameters: [
            { parameterId: 23, parameterPath: 'TREG', required: true, status: 'valid', resolutionKind: 'default-fixed', hints: ['ok'] },
            { parameterId: 24, parameterPath: 'TIPTRAN', required: true, status: 'valid', resolutionKind: 'source-field', hints: ['ok'] }
          ]
        })
      ),
      preview: jasmine.createSpy().and.returnValue(
        of({ mappingSetId, methodId: 2, methodCode, contextMode: 'controlled-sample', items: [], payloadPreviewJson: '{}', rawPreviewJson: '[]' })
      ),
      publish: jasmine.createSpy().and.returnValue(of(buildMappingSet({ status: 'Published', version: 2 }))),
      clone: jasmine.createSpy().and.returnValue(of(buildMappingSet({ id: 'set-2', name: 'Clone' }))),
      ...overrides
    } as any;

    return apiMock;
  }

  async function setup(options?: {
    apiOverrides?: Partial<Record<keyof IntegrationMappingAdminService, jasmine.Spy>>;
    routeParams?: Record<string, string>;
  }): Promise<{ fixture: ComponentFixture<MappingEditorPageComponent>; component: MappingEditorPageComponent; apiMock: any }> {
    const apiMock = createApiMock(options?.apiOverrides);

    await TestBed.configureTestingModule({
      imports: [FormsModule, ReactiveFormsModule, MappingEditorPageComponent],
      providers: [
        { provide: IntegrationMappingAdminService, useValue: apiMock },
        { provide: NotificationService, useValue: { success: jasmine.createSpy(), error: jasmine.createSpy() } },
        { provide: Router, useValue: { navigate: jasmine.createSpy() } },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(
              convertToParamMap({
                mappingSetId,
                methodCode,
                ...(options?.routeParams ?? {})
              })
            )
          }
        }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(MappingEditorPageComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    return { fixture, component, apiMock };
  }

  afterEach(() => TestBed.resetTestingModule());

  it('MappingEditor_ShouldLoadExistingMappingSet', async () => {
    const { component, apiMock } = await setup();

    expect(component.mappingSet?.id).toBe(mappingSetId);
    expect(apiMock.getMappingSetById).toHaveBeenCalledWith(mappingSetId);
  });

  it('MappingEditor_ShouldClearLoading_WhenAllCatalogsLoad', async () => {
    const { component } = await setup();

    expect(component.loading).toBeFalse();
    expect(component.viewState).toBe('ready');
  });

  it('MappingEditor_ShouldShowError_WhenMappingSetNotFound', async () => {
    const { component, fixture } = await setup({
      apiOverrides: {
        getMappingSetById: jasmine.createSpy().and.returnValue(throwError(() => ({ status: 404 })))
      }
    });
    fixture.detectChanges();

    expect(component.loading).toBeFalse();
    expect(component.viewState).toBe('error');
    expect(component.errorMessage).toContain('mapping set solicitado');
  });

  it('MappingEditor_ShouldShowError_WhenSourceCatalogFails', async () => {
    const { component } = await setup({
      apiOverrides: {
        getSourceCatalog: jasmine.createSpy().and.returnValue(throwError(() => ({ status: 500 })))
      }
    });

    expect(component.loading).toBeFalse();
    expect(component.viewState).toBe('error');
    expect(component.errorMessage).toContain('catálogo controlado de campos origen');
  });

  it('MappingEditor_ShouldShowError_WhenTargetCatalogFails', async () => {
    const { component } = await setup({
      apiOverrides: {
        getMethodParameters: jasmine.createSpy().and.returnValue(throwError(() => ({ status: 500 })))
      }
    });

    expect(component.loading).toBeFalse();
    expect(component.viewState).toBe('error');
    expect(component.errorMessage).toContain('campos destino SOAP/XML');
  });

  it('MappingEditor_ShouldNotKeepSpinnerForever_OnHttpError', async () => {
    const { fixture } = await setup({
      apiOverrides: {
        getTransformations: jasmine.createSpy().and.returnValue(throwError(() => ({ status: 503 })))
      }
    });
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).not.toContain('Cargando editor funcional');
    expect(text).toContain('No se pudo abrir el editor');
    expect(text).toContain('Reintentar');
    expect(text).toContain('Volver al listado');
  });

  it('MappingEditor_ShouldParseMappingKey_WithDot', async () => {
    const { component } = await setup();

    expect(component.methodCode).toBe('WSCFAACH.Proc_Transacciones');
    expect(component.viewState).toBe('ready');
  });

  it('MappingEditor_ShouldShowNachaSources_ForProcTransacciones', async () => {
    const { fixture } = await setup();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Archivo NACHA');
    expect(text).toContain('Lote NACHA');
    expect(text).toContain('Detalle NACHA');
  });

  it('MappingEditor_ShouldKeepSourceFieldPathReadonly', async () => {
    const { component, fixture } = await setup();
    component.ruleForm.patchValue({ sourceKind: 'EntryDetail' });
    fixture.detectChanges();

    const pathInput = fixture.nativeElement.querySelector('[data-testid="source-field-path-readonly"]') as HTMLInputElement;
    expect(pathInput.readOnly).toBeTrue();
  });

  it('MappingEditor_ShouldRenderRetryAction_OnFailure', async () => {
    const { fixture } = await setup({
      apiOverrides: {
        getMappingSetById: jasmine.createSpy().and.returnValue(throwError(() => ({ status: 404 })))
      }
    });
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Reintentar');
    expect(text).toContain('Volver al listado');
  });

  it('should save rule using source path derived from controlled catalog', async () => {
    const { component, apiMock } = await setup();
    component.ruleForm.patchValue({
      sourceKind: 'EntryDetail',
      sourceCatalogFieldId: 100,
      sourceFieldPath: 'malicious.sql.free.path'
    });

    component.saveRule();

    const payload = apiMock.upsertRules.calls.mostRecent().args[2][0];
    expect(payload.sourceFieldPath).toBe('EntryDetails.TraceNumber');
  });
});
