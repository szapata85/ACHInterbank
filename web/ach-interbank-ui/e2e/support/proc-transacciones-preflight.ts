import type { IncomingProcTransaccionesFixture } from './incoming-proc-transacciones-fixture';

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

export function assertExpectedReceiverAccount(fixture: IncomingProcTransaccionesFixture, expectedAccount: string): void {
  if (!expectedAccount || fixture.receiverAccount !== expectedAccount.trim()) {
    throw new Error(`La cuenta receptora del fixture (${maskSensitive(fixture.receiverAccount)}) no coincide con ACH_E2E_PROC_TRANSACCIONES_RECEIVER_ACCOUNT (${maskSensitive(expectedAccount)}).`);
  }
}

export function assertExpectedAmount(fixture: IncomingProcTransaccionesFixture, expectedAmount: string): void {
  const parsed = Number(expectedAmount);
  if (!Number.isFinite(parsed) || parsed < 0 || fixture.amount !== parsed) {
    throw new Error('El monto del fixture no coincide con ACH_E2E_PROC_TRANSACCIONES_EXPECTED_AMOUNT. La carga NACHA-M fue bloqueada antes del upload.');
  }
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

export function getConfirmedSoapCorrelationTokens(requestPayloadXml: string, fixture: IncomingProcTransaccionesFixture): string[] {
  const idTran = readSoapElement(requestPayloadXml, 'IDTRAN');
  const idLote = readSoapElement(requestPayloadXml, 'IDLOTE');
  if (idTran !== fixture.idTran || idLote !== fixture.idLote) {
    throw new Error('RequestPayloadXml no confirma los tokens IDTRAN/IDLOTE esperados para correlacion SOAP.');
  }
  return [idTran, idLote];
}

export function maskSensitive(value: string): string {
  const normalized = value?.trim() ?? '';
  if (!normalized) {
    return '<vacio>';
  }
  return normalized.length <= 4 ? '****' : `${'*'.repeat(Math.max(4, normalized.length - 4))}${normalized.slice(-4)}`;
}

function readSoapElement(xml: string, localName: string): string {
  const match = new RegExp(`<[^>]*${localName}[^>]*>([^<]*)<\\/[^>]*${localName}>`, 'i').exec(xml);
  if (!match?.[1]) {
    throw new Error(`RequestPayloadXml no contiene ${localName}.`);
  }
  return match[1].trim();
}
