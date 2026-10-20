import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { BehaviorSubject, Subject, of, throwError } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { UserSession } from '../../../core/models/auth.models';
import { IntegrationContractApi } from '../data-access/integration-contract.api';
import {
  IntegrationContractArtifactAnalysisDto,
  IntegrationContractArtifactPreviewDto,
  IntegrationContractVersionDetailDto,
  IntegrationContractVersionDto,
  IntegrationMappingSetDto,
  IntegrationMethodDto
} from '../data-access/integration-api.models';
import { IntegrationMappingApi } from '../data-access/integration-mapping.api';
import { IntegrationWorkspaceApi } from '../data-access/integration-workspace.api';
import { ContractWorkspaceStore } from './contract-workspace.store';

describe('ContractWorkspaceStore', () => {
  let contractApi: jasmine.SpyObj<IntegrationContractApi>;
  let mappingApi: jasmine.SpyObj<IntegrationMappingApi>;
  let workspaceApi: jasmine.SpyObj<IntegrationWorkspaceApi>;
  let user$: BehaviorSubject<UserSession | null>;

  const method: IntegrationMethodDto = {
    id: 3,
    code: 'PROC_CONTRAPARTIDAS',
    displayName: 'Débitos originados en CFA',
    soapClientCode: 'WSCFAACH',
    isActive: true,
    integrationKey: 'CFA',
    operationKey: 'Proc_Contrapartidas',
    mappingDirection: 'Outbound',
    mappingPurpose: 'Origina débitos hacia CFA.',
    functionalNature: 'Debit',
    functionalOriginator: 'Internal',
    movesMoney: true
  };

  beforeEach(() => {
    contractApi = jasmine.createSpyObj<IntegrationContractApi>('IntegrationContractApi', [
      'getByMethod',
      'getById',
      'updateParameterPresentation',
      'analyze',
      'preview',
      'import'
    ]);
    mappingApi = jasmine.createSpyObj<IntegrationMappingApi>('IntegrationMappingApi', ['getByMethod']);
    workspaceApi = jasmine.createSpyObj<IntegrationWorkspaceApi>('IntegrationWorkspaceApi', ['getMethods']);
    user$ = new BehaviorSubject<UserSession | null>(session(['CanReadAch', 'CanManageAch']));

    contractApi.getByMethod.and.returnValue(of([]));
    mappingApi.getByMethod.and.returnValue(of([]));
    workspaceApi.getMethods.and.returnValue(of([method]));

    const auth = {
      user$: user$.asObservable(),
      getCurrentUser: () => user$.value
    } as AuthService;

    TestBed.configureTestingModule({
      providers: [
        ContractWorkspaceStore,
        { provide: IntegrationContractApi, useValue: contractApi },
        { provide: IntegrationMappingApi, useValue: mappingApi },
        { provide: IntegrationWorkspaceApi, useValue: workspaceApi },
        { provide: AuthService, useValue: auth }
      ]
    });
  });

  it('trata colección contractual vacía como empty', () => {
    const store = TestBed.inject(ContractWorkspaceStore);

    store.loadVersions(3);

    expect(store.methodId()).toBe(3);
    expect(store.listStatus()).toBe('empty');
    expect(store.activeContract()).toBeNull();
  });

  it('resuelve el servicio por operationKey y conserva el estado de relación entregado por backend', () => {
    const draft = contract('draft', 'v2', 'Draft', false, true);
    contractApi.getByMethod.and.returnValue(of([draft]));
    mappingApi.getByMethod.and.returnValue(of([mapping(draft.id, 'Draft')]));
    const store = TestBed.inject(ContractWorkspaceStore);

    store.loadForOperation('Proc_Contrapartidas');

    expect(store.method()?.displayName).toBe('Débitos originados en CFA');
    expect(store.listStatus()).toBe('ready');
    expect(store.versionRows()[0].relationship.label).toBe('Relación en borrador');
    expect(store.versionRows()[0].canEditFunctionalMetadata).toBeTrue();
  });

  it('ordena parámetros activos y conserva metadata funcional/técnica separada', () => {
    const detail = contractDetail(contract('active', 'v1', 'Active', true, false));
    contractApi.getById.and.returnValue(of(detail));
    const store = TestBed.inject(ContractWorkspaceStore);

    store.loadDetail(detail.contract.id);

    expect(store.detailStatus()).toBe('ready');
    expect(store.parameters().map((item) => item.technicalName)).toEqual(['A', 'B']);
    expect(store.parameters()[0].functionalName).toBe('Cuenta destino');
    expect(store.parameters()[0].uiHelpText).toBe('Use el dato funcional autorizado.');
    expect(store.parameters()[0].technicalDetails.xsdType).toBe('xsd:string');
  });

  it('edita Draft cuando backend y el fallback CanManageAch lo permiten', () => {
    const draft = contract('draft', 'v2', 'Draft', false, true, 5);
    const detail = contractDetail(draft);
    // The fixture is deliberately unordered: parameter B precedes A in the
    // backend array, while the read model sorts by sortOrder (A, B).
    const accountParameter = detail.parameters.find((parameter) => parameter.parameterPath === 'A')!;
    contractApi.getById.and.returnValue(of(detail));
    contractApi.updateParameterPresentation.and.returnValue(of({
      contractVersionId: draft.id,
      contractRevision: 6,
      parameter: { ...accountParameter, displayName: 'Cuenta beneficiaria' }
    }));
    const store = TestBed.inject(ContractWorkspaceStore);
    store.loadDetail(draft.id);

    store.saveParameterPresentation(accountParameter.id, {
      displayName: 'Cuenta beneficiaria',
      descriptionEs: 'Cuenta que recibirá la operación.',
      uiHelpText: 'Seleccione la cuenta autorizada.',
      category: 'Cuenta',
      exampleValue: '****1234'
    });

    expect(contractApi.updateParameterPresentation).toHaveBeenCalledOnceWith(
      draft.id,
      accountParameter.id,
      jasmine.objectContaining({ expectedRevision: 5, actor: 'operador' })
    );
    expect(store.detail()?.contract.revision).toBe(6);
    expect(store.parameters()[0].functionalName).toBe('Cuenta beneficiaria');
    expect(store.parameterSaveStatus()).toBe('ready');
  });

  it('acepta Integrations.ManageContracts como capacidad fina de gestión sin sustituir una capacidad de lectura', () => {
    user$.next(session(['CanReadAch', 'Integrations.ManageContracts']));
    const draft = contract('draft-fine', 'v3', 'Draft', false, true, 7);
    const detail = contractDetail(draft);
    contractApi.getById.and.returnValue(of(detail));
    contractApi.updateParameterPresentation.and.returnValue(of({
      contractVersionId: draft.id,
      contractRevision: 8,
      parameter: { ...detail.parameters[0], displayName: 'Nombre fino' }
    }));
    const store = TestBed.inject(ContractWorkspaceStore);

    store.loadDetail(draft.id);
    store.saveParameterPresentation(detail.parameters[0].id, {
      displayName: 'Nombre fino',
      descriptionEs: '',
      uiHelpText: '',
      category: '',
      exampleValue: ''
    });

    expect(store.permissions()).toEqual({ canRead: true, canManage: true });
    expect(contractApi.updateParameterPresentation).toHaveBeenCalled();
  });

  it('acepta Integrations.Read como lectura semántica sin habilitar gestión', () => {
    user$.next(session(['Integrations.Read']));
    workspaceApi.getMethods.and.returnValue(of([method]));
    const store = TestBed.inject(ContractWorkspaceStore);

    store.loadForOperation('Proc_Contrapartidas');

    expect(store.permissions()).toEqual({ canRead: true, canManage: false });
    expect(workspaceApi.getMethods).toHaveBeenCalled();
  });

  it('no permite que Integrations.ManageContracts por sí sola sustituya la capacidad de lectura', () => {
    user$.next(session(['Integrations.ManageContracts']));
    const store = TestBed.inject(ContractWorkspaceStore);

    store.loadForOperation('Proc_Contrapartidas');

    expect(store.permissions()).toEqual({ canRead: false, canManage: true });
    expect(store.listStatus()).toBe('forbidden');
    expect(workspaceApi.getMethods).not.toHaveBeenCalled();
  });

  it('bloquea edición si backend marca solo lectura aunque el usuario pueda administrar', () => {
    const active = contract('active', 'v1', 'Active', true, false);
    contractApi.getById.and.returnValue(of(contractDetail(active)));
    const store = TestBed.inject(ContractWorkspaceStore);
    store.loadDetail(active.id);

    store.saveParameterPresentation(1, {
      displayName: 'No permitido',
      descriptionEs: '',
      uiHelpText: '',
      category: '',
      exampleValue: ''
    });

    expect(contractApi.updateParameterPresentation).not.toHaveBeenCalled();
    expect(store.parameterSaveStatus()).toBe('forbidden');
  });

  it('bloquea edición Draft sin Integrations.ManageContracts ni CanManageAch aunque backend permita editar', () => {
    user$.next(session(['CanReadAch']));
    const draft = contract('draft', 'v2', 'Draft', false, true);
    contractApi.getById.and.returnValue(of(contractDetail(draft)));
    const store = TestBed.inject(ContractWorkspaceStore);
    store.loadDetail(draft.id);

    store.saveParameterPresentation(1, {
      displayName: 'No permitido',
      descriptionEs: '',
      uiHelpText: '',
      category: '',
      exampleValue: ''
    });

    expect(contractApi.updateParameterPresentation).not.toHaveBeenCalled();
    expect(store.parameterSaveStatus()).toBe('forbidden');
  });

  it('mantiene loading mientras backend no responde', () => {
    const pending = new Subject<IntegrationContractVersionDto[]>();
    contractApi.getByMethod.and.returnValue(pending.asObservable());
    const store = TestBed.inject(ContractWorkspaceStore);

    store.loadVersions(3);

    expect(store.listStatus()).toBe('loading');
    pending.next([]);
    pending.complete();
    expect(store.listStatus()).toBe('empty');
  });

  it('expone error backend sin reemplazarlo por reglas locales', () => {
    contractApi.getByMethod.and.returnValue(throwError(() => new HttpErrorResponse({ status: 500, error: { detail: 'Fallo contractual' } })));
    const store = TestBed.inject(ContractWorkspaceStore);

    store.loadVersions(3);

    expect(store.listStatus()).toBe('error');
    expect(store.listError()?.message).toBe('Fallo contractual');
  });

  it('entra en forbidden sin llamadas de red cuando faltan Integrations.Read y CanReadAch', () => {
    user$.next(session([]));
    const store = TestBed.inject(ContractWorkspaceStore);

    store.loadForOperation('Proc_Contrapartidas');

    expect(store.listStatus()).toBe('forbidden');
    expect(workspaceApi.getMethods).not.toHaveBeenCalled();
    expect(contractApi.getByMethod).not.toHaveBeenCalled();
  });


  it('conserva las operaciones compatibles y los errores de análisis exactamente como los entrega backend', () => {
    const analysis: IntegrationContractArtifactAnalysisDto = {
      fileName: 'multiple.wsdl', artifactKind: 'Wsdl11' as const, sizeBytes: 100, sha256: 'hash',
      operationName: null, targetNamespace: 'urn:test', soapAction: null,
      availableOperations: ['OperacionA', 'OperacionB'], importable: false,
      issues: [{ code: 'WSDL_OPERATION_AMBIGUOUS', message: 'Seleccione una operación.' }], parameters: []
    };
    contractApi.analyze.and.returnValue(of(analysis));
    const store = TestBed.inject(ContractWorkspaceStore);
    store.selectArtifact(new File(['wsdl'], 'multiple.wsdl'));

    store.analyzeArtifact();

    expect(store.artifactAnalysis()).toEqual(analysis);
    expect(store.artifactAnalysis()?.availableOperations).toEqual(['OperacionA', 'OperacionB']);
    expect(store.artifactAnalysis()?.issues[0].message).toBe('Seleccione una operación.');
  });

  it('mantiene MappingRequired y RequiresDevelopment del diff backend sin reclasificación local', () => {
    const active = contract('active', 'v1', 'Active', true, false);
    contractApi.getByMethod.and.returnValue(of([active]));
    mappingApi.getByMethod.and.returnValue(of([]));
    const analysis: IntegrationContractArtifactAnalysisDto = {
      fileName: 'valid.wsdl', artifactKind: 'Wsdl11' as const, sizeBytes: 100, sha256: 'hash',
      operationName: method.operationKey, targetNamespace: 'urn:test', soapAction: 'urn:test/action',
      availableOperations: [method.operationKey], importable: true, issues: [], parameters: []
    };
    contractApi.analyze.and.returnValue(of(analysis));
    const preview: IntegrationContractArtifactPreviewDto = {
      analysis,
      diff: {
        leftContractVersionId: active.id, rightContractVersionId: '00000000-0000-0000-0000-000000000000', methodId: method.id,
        summary: { added: 1, removed: 0, modified: 1, requiresMapping: 1, requiresDevelopment: 1 },
        changes: [
          { changeType: 'Added', technicalName: 'NUEVO', functionalName: 'Nuevo', impact: 'MappingRequired' },
          { changeType: 'NamespaceChanged', technicalName: '$contract', functionalName: 'Namespace SOAP', impact: 'RequiresDevelopment' }
        ]
      }
    };
    contractApi.preview.and.returnValue(of(preview));
    const store = TestBed.inject(ContractWorkspaceStore);
    store.loadForOperation(method.operationKey);
    store.selectArtifact(new File(['wsdl'], 'valid.wsdl'));
    store.analyzeArtifact();

    store.previewArtifact(active.id, method.operationKey);

    expect(store.artifactPreviewStatus()).toBe('ready');
    expect(store.artifactPreview()?.diff?.changes.map((change) => change.impact)).toEqual(['MappingRequired', 'RequiresDevelopment']);
  });

  it('acepta importación solo cuando backend confirma Draft y nunca trata Active como éxito', () => {
    const active = contract('active', 'v1', 'Active', true, false);
    contractApi.getByMethod.and.returnValue(of([active]));
    mappingApi.getByMethod.and.returnValue(of([]));
    const analysis: IntegrationContractArtifactAnalysisDto = {
      fileName: 'valid.wsdl', artifactKind: 'Wsdl11' as const, sizeBytes: 100, sha256: 'hash',
      operationName: method.operationKey, targetNamespace: 'urn:test', soapAction: 'urn:test/action',
      availableOperations: [method.operationKey], importable: true, issues: [], parameters: []
    };
    const preview: IntegrationContractArtifactPreviewDto = {
      analysis,
      diff: {
        leftContractVersionId: active.id, rightContractVersionId: '00000000-0000-0000-0000-000000000000', methodId: method.id,
        summary: { added: 0, removed: 0, modified: 0, requiresMapping: 0, requiresDevelopment: 0 }, changes: []
      }
    };
    contractApi.analyze.and.returnValue(of(analysis));
    contractApi.preview.and.returnValue(of(preview));
    const store = TestBed.inject(ContractWorkspaceStore);
    store.loadForOperation(method.operationKey);
    store.selectArtifact(new File(['wsdl'], 'valid.wsdl'));
    store.analyzeArtifact();
    store.previewArtifact(active.id, method.operationKey);

    const unexpectedActive = contract('unexpected', 'v2', 'Active', true, false);
    contractApi.import.and.returnValue(of({ analysis, contract: contractDetail(unexpectedActive) }));
    store.importArtifactDraft('v2', '', method.operationKey);
    expect(store.artifactImportStatus()).toBe('error');
    expect(store.artifactImportResult()).toBeNull();
    expect(store.versions().some((version) => version.id === unexpectedActive.id)).toBeFalse();

    const draft = contract('draftnew', 'v2', 'Draft', false, true);
    contractApi.import.and.returnValue(of({ analysis, contract: contractDetail(draft) }));
    store.importArtifactDraft('v2', '', method.operationKey);
    expect(store.artifactImportStatus()).toBe('ready');
    expect(store.artifactImportResult()?.contract.contract.status).toBe('Draft');
    expect(store.versions()[0].status).toBe('Draft');
  });
  function contract(
    id: string,
    versionLabel: string,
    status: IntegrationContractVersionDto['status'],
    isReadOnly: boolean,
    canEditFunctionalMetadata: boolean,
    revision = 2
  ): IntegrationContractVersionDto {
    return {
      id: `11111111-1111-1111-1111-${id.padEnd(12, '1').slice(0, 12)}`,
      methodId: method.id,
      versionLabel,
      status,
      operationName: method.operationKey,
      targetNamespace: 'urn:test',
      soapAction: 'urn:test/action',
      sourceType: 'ManualApproved',
      revision,
      importedAtUtc: '2026-10-13T10:00:00Z',
      importedBy: 'operador',
      capabilities: { isReadOnly, canEditFunctionalMetadata }
    };
  }

  function mapping(contractVersionId: string, status: IntegrationMappingSetDto['status']): IntegrationMappingSetDto {
    return {
      id: '22222222-2222-2222-2222-222222222222',
      methodId: method.id,
      methodCode: method.code,
      contractVersionId,
      revision: 3,
      name: 'Relación contractual',
      version: 2,
      status,
      isActive: status === 'Published',
      notes: '',
      publishedBy: '',
      rules: []
    };
  }

  function contractDetail(contractVersion: IntegrationContractVersionDto): IntegrationContractVersionDetailDto {
    return {
      contract: contractVersion,
      parameters: [
        {
          id: 2,
          contractVersionId: contractVersion.id,
          methodId: method.id,
          parameterPath: 'B',
          xmlElementName: 'B',
          displayName: 'Referencia',
          descriptionEs: 'Referencia funcional.',
          uiHelpText: '',
          category: 'General',
          exampleValue: 'ABC',
          dataType: 'string',
          xsdType: 'xsd:string',
          direction: 'Input',
          cardinality: 'Scalar',
          required: false,
          nillable: true,
          minOccurs: 0,
          maxOccurs: 1,
          supportStatus: 'Supported',
          sortOrder: 20,
          isActive: true
        },
        {
          id: 1,
          contractVersionId: contractVersion.id,
          methodId: method.id,
          parameterPath: 'A',
          xmlElementName: 'A',
          displayName: 'Cuenta destino',
          descriptionEs: 'Cuenta funcional de destino.',
          uiHelpText: 'Use el dato funcional autorizado.',
          category: 'Cuenta',
          exampleValue: '****1234',
          dataType: 'string',
          xsdType: 'xsd:string',
          direction: 'Input',
          cardinality: 'Scalar',
          required: true,
          nillable: false,
          minOccurs: 1,
          maxOccurs: 1,
          supportStatus: 'Supported',
          sortOrder: 10,
          isActive: true
        }
      ]
    };
  }

  function session(permissions: string[]): UserSession {
    return {
      token: 'test-token',
      username: 'operador',
      fullName: 'Operador Pruebas',
      userId: 'user-1',
      roles: ['ACH.Operator'],
      permissions
    };
  }
});
