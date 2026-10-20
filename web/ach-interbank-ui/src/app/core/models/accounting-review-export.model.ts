export type AccountingReviewExportFormat = 'pdf' | 'csv' | 'excel' | 'xlsx';

export interface AccountingReviewExportRequest {
  format: AccountingReviewExportFormat;
  csvDelimiter?: string;
  requestedBy?: string;
  correlationId?: string;
  dateFrom?: string;
  dateTo?: string;
  clearingHouseCode?: string;
  cycleCode?: string;
  fileId?: string;
  fileHash?: string;
  transactionId?: string;
  status?: string;
  causeCode?: string;
  includeOutbound?: boolean;
  includeIncoming?: boolean;
  includeReturns?: boolean;
  includeReturnOfReturn?: boolean;
  includeManualAuditOnly?: boolean;
  includeNetting?: boolean;
  includeLiquidity?: boolean;
  includeCudEvidence?: boolean;
}
