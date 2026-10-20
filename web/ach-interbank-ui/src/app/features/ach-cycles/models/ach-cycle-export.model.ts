export interface ExportableAchCycle {
  id: string;
  cycleId?: string | null;
  cycleName: string;
  processingDate: string;
  clearingHouseId?: number;
  clearingHouseName?: string;
  batchCount?: number;
  transactionCount: number;
  isExportable?: boolean;
  exportUnavailableReason?: string | null;
  exportIdentifier?: string | null;
  nachaId?: string | null;
  fileHash?: string | null;
  hash?: string | null;
  hasGeneratedFile?: boolean;
  hasDigitalEnvelope?: boolean;
  fileName?: string | null;
  generatedAtUtc?: string | null;
  protectedAtUtc?: string | null;
}

export interface ExportableAchCycleFilter {
  clearingHouseId?: number;
  startDate?: string;
  endDate?: string;
}
