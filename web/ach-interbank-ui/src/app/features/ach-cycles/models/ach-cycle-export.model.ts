export interface ExportableAchCycle {
  id: string;
  cycleName: string;
  processingDate: string;
  clearingHouseName?: string;
  transactionCount: number;
  isExportable?: boolean;
  exportUnavailableReason?: string | null;
}

export interface ExportableAchCycleFilter {
  clearingHouseId?: number;
  startDate?: string;
  endDate?: string;
}
