export type OperationalErrorSeverity = 'error' | 'warning' | 'info';

export interface OperationalErrorSupport {
  errorCode?: string;
  traceId?: string;
  ruleId?: string;
  recordType?: string;
  fieldCode?: string;
  fieldDisplayName?: string;
  startPosition?: number;
  expectedLength?: number;
  reason?: string;
  endpoint?: string;
}

export interface OperationalErrorView {
  title: string;
  message: string;
  action: string;
  severity: OperationalErrorSeverity;
  retryable: boolean;
  correctionLabel?: string;
  correctionRoute?: string;
  support: OperationalErrorSupport;
}
