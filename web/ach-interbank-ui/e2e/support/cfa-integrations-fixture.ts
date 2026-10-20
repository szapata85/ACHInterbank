import type { Page, Route } from '@playwright/test';

export type CfaBrandingFixture = {
  name: string;
  payload: Record<string, string | null>;
};

export const CFA_BRANDING_CASES: readonly CfaBrandingFixture[] = [
  {
    name: 'primary oscuro',
    payload: {
      logoUrl: null,
      primaryColor: '#102a43',
      secondaryColor: '#486581',
      sidebarBackground: '#102a43',
      publicBackground: '#ffffff',
      privateBackground: '#ffffff'
    }
  },
  {
    name: 'primary claro',
    payload: {
      logoUrl: null,
      primaryColor: '#d9eafd',
      secondaryColor: '#334e68',
      sidebarBackground: '#334e68',
      publicBackground: '#ffffff',
      privateBackground: '#ffffff'
    }
  },
  {
    name: 'sidebar oscuro',
    payload: {
      logoUrl: null,
      primaryColor: '#2f6f9f',
      secondaryColor: '#243b53',
      sidebarBackground: '#243b53',
      publicBackground: '#ffffff',
      privateBackground: '#ffffff'
    }
  },
  {
    name: 'background claro',
    payload: {
      logoUrl: null,
      primaryColor: '#2f6f9f',
      secondaryColor: '#486581',
      sidebarBackground: '#243b53',
      publicBackground: '#ffffff',
      privateBackground: '#f7fafc'
    }
  }
] as const;

export interface CfaMockOptions {
  permissions?: string[];
  branding?: Record<string, string | null>;
  longContent?: boolean;
}

export interface CfaMockState {
  mappingWrites: number;
  publishWrites: number;
  contractImports: number;
  connectionPreviewWrites: number;
  connectionWrites: number;
  mappingConflictOnce: boolean;
  connectionConflictOnce: boolean;
  lastMappingRequest: Record<string, unknown> | null;
  lastPublishRequest: Record<string, unknown> | null;
  lastConnectionRequest: Record<string, unknown> | null;
  currentMappingRevision: number;
  currentConnectionRevision: string;
}

const DEFAULT_PERMISSIONS = ['CanReadAch', 'CanManageAch'];
const DRAFT_MAPPING_ID = '11111111-1111-1111-1111-111111111111';
const PUBLISHED_MAPPING_ID = '11111111-1111-1111-1111-111111111112';
const ACTIVE_CONTRACT_ID = '22222222-2222-2222-2222-222222222222';
const DRAFT_CONTRACT_ID = '22222222-2222-2222-2222-222222222223';
const RETIRED_CONTRACT_ID = '22222222-2222-2222-2222-222222222221';

export async function installCfaIntegrationsMock(page: Page, options: CfaMockOptions = {}): Promise<CfaMockState> {
  const permissions = options.permissions ?? DEFAULT_PERMISSIONS;
  const token = createUnsignedJwt({
    unique_name: 'qa.cfa.integrations',
    name: 'Entidad financiera con un nombre funcional extraordinariamente largo para robustez UX',
    uid: 'qa-cfa-integrations',
    role: ['Admin'],
    permission: permissions,
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000)
  });

  const methods = buildMethods(options.longContent === true);
  const draftWorkspace = buildDraftWorkspace(options.longContent === true);
  const publishedWorkspaces = new Map<string, Record<string, unknown>>([
    ['mapping-published-2', buildPublishedWorkspace(2, 'Débitos originados en CFA')],
    ['mapping-published-3', buildPublishedWorkspace(3, 'Respuestas de transacciones')]
  ]);
  let connectionSettings = buildConnectionSettings(options.longContent === true);
  let analysisCount = 0;

  const state: CfaMockState = {
    mappingWrites: 0,
    publishWrites: 0,
    contractImports: 0,
    connectionPreviewWrites: 0,
    connectionWrites: 0,
    mappingConflictOnce: false,
    connectionConflictOnce: false,
    lastMappingRequest: null,
    lastPublishRequest: null,
    lastConnectionRequest: null,
    currentMappingRevision: Number((draftWorkspace.mappingSet as Record<string, unknown>)['revision']),
    currentConnectionRevision: connectionSettings.revision
  };

  await page.addInitScript((accessToken) => {
    window.sessionStorage.setItem('ach.interbank.access_token', accessToken);
  }, token);

  await page.route(/\/auth\/refresh(?:\?.*)?$/i, async route => {
    await json(route, 200, {
      sucess: true,
      data: {
        token,
        username: 'qa.cfa.integrations',
        fullName: 'Usuario QA Integraciones CFA',
        roles: ['Admin'],
        permissions
      }
    });
  });

  await page.route(/\/api\/navigation\/menu(?:\?.*)?$/i, async route => {
    await json(route, 200, []);
  });

  await page.route(/\/api\/(?:integrations|users\/soap-integrations|users\/branding|navigation-logs)(?:\/.*)?(?:\?.*)?$/i, async route => {
    if (route.request().resourceType() === 'document') {
      await route.fallback();
      return;
    }

    const request = route.request();
    const url = new URL(request.url());
    const path = url.pathname;
    const method = request.method().toUpperCase();

    if (method === 'GET' && path === '/api/users/branding') {
      await json(route, 200, options.branding ?? CFA_BRANDING_CASES[0].payload);
      return;
    }

    if (method === 'POST' && path === '/api/navigation-logs') {
      await route.fulfill({ status: 204 });
      return;
    }

    if (method === 'GET' && path === '/api/integrations/methods') {
      await json(route, 200, methods);
      return;
    }

    if (method === 'GET' && path === '/api/users/soap-integrations') {
      await json(route, 200, connectionSettings);
      return;
    }

    if (method === 'POST' && path === '/api/users/soap-integrations/preview-change') {
      state.connectionPreviewWrites += 1;
      const body = safeJsonBody(request.postData());
      const methodName = String(body['methodName'] ?? 'Proc_Transacciones');
      await json(route, 200, {
        methodName,
        revision: connectionSettings.revision,
        currentConfigurationState: 'Simulation',
        requestedConfigurationState: 'RealSendEnabled',
        sanitizedEndpoint: sanitizedDestinationFor(methodName, options.longContent === true),
        sanitizedSoapAction: `https://cfa.example.test/actions/${methodName}`,
        wouldEnableRealSend: true,
        canApply: true,
        blockingReason: null
      });
      return;
    }

    if (method === 'PUT' && path === '/api/users/soap-integrations') {
      state.connectionWrites += 1;
      state.lastConnectionRequest = safeJsonBody(request.postData());
      if (state.connectionConflictOnce) {
        state.connectionConflictOnce = false;
        await json(route, 409, {
          title: 'Conflicto de revisión',
          detail: 'La configuración cambió en backend.',
          expectedRevision: connectionSettings.revision,
          currentRevision: 'rev-99'
        });
        return;
      }

      const incoming = state.lastConnectionRequest as Record<string, unknown>;
      const requested = Array.isArray(incoming['wscfaachMappings']) ? incoming['wscfaachMappings'] as Array<Record<string, unknown>> : [];
      const live = requested.find(item => item['methodName'] === 'Proc_Transacciones');
      connectionSettings = {
        ...connectionSettings,
        revision: 'rev-8',
        expectedRevision: null,
        wscfaachMappings: connectionSettings.wscfaachMappings.map(item => item.methodName === 'Proc_Transacciones'
          ? {
              ...item,
              endpoint: String(live?.['endpoint'] ?? item.endpoint),
              soapAction: String(live?.['soapAction'] ?? item.soapAction),
              operatingMode: String(live?.['operatingMode'] ?? item.operatingMode),
              enabled: Boolean(live?.['enabled'] ?? item.enabled),
              configurationState: live?.['operatingMode'] === 'Live' && live?.['enabled'] !== false ? 'RealSendEnabled' : item.configurationState,
              sanitizedEndpoint: sanitizedDestinationFor('Proc_Transacciones', options.longContent === true)
            }
          : item)
      };
      state.currentConnectionRevision = connectionSettings.revision;
      await json(route, 200, connectionSettings);
      return;
    }

    if (method === 'GET' && /^\/api\/integrations\/methods\/\d+\/contracts$/.test(path)) {
      const methodId = Number(path.split('/')[4]);
      await json(route, 200, contractsFor(methodId, options.longContent === true));
      return;
    }

    if (method === 'GET' && path === '/api/integrations/mappingsets/published') {
      const methodId = Number(url.searchParams.get('methodId') ?? '1');
      await json(route, 200, publishedMapping(methodId));
      return;
    }

    if (method === 'GET' && path === '/api/integrations/mappingsets') {
      const methodId = Number(url.searchParams.get('methodId') ?? '1');
      await json(route, 200, mappingSetsFor(methodId, draftWorkspace));
      return;
    }

    const workspaceMatch = path.match(/^\/api\/integrations\/mappingsets\/([^/]+)\/workspace$/);
    if (method === 'GET' && workspaceMatch) {
      const mappingId = workspaceMatch[1];
      if (mappingId === DRAFT_MAPPING_ID || mappingId === PUBLISHED_MAPPING_ID) {
        await json(route, 200, mappingId === DRAFT_MAPPING_ID ? draftWorkspace : buildPublishedWorkspace(1, 'Créditos recibidos en CFA'));
        return;
      }
      await json(route, 200, publishedWorkspaces.get(mappingId) ?? buildPublishedWorkspace(1, 'Servicio CFA'));
      return;
    }

    const mappingParameterMatch = path.match(/^\/api\/integrations\/mappingsets\/([^/]+)\/parameters\/(\d+)$/);
    if (method === 'PUT' && mappingParameterMatch) {
      state.mappingWrites += 1;
      state.lastMappingRequest = safeJsonBody(request.postData());
      if (state.mappingConflictOnce) {
        state.mappingConflictOnce = false;
        await json(route, 409, {
          title: 'Conflicto de revisión',
          detail: 'mapping revision conflict',
          expectedRevision: state.currentMappingRevision,
          currentRevision: state.currentMappingRevision + 1
        });
        return;
      }

      applyMappingSave(draftWorkspace, Number(mappingParameterMatch[2]), state.lastMappingRequest);
      state.currentMappingRevision = Number((draftWorkspace.mappingSet as Record<string, unknown>)['revision']);
      await json(route, 200, draftMappingDto(draftWorkspace));
      return;
    }

    const mappingPreviewMatch = path.match(/^\/api\/integrations\/mappingsets\/([^/]+)\/preview$/);
    if (method === 'POST' && mappingPreviewMatch) {
      const workspace = mappingPreviewMatch[1] === DRAFT_MAPPING_ID ? draftWorkspace : buildPublishedWorkspace(1, 'Servicio CFA');
      await json(route, 200, buildMappingPreview(workspace));
      return;
    }

    const mappingValidateMatch = path.match(/^\/api\/integrations\/mappingsets\/([^/]+)\/validate$/);
    if (method === 'POST' && mappingValidateMatch) {
      const mappingId = mappingValidateMatch[1];
      const workspace = mappingId === DRAFT_MAPPING_ID ? draftWorkspace : buildPublishedWorkspace(1, 'Servicio CFA');
      await json(route, 200, buildValidation(workspace));
      return;
    }

    const mappingPublishMatch = path.match(/^\/api\/integrations\/mappingsets\/([^/]+)\/publish$/);
    if (method === 'POST' && mappingPublishMatch) {
      state.publishWrites += 1;
      state.lastPublishRequest = safeJsonBody(request.postData());
      const publication = draftWorkspace.publication as Record<string, unknown>;
      if (Number(publication['pendingRequired']) > 0 || publication['canPublish'] !== true) {
        await json(route, 409, { detail: 'Backend bloquea publicación por datos requeridos pendientes.' });
        return;
      }

      (draftWorkspace.mappingSet as Record<string, unknown>)['status'] = 'Published';
      (draftWorkspace.mappingSet as Record<string, unknown>)['revision'] = state.currentMappingRevision + 1;
      publication['canPublish'] = false;
      state.currentMappingRevision += 1;
      await json(route, 200, draftMappingDto(draftWorkspace));
      return;
    }

    const contractDetailMatch = path.match(/^\/api\/integrations\/contracts\/([^/]+)$/);
    if (method === 'GET' && contractDetailMatch) {
      await json(route, 200, contractDetail(contractDetailMatch[1], options.longContent === true));
      return;
    }

    const parameterPresentationMatch = path.match(/^\/api\/integrations\/contracts\/([^/]+)\/parameters\/(\d+)\/presentation$/);
    if (method === 'PUT' && parameterPresentationMatch) {
      const detail = contractDetail(parameterPresentationMatch[1], options.longContent === true);
      const requestBody = safeJsonBody(request.postData());
      const parameter = { ...detail.parameters[0], displayName: String(requestBody['displayName'] ?? detail.parameters[0].displayName) };
      await json(route, 200, { contractVersionId: detail.contract.id, contractRevision: detail.contract.revision + 1, parameter });
      return;
    }

    if (method === 'POST' && path === '/api/integrations/contracts/analyze') {
      analysisCount += 1;
      await json(route, 200, buildArtifactAnalysis(analysisCount > 1, options.longContent === true));
      return;
    }

    const contractPreviewMatch = path.match(/^\/api\/integrations\/methods\/(\d+)\/contracts\/preview\/([^/]+)$/);
    if (method === 'POST' && contractPreviewMatch) {
      await json(route, 200, {
        analysis: buildArtifactAnalysis(true, options.longContent === true),
        diff: buildContractDiff(contractPreviewMatch[2], options.longContent === true)
      });
      return;
    }

    const contractImportMatch = path.match(/^\/api\/integrations\/methods\/(\d+)\/contracts\/import$/);
    if (method === 'POST' && contractImportMatch) {
      state.contractImports += 1;
      await json(route, 200, {
        analysis: buildArtifactAnalysis(true, options.longContent === true),
        contract: contractDetail('imported-draft', options.longContent === true)
      });
      return;
    }

    await json(route, 200, {});
  });

  return state;
}

function buildMethods(longContent: boolean): Array<Record<string, unknown>> {
  const longSuffix = longContent
    ? ' — servicio corporativo con una descripción funcional extensa para validar reflow, zoom y nombres de entidad sin truncamiento'
    : '';
  return [
    method(1, 'WSCFAACH.Proc_Transacciones', 'Proc_Transacciones', `Créditos recibidos en CFA${longSuffix}`, `Procesa créditos recibidos en CFA${longSuffix}.`),
    method(2, 'WSCFAACH.Proc_Contrapartidas', 'Proc_Contrapartidas', `Débitos originados en CFA${longSuffix}`, `Procesa débitos originados en CFA${longSuffix}.`),
    method(3, 'WSAXON.RegistrarRespuestaTransaccion', 'RegistrarRespuestaTransaccion', `Respuestas de transacciones${longSuffix}`, `Registra respuestas de transacciones${longSuffix}.`)
  ];
}

function method(id: number, code: string, operationKey: string, displayName: string, mappingPurpose: string): Record<string, unknown> {
  return {
    id,
    code,
    displayName,
    soapClientCode: id === 3 ? 'WSAXON' : 'WSCFAACH',
    isActive: true,
    integrationKey: 'CFA',
    operationKey,
    mappingDirection: 'Inbound',
    mappingPurpose,
    functionalNature: displayName,
    functionalOriginator: 'CFA',
    movesMoney: id !== 3
  };
}

function buildDraftWorkspace(longContent: boolean): Record<string, any> {
  const namespace = longContent
    ? 'https://cfa.example.test/contracts/namespace/with/a/very/long/technical/path/that/must/wrap/without/global/overflow'
    : 'https://cfa.example.test/contracts';
  return {
    mappingSet: { id: DRAFT_MAPPING_ID, name: 'Configuración de trabajo CFA', status: 'Draft', revision: 8 },
    contract: { id: ACTIVE_CONTRACT_ID, versionLabel: 'cfa-2026-11', targetNamespace: namespace },
    coverage: { total: 2, configured: 1, pendingRequired: 1, complete: 1, pending: 1, invalid: 0, notApplicable: 0 },
    publication: { canPublish: false, pendingRequired: 1, newRequiredPending: 1, blockingIssues: 1 },
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
          targetNamespace: namespace,
          sortOrder: 10
        }
      },
      {
        parameterId: 11,
        functionalName: longContent
          ? 'Número de referencia funcional de la transacción con un nombre deliberadamente largo para probar envoltura de texto'
          : 'Número de referencia',
        functionalDescription: 'Referencia funcional de la transacción.',
        technicalName: 'OFIDTX_TECHNICAL_IDENTIFIER_WITH_A_LONG_SUFFIX_FOR_REFLOW',
        required: true,
        dataType: 'string',
        category: 'Transacción',
        status: 'Pending',
        source: null,
        transformation: null,
        currentRule: null,
        technicalDetails: {
          parameterPath: 'OFIDTX_TECHNICAL_IDENTIFIER_WITH_A_LONG_SUFFIX_FOR_REFLOW',
          xmlElementName: 'OFIDTX',
          dataType: 'string',
          xsdType: 'xsd:string',
          targetNamespace: namespace,
          sortOrder: 20
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
            entityName: longContent ? 'AchTransactionEntityNameWithAnExtremelyLongFunctionalIdentifierForResponsiveCoverage' : 'AchTransaction',
            fieldPath: 'transaction.reference.path.with.a.long.technical.segment.for.support',
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
      { code: 'Trim', displayName: 'Recortar', description: 'Elimina espacios al inicio y final.', supportsFormatMask: false, supportsMultipleSources: false },
      { code: 'DateFormat', displayName: 'Formatear fecha', description: 'Formatea una fecha con una máscara aprobada.', supportsFormatMask: true, supportsMultipleSources: false }
    ],
    sourceCompatibility: [
      {
        parameterId: 10,
        sourceCatalogFieldId: 101,
        allowsNoTransformation: true,
        transformations: [{ code: 'Trim', displayName: 'Recortar', description: 'Elimina espacios.', requiresFormatMask: false }]
      },
      {
        parameterId: 11,
        sourceCatalogFieldId: 101,
        allowsNoTransformation: true,
        transformations: [{ code: 'Trim', displayName: 'Recortar', description: 'Elimina espacios.', requiresFormatMask: false }]
      }
    ]
  };
}

function buildPublishedWorkspace(methodId: number, displayName: string): Record<string, any> {
  return {
    mappingSet: { id: `mapping-published-${methodId}`, name: `Configuración publicada ${displayName}`, status: 'Published', revision: 4 },
    contract: { id: `contract-active-${methodId}`, versionLabel: 'v2', targetNamespace: 'https://cfa.example.test/contracts' },
    coverage: { total: 1, configured: 1, pendingRequired: 0, complete: 1, pending: 0, invalid: 0, notApplicable: 0 },
    publication: { canPublish: false, pendingRequired: 0, newRequiredPending: 0, blockingIssues: 0 },
    parameters: [
      {
        parameterId: 100 + methodId,
        functionalName: `Dato funcional ${displayName}`,
        functionalDescription: 'Dato configurado y publicado.',
        technicalName: `TECH_${methodId}`,
        required: true,
        dataType: 'string',
        category: 'Servicio',
        status: 'Complete',
        source: 'Referencia de la transacción',
        transformation: 'Recortar',
        currentRule: { ruleId: 1, sourceCatalogFieldId: 101, sourceKind: 'Transaction', sourceFieldPath: 'transaction.reference', transformationCode: 'Trim', formatMask: null, priority: 1, enabled: true },
        technicalDetails: { parameterPath: `TECH_${methodId}`, xmlElementName: `TECH_${methodId}`, dataType: 'string', xsdType: 'xsd:string', targetNamespace: 'https://cfa.example.test/contracts', sortOrder: 1 }
      }
    ],
    sourceGroups: [],
    transformations: [],
    sourceCompatibility: []
  };
}

function mappingSetsFor(methodId: number, draftWorkspace: Record<string, any>): Array<Record<string, unknown>> {
  if (methodId === 1) {
    return [draftMappingDto(draftWorkspace), publishedMapping(1)];
  }
  return [publishedMapping(methodId)];
}

function draftMappingDto(workspace: Record<string, any>): Record<string, unknown> {
  const mappingSet = workspace.mappingSet as Record<string, unknown>;
  return {
    id: mappingSet['id'],
    methodId: 1,
    methodCode: 'Proc_Transacciones',
    contractVersionId: ACTIVE_CONTRACT_ID,
    revision: mappingSet['revision'],
    name: mappingSet['name'],
    version: 0,
    status: mappingSet['status'],
    isActive: true,
    notes: '',
    publishedBy: '',
    rules: []
  };
}

function publishedMapping(methodId: number): Record<string, unknown> {
  return {
    id: methodId === 1 ? PUBLISHED_MAPPING_ID : `mapping-published-${methodId}`,
    methodId,
    methodCode: methodId === 1 ? 'Proc_Transacciones' : methodId === 2 ? 'Proc_Contrapartidas' : 'RegistrarRespuestaTransaccion',
    contractVersionId: methodId === 1 ? ACTIVE_CONTRACT_ID : `contract-active-${methodId}`,
    revision: 4,
    name: `Configuración publicada ${methodId}`,
    version: 2,
    status: 'Published',
    isActive: true,
    notes: '',
    publishedAtUtc: '2026-10-10T14:00:00Z',
    publishedBy: 'backend.qa',
    rules: []
  };
}

function applyMappingSave(workspace: Record<string, any>, parameterId: number, request: Record<string, unknown> | null): void {
  const parameter = (workspace.parameters as Array<Record<string, any>>).find(item => item['parameterId'] === parameterId);
  if (!parameter) return;

  parameter['status'] = 'Complete';
  parameter['source'] = 'Referencia de la transacción';
  parameter['transformation'] = request?.['transformationCode'] ? 'Recortar' : 'Sin tratamiento';
  parameter['currentRule'] = {
    ruleId: 777,
    sourceCatalogFieldId: Number(request?.['sourceCatalogFieldId'] ?? 101),
    sourceKind: 'Transaction',
    sourceFieldPath: 'transaction.reference',
    transformationCode: request?.['transformationCode'] ?? null,
    formatMask: request?.['formatMask'] ?? null,
    priority: 1,
    enabled: true
  };
  workspace.mappingSet.revision += 1;
  workspace.coverage = { total: 2, configured: 2, pendingRequired: 0, complete: 2, pending: 0, invalid: 0, notApplicable: 0 };
  workspace.publication = { canPublish: true, pendingRequired: 0, newRequiredPending: 0, blockingIssues: 0 };
}

function buildMappingPreview(workspace: Record<string, any>): Record<string, unknown> {
  const pending = Number(workspace.publication.pendingRequired) > 0;
  return {
    mappingSetId: workspace.mappingSet.id,
    methodId: 1,
    methodCode: 'Proc_Transacciones',
    contextMode: 'real-latest',
    items: (workspace.parameters as Array<Record<string, any>>).map(parameter => ({
      parameterId: parameter['parameterId'],
      parameterPath: parameter['technicalDetails']['parameterPath'],
      resolvedFrom: parameter['status'] === 'Complete' ? 'transaction.reference' : 'Sin origen configurado',
      previewValue: parameter['status'] === 'Complete' ? '••••9012' : null,
      sourceSection: parameter['status'] === 'Complete' ? 'transaccion' : 'sin-configurar',
      resolutionKind: parameter['status'] === 'Complete' ? 'source-field' : 'none',
      appliedTransformation: parameter['status'] === 'Complete' ? 'Trim' : null,
      priority: parameter['status'] === 'Complete' ? 1 : 0,
      enabled: parameter['status'] === 'Complete',
      functionalName: parameter['functionalName'],
      functionalDescription: parameter['functionalDescription'],
      required: parameter['required'],
      sourceDisplayName: parameter['status'] === 'Complete' ? 'Referencia de la transacción' : null,
      transformationDisplayName: parameter['status'] === 'Complete' ? 'Recortar' : null,
      validationStatus: parameter['status'] === 'Complete' ? 'Complete' : 'Pending',
      validationIssues: parameter['status'] === 'Complete' ? [] : [{ severity: 'Error', code: 'REQUIRED_PARAMETER_MISSING', message: 'Required parameter has no enabled mapping rule.', path: parameter['technicalDetails']['parameterPath'], category: 'Coverage' }],
      isMasked: parameter['status'] === 'Complete'
    })),
    payloadPreviewJson: '{"sanitized":true}',
    rawPreviewJson: '{"sanitized":true}',
    validation: buildValidation(workspace, pending)
  };
}

function buildValidation(workspace: Record<string, any>, pending = Number(workspace.publication.pendingRequired) > 0): Record<string, unknown> {
  return {
    mappingSetId: workspace.mappingSet.id,
    isValid: !pending,
    issues: pending ? [{ severity: 'Error', code: 'REQUIRED_PARAMETER_MISSING', message: 'Required parameter OFIDTX has no enabled mapping rule.', path: 'OFIDTX', category: 'Coverage' }] : [],
    coverage: {
      totalParameters: workspace.coverage.total,
      validParameters: workspace.coverage.complete,
      incompleteParameters: workspace.coverage.pending,
      invalidParameters: workspace.coverage.invalid,
      inactiveParameters: workspace.coverage.notApplicable,
      coveredByDefaultOrFixed: 0,
      coveredBySourceField: workspace.coverage.complete
    },
    parameters: []
  };
}

function contractsFor(methodId: number, longContent: boolean): Array<Record<string, unknown>> {
  if (methodId !== 1) {
    return [contractVersion(`contract-active-${methodId}`, methodId, 'v2', 'Active', false, longContent)];
  }
  return [
    contractVersion(DRAFT_CONTRACT_ID, 1, 'v3-borrador', 'Draft', true, longContent),
    contractVersion(ACTIVE_CONTRACT_ID, 1, 'v2-activa', 'Active', false, longContent),
    contractVersion(RETIRED_CONTRACT_ID, 1, 'v1-retirada', 'Retired', false, longContent)
  ];
}

function contractVersion(id: string, methodId: number, versionLabel: string, status: string, editable: boolean, longContent: boolean): Record<string, unknown> {
  return {
    id,
    methodId,
    versionLabel,
    status,
    operationName: methodId === 1 ? 'Proc_Transacciones' : methodId === 2 ? 'Proc_Contrapartidas' : 'RegistrarRespuestaTransaccion',
    targetNamespace: longContent ? 'https://cfa.example.test/contracts/namespace/that/is/intentionally/long/for/technical/wrapping' : 'https://cfa.example.test/contracts',
    soapAction: `https://cfa.example.test/actions/${methodId}`,
    sourceType: 'WsdlImport',
    sourceDocumentName: 'contrato-cfa.wsdl',
    sourceDocumentHash: 'a'.repeat(64),
    sourceReference: null,
    notes: status === 'Draft' ? 'Borrador funcional para revisión.' : 'Contrato funcional.',
    revision: status === 'Draft' ? 7 : status === 'Active' ? 6 : 5,
    importedAtUtc: '2026-10-12T13:30:00Z',
    importedBy: 'qa.backend',
    activatedAtUtc: status === 'Draft' ? null : '2026-10-12T14:00:00Z',
    activatedBy: status === 'Draft' ? null : 'qa.aprobador',
    capabilities: { isReadOnly: !editable, canEditFunctionalMetadata: editable }
  };
}

function contractDetail(id: string, longContent: boolean): { contract: Record<string, any>; parameters: Array<Record<string, unknown>> } {
  const status = id === 'imported-draft' || id === DRAFT_CONTRACT_ID ? 'Draft' : id === ACTIVE_CONTRACT_ID ? 'Active' : 'Retired';
  const contract = contractVersion(id, 1, id === 'imported-draft' ? 'v4-importado' : status === 'Draft' ? 'v3-borrador' : status === 'Active' ? 'v2-activa' : 'v1-retirada', status, status === 'Draft', longContent) as Record<string, any>;
  return {
    contract,
    parameters: [
      {
        id: 9001,
        contractVersionId: id,
        methodId: 1,
        parameterPath: 'OFIDTX',
        xmlElementName: 'OFIDTX',
        displayName: 'Número de referencia',
        descriptionEs: 'Referencia funcional de la transacción.',
        uiHelpText: 'Seleccione el origen funcional que alimenta este dato.',
        category: 'Transacción',
        exampleValue: 'REF-123',
        dataType: 'string',
        xsdType: 'xsd:string',
        direction: 'Input',
        cardinality: 'Scalar',
        required: true,
        nillable: false,
        minOccurs: 1,
        maxOccurs: 1,
        supportStatus: 'Supported',
        sortOrder: 1,
        isActive: true
      }
    ]
  };
}

function buildArtifactAnalysis(validated: boolean, longContent: boolean): Record<string, unknown> {
  return {
    fileName: 'contrato-cfa.wsdl',
    artifactKind: 'Wsdl11',
    sizeBytes: 8421,
    sha256: 'b'.repeat(64),
    operationName: validated ? 'Proc_Transacciones' : null,
    targetNamespace: longContent ? 'https://cfa.example.test/contracts/very/long/imported/namespace/for/reflow' : 'https://cfa.example.test/imported',
    soapAction: validated ? 'https://cfa.example.test/actions/Proc_Transacciones' : null,
    availableOperations: ['Proc_Transacciones', 'Proc_Contrapartidas'],
    importable: validated,
    issues: validated ? [] : [{ code: 'OPERATION_SELECTION_REQUIRED', message: 'Seleccione una operación compatible para continuar.' }],
    parameters: [
      { parameterPath: 'OFIDTX', xmlElementName: 'OFIDTX', displayName: 'Número de referencia', dataType: 'string', xsdType: 'xsd:string', direction: 'Input', cardinality: 'Scalar', required: true, nillable: false, minOccurs: 1, maxOccurs: 1, supportStatus: 'Supported', sortOrder: 1 },
      { parameterPath: 'NEWFIELD', xmlElementName: 'NEWFIELD', displayName: 'Dato nuevo de negocio', dataType: 'string', xsdType: 'xsd:string', direction: 'Input', cardinality: 'Scalar', required: true, nillable: false, minOccurs: 1, maxOccurs: 1, supportStatus: 'Supported', sortOrder: 2 }
    ]
  };
}

function buildContractDiff(baseId: string, longContent: boolean): Record<string, unknown> {
  return {
    leftContractVersionId: baseId,
    rightContractVersionId: 'artifact-preview',
    methodId: 1,
    summary: { added: 1, removed: 1, modified: 6, requiresMapping: 4, requiresDevelopment: 4 },
    changes: [
      { changeType: 'Added', technicalName: 'NEWFIELD', functionalName: 'Dato nuevo de negocio', impact: 'MappingRequired', leftValue: null, rightValue: 'string' },
      { changeType: 'Removed', technicalName: 'OLDFIELD', functionalName: 'Dato retirado', impact: 'RequiresDevelopment', leftValue: 'string', rightValue: null },
      { changeType: 'RequiredChanged', technicalName: 'OFIDTX', functionalName: 'Número de referencia', impact: 'MappingRequired', leftValue: 'Opcional', rightValue: 'Obligatorio' },
      { changeType: 'TypeChanged', technicalName: 'AMOUNT', functionalName: 'Valor', impact: 'RequiresDevelopment', leftValue: 'int', rightValue: 'decimal' },
      { changeType: 'CardinalityChanged', technicalName: 'DETAIL', functionalName: 'Detalle', impact: 'MappingRequired', leftValue: 'Scalar', rightValue: 'Collection' },
      { changeType: 'OrderChanged', technicalName: 'ORDER', functionalName: 'Orden funcional', impact: 'MappingRequired', leftValue: '2', rightValue: '3' },
      { changeType: 'NamespaceChanged', technicalName: 'NSFIELD', functionalName: 'Namespace técnico', impact: 'RequiresDevelopment', leftValue: 'urn:old', rightValue: longContent ? 'https://cfa.example.test/namespace/with/a/very/long/value/that/must/wrap' : 'urn:new' },
      { changeType: 'DirectionChanged', technicalName: 'DIRECTION', functionalName: 'Dirección', impact: 'RequiresDevelopment', leftValue: 'Input', rightValue: 'Output' }
    ]
  };
}

function buildConnectionSettings(longContent: boolean): any {
  return {
    revision: 'rev-7',
    expectedRevision: null,
    wscfaachMappings: [
      connectionMethod('Proc_Transacciones', longContent, 'DryRun', true),
      connectionMethod('Proc_Contrapartidas', longContent, 'DryRun', true)
    ],
    wsAxonRespuestaTransaccionesMappings: [
      connectionMethod('RegistrarRespuestaTransaccion', longContent, 'Disabled', false)
    ]
  };
}

function connectionMethod(methodName: string, longContent: boolean, operatingMode: string, enabled: boolean): Record<string, unknown> {
  const real = enabled && operatingMode === 'Live';
  return {
    methodName,
    endpoint: sanitizedDestinationFor(methodName, longContent),
    soapAction: `https://cfa.example.test/actions/${methodName}`,
    operatingMode,
    enabled,
    sanitizedEndpoint: sanitizedDestinationFor(methodName, longContent),
    sanitizedSoapAction: `https://cfa.example.test/actions/${methodName}`,
    configurationState: !enabled || operatingMode === 'Disabled' ? 'Disabled' : real ? 'RealSendEnabled' : 'Simulation',
    capabilities: { configurationStateKnown: true, canRequestRealSend: true, blockingReason: null },
    inputParameterMappings: [{ inputName: 'dato', soapParameterName: 'Dato', defaultValue: null, required: true }]
  };
}

function sanitizedDestinationFor(methodName: string, longContent: boolean): string {
  return longContent
    ? `https://cfa.example.test/services/${methodName}/with/a/very/long/sanitized/path/without/query/or/secret`
    : `https://cfa.example.test/services/${methodName}`;
}

function safeJsonBody(postData: string | null): Record<string, any> {
  if (!postData) return {};
  try {
    return JSON.parse(postData) as Record<string, any>;
  } catch {
    return {};
  }
}

async function json(route: Route, status: number, body: unknown): Promise<void> {
  await route.fulfill({
    status,
    contentType: 'application/json; charset=utf-8',
    body: JSON.stringify(body)
  });
}

function createUnsignedJwt(payload: Record<string, unknown>): string {
  const header = { alg: 'none', typ: 'JWT' };
  return `${base64UrlEncode(JSON.stringify(header))}.${base64UrlEncode(JSON.stringify(payload))}.`;
}

function base64UrlEncode(value: string): string {
  return Buffer.from(value)
    .toString('base64')
    .replace(/=/g, '')
    .replace(/\+/g, '-')
    .replace(/\//g, '_');
}
