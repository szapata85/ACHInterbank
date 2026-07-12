import type { IncomingProcTransaccionesFixture } from './incoming-proc-transacciones-fixture';

export type SoapEndpointMethodMapping = {
  methodName: string;
  endpoint: string;
  soapAction: string;
  enabled: boolean;
};

export type SoapIntegrationSettings = {
  wscfaachMappings: SoapEndpointMethodMapping[];
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

export function getConfirmedSoapCorrelationTokens(requestPayloadXml: string, fixture: IncomingProcTransaccionesFixture): string[] {
  const idTran = readSoapElement(requestPayloadXml, 'IDTRAN');
  const idLote = readSoapElement(requestPayloadXml, 'IDLOTE');
  if (idTran !== fixture.idTran || idLote !== fixture.idLote) {
    throw new Error('RequestPayloadXml no confirma los tokens IDTRAN/IDLOTE esperados para correlación SOAP.');
  }
  return [idTran, idLote];
}

export function assertProcTransaccionesEndpointConfigured(settings: SoapIntegrationSettings): string {
  const mapping = settings.wscfaachMappings?.find((item) => item.methodName === 'Proc_Transacciones');
  if (!mapping?.enabled || !mapping.endpoint?.trim()) {
    throw new Error('La configuración autenticada no contiene un endpoint habilitado para Proc_Transacciones.');
  }
  return mapping.endpoint.trim();
}

export function blockWithoutEffectiveApiMode(): never {
  throw new Error('BLOCKED_EFFECTIVE_API_MODE: no existe un endpoint autenticado que exponga el modo efectivo resuelto por la API para Proc_Transacciones. La configuración SOAP solo confirma endpoint/mapping; este spec no puede ejecutar LIVE automáticamente.');
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
