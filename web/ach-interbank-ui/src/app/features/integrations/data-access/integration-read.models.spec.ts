import {
  IntegrationContractParameterDto,
  IntegrationContractVersionDto,
  IntegrationMethodDto
} from './integration-api.models';
import {
  toIntegrationContractVersionReadModel,
  toIntegrationInformationParameterReadModel,
  toIntegrationParameterReadModel,
  toIntegrationServiceReadModel
} from './integration-read.models';

describe('integration read models', () => {
  const method: IntegrationMethodDto = {
    id: 7,
    code: 'PROC_TRANSACCIONES',
    displayName: 'Créditos recibidos en CFA',
    soapClientCode: 'WSCFAACH',
    isActive: true,
    integrationKey: 'CFA',
    operationKey: 'Proc_Transacciones',
    mappingDirection: 'Inbound',
    mappingPurpose: 'Procesa créditos recibidos por CFA.',
    functionalNature: 'Credit',
    functionalOriginator: 'External',
    movesMoney: true
  };

  it('usa metadata funcional del backend y no inventa estado contractual o capacidades', () => {
    const readModel = toIntegrationServiceReadModel(method);

    expect(readModel.functionalTitle).toBe(method.displayName);
    expect(readModel.functionalDescription).toBe(method.mappingPurpose);
    expect(readModel.technicalMethodName).toBe(method.operationKey);
    expect(readModel.contractStatus).toBeNull();
    expect(readModel.mappingStatus).toBeNull();
    expect(readModel.canPublish).toBeNull();
    expect(readModel.canManageContract).toBeNull();
  });

  it('proyecta únicamente estados recibidos del backend en el snapshot', () => {
    const contract: IntegrationContractVersionDto = {
      id: '11111111-1111-1111-1111-111111111111',
      methodId: method.id,
      versionLabel: 'v2',
      status: 'Validated',
      operationName: 'Proc_Transacciones',
      targetNamespace: 'urn:test',
      soapAction: 'urn:test/action',
      sourceType: 'WsdlImport',
      revision: 4
    };

    const readModel = toIntegrationServiceReadModel(method, {
      contract,
      capabilities: { canManageContract: true, canPublish: false }
    });

    expect(readModel.contractVersionLabel).toBe('v2');
    expect(readModel.contractStatus).toBe('Validated');
    expect(readModel.canManageContract).toBeTrue();
    expect(readModel.canPublish).toBeFalse();
    expect(readModel.canEditMapping).toBeNull();
  });

  it('mantiene separado el detalle funcional del detalle técnico de parámetros', () => {
    const parameter: IntegrationContractParameterDto = {
      id: 23,
      contractVersionId: '11111111-1111-1111-1111-111111111111',
      methodId: method.id,
      parameterPath: 'TIPOCLIENTE',
      xmlElementName: 'TIPOCLIENTE',
      displayName: 'Tipo de cliente',
      descriptionEs: 'Clasificación funcional del cliente.',
      uiHelpText: 'Seleccione el origen autorizado.',
      category: 'Cliente',
      exampleValue: 'N',
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
    };

    const readModel = toIntegrationParameterReadModel(parameter, {
      validation: {
        parameterId: parameter.id,
        parameterPath: parameter.parameterPath,
        required: true,
        status: 'incomplete',
        resolutionKind: 'none',
        hints: ['REQUIRED_PARAMETER_MISSING']
      }
    });

    expect(readModel.functionalName).toBe('Tipo de cliente');
    expect(readModel.status).toBe('incomplete');
    expect(readModel.sourceSummary).toBeNull();
    expect(readModel.uiHelpText).toBe('Seleccione el origen autorizado.');
    expect(readModel.technicalDetails.xsdType).toBe('xsd:string');
    expect(readModel.technicalDetails.supportStatus).toBe('Supported');
  });

  it('proyecta el nombre funcional y conserva el detalle técnico como sección separada', () => {
    const readModel = toIntegrationInformationParameterReadModel({
      parameterId: 44,
      functionalName: 'Tipo de cliente',
      functionalDescription: 'Clasificación funcional del cliente.',
      technicalName: 'TIPOCLIENTE',
      required: true,
      dataType: 'string',
      category: 'Cliente',
      status: 'Pending',
      source: 'Referencia de la transacción',
      transformation: 'Recortar',
      currentRule: {
        ruleId: 71,
        sourceCatalogFieldId: 18,
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
        targetNamespace: 'urn:cfa',
        sortOrder: 20
      }
    });

    expect(readModel.functionalName).toBe('Tipo de cliente');
    expect(readModel.status).toBe('Pending');
    expect(readModel.sourceSummary).toBe('Referencia de la transacción');
    expect(readModel.transformationSummary).toBe('Recortar');
    expect(readModel.currentRule?.sourceCatalogFieldId).toBe(18);
    expect(readModel.technicalDetails.parameterPath).toBe('TIPOCLIENTE');
    expect(readModel.technicalDetails.targetNamespace).toBe('urn:cfa');
  });

  it('proyecta conexión deshabilitada desde el estado backend sin inferir modo operativo', () => {
    const readModel = toIntegrationServiceReadModel(method, {
      connection: {
        methodName: 'Proc_Transacciones',
        endpoint: 'https://local.test/service',
        soapAction: 'urn:test/action',
        operatingMode: 'Live',
        enabled: false,
        sanitizedEndpoint: 'https://local.test/service',
        sanitizedSoapAction: 'urn:test/action',
        configurationState: 'Disabled',
        inputParameterMappings: []
      }
    });

    expect(readModel.connectionStatus).toBe('Disabled');
  });

  it('agrupa Validated como borrador visible sin derivar permisos de edición en Angular', () => {
    const contract: IntegrationContractVersionDto = {
      id: '22222222-2222-2222-2222-222222222222',
      methodId: method.id,
      versionLabel: 'v3',
      status: 'Validated',
      operationName: 'Proc_Transacciones',
      targetNamespace: 'urn:test',
      soapAction: 'urn:test/action',
      sourceType: 'ManualApproved',
      revision: 7,
      importedAtUtc: '2026-10-13T10:00:00Z',
      importedBy: 'operador',
      capabilities: {
        isReadOnly: false,
        canEditFunctionalMetadata: true
      }
    };

    const readModel = toIntegrationContractVersionReadModel(contract, [{
      id: '33333333-3333-3333-3333-333333333333',
      methodId: method.id,
      methodCode: 'PROC_TRANSACCIONES',
      contractVersionId: contract.id,
      revision: 4,
      name: 'Relación v3',
      version: 3,
      status: 'Draft',
      isActive: false,
      notes: '',
      publishedBy: '',
      rules: []
    }]);

    expect(readModel.displayState).toBe('Draft');
    expect(readModel.statusLabel).toBe('Borrador validado');
    expect(readModel.canEditFunctionalMetadata).toBeTrue();
    expect(readModel.isReadOnly).toBeFalse();
    expect(readModel.relationship.label).toBe('Relación en borrador');
  });

  it('falla cerrado si backend no entrega capacidades contractuales', () => {
    const contract: IntegrationContractVersionDto = {
      id: '44444444-4444-4444-4444-444444444444',
      methodId: method.id,
      versionLabel: 'v1',
      status: 'Draft',
      operationName: 'Proc_Transacciones',
      targetNamespace: 'urn:test',
      soapAction: 'urn:test/action',
      sourceType: 'BaselineMigration',
      revision: 1
    };

    const readModel = toIntegrationContractVersionReadModel(contract);

    expect(readModel.isReadOnly).toBeTrue();
    expect(readModel.canEditFunctionalMetadata).toBeFalse();
  });

});
