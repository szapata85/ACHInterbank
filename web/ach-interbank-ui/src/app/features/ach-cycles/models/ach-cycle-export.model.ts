export interface ExportableAchCycle {
  id: string;
  cycleId?: string | null;
  cycleName: string;
  processingDate: string;
  clearingHouseName?: string;
  transactionCount: number;
  isExportable?: boolean;
  exportUnavailableReason?: string | null;
  exportIdentifier?: string | null;
  nachaId?: string | null;
  fileHash?: string | null;
  hash?: string | null;
}

export interface ExportableAchCycleFilter {
  clearingHouseId?: number;
  startDate?: string;
  endDate?: string;
}
