import type {
  IncomingProcTransaccionesFixture,
  IncomingProcTransaccionesFixtureInput
} from './incoming-proc-transacciones-fixture';

export type SoapEndpointMethodMapping = {
  methodName: string;
  endpoint: string;
  soapAction: string;
  enabled: boolean;
};

export type ProcTransaccionesEffectiveSettings = {
  operation: string;
  effectiveMode: string;
  endpoint: string;
  enabled: boolean;
  mappingReady: boolean;
};

export type SoapIntegrationSettings = {
  wscfaachMappings: SoapEndpointMethodMapping[];
  procTransaccionesEffectiveSettings?: ProcTransaccionesEffectiveSettings;
};

export type ProcTransaccionesSyntheticSetupResult = {
  isReady: boolean;
  setupAuthorized: boolean;
  cfaInstitutionId: number;
  externalInstitutionId: number;
  transactionId: number;
  receivingDfi: string;
  externalOriginRouting: string;
  receiverAccountMasked: string;
  authorizedAmount: number;
  transactionExternalId: string;
};

export function readAuthorizedFixtureInput(
  uniqueRunKey: string,
  setup: Pick<ProcTransaccionesSyntheticSetupResult, 'receivingDfi' | 'externalOriginRouting'>,
  environment: NodeJS.ProcessEnv = process.env
): IncomingProcTransaccionesFixtureInput {
  const receiverAccount = environment['ACH_E2E_PROC_TRANSACCIONES_RECEIVER_ACCOUNT']?.trim() ?? '';
  const rawAmount = environment['ACH_E2E_PROC_TRANSACCIONES_EXPECTED_AMOUNT']?.trim() ?? '';
  if (!receiverAccount) {
    throw new Error('La cuenta autorizada ACH_E2E_PROC_TRANSACCIONES_RECEIVER_ACCOUNT es obligatoria y no tiene fallback.');
  }
  if (!rawAmount) {
    throw new Error('El monto autorizado ACH_E2E_PROC_TRANSACCIONES_EXPECTED_AMOUNT es obligatorio y no tiene fallback.');
  }
  const amount = parseAuthorizedProcTransaccionesAmount(rawAmount);
  return {
    receiverAccount,
    receivingDfi: setup.receivingDfi,
    amount,
    externalOriginRouting: setup.externalOriginRouting,
    uniqueRunKey
  };
}

export function assertSyntheticSetupAuthorization(
  environment: NodeJS.ProcessEnv = process.env
): void {
  if ((environment['ALLOW_PROC_TRANSACCIONES_SYNTHETIC_DATA_SETUP'] ?? '').trim().toLowerCase() !== 'true') {
    throw new Error('ALLOW_PROC_TRANSACCIONES_SYNTHETIC_DATA_SETUP=true es obligatorio para preparar datos sintéticos; no autoriza SOAP LIVE.');
  }
}

export function assertSyntheticSetupReadiness(result: ProcTransaccionesSyntheticSetupResult): void {
  if (!result.isReady
    || !result.setupAuthorized
    || !Number.isInteger(result.cfaInstitutionId)
    || result.cfaInstitutionId <= 0
    || !Number.isInteger(result.externalInstitutionId)
    || result.externalInstitutionId <= 0
    || result.externalInstitutionId === result.cfaInstitutionId
    || !Number.isInteger(result.transactionId)
    || result.transactionId <= 0
    || !/^\d{9}$/.test(result.receivingDfi)
    || !/^\d{8}$/.test(result.externalOriginRouting)
    || !result.receiverAccountMasked.includes('*')
    || !Number.isFinite(result.authorizedAmount)
    || result.authorizedAmount <= 0
    || !result.transactionExternalId.startsWith('E2E-PTX-IN-')) {
    throw new Error('El setup Proc_Transacciones no confirma CFA única, origen sintético, transacción receptora y valores autorizados.');
  }
}

export function assertExpectedReceiverAccount(fixture: IncomingProcTransaccionesFixture, expectedAccount: string): void {
  if (!expectedAccount || fixture.receiverAccount !== expectedAccount.trim()) {
    throw new Error(`La cuenta receptora del fixture (${maskSensitive(fixture.receiverAccount)}) no coincide con ACH_E2E_PROC_TRANSACCIONES_RECEIVER_ACCOUNT (${maskSensitive(expectedAccount)}).`);
  }
}

export function assertExpectedAmount(fixture: IncomingProcTransaccionesFixture, expectedAmount: string): void {
  const parsed = parseAuthorizedProcTransaccionesAmount(expectedAmount);
  if (!Number.isFinite(parsed) || parsed <= 0 || fixture.amount !== parsed) {
    throw new Error('El monto del fixture no coincide con ACH_E2E_PROC_TRANSACCIONES_EXPECTED_AMOUNT. La carga NACHA-M fue bloqueada antes del upload.');
  }
}

export function parseAuthorizedProcTransaccionesAmount(rawAmount: string): number {
  const normalized = rawAmount.trim();
  if (!/^\d+(?:[.,]\d{1,2})?$/.test(normalized)) {
    throw new Error('El monto autorizado debe ser decimal sin separadores de miles, usando punto o coma y máximo dos decimales.');
  }
  const amount = Number(normalized.replace(',', '.'));
  if (!Number.isFinite(amount) || amount <= 0) {
    throw new Error('El crédito entrante requiere un monto mayor que cero.');
  }
  return amount;
}

export function assertEffectiveProcTransaccionesPreflight(
  settings: SoapIntegrationSettings,
  expectedEndpoint: string
): string {
  const mapping = settings.wscfaachMappings?.find((item) => item.methodName === 'Proc_Transacciones');
  const effective = settings.procTransaccionesEffectiveSettings;
  if (!mapping?.enabled
    || !mapping.endpoint?.trim()
    || effective?.operation !== 'Proc_Transacciones'
    || effective.effectiveMode !== 'Live'
    || !effective.enabled
    || !effective.mappingReady
    || effective.endpoint?.trim() !== expectedEndpoint.trim()
    || mapping.endpoint.trim() !== effective.endpoint.trim()) {
    throw new Error('El preflight efectivo de Proc_Transacciones no confirma Live, endpoint esperado, integración habilitada y mapping listo. La carga NACHA-M fue bloqueada antes del upload.');
  }
  return effective.endpoint.trim();
}

export function getConfirmedSoapCorrelationTokens(
  requestPayloadXml: string,
  source: Pick<IncomingProcTransaccionesFixture, 'idTran' | 'idLote'>
): string[] {
  const idTran = readSoapElement(requestPayloadXml, 'IDTRAN');
  const idLote = readSoapElement(requestPayloadXml, 'IDLOTE');
  if (idTran !== source.idTran || idLote !== source.idLote) {
    throw new Error('RequestPayloadXml no confirma los tokens IDTRAN/IDLOTE esperados para correlación SOAP.');
  }
  return [idTran, idLote];
}

export function maskSensitive(value: string): string {
  const normalized = value?.trim() ?? '';
  if (!normalized) {
    return '<vacío>';
  }
  return normalized.length <= 4 ? '****' : `${'*'.repeat(Math.max(4, normalized.length - 4))}${normalized.slice(-4)}`;
}

function readSoapElement(xml: string, localName: string): string {
  const match = new RegExp(`<[^>]*${localName}[^>]*>([^<]*)<\/[^>]*${localName}>`, 'i').exec(xml);
  if (!match?.[1]) {
    throw new Error(`RequestPayloadXml no contiene ${localName}.`);
  }
  return match[1].trim();
}
