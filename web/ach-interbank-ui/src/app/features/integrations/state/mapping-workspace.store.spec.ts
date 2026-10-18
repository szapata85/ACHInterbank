import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { IntegrationMappingPreviewResultDto, IntegrationMappingSetDto, IntegrationMappingWorkspaceDto } from '../data-access/integration-api.models';
import { IntegrationMappingApi } from '../data-access/integration-mapping.api';
import { IntegrationWorkspaceApi } from '../data-access/integration-workspace.api';
import { MappingWorkspaceStore } from './mapping-workspace.store';

describe('MappingWorkspaceStore P6.3', () => {
  let mappingApi: jasmine.SpyObj<IntegrationMappingApi>;
  let workspaceApi: jasmine.SpyObj<IntegrationWorkspaceApi>;

  const method = {
    id: 12,
    code: 'WSCFAACH.Proc_Transacciones',
    displayName: 'Créditos recibidos en CFA',
    soapClientCode: 'WSCFAACH',
    isActive: true,
    integrationKey: 'CFA',
    operationKey: 'Proc_Transacciones',
    mappingDirection: 'Inbound',
    mappingPurpose: 'Procesa créditos recibidos.',
    functionalNature: 'Credit',
    functionalOriginator: 'External',
    movesMoney: true
  };

  const mapping: IntegrationMappingSetDto = {
    id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    methodId: method.id,
    methodCode: method.operationKey,
    contractVersionId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    revision: 6,
    name: 'Borrador',
    version: 0,
    status: 'Draft',
    isActive: true,
    notes: '',
    publishedBy: '',
    rules: []
  };

  const workspace: IntegrationMappingWorkspaceDto = {
    mappingSet: { id: mapping.id, name: mapping.name, status: mapping.status, revision: mapping.revision },
    contract: { id: mapping.contractVersionId!, versionLabel: 'v2', targetNamespace: 'urn:test' },
    coverage: { total: 1, configured: 0, pendingRequired: 1, complete: 0, pending: 1, invalid: 0, notApplicable: 0 },
    publication: { canPublish: false, pendingRequired: 1, newRequiredPending: 1, blockingIssues: 1 },
    parameters: [
      {
        parameterId: 44,
        functionalName: 'Referencia',
        functionalDescription: 'Referencia funcional.',
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
          targetNamespace: 'urn:test',
          sortOrder: 1
        }
      }
    ],
    sourceGroups: [
      {
        key: 'Transaction',
        label: 'Transacción',
        fields: [
          {
            id: 81,
            methodId: method.id,
            sourceKind: 'Transaction',
            entityName: 'AchTransaction',
            fieldPath: 'transaction.reference',
            displayName: 'Referencia de transacción',
            dataType: 'string',
            cardinality: 'Scalar',
            nullable: true,
            sortOrder: 1,
            isActive: true
          }
        ]
      }
    ],
    transformations: [],
    sourceCompatibility: [
      {
        parameterId: 44,
        sourceCatalogFieldId: 81,
        allowsNoTransformation: true,
        transformations: []
      }
    ]
  };

  const preview: IntegrationMappingPreviewResultDto = {
    mappingSetId: mapping.id,
    methodId: method.id,
    methodCode: method.operationKey,
    contextMode: 'real-latest',
    items: [
      {
        parameterId: 44,
        parameterPath: 'OFIDTX',
        resolvedFrom: 'Sin origen configurado',
        previewValue: null,
        sourceSection: 'sin-configurar',
        resolutionKind: 'none',
        appliedTransformation: null,
        priority: 0,
        enabled: false,
        functionalName: 'Referencia',
        functionalDescription: 'Referencia funcional.',
        required: true,
        sourceDisplayName: null,
        transformationDisplayName: null,
        validationStatus: 'incomplete',
        validationIssues: [],
        isMasked: false
      }
    ],
    payloadPreviewJson: '{"OFIDTX":null}',
    rawPreviewJson: '[]',
    validation: {
      mappingSetId: mapping.id,
      isValid: false,
      issues: [],
      coverage: {
        totalParameters: 1,
        validParameters: 0,
        incompleteParameters: 1,
        invalidParameters: 0,
        inactiveParameters: 0,
        coveredByDefaultOrFixed: 0,
        coveredBySourceField: 0
      },
      parameters: [
        { parameterId: 44, parameterPath: 'OFIDTX', required: true, status: 'incomplete', resolutionKind: 'none', hints: [] }
      ]
    }
  };

  beforeEach(() => {
    mappingApi = jasmine.createSpyObj<IntegrationMappingApi>('IntegrationMappingApi', [
      'getByMethod',
      'getWorkspace',
      'preview',
      'updateParameterMapping',
      'publishWithRevision'
    ]);
    workspaceApi = jasmine.createSpyObj<IntegrationWorkspaceApi>('IntegrationWorkspaceApi', ['getMethods']);
    workspaceApi.getMethods.and.returnValue(of([method]));
    mappingApi.getByMethod.and.returnValue(of([mapping]));
    mappingApi.getWorkspace.and.returnValue(of(workspace));
    mappingApi.preview.and.returnValue(of(preview));
    mappingApi.updateParameterMapping.and.returnValue(of(mapping));
    mappingApi.publishWithRevision.and.returnValue(of(mapping));

    TestBed.configureTestingModule({
      providers: [
        MappingWorkspaceStore,
        { provide: IntegrationMappingApi, useValue: mappingApi },
        { provide: IntegrationWorkspaceApi, useValue: workspaceApi }
      ]
    });
  });

  it('selecciona el borrador editable y consume el workspace autoritativo del backend', () => {
    const store = TestBed.inject(MappingWorkspaceStore);

    store.load('Proc_Transacciones', true);

    expect(mappingApi.getByMethod).toHaveBeenCalledOnceWith(method.id);
    expect(mappingApi.getWorkspace).toHaveBeenCalledOnceWith(mapping.id);
    expect(mappingApi.preview).toHaveBeenCalledOnceWith(mapping.id, { maxItems: 1, useControlledSample: false });
    expect(store.preview()).toEqual(preview);
    expect(store.previewStatus()).toBe('ready');
    expect(store.workspace()?.mappingSet.status).toBe('Draft');
    expect(store.selectedMethod()?.displayName).toBe('Créditos recibidos en CFA');
  });


  it('no invoca vista previa cuando la capacidad de validación no fue autorizada', () => {
    const store = TestBed.inject(MappingWorkspaceStore);

    store.load('Proc_Transacciones');

    expect(mappingApi.getByMethod).toHaveBeenCalledOnceWith(method.id);
    expect(mappingApi.getWorkspace).toHaveBeenCalledOnceWith(mapping.id);
    expect(mappingApi.preview).not.toHaveBeenCalled();
    expect(store.previewStatus()).toBe('forbidden');
  });

  it('expone sin recalcular los conteos de cobertura recibidos del backend', () => {
    const store = TestBed.inject(MappingWorkspaceStore);
    store.load('Proc_Transacciones', true);
    expect(store.coverage()).toEqual(workspace.coverage);
  });

  it('envía Revision esperada y selección por catálogo, luego refresca el read model desde backend', () => {
    const refreshed: IntegrationMappingWorkspaceDto = {
      ...workspace,
      mappingSet: { ...workspace.mappingSet, revision: 7 },
      parameters: workspace.parameters.map(parameter => ({
        ...parameter,
        status: 'Complete' as const,
        source: 'Referencia de transacción',
        currentRule: {
          ruleId: 1001,
          sourceCatalogFieldId: 81,
          sourceKind: 'Transaction' as const,
          sourceFieldPath: 'transaction.reference',
          transformationCode: null,
          formatMask: null,
          priority: 1,
          enabled: true
        }
      }))
    };
    mappingApi.getWorkspace.and.returnValues(of(workspace), of(refreshed));
    const store = TestBed.inject(MappingWorkspaceStore);
    store.load('Proc_Transacciones', true);

    store.saveParameter(44, {
      sourceCatalogFieldId: 81,
      transformationCode: null,
      formatMask: null
    }, 'qa-user');

    expect(mappingApi.updateParameterMapping).toHaveBeenCalledOnceWith(mapping.id, 44, {
      expectedRevision: 6,
      sourceCatalogFieldId: 81,
      transformationCode: null,
      formatMask: null,
      updatedBy: 'qa-user'
    });
    expect(mappingApi.getWorkspace).toHaveBeenCalledTimes(2);
    expect(mappingApi.preview).toHaveBeenCalledTimes(2);
    expect(store.workspace()?.mappingSet.revision).toBe(7);
    expect(store.parameters()[0].source).toBe('Referencia de transacción');
  });

  it('no aplica éxito local antes de recibir respuesta del servidor', () => {
    const save$ = new Subject<IntegrationMappingSetDto>();
    mappingApi.updateParameterMapping.and.returnValue(save$);
    const store = TestBed.inject(MappingWorkspaceStore);
    store.load('Proc_Transacciones', true);

    store.saveParameter(44, {
      sourceCatalogFieldId: 81,
      transformationCode: null,
      formatMask: null
    }, 'qa-user');

    expect(store.saveSucceededParameterId()).toBeNull();
    expect(store.workspace()?.mappingSet.revision).toBe(6);
    expect(mappingApi.getWorkspace).toHaveBeenCalledTimes(1);
  });

  it('rechaza selecciones no publicadas por metadata backend antes de invocar guardado', () => {
    const store = TestBed.inject(MappingWorkspaceStore);
    store.load('Proc_Transacciones', true);

    store.saveParameter(44, {
      sourceCatalogFieldId: 999,
      transformationCode: null,
      formatMask: null
    }, 'qa-user');

    expect(mappingApi.updateParameterMapping).not.toHaveBeenCalled();
    expect(store.saveError()?.message).toContain('origen disponible');
  });

  it('mantiene el workspace visible y expone forbidden cuando falla el guardado', () => {
    mappingApi.updateParameterMapping.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 403, statusText: 'Forbidden' }))
    );
    const store = TestBed.inject(MappingWorkspaceStore);
    store.load('Proc_Transacciones', true);

    store.saveParameter(44, {
      sourceCatalogFieldId: 81,
      transformationCode: null,
      formatMask: null
    }, 'qa-user');

    expect(store.status()).toBe('ready');
    expect(store.workspace()).not.toBeNull();
    expect(store.saveError()?.kind).toBe('forbidden');
  });

  it('mantiene el workspace y expone forbidden cuando falla únicamente la vista previa', () => {
    mappingApi.preview.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 403, statusText: 'Forbidden' }))
    );
    const store = TestBed.inject(MappingWorkspaceStore);

    store.load('Proc_Transacciones', true);

    expect(store.status()).toBe('ready');
    expect(store.workspace()).toEqual(workspace);
    expect(store.preview()).toBeNull();
    expect(store.previewStatus()).toBe('forbidden');
    expect(store.previewError()?.kind).toBe('forbidden');
  });

  it('refresca la vista previa desde backend sin derivar valores sensibles en el store', () => {
    const refreshedPreview: IntegrationMappingPreviewResultDto = {
      ...preview,
      items: preview.items.map(item => ({ ...item, previewValue: '••••4321', isMasked: true }))
    };
    mappingApi.preview.and.returnValues(of(preview), of(refreshedPreview));
    const store = TestBed.inject(MappingWorkspaceStore);
    store.load('Proc_Transacciones', true);

    store.refreshPreview();

    expect(mappingApi.preview).toHaveBeenCalledTimes(2);
    expect(store.preview()?.items[0].previewValue).toBe('••••4321');
    expect(store.preview()?.items[0].isMasked).toBeTrue();
  });

  it('detecta Revision obsoleta al guardar sin sobrescribir el workspace local', () => {
    mappingApi.updateParameterMapping.and.returnValue(
      throwError(() => new HttpErrorResponse({
        status: 409,
        error: {
          detail: 'La configuración cambió. Revisión esperada=6, actual=7.',
          expectedRevision: 6,
          currentRevision: 7
        }
      }))
    );
    const store = TestBed.inject(MappingWorkspaceStore);
    store.load('Proc_Transacciones', true);

    store.saveParameter(44, { sourceCatalogFieldId: 81, transformationCode: null, formatMask: null }, 'qa-user');

    expect(store.revisionConflict()?.operation).toBe('parameter');
    expect(store.revisionConflict()?.expectedRevision).toBe(6);
    expect(store.revisionConflict()?.currentRevision).toBe(7);
    expect(store.workspace()?.mappingSet.revision).toBe(6);
    expect(store.saveSucceededParameterId()).toBeNull();
  });

  it('recarga el estado vigente desde backend después de un conflicto sin hacer merge automático', () => {
    const refreshed = {
      ...workspace,
      mappingSet: { ...workspace.mappingSet, revision: 7 }
    };
    mappingApi.getWorkspace.and.returnValues(of(workspace), of(refreshed));
    mappingApi.updateParameterMapping.and.returnValue(
      throwError(() => new HttpErrorResponse({
        status: 409,
        error: { detail: 'conflict', expectedRevision: 6, currentRevision: 7 }
      }))
    );
    const store = TestBed.inject(MappingWorkspaceStore);
    store.load('Proc_Transacciones', true);
    store.saveParameter(44, { sourceCatalogFieldId: 81, transformationCode: null, formatMask: null }, 'qa-user');

    store.reloadAfterRevisionConflict();

    expect(mappingApi.getWorkspace).toHaveBeenCalledTimes(2);
    expect(store.workspace()?.mappingSet.revision).toBe(7);
    expect(store.revisionConflict()).toBeNull();
  });

  it('impide bypass de publicación cuando el read model backend no informa elegibilidad', () => {
    const store = TestBed.inject(MappingWorkspaceStore);
    store.load('Proc_Transacciones', true);

    store.publish('qa-user');

    expect(mappingApi.publishWithRevision).not.toHaveBeenCalled();
    expect(store.publicationError()?.message).toContain('Falta 1 dato requerido');
  });

  it('falla cerrado si backend reporta required pendiente aunque canPublish llegue inconsistente en true', () => {
    const inconsistent: IntegrationMappingWorkspaceDto = {
      ...workspace,
      publication: { canPublish: true, pendingRequired: 1, newRequiredPending: 1, blockingIssues: 1 }
    };
    mappingApi.getWorkspace.and.returnValue(of(inconsistent));
    const store = TestBed.inject(MappingWorkspaceStore);
    store.load('Proc_Transacciones', true);

    store.publish('qa-user');

    expect(mappingApi.publishWithRevision).not.toHaveBeenCalled();
    expect(store.publicationError()?.message).toContain('Falta 1 dato requerido');
  });

  it('publica con Revision esperada y refresca workspace solo después de la respuesta del servidor', () => {
    const eligible: IntegrationMappingWorkspaceDto = {
      ...workspace,
      coverage: { ...workspace.coverage, configured: 1, pendingRequired: 0, complete: 1, pending: 0 },
      publication: { canPublish: true, pendingRequired: 0, newRequiredPending: 0, blockingIssues: 0 },
      parameters: workspace.parameters.map(parameter => ({ ...parameter, status: 'Complete' as const }))
    };
    const published: IntegrationMappingWorkspaceDto = {
      ...eligible,
      mappingSet: { ...eligible.mappingSet, status: 'Published' as const, revision: 7 },
      publication: { ...eligible.publication, canPublish: false }
    };
    const publish$ = new Subject<IntegrationMappingSetDto>();
    mappingApi.getWorkspace.and.returnValues(of(eligible), of(published));
    mappingApi.publishWithRevision.and.returnValue(publish$);
    const store = TestBed.inject(MappingWorkspaceStore);
    store.load('Proc_Transacciones', true);

    store.publish('qa-user');

    expect(mappingApi.publishWithRevision).toHaveBeenCalledOnceWith(mapping.id, {
      expectedRevision: 6,
      publishedBy: 'qa-user',
      publishNote: null
    });
    expect(mappingApi.getWorkspace).toHaveBeenCalledTimes(1);
    expect(store.publicationSucceeded()).toBeFalse();

    publish$.next({ ...mapping, status: 'Published', revision: 7 });
    publish$.complete();

    expect(mappingApi.getWorkspace).toHaveBeenCalledTimes(2);
    expect(store.workspace()?.mappingSet.status).toBe('Published');
    expect(store.publicationSucceeded()).toBeTrue();
  });

  it('detecta conflicto de Revision durante publicación y conserva el estado visible', () => {
    const eligible: IntegrationMappingWorkspaceDto = {
      ...workspace,
      publication: { canPublish: true, pendingRequired: 0, newRequiredPending: 0, blockingIssues: 0 },
      coverage: { ...workspace.coverage, pendingRequired: 0 }
    };
    mappingApi.getWorkspace.and.returnValue(of(eligible));
    mappingApi.publishWithRevision.and.returnValue(
      throwError(() => new HttpErrorResponse({
        status: 409,
        error: { detail: 'publish conflict', expectedRevision: 6, currentRevision: 9 }
      }))
    );
    const store = TestBed.inject(MappingWorkspaceStore);
    store.load('Proc_Transacciones', true);

    store.publish('qa-user');

    expect(store.revisionConflict()?.operation).toBe('publication');
    expect(store.revisionConflict()?.currentRevision).toBe(9);
    expect(store.workspace()?.mappingSet.revision).toBe(6);
    expect(store.publicationSucceeded()).toBeFalse();
  });

  it('traduce un 403 del workspace al estado forbidden', () => {
    mappingApi.getWorkspace.and.returnValue(throwError(() => new HttpErrorResponse({ status: 403 })));
    const store = TestBed.inject(MappingWorkspaceStore);

    store.load('Proc_Transacciones', true);

    expect(store.status()).toBe('forbidden');
    expect(store.error()?.kind).toBe('forbidden');
  });
});
